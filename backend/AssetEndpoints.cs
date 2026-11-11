using Microsoft.EntityFrameworkCore;
using System.Buffers.Binary;
using System.IO.Compression;
namespace Society.Api;

public interface IObjectStore { Task Put(Guid tenant, Guid id, byte[] bytes); Stream Open(Guid tenant, Guid id); }
public class LocalObjectStore(IConfiguration c, IHostEnvironment env) : IObjectStore
{
    string Root => Path.GetFullPath(c["Storage:Root"] ?? Path.Combine(env.ContentRootPath, "../.local/assets"));
    public async Task Put(Guid tenant, Guid id, byte[] bytes) { var dir = Path.Combine(Root, tenant.ToString()); Directory.CreateDirectory(dir); await File.WriteAllBytesAsync(Path.Combine(dir, id + ".png"), bytes); }
    public Stream Open(Guid tenant, Guid id) => File.OpenRead(Path.Combine(Root, tenant.ToString(), id + ".png"));
}
public static class AssetEndpoints
{
    public static void Map(RouteGroupBuilder api)
    {
        api.MapPost("/societies/{tenant:guid}/assets", async (Guid tenant, Guid? unit, HttpRequest request, AppDb db, Access a, IObjectStore storage) => { await a.Require(tenant, "units.write", unit); if (request.ContentLength is null or > 2097152) throw new DomainException("Upload a PNG image smaller than 2 MB"); using var output = new MemoryStream(); await request.Body.CopyToAsync(output); var clean = SanitizePng(output.ToArray()); var asset = new Asset { TenantId = tenant, Length = clean.Length }; await storage.Put(tenant, asset.Id, clean); db.Assets.Add(asset); a.Audit(tenant, "asset.uploaded", asset.Id, new { asset.Length }); await db.SaveChangesAsync(); return Results.Ok(new { url = $"/api/v1/societies/{tenant}/assets/{asset.Id}" }); }).RequireAuthorization();
        api.MapGet("/societies/{tenant:guid}/assets/{id:guid}", async (Guid tenant, Guid id, AppDb db, Access a, IObjectStore storage) => { await a.Member(tenant); if (!await db.Assets.AnyAsync(x => x.TenantId == tenant && x.Id == id)) return Results.NotFound(); return Results.File(storage.Open(tenant, id), "image/png"); }).RequireAuthorization();
    }
    public static byte[] SanitizePng(byte[] bytes)
    {
        byte[] signature = [137, 80, 78, 71, 13, 10, 26, 10]; if (bytes.Length < 45 || bytes.Length > 2097152 || !bytes.AsSpan(0, 8).SequenceEqual(signature)) throw new DomainException("Upload a valid PNG image");
        using var output = new MemoryStream(); output.Write(signature); using var compressed = new MemoryStream(); int offset = 8, width = 0, height = 0, channels = 0; bool ended = false;
        while (offset + 12 <= bytes.Length)
        {
            int length = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(offset, 4)); if (length < 0 || length > bytes.Length - offset - 12) throw new DomainException("Invalid PNG chunk"); var type = System.Text.Encoding.ASCII.GetString(bytes, offset + 4, 4); var data = bytes.AsSpan(offset + 8, length); uint crc = 0xffffffff; foreach (byte b in bytes.AsSpan(offset + 4, length + 4)) { crc ^= b; for (int bit = 0; bit < 8; bit++) crc = (crc >> 1) ^ ((crc & 1) != 0 ? 0xedb88320u : 0u); }
            if ((crc ^ 0xffffffff) != BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(offset + 8 + length, 4))) throw new DomainException("Image checksum failed");
            if (type == "IHDR") { if (offset != 8 || length != 13) throw new DomainException("Invalid PNG header"); width = BinaryPrimitives.ReadInt32BigEndian(data); height = BinaryPrimitives.ReadInt32BigEndian(data[4..]); if (width is < 1 or > 4096 || height is < 1 or > 4096 || data[8] != 8 || data[9] is not (2 or 6) || data[10] != 0 || data[11] != 0 || data[12] != 0) throw new DomainException("Use an 8-bit RGB/RGBA PNG up to 4096 x 4096 pixels, without interlacing"); channels = data[9] == 2 ? 3 : 4; }
            else if (type == "IDAT") { if (width == 0) throw new DomainException("Invalid PNG order"); compressed.Write(data); }
            else if (type == "IEND") { if (length != 0) throw new DomainException("Invalid image end"); ended = true; }
            else if (type.Length != 4 || char.IsUpper(type[0]) && type != "PLTE") throw new DomainException("Unsupported PNG chunk");
            if (type is "IHDR" or "IDAT" or "IEND" or "PLTE" or "tRNS") output.Write(bytes, offset, length + 12); offset += length + 12; if (ended) break;
        }
        if (!ended || width == 0 || offset != bytes.Length) throw new DomainException("Invalid PNG image"); compressed.Position = 0; try { using var zlib = new ZLibStream(compressed, CompressionMode.Decompress); var row = new byte[checked(width * channels + 1)]; for (int y = 0; y < height; y++) { zlib.ReadExactly(row); if (row[0] > 4) throw new InvalidDataException(); } if (zlib.ReadByte() != -1) throw new InvalidDataException(); } catch (Exception e) when (e is InvalidDataException or EndOfStreamException) { throw new DomainException("Invalid PNG pixel data"); }
        return output.ToArray();
    }
}
