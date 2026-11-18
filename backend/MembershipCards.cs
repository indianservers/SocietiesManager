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
            var platform=await a.Platform();if(platform)await PlatformMemberAdmin.Require(a);else {await a.Member(tenant);if(!await studio.Own(tenant,id))throw new AccessDeniedException();if(!await db.MembershipCategories.AnyAsync(c=>db.Memberships.Any(m=>m.TenantId==tenant&&m.Id==id&&m.CategoryId==c.Id)&&(c.Kind=="Individual"||c.Kind=="Institutional"))&&!(await studio.Policy(tenant)).MemberDownloads)throw new AccessDeniedException();}
            var m = await db.Memberships.SingleOrDefaultAsync(m=>m.TenantId==tenant && m.Id==id);if(m==null)return Results.NotFound();
            var pdf=await Build(m,db,renderer,protection,config);
            a.Audit(tenant,"membership.card.downloaded",id); await db.SaveChangesAsync();
            return Results.File(pdf,"application/pdf",(await db.MembershipCategories.AnyAsync(c=>c.Id==m.CategoryId&&c.Kind=="Individual")?"Membership-ID-":"Membership-Certificate-")+m.Number+".pdf");
        }).RequireAuthorization().RequireRateLimiting("member-card");
        api.MapGet("/societies/{tenant:guid}/membership/members/{id:guid}/membership-certificate", async
            (Guid tenant, Guid id, AppDb db, Access a, StudioService studio, StudioRenderer renderer, IDataProtectionProvider protection, IConfiguration config) =>
        {
            var platform=await a.Platform();if(platform)await PlatformMemberAdmin.Require(a);else {await a.Member(tenant);if(!await studio.Own(tenant,id))throw new AccessDeniedException();if(!await db.MembershipCategories.AnyAsync(c=>db.Memberships.Any(m=>m.TenantId==tenant&&m.Id==id&&m.CategoryId==c.Id)&&(c.Kind=="Individual"||c.Kind=="Institutional"))&&!(await studio.Policy(tenant)).MemberDownloads)throw new AccessDeniedException();}
            var m = await db.Memberships.SingleOrDefaultAsync(m=>m.TenantId==tenant && m.Id==id);if(m==null)return Results.NotFound();
            if(!await db.MembershipCategories.AnyAsync(c=>c.Id==m.CategoryId&&c.Kind=="Institutional"))throw new DomainException("Membership certificates are only for Institutional Members");
            var pdf=await Build(m,db,renderer,protection,config);
            a.Audit(tenant,"membership.certificate.downloaded",id); await db.SaveChangesAsync();
            return Results.File(pdf,"application/pdf","Membership-Certificate-"+m.Number+".pdf");
        }).RequireAuthorization().RequireRateLimiting("member-card");
        api.MapGet("/societies/{tenant:guid}/membership/members/{id:guid}/membership-certificate/preview.png", async
            (Guid tenant, Guid id, AppDb db, Access a, StudioService studio, StudioRenderer renderer, IDataProtectionProvider protection, IConfiguration config) =>
        {
            var platform=await a.Platform();if(platform)await PlatformMemberAdmin.Require(a);else {await a.Member(tenant);if(!await studio.Own(tenant,id))throw new AccessDeniedException();if(!await db.MembershipCategories.AnyAsync(c=>db.Memberships.Any(m=>m.TenantId==tenant&&m.Id==id&&m.CategoryId==c.Id)&&(c.Kind=="Individual"||c.Kind=="Institutional"))&&!(await studio.Policy(tenant)).MemberDownloads)throw new AccessDeniedException();}
            var m = await db.Memberships.SingleOrDefaultAsync(m=>m.TenantId==tenant && m.Id==id);if(m==null)return Results.NotFound();
            if(!await db.MembershipCategories.AnyAsync(c=>c.Id==m.CategoryId&&c.Kind=="Institutional"))throw new DomainException("Membership certificates are only for Institutional Members");
            var pdf=await Build(m,db,renderer,protection,config,certificatePreview:true);
            a.Audit(tenant,"membership.certificate.previewed",id); await db.SaveChangesAsync();
            return Results.File(pdf,"image/png");
        }).RequireAuthorization().RequireRateLimiting("member-card");
        api.MapGet("/societies/{tenant:guid}/membership/members/{id:guid}/id-card/front.png", async
            (Guid tenant, Guid id, AppDb db, Access a, StudioService studio, StudioRenderer renderer, IDataProtectionProvider protection, IConfiguration config) =>
        {
            var platform=await a.Platform();if(platform)await PlatformMemberAdmin.Require(a);else {await a.Member(tenant);if(!await studio.Own(tenant,id))throw new AccessDeniedException();if(!await db.MembershipCategories.AnyAsync(c=>db.Memberships.Any(m=>m.TenantId==tenant&&m.Id==id&&m.CategoryId==c.Id)&&(c.Kind=="Individual"||c.Kind=="Institutional"))&&!(await studio.Policy(tenant)).MemberDownloads)throw new AccessDeniedException();}
            var m = await db.Memberships.SingleOrDefaultAsync(m=>m.TenantId==tenant && m.Id==id);if(m==null)return Results.NotFound();
            var pdf=await Build(m,db,renderer,protection,config,frontOnly:true);
            a.Audit(tenant,"membership.card.previewed",id); await db.SaveChangesAsync();
            return Results.File(pdf,"image/png");
        }).RequireAuthorization().RequireRateLimiting("member-card");
        api.MapGet("/societies/{tenant:guid}/membership/members/{id:guid}/id-card/back.png", async
            (Guid tenant, Guid id, AppDb db, Access a, StudioService studio, StudioRenderer renderer, IDataProtectionProvider protection, IConfiguration config) =>
        {
            var platform=await a.Platform();if(platform)await PlatformMemberAdmin.Require(a);else {await a.Member(tenant);if(!await studio.Own(tenant,id))throw new AccessDeniedException();if(!await db.MembershipCategories.AnyAsync(c=>db.Memberships.Any(m=>m.TenantId==tenant&&m.Id==id&&m.CategoryId==c.Id)&&(c.Kind=="Individual"||c.Kind=="Institutional"))&&!(await studio.Policy(tenant)).MemberDownloads)throw new AccessDeniedException();}
            var m = await db.Memberships.SingleOrDefaultAsync(m=>m.TenantId==tenant && m.Id==id);if(m==null)return Results.NotFound();
            var pdf=await Build(m,db,renderer,protection,config,frontOnly:true,side:"Back");
            a.Audit(tenant,"membership.card.previewed",id); await db.SaveChangesAsync();
            return Results.File(pdf,"image/png");
        }).RequireAuthorization().RequireRateLimiting("member-card");
        api.MapGet("/platform/membership/{tenant:guid}/id-cards.zip",async(Guid tenant,string? ids,Guid? chapter,AppDb db,Access a,StudioRenderer renderer,IDataProtectionProvider protection,IConfiguration config)=>{
            await PlatformMemberAdmin.Require(a);var q=MembershipIdentity.Filter(db,db.Memberships.Where(m=>m.TenantId==tenant&&!EF.Functions.JsonContains(m.Profile,"{\"adminArchived\":true}")),"Individual");if(chapter!=null)q=q.Where(m=>m.ChapterId==chapter);Guid[]? selected=null;if(!string.IsNullOrWhiteSpace(ids)){var values=ids.Split(',');if(values.Length>2000||values.Any(v=>!Guid.TryParse(v,out _)))throw new DomainException("Choose up to 2,000 individual memberships per download");selected=values.Select(Guid.Parse).Distinct().ToArray();q=q.Where(m=>selected.Contains(m.Id));}var rows=await q.OrderBy(m=>m.Number).Take(2001).ToListAsync();if(rows.Count==0)throw new DomainException("No individual memberships match this download");if(rows.Count>2000)throw new DomainException("Download a chapter or select up to 2,000 individuals per ZIP");if(selected!=null&&selected.Length!=rows.Count)throw new DomainException("Every selected record must be an individual membership in this society");using var buffer=new MemoryStream();using(var zip=new ZipArchive(buffer,ZipArchiveMode.Create,true)){foreach(var m in rows){var pdf=await Build(m,db,renderer,protection,config);var entry=zip.CreateEntry("Membership-ID-"+System.Text.RegularExpressions.Regex.Replace(m.Number,"[^A-Za-z0-9_-]","_")+".pdf");await using var stream=entry.Open();await stream.WriteAsync(pdf);}}a.Audit(tenant,"platform.membership.cards.downloaded",tenant,new{count=rows.Count,chapter});await db.SaveChangesAsync();return Results.File(buffer.ToArray(),"application/zip","Individual-Membership-ID-Cards.zip");
        }).RequireAuthorization();

    }
    public static async Task<byte[]> Build(Membership m,AppDb db,StudioRenderer renderer,IDataProtectionProvider protection,IConfiguration config,bool frontOnly=false,string side="Front",bool certificatePreview=false){
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
        var professional=category.Kind=="Individual"?await db.Set<DesignTemplate>().SingleOrDefaultAsync(t=>t.TenantId==m.TenantId&&t.Id==AimerCyanCard.TemplateId&&t.Status=="Published"):null;
        if(category.Kind=="Institutional")professional=await db.Set<DesignTemplate>().SingleOrDefaultAsync(t=>t.TenantId==m.TenantId&&t.Id==AimerInstitutionalCertificate.TemplateId&&t.Status=="Published");
        var published=professional==null?null:await db.Set<DesignVersion>().Where(v=>v.TenantId==m.TenantId&&v.TemplateId==professional.Id).OrderByDescending(v=>v.Version).Select(v=>v.Design).FirstOrDefaultAsync();
        var design=published==null?DesignSchema.Starter(category.Kind=="Individual"?"Card":"Certificate"):DesignSchema.Parse(published);
        if(published==null&&category.Kind=="Individual")design=design with {Back=design.Back.Concat(new[]{new DesignElement{Id="status",X=6,Y=published==null?14:48.5,Width=73,Height=published==null?5:2,Text="Status: "+MembershipExperience.State(m),FontSize=published==null?3:1.5,Fill=published==null?"#173e35":"#B8C5E1",Bold=true}}).ToArray()};
        if(published!=null&&m.EndsAt!=null)fields["card.expiry_date"]=TimeZoneInfo.ConvertTime(m.EndsAt.Value.AddTicks(-1),TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata")).ToString("dd MMM yyyy",System.Globalization.CultureInfo.InvariantCulture);
        if(category.Kind!="Individual"&&published==null)design=design with{Front=design.Front.Select(e=>e.Id=="title"?e with{Text="Institutional Membership Certificate"}:e.Id=="chapter"?e with{Text="Membership ID: {{member.member_id}} · Valid through {{card.expiry_date}}"}:e).ToArray()};
        fields["member.joining_date"]=TimeZoneInfo.ConvertTime(m.EffectiveFrom,TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata")).ToString("dd MMM yyyy",System.Globalization.CultureInfo.InvariantCulture);
        fields["member.place"]=MembershipService.Json(m.Profile)["city"]?.GetValue<string>()??"Not provided";
        fields["member.membership_status"]=MembershipExperience.State(m);
        var snapshot=new CredentialSnapshot(design,fields,new Dictionary<string,Guid>());
        if(certificatePreview){if(category.Kind!="Institutional")throw new DomainException("This certificate is only for Institutional Members");return renderer.Image(m.TenantId,snapshot,"Front",dpi:120);}
        if(frontOnly){if(category.Kind!="Individual")throw new DomainException("ID cards are only for Individual Members; institutions receive certificates");return renderer.Image(m.TenantId,snapshot,side);}
        return renderer.Pdf(snapshot,renderer.Image(m.TenantId,snapshot,"Front",jpeg:true),category.Kind=="Individual"&&design.Back.Length>0?renderer.Image(m.TenantId,snapshot,"Back",jpeg:true):null);
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
