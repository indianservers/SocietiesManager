using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;

namespace Society.Api;

public record RosterUpload([property: Required] string Format, [property: Required] string Content);
public record RosterPreviewRow(int Row, OrganizationMember Member, string[] Errors);

public static class OrganizationRoster
{
    public static string Key(string value) => Regex.Replace(value.Normalize(NormalizationForm.FormKC).Trim(), @"\s+", "").ToUpperInvariant();
    public static string? Mobile(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!Regex.IsMatch(value, @"^[+()0-9 .-]+$")) throw new DomainException("Enter mobile digits, with optional country code");
        var digits = Regex.Replace(value, "[^0-9]", "");
        if (digits.StartsWith("00")) digits = digits[2..];
        if (digits.Length is < 7 or > 15) throw new DomainException("Mobile must contain 7–15 digits");
        return digits;
    }
    public static string NumberKey(string value) => value.TrimStart().StartsWith('+') && Regex.IsMatch(value, @"^[+()0-9 .-]{7,}$") ? Mobile(value)! : Key(value);

    public static OrganizationMember Create(Guid tenant, Guid membership, OrganizationMemberWrite r)
    {
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(r, new ValidationContext(r), errors, true)) throw new DomainException(string.Join("; ", errors.Select(e => e.ErrorMessage)));
        if (string.IsNullOrWhiteSpace(r.FullName) || string.IsNullOrWhiteSpace(r.Role)) throw new DomainException("Enter a name and member type");
        var row = new OrganizationMember { TenantId = tenant, MembershipId = membership, FullName = r.FullName.Trim(),
            Email = string.IsNullOrWhiteSpace(r.Email) ? null : r.Email.Trim().ToUpperInvariant(), Role = r.Role.Trim(),
            Reference = r.Reference?.Trim() ?? "", Mobile = Mobile(r.Mobile) };
        row.ReferenceKey = row.Reference == "" ? null : Key(row.Reference);
        row.Number = !string.IsNullOrWhiteSpace(r.MemberId) ? r.MemberId.Trim() : row.Reference != "" ? row.Reference : row.Mobile ?? row.Email ?? "ORG-" + row.Id.ToString("N").ToUpperInvariant();
        if (row.Number.Length > 254 || row.Number.Any(char.IsControl) || row.Reference.Any(char.IsControl)) throw new DomainException("Use a member ID up to 254 characters without control characters");
        row.NumberKey = NumberKey(row.Number);
        return row;
    }

    // One index is used for single additions, preview and transactional commit.
    public sealed class Duplicates
    {
        readonly HashSet<string> numbers = new(StringComparer.Ordinal), emails = new(StringComparer.Ordinal), mobiles = new(StringComparer.Ordinal), references = new(StringComparer.Ordinal), names = new(StringComparer.Ordinal);
        public Duplicates(IEnumerable<OrganizationMember> rows)
        {
            foreach (var row in rows)
            {
                numbers.Add(row.NumberKey ?? NumberKey(row.Number)); // IDs are never recycled, including removed records.
                if (row.RevokedAt != null) continue;
                names.Add(Key(row.FullName));
                if (row.Email != null) emails.Add(Key(row.Email));
                if (row.Mobile != null) mobiles.Add(row.Mobile);
                if (row.Reference != "") references.Add(row.ReferenceKey ?? Key(row.Reference));
            }
        }
        public string[] Check(OrganizationMember row)
        {
            var errors = new List<string>();
            if (numbers.Contains(row.NumberKey!)) errors.Add("Member ID already exists in this organization (including history)");
            if (row.Email != null && emails.Contains(Key(row.Email))) errors.Add("Email already exists in this organization");
            if (row.Mobile != null && mobiles.Contains(row.Mobile)) errors.Add("Mobile already exists in this organization");
            if (row.ReferenceKey != null && references.Contains(row.ReferenceKey)) errors.Add("Roll / employee reference already exists in this organization");
            if (Generated(row) && names.Contains(Key(row.FullName))) errors.Add("Name already exists. Supply a roll number, mobile, email or explicit ID to distinguish this person");
            return errors.ToArray();
        }
    }

    static bool Generated(OrganizationMember row) => row.Number == "ORG-" + row.Id.ToString("N").ToUpperInvariant();

    public static async Task<Duplicates> Existing(AppDb db, Guid tenant, Guid membership) => new(await db.Set<OrganizationMember>().AsNoTracking().Where(r => r.TenantId == tenant && r.MembershipId == membership).ToListAsync());

    public static async Task<List<RosterPreviewRow>> Preview(AppDb db, Guid tenant, Guid membership, RosterUpload upload)
    {
        if (upload.Format is not ("xlsx" or "csv")) throw new DomainException("Use Excel .xlsx or CSV; save legacy .xls as .xlsx first");
        var records = MemberImportEndpoints.Read(upload.Format, upload.Content);
        if (records.Count is < 2 or > 10001) throw new DomainException("Upload a header and 1–10,000 data rows");
        var aliases = new Dictionary<string, string[]> {
            ["fullName"] = ["fullname", "name", "studentname", "facultyname"], ["role"] = ["role", "type", "membertype"],
            ["reference"] = ["reference", "rollnumber", "rollno", "roll", "employeeid"], ["mobile"] = ["mobile", "mobilenumber", "phone", "phonenumber"],
            ["email"] = ["email", "emailid", "emailaddress"], ["memberId"] = ["memberid", "membershipid", "uniqueid", "id"] };
        string Header(string value) => Regex.Replace(value.Trim().TrimStart('\uFEFF').ToLowerInvariant(), "[^a-z0-9]", "");
        var headers = records[0].Select(Header).ToArray();
        var columns = aliases.ToDictionary(p => p.Key, p => headers.Select((h, i) => (h, i)).Where(x => p.Value.Contains(x.h)).Select(x => x.i).ToArray());
        if (columns["fullName"].Length != 1) throw new DomainException("Include exactly one Full name column");
        if (columns.Any(c => c.Value.Length > 1)) throw new DomainException("Remove duplicate mapped columns (for example Email and Email ID)");
        var existing = await Existing(db, tenant, membership);
        var rows = new List<RosterPreviewRow>();
        for (int i = 1; i < records.Count; i++)
        {
            var record = records[i];
            if (record.All(string.IsNullOrWhiteSpace)) continue;
            string Value(string key) => columns[key].Length == 1 && columns[key][0] < record.Length ? record[columns[key][0]].Trim() : "";
            try
            {
                if (new[] { "reference", "mobile", "memberId" }.Any(k => Regex.IsMatch(Value(k), @"^[0-9]+(?:\.[0-9]+)?[Ee][+-][0-9]+$")))
                    throw new DomainException("Identifier is in scientific notation. Format that Excel column as Text and re-enter the original value before uploading");
                var role = Value("role");
                if (role == "") throw new DomainException("Member type is required (Student, Faculty, Staff or Employee)");
                var row = Create(tenant, membership, new(Value("fullName"), Value("email") == "" ? null : Value("email"), role, Value("reference"), Value("mobile"), Value("memberId")));
                rows.Add(new(i + 1, row, existing.Check(row)));
            }
            catch (DomainException e)
            {
                rows.Add(new(i + 1, new OrganizationMember { FullName = Value("fullName"), Role = Value("role"), Number = Value("memberId"), Reference = Value("reference"), ReferenceKey = Value("reference") == "" ? null : Key(Value("reference")), Email = Value("email"), Mobile = Value("mobile") }, [e.Message]));
            }
        }
        if (rows.Count == 0) throw new DomainException("The workbook has no member rows");
        // Mark every occurrence of a duplicate, rather than silently importing the first one.
        var collisions = new Dictionary<int, List<string>>();
        void Mark(Func<OrganizationMember, string?> field, string label)
        {
            foreach (var group in rows.GroupBy(r => field(r.Member)).Where(g => !string.IsNullOrEmpty(g.Key) && g.Count() > 1))
            {
                var locations = string.Join(", ", group.Take(8).Select(r => r.Row)) + (group.Count() > 8 ? $" (and {group.Count() - 8} more)" : "");
                var message = $"Duplicate {label} in upload rows {locations}";
                foreach (var row in group)
                {
                    if (!collisions.TryGetValue(row.Row, out var messages)) collisions[row.Row] = messages = [];
                    messages.Add(message);
                }
            }
        }
        Mark(r => r.NumberKey, "member ID"); Mark(r => r.Email == null ? null : Key(r.Email), "email"); Mark(r => r.Mobile, "mobile"); Mark(r => r.ReferenceKey, "roll / employee reference");
        Mark(r => Generated(r) ? Key(r.FullName) : null, "name without identifying details; add a roll number, mobile or email");
        return rows.Select(r => r with { Errors = r.Errors.Concat(collisions.GetValueOrDefault(r.Row) ?? []).ToArray() }).ToList();
    }

    public static object View(OrganizationRosterImport job, List<RosterPreviewRow> rows) => new { job.Id, job.ExpiresAt, job.CommittedAt, total = rows.Count, valid = rows.Count(r => r.Errors.Length == 0), invalid = rows.Count(r => r.Errors.Length != 0), rows = rows.Select(r => new { r.Row, r.Member.FullName, r.Member.Role, r.Member.Number, r.Member.Email, r.Member.Mobile, r.Member.Reference, r.Errors }) };

    public static string Serialize(List<RosterPreviewRow> rows) => JsonSerializer.Serialize(rows);
    public static List<RosterPreviewRow> Deserialize(string rows) => JsonSerializer.Deserialize<List<RosterPreviewRow>>(rows)!;
}
