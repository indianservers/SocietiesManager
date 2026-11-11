namespace Society.Api;

public static class RoleTemplates
{
    public static IEnumerable<GovernanceRole> For(Guid tenant)
    {
        var scopeRead = new[] { "units.read", "directory.chapter" };
        var scopeWrite = new[] { "units.read", "units.write", "directory.chapter" };
        string[] governors = ["Society President", "Society Secretary", "Society Treasurer", "Society Executive Member", "National President", "National Secretary", "National Treasurer", "National Director", "Regional President", "Regional Secretary", "Regional Treasurer", "Regional Coordinator", "District Governor", "District Secretary", "District Treasurer", "Zone Chairperson", "Chapter President", "Chapter Vice President", "Chapter Secretary", "Chapter Treasurer", "Chapter Manager", "Chapter Committee Chair"];
        foreach (var name in governors) yield return new GovernanceRole { TenantId = tenant, Name = name, Permissions = (name.Contains("Treasurer") || name.Contains("Executive") ? scopeRead : scopeWrite).Concat(MembershipSeed.TemplatePermissions(name)).Concat(CommunityService.TemplatePermissions(name)).Distinct().ToArray(), Responsibilities = "Governance position template. Assign an explicit organizational scope and effective dates." };
    }
    public static readonly string[] MembershipCategories = ["Active Member", "Associate Member", "Honorary Member", "Guest", "Applicant"];
}
