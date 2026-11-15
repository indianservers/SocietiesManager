using Microsoft.EntityFrameworkCore;
namespace Society.Api;

public static class AimerCyanCard {
 public static readonly Guid LinesId=Guid.Parse("6d4c35fd-dcc4-4c22-98ab-39e1a5d86644");
 public static readonly Guid TemplateId=Guid.Parse("c55baa72-5665-47d4-8d33-d4410dd25610");
 public static readonly Guid LogoId=Guid.Parse("955410b4-3c7a-4cd4-851f-c6de3d302811");
 public const string Name="AIMER • Professional Individual ID";
 static byte[] Resource(string name){using var stream=typeof(AimerCyanCard).Assembly.GetManifestResourceStream("Society.Api.Templates."+name)??throw new InvalidOperationException("Missing card resource");using var buffer=new MemoryStream();stream.CopyTo(buffer);return buffer.ToArray();}
 public static DocumentDesign Design()=>DesignSchema.Parse(System.Text.Encoding.UTF8.GetString(Resource("aimer-cyan-front.json")));
 public static async Task Install(IServiceProvider services,Guid tenant){
  var db=services.GetRequiredService<AppDb>();var store=services.GetRequiredService<IObjectStore>();
  var society=await db.Tenants.SingleAsync(t=>t.Id==tenant);if(!society.Name.Contains("AIMER",StringComparison.OrdinalIgnoreCase))throw new DomainException("Install this society-specific template only for AIMER");
  var actor=await db.Users.Where(u=>u.PlatformAdmin).OrderBy(u=>u.Id).Select(u=>u.Id).FirstAsync();
  var existing=await db.Set<DesignTemplate>().SingleOrDefaultAsync(t=>t.Id==TemplateId&&t.TenantId==tenant);
  var lines=Resource("aimer-cyan-lines.png");await store.Put(tenant,LinesId,lines);
  if(existing!=null){
   if(existing.PublishedVersion==3){Console.WriteLine("Individual card back version already published.");return;}
   if(existing.PublishedVersion!=2)throw new DomainException("Review later template changes before updating the background");
   await using var update=await db.Database.BeginTransactionAsync();
   if(!await db.Set<StudioAsset>().AnyAsync(a=>a.Id==LinesId))db.Set<StudioAsset>().Add(new StudioAsset{Id=LinesId,TenantId=tenant,Name="Supplied cyan PNG background lines",Kind="Image",Length=lines.Length,AuthorId=actor});
   var revised=System.Text.Json.JsonSerializer.Serialize(Design(),DesignSchema.Json);
   existing.Design=revised;existing.Name=Name;existing.PublishedVersion=3;
   db.Set<DesignVersion>().Add(new DesignVersion{TenantId=tenant,TemplateId=TemplateId,Version=3,Design=revised,PublisherId=actor});
   db.Audits.Add(new Audit{TenantId=tenant,ActorId=actor,ResourceId=TemplateId,Action="design.background.updated",Details=System.Text.Json.JsonSerializer.Serialize(new{version=3,change="Added supplied back text and member name over glow PNG; approved front unchanged"})});
   await db.SaveChangesAsync();await update.CommitAsync();Console.WriteLine("Published Individual card front and back as version 3.");return;
  }
  db.Set<StudioAsset>().Add(new StudioAsset{Id=LinesId,TenantId=tenant,Name="Supplied cyan PNG background lines",Kind="Image",Length=lines.Length,AuthorId=actor});
  var chipId=Guid.Parse("ab7161e9-40c0-4ce3-8b2d-ec374e304812");var chip=Resource("aimer-brain-chip.png");await store.Put(tenant,chipId,chip);
  var design=System.Text.Json.JsonSerializer.Serialize(Design(),DesignSchema.Json);var logo=Resource("aimer-cyan-brain.png");await store.Put(tenant,LogoId,logo);
  await using var tx=await db.Database.BeginTransactionAsync();
  db.Set<StudioAsset>().Add(new StudioAsset{Id=chipId,TenantId=tenant,Name="Supplied luminous brain on chip",Kind="Image",Length=chip.Length,AuthorId=actor});
  db.Set<StudioAsset>().Add(new StudioAsset{Id=LogoId,TenantId=tenant,Name="Supplied luminous cyan brain",Kind="Image",Length=logo.Length,AuthorId=actor});
  db.Set<DesignTemplate>().Add(new DesignTemplate{Id=TemplateId,TenantId=tenant,Name=Name,Kind="Card",Category="Individual Member",Status="Published",Design=design,PublishedVersion=3,AuthorId=actor});
  db.Set<DesignVersion>().Add(new DesignVersion{TenantId=tenant,TemplateId=TemplateId,Version=3,Design=design,PublisherId=actor});
  db.Audits.Add(new Audit{TenantId=tenant,ActorId=actor,ResourceId=TemplateId,Action="design.professional.installed",Details="{\"version\":1,\"kind\":\"Individual\"}"});
  await db.SaveChangesAsync();await tx.CommitAsync();Console.WriteLine("Published AIMER Cyan Individual ID front with supplied artwork.");
 }
 public static void Preview(string directory){Directory.CreateDirectory(directory);var renderer=new StudioRenderer(new PreviewStore());var fields=DesignSchema.Fields.ToDictionary(k=>k,k=>"");fields["member.full_name"]="Dr. Alex Morgan";fields["member.member_id"]="AIMERS-SAMPLE-001";fields["member.membership_category"]="INDIVIDUAL MEMBER";fields["member.phone"]="+91 90000 00000";fields["card.expiry_date"]="31 Jan 2027";fields["card.qr_code"]="https://members.aimersociety.com";var snapshot=new CredentialSnapshot(Design(),fields,new());foreach(var side in new[]{"Front","Back"})File.WriteAllBytes(Path.Combine(directory,side.ToLowerInvariant()+".png"),renderer.Image(Guid.Empty,snapshot,side));fields["member.full_name"]="Professor Dr. Alexandria Chandrasekhar Radhakrishnan";File.WriteAllBytes(Path.Combine(directory,"long-name.png"),renderer.Image(Guid.Empty,snapshot,"Front"));Console.WriteLine("Rendered front, back and long-name layout check.");}
 sealed class PreviewStore:IObjectStore{public Stream Open(Guid tenant,Guid id)=>id==LinesId?new MemoryStream(Resource("aimer-cyan-lines.png")):id==LogoId?new MemoryStream(Resource("aimer-cyan-brain.png")):id==Guid.Parse("ab7161e9-40c0-4ce3-8b2d-ec374e304812")?new MemoryStream(Resource("aimer-brain-chip.png")):throw new FileNotFoundException();public Task Put(Guid tenant,Guid id,byte[] bytes)=>throw new NotSupportedException();public StorageUsage Usage(Guid tenant)=>new(0,0);}
}
