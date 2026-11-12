using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
namespace Society.Api;

public record ForgotRequest([System.ComponentModel.DataAnnotations.Required, System.ComponentModel.DataAnnotations.EmailAddress] string Email);
public static class AuthEndpoints
{
    public static void Map(RouteGroupBuilder api)
    {
        var auth = api.MapGroup("/auth").RequireRateLimiting("auth");
        auth.MapPost("/signup", async (RegisterRequest r, UserManager<Account> users, AppDb db, IConfiguration config) => { var u = new Account { Id = Guid.NewGuid(), Email = r.Email, UserName = r.Email, FullName = r.FullName }; var result = await users.CreateAsync(u, r.Password); if (!result.Succeeded) return Results.ValidationProblem(new Dictionary<string, string[]> { { "account", result.Errors.Select(x => x.Description).ToArray() } }); var token = await users.GenerateEmailConfirmationTokenAsync(u); Queue(db, u.Email!, "Verify your email", $"{config["PublicUrl"]}/verify?userId={u.Id}&token={Uri.EscapeDataString(token)}"); await db.SaveChangesAsync(); return Results.Ok(new { message = "Check your email to verify your account." }); });
        auth.MapPost("/verify", async (Guid userId, string token, UserManager<Account> users) => { var u = await users.FindByIdAsync(userId.ToString()); if (u == null) return Results.BadRequest(); var r = await users.ConfirmEmailAsync(u, token); return r.Succeeded ? Results.Ok(new { message = "Email verified. You can sign in." }) : Results.BadRequest(new { title = "Invalid or expired verification link" }); });
        auth.MapPost("/login", async (LoginRequest r, SignInManager<Account> signIn) => { var result = await signIn.PasswordSignInAsync(r.Email, r.Password, false, true); return result.RequiresTwoFactor ? Results.Ok(new { requiresTwoFactor = true, message = "Enter your authenticator or recovery code" }) : result.Succeeded ? Results.Ok(new { message = "Signed in" }) : Results.Problem(statusCode: 401, title: "Sign in failed. Verify your email and credentials, or try again later."); });
        auth.MapPost("/logout", async (SignInManager<Account> signIn) => { await signIn.SignOutAsync(); return Results.NoContent(); }).RequireAuthorization();
        auth.MapPost("/forgot", async (ForgotRequest r, UserManager<Account> users, AppDb db, IConfiguration config) => { var u = await users.FindByEmailAsync(r.Email); if (u != null && u.EmailConfirmed) { var token = await users.GeneratePasswordResetTokenAsync(u); Queue(db, u.Email!, "Reset your password", $"{config["PublicUrl"]}/reset?email={Uri.EscapeDataString(u.Email!)}&token={Uri.EscapeDataString(token)}"); await db.SaveChangesAsync(); } return Results.Ok(new { message = "If the account is eligible, a recovery email is on its way." }); });
        auth.MapPost("/reset", async (ResetRequest r, UserManager<Account> users) => { var u = await users.FindByEmailAsync(r.Email); if (u == null) return Results.BadRequest(); var result = await users.ResetPasswordAsync(u, r.Token, r.Password); return result.Succeeded ? Results.Ok(new { message = "Password reset. Sign in with your new password." }) : Results.BadRequest(new { title = string.Join(" ", result.Errors.Select(x => x.Description)) }); });
        api.MapGet("/users/me", async (Access a, AppDb db) => await db.Users.Where(x => x.Id == a.UserId).Select(x => new { x.Id, x.FullName, x.Email, x.PlatformAdmin, x.EmailConfirmed }).SingleAsync()).RequireAuthorization();
        api.MapPut("/users/me", async (ProfileRequest r, Access a, UserManager<Account> users) => { if (string.IsNullOrWhiteSpace(r.FullName) || r.FullName.Length > 160) throw new DomainException("Enter a name of up to 160 characters"); var u = (await users.FindByIdAsync(a.UserId.ToString()))!; u.FullName = r.FullName; await users.UpdateAsync(u); return Results.NoContent(); }).RequireAuthorization();
        api.MapPost("/users/me/password", async (PasswordRequest r, Access a, UserManager<Account> users) => { var u = (await users.FindByIdAsync(a.UserId.ToString()))!; var result = await users.ChangePasswordAsync(u, r.CurrentPassword, r.NewPassword); if (!result.Succeeded) throw new DomainException(string.Join(" ", result.Errors.Select(x => x.Description))); return Results.NoContent(); }).RequireAuthorization();
    }
    static void Queue(AppDb db, string recipient, string subject, string body) => db.Outbox.Add(new Outbox { Recipient = recipient, Subject = subject, Body = body });
}
