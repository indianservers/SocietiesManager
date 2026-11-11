using System.Text.Json;
using System.Text.RegularExpressions;
namespace Society.Api;

public static class SettingsPolicy
{
    public static void Validate(JsonElement value, Guid tenant) { if (value.ValueKind != JsonValueKind.Object) throw new DomainException("Settings must be an object"); foreach (var p in value.EnumerateObject()) { if (p.Value.ValueKind == JsonValueKind.Null) continue; if (p.Value.ValueKind != JsonValueKind.String || p.Value.GetString()!.Length > 4000) throw new DomainException("Settings values must be text up to 4000 characters"); var text = p.Value.GetString()!; if (p.Name == "brandColor" && !Regex.IsMatch(text, "^#[0-9A-Fa-f]{6}$")) throw new DomainException("Choose a valid brand color"); if (p.Name == "logoUrl" && text != "" && !text.StartsWith($"/api/v1/societies/{tenant}/assets/") && (!Uri.TryCreate(text, UriKind.Absolute, out var uri) || uri.Scheme != "https")) throw new DomainException("Logo must be an uploaded asset or HTTPS image"); if (p.Name == "directoryVisibility" && !new[] { "chapter", "region", "society" }.Contains(text)) throw new DomainException("Invalid directory policy"); } }
}
