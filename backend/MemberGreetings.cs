using Microsoft.EntityFrameworkCore;
namespace Society.Api;

public static class MemberGreetings
{
    public static readonly (string Trigger, string Name, string Subject, string Body)[] Defaults = [
        ("MemberBirthday", "Birthday Greeting", "Happy birthday, {{member_name}}!",
         "Dear {{member_name}},\n\nWishing you a very happy birthday! May the year ahead bring you good health, happiness, and success. Thank you for being a valued part of our community.\n\nWarm wishes,\n{{society_name}}"),
        ("WeddingAnniversary", "Wedding Anniversary Greeting", "Happy wedding anniversary, {{member_name}}!",
         "Dear {{member_name}},\n\nWishing you and your spouse a wonderful wedding anniversary! May your journey together continue to be filled with love, happiness, and cherished memories.\n\nWarm wishes,\n{{society_name}}")
    ];

    // Caller holds the tenant lock, so repeated setup cannot create duplicate rules.
    public static async Task Enable(AppDb db, Guid tenant, Guid author)
    {
        foreach (var item in Defaults)
        {
            var template = await db.Set<CommunicationTemplate>().SingleOrDefaultAsync(t => t.TenantId == tenant && t.Name == item.Name && t.Language == "en" && t.Channel == "InApp");
            if (template == null)
            {
                template = new() { TenantId = tenant, Name = item.Name, Category = item.Name, Subject = item.Subject, Body = item.Body };
                db.Add(template);
            }
            // Upgrade the old placeholder while preserving customized wording.
            if (template.Body == "Hello {{member_name}},\n" + item.Name + " from {{society_name}}.")
            {
                template.Body = item.Body;
                template.Subject = item.Subject;
                template.Revision++;
            }
            template.Active = true;
            var rule = await db.Set<CommunicationAutomation>().FirstOrDefaultAsync(r => r.TenantId == tenant && r.UnitId == null && r.Trigger == item.Trigger && r.Channel == "InApp");
            if (rule == null)
            {
                rule = new() { TenantId = tenant, Name = item.Name, Trigger = item.Trigger, Channel = "InApp", Audience = "{\"match\":\"AND\",\"filters\":[]}" };
                db.Add(rule);
            }
            rule.TemplateId = template.Id;
            rule.AuthorId = author;
            rule.Enabled = true;
            rule.TestMode = false;
            rule.OffsetMinutes = 0;
        }
    }
}
