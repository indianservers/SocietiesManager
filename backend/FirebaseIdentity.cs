using System.ComponentModel.DataAnnotations;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Society.Api;

public record FirebaseSettings(bool Enabled, string ProjectId, string ApiKey, string AuthDomain, string AppId, string[] Providers)
{
    public static FirebaseSettings Read(IConfiguration configuration)
    {
        var c = configuration.GetSection("Authentication:Firebase");
        return new(c.GetValue<bool>("Enabled"), c["ProjectId"] ?? "", c["ApiKey"] ?? "", c["AuthDomain"] ?? "", c["AppId"] ?? "", c.GetSection("Providers").Get<string[]>() ?? []);
    }
    public bool Ready => Enabled && ProjectId.Length > 0 && ApiKey.Length > 0 && AuthDomain.Length > 0;
}
public record FirebaseSignIn([property: Required, MaxLength(16000)] string IdToken, string MembershipKind = "Individual", Guid? SocietyId = null);
public record FirebaseIdentity(string Uid, string Email, bool EmailVerified, string Name, string Provider, long AuthTime);
public interface IFirebaseIdentityVerifier
{
    Task<FirebaseIdentity> Verify(string token, FirebaseSettings settings, CancellationToken ct);
}
public sealed class FirebaseIdentityVerifier : IFirebaseIdentityVerifier
{
    public Task<FirebaseIdentity> Verify(string token, FirebaseSettings settings, CancellationToken ct) => FirebaseTokenVerifier.Verify(token, settings, ct);
}

public static class FirebaseTokenVerifier
{
    static readonly HttpClient Client = new(new SocketsHttpHandler { AllowAutoRedirect = false, PooledConnectionLifetime = TimeSpan.FromMinutes(10) }) { Timeout = TimeSpan.FromSeconds(15) };

    // The JWT claims are only preconditions. Google's account-lookup endpoint validates
    // the signature; the signed subject must also match the returned project user.
    public static async Task<FirebaseIdentity> Verify(string token, FirebaseSettings settings, CancellationToken ct, HttpClient? http = null)
    {
        if (!settings.Ready) throw new DomainException("Social sign-in is not configured");
        try
        {
            var parts = token.Split('.');
            if (parts.Length != 3 || token.Length > 16000) throw new DomainException("Invalid sign-in token");
            static byte[] Decode(string value) => Convert.FromBase64String(value.Replace('-', '+').Replace('_', '/') + new string('=', (4 - value.Length % 4) % 4));
            using var header = JsonDocument.Parse(Decode(parts[0]));
            using var payload = JsonDocument.Parse(Decode(parts[1]));
            var claims = payload.RootElement;
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var uid = claims.GetProperty("sub").GetString() ?? "";
            var authTime = claims.GetProperty("auth_time").GetInt64();
            var provider = claims.GetProperty("firebase").GetProperty("sign_in_provider").GetString() ?? "";
            if (claims.GetProperty("firebase").TryGetProperty("tenant", out var identityTenant) && identityTenant.ValueKind != JsonValueKind.Null)
                throw new DomainException("This Firebase identity tenant is not configured for the portal");
            if (header.RootElement.GetProperty("alg").GetString() != "RS256" ||
                claims.GetProperty("aud").GetString() != settings.ProjectId || claims.GetProperty("iss").GetString() != "https://securetoken.google.com/" + settings.ProjectId ||
                uid.Length is < 1 or > 128 || claims.GetProperty("exp").GetInt64() <= now || claims.GetProperty("iat").GetInt64() > now + 30 ||
                authTime > now + 30 || authTime < now - 600 || provider is not ("google.com" or "microsoft.com") || !settings.Providers.Contains(provider))
                throw new DomainException("Sign-in expired or is not from an enabled provider. Sign in again");
            using var response = await (http ?? Client).PostAsJsonAsync("https://identitytoolkit.googleapis.com/v1/accounts:lookup?key=" + Uri.EscapeDataString(settings.ApiKey), new { idToken = token }, ct);
            if (!response.IsSuccessStatusCode) throw new DomainException("The sign-in token could not be verified. Sign in again");
            using var verified = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            var users = verified.RootElement.GetProperty("users");
            if (users.GetArrayLength() != 1) throw new DomainException("Sign-in account was not found");
            var user = users[0];
            var email = user.TryGetProperty("email", out var address) ? address.GetString() ?? "" : "";
            if (user.GetProperty("localId").GetString() != uid || !new EmailAddressAttribute().IsValid(email) || email.Length > 254 ||
                user.TryGetProperty("disabled", out var disabled) && disabled.GetBoolean() ||
                user.TryGetProperty("validSince", out var since) && long.TryParse(since.GetString(), out var oldest) && authTime < oldest ||
                !user.GetProperty("providerUserInfo").EnumerateArray().Any(p => p.GetProperty("providerId").GetString() == provider))
                throw new DomainException("This provider account is unavailable");
            var name = user.TryGetProperty("displayName", out var displayName) ? displayName.GetString() ?? email : email;
            return new(uid, email.Trim(), user.TryGetProperty("emailVerified", out var emailVerified) && emailVerified.GetBoolean(), name[..Math.Min(name.Length, 160)], provider, authTime);
        }
        catch (DomainException) { throw; }
        catch (Exception error) when (error is JsonException or FormatException or KeyNotFoundException or InvalidOperationException or ArgumentException)
        { throw new DomainException("Invalid sign-in token"); }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
        { throw new DomainException("The identity provider is temporarily unavailable. Use your username and password or try again"); }
    }
}
