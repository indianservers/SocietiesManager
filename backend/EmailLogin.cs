using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
namespace Society.Api;
public class EmailLoginChallenge {
 public Guid Id {get;set;}=Guid.NewGuid(); public Guid? UserId {get;set;} public string IdentifierHash {get;set;}=""; public string ProtectedCode {get;set;}=""; public string SecurityStamp {get;set;}=""; public DateTimeOffset CreatedAt {get;set;}=DateTimeOffset.UtcNow; public DateTimeOffset ExpiresAt {get;set;} public DateTimeOffset? UsedAt {get;set;} public int Attempts {get;set;}
}
public static class EmailLogin {
 public record Start([Required,EmailAddress,MaxLength(254)] string Email);
 public record Verify(Guid ChallengeId,[Required,RegularExpression("^[0-9]{6}$")] string Code,bool RememberMe=false);
 static string Hash(string v)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(v)));
 public static void Model(ModelBuilder b){b.Entity<EmailLoginChallenge>().HasIndex(c=>new{c.IdentifierHash,c.CreatedAt});b.Entity<EmailLoginChallenge>().HasIndex(c=>c.ExpiresAt);b.Entity<EmailLoginChallenge>().HasOne<Account>().WithMany().HasForeignKey(c=>c.UserId).OnDelete(DeleteBehavior.Cascade);}
 public static void Map(RouteGroupBuilder api){var auth=api.MapGroup("/auth/email-otp").RequireRateLimiting("auth");
 auth.MapPost("/request",async(Start r,AppDb db,UserManager<Account> users,IDataProtectionProvider protection)=>{
 var normalized=r.Email.Trim().ToUpperInvariant();var hash=Hash(normalized);var now=DateTimeOffset.UtcNow;await db.Set<EmailLoginChallenge>().Where(c=>c.ExpiresAt<now.AddDays(-1)).ExecuteDeleteAsync();await using var tx=await db.Database.BeginTransactionAsync();await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({"email-otp:"+hash}))");
 var recent=await db.Set<EmailLoginChallenge>().Where(c=>c.IdentifierHash==hash&&c.CreatedAt>now.AddHours(-1)).OrderByDescending(c=>c.CreatedAt).ToListAsync();
 var latest=recent.FirstOrDefault();var id=latest?.Id??Guid.NewGuid();
 if((latest==null||latest.CreatedAt<now.AddMinutes(-1))&&recent.Count<6){
 await db.Set<EmailLoginChallenge>().Where(c=>c.IdentifierHash==hash&&c.UsedAt==null).ExecuteUpdateAsync(s=>s.SetProperty(c=>c.UsedAt,now));
 await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({"auth-email:"+normalized}))");
 var user=await users.FindByEmailAsync(r.Email.Trim());var approved=await ImportedMemberships.ForLoginEmail(db,normalized).OrderBy(m=>m.Id).FirstOrDefaultAsync();
 if(user==null&&approved!=null){var person=await db.Persons.SingleAsync(p=>p.Id==approved.PersonId);var kind=await db.MembershipCategories.Where(c=>c.Id==approved.CategoryId).Select(c=>c.Kind).SingleAsync();user=new Account{Id=Guid.NewGuid(),Email=r.Email.Trim(),UserName=r.Email.Trim(),FullName=person.FullName,PreferredMembershipKind=kind,SignupTenantId=approved.TenantId};var created=await users.CreateAsync(user);if(!created.Succeeded)throw new DomainException("Could not prepare member sign-in. Please try again.");}
 var eligible=user!=null&&(user.EmailConfirmed||approved!=null)&&!await users.IsLockedOutAsync(user);var code=RandomNumberGenerator.GetInt32(1000000).ToString("D6");var challenge=new EmailLoginChallenge{UserId=eligible?user!.Id:null,IdentifierHash=hash,ProtectedCode=protection.CreateProtector("AIMER.EmailLogin.v1").Protect(code),SecurityStamp=eligible?user!.SecurityStamp??"":"",ExpiresAt=now.AddMinutes(5)};id=challenge.Id;db.Add(challenge);
 if(eligible)db.Outbox.Add(new Outbox{Recipient=user!.Email!,Subject="Your AIMER sign-in code",Body=$"Your one-time AIMER sign-in code is {code}.\nIt expires in 5 minutes. Never share this code.\nIf you did not request this sign-in, ignore this email."});
 await db.SaveChangesAsync();
 }
 await tx.CommitAsync();return Results.Ok(new{challengeId=id,message="If this email has an eligible account, a six-digit sign-in code has been sent. Check your inbox and spam folder. The code expires in 5 minutes."});
 });
 auth.MapPost("/verify",async(Verify r,AppDb db,UserManager<Account> users,SignInManager<Account> signIn,IDataProtectionProvider protection,HttpContext context)=>{
 var now=DateTimeOffset.UtcNow;await using var tx=await db.Database.BeginTransactionAsync();var identifier=await db.Set<EmailLoginChallenge>().Where(c=>c.Id==r.ChallengeId).Select(c=>c.IdentifierHash).SingleOrDefaultAsync()??r.ChallengeId.ToString();await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtext({"email-otp:"+identifier}))");var challenge=await db.Set<EmailLoginChallenge>().SingleOrDefaultAsync(c=>c.Id==r.ChallengeId);var user=challenge?.UserId is Guid uid?await users.FindByIdAsync(uid.ToString()):null;
 var valid=challenge!=null&&challenge.UsedAt==null&&challenge.ExpiresAt>now&&challenge.Attempts<5;
 if(valid){challenge!.Attempts++;string code;try{code=protection.CreateProtector("AIMER.EmailLogin.v1").Unprotect(challenge.ProtectedCode);}catch(CryptographicException){code="";}valid=user!=null&&challenge.SecurityStamp==user.SecurityStamp&&(user.EmailConfirmed||await ImportedMemberships.ForLoginEmail(db,user.NormalizedEmail!).AnyAsync())&&!await users.IsLockedOutAsync(user)&&CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(code),Encoding.UTF8.GetBytes(r.Code));if(valid&&!user!.EmailConfirmed){user.EmailConfirmed=true;var confirmed=await users.UpdateAsync(user);if(!confirmed.Succeeded)throw new DomainException("Could not verify member email. Please request a fresh code.");}if(valid||challenge.Attempts>=5)challenge.UsedAt=now;if(!valid&&user!=null)db.Audits.Add(new Audit{ActorId=user.Id,ResourceId=user.Id,Action="auth.login.failed",Details=System.Text.Json.JsonSerializer.Serialize(new{method="email-otp",reason="Invalid code",Address=context.Connection.RemoteIpAddress?.ToString()})});await db.SaveChangesAsync();}
 await tx.CommitAsync();if(!valid)return Results.Problem(statusCode:401,title:"Invalid or expired sign-in code. Request a fresh code and try again.");
 return await MemberLogin.Complete(user!,r.RememberMe,"email-otp",context,signIn,db);
 });
 }
}
