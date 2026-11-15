using Microsoft.EntityFrameworkCore;
namespace Society.Api;
public static class AimerInstitutionalCertificate {
 public static readonly Guid TemplateId=Guid.Parse("8feb3aba-cfb6-4d3e-9826-f265baf219bc");
 public static readonly Guid LetterheadId=Guid.Parse("cc2ce377-0a49-42cf-905f-c7fb1334825a");
 public static readonly Guid SignatureId=Guid.Parse("a9c26034-aa51-47c2-ad05-7c637996325b");
 static byte[] Resource(string name){using var input=typeof(AimerInstitutionalCertificate).Assembly.GetManifestResourceStream("Society.Api.Templates."+name)??throw new InvalidOperationException("Missing institutional certificate resource");using var buffer=new MemoryStream();input.CopyTo(buffer);return buffer.ToArray();}
 public static DocumentDesign Design()=>DesignSchema.Parse(System.Text.Encoding.UTF8.GetString(Resource("aimer-institution-certificate.json")));
 public static async Task Install(IServiceProvider services,Guid tenant){
  var db=services.GetRequiredService<AppDb>();var store=services.GetRequiredService<IObjectStore>();
  if(!await db.Tenants.AnyAsync(t=>t.Id==tenant&&t.Name.Contains("AIMER")))throw new DomainException("This template is only for AIMER Society");
  var existing=await db.Set<DesignTemplate>().SingleOrDefaultAsync(t=>t.Id==TemplateId&&t.TenantId==tenant);
  if(existing!=null){
   if(existing.PublishedVersion==2){Console.WriteLine("Compact institutional certificate already published.");return;}
   if(existing.PublishedVersion!=1)throw new DomainException("Review later certificate edits before publishing compact spacing");
   var publisher=await db.Users.Where(u=>u.PlatformAdmin).OrderBy(u=>u.Id).Select(u=>u.Id).FirstAsync();
   var revised=System.Text.Json.JsonSerializer.Serialize(Design(),DesignSchema.Json);await using var update=await db.Database.BeginTransactionAsync();
   existing.Design=revised;existing.PublishedVersion=2;
   db.Set<DesignVersion>().Add(new DesignVersion{TenantId=tenant,TemplateId=TemplateId,Version=2,Design=revised,PublisherId=publisher});
   db.Audits.Add(new Audit{TenantId=tenant,ActorId=publisher,ResourceId=TemplateId,Action="design.institutional-certificate.spacing",Details="{\"version\":2,\"change\":\"Reduced gaps between membership terms\"}"});
   await db.SaveChangesAsync();await update.CommitAsync();Console.WriteLine("Published compact institutional certificate version 2.");return;
  }
  var actor=await db.Users.Where(u=>u.PlatformAdmin).OrderBy(u=>u.Id).Select(u=>u.Id).FirstAsync();
  foreach(var asset in new[]{(LetterheadId,"aimer-institution-letterhead.png","Supplied institutional certificate letterhead"),(SignatureId,"aimer-president-reference-signature.png","President signature from supplied BVC certificate")}){
   var bytes=Resource(asset.Item2);await store.Put(tenant,asset.Item1,bytes);db.Set<StudioAsset>().Add(new StudioAsset{Id=asset.Item1,TenantId=tenant,Name=asset.Item3,Kind="Image",Length=bytes.Length,AuthorId=actor});
  }
  var design=System.Text.Json.JsonSerializer.Serialize(Design(),DesignSchema.Json);
  await using var tx=await db.Database.BeginTransactionAsync();
  db.Set<DesignTemplate>().Add(new DesignTemplate{Id=TemplateId,TenantId=tenant,Name="AIMER Institutional Certificate of Membership",Kind="Certificate",Category="Institutional Membership",Status="Published",Design=design,PublishedVersion=2,AuthorId=actor});
  db.Set<DesignVersion>().Add(new DesignVersion{TenantId=tenant,TemplateId=TemplateId,Version=2,Design=design,PublisherId=actor});
  db.Audits.Add(new Audit{TenantId=tenant,ActorId=actor,ResourceId=TemplateId,Action="design.institutional-certificate.installed",Details="{\"version\":1,\"kind\":\"Institutional\"}"});
  await db.SaveChangesAsync();await tx.CommitAsync();Console.WriteLine("Published Institutional-only letterhead membership certificate.");
 }
 public static void Preview(string folder){Directory.CreateDirectory(folder);var fields=DesignSchema.Fields.ToDictionary(k=>k,k=>"");fields["member.full_name"]="BVC Group of Institutions Amalapuram";fields["member.member_id"]="AIMERS-INS117";fields["member.joining_date"]="01 Jun 2024";fields["card.expiry_date"]="30 Jun 2025";fields["member.place"]="Amalapuram";fields["member.membership_status"]="Expired";var renderer=new StudioRenderer(new PreviewStore());var snapshot=new CredentialSnapshot(Design(),fields,new());File.WriteAllBytes(Path.Combine(folder,"institution-preview.png"),renderer.Image(Guid.Empty,snapshot,"Front"));fields["member.full_name"]="Kalasalingam Academy of Research and Education";File.WriteAllBytes(Path.Combine(folder,"institution-long-name.png"),renderer.Image(Guid.Empty,snapshot,"Front"));}
 sealed class PreviewStore:IObjectStore{public Stream Open(Guid tenant,Guid id)=>new MemoryStream(Resource(id==LetterheadId?"aimer-institution-letterhead.png":id==SignatureId?"aimer-president-reference-signature.png":throw new FileNotFoundException()));public Task Put(Guid tenant,Guid id,byte[] bytes)=>throw new NotSupportedException();public StorageUsage Usage(Guid tenant)=>new(0,0);}
}
