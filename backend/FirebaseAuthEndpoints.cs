using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace Society.Api;

public static class FirebaseAuthEndpoints
{
    // Firebase's stable UID identifies the same person across providers connected
    // in that Firebase account. Never use an email address as an external key.
    public static string ProviderKey(FirebaseSettings settings, FirebaseIdentity identity) => "Firebase:" + settings.ProjectId;
    public static Claim[] RecentClaims(string method) => [new("amr", method), new("auth_time", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())];
    public static bool Recent(ClaimsPrincipal principal, bool mfaRequired)
    {
        var key = mfaRequired ? "mfa_at" : "auth_time";
        return (!mfaRequired || principal.FindFirstValue("amr") == "mfa") && long.TryParse(principal.FindFirstValue(key), out var time) && time >= DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds();
    }
    public static void Map(RouteGroupBuilder auth)
    {
        auth.MapGet("/providers", (IConfiguration config) =>
        {
            var settings = FirebaseSettings.Read(config);
            return new { enabled = settings.Ready, firebase = settings.Ready ? new { apiKey = settings.ApiKey, authDomain = settings.AuthDomain, projectId = settings.ProjectId, appId = settings.AppId } : null,
                providers = settings.Ready ? settings.Providers.Where(p => p is "google.com" or "microsoft.com").ToArray() : [], usernamePassword = true };
        }).RequireRateLimiting("verify");
        auth.MapPost("/firebase", async (FirebaseSignIn r, IConfiguration config, AppDb db, UserManager<Account> users, SignInManager<Account> signIn, IFirebaseIdentityVerifier verifier, CancellationToken ct) =>
        {
            var settings = FirebaseSettings.Read(config);
            var identity = await verifier.Verify(r.IdToken, settings, ct);
            if (!MembershipExperience.Kinds.Contains(r.MembershipKind)) throw new DomainException("Choose a valid membership type");
            Account? user;
            await using (var tx = await db.Database.BeginTransactionAsync(ct))
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({"auth-email:" + identity.Email.ToUpperInvariant()}))", ct);
                user = await users.FindByLoginAsync(ProviderKey(settings, identity), identity.Uid);
                if (user == null)
                {
                    if (await users.FindByEmailAsync(identity.Email) != null)
                        return Results.Ok(new { requiresAccountLink = true, message = "Confirm your existing account with its password or a previously connected provider. This keeps your memberships and roles together." });
                    var tenant = r.SocietyId ?? (Guid.TryParse(config["Society:DefaultTenantId"], out var defaultTenant) ? defaultTenant : (Guid?)null);
                    if (tenant != null && !await db.Tenants.AnyAsync(t => t.Id == tenant && t.Status == "Active", ct)) throw new DomainException("Choose an active society");
                    user = new Account { Id = Guid.NewGuid(), UserName = identity.Email, Email = identity.Email, EmailConfirmed = identity.EmailVerified, FullName = identity.Name,
                        PreferredMembershipKind = r.MembershipKind, SignupTenantId = tenant };
                    var created = await users.CreateAsync(user);
                    if (!created.Succeeded) throw new DomainException("The account could not be created. Try username/password sign-in");
                    var linked = await users.AddLoginAsync(user, new UserLoginInfo(ProviderKey(settings, identity), identity.Uid, identity.Provider));
                    if (!linked.Succeeded) throw new DomainException("The provider could not be connected");
                    if (!user.EmailConfirmed)
                    {
                        var token = await users.GenerateEmailConfirmationTokenAsync(user);
                        db.Outbox.Add(new Outbox { Recipient = user.Email!, Subject = "Verify your email", Body = $"{config["PublicUrl"]}/verify?userId={user.Id}&token={Uri.EscapeDataString(token)}" });
                    }
                    db.Audits.Add(new Audit { TenantId = Guid.Empty, ActorId = user.Id, Action = "auth.external.registered", ResourceId = user.Id, Details = JsonSerializer.Serialize(new { identity.Provider }) });
                    await db.SaveChangesAsync(ct);
                }
                await tx.CommitAsync(ct);
            }
            if (!user.EmailConfirmed) return Results.Ok(new { requiresEmailVerification = true, message = "Verify this email before signing in. A verification email has been requested; delivery resumes when email is connected." });
            var result = await signIn.ExternalLoginSignInAsync(ProviderKey(settings, identity), identity.Uid, false, bypassTwoFactor: false);
            if (result.RequiresTwoFactor) return Results.Ok(new { requiresTwoFactor = true });
            if (!result.Succeeded) return Results.Problem(statusCode: 401, title: "Sign in failed. Verify your account or try again later");
            await signIn.SignInWithClaimsAsync(user, false, RecentClaims("external"));
            return Results.Ok(new { message = "Signed in" });
        });
        auth.MapPost("/firebase/link", async (FirebaseSignIn r, IConfiguration config, AppDb db, UserManager<Account> users, Access access, HttpContext context, IFirebaseIdentityVerifier verifier, CancellationToken ct) =>
        {
            var user = await users.FindByIdAsync(access.UserId.ToString()) ?? throw new AccessDeniedException();
            if (await users.IsLockedOutAsync(user)) throw new AccessDeniedException();
            if (!user.EmailConfirmed || !Recent(context.User, user.TwoFactorEnabled)) throw new DomainException("Sign in again to confirm your account before connecting a provider");
            var settings = FirebaseSettings.Read(config); var identity = await verifier.Verify(r.IdToken, settings, ct);
            if (!string.Equals(user.Email, identity.Email, StringComparison.OrdinalIgnoreCase)) throw new DomainException("The provider email must match your existing account email");
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({"auth-email:" + identity.Email.ToUpperInvariant()}))", ct);
            var existing = await users.FindByLoginAsync(ProviderKey(settings, identity), identity.Uid);
            if (existing != null && existing.Id != user.Id) throw new DomainException("This provider is connected to another account");
            if (existing == null)
            {
                var result = await users.AddLoginAsync(user, new UserLoginInfo(ProviderKey(settings, identity), identity.Uid, identity.Provider));
                if (!result.Succeeded) throw new DomainException("This provider could not be connected");
                access.Audit(Guid.Empty, "auth.external.linked", user.Id, new { identity.Provider }); await db.SaveChangesAsync(ct);
            }
            await tx.CommitAsync(ct); return Results.Ok(new { message = "Provider connected. Your account, memberships and permissions are unchanged." });
        }).RequireAuthorization();
    }
}
