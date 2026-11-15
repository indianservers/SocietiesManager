using Microsoft.EntityFrameworkCore;
namespace Society.Api;

public static class AimerProfessionalCard {
 public static readonly Guid TemplateId=Guid.Parse("8685a442-ed2f-49c1-9d04-fcaa8472a837");
 public static readonly Guid LogoId=Guid.Parse("ba85e779-c614-4c41-b833-520b30eb14d1");
 public const string Name="AIMER • Professional Individual ID";
 static byte[] Resource(string name){using var stream=typeof(AimerProfessionalCard).Assembly.GetManifestResourceStream("Society.Api.Templates."+name)??throw new InvalidOperationException("Missing card resource");using var buffer=new MemoryStream();stream.CopyTo(buffer);return buffer.ToArray();}
 public static DocumentDesign Design()=>DesignSchema.Parse(System.Text.Encoding.UTF8.GetString(Resource("aimer-professional.json")));
 public static async Task Install(IServiceProvider services,Guid tenant){
  var db=services.GetRequiredService<AppDb>();var store=services.GetRequiredService<IObjectStore>();
  var society=await db.Tenants.SingleAsync(t=>t.Id==tenant);if(!society.Name.Contains("AIMER",StringComparison.OrdinalIgnoreCase))throw new DomainException("Install this society-specific template only for AIMER");
  var actor=await db.Users.Where(u=>u.PlatformAdmin).OrderBy(u=>u.Id).Select(u=>u.Id).FirstAsync();
  if(await db.Set<DesignTemplate>().AnyAsync(t=>t.Id==TemplateId)){Console.WriteLine("Professional template already installed; existing edits preserved.");return;}
  var design=System.Text.Json.JsonSerializer.Serialize(Design(),DesignSchema.Json);var logo=Resource("aimer-logo.png");await store.Put(tenant,LogoId,logo);
  await using var tx=await db.Database.BeginTransactionAsync();
  db.Set<StudioAsset>().Add(new StudioAsset{Id=LogoId,TenantId=tenant,Name="AIMER Society organization logo",Kind="Image",Length=logo.Length,AuthorId=actor});
  db.Set<DesignTemplate>().Add(new DesignTemplate{Id=TemplateId,TenantId=tenant,Name=Name,Kind="Card",Category="Individual Member",Status="Published",Design=design,PublishedVersion=1,AuthorId=actor});
  db.Set<DesignVersion>().Add(new DesignVersion{TenantId=tenant,TemplateId=TemplateId,Version=1,Design=design,PublisherId=actor});
  db.Audits.Add(new Audit{TenantId=tenant,ActorId=actor,ResourceId=TemplateId,Action="design.professional.installed",Details="{\"version\":1,\"kind\":\"Individual\"}"});
  await db.SaveChangesAsync();await tx.CommitAsync();Console.WriteLine("Published AIMER Professional Individual ID template with organization logo.");
 }
 public static void Preview(string directory){Directory.CreateDirectory(directory);var renderer=new StudioRenderer(new PreviewStore());var fields=DesignSchema.Fields.ToDictionary(k=>k,k=>"");fields["member.full_name"]="Dr. Alex Morgan";fields["member.member_id"]="AIMERS-SAMPLE-001";fields["member.membership_category"]="INDIVIDUAL MEMBER";fields["member.phone"]="+91 90000 00000";fields["card.expiry_date"]="31 Jan 2027";fields["card.qr_code"]="https://members.aimersociety.com";var snapshot=new CredentialSnapshot(Design(),fields,new());foreach(var side in new[]{"Front","Back"})File.WriteAllBytes(Path.Combine(directory,side.ToLowerInvariant()+".png"),renderer.Image(Guid.Empty,snapshot,side));fields["member.full_name"]="Professor Dr. Alexandria Chandrasekhar Radhakrishnan";File.WriteAllBytes(Path.Combine(directory,"long-name.png"),renderer.Image(Guid.Empty,snapshot,"Front"));Console.WriteLine("Rendered front, back and long-name layout check.");}
 sealed class PreviewStore:IObjectStore{public Stream Open(Guid tenant,Guid id)=>id==LogoId?new MemoryStream(Resource("aimer-logo.png")):throw new FileNotFoundException();public Task Put(Guid tenant,Guid id,byte[] bytes)=>throw new NotSupportedException();public StorageUsage Usage(Guid tenant)=>new(0,0);}
}
