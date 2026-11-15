using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.IO.Compression;
namespace Society.Api;

public static class MembershipCards
{
    const string Purpose = "society.membership-card.v1";
    public static void Map(RouteGroupBuilder api)
    {
        api.MapGet("/societies/{tenant:guid}/membership/members/{id:guid}/id-card", async
            (Guid tenant, Guid id, AppDb db, Access a, StudioService studio, StudioRenderer renderer, IDataProtectionProvider protection, IConfiguration config) =>
        {
            var platform=await a.Platform();if(platform)await PlatformMemberAdmin.Require(a);else {await a.Member(tenant);if(!await studio.Own(tenant,id))throw new AccessDeniedException();if(!await db.MembershipCategories.AnyAsync(c=>db.Memberships.Any(m=>m.TenantId==tenant&&m.Id==id&&m.CategoryId==c.Id)&&c.Kind=="Individual")&&!(await studio.Policy(tenant)).MemberDownloads)throw new AccessDeniedException();}
            var m = await db.Memberships.SingleOrDefaultAsync(m=>m.TenantId==tenant && m.Id==id);if(m==null)return Results.NotFound();
            var pdf=await Build(m,db,renderer,protection,config);
            a.Audit(tenant,"membership.card.downloaded",id); await db.SaveChangesAsync();
            return Results.File(pdf,"application/pdf","Membership-ID-"+m.Number+".pdf");
        }).RequireAuthorization().RequireRateLimiting("member-card");
        api.MapGet("/platform/membership/{tenant:guid}/id-cards.zip",async(Guid tenant,string? ids,Guid? chapter,AppDb db,Access a,StudioRenderer renderer,IDataProtectionProvider protection,IConfiguration config)=>{
            await PlatformMemberAdmin.Require(a);var q=MembershipIdentity.Filter(db,db.Memberships.Where(m=>m.TenantId==tenant&&!EF.Functions.JsonContains(m.Profile,"{\"adminArchived\":true}")),"Individual");if(chapter!=null)q=q.Where(m=>m.ChapterId==chapter);Guid[]? selected=null;if(!string.IsNullOrWhiteSpace(ids)){var values=ids.Split(',');if(values.Length>2000||values.Any(v=>!Guid.TryParse(v,out _)))throw new DomainException("Choose up to 2,000 individual memberships per download");selected=values.Select(Guid.Parse).Distinct().ToArray();q=q.Where(m=>selected.Contains(m.Id));}var rows=await q.OrderBy(m=>m.Number).Take(2001).ToListAsync();if(rows.Count==0)throw new DomainException("No individual memberships match this download");if(rows.Count>2000)throw new DomainException("Download a chapter or select up to 2,000 individuals per ZIP");if(selected!=null&&selected.Length!=rows.Count)throw new DomainException("Every selected record must be an individual membership in this society");using var buffer=new MemoryStream();using(var zip=new ZipArchive(buffer,ZipArchiveMode.Create,true)){foreach(var m in rows){var pdf=await Build(m,db,renderer,protection,config);var entry=zip.CreateEntry("Membership-ID-"+System.Text.RegularExpressions.Regex.Replace(m.Number,"[^A-Za-z0-9_-]","_")+".pdf");await using var stream=entry.Open();await stream.WriteAsync(pdf);}}a.Audit(tenant,"platform.membership.cards.downloaded",tenant,new{count=rows.Count,chapter});await db.SaveChangesAsync();return Results.File(buffer.ToArray(),"application/zip","Individual-Membership-ID-Cards.zip");
        }).RequireAuthorization();

    }
    public static async Task<byte[]> Build(Membership m,AppDb db,StudioRenderer renderer,IDataProtectionProvider protection,IConfiguration config){
        var person = await db.Persons.SingleAsync(p=>p.Id==m.PersonId);
        var society = await db.Tenants.SingleAsync(t=>t.Id==m.TenantId);
        var chapter = await db.Units.SingleAsync(u=>u.TenantId==m.TenantId && u.Id==m.ChapterId);
        var category = await db.MembershipCategories.SingleAsync(c=>c.Id==m.CategoryId);
        var token = "m-"+protection.CreateProtector(Purpose).Protect(m.TenantId+":"+m.Id);
        var expiry = m.EndsAt == null ? "Not specified" : TimeZoneInfo.ConvertTime(m.EndsAt.Value.AddTicks(-1),TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata")).ToString("yyyy-MM-dd");
        var fields = new Dictionary<string,string> {
            ["member.phone"]=MembershipService.Json(m.Profile)["phone"]?.GetValue<string>()??"Not provided",["member.membership_category"]=category.Kind.ToUpperInvariant()+" MEMBER",["society.name"]=society.Name,["member.full_name"]=person.FullName,["member.member_id"]=m.Number,
            ["chapter.name"]=category.Kind+" Member",["card.serial_number"]=m.Number,
            ["card.expiry_date"]=expiry,["card.qr_code"]=(config["PublicUrl"]??"").TrimEnd('/')+"/verify/credential/"+token };
        var professional=category.Kind=="Individual"?await db.Set<DesignTemplate>().SingleOrDefaultAsync(t=>t.TenantId==m.TenantId&&t.Id==AimerProfessionalCard.TemplateId&&t.Status=="Published"):null;
        var published=professional==null?null:await db.Set<DesignVersion>().Where(v=>v.TenantId==m.TenantId&&v.TemplateId==professional.Id).OrderByDescending(v=>v.Version).Select(v=>v.Design).FirstOrDefaultAsync();
        var design=published==null?DesignSchema.Starter("Card"):DesignSchema.Parse(published);
        design=design with {Back=design.Back.Concat(new[]{new DesignElement{Id="status",X=6,Y=published==null?14:48.5,Width=73,Height=published==null?5:2,Text="Status: "+MembershipExperience.State(m),FontSize=published==null?3:1.5,Fill=published==null?"#173e35":"#B8C5E1",Bold=true}}).ToArray()};
        if(published!=null&&m.EndsAt!=null)fields["card.expiry_date"]=TimeZoneInfo.ConvertTime(m.EndsAt.Value.AddTicks(-1),TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata")).ToString("dd MMM yyyy",System.Globalization.CultureInfo.InvariantCulture);
        var snapshot=new CredentialSnapshot(design,fields,new Dictionary<string,Guid>());
        return renderer.Pdf(snapshot,renderer.Image(m.TenantId,snapshot,"Front",jpeg:true),renderer.Image(m.TenantId,snapshot,"Back",jpeg:true));
    }
    public static async Task<IResult> Verify(string token, AppDb db, IDataProtectionProvider protection)
    {
        string[] ids;
        try { ids=protection.CreateProtector(Purpose).Unprotect(token[2..]).Split(':'); }
        catch(CryptographicException) { return Results.NotFound(); }
        if(ids.Length!=2 || !Guid.TryParse(ids[0],out var tenant) || !Guid.TryParse(ids[1],out var id)) return Results.NotFound();
        var m=await db.Memberships.SingleOrDefaultAsync(m=>m.TenantId==tenant && m.Id==id);
        if(m==null)return Results.NotFound();
        var society=await db.Tenants.SingleAsync(t=>t.Id==tenant);
        var valid=society.Status=="Active" && MembershipExperience.Current(m);
        var expiry=m.EndsAt==null?null:TimeZoneInfo.ConvertTime(m.EndsAt.Value.AddTicks(-1),TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata")).ToString("yyyy-MM-dd");
        return Results.Ok(new {valid,status=valid?"Valid":MembershipExperience.State(m),society=society.Name,chapter="Membership register",category=await db.MembershipCategories.Where(c=>c.Id==m.CategoryId).Select(c=>c.Name).SingleAsync(),expiry});
    }
}
