using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Net.Mail;
namespace Society.Api;

public record InviteRequest(string Email);
public record AcceptRequest(string Token);
public static class InvitationEndpoints
{
    public static void Map(RouteGroupBuilder api)
    {
        api.MapPost("/societies/{tenant:guid}/invitations", async (Guid tenant, InviteRequest r, AppDb db, Access a, IConfiguration c) => { await a.Require(tenant, "roles.manage"); if (!MailAddress.TryCreate(r.Email, out _)) throw new DomainException("Enter a valid email"); var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)); var invite = new Invitation { TenantId = tenant, Email = r.Email.Trim().ToUpperInvariant(), TokenHash = Hash(token) }; db.Invitations.Add(invite); db.Outbox.Add(new Outbox { Recipient = r.Email, Subject = "Society workspace invitation", Body = $"Sign in with your verified email, then open {c["PublicUrl"]}/app/accept-invitation?token={token}" }); a.Audit(tenant, "invitation.sent", invite.Id); await db.SaveChangesAsync(); return Results.Ok(new { invite.Id }); }).RequireAuthorization();
        api.MapPost("/invitations/accept", async (AcceptRequest r, AppDb db, Access a) => { await using var tx = await db.Database.BeginTransactionAsync(); var hash = Hash(r.Token); var invite = await db.Invitations.FromSqlInterpolated($"SELECT * FROM \"Invitations\" WHERE \"TokenHash\"={hash} FOR UPDATE").SingleOrDefaultAsync(); var user = await db.Users.SingleAsync(x => x.Id == a.UserId); if (invite == null || invite.AcceptedAt != null || invite.ExpiresAt <= DateTimeOffset.UtcNow || invite.Email != user.NormalizedEmail || !user.EmailConfirmed) throw new DomainException("Invitation is invalid, expired, or addressed to another account"); if (!await db.Links.AnyAsync(x => x.TenantId == invite.TenantId && x.UserId == a.UserId)) db.Links.Add(new Link { TenantId = invite.TenantId, UserId = a.UserId }); invite.AcceptedAt = DateTimeOffset.UtcNow; a.Audit(invite.TenantId, "invitation.accepted", invite.Id); await db.SaveChangesAsync(); await tx.CommitAsync(); return Results.Ok(new { invite.TenantId, user.Id }); }).RequireAuthorization();
    }
    static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
