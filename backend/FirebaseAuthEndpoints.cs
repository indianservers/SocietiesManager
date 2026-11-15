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
                providers = settings.Ready ? settings.Providers.Where(p => p is "google.com" or "microsoft.com" or "phone").ToArray() : [], usernamePassword = true };
        }).RequireRateLimiting("verify");
        auth.MapPost("/firebase", async (FirebaseSignIn r, IConfiguration config, AppDb db, UserManager<Account> users, SignInManager<Account> signIn, IFirebaseIdentityVerifier verifier, HttpContext context, CancellationToken ct) =>
        {
            var settings = FirebaseSettings.Read(config);
            var identity = await verifier.Verify(r.IdToken, settings, ct);
            if (!MembershipExperience.Kinds.Contains(r.MembershipKind)) throw new DomainException("Choose a valid membership type");
            Account? user;
            await using (var tx = await db.Database.BeginTransactionAsync(ct))
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({(identity.Provider=="phone"?"auth-phone:"+identity.PhoneNumber:"auth-email:"+identity.Email.ToUpperInvariant())}))", ct);
                user = await users.FindByLoginAsync(ProviderKey(settings, identity), identity.Uid);
                if (user == null)
                {
                    if(identity.Provider=="phone")return Results.Ok(new{requiresAccountLink=true,message="Confirm your existing account with email OTP or password to connect this verified mobile number. New members should create and verify an email account first."});
                    var existingUser = await users.FindByEmailAsync(identity.Email);
                    if (existingUser != null)
                    {
                        // Google verifies ownership of this email; retain the existing member identity and MFA.
                        if (identity.Provider != "google.com" || !identity.EmailVerified || !existingUser.EmailConfirmed)
                            return Results.Ok(new { requiresAccountLink = true, email=identity.Email, message = "Confirm your existing account with email OTP or password to connect this provider." });
                        if (await users.IsLockedOutAsync(existingUser))
                            return Results.Problem(statusCode: 401, title: "Sign in failed. Try again later.");
                        var connected = await users.AddLoginAsync(existingUser, new UserLoginInfo(ProviderKey(settings, identity), identity.Uid, identity.Provider));
                        if (!connected.Succeeded) throw new DomainException("This provider could not be connected");
                        user = existingUser;
                        db.Audits.Add(new Audit { ActorId=user.Id, Action="auth.external.linked", ResourceId=user.Id, Details=JsonSerializer.Serialize(new { identity.Provider }) });
                        await db.SaveChangesAsync(ct);
                    }
                    else
                    {
                    if(!r.CreateAccount)return Results.Ok(new{requiresAccountLink=true,email=identity.Email,message="Connect this provider to your existing account using email OTP or password. New members can create an account from the signup page."});
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
                }
                await tx.CommitAsync(ct);
            }
            if (!user.EmailConfirmed) return Results.Ok(new { requiresEmailVerification = true, message = "Verify this email before signing in. A verification email has been requested; delivery resumes when email is connected." });
            return await MemberLogin.Complete(user, true, "external", context, signIn, db);
        });
        auth.MapPost("/firebase/link", async (FirebaseSignIn r, IConfiguration config, AppDb db, UserManager<Account> users, Access access, HttpContext context, IFirebaseIdentityVerifier verifier, CancellationToken ct) =>
        {
            var user = await users.FindByIdAsync(access.UserId.ToString()) ?? throw new AccessDeniedException();
            if (await users.IsLockedOutAsync(user)) throw new AccessDeniedException();
            if (!user.EmailConfirmed || !Recent(context.User, user.TwoFactorEnabled)) throw new DomainException("Sign in again to confirm your account before connecting a provider");
            var settings = FirebaseSettings.Read(config); var identity = await verifier.Verify(r.IdToken, settings, ct);
            if(identity.Provider!="phone"&&!identity.EmailVerified)throw new DomainException("Verify the provider email before connecting it to your account");
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({(identity.Provider=="phone"?"auth-phone:"+identity.PhoneNumber:"auth-email:"+identity.Email.ToUpperInvariant())}))", ct);
            if(identity.Provider=="phone"){
                if(identity.PhoneNumber==null)throw new DomainException("Verify your mobile number first");
                if(await db.Users.AnyAsync(u=>u.Id!=user.Id&&u.PhoneNumberConfirmed&&u.PhoneNumber==identity.PhoneNumber,ct))throw new DomainException("This verified mobile number is connected to another account");
                user.PhoneNumber=identity.PhoneNumber;user.PhoneNumberConfirmed=true;
                var updated=await users.UpdateAsync(user);if(!updated.Succeeded)throw new DomainException("The verified mobile number could not be connected");
            }
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
