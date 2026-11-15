using Microsoft.EntityFrameworkCore;
namespace Society.Api;

public static class MembershipSeed
{
    public static readonly string[] ChapterPositions = ["Chapter President", "President Elect", "Immediate Past President", "First Vice President", "Second Vice President", "Chapter Secretary", "Joint Secretary", "Chapter Treasurer", "Joint Treasurer", "Membership Director", "Event Director", "Service Director", "Training Director", "Public Relations Officer", "Board Director", "Committee Chair", "Chapter Manager", "Advisor", "Vice President", "Past President", "Honorary Secretary", "Assistant Secretary", "Assistant Treasurer", "Sergeant at Arms", "Club Administrator", "Club Mentor", "Club Patron", "Youth Director", "Sports Director", "Cultural Director", "Education Director", "Technology Director", "Fundraising Director", "Welfare Director", "International Relations Director", "Environment Director", "Health Director", "Volunteer Coordinator", "Audit Committee Chair", "Ethics Committee Chair", "Board Member"];
    public static readonly string[] RegionalPositions = ["Regional President", "Regional Vice President", "Regional Secretary", "Regional Treasurer", "Regional Coordinator", "Regional Membership Director", "Regional Service Director", "Regional Events Director", "Regional Committee Chair"];
    public static readonly string[] SocietyPositions = ["Society President", "Society Vice President", "Society Secretary", "Society Treasurer", "Executive Director", "Executive Committee Member", "National President", "National Secretary", "National Treasurer", "National Director", "Society Membership Director", "Society Events Director", "Society Service Director", "Society Training Director", "Society Board Member", "Society Advisor", "Society Past President", "Society President Elect", "Society Joint Secretary", "Society Joint Treasurer", "Society Patron"];
    public static async Task Ensure(AppDb db, Guid tenant)
    {
        await CommunityService.Seed(db,tenant); await CommunicationService.Seed(db,tenant); FinanceCommunication.Seed(db,tenant); db.Add(new StudioPolicy{TenantId=tenant}); db.Add(new TenantBrand{TenantId=tenant}); db.Add(new TenantSubscription{TenantId=tenant});
        foreach (var name in RoleTemplates.MembershipCategories) db.MembershipCategories.Add(new MembershipCategory { TenantId = tenant, Name = name });
        db.MembershipPolicies.Add(new MembershipPolicyRecord { TenantId = tenant });
        foreach (var tuple in ChapterPositions.Select(n => (name: n, level: "Chapter")).Concat(RegionalPositions.Select(n => (name: n, level: "Region"))).Concat(SocietyPositions.Select(n => (name: n, level: "Society"))))
        {
            var role = db.GovernanceRoles.Local.FirstOrDefault(r => r.TenantId == tenant && r.Name == tuple.name) ?? await db.GovernanceRoles.FirstOrDefaultAsync(r => r.TenantId == tenant && r.Name == tuple.name);
            db.OfficerPositions.Add(new OfficerPosition { TenantId = tenant, Name = tuple.name, Level = tuple.level, RoleId = role?.Id, AllowConcurrent = true, Responsibilities = "Configurable governance position. Access is granted only by an explicitly approved, dated appointment." });
        }
        db.GovernanceRoles.Add(new GovernanceRole { TenantId = tenant, Name = "Member Directory", Permissions = ["units.read", "directory.chapter"], Responsibilities = "Basic chapter navigation; directory data also follows active membership and privacy policy." });
    }
    public static string[] TemplatePermissions(string name) { if (name.Contains("Treasurer")) return ["members.billing"]; if (name.Contains("Secretary") || name.Contains("Manager")) return ["members.read", "members.write", "members.approve", "members.transfer", "members.import", "committees.manage"]; if (name.Contains("President") || name.Contains("Coordinator") || name.Contains("Governor")) return ["members.read", "members.approve", "members.transfer", "governance.manage", "committees.manage"]; return ["members.read"]; }
}
