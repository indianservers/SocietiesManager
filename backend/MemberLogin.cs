using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace Society.Api;

public class MemberSession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string DeviceHash { get; set; } = "";
    public string Device { get; set; } = "";
    public string Address { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
public class MemberLoginLink
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = "";
    public string SecurityStamp { get; set; } = "";
    public string ReturnTo { get; set; } = "/app/membership";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
}
public class MembershipLinkRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    public string Number { get; set; } = "";
    public string Note { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAt { get; set; }
    public Guid? ReviewerId { get; set; }
    public string ReviewNote { get; set; } = "";
}
public record MagicRequest([Required, EmailAddress, MaxLength(254)] string Email, string? ReturnTo);
public record MagicConsume([Required, MaxLength(200)] string Token);
public record PasskeyCredential([Required, MaxLength(20000)] string CredentialJson, [MaxLength(80)] string Name = "My passkey", bool RememberMe = false);
public record ProviderRemoval([Required] string LoginProvider, [Required] string ProviderKey);
public record LinkMembershipRequest(Guid TenantId, [Required, MaxLength(100)] string Number, [Required, MinLength(10), MaxLength(1000)] string Note);
public record LinkMembershipReview(bool Approve, [Required, MinLength(10), MaxLength(1000)] string Note);
public record ChecklistUpdate(bool Dismissed);

public static class MemberLogin
{
    public static void ConfigureModel(ModelBuilder b)
    {
        // Explicit mapping also applies to migration tooling, whose DbFactory has no Identity service options.
        b.Entity<IdentityUserPasskey<Guid>>(p => { p.HasKey(x => x.CredentialId); p.ToTable("AspNetUserPasskeys"); p.Property(x => x.CredentialId).HasMaxLength(1024); p.OwnsOne(x => x.Data).ToJson(); p.HasOne<Account>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade); });
        b.Entity<MemberSession>().HasIndex(x => new { x.UserId, x.CreatedAt });
        b.Entity<MemberSession>().HasOne<Account>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<MemberLoginLink>().HasIndex(x => x.TokenHash).IsUnique();
        b.Entity<MemberLoginLink>().HasIndex(x => new { x.UserId, x.CreatedAt });
        b.Entity<MemberLoginLink>().HasOne<Account>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<MembershipLinkRequest>().HasIndex(x => new { x.TenantId, x.Status });
        b.Entity<MembershipLinkRequest>().HasOne<Account>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<MembershipLinkRequest>().HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
    }

    public static string SafeReturn(string? value) => value != null && value.Length < 2000 &&
        (value == "/app" || value.StartsWith("/app/", StringComparison.Ordinal) || value.StartsWith("/app?", StringComparison.Ordinal)) &&
        !value.Any(c => char.IsControl(c) || c == '\\') && !value.Contains("%", StringComparison.Ordinal)
        ? value : "/app/membership";
    static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    static string Short(string? value, int max) => (value ?? "")[..Math.Min((value ?? "").Length, max)];
    static Guid User(ClaimsPrincipal p) => Guid.Parse(p.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Cookie events cover password, social, MFA and passkey sign-ins consistently.
    public static async Task SigningIn(CookieSigningInContext c)
    {
        if (c.Principal?.Identity is not ClaimsIdentity identity || !Guid.TryParse(c.Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var user)) return;
        var db = c.HttpContext.RequestServices.GetRequiredService<AppDb>();
        var device = c.Request.Cookies["society.device"];
        if (device == null || device.Length != 64)
        {
            device = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            c.Response.Cookies.Append("society.device", device, new CookieOptions { HttpOnly = true, Secure = c.Request.IsHttps, SameSite = SameSiteMode.Strict, MaxAge = TimeSpan.FromDays(180), IsEssential = true });
        }
        var deviceHash = Hash(device);
        var known = await db.Set<MemberSession>().AnyAsync(s => s.UserId == user && s.DeviceHash == deviceHash);
        // Password/external login may issue twice while additional claims are attached.
        if (c.HttpContext.Items["member.session"] is not MemberSession session)
        {
            if (Guid.TryParse(c.HttpContext.User.FindFirstValue("member_sid"), out var previous))
                await db.Set<MemberSession>().Where(s => s.Id == previous && s.UserId == user && s.RevokedAt == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, DateTimeOffset.UtcNow));
            session = new MemberSession { UserId = user, DeviceHash = deviceHash, Device = Short(c.Request.Headers.UserAgent, 300), Address = Short(c.HttpContext.Connection.RemoteIpAddress?.ToString(), 64), ExpiresAt = c.Properties.ExpiresUtc ?? DateTimeOffset.UtcNow.AddHours(8) };
            db.Add(session);
            c.HttpContext.Items["member.session"] = session;
            db.Audits.Add(new Audit { ActorId = user, Action = "auth.login.success", ResourceId = session.Id, Details = JsonSerializer.Serialize(new { session.Device, session.Address }) });
            if (!known)
            {
                var email = await db.Users.Where(u => u.Id == user).Select(u => u.Email).SingleAsync();
                var url = c.HttpContext.RequestServices.GetRequiredService<IConfiguration>()["PublicUrl"];
                db.Outbox.Add(new Outbox { Recipient = email!, Subject = "New device signed in", Body = $"A new device signed in at {session.CreatedAt:u}.\nDevice: {session.Device}\nAddress: {session.Address}\nIf this wasn't you, reset your password at {url}/forgot and review your sessions at {url}/app/account?view=security." });
            }
            await db.SaveChangesAsync();
        }
        foreach (var old in identity.FindAll("member_sid").ToArray()) identity.RemoveClaim(old);
        identity.AddClaim(new Claim("member_sid", session.Id.ToString()));
    }

    public static async Task Validate(CookieValidatePrincipalContext c)
    {
        var retained = c.Principal?.Claims.Where(x => x.Type is "member_sid" or "amr" or "auth_time" or "mfa_at").ToArray() ?? [];
        await SecurityStampValidator.ValidatePrincipalAsync(c);
        if (c.Principal == null) return;
        if (c.Principal.Identity is ClaimsIdentity renewed) foreach(var claim in retained) if(!renewed.HasClaim(x=>x.Type==claim.Type)) renewed.AddClaim(claim);
        var db = c.HttpContext.RequestServices.GetRequiredService<AppDb>();
        var stamp = c.Principal.FindFirstValue("AspNet.Identity.SecurityStamp");
        var userStamp = await db.Users.Where(u => u.Id == User(c.Principal)).Select(u => u.SecurityStamp).SingleOrDefaultAsync();
        if (stamp != userStamp || userStamp == null)
        {
            c.RejectPrincipal();
            await c.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return;
        }
        if (!Guid.TryParse(c.Principal.FindFirstValue("member_sid"), out var sid)) return; // Legacy cookies still honor global security-stamp revocation.
        var session = await db.Set<MemberSession>().SingleOrDefaultAsync(s => s.Id == sid && s.UserId == User(c.Principal));
        if (session == null || session.RevokedAt != null)
        {
            c.RejectPrincipal();
            await c.HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            return;
        }
        if (session.LastSeenAt < DateTimeOffset.UtcNow.AddMinutes(-2))
        {
            session.LastSeenAt = DateTimeOffset.UtcNow;
            session.ExpiresAt = c.Properties.ExpiresUtc ?? session.ExpiresAt;
            await db.SaveChangesAsync();
        }
    }

    public static async Task<IResult> Complete(Account user, bool remember, string method, HttpContext context, SignInManager<Account> signIn, AppDb db)
    {
        if (!await signIn.CanSignInAsync(user) || await signIn.UserManager.IsLockedOutAsync(user)) return Results.Problem(statusCode: 401, title: "Sign in failed. Check your account or try again later.");
        if (user.TwoFactorEnabled)
        {
            await context.SignInAsync(IdentityConstants.TwoFactorUserIdScheme, new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, user.Id.ToString()), new Claim(ClaimTypes.AuthenticationMethod, method)], IdentityConstants.TwoFactorUserIdScheme)));
            return Results.Ok(new { requiresTwoFactor = true });
        }
        await signIn.SignInWithClaimsAsync(user, remember, FirebaseAuthEndpoints.RecentClaims(method));
        await ImportedMemberships.LinkAccount(db, user.Id);
        return Results.Ok(new { message = "Signed in" });
    }

    static async Task<Account> Recent(Access a, HttpContext c, UserManager<Account> users)
    {
        var user = await users.FindByIdAsync(a.UserId.ToString()) ?? throw new AccessDeniedException();
        if (!FirebaseAuthEndpoints.Recent(c.User, user.TwoFactorEnabled)) throw new DomainException("Sign in again before changing your sign-in methods.");
        return user;
    }
    static void Check(IdentityResult result) { if (!result.Succeeded) throw new DomainException(string.Join(" ", result.Errors.Select(e => e.Description))); }

    public static void Map(RouteGroupBuilder api)
    {
        var auth = api.MapGroup("/auth").RequireRateLimiting("auth");
        auth.MapPost("/restart", async (HttpContext c) => { await c.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme); await c.SignOutAsync(IdentityConstants.ExternalScheme); return Results.NoContent(); });
        auth.MapPost("/magic/request", async (MagicRequest r, AppDb db, UserManager<Account> users, IConfiguration config) =>
        {
            var user = await users.FindByEmailAsync(r.Email.Trim());
            if (user?.EmailConfirmed == true && !await users.IsLockedOutAsync(user))
            {
                await using var tx = await db.Database.BeginTransactionAsync();
                await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({"magic:" + user.Id}))");
                if (!await db.Set<MemberLoginLink>().AnyAsync(l => l.UserId == user.Id && l.CreatedAt > DateTimeOffset.UtcNow.AddMinutes(-1)))
                {
                    var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
                    db.Add(new MemberLoginLink { UserId = user.Id, TokenHash = Hash(token), SecurityStamp = user.SecurityStamp!, ReturnTo = SafeReturn(r.ReturnTo), ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10) });
                    db.Outbox.Add(new Outbox { Recipient = user.Email!, Subject = "Your sign-in link", Body = $"Use this single-use link within 10 minutes. Open it, then confirm sign-in.\n{config["PublicUrl"]}/magic#token={token}\nIgnore this email if you did not request it." });
                    await db.SaveChangesAsync();
                }
                await tx.CommitAsync();
            }
            return Results.Ok(new { message = "If your account is eligible, a sign-in link has been requested. Check your inbox and spam folder. The link expires in 10 minutes." });
        });
        auth.MapPost("/magic/consume", async (MagicConsume r, AppDb db, UserManager<Account> users, SignInManager<Account> signIn, HttpContext c) =>
        {
            var hash = Hash(r.Token);
            var link = await db.Set<MemberLoginLink>().SingleOrDefaultAsync(l => l.TokenHash == hash);
            var user = link == null ? null : await users.FindByIdAsync(link.UserId.ToString());
            if (link == null || user == null || link.SecurityStamp != user.SecurityStamp || link.ExpiresAt <= DateTimeOffset.UtcNow || link.UsedAt != null) return Results.Problem(statusCode: 401, title: "This sign-in link has expired or was already used. Request a fresh link.");
            var used = await db.Set<MemberLoginLink>().Where(l => l.Id == link.Id && l.UsedAt == null && l.ExpiresAt > DateTimeOffset.UtcNow).ExecuteUpdateAsync(s => s.SetProperty(l => l.UsedAt, DateTimeOffset.UtcNow));
            if (used != 1) return Results.Problem(statusCode: 401, title: "This sign-in link was already used.");
            c.Response.Headers["X-Member-Return"] = SafeReturn(link.ReturnTo);
            return await Complete(user, false, "magic", c, signIn, db);
        });
        auth.MapPost("/passkeys/options", async (SignInManager<Account> signIn) => Results.Content(await signIn.MakePasskeyRequestOptionsAsync(null), "application/json"));
        auth.MapPost("/passkeys/login", async (PasskeyCredential r, SignInManager<Account> signIn, AppDb db, HttpContext c) =>
        {
            var result = await signIn.PerformPasskeyAssertionAsync(r.CredentialJson);
            if (!result.Succeeded) return Results.Problem(statusCode: 401, title: "Passkey verification failed. Try again or use another sign-in method.");
            Check(await signIn.UserManager.AddOrUpdatePasskeyAsync(result.User, result.Passkey));
            return await Complete(result.User, r.RememberMe, "passkey", c, signIn, db);
        });
        var security = api.MapGroup("/security").RequireAuthorization().RequireRateLimiting("verify");
        security.MapGet("/sessions", async (Access a, AppDb db, HttpContext c) => { var sid = c.User.FindFirstValue("member_sid"); return await db.Set<MemberSession>().Where(s => s.UserId == a.UserId && s.RevokedAt == null && s.ExpiresAt > DateTimeOffset.UtcNow).OrderByDescending(s => s.LastSeenAt).Select(s => new { s.Id, s.Device, s.Address, s.CreatedAt, s.LastSeenAt, s.ExpiresAt, current = s.Id.ToString() == sid }).Take(50).ToListAsync(); });
        security.MapDelete("/sessions/{id:guid}", async (Guid id, Access a, AppDb db, HttpContext c) => { var count = await db.Set<MemberSession>().Where(s => s.Id == id && s.UserId == a.UserId).ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, DateTimeOffset.UtcNow)); if (count == 0) return Results.NotFound(); if (c.User.FindFirstValue("member_sid") == id.ToString()) await c.SignOutAsync(IdentityConstants.ApplicationScheme); return Results.NoContent(); });
        security.MapPost("/sessions/revoke-all", async (Access a, AppDb db, UserManager<Account> users, SignInManager<Account> signIn) => { await db.Set<MemberSession>().Where(s => s.UserId == a.UserId && s.RevokedAt == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, DateTimeOffset.UtcNow)); Check(await users.UpdateSecurityStampAsync((await users.FindByIdAsync(a.UserId.ToString()))!)); await signIn.SignOutAsync(); return Results.NoContent(); });
        security.MapGet("/activity", async (Access a, AppDb db) => await db.Audits.Where(x => x.ActorId == a.UserId && (x.Action == "auth.login.success" || x.Action == "auth.login.failed")).OrderByDescending(x => x.At).Select(x => new { x.Id, x.At, x.Action, x.Details }).Take(50).ToListAsync());
        security.MapGet("/session", async (HttpContext c) => { var ticket = await c.AuthenticateAsync(IdentityConstants.ApplicationScheme); return Results.Ok(new { expiresAt = ticket.Properties?.ExpiresUtc, persistent = ticket.Properties?.IsPersistent ?? false }); });
        security.MapPost("/session/extend", async (Access a, AppDb db, HttpContext c, UserManager<Account> users, SignInManager<Account> signIn) => {
            var ticket = await c.AuthenticateAsync(IdentityConstants.ApplicationScheme);
            var expires = DateTimeOffset.UtcNow.AddHours(8);
            if (Guid.TryParse(c.User.FindFirstValue("member_sid"), out var sid)) {
                var session = await db.Set<MemberSession>().SingleOrDefaultAsync(s => s.Id == sid && s.UserId == a.UserId && s.RevokedAt == null);
                if (session == null) throw new AccessDeniedException();
                session.ExpiresAt = expires; session.LastSeenAt = DateTimeOffset.UtcNow; c.Items["member.session"] = session; await db.SaveChangesAsync();
            }
            var user = (await users.FindByIdAsync(a.UserId.ToString()))!;
            var claims = c.User.Claims.Where(x => x.Type is "amr" or "auth_time" or "mfa_at").ToArray();
            await signIn.SignInWithClaimsAsync(user, new AuthenticationProperties { IsPersistent = ticket.Properties?.IsPersistent ?? false, ExpiresUtc = expires }, claims);
            c.Response.Headers["X-Member-Session-Expires"] = expires.ToString("O");
            return Results.Ok(new { expiresAt = expires });
        });
        security.MapGet("/methods", async (Access a, UserManager<Account> users) => { var u = (await users.FindByIdAsync(a.UserId.ToString()))!; return Results.Ok(new { hasPassword = await users.HasPasswordAsync(u), recoveryCodes = await users.CountRecoveryCodesAsync(u), mfaEnabled = u.TwoFactorEnabled, providers = (await users.GetLoginsAsync(u)).Select(l => new { l.LoginProvider, l.ProviderKey, name = l.ProviderDisplayName }), passkeys = (await users.GetPasskeysAsync(u)).Select(p => new { id = WebEncoders.Base64UrlEncode(p.CredentialId), p.Name, p.CreatedAt }) }); });
        security.MapPost("/providers/remove", async (ProviderRemoval r, Access a, HttpContext c, UserManager<Account> users, SignInManager<Account> signIn) => {
            var u = await Recent(a, c, users);
            var logins = await users.GetLoginsAsync(u);
            if (!logins.Any(l => l.LoginProvider == r.LoginProvider && l.ProviderKey == r.ProviderKey)) throw new DomainException("That provider is not connected to your account.");
            if (!await users.HasPasswordAsync(u) && logins.Count <= 1 && (await users.GetPasskeysAsync(u)).Count == 0) throw new DomainException("Add another sign-in method before disconnecting your last provider.");
            Check(await users.RemoveLoginAsync(u, r.LoginProvider, r.ProviderKey));
            var ticket = await c.AuthenticateAsync(IdentityConstants.ApplicationScheme);
            await signIn.SignInWithClaimsAsync(u, ticket.Properties?.IsPersistent ?? false, c.User.Claims.Where(x => x.Type is "amr" or "auth_time" or "mfa_at"));
            return Results.NoContent();
        });
        security.MapPost("/mfa/recovery-codes", async (Access a, HttpContext c, UserManager<Account> users, MfaCode r) => { var u = await Recent(a, c, users); if (!u.TwoFactorEnabled || !await users.VerifyTwoFactorTokenAsync(u, users.Options.Tokens.AuthenticatorTokenProvider, r.Code.Replace(" ", "").Replace("-", ""))) throw new DomainException("Enter a valid authenticator code."); return Results.Ok(new { recoveryCodes = await users.GenerateNewTwoFactorRecoveryCodesAsync(u, 10) }); });
        security.MapPost("/passkeys/options", async (Access a, HttpContext c, UserManager<Account> users, SignInManager<Account> signIn) => { var u = await Recent(a, c, users); if ((await users.GetPasskeysAsync(u)).Count >= 10) throw new DomainException("You can register up to 10 passkeys."); return Results.Content(await signIn.MakePasskeyCreationOptionsAsync(new PasskeyUserEntity { Id = u.Id.ToString(), Name = u.Email!, DisplayName = u.FullName }), "application/json"); });
        security.MapPost("/passkeys", async (PasskeyCredential r, Access a, HttpContext c, UserManager<Account> users, SignInManager<Account> signIn) => { var u = await Recent(a, c, users); if ((await users.GetPasskeysAsync(u)).Count >= 10) throw new DomainException("You can register up to 10 passkeys."); var result = await signIn.PerformPasskeyAttestationAsync(r.CredentialJson); if (!result.Succeeded || result.UserEntity.Id != u.Id.ToString()) throw new DomainException("Passkey registration failed. Restart registration."); result.Passkey.Name = string.IsNullOrWhiteSpace(r.Name) ? "My passkey" : r.Name.Trim(); Check(await users.AddOrUpdatePasskeyAsync(u, result.Passkey)); return Results.NoContent(); });
        security.MapDelete("/passkeys/{id}", async (string id, Access a, HttpContext c, UserManager<Account> users) => { var u = await Recent(a, c, users); if (!await users.HasPasswordAsync(u) && (await users.GetLoginsAsync(u)).Count == 0 && (await users.GetPasskeysAsync(u)).Count <= 1) throw new DomainException("Keep at least one sign-in method."); byte[] bytes; try { bytes = WebEncoders.Base64UrlDecode(id); } catch (FormatException) { throw new DomainException("Invalid passkey."); } Check(await users.RemovePasskeyAsync(u, bytes)); return Results.NoContent(); });

        api.MapGet("/membership/link-requests", async (Access a, AppDb db) => await db.Set<MembershipLinkRequest>().Where(r => r.UserId == a.UserId).OrderByDescending(r => r.CreatedAt).Select(r => new { r.Id, r.TenantId, r.Number, r.Status, r.CreatedAt, r.ReviewNote }).Take(50).ToListAsync()).RequireAuthorization();
        api.MapPost("/membership/link-requests", async (LinkMembershipRequest r, Access a, AppDb db) => { if (!await db.Users.AnyAsync(u => u.Id == a.UserId && u.EmailConfirmed) || !await db.Tenants.AnyAsync(t => t.Id == r.TenantId && t.Status == "Active")) throw new DomainException("Choose an active society and verify your email first."); await using var tx = await db.Database.BeginTransactionAsync(); await MembershipService.Lock(db, r.TenantId); if (await db.Set<MembershipLinkRequest>().AnyAsync(x => x.UserId == a.UserId && x.TenantId == r.TenantId && x.Status == "Pending")) throw new DomainException("Your existing request is awaiting review."); var request = new MembershipLinkRequest { UserId = a.UserId, TenantId = r.TenantId, Number = r.Number.Trim(), Note = r.Note.Trim() }; db.Add(request); await db.SaveChangesAsync(); await tx.CommitAsync(); return Results.Ok(new { request.Id, request.Status }); }).RequireAuthorization().RequireRateLimiting("auth");
        api.MapGet("/societies/{tenant:guid}/membership/link-requests", async (Guid tenant, Access a, AppDb db) => { await a.Require(tenant, "members.approve"); return Results.Ok(await (from r in db.Set<MembershipLinkRequest>() join u in db.Users on r.UserId equals u.Id where r.TenantId == tenant orderby r.CreatedAt descending select new { r.Id, r.Number, r.Note, r.Status, r.CreatedAt, u.FullName, u.Email }).Take(100).ToListAsync()); }).RequireAuthorization();
        api.MapPost("/societies/{tenant:guid}/membership/link-requests/{id:guid}/review", async (Guid tenant, Guid id, LinkMembershipReview r, Access a, AppDb db, MembershipService s) => {
            await a.Require(tenant, "members.approve"); await using var tx = await db.Database.BeginTransactionAsync(); await MembershipService.Lock(db, tenant);
            var request = await db.Set<MembershipLinkRequest>().SingleOrDefaultAsync(x => x.TenantId == tenant && x.Id == id) ?? throw new DomainException("Request not found.");
            if (request.Status != "Pending") throw new DomainException("This request has already been reviewed.");
            if (request.UserId == a.UserId) throw new DomainException("Another administrator must review your request.");
            if (r.Approve)
            {
                var m = await db.Memberships.SingleOrDefaultAsync(m => m.TenantId == tenant && m.Number == request.Number) ?? throw new DomainException("Check the membership number before approving.");
                await MembershipIdentity.RequireIndividual(db, tenant, m.Id);
                var person = await db.Persons.SingleAsync(p => p.Id == m.PersonId);
                if (person.UserId != null && person.UserId != request.UserId) throw new DomainException("This membership is already linked to another person.");
                var claimed = await db.Database.SqlQuery<bool>($"""
                    SELECT EXISTS(SELECT 1 FROM "MembershipHistories" h JOIN "AspNetUsers" u
                    ON u."EmailConfirmed" AND u."Id"<>{request.UserId}
                    WHERE h."TenantId"={tenant} AND h."MembershipId"={m.Id}
                    AND h."Action" IN ('membership.import.approved','membership.manual.claim.approved')
                    AND (h."Details"->'claimEmails') ? u."NormalizedEmail") AS "Value"
                    """).SingleAsync();
                if (claimed) throw new DomainException("This membership is already claimed by another verified account. Resolve that ownership with the membership team first.");
                var u = await db.Users.SingleAsync(u => u.Id == request.UserId && u.EmailConfirmed);
                // Scope the grant to this membership; a shared Person may span other societies.
                s.History(m, a.UserId, "membership.manual.claim.approved", new { claimEmails = new[] { u.NormalizedEmail! }, reason = r.Note, requestId = id });
            }
            request.Status = r.Approve ? "Approved" : "Rejected"; request.ReviewNote = r.Note; request.ReviewedAt = DateTimeOffset.UtcNow; request.ReviewerId = a.UserId; await db.SaveChangesAsync(); await tx.CommitAsync(); return Results.NoContent();
        }).RequireAuthorization();
        api.MapGet("/users/me/login-checklist", async (Access a, AppDb db, UserManager<Account> users) => { var u = (await users.FindByIdAsync(a.UserId.ToString()))!; return Results.Ok(new { dismissed = u.LoginChecklistDismissed, profile = u.LoginProfileConfirmed, membership = await MembershipExperience.Entitled(db, a.UserId).AnyAsync(), secured = u.TwoFactorEnabled || (await users.GetPasskeysAsync(u)).Count > 0, idViewed = u.LoginIdViewed }); }).RequireAuthorization();
        api.MapPut("/users/me/login-checklist", async (ChecklistUpdate r, Access a, UserManager<Account> users) => { var u = (await users.FindByIdAsync(a.UserId.ToString()))!; u.LoginChecklistDismissed = r.Dismissed; Check(await users.UpdateAsync(u)); return Results.NoContent(); }).RequireAuthorization();
        api.MapPost("/users/me/login-checklist/{step}", async (string step, Access a, UserManager<Account> users) => { var u = (await users.FindByIdAsync(a.UserId.ToString()))!; if (step == "profile") u.LoginProfileConfirmed = true; else if (step == "id") u.LoginIdViewed = true; else throw new DomainException("Unknown checklist step."); Check(await users.UpdateAsync(u)); return Results.NoContent(); }).RequireAuthorization();
    }
}
