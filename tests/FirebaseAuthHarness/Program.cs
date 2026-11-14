using Society.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Security.Claims;
using System.Net;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

if (args.Contains("--verifier-tests")) { await VerifierChecks.Run(); return; }
var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddInMemoryCollection(new Dictionary<string,string?> {
    ["Authentication:Firebase:Enabled"]="true",["Authentication:Firebase:ProjectId"]="test-project",["Authentication:Firebase:ApiKey"]="test-api-key",
    ["Authentication:Firebase:AuthDomain"]="test-project.firebaseapp.com",["Authentication:Firebase:AppId"]="test-app",
    ["Authentication:Firebase:Providers:0"]="google.com",["Authentication:Firebase:Providers:1"]="microsoft.com" });
builder.Services.AddDbContext<AppDb>(o=>o.UseNpgsql(builder.Configuration.GetConnectionString("Database")));
builder.Services.AddIdentity<Account,IdentityRole<Guid>>(o=>{o.Password.RequiredLength=12;o.User.RequireUniqueEmail=true;o.SignIn.RequireConfirmedEmail=true;o.Lockout.MaxFailedAccessAttempts=5;}).AddEntityFrameworkStores<AppDb>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(o=>{o.Cookie.Name="test.social.session";o.Cookie.HttpOnly=true;o.Cookie.SameSite=SameSiteMode.Strict;o.Events.OnRedirectToLogin=c=>{c.Response.StatusCode=401;return Task.CompletedTask;};});
builder.Services.AddAntiforgery(o=>o.HeaderName="X-CSRF-TOKEN");builder.Services.AddAuthorization();builder.Services.AddHttpContextAccessor();builder.Services.AddScoped<Access>();
builder.Services.AddSingleton<IFirebaseIdentityVerifier,FakeVerifier>();
builder.Services.AddRateLimiter(o=>{foreach(var name in new[]{"auth","verify"})o.AddFixedWindowLimiter(name,c=>{c.PermitLimit=10000;c.Window=TimeSpan.FromMinutes(1);});});
var app=builder.Build();app.UseAuthentication();
app.Use(async(context,next)=>{try{if(context.Request.Path.StartsWithSegments("/api")&&!HttpMethods.IsGet(context.Request.Method))await context.RequestServices.GetRequiredService<IAntiforgery>().ValidateRequestAsync(context);await next();}catch(AntiforgeryValidationException){context.Response.StatusCode=400;}catch(DomainException e){await Results.Problem(statusCode:400,title:e.Message).ExecuteAsync(context);}catch(AccessDeniedException){context.Response.StatusCode=403;}});
app.UseRateLimiter();app.UseAuthorization();var api=app.MapGroup("/api/v1");
api.AddEndpointFilter(async(ctx,next)=>{foreach(var arg in ctx.Arguments){if(arg==null||arg.GetType().Namespace!="Society.Api")continue;var errors=new List<ValidationResult>();if(!Validator.TryValidateObject(arg,new ValidationContext(arg),errors,true))return Results.ValidationProblem(errors.GroupBy(e=>e.MemberNames.FirstOrDefault()??"request").ToDictionary(g=>g.Key,g=>g.Select(e=>e.ErrorMessage!).ToArray()));}return await next(ctx);});
api.MapGet("/auth/csrf",(HttpContext c,IAntiforgery a)=>new{token=a.GetAndStoreTokens(c).RequestToken});AuthEndpoints.Map(api);SecurityCenter.Map(api);
api.MapGet("/branding/resolve",(IConfiguration c)=>new{tenantId=c["Test:Tenant"],displayName="AIMER Society",chapterTerm="Chapter",authLanding=new{communityName="AIMER",defaultDialCode="+91",loginHeading="Welcome to the AIMER community"}});
api.MapGet("/membership/catalog",()=>new{chapters=Array.Empty<object>(),categories=new[]{new{id="individual",kind="Individual",offer=new{price=500,currency="INR",termMonths=12}},new{id="institutional",kind="Institutional",offer=new{price=10000,currency="INR",termMonths=12}},new{id="corporate",kind="Corporate",offer=new{price=10000,currency="INR",termMonths=12}}}});
foreach(var path in new[]{"/workspaces","/membership/portfolio","/membership/affiliations"})api.MapGet(path,()=>Array.Empty<object>()).RequireAuthorization();
app.MapGet("/health/ready",()=>new{ready=true});app.UseDefaultFiles();app.UseStaticFiles();app.MapFallbackToFile("index.html");app.Run();

sealed class FakeVerifier:IFirebaseIdentityVerifier
{
    public Task<FirebaseIdentity> Verify(string token,FirebaseSettings settings,CancellationToken ct)
    {
        // Deliberately test-host-only. No test-token path exists in the app verifier.
        var value=JsonSerializer.Deserialize<TestIdentity>(token)??throw new DomainException("Missing test identity");
        if(value.Provider is not("google.com" or "microsoft.com"))throw new DomainException("Unsupported test provider");
        return Task.FromResult(new FirebaseIdentity(value.Uid,value.Email,value.Verified,"Synthetic Provider User",value.Provider,DateTimeOffset.UtcNow.ToUnixTimeSeconds()));
    }
    record TestIdentity(string Uid,string Email,bool Verified,string Provider);
}
static class VerifierChecks
{
    public static async Task Run()
    {
        var settings=new FirebaseSettings(true,"test-project","test-key","test-project.firebaseapp.com","test-app",["google.com","microsoft.com"]);
        var now=DateTimeOffset.UtcNow.ToUnixTimeSeconds();int passed=0;
        string Token(Dictionary<string,object?>? changes=null,string algorithm="RS256")
        {
            var claims=new Dictionary<string,object?>{{"sub","test-uid"},{"aud","test-project"},{"iss","https://securetoken.google.com/test-project"},{"exp",now+3600},{"iat",now},{"auth_time",now},{"firebase",new{sign_in_provider="google.com"}}};
            if(changes!=null)foreach(var change in changes)claims[change.Key]=change.Value;
            string Encode(object value)=>Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(value)).TrimEnd('=').Replace('+','-').Replace('/','_');
            return Encode(new{alg=algorithm,kid="synthetic"})+"."+Encode(claims)+".synthetic-signature";
        }
        async Task Check(string name,string token,bool valid,string uid="test-uid",bool disabled=false,long? since=null,bool reject=false)
        {
            var handler=new Lookup(uid,disabled,since,reject);var client=new HttpClient(handler);
            try{var result=await FirebaseTokenVerifier.Verify(token,settings,CancellationToken.None,client);if(!valid)throw new Exception("Accepted unsafe token: "+name);if(result.Email!="verified@example.test")throw new Exception("Wrong verified identity");}
            catch(DomainException){if(valid)throw;}
            passed++;Console.WriteLine("PASS VERIFY "+name);
        }
        await Check("valid token requires successful Google verification",Token(),true);
        await Check("wrong project is rejected",Token(new(){{"aud","other-project"}}),false);
        await Check("wrong issuer is rejected",Token(new(){{"iss","https://attacker.test"}}),false);
        await Check("unsigned token is rejected",Token(algorithm:"none"),false);
        await Check("expired token is rejected",Token(new(){{"exp",now-1}}),false);
        await Check("future issued token is rejected",Token(new(){{"iat",now+120}}),false);
        await Check("stale authentication requires another sign-in",Token(new(){{"auth_time",now-1200}}),false);
        await Check("unconfigured Firebase tenant cannot reuse a portal identity",Token(new(){{"firebase",new{sign_in_provider="google.com",tenant="other-tenant"}}}),false);
        await Check("anonymous and password provider tokens are rejected",Token(new(){{"firebase",new{sign_in_provider="password"}}}),false);
        await Check("Google rejection cannot create a portal session",Token(),false,reject:true);
        await Check("verified subject must match Google account lookup",Token(),false,uid:"different-uid");
        await Check("disabled Firebase account is rejected",Token(),false,disabled:true);
        await Check("revoked authentication is rejected",Token(),false,since:now+1);
        await Check("malformed tokens are rejected","not-a-token",false);
        if(FirebaseAuthEndpoints.Recent(new ClaimsPrincipal(),false))throw new Exception("Anonymous principal was considered recent");passed++;
        Console.WriteLine("VERIFY_TOTAL="+passed);
    }
    sealed class Lookup(string uid,bool disabled,long? since,bool reject):HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
        {
            if(request.RequestUri!.Host!="identitytoolkit.googleapis.com")throw new Exception("Untrusted upstream");
            return Task.FromResult(new HttpResponseMessage(reject?HttpStatusCode.BadRequest:HttpStatusCode.OK){Content=JsonContent.Create(new{users=new[]{new{localId=uid,email="verified@example.test",emailVerified=true,disabled,validSince=(since??0).ToString(),providerUserInfo=new[]{new{providerId="google.com"}}}}})});
        }
    }
}
