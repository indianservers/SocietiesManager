# Database schema reference

Generated from the migrated local PostgreSQL database on 12 November 2026. This is a structural reference; no member data or credentials are included.

130 public tables. Canonical organization, person, membership, finance and communication records remain shared across all six phases. Tenant-owned references use composite tenant keys where declared. Enforcement is in EF migrations and database triggers, not this document.

Money and verified hours use decimal PostgreSQL numeric columns. Timestamps use UTC timestamptz; printed issue/expiry dates use date. JSON templates and historical snapshots use jsonb.

Migration history:
- 20261111031228_InitialFoundation
- 20261111031340_HierarchyIntegrity
- 20261111031446_SocietyInvitations
- 20261111033106_BrandingAssets
- 20261111033306_OrganizationReadModels
- 20261111040200_MembershipGovernance
- 20261111041621_MembershipPrivacyAndReviewMetadata
- 20261111042717_MembershipEvidenceDocuments
- 20261111044908_RequiredLeadershipCoverage
- 20261111045339_MemberImportLookupIndex
- 20261111052753_ChapterActivitiesEvents
- 20261111054922_ActualAttendeePartyCount
- 20261111060822_CommunityIntegrity
- 20261111070234_UnifiedCommunicationCenter
- 20261111070652_ProviderTemplateBindings
- 20261111075755_UnifiedFinancialEngine
- 20261111081643_FinancialIntegrityAndSources
- 20261111084000_FinancialPaymentRecovery
- 20261111085247_FinancialDeliveryReferences
- 20261111085518_InvoiceSubledgerReferences
- 20261111091909_FinancialReferenceConstraints
- 20261111095729_UnifiedCredentialServicePlatform
- 20261111104201_CredentialQueueIndexesAndPlanLimits
- 20261112161535_SharedStandardDesignScopes

## AccountingPeriod

Columns:
- `Id`: uuid; required
- `Name`: text; required
- `StartsOn`: date; required
- `EndsOn`: date; required
- `Closed`: bool; required
- `ClosedBy`: uuid; nullable
- `ClosedAt`: timestamptz; nullable
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_AccountingPeriod_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_AccountingPeriod_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_AccountingPeriod_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_AccountingPeriod_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_AccountingPeriod_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_AccountingPeriod`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `finance_period_dates`: `CREATE TRIGGER finance_period_dates BEFORE INSERT OR UPDATE ON public."AccountingPeriod" FOR EACH ROW EXECUTE FUNCTION finance_period_guard()`

## ActivityBookmark

Columns:
- `Id`: uuid; required
- `PostId`: uuid; required
- `UserId`: uuid; required
- `TenantId`: uuid; required

Constraints:
- `AK_ActivityBookmark_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_ActivityBookmark_ActivityPost_TenantId_PostId`: `FOREIGN KEY ("TenantId", "PostId") REFERENCES "ActivityPost"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_ActivityBookmark_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_ActivityBookmark`: `PRIMARY KEY ("Id")`

## ActivityCategory

Columns:
- `Id`: uuid; required
- `Name`: text; required
- `Kind`: text; required
- `Active`: bool; required
- `TenantId`: uuid; required

Constraints:
- `AK_ActivityCategory_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_ActivityCategory_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_ActivityCategory`: `PRIMARY KEY ("Id")`

## ActivityComment

Columns:
- `Id`: uuid; required
- `PostId`: uuid; required
- `UserId`: uuid; required
- `ParentId`: uuid; nullable
- `Body`: text; required
- `Hidden`: bool; required
- `At`: timestamptz; required
- `TenantId`: uuid; required

Constraints:
- `AK_ActivityComment_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_ActivityComment_ActivityComment_TenantId_ParentId`: `FOREIGN KEY ("TenantId", "ParentId") REFERENCES "ActivityComment"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_ActivityComment_ActivityPost_TenantId_PostId`: `FOREIGN KEY ("TenantId", "PostId") REFERENCES "ActivityPost"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_ActivityComment_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_ActivityComment`: `PRIMARY KEY ("Id")`

## ActivityPost

Columns:
- `Id`: uuid; required
- `AuthorId`: uuid; required
- `UnitId`: uuid; nullable
- `CommitteeId`: uuid; nullable
- `Official`: bool; required
- `CategoryId`: uuid; required
- `EventId`: uuid; nullable
- `Title`: text; required
- `Body`: text; required
- `Location`: text; required
- `Tags`: _text; required
- `Mentions`: _uuid; required
- `Audience`: _uuid; required
- `Visibility`: text; required
- `Status`: text; required
- `Stages`: text; required
- `Stage`: int4; required
- `Revision`: int4; required
- `Featured`: bool; required
- `Achievement`: bool; required
- `CreatedAt`: timestamptz; required
- `ScheduledAt`: timestamptz; nullable
- `PublishedAt`: timestamptz; nullable
- `TenantId`: uuid; required

Constraints:
- `AK_ActivityPost_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_ActivityPost_ActivityCategory_TenantId_CategoryId`: `FOREIGN KEY ("TenantId", "CategoryId") REFERENCES "ActivityCategory"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_ActivityPost_Committees_TenantId_CommitteeId`: `FOREIGN KEY ("TenantId", "CommitteeId") REFERENCES "Committees"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_ActivityPost_CommunityEvent_TenantId_EventId`: `FOREIGN KEY ("TenantId", "EventId") REFERENCES "CommunityEvent"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_ActivityPost_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_ActivityPost_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_ActivityPost`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `post_no_delete`: `CREATE TRIGGER post_no_delete BEFORE DELETE ON public."ActivityPost" FOR EACH ROW EXECUTE FUNCTION prevent_history_mutation()`
- `post_audience_integrity`: `CREATE TRIGGER post_audience_integrity BEFORE INSERT OR UPDATE ON public."ActivityPost" FOR EACH ROW EXECUTE FUNCTION guard_community_audience()`

## ActivityReaction

Columns:
- `Id`: uuid; required
- `PostId`: uuid; required
- `UserId`: uuid; required
- `Kind`: text; required
- `TenantId`: uuid; required

Constraints:
- `AK_ActivityReaction_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_ActivityReaction_ActivityPost_TenantId_PostId`: `FOREIGN KEY ("TenantId", "PostId") REFERENCES "ActivityPost"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_ActivityReaction_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_ActivityReaction`: `PRIMARY KEY ("Id")`

## ActivityReport

Columns:
- `Id`: uuid; required
- `PostId`: uuid; required
- `UserId`: uuid; required
- `Reason`: text; required
- `Status`: text; required
- `At`: timestamptz; required
- `TenantId`: uuid; required

Constraints:
- `AK_ActivityReport_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_ActivityReport_ActivityPost_TenantId_PostId`: `FOREIGN KEY ("TenantId", "PostId") REFERENCES "ActivityPost"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_ActivityReport_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_ActivityReport`: `PRIMARY KEY ("Id")`

## Applications

Columns:
- `Id`: uuid; required
- `ApplicantId`: uuid; required
- `Name`: text; required
- `Fingerprint`: text; required
- `Details`: jsonb; required
- `Status`: text; required
- `ReviewNote`: text; nullable
- `TenantId`: uuid; nullable
- `CreatedAt`: timestamptz; required

Constraints:
- `FK_Applications_AspNetUsers_ApplicantId`: `FOREIGN KEY ("ApplicantId") REFERENCES "AspNetUsers"("Id") ON DELETE RESTRICT`
- `PK_Applications`: `PRIMARY KEY ("Id")`

## AspNetRoleClaims

Columns:
- `Id`: int4; required
- `RoleId`: uuid; required
- `ClaimType`: text; nullable
- `ClaimValue`: text; nullable

Constraints:
- `FK_AspNetRoleClaims_AspNetRoles_RoleId`: `FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles"("Id") ON DELETE CASCADE`
- `PK_AspNetRoleClaims`: `PRIMARY KEY ("Id")`

## AspNetRoles

Columns:
- `Id`: uuid; required
- `Name`: varchar; nullable
- `NormalizedName`: varchar; nullable
- `ConcurrencyStamp`: text; nullable

Constraints:
- `PK_AspNetRoles`: `PRIMARY KEY ("Id")`

## AspNetUserClaims

Columns:
- `Id`: int4; required
- `UserId`: uuid; required
- `ClaimType`: text; nullable
- `ClaimValue`: text; nullable

Constraints:
- `FK_AspNetUserClaims_AspNetUsers_UserId`: `FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE`
- `PK_AspNetUserClaims`: `PRIMARY KEY ("Id")`

## AspNetUserLogins

Columns:
- `LoginProvider`: text; required
- `ProviderKey`: text; required
- `ProviderDisplayName`: text; nullable
- `UserId`: uuid; required

Constraints:
- `FK_AspNetUserLogins_AspNetUsers_UserId`: `FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE`
- `PK_AspNetUserLogins`: `PRIMARY KEY ("LoginProvider", "ProviderKey")`

## AspNetUserRoles

Columns:
- `UserId`: uuid; required
- `RoleId`: uuid; required

Constraints:
- `FK_AspNetUserRoles_AspNetRoles_RoleId`: `FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles"("Id") ON DELETE CASCADE`
- `FK_AspNetUserRoles_AspNetUsers_UserId`: `FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE`
- `PK_AspNetUserRoles`: `PRIMARY KEY ("UserId", "RoleId")`

## AspNetUserTokens

Columns:
- `UserId`: uuid; required
- `LoginProvider`: text; required
- `Name`: text; required
- `Value`: text; nullable

Constraints:
- `FK_AspNetUserTokens_AspNetUsers_UserId`: `FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE CASCADE`
- `PK_AspNetUserTokens`: `PRIMARY KEY ("UserId", "LoginProvider", "Name")`

## AspNetUsers

Columns:
- `Id`: uuid; required
- `FullName`: text; required
- `PlatformAdmin`: bool; required
- `UserName`: varchar; nullable
- `NormalizedUserName`: varchar; nullable
- `Email`: varchar; nullable
- `NormalizedEmail`: varchar; nullable
- `EmailConfirmed`: bool; required
- `PasswordHash`: text; nullable
- `SecurityStamp`: text; nullable
- `ConcurrencyStamp`: text; nullable
- `PhoneNumber`: text; nullable
- `PhoneNumberConfirmed`: bool; required
- `TwoFactorEnabled`: bool; required
- `LockoutEnd`: timestamptz; nullable
- `LockoutEnabled`: bool; required
- `AccessFailedCount`: int4; required

Constraints:
- `PK_AspNetUsers`: `PRIMARY KEY ("Id")`

## Assets

Columns:
- `Id`: uuid; required
- `TenantId`: uuid; required
- `Length`: int8; required
- `CreatedAt`: timestamptz; required

Constraints:
- `AK_Assets_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_Assets_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_Assets`: `PRIMARY KEY ("Id")`

## Assignments

Columns:
- `Id`: uuid; required
- `TenantId`: uuid; required
- `UserId`: uuid; required
- `RoleId`: uuid; required
- `UnitId`: uuid; nullable
- `StartsAt`: timestamptz; required
- `EndsAt`: timestamptz; nullable
- `MembershipId`: uuid; nullable
- `RevokedAt`: timestamptz; nullable

Constraints:
- `FK_Assignments_GovernanceRoles_TenantId_RoleId`: `FOREIGN KEY ("TenantId", "RoleId") REFERENCES "GovernanceRoles"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_Assignments_Links_TenantId_UserId`: `FOREIGN KEY ("TenantId", "UserId") REFERENCES "Links"("TenantId", "UserId") ON DELETE RESTRICT`
- `FK_Assignments_Memberships_TenantId_MembershipId`: `FOREIGN KEY ("TenantId", "MembershipId") REFERENCES "Memberships"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_Assignments_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_Assignments`: `PRIMARY KEY ("Id")`
- `assignment_dates`: `CHECK ((("EndsAt" IS NULL) OR ("EndsAt" > "StartsAt")))`

## AssistantQuery

Columns:
- `Id`: uuid; required
- `UserId`: uuid; required
- `Intent`: text; required
- `Mode`: text; required
- `InputTokens`: int4; required
- `OutputTokens`: int4; required
- `At`: timestamptz; required
- `TenantId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_AssistantQuery_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_AssistantQuery_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_AssistantQuery_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_AssistantQuery`: `PRIMARY KEY ("Id")`

## Audits

Columns:
- `Id`: uuid; required
- `TenantId`: uuid; required
- `ActorId`: uuid; required
- `Action`: text; required
- `ResourceId`: uuid; required
- `Details`: jsonb; required
- `At`: timestamptz; required

Constraints:
- `PK_Audits`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `audit_immutable`: `CREATE TRIGGER audit_immutable BEFORE DELETE OR UPDATE ON public."Audits" FOR EACH ROW EXECUTE FUNCTION prevent_history_mutation()`

## AutomationExecution

Columns:
- `Id`: uuid; required
- `RuleId`: uuid; required
- `SourceKey`: text; required
- `Status`: text; required
- `Note`: text; required
- `CampaignId`: uuid; nullable
- `At`: timestamptz; required
- `TenantId`: uuid; required

Constraints:
- `AK_AutomationExecution_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_AutomationExecution_CommunicationAutomation_TenantId_RuleId`: `FOREIGN KEY ("TenantId", "RuleId") REFERENCES "CommunicationAutomation"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_AutomationExecution_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_AutomationExecution`: `PRIMARY KEY ("Id")`

## BankTransaction

Columns:
- `Id`: uuid; required
- `BankId`: uuid; required
- `Reference`: text; required
- `Date`: date; required
- `Amount`: numeric; required
- `Currency`: text; required
- `Description`: text; required
- `Fingerprint`: text; required
- `Status`: text; required
- `PaymentId`: uuid; nullable
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_BankTransaction_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_BankTransaction_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_BankTransaction_FinanceBank_TenantId_EntityId_BankId`: `FOREIGN KEY ("TenantId", "EntityId", "BankId") REFERENCES "FinanceBank"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_BankTransaction_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_BankTransaction_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_BankTransaction_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_BankTransaction`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `finance_currency_precision`: `CREATE TRIGGER finance_currency_precision BEFORE INSERT OR UPDATE ON public."BankTransaction" FOR EACH ROW EXECUTE FUNCTION finance_currency_guard()`

## BudgetRevision

Columns:
- `Id`: uuid; required
- `BudgetId`: uuid; required
- `Revision`: int4; required
- `Snapshot`: text; required
- `AuthorId`: uuid; required
- `At`: timestamptz; required
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_BudgetRevision_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_BudgetRevision_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_BudgetRevision_FinanceBudget_TenantId_EntityId_BudgetId`: `FOREIGN KEY ("TenantId", "EntityId", "BudgetId") REFERENCES "FinanceBudget"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_BudgetRevision_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_BudgetRevision_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_BudgetRevision_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_BudgetRevision`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `finance_budget_version_frozen`: `CREATE TRIGGER finance_budget_version_frozen BEFORE DELETE OR UPDATE ON public."BudgetRevision" FOR EACH ROW EXECUTE FUNCTION finance_frozen_guard()`

## CampaignRecipient

Columns:
- `Id`: uuid; required
- `SnapshotVersion`: int4; required
- `CampaignId`: uuid; required
- `MembershipId`: uuid; required
- `PersonId`: uuid; required
- `UserId`: uuid; nullable
- `Destination`: text; required
- `Variables`: text; required
- `Status`: text; required
- `OutboxId`: uuid; nullable
- `AcknowledgedAt`: timestamptz; nullable
- `TenantId`: uuid; required

Constraints:
- `AK_CampaignRecipient_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_CampaignRecipient_CommunicationCampaign_TenantId_CampaignId`: `FOREIGN KEY ("TenantId", "CampaignId") REFERENCES "CommunicationCampaign"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_CampaignRecipient_Memberships_TenantId_MembershipId`: `FOREIGN KEY ("TenantId", "MembershipId") REFERENCES "Memberships"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_CampaignRecipient_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_CampaignRecipient`: `PRIMARY KEY ("Id")`

## CommitteeMemberships

Columns:
- `Id`: uuid; required
- `TenantId`: uuid; required
- `CommitteeId`: uuid; required
- `MembershipId`: uuid; required
- `Role`: text; required
- `Responsibilities`: text; required
- `StartsAt`: timestamptz; required
- `EndsAt`: timestamptz; nullable

Constraints:
- `AK_CommitteeMemberships_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_CommitteeMemberships_Committees_TenantId_CommitteeId`: `FOREIGN KEY ("TenantId", "CommitteeId") REFERENCES "Committees"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_CommitteeMemberships_Memberships_TenantId_MembershipId`: `FOREIGN KEY ("TenantId", "MembershipId") REFERENCES "Memberships"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_CommitteeMemberships_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_CommitteeMemberships`: `PRIMARY KEY ("Id")`

## Committees

Columns:
- `Id`: uuid; required
- `TenantId`: uuid; required
- `UnitId`: uuid; nullable
- `Name`: text; required
- `Purpose`: text; required
- `Status`: text; required
- `StartsAt`: timestamptz; required
- `EndsAt`: timestamptz; nullable
- `Revision`: int4; required

Constraints:
- `AK_Committees_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_Committees_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_Committees_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_Committees`: `PRIMARY KEY ("Id")`

## CommunicationAccount

Columns:
- `Id`: uuid; required
- `UnitId`: uuid; nullable
- `Name`: text; required
- `Channel`: text; required
- `Provider`: text; required
- `Sender`: text; required
- `ProviderReference`: text; required
- `ProtectedSecrets`: text; required
- `Enabled`: bool; required
- `Verified`: bool; required
- `InboundEnabled`: bool; required
- `Health`: text; required
- `PerMinute`: int4; required
- `WindowStart`: timestamptz; required
- `WindowCount`: int4; required
- `CheckedAt`: timestamptz; nullable
- `TenantId`: uuid; required

Constraints:
- `AK_CommunicationAccount_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_CommunicationAccount_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_CommunicationAccount_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_CommunicationAccount`: `PRIMARY KEY ("Id")`

## CommunicationAudience

Columns:
- `Id`: uuid; required
- `OwnerId`: uuid; required
- `UnitId`: uuid; nullable
- `Name`: text; required
- `Rules`: text; required
- `UpdatedAt`: timestamptz; required
- `TenantId`: uuid; required

Constraints:
- `AK_CommunicationAudience_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_CommunicationAudience_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_CommunicationAudience_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_CommunicationAudience`: `PRIMARY KEY ("Id")`

## CommunicationAutomation

Columns:
- `Id`: uuid; required
- `AuthorId`: uuid; required
- `UnitId`: uuid; nullable
- `TemplateId`: uuid; required
- `AccountId`: uuid; nullable
- `Name`: text; required
- `Trigger`: text; required
- `Channel`: text; required
- `Audience`: text; required
- `OffsetMinutes`: int4; required
- `RepeatMinutes`: int4; required
- `Enabled`: bool; required
- `TestMode`: bool; required
- `CreatedAt`: timestamptz; required
- `LastRun`: timestamptz; nullable
- `TenantId`: uuid; required

Constraints:
- `AK_CommunicationAutomation_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_CommunicationAutomation_CommunicationAccount_TenantId_Accou~`: `FOREIGN KEY ("TenantId", "AccountId") REFERENCES "CommunicationAccount"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_CommunicationAutomation_CommunicationTemplate_TenantId_Temp~`: `FOREIGN KEY ("TenantId", "TemplateId") REFERENCES "CommunicationTemplate"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_CommunicationAutomation_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_CommunicationAutomation_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_CommunicationAutomation`: `PRIMARY KEY ("Id")`

## CommunicationCampaign

Columns:
- `Id`: uuid; required
- `SnapshotVersion`: int4; required
- `AuthorId`: uuid; required
- `UnitId`: uuid; nullable
- `AccountId`: uuid; nullable
- `TemplateId`: uuid; nullable
- `EventId`: uuid; nullable
- `Kind`: text; required
- `Name`: text; required
- `Channel`: text; required
- `Subject`: text; required
- `Body`: text; required
- `Audience`: text; required
- `Status`: text; required
- `Stages`: text; required
- `Stage`: int4; required
- `Revision`: int4; required
- `Priority`: text; required
- `Pinned`: bool; required
- `Acknowledge`: bool; required
- `ExpiresAt`: timestamptz; nullable
- `ScheduledAt`: timestamptz; required
- `CreatedAt`: timestamptz; required
- `SnapshotCount`: int4; required
- `EnqueuedCount`: int4; required
- `SnapshotAt`: timestamptz; nullable
- `TenantId`: uuid; required

Constraints:
- `AK_CommunicationCampaign_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_CommunicationCampaign_CommunicationAccount_TenantId_Account~`: `FOREIGN KEY ("TenantId", "AccountId") REFERENCES "CommunicationAccount"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_CommunicationCampaign_CommunicationTemplate_TenantId_Templa~`: `FOREIGN KEY ("TenantId", "TemplateId") REFERENCES "CommunicationTemplate"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_CommunicationCampaign_CommunityEvent_TenantId_EventId`: `FOREIGN KEY ("TenantId", "EventId") REFERENCES "CommunityEvent"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_CommunicationCampaign_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_CommunicationCampaign_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_CommunicationCampaign`: `PRIMARY KEY ("Id")`

## CommunicationMessage

Columns:
- `Id`: uuid; required
- `ConversationId`: uuid; required
- `SenderId`: uuid; required
- `Direction`: text; required
- `Body`: text; required
- `Internal`: bool; required
- `Status`: text; required
- `ClientKey`: text; required
- `ReplyTo`: uuid; nullable
- `At`: timestamptz; required
- `RemovedAt`: timestamptz; nullable
- `TenantId`: uuid; required

Constraints:
- `AK_CommunicationMessage_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_CommunicationMessage_CommunicationMessage_TenantId_ReplyTo`: `FOREIGN KEY ("TenantId", "ReplyTo") REFERENCES "CommunicationMessage"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_CommunicationMessage_Conversation_TenantId_ConversationId`: `FOREIGN KEY ("TenantId", "ConversationId") REFERENCES "Conversation"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_CommunicationMessage_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_CommunicationMessage`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `communication_message_integrity`: `CREATE TRIGGER communication_message_integrity BEFORE INSERT OR UPDATE ON public."CommunicationMessage" FOR EACH ROW EXECUTE FUNCTION guard_communication_message()`

## CommunicationNotification

Columns:
- `Id`: uuid; required
- `UserId`: uuid; required
- `Kind`: text; required
- `Title`: text; required
- `Link`: text; required
- `ResourceId`: uuid; required
- `DedupKey`: text; required
- `At`: timestamptz; required
- `ReadAt`: timestamptz; nullable
- `TenantId`: uuid; required

Constraints:
- `AK_CommunicationNotification_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_CommunicationNotification_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_CommunicationNotification`: `PRIMARY KEY ("Id")`

## CommunicationPolicy

Columns:
- `Id`: uuid; required
- `DirectScope`: text; required
- `DirectEnabled`: bool; required
- `ApprovalStages`: text; required
- `HighVolumeThreshold`: int4; required
- `RetentionDays`: int4; required
- `Revision`: int4; required
- `TenantId`: uuid; required

Constraints:
- `AK_CommunicationPolicy_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_CommunicationPolicy_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_CommunicationPolicy`: `PRIMARY KEY ("Id")`

## CommunicationPreference

Columns:
- `Id`: uuid; required
- `UserId`: uuid; required
- `DirectMessages`: bool; required
- `InApp`: bool; required
- `Email`: bool; required
- `Sms`: bool; required
- `WhatsApp`: bool; required
- `TimeZone`: text; required
- `QuietStart`: int4; required
- `QuietEnd`: int4; required
- `UpdatedAt`: timestamptz; required
- `TenantId`: uuid; required

Constraints:
- `AK_CommunicationPreference_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_CommunicationPreference_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_CommunicationPreference`: `PRIMARY KEY ("Id")`
- `preference_user_tenant`: `FOREIGN KEY ("TenantId", "UserId") REFERENCES "Links"("TenantId", "UserId") ON DELETE RESTRICT`

## CommunicationReport

Columns:
- `Id`: uuid; required
- `MessageId`: uuid; required
- `ReporterId`: uuid; required
- `Reason`: text; required
- `Status`: text; required
- `At`: timestamptz; required
- `TenantId`: uuid; required

Constraints:
- `AK_CommunicationReport_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_CommunicationReport_CommunicationMessage_TenantId_MessageId`: `FOREIGN KEY ("TenantId", "MessageId") REFERENCES "CommunicationMessage"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_CommunicationReport_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_CommunicationReport`: `PRIMARY KEY ("Id")`

## CommunicationSuppression

Columns:
- `Id`: uuid; required
- `Channel`: text; required
- `DestinationHash`: text; required
- `Reason`: text; required
- `At`: timestamptz; required
- `TenantId`: uuid; required

Constraints:
- `AK_CommunicationSuppression_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_CommunicationSuppression_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_CommunicationSuppression`: `PRIMARY KEY ("Id")`

## CommunicationTemplate

Columns:
- `Id`: uuid; required
- `UnitId`: uuid; nullable
- `Name`: text; required
- `Channel`: text; required
- `Category`: text; required
- `Language`: text; required
- `Subject`: text; required
- `Body`: text; required
- `ProviderName`: text; nullable
- `ProviderStatus`: text; required
- `Active`: bool; required
- `Revision`: int4; required
- `TenantId`: uuid; required
- `ProviderAccountId`: uuid; nullable
- `ProviderBody`: text; required; default `''::text`

Constraints:
- `AK_CommunicationTemplate_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_CommunicationTemplate_CommunicationAccount_TenantId_Provide~`: `FOREIGN KEY ("TenantId", "ProviderAccountId") REFERENCES "CommunicationAccount"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_CommunicationTemplate_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_CommunicationTemplate_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_CommunicationTemplate`: `PRIMARY KEY ("Id")`

## CommunityBlock

Columns:
- `Id`: uuid; required
- `UserId`: uuid; required
- `BlockedUserId`: uuid; required
- `TenantId`: uuid; required

Constraints:
- `AK_CommunityBlock_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_CommunityBlock_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_CommunityBlock`: `PRIMARY KEY ("Id")`

## CommunityDecision

Columns:
- `Id`: uuid; required
- `ResourceId`: uuid; required
- `ActorId`: uuid; required
- `UnitId`: uuid; nullable
- `Kind`: text; required
- `Decision`: text; required
- `Permission`: text; required
- `Roles`: text; required
- `Note`: text; required
- `Revision`: int4; required
- `At`: timestamptz; required
- `TenantId`: uuid; required

Constraints:
- `AK_CommunityDecision_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_CommunityDecision_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_CommunityDecision`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `community_decision_immutable`: `CREATE TRIGGER community_decision_immutable BEFORE DELETE OR UPDATE ON public."CommunityDecision" FOR EACH ROW EXECUTE FUNCTION prevent_history_mutation()`

## CommunityEvent

Columns:
- `Id`: uuid; required
- `AuthorId`: uuid; required
- `UnitId`: uuid; nullable
- `CategoryId`: uuid; required
- `CoHosts`: _uuid; required
- `Title`: text; required
- `Body`: text; required
- `Venue`: text; required
- `Mode`: text; required
- `OnlineUrl`: text; nullable
- `TimeZone`: text; required
- `StartsAt`: timestamptz; required
- `EndsAt`: timestamptz; required
- `RegistrationOpens`: timestamptz; required
- `RegistrationCloses`: timestamptz; required
- `ScheduledAt`: timestamptz; nullable
- `PublishedAt`: timestamptz; nullable
- `Visibility`: text; required
- `Audience`: _uuid; required
- `MemberIds`: _uuid; required
- `CategoryIds`: _uuid; required
- `CommitteeIds`: _uuid; required
- `LeadersOnly`: bool; required
- `InvitationOnly`: bool; required
- `AllowGuests`: bool; required
- `MaxCompanions`: int4; required
- `RegistrationApproval`: bool; required
- `Capacity`: int4; required
- `Questions`: text; required
- `Status`: text; required
- `Stages`: text; required
- `Stage`: int4; required
- `Revision`: int4; required
- `CreatedAt`: timestamptz; required
- `TenantId`: uuid; required

Constraints:
- `AK_CommunityEvent_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_CommunityEvent_ActivityCategory_TenantId_CategoryId`: `FOREIGN KEY ("TenantId", "CategoryId") REFERENCES "ActivityCategory"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_CommunityEvent_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_CommunityEvent_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_CommunityEvent`: `PRIMARY KEY ("Id")`
- `event_dates_capacity`: `CHECK ((("EndsAt" > "StartsAt") AND ("RegistrationCloses" > "RegistrationOpens") AND ("Capacity" > 0) AND (("MaxCompanions" >= 0) AND ("MaxCompanions" <= 10))))`

Application integrity triggers:
- `event_no_delete`: `CREATE TRIGGER event_no_delete BEFORE DELETE ON public."CommunityEvent" FOR EACH ROW EXECUTE FUNCTION prevent_history_mutation()`
- `event_audience_integrity`: `CREATE TRIGGER event_audience_integrity BEFORE INSERT OR UPDATE ON public."CommunityEvent" FOR EACH ROW EXECUTE FUNCTION guard_community_audience()`

## CommunityMedia

Columns:
- `Id`: uuid; required
- `ResourceId`: uuid; required
- `ResourceKind`: text; required
- `Name`: text; required
- `ContentType`: text; required
- `Sort`: int4; required
- `Cover`: bool; required
- `Length`: int8; required
- `TenantId`: uuid; required

Constraints:
- `AK_CommunityMedia_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_CommunityMedia_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_CommunityMedia`: `PRIMARY KEY ("Id")`

## CommunityPolicy

Columns:
- `Id`: uuid; required
- `MemberPosting`: bool; required
- `PublicAllowed`: bool; required
- `BlockingAllowed`: bool; required
- `PostStages`: text; required
- `EventStages`: text; required
- `Revision`: int4; required
- `TenantId`: uuid; required

Constraints:
- `AK_CommunityPolicy_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_CommunityPolicy_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_CommunityPolicy`: `PRIMARY KEY ("Id")`

## CommunityVersion

Columns:
- `Id`: uuid; required
- `ResourceId`: uuid; required
- `ActorId`: uuid; required
- `Kind`: text; required
- `Revision`: int4; required
- `Snapshot`: text; required
- `At`: timestamptz; required
- `TenantId`: uuid; required

Constraints:
- `AK_CommunityVersion_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_CommunityVersion_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_CommunityVersion`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `community_version_immutable`: `CREATE TRIGGER community_version_immutable BEFORE DELETE OR UPDATE ON public."CommunityVersion" FOR EACH ROW EXECUTE FUNCTION prevent_history_mutation()`

## Conversation

Columns:
- `Id`: uuid; required
- `ExternalAddress`: text; required
- `ExternalMembershipId`: uuid; nullable
- `OwnerId`: uuid; required
- `UnitId`: uuid; nullable
- `Kind`: text; required
- `Channel`: text; required
- `AccountId`: uuid; nullable
- `Subject`: text; required
- `Description`: text; required
- `DirectKey`: text; nullable
- `CommitteeId`: uuid; nullable
- `Posting`: text; required
- `Status`: text; required
- `Priority`: text; required
- `AssignedTo`: uuid; nullable
- `FollowUpAt`: timestamptz; nullable
- `Tags`: _text; required
- `Resolution`: text; required
- `LastAt`: timestamptz; required
- `Revision`: int4; required
- `TenantId`: uuid; required

Constraints:
- `AK_Conversation_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_Conversation_Committees_TenantId_CommitteeId`: `FOREIGN KEY ("TenantId", "CommitteeId") REFERENCES "Committees"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_Conversation_CommunicationAccount_TenantId_AccountId`: `FOREIGN KEY ("TenantId", "AccountId") REFERENCES "CommunicationAccount"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_Conversation_Memberships_TenantId_ExternalMembershipId`: `FOREIGN KEY ("TenantId", "ExternalMembershipId") REFERENCES "Memberships"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_Conversation_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_Conversation_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_Conversation`: `PRIMARY KEY ("Id")`

## ConversationParticipant

Columns:
- `Id`: uuid; required
- `ConversationId`: uuid; required
- `UserId`: uuid; required
- `MembershipId`: uuid; nullable
- `Role`: text; required
- `JoinedAt`: timestamptz; required
- `LeftAt`: timestamptz; nullable
- `ReadAt`: timestamptz; required
- `Muted`: bool; required
- `TenantId`: uuid; required

Constraints:
- `AK_ConversationParticipant_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_ConversationParticipant_Conversation_TenantId_ConversationId`: `FOREIGN KEY ("TenantId", "ConversationId") REFERENCES "Conversation"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_ConversationParticipant_Memberships_TenantId_MembershipId`: `FOREIGN KEY ("TenantId", "MembershipId") REFERENCES "Memberships"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_ConversationParticipant_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_ConversationParticipant`: `PRIMARY KEY ("Id")`
- `participant_user_tenant`: `FOREIGN KEY ("TenantId", "UserId") REFERENCES "Links"("TenantId", "UserId") ON DELETE RESTRICT`

## Credential

Columns:
- `Id`: uuid; required
- `MembershipId`: uuid; required
- `PersonId`: uuid; required
- `TemplateVersionId`: uuid; required
- `JobId`: uuid; nullable
- `PreviousId`: uuid; nullable
- `EventId`: uuid; nullable
- `ProjectId`: uuid; nullable
- `Kind`: text; required
- `Category`: text; required
- `Serial`: text; required
- `Status`: text; required
- `Snapshot`: jsonb; required
- `TokenHash`: text; required
- `ProtectedToken`: text; required
- `AuthorId`: uuid; required
- `ApproverId`: uuid; nullable
- `IssueDate`: date; required
- `ExpiryDate`: date; required
- `PdfId`: uuid; nullable
- `FrontId`: uuid; nullable
- `BackId`: uuid; nullable
- `Reason`: text; required
- `At`: timestamptz; required
- `TenantId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_Credential_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_Credential_CommunityEvent_TenantId_EventId`: `FOREIGN KEY ("TenantId", "EventId") REFERENCES "CommunityEvent"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_Credential_Credential_TenantId_PreviousId`: `FOREIGN KEY ("TenantId", "PreviousId") REFERENCES "Credential"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_Credential_DesignVersion_TenantId_TemplateVersionId`: `FOREIGN KEY ("TenantId", "TemplateVersionId") REFERENCES "DesignVersion"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_Credential_Memberships_TenantId_MembershipId`: `FOREIGN KEY ("TenantId", "MembershipId") REFERENCES "Memberships"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_Credential_Persons_PersonId`: `FOREIGN KEY ("PersonId") REFERENCES "Persons"("Id") ON DELETE RESTRICT`
- `FK_Credential_ServiceProject_TenantId_ProjectId`: `FOREIGN KEY ("TenantId", "ProjectId") REFERENCES "ServiceProject"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_Credential_StudioJob_TenantId_JobId`: `FOREIGN KEY ("TenantId", "JobId") REFERENCES "StudioJob"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_Credential_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_Credential_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_Credential`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `credential_history_guard`: `CREATE TRIGGER credential_history_guard BEFORE INSERT OR DELETE OR UPDATE ON public."Credential" FOR EACH ROW EXECUTE FUNCTION credential_guard()`

## CredentialRequest

Columns:
- `Id`: uuid; required
- `CredentialId`: uuid; required
- `UserId`: uuid; required
- `Kind`: text; required
- `Note`: text; required
- `Status`: text; required
- `At`: timestamptz; required
- `TenantId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_CredentialRequest_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_CredentialRequest_Credential_TenantId_CredentialId`: `FOREIGN KEY ("TenantId", "CredentialId") REFERENCES "Credential"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_CredentialRequest_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_CredentialRequest_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_CredentialRequest`: `PRIMARY KEY ("Id")`

## DeliveryAttempt

Columns:
- `Id`: uuid; required
- `OutboxId`: uuid; required
- `Number`: int4; required
- `Status`: text; required
- `Code`: text; required
- `ProviderId`: text; required
- `At`: timestamptz; required
- `Cost`: numeric; nullable
- `Currency`: text; nullable
- `TenantId`: uuid; required

Constraints:
- `AK_DeliveryAttempt_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_DeliveryAttempt_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_DeliveryAttempt`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `communication_attempt_immutable`: `CREATE TRIGGER communication_attempt_immutable BEFORE DELETE OR UPDATE ON public."DeliveryAttempt" FOR EACH ROW EXECUTE FUNCTION prevent_history_mutation()`

## DesignTemplate

Columns:
- `Id`: uuid; required
- `Name`: text; required
- `Kind`: text; required
- `Category`: text; required
- `CategoryId`: uuid; nullable
- `Designation`: text; required
- `Status`: text; required
- `Design`: jsonb; required
- `Revision`: int4; required
- `PublishedVersion`: int4; required
- `AuthorId`: uuid; required
- `UpdatedAt`: timestamptz; required
- `TenantId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_DesignTemplate_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_DesignTemplate_MembershipCategories_TenantId_CategoryId`: `FOREIGN KEY ("TenantId", "CategoryId") REFERENCES "MembershipCategories"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_DesignTemplate_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_DesignTemplate_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_DesignTemplate`: `PRIMARY KEY ("Id")`

## DesignVersion

Columns:
- `Id`: uuid; required
- `TemplateId`: uuid; required
- `Version`: int4; required
- `Design`: jsonb; required
- `PublisherId`: uuid; required
- `At`: timestamptz; required
- `TenantId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_DesignVersion_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_DesignVersion_DesignTemplate_TenantId_TemplateId`: `FOREIGN KEY ("TenantId", "TemplateId") REFERENCES "DesignTemplate"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_DesignVersion_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_DesignVersion_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_DesignVersion`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `design_version_frozen`: `CREATE TRIGGER design_version_frozen BEFORE DELETE OR UPDATE ON public."DesignVersion" FOR EACH ROW EXECUTE FUNCTION studio_immutable()`

## DuesAssessment

Columns:
- `Id`: uuid; required
- `PlanId`: uuid; required
- `MembershipId`: uuid; nullable
- `ChapterId`: uuid; nullable
- `CounterpartyKey`: text; required
- `StartsOn`: date; required
- `EndsOn`: date; required
- `DueOn`: date; required
- `MemberCount`: int4; required
- `Snapshot`: text; required
- `BaseAmount`: numeric; required
- `PerMemberAmount`: numeric; required
- `Discount`: numeric; required
- `Total`: numeric; required
- `Currency`: text; required
- `InvoiceId`: uuid; required
- `At`: timestamptz; required
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_DuesAssessment_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_DuesAssessment_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_DuesAssessment_DuesPlan_TenantId_EntityId_PlanId`: `FOREIGN KEY ("TenantId", "EntityId", "PlanId") REFERENCES "DuesPlan"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_DuesAssessment_FinanceInvoice_TenantId_EntityId_InvoiceId`: `FOREIGN KEY ("TenantId", "EntityId", "InvoiceId") REFERENCES "FinanceInvoice"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_DuesAssessment_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_DuesAssessment_Memberships_TenantId_MembershipId`: `FOREIGN KEY ("TenantId", "MembershipId") REFERENCES "Memberships"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_DuesAssessment_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_DuesAssessment_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_DuesAssessment`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `finance_assessment_frozen`: `CREATE TRIGGER finance_assessment_frozen BEFORE DELETE OR UPDATE ON public."DuesAssessment" FOR EACH ROW EXECUTE FUNCTION finance_frozen_guard()`
- `finance_currency_precision`: `CREATE TRIGGER finance_currency_precision BEFORE INSERT OR UPDATE ON public."DuesAssessment" FOR EACH ROW EXECUTE FUNCTION finance_currency_guard()`

## DuesExemption

Columns:
- `Id`: uuid; required
- `PlanId`: uuid; required
- `MembershipId`: uuid; nullable
- `ChapterId`: uuid; nullable
- `StartsOn`: date; required
- `EndsOn`: date; required
- `Percent`: numeric; required
- `Reason`: text; required
- `AuthorId`: uuid; required
- `ApprovedBy`: uuid; nullable
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_DuesExemption_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_DuesExemption_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_DuesExemption_DuesPlan_TenantId_EntityId_PlanId`: `FOREIGN KEY ("TenantId", "EntityId", "PlanId") REFERENCES "DuesPlan"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_DuesExemption_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_DuesExemption_Memberships_TenantId_MembershipId`: `FOREIGN KEY ("TenantId", "MembershipId") REFERENCES "Memberships"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_DuesExemption_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_DuesExemption_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_DuesExemption`: `PRIMARY KEY ("Id")`

## DuesPlan

Columns:
- `Id`: uuid; required
- `Name`: text; required
- `Code`: text; required
- `Kind`: text; required
- `Target`: text; required
- `CategoryId`: uuid; nullable
- `Frequency`: text; required
- `Amount`: numeric; required
- `PerMember`: numeric; required
- `Currency`: text; required
- `EffectiveFrom`: date; required
- `EffectiveTo`: date; nullable
- `Proration`: text; required
- `GraceDays`: int4; required
- `LateFee`: numeric; required
- `DiscountPercent`: numeric; required
- `IncomeCode`: text; required
- `Active`: bool; required
- `AutoBill`: bool; required
- `RequiresApproval`: bool; required
- `Revision`: int4; required
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable
- `AuthorId`: uuid; required; default `'00000000-0000-0000-0000-000000000000'::uuid`

Constraints:
- `AK_DuesPlan_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_DuesPlan_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_DuesPlan_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_DuesPlan_MembershipCategories_TenantId_CategoryId`: `FOREIGN KEY ("TenantId", "CategoryId") REFERENCES "MembershipCategories"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_DuesPlan_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_DuesPlan_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_DuesPlan`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `finance_currency_precision`: `CREATE TRIGGER finance_currency_precision BEFORE INSERT OR UPDATE ON public."DuesPlan" FOR EACH ROW EXECUTE FUNCTION finance_currency_guard()`

## EventAttendance

Columns:
- `Id`: uuid; required
- `EventId`: uuid; required
- `RegistrationId`: uuid; required
- `OperatorId`: uuid; required
- `At`: timestamptz; required
- `EntryPoint`: text; required
- `Voided`: bool; required
- `Correction`: text; required
- `TenantId`: uuid; required
- `PeopleCount`: int4; required; default `1`

Constraints:
- `AK_EventAttendance_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_EventAttendance_CommunityEvent_TenantId_EventId`: `FOREIGN KEY ("TenantId", "EventId") REFERENCES "CommunityEvent"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_EventAttendance_EventRegistration_TenantId_RegistrationId`: `FOREIGN KEY ("TenantId", "RegistrationId") REFERENCES "EventRegistration"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_EventAttendance_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_EventAttendance`: `PRIMARY KEY ("Id")`
- `attendance_people_positive`: `CHECK (("PeopleCount" > 0))`

Application integrity triggers:
- `event_attendance_match`: `CREATE TRIGGER event_attendance_match BEFORE INSERT OR UPDATE ON public."EventAttendance" FOR EACH ROW EXECUTE FUNCTION guard_event_reference()`
- `attendance_no_delete`: `CREATE TRIGGER attendance_no_delete BEFORE DELETE ON public."EventAttendance" FOR EACH ROW EXECUTE FUNCTION prevent_history_mutation()`

## EventFinancePolicy

Columns:
- `Id`: uuid; required
- `EventId`: uuid; required
- `MemberPrice`: numeric; required
- `GuestPrice`: numeric; required
- `Currency`: text; required
- `Active`: bool; required
- `AllowManualSettlement`: bool; required
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable
- `ApprovedBy`: uuid; nullable
- `AuthorId`: uuid; required; default `'00000000-0000-0000-0000-000000000000'::uuid`

Constraints:
- `AK_EventFinancePolicy_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_EventFinancePolicy_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_EventFinancePolicy_CommunityEvent_TenantId_EventId`: `FOREIGN KEY ("TenantId", "EventId") REFERENCES "CommunityEvent"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_EventFinancePolicy_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_EventFinancePolicy_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_EventFinancePolicy_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_EventFinancePolicy`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `finance_currency_precision`: `CREATE TRIGGER finance_currency_precision BEFORE INSERT OR UPDATE ON public."EventFinancePolicy" FOR EACH ROW EXECUTE FUNCTION finance_currency_guard()`

## EventInvitation

Columns:
- `Id`: uuid; required
- `EventId`: uuid; required
- `SenderId`: uuid; required
- `UserId`: uuid; nullable
- `MembershipId`: uuid; nullable
- `Email`: text; required
- `Name`: text; required
- `TokenHash`: text; required
- `Status`: text; required
- `At`: timestamptz; required
- `ExpiresAt`: timestamptz; required
- `RespondedAt`: timestamptz; nullable
- `TenantId`: uuid; required

Constraints:
- `AK_EventInvitation_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_EventInvitation_CommunityEvent_TenantId_EventId`: `FOREIGN KEY ("TenantId", "EventId") REFERENCES "CommunityEvent"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_EventInvitation_Memberships_TenantId_MembershipId`: `FOREIGN KEY ("TenantId", "MembershipId") REFERENCES "Memberships"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_EventInvitation_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_EventInvitation`: `PRIMARY KEY ("Id")`

## EventPass

Columns:
- `Id`: uuid; required
- `EventId`: uuid; required
- `RegistrationId`: uuid; required
- `TokenHash`: text; required
- `IssuedAt`: timestamptz; required
- `RevokedAt`: timestamptz; nullable
- `TenantId`: uuid; required

Constraints:
- `AK_EventPass_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_EventPass_CommunityEvent_TenantId_EventId`: `FOREIGN KEY ("TenantId", "EventId") REFERENCES "CommunityEvent"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_EventPass_EventRegistration_TenantId_RegistrationId`: `FOREIGN KEY ("TenantId", "RegistrationId") REFERENCES "EventRegistration"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_EventPass_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_EventPass`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `event_pass_match`: `CREATE TRIGGER event_pass_match BEFORE INSERT OR UPDATE ON public."EventPass" FOR EACH ROW EXECUTE FUNCTION guard_event_reference()`

## EventRegistration

Columns:
- `Id`: uuid; required
- `EventId`: uuid; required
- `UserId`: uuid; required
- `MembershipId`: uuid; nullable
- `Name`: text; required
- `Status`: text; required
- `Companions`: int4; required
- `Answers`: text; required
- `At`: timestamptz; required
- `Revision`: int4; required
- `TenantId`: uuid; required

Constraints:
- `AK_EventRegistration_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_EventRegistration_CommunityEvent_TenantId_EventId`: `FOREIGN KEY ("TenantId", "EventId") REFERENCES "CommunityEvent"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_EventRegistration_Memberships_TenantId_MembershipId`: `FOREIGN KEY ("TenantId", "MembershipId") REFERENCES "Memberships"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_EventRegistration_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_EventRegistration`: `PRIMARY KEY ("Id")`
- `companion_count`: `CHECK ((("Companions" >= 0) AND ("Companions" <= 10)))`

Application integrity triggers:
- `registration_no_delete`: `CREATE TRIGGER registration_no_delete BEFORE DELETE ON public."EventRegistration" FOR EACH ROW EXECUTE FUNCTION prevent_history_mutation()`

## EventRsvp

Columns:
- `Id`: uuid; required
- `EventId`: uuid; required
- `UserId`: uuid; required
- `Response`: text; required
- `At`: timestamptz; required
- `TenantId`: uuid; required

Constraints:
- `AK_EventRsvp_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_EventRsvp_CommunityEvent_TenantId_EventId`: `FOREIGN KEY ("TenantId", "EventId") REFERENCES "CommunityEvent"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_EventRsvp_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_EventRsvp`: `PRIMARY KEY ("Id")`

## FinanceBank

Columns:
- `Id`: uuid; required
- `Name`: text; required
- `LastFour`: text; required
- `ProtectedDetails`: text; required
- `Currency`: text; required
- `AccountCode`: text; required
- `Active`: bool; required
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_FinanceBank_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_FinanceBank_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_FinanceBank_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_FinanceBank_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_FinanceBank_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_FinanceBank`: `PRIMARY KEY ("Id")`

## FinanceBudget

Columns:
- `Id`: uuid; required
- `EventId`: uuid; nullable
- `CommitteeId`: uuid; nullable
- `PeriodId`: uuid; required
- `Name`: text; required
- `Currency`: text; required
- `Lines`: text; required
- `Amount`: numeric; required
- `Status`: text; required
- `Revision`: int4; required
- `AuthorId`: uuid; required
- `ApprovedBy`: uuid; nullable
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_FinanceBudget_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_FinanceBudget_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_FinanceBudget_AccountingPeriod_TenantId_EntityId_PeriodId`: `FOREIGN KEY ("TenantId", "EntityId", "PeriodId") REFERENCES "AccountingPeriod"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_FinanceBudget_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_FinanceBudget_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_FinanceBudget_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_FinanceBudget`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `finance_currency_precision`: `CREATE TRIGGER finance_currency_precision BEFORE INSERT OR UPDATE ON public."FinanceBudget" FOR EACH ROW EXECUTE FUNCTION finance_currency_guard()`

## FinanceContribution

Columns:
- `Id`: uuid; required
- `DonorId`: uuid; required
- `Kind`: text; required
- `Purpose`: text; required
- `Pledged`: numeric; required
- `Currency`: text; required
- `Date`: date; required
- `DueOn`: date; required
- `Restricted`: bool; required
- `InvoiceId`: uuid; nullable
- `AuthorId`: uuid; required
- `Status`: text; required
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_FinanceContribution_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_FinanceContribution_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_FinanceContribution_FinanceDonor_TenantId_EntityId_DonorId`: `FOREIGN KEY ("TenantId", "EntityId", "DonorId") REFERENCES "FinanceDonor"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_FinanceContribution_FinanceInvoice_TenantId_EntityId_Invoic~`: `FOREIGN KEY ("TenantId", "EntityId", "InvoiceId") REFERENCES "FinanceInvoice"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_FinanceContribution_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_FinanceContribution_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_FinanceContribution_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_FinanceContribution`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `finance_currency_precision`: `CREATE TRIGGER finance_currency_precision BEFORE INSERT OR UPDATE ON public."FinanceContribution" FOR EACH ROW EXECUTE FUNCTION finance_currency_guard()`

## FinanceDonor

Columns:
- `Id`: uuid; required
- `Name`: text; required
- `Kind`: text; required
- `Contact`: text; required
- `PersonId`: uuid; nullable
- `Notes`: text; required
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_FinanceDonor_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_FinanceDonor_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_FinanceDonor_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_FinanceDonor_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_FinanceDonor_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_FinanceDonor`: `PRIMARY KEY ("Id")`

## FinanceExchangeRate

Columns:
- `Id`: uuid; required
- `FromCurrency`: text; required
- `ToCurrency`: text; required
- `Date`: date; required
- `Rate`: numeric; required
- `Source`: text; required
- `AuthorId`: uuid; required
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_FinanceExchangeRate_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_FinanceExchangeRate_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_FinanceExchangeRate_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_FinanceExchangeRate_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_FinanceExchangeRate_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_FinanceExchangeRate`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `finance_rate_frozen`: `CREATE TRIGGER finance_rate_frozen BEFORE DELETE OR UPDATE ON public."FinanceExchangeRate" FOR EACH ROW EXECUTE FUNCTION finance_frozen_guard()`

## FinanceExpense

Columns:
- `Id`: uuid; required
- `BudgetId`: uuid; nullable
- `EventId`: uuid; nullable
- `CommitteeId`: uuid; nullable
- `AuthorId`: uuid; required
- `ApprovedBy`: uuid; nullable
- `Vendor`: text; required
- `Description`: text; required
- `AccountCode`: text; required
- `Amount`: numeric; required
- `Currency`: text; required
- `Date`: date; required
- `Status`: text; required
- `JournalId`: uuid; nullable
- `PaymentJournalId`: uuid; nullable
- `PaymentReference`: text; required
- `BankId`: uuid; nullable
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_FinanceExpense_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_FinanceExpense_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_FinanceExpense_FinanceBank_TenantId_EntityId_BankId`: `FOREIGN KEY ("TenantId", "EntityId", "BankId") REFERENCES "FinanceBank"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_FinanceExpense_FinanceBudget_TenantId_EntityId_BudgetId`: `FOREIGN KEY ("TenantId", "EntityId", "BudgetId") REFERENCES "FinanceBudget"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_FinanceExpense_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_FinanceExpense_JournalEntry_TenantId_EntityId_JournalId`: `FOREIGN KEY ("TenantId", "EntityId", "JournalId") REFERENCES "JournalEntry"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_FinanceExpense_JournalEntry_TenantId_EntityId_PaymentJourna~`: `FOREIGN KEY ("TenantId", "EntityId", "PaymentJournalId") REFERENCES "JournalEntry"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_FinanceExpense_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_FinanceExpense_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_FinanceExpense`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `finance_currency_precision`: `CREATE TRIGGER finance_currency_precision BEFORE INSERT OR UPDATE ON public."FinanceExpense" FOR EACH ROW EXECUTE FUNCTION finance_currency_guard()`

## FinanceFile

Columns:
- `Id`: uuid; required
- `ResourceId`: uuid; required
- `Kind`: text; required
- `Name`: text; required
- `ContentType`: text; required
- `Length`: int8; required
- `AuthorId`: uuid; required
- `At`: timestamptz; required
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_FinanceFile_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_FinanceFile_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_FinanceFile_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_FinanceFile_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_FinanceFile_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_FinanceFile`: `PRIMARY KEY ("Id")`

## FinanceInvoice

Columns:
- `Id`: uuid; required
- `AuthorId`: uuid; required
- `ApprovedBy`: uuid; nullable
- `Number`: text; required
- `Kind`: text; required
- `OriginalId`: uuid; nullable
- `MembershipId`: uuid; nullable
- `BilledUnitId`: uuid; nullable
- `DonorId`: uuid; nullable
- `EventId`: uuid; nullable
- `RegistrationId`: uuid; nullable
- `Purpose`: text; required
- `CounterpartyName`: text; required
- `CounterpartyAddress`: text; required
- `Date`: date; required
- `DueOn`: date; required
- `Currency`: text; required
- `Lines`: text; required
- `Subtotal`: numeric; required
- `Discount`: numeric; required
- `Tax`: numeric; required
- `Total`: numeric; required
- `Paid`: numeric; required
- `Credited`: numeric; required
- `Status`: text; required
- `Notes`: text; required
- `IssuerSnapshot`: text; required
- `JournalId`: uuid; nullable
- `DocumentId`: uuid; nullable
- `Revision`: int4; required
- `At`: timestamptz; required
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_FinanceInvoice_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_FinanceInvoice_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_FinanceInvoice_CommunityEvent_TenantId_EventId`: `FOREIGN KEY ("TenantId", "EventId") REFERENCES "CommunityEvent"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_FinanceInvoice_EventRegistration_TenantId_RegistrationId`: `FOREIGN KEY ("TenantId", "RegistrationId") REFERENCES "EventRegistration"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_FinanceInvoice_FinanceDonor_TenantId_EntityId_DonorId`: `FOREIGN KEY ("TenantId", "EntityId", "DonorId") REFERENCES "FinanceDonor"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_FinanceInvoice_FinanceFile_TenantId_EntityId_DocumentId`: `FOREIGN KEY ("TenantId", "EntityId", "DocumentId") REFERENCES "FinanceFile"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_FinanceInvoice_FinanceInvoice_TenantId_EntityId_OriginalId`: `FOREIGN KEY ("TenantId", "EntityId", "OriginalId") REFERENCES "FinanceInvoice"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_FinanceInvoice_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_FinanceInvoice_JournalEntry_TenantId_EntityId_JournalId`: `FOREIGN KEY ("TenantId", "EntityId", "JournalId") REFERENCES "JournalEntry"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_FinanceInvoice_Memberships_TenantId_MembershipId`: `FOREIGN KEY ("TenantId", "MembershipId") REFERENCES "Memberships"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_FinanceInvoice_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_FinanceInvoice_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_FinanceInvoice`: `PRIMARY KEY ("Id")`
- `finance_invoice_amounts`: `CHECK ((("Total" >= (0)::numeric) AND ("Paid" >= (0)::numeric) AND ("Credited" >= (0)::numeric) AND (("Paid" + "Credited") <= "Total") AND ((("Subtotal" - "Discount") + "Tax") = "Total")))`

Application integrity triggers:
- `finance_invoice_history`: `CREATE TRIGGER finance_invoice_history BEFORE UPDATE ON public."FinanceInvoice" FOR EACH ROW EXECUTE FUNCTION finance_invoice_guard()`
- `finance_currency_precision`: `CREATE TRIGGER finance_currency_precision BEFORE INSERT OR UPDATE ON public."FinanceInvoice" FOR EACH ROW EXECUTE FUNCTION finance_currency_guard()`

## FinanceJob

Columns:
- `Id`: uuid; required
- `Kind`: text; required
- `AuthorId`: uuid; required
- `PlanId`: uuid; nullable
- `StartsOn`: date; required
- `EndsOn`: date; required
- `State`: text; required
- `Payload`: text; required
- `Cursor`: text; required
- `Processed`: int4; required
- `Failed`: int4; required
- `Error`: text; required
- `DocumentId`: uuid; nullable
- `Key`: text; required
- `At`: timestamptz; required
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_FinanceJob_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_FinanceJob_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_FinanceJob_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_FinanceJob_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_FinanceJob_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_FinanceJob`: `PRIMARY KEY ("Id")`

## FinancePayment

Columns:
- `Id`: uuid; required
- `AuthorId`: uuid; required
- `ApprovedBy`: uuid; nullable
- `MembershipId`: uuid; nullable
- `PayerUnitId`: uuid; nullable
- `DonorId`: uuid; nullable
- `Payer`: text; required
- `Reference`: text; required
- `Method`: text; required
- `Amount`: numeric; required
- `Refunded`: numeric; required
- `ChargedBack`: numeric; required
- `Currency`: text; required
- `Date`: date; required
- `SettledOn`: date; nullable
- `Status`: text; required
- `ProviderId`: uuid; nullable
- `ProviderReference`: text; nullable
- `JournalId`: uuid; nullable
- `BankId`: uuid; nullable
- `Notes`: text; required
- `At`: timestamptz; required
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_FinancePayment_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_FinancePayment_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_FinancePayment_FinanceBank_TenantId_EntityId_BankId`: `FOREIGN KEY ("TenantId", "EntityId", "BankId") REFERENCES "FinanceBank"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_FinancePayment_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_FinancePayment_JournalEntry_TenantId_EntityId_JournalId`: `FOREIGN KEY ("TenantId", "EntityId", "JournalId") REFERENCES "JournalEntry"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_FinancePayment_Memberships_TenantId_MembershipId`: `FOREIGN KEY ("TenantId", "MembershipId") REFERENCES "Memberships"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_FinancePayment_PaymentConnection_TenantId_EntityId_Provider~`: `FOREIGN KEY ("TenantId", "EntityId", "ProviderId") REFERENCES "PaymentConnection"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_FinancePayment_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_FinancePayment_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_FinancePayment`: `PRIMARY KEY ("Id")`
- `finance_payment_amounts`: `CHECK ((("Amount" > (0)::numeric) AND ("Refunded" >= (0)::numeric) AND ("ChargedBack" >= (0)::numeric) AND (("Refunded" + "ChargedBack") <= "Amount")))`

Application integrity triggers:
- `finance_currency_precision`: `CREATE TRIGGER finance_currency_precision BEFORE INSERT OR UPDATE ON public."FinancePayment" FOR EACH ROW EXECUTE FUNCTION finance_currency_guard()`

## FinanceProviderEvent

Columns:
- `Id`: uuid; required
- `ConnectionId`: uuid; required
- `EventKey`: text; required
- `Kind`: text; required
- `Hash`: text; required
- `Reference`: text; required
- `At`: timestamptz; required
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_FinanceProviderEvent_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_FinanceProviderEvent_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_FinanceProviderEvent_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_FinanceProviderEvent_PaymentConnection_TenantId_EntityId_Co~`: `FOREIGN KEY ("TenantId", "EntityId", "ConnectionId") REFERENCES "PaymentConnection"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_FinanceProviderEvent_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_FinanceProviderEvent_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_FinanceProviderEvent`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `finance_provider_frozen`: `CREATE TRIGGER finance_provider_frozen BEFORE DELETE OR UPDATE ON public."FinanceProviderEvent" FOR EACH ROW EXECUTE FUNCTION finance_frozen_guard()`

## FinanceReceipt

Columns:
- `Id`: uuid; required
- `PaymentId`: uuid; required
- `Number`: text; required
- `Amount`: numeric; required
- `Currency`: text; required
- `Snapshot`: text; required
- `DocumentId`: uuid; nullable
- `At`: timestamptz; required
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_FinanceReceipt_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_FinanceReceipt_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_FinanceReceipt_FinanceFile_TenantId_EntityId_DocumentId`: `FOREIGN KEY ("TenantId", "EntityId", "DocumentId") REFERENCES "FinanceFile"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_FinanceReceipt_FinancePayment_TenantId_EntityId_PaymentId`: `FOREIGN KEY ("TenantId", "EntityId", "PaymentId") REFERENCES "FinancePayment"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_FinanceReceipt_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_FinanceReceipt_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_FinanceReceipt_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_FinanceReceipt`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `finance_receipt_history`: `CREATE TRIGGER finance_receipt_history BEFORE DELETE OR UPDATE ON public."FinanceReceipt" FOR EACH ROW EXECUTE FUNCTION finance_receipt_guard()`
- `finance_currency_precision`: `CREATE TRIGGER finance_currency_precision BEFORE INSERT OR UPDATE ON public."FinanceReceipt" FOR EACH ROW EXECUTE FUNCTION finance_currency_guard()`

## FinanceRefund

Columns:
- `Id`: uuid; required
- `PaymentId`: uuid; required
- `AuthorId`: uuid; required
- `ApprovedBy`: uuid; nullable
- `Amount`: numeric; required
- `Currency`: text; required
- `Reason`: text; required
- `Reference`: text; required
- `Status`: text; required
- `ProviderReference`: text; nullable
- `JournalId`: uuid; nullable
- `Date`: date; required
- `At`: timestamptz; required
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable
- `Kind`: text; required; default `''::text`
- `ProcessingAt`: timestamptz; nullable

Constraints:
- `AK_FinanceRefund_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_FinanceRefund_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_FinanceRefund_FinancePayment_TenantId_EntityId_PaymentId`: `FOREIGN KEY ("TenantId", "EntityId", "PaymentId") REFERENCES "FinancePayment"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_FinanceRefund_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_FinanceRefund_JournalEntry_TenantId_EntityId_JournalId`: `FOREIGN KEY ("TenantId", "EntityId", "JournalId") REFERENCES "JournalEntry"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_FinanceRefund_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_FinanceRefund_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_FinanceRefund`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `finance_currency_precision`: `CREATE TRIGGER finance_currency_precision BEFORE INSERT OR UPDATE ON public."FinanceRefund" FOR EACH ROW EXECUTE FUNCTION finance_currency_guard()`

## FinanceReminder

Columns:
- `Id`: uuid; required
- `Name`: text; required
- `DaysFromDue`: int4; required
- `Channel`: text; required
- `AccountId`: uuid; nullable
- `TemplateId`: uuid; nullable
- `AuthorId`: uuid; required
- `Enabled`: bool; required
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_FinanceReminder_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_FinanceReminder_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_FinanceReminder_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_FinanceReminder_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_FinanceReminder_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_FinanceReminder`: `PRIMARY KEY ("Id")`

## FinanceReminderRun

Columns:
- `Id`: uuid; required
- `RuleId`: uuid; required
- `InvoiceId`: uuid; required
- `Cycle`: text; required
- `OutboxId`: uuid; nullable
- `Status`: text; required
- `At`: timestamptz; required
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_FinanceReminderRun_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_FinanceReminderRun_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_FinanceReminderRun_FinanceInvoice_TenantId_EntityId_Invoice~`: `FOREIGN KEY ("TenantId", "EntityId", "InvoiceId") REFERENCES "FinanceInvoice"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_FinanceReminderRun_FinanceReminder_TenantId_EntityId_RuleId`: `FOREIGN KEY ("TenantId", "EntityId", "RuleId") REFERENCES "FinanceReminder"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_FinanceReminderRun_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_FinanceReminderRun_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_FinanceReminderRun_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_FinanceReminderRun`: `PRIMARY KEY ("Id")`

## FinancialEntity

Columns:
- `Id`: uuid; required
- `UnitId`: uuid; nullable
- `Name`: text; required
- `LegalName`: text; required
- `Country`: text; required
- `Address`: text; required
- `TaxRegistration`: text; required
- `Currency`: text; required
- `FiscalMonth`: int4; required
- `Active`: bool; required
- `InvoicePrefix`: text; required
- `ReceiptPrefix`: text; required
- `NextInvoice`: int8; required
- `NextReceipt`: int8; required
- `NextJournal`: int8; required
- `RequireInvoiceApproval`: bool; required
- `ApprovalLimit`: numeric; required
- `Revision`: int4; required
- `TenantId`: uuid; required

Constraints:
- `AK_FinancialEntity_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_FinancialEntity_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_FinancialEntity`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `finance_entity_currency`: `CREATE TRIGGER finance_entity_currency BEFORE UPDATE ON public."FinancialEntity" FOR EACH ROW EXECUTE FUNCTION finance_entity_guard()`

## FinancialUnit

Columns:
- `Id`: uuid; required
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_FinancialUnit_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_FinancialUnit_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_FinancialUnit_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_FinancialUnit_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_FinancialUnit_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_FinancialUnit`: `PRIMARY KEY ("Id")`

## GovernanceRoles

Columns:
- `Id`: uuid; required
- `TenantId`: uuid; required
- `Name`: text; required
- `Permissions`: _text; required
- `Responsibilities`: text; required

Constraints:
- `AK_GovernanceRoles_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_GovernanceRoles_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_GovernanceRoles`: `PRIMARY KEY ("Id")`

## ImpactMetric

Columns:
- `Id`: uuid; required
- `Name`: text; required
- `Unit`: text; required
- `Category`: text; required
- `Aggregation`: text; required
- `EvidenceRequired`: bool; required
- `Active`: bool; required
- `TenantId`: uuid; required

Constraints:
- `AK_ImpactMetric_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_ImpactMetric_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_ImpactMetric`: `PRIMARY KEY ("Id")`

## ImpactResult

Columns:
- `Id`: uuid; required
- `ProjectId`: uuid; required
- `MetricId`: uuid; required
- `OutcomeKey`: text; required
- `Value`: numeric; required
- `Evidence`: text; required
- `EvidenceId`: uuid; nullable
- `Status`: text; required
- `AuthorId`: uuid; required
- `VerifierId`: uuid; nullable
- `Note`: text; required
- `Revision`: int4; required
- `At`: timestamptz; required
- `TenantId`: uuid; required

Constraints:
- `AK_ImpactResult_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_ImpactResult_ImpactMetric_TenantId_MetricId`: `FOREIGN KEY ("TenantId", "MetricId") REFERENCES "ImpactMetric"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_ImpactResult_ServiceProject_TenantId_ProjectId`: `FOREIGN KEY ("TenantId", "ProjectId") REFERENCES "ServiceProject"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_ImpactResult_StudioAsset_TenantId_EvidenceId`: `FOREIGN KEY ("TenantId", "EvidenceId") REFERENCES "StudioAsset"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_ImpactResult_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_ImpactResult`: `PRIMARY KEY ("Id")`
- `impact_positive`: `CHECK ((("Value" >= (0)::numeric) AND (("Status" <> 'Verified'::text) OR (("VerifierId" IS NOT NULL) AND ("VerifierId" <> "AuthorId")))))`

## InterEntityObligation

Columns:
- `Id`: uuid; required
- `OtherEntityId`: uuid; required
- `OtherUnitId`: uuid; required
- `Amount`: numeric; required
- `Settled`: numeric; required
- `Currency`: text; required
- `Purpose`: text; required
- `AuthorId`: uuid; required
- `ApprovedBy`: uuid; nullable
- `Status`: text; required
- `JournalId`: uuid; nullable
- `OtherJournalId`: uuid; nullable
- `Date`: date; required
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_InterEntityObligation_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_InterEntityObligation_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_InterEntityObligation_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_InterEntityObligation_FinancialEntity_TenantId_OtherEntityId`: `FOREIGN KEY ("TenantId", "OtherEntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_InterEntityObligation_JournalEntry_TenantId_OtherEntityId_O~`: `FOREIGN KEY ("TenantId", "OtherEntityId", "OtherJournalId") REFERENCES "JournalEntry"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_InterEntityObligation_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_InterEntityObligation_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_InterEntityObligation`: `PRIMARY KEY ("Id")`
- `finance_obligation_amounts`: `CHECK ((("Amount" > (0)::numeric) AND ("Settled" >= (0)::numeric) AND ("Settled" <= "Amount") AND ("EntityId" <> "OtherEntityId")))`

Application integrity triggers:
- `finance_currency_precision`: `CREATE TRIGGER finance_currency_precision BEFORE INSERT OR UPDATE ON public."InterEntityObligation" FOR EACH ROW EXECUTE FUNCTION finance_currency_guard()`

## InterEntitySettlement

Columns:
- `Id`: uuid; required
- `ObligationId`: uuid; required
- `Amount`: numeric; required
- `Reference`: text; required
- `Date`: date; required
- `AuthorId`: uuid; required
- `ApprovedBy`: uuid; nullable
- `Status`: text; required
- `JournalId`: uuid; nullable
- `OtherJournalId`: uuid; nullable
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_InterEntitySettlement_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_InterEntitySettlement_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_InterEntitySettlement_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_InterEntitySettlement_InterEntityObligation_TenantId_Entity~`: `FOREIGN KEY ("TenantId", "EntityId", "ObligationId") REFERENCES "InterEntityObligation"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_InterEntitySettlement_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_InterEntitySettlement_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_InterEntitySettlement`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `finance_currency_precision`: `CREATE TRIGGER finance_currency_precision BEFORE INSERT OR UPDATE ON public."InterEntitySettlement" FOR EACH ROW EXECUTE FUNCTION finance_currency_guard()`

## Invitations

Columns:
- `Id`: uuid; required
- `TenantId`: uuid; required
- `Email`: text; required
- `TokenHash`: text; required
- `ExpiresAt`: timestamptz; required
- `AcceptedAt`: timestamptz; nullable

Constraints:
- `FK_Invitations_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_Invitations`: `PRIMARY KEY ("Id")`

## JournalEntry

Columns:
- `Id`: uuid; required
- `PeriodId`: uuid; required
- `Number`: text; required
- `Date`: date; required
- `Currency`: text; required
- `Purpose`: text; required
- `SourceKind`: text; required
- `SourceId`: uuid; required
- `SourceKey`: text; required
- `AuthorId`: uuid; required
- `ApprovedBy`: uuid; nullable
- `Posted`: bool; required
- `PostedAt`: timestamptz; nullable
- `ReversesId`: uuid; nullable
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_JournalEntry_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_JournalEntry_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_JournalEntry_AccountingPeriod_TenantId_EntityId_PeriodId`: `FOREIGN KEY ("TenantId", "EntityId", "PeriodId") REFERENCES "AccountingPeriod"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_JournalEntry_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_JournalEntry_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_JournalEntry_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_JournalEntry`: `PRIMARY KEY ("Id")`
- `finance_header_balance`: `TRIGGER DEFERRABLE INITIALLY DEFERRED`

Application integrity triggers:
- `finance_header_balance`: `CREATE CONSTRAINT TRIGGER finance_header_balance AFTER INSERT OR UPDATE ON public."JournalEntry" DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION finance_journal_check()`
- `finance_posted_header`: `CREATE TRIGGER finance_posted_header BEFORE DELETE OR UPDATE ON public."JournalEntry" FOR EACH ROW EXECUTE FUNCTION finance_posted_guard()`

## JournalLine

Columns:
- `Id`: uuid; required
- `JournalId`: uuid; required
- `AccountId`: uuid; required
- `Debit`: numeric; required
- `Credit`: numeric; required
- `Memo`: text; required
- `MembershipId`: uuid; nullable
- `CounterpartyUnitId`: uuid; nullable
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable
- `InvoiceId`: uuid; nullable

Constraints:
- `AK_JournalLine_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_JournalLine_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_JournalLine_FinanceInvoice_TenantId_EntityId_InvoiceId`: `FOREIGN KEY ("TenantId", "EntityId", "InvoiceId") REFERENCES "FinanceInvoice"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_JournalLine_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_JournalLine_JournalEntry_TenantId_EntityId_JournalId`: `FOREIGN KEY ("TenantId", "EntityId", "JournalId") REFERENCES "JournalEntry"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_JournalLine_LedgerAccount_TenantId_EntityId_AccountId`: `FOREIGN KEY ("TenantId", "EntityId", "AccountId") REFERENCES "LedgerAccount"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_JournalLine_Memberships_TenantId_MembershipId`: `FOREIGN KEY ("TenantId", "MembershipId") REFERENCES "Memberships"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_JournalLine_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_JournalLine_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_JournalLine`: `PRIMARY KEY ("Id")`
- `finance_line_balance`: `TRIGGER DEFERRABLE INITIALLY DEFERRED`
- `finance_line_sign`: `CHECK (((("Debit" > (0)::numeric) AND ("Credit" = (0)::numeric)) OR (("Credit" > (0)::numeric) AND ("Debit" = (0)::numeric))))`

Application integrity triggers:
- `finance_line_balance`: `CREATE CONSTRAINT TRIGGER finance_line_balance AFTER INSERT OR DELETE OR UPDATE ON public."JournalLine" DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION finance_journal_check()`
- `finance_posted_line`: `CREATE TRIGGER finance_posted_line BEFORE INSERT OR DELETE OR UPDATE ON public."JournalLine" FOR EACH ROW EXECUTE FUNCTION finance_posted_guard()`
- `finance_currency_precision`: `CREATE TRIGGER finance_currency_precision BEFORE INSERT OR UPDATE ON public."JournalLine" FOR EACH ROW EXECUTE FUNCTION finance_currency_guard()`

## LeadershipHandovers

Columns:
- `Id`: uuid; required
- `TenantId`: uuid; required
- `OutgoingId`: uuid; required
- `IncomingId`: uuid; required
- `Notes`: text; required
- `Status`: text; required
- `Revision`: int4; required

Constraints:
- `AK_LeadershipHandovers_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_LeadershipHandovers_OfficerAppointments_TenantId_IncomingId`: `FOREIGN KEY ("TenantId", "IncomingId") REFERENCES "OfficerAppointments"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_LeadershipHandovers_OfficerAppointments_TenantId_OutgoingId`: `FOREIGN KEY ("TenantId", "OutgoingId") REFERENCES "OfficerAppointments"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_LeadershipHandovers_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_LeadershipHandovers`: `PRIMARY KEY ("Id")`

## LedgerAccount

Columns:
- `Id`: uuid; required
- `Code`: text; required
- `Name`: text; required
- `Type`: text; required
- `Active`: bool; required
- `Control`: bool; required
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_LedgerAccount_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_LedgerAccount_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_LedgerAccount_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_LedgerAccount_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_LedgerAccount_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_LedgerAccount`: `PRIMARY KEY ("Id")`

## Levels

Columns:
- `Id`: uuid; required
- `TenantId`: uuid; required
- `Name`: text; required
- `Kind`: text; required
- `AllowedParents`: _uuid; required
- `AllowRoot`: bool; required

Constraints:
- `AK_Levels_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_Levels_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_Levels`: `PRIMARY KEY ("Id")`

## Links

Columns:
- `TenantId`: uuid; required
- `UserId`: uuid; required

Constraints:
- `FK_Links_AspNetUsers_UserId`: `FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE RESTRICT`
- `FK_Links_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_Links`: `PRIMARY KEY ("TenantId", "UserId")`

## MemberApplications

Columns:
- `Id`: uuid; required
- `TenantId`: uuid; required
- `UserId`: uuid; required
- `ChapterId`: uuid; required
- `CategoryId`: uuid; required
- `MembershipId`: uuid; nullable
- `Status`: text; required
- `Profile`: jsonb; required
- `Stages`: jsonb; required
- `Stage`: int4; required
- `Revision`: int4; required
- `CreatedAt`: timestamptz; required

Constraints:
- `AK_MemberApplications_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_MemberApplications_AspNetUsers_UserId`: `FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE RESTRICT`
- `FK_MemberApplications_MembershipCategories_TenantId_CategoryId`: `FOREIGN KEY ("TenantId", "CategoryId") REFERENCES "MembershipCategories"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_MemberApplications_Memberships_TenantId_MembershipId`: `FOREIGN KEY ("TenantId", "MembershipId") REFERENCES "Memberships"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_MemberApplications_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_MemberApplications_Units_TenantId_ChapterId`: `FOREIGN KEY ("TenantId", "ChapterId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_MemberApplications`: `PRIMARY KEY ("Id")`

## MemberDocuments

Columns:
- `Id`: uuid; required
- `TenantId`: uuid; required
- `MembershipId`: uuid; nullable
- `Name`: text; required
- `Length`: int8; required
- `UploadedBy`: uuid; required
- `CreatedAt`: timestamptz; required
- `ContentType`: text; required; default `''::text`
- `Kind`: text; required; default `''::text`
- `ApplicationId`: uuid; nullable

Constraints:
- `AK_MemberDocuments_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_MemberDocuments_MemberApplications_TenantId_ApplicationId`: `FOREIGN KEY ("TenantId", "ApplicationId") REFERENCES "MemberApplications"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_MemberDocuments_Memberships_TenantId_MembershipId`: `FOREIGN KEY ("TenantId", "MembershipId") REFERENCES "Memberships"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_MemberDocuments_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_MemberDocuments`: `PRIMARY KEY ("Id")`
- `member_document_owner`: `CHECK ((((("MembershipId" IS NOT NULL))::integer + (("ApplicationId" IS NOT NULL))::integer) = 1))`

## MemberImportJobs

Columns:
- `Id`: uuid; required
- `TenantId`: uuid; required
- `ChapterId`: uuid; required
- `CategoryId`: uuid; required
- `ActorId`: uuid; required
- `Status`: text; required
- `Total`: int4; required
- `Processed`: int4; required
- `CreatedAt`: timestamptz; required

Constraints:
- `AK_MemberImportJobs_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_MemberImportJobs_MembershipCategories_TenantId_CategoryId`: `FOREIGN KEY ("TenantId", "CategoryId") REFERENCES "MembershipCategories"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_MemberImportJobs_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_MemberImportJobs_Units_TenantId_ChapterId`: `FOREIGN KEY ("TenantId", "ChapterId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_MemberImportJobs`: `PRIMARY KEY ("Id")`

## MemberImportRows

Columns:
- `Id`: uuid; required
- `TenantId`: uuid; required
- `JobId`: uuid; required
- `Row`: int4; required
- `Data`: jsonb; required
- `Error`: text; required
- `Status`: text; required
- `MembershipId`: uuid; nullable

Constraints:
- `AK_MemberImportRows_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_MemberImportRows_MemberImportJobs_TenantId_JobId`: `FOREIGN KEY ("TenantId", "JobId") REFERENCES "MemberImportJobs"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_MemberImportRows_Memberships_TenantId_MembershipId`: `FOREIGN KEY ("TenantId", "MembershipId") REFERENCES "Memberships"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_MemberImportRows_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_MemberImportRows`: `PRIMARY KEY ("Id")`

## MemberInvitations

Columns:
- `Id`: uuid; required
- `TenantId`: uuid; required
- `ChapterId`: uuid; required
- `CategoryId`: uuid; required
- `PersonId`: uuid; nullable
- `InviterId`: uuid; required
- `Email`: text; required
- `TokenHash`: text; required
- `ExpiresAt`: timestamptz; required
- `Limit`: int4; required
- `Uses`: int4; required
- `Revoked`: bool; required

Constraints:
- `AK_MemberInvitations_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_MemberInvitations_MembershipCategories_TenantId_CategoryId`: `FOREIGN KEY ("TenantId", "CategoryId") REFERENCES "MembershipCategories"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_MemberInvitations_Persons_PersonId`: `FOREIGN KEY ("PersonId") REFERENCES "Persons"("Id") ON DELETE RESTRICT`
- `FK_MemberInvitations_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_MemberInvitations_Units_TenantId_ChapterId`: `FOREIGN KEY ("TenantId", "ChapterId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_MemberInvitations`: `PRIMARY KEY ("Id")`

## MemberSavedViews

Columns:
- `Id`: uuid; required
- `TenantId`: uuid; required
- `UserId`: uuid; required
- `Name`: text; required
- `Data`: jsonb; required

Constraints:
- `AK_MemberSavedViews_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_MemberSavedViews_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_MemberSavedViews`: `PRIMARY KEY ("Id")`

## MembershipCategories

Columns:
- `Id`: uuid; required
- `TenantId`: uuid; required
- `Name`: text; required
- `Description`: text; required
- `Active`: bool; required

Constraints:
- `AK_MembershipCategories_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_MembershipCategories_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_MembershipCategories`: `PRIMARY KEY ("Id")`

## MembershipDecisions

Columns:
- `Id`: uuid; required
- `TenantId`: uuid; required
- `ApplicationId`: uuid; required
- `Sequence`: int4; required
- `ReviewerId`: uuid; required
- `Permission`: text; required
- `Decision`: text; required
- `Comment`: text; required
- `At`: timestamptz; required
- `OrganizationId`: uuid; required; default `'00000000-0000-0000-0000-000000000000'::uuid`
- `ReviewerRoles`: text; required; default `''::text`
- `DocumentIds`: _uuid; required; default `ARRAY[]::uuid[]`

Constraints:
- `AK_MembershipDecisions_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_MembershipDecisions_MemberApplications_TenantId_Application~`: `FOREIGN KEY ("TenantId", "ApplicationId") REFERENCES "MemberApplications"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_MembershipDecisions_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_MembershipDecisions_Units_TenantId_OrganizationId`: `FOREIGN KEY ("TenantId", "OrganizationId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_MembershipDecisions`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `member_decision_immutable`: `CREATE TRIGGER member_decision_immutable BEFORE DELETE OR UPDATE ON public."MembershipDecisions" FOR EACH ROW EXECUTE FUNCTION prevent_history_mutation()`

## MembershipHistories

Columns:
- `Id`: uuid; required
- `TenantId`: uuid; required
- `MembershipId`: uuid; required
- `ActorId`: uuid; required
- `Action`: text; required
- `Details`: jsonb; required
- `At`: timestamptz; required

Constraints:
- `AK_MembershipHistories_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_MembershipHistories_Memberships_TenantId_MembershipId`: `FOREIGN KEY ("TenantId", "MembershipId") REFERENCES "Memberships"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_MembershipHistories_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_MembershipHistories`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `member_history_immutable`: `CREATE TRIGGER member_history_immutable BEFORE DELETE OR UPDATE ON public."MembershipHistories" FOR EACH ROW EXECUTE FUNCTION prevent_history_mutation()`

## MembershipPolicies

Columns:
- `Id`: uuid; required
- `TenantId`: uuid; required
- `ChapterId`: uuid; nullable
- `AllowMultipleChapters`: bool; required
- `DirectoryScope`: text; required
- `SelectedChapters`: _uuid; required
- `ContactsAllowed`: bool; required
- `SourceTransferReview`: bool; required
- `SocietyTransferReview`: bool; required
- `ApprovalStages`: jsonb; required
- `Revision`: int4; required
- `DirectoryLevelId`: uuid; nullable

Constraints:
- `AK_MembershipPolicies_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_MembershipPolicies_Levels_TenantId_DirectoryLevelId`: `FOREIGN KEY ("TenantId", "DirectoryLevelId") REFERENCES "Levels"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_MembershipPolicies_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_MembershipPolicies_Units_TenantId_ChapterId`: `FOREIGN KEY ("TenantId", "ChapterId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_MembershipPolicies`: `PRIMARY KEY ("Id")`

## MembershipTransfers

Columns:
- `Id`: uuid; required
- `TenantId`: uuid; required
- `MembershipId`: uuid; required
- `SourceId`: uuid; required
- `DestinationId`: uuid; required
- `RequestedBy`: uuid; required
- `Status`: text; required
- `SocietyReview`: bool; required
- `EffectiveAt`: timestamptz; required
- `AppliedAt`: timestamptz; nullable
- `Notes`: text; required
- `Revision`: int4; required

Constraints:
- `AK_MembershipTransfers_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_MembershipTransfers_Memberships_TenantId_MembershipId`: `FOREIGN KEY ("TenantId", "MembershipId") REFERENCES "Memberships"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_MembershipTransfers_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_MembershipTransfers_Units_TenantId_DestinationId`: `FOREIGN KEY ("TenantId", "DestinationId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_MembershipTransfers_Units_TenantId_SourceId`: `FOREIGN KEY ("TenantId", "SourceId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_MembershipTransfers`: `PRIMARY KEY ("Id")`

## Memberships

Columns:
- `Id`: uuid; required
- `TenantId`: uuid; required
- `PersonId`: uuid; required
- `ChapterId`: uuid; required
- `CategoryId`: uuid; required
- `Number`: text; required
- `Status`: text; required
- `JoinedAt`: timestamptz; required
- `EffectiveFrom`: timestamptz; required
- `EndsAt`: timestamptz; nullable
- `Profile`: jsonb; required
- `Privacy`: jsonb; required
- `Revision`: int4; required

Constraints:
- `AK_Memberships_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_Memberships_MembershipCategories_TenantId_CategoryId`: `FOREIGN KEY ("TenantId", "CategoryId") REFERENCES "MembershipCategories"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_Memberships_Persons_PersonId`: `FOREIGN KEY ("PersonId") REFERENCES "Persons"("Id") ON DELETE RESTRICT`
- `FK_Memberships_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_Memberships_Units_TenantId_ChapterId`: `FOREIGN KEY ("TenantId", "ChapterId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_Memberships`: `PRIMARY KEY ("Id")`
- `membership_dates`: `CHECK ((("EndsAt" IS NULL) OR ("EndsAt" > "EffectiveFrom")))`

Application integrity triggers:
- `membership_no_delete`: `CREATE TRIGGER membership_no_delete BEFORE DELETE ON public."Memberships" FOR EACH ROW EXECUTE FUNCTION prevent_history_mutation()`
- `membership_plan_insert`: `CREATE TRIGGER membership_plan_insert AFTER INSERT ON public."Memberships" REFERENCING NEW TABLE AS membership_new FOR EACH STATEMENT EXECUTE FUNCTION membership_plan_quota()`
- `membership_plan_update`: `CREATE TRIGGER membership_plan_update AFTER UPDATE ON public."Memberships" REFERENCING OLD TABLE AS membership_old NEW TABLE AS membership_new FOR EACH STATEMENT EXECUTE FUNCTION membership_plan_quota()`

## MessageAttachment

Columns:
- `Id`: uuid; required
- `MessageId`: uuid; required
- `Name`: text; required
- `ContentType`: text; required
- `Length`: int8; required
- `TenantId`: uuid; required

Constraints:
- `AK_MessageAttachment_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_MessageAttachment_CommunicationMessage_TenantId_MessageId`: `FOREIGN KEY ("TenantId", "MessageId") REFERENCES "CommunicationMessage"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_MessageAttachment_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_MessageAttachment`: `PRIMARY KEY ("Id")`

## OfficerAppointments

Columns:
- `Id`: uuid; required
- `TenantId`: uuid; required
- `MembershipId`: uuid; required
- `UnitId`: uuid; nullable
- `PositionId`: uuid; required
- `AssignmentId`: uuid; nullable
- `StartsAt`: timestamptz; required
- `EndsAt`: timestamptz; required
- `Method`: text; required
- `Status`: text; required
- `AuthorityId`: uuid; required
- `Notes`: text; required
- `Revision`: int4; required
- `DocumentIds`: _uuid; required; default `ARRAY[]::uuid[]`

Constraints:
- `AK_OfficerAppointments_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_OfficerAppointments_Assignments_AssignmentId`: `FOREIGN KEY ("AssignmentId") REFERENCES "Assignments"("Id") ON DELETE RESTRICT`
- `FK_OfficerAppointments_Memberships_TenantId_MembershipId`: `FOREIGN KEY ("TenantId", "MembershipId") REFERENCES "Memberships"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_OfficerAppointments_OfficerPositions_TenantId_PositionId`: `FOREIGN KEY ("TenantId", "PositionId") REFERENCES "OfficerPositions"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_OfficerAppointments_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_OfficerAppointments_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_OfficerAppointments`: `PRIMARY KEY ("Id")`
- `appointment_dates`: `CHECK (("EndsAt" > "StartsAt"))`

Application integrity triggers:
- `officer_term_integrity`: `CREATE TRIGGER officer_term_integrity BEFORE INSERT OR UPDATE ON public."OfficerAppointments" FOR EACH ROW EXECUTE FUNCTION guard_officer_term()`

## OfficerPositions

Columns:
- `Id`: uuid; required
- `TenantId`: uuid; required
- `Name`: text; required
- `Level`: text; required
- `RoleId`: uuid; nullable
- `AllowConcurrent`: bool; required
- `Responsibilities`: text; required
- `Active`: bool; required
- `Required`: bool; required; default `false`

Constraints:
- `AK_OfficerPositions_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_OfficerPositions_GovernanceRoles_TenantId_RoleId`: `FOREIGN KEY ("TenantId", "RoleId") REFERENCES "GovernanceRoles"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_OfficerPositions_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_OfficerPositions`: `PRIMARY KEY ("Id")`

## Onboardings

Columns:
- `TenantId`: uuid; required
- `Step`: int4; required
- `Data`: jsonb; required
- `Complete`: bool; required

Constraints:
- `FK_Onboardings_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_Onboardings`: `PRIMARY KEY ("TenantId")`

## Outbox

Columns:
- `Id`: uuid; required
- `Recipient`: text; required
- `Subject`: text; required
- `Body`: text; required
- `CreatedAt`: timestamptz; required
- `SentAt`: timestamptz; nullable
- `Attempts`: int4; required
- `AccountId`: uuid; nullable
- `CampaignId`: uuid; nullable
- `Channel`: text; required; default `''::text`
- `ConversationId`: uuid; nullable
- `DeliveredAt`: timestamptz; nullable
- `Error`: text; nullable
- `IdempotencyKey`: text; nullable
- `LeaseOwner`: text; nullable
- `LeaseUntil`: timestamptz; nullable
- `MembershipId`: uuid; nullable
- `MessageId`: uuid; nullable
- `NextAttemptAt`: timestamptz; required; default `'-infinity'::timestamp with time zone`
- `ProviderId`: text; nullable
- `ReadAt`: timestamptz; nullable
- `Status`: text; required; default `''::text`
- `TemplateLanguage`: text; nullable
- `TemplateName`: text; nullable
- `TenantId`: uuid; nullable
- `UnitId`: uuid; nullable
- `UnsubscribeHash`: text; nullable
- `UserId`: uuid; nullable
- `TemplateParameters`: text; required; default `''::text`
- `SenderId`: uuid; nullable
- `FinanceInvoiceId`: uuid; nullable

Constraints:
- `FK_Outbox_FinanceInvoice_TenantId_FinanceInvoiceId`: `FOREIGN KEY ("TenantId", "FinanceInvoiceId") REFERENCES "FinanceInvoice"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_Outbox`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `communication_outbox_integrity`: `CREATE TRIGGER communication_outbox_integrity BEFORE INSERT OR UPDATE ON public."Outbox" FOR EACH ROW EXECUTE FUNCTION guard_communication_outbox()`

## PaymentAllocation

Columns:
- `Id`: uuid; required
- `PaymentId`: uuid; required
- `InvoiceId`: uuid; required
- `Amount`: numeric; required
- `At`: timestamptz; required
- `Key`: text; required
- `JournalId`: uuid; required
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_PaymentAllocation_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_PaymentAllocation_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_PaymentAllocation_FinanceInvoice_TenantId_EntityId_InvoiceId`: `FOREIGN KEY ("TenantId", "EntityId", "InvoiceId") REFERENCES "FinanceInvoice"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_PaymentAllocation_FinancePayment_TenantId_EntityId_PaymentId`: `FOREIGN KEY ("TenantId", "EntityId", "PaymentId") REFERENCES "FinancePayment"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_PaymentAllocation_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_PaymentAllocation_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_PaymentAllocation_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_PaymentAllocation`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `finance_currency_precision`: `CREATE TRIGGER finance_currency_precision BEFORE INSERT OR UPDATE ON public."PaymentAllocation" FOR EACH ROW EXECUTE FUNCTION finance_currency_guard()`

## PaymentConnection

Columns:
- `Id`: uuid; required
- `Name`: text; required
- `Provider`: text; required
- `PublicKey`: text; required
- `ProtectedSecrets`: text; required
- `Mode`: text; required
- `Enabled`: bool; required
- `Verified`: bool; required
- `Health`: text; required
- `CheckedAt`: timestamptz; nullable
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_PaymentConnection_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_PaymentConnection_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_PaymentConnection_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_PaymentConnection_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_PaymentConnection_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_PaymentConnection`: `PRIMARY KEY ("Id")`

## PaymentIntent

Columns:
- `Id`: uuid; required
- `InvoiceId`: uuid; required
- `ConnectionId`: uuid; required
- `UserId`: uuid; required
- `Amount`: numeric; required
- `Currency`: text; required
- `Status`: text; required
- `OrderId`: text; nullable
- `PaymentId`: uuid; nullable
- `Key`: text; required
- `At`: timestamptz; required
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_PaymentIntent_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_PaymentIntent_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_PaymentIntent_FinanceInvoice_TenantId_EntityId_InvoiceId`: `FOREIGN KEY ("TenantId", "EntityId", "InvoiceId") REFERENCES "FinanceInvoice"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_PaymentIntent_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_PaymentIntent_PaymentConnection_TenantId_EntityId_Connectio~`: `FOREIGN KEY ("TenantId", "EntityId", "ConnectionId") REFERENCES "PaymentConnection"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_PaymentIntent_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_PaymentIntent_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_PaymentIntent`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `finance_currency_precision`: `CREATE TRIGGER finance_currency_precision BEFORE INSERT OR UPDATE ON public."PaymentIntent" FOR EACH ROW EXECUTE FUNCTION finance_currency_guard()`

## Persons

Columns:
- `Id`: uuid; required
- `UserId`: uuid; nullable
- `FullName`: text; required
- `SearchName`: text; required
- `CreatedAt`: timestamptz; required
- `Revision`: int4; required

Constraints:
- `FK_Persons_AspNetUsers_UserId`: `FOREIGN KEY ("UserId") REFERENCES "AspNetUsers"("Id") ON DELETE RESTRICT`
- `PK_Persons`: `PRIMARY KEY ("Id")`

## ProjectChapter

Columns:
- `Id`: uuid; required
- `ProjectId`: uuid; required
- `ChapterId`: uuid; required
- `TenantId`: uuid; required

Constraints:
- `AK_ProjectChapter_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_ProjectChapter_ServiceProject_TenantId_ProjectId`: `FOREIGN KEY ("TenantId", "ProjectId") REFERENCES "ServiceProject"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_ProjectChapter_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_ProjectChapter_Units_TenantId_ChapterId`: `FOREIGN KEY ("TenantId", "ChapterId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_ProjectChapter`: `PRIMARY KEY ("Id")`

## ProjectTask

Columns:
- `Id`: uuid; required
- `ProjectId`: uuid; required
- `MembershipId`: uuid; nullable
- `Title`: text; required
- `Status`: text; required
- `DueOn`: date; required
- `Revision`: int4; required
- `TenantId`: uuid; required

Constraints:
- `AK_ProjectTask_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_ProjectTask_Memberships_TenantId_MembershipId`: `FOREIGN KEY ("TenantId", "MembershipId") REFERENCES "Memberships"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_ProjectTask_ServiceProject_TenantId_ProjectId`: `FOREIGN KEY ("TenantId", "ProjectId") REFERENCES "ServiceProject"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_ProjectTask_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_ProjectTask`: `PRIMARY KEY ("Id")`

## ProviderEvent

Columns:
- `Id`: uuid; required
- `AccountId`: uuid; required
- `ExternalId`: text; required
- `ProviderId`: text; required
- `Kind`: text; required
- `PayloadHash`: text; required
- `At`: timestamptz; required
- `TenantId`: uuid; required

Constraints:
- `AK_ProviderEvent_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_ProviderEvent_CommunicationAccount_TenantId_AccountId`: `FOREIGN KEY ("TenantId", "AccountId") REFERENCES "CommunicationAccount"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_ProviderEvent_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_ProviderEvent`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `provider_event_immutable`: `CREATE TRIGGER provider_event_immutable BEFORE DELETE OR UPDATE ON public."ProviderEvent" FOR EACH ROW EXECUTE FUNCTION prevent_history_mutation()`

## ReconciliationMatch

Columns:
- `Id`: uuid; required
- `BankTransactionId`: uuid; required
- `PaymentId`: uuid; required
- `Amount`: numeric; required
- `AuthorId`: uuid; required
- `At`: timestamptz; required
- `TenantId`: uuid; required
- `EntityId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_ReconciliationMatch_TenantId_EntityId_Id`: `UNIQUE ("TenantId", "EntityId", "Id")`
- `AK_ReconciliationMatch_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_ReconciliationMatch_BankTransaction_TenantId_EntityId_BankT~`: `FOREIGN KEY ("TenantId", "EntityId", "BankTransactionId") REFERENCES "BankTransaction"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_ReconciliationMatch_FinancePayment_TenantId_EntityId_Paymen~`: `FOREIGN KEY ("TenantId", "EntityId", "PaymentId") REFERENCES "FinancePayment"("TenantId", "EntityId", "Id") ON DELETE RESTRICT`
- `FK_ReconciliationMatch_FinancialEntity_TenantId_EntityId`: `FOREIGN KEY ("TenantId", "EntityId") REFERENCES "FinancialEntity"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_ReconciliationMatch_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_ReconciliationMatch_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_ReconciliationMatch`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `finance_currency_precision`: `CREATE TRIGGER finance_currency_precision BEFORE INSERT OR UPDATE ON public."ReconciliationMatch" FOR EACH ROW EXECUTE FUNCTION finance_currency_guard()`

## SavedUnifiedReport

Columns:
- `Id`: uuid; required
- `AuthorId`: uuid; required
- `Name`: text; required
- `Definition`: jsonb; required
- `TenantId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_SavedUnifiedReport_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_SavedUnifiedReport_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_SavedUnifiedReport_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_SavedUnifiedReport`: `PRIMARY KEY ("Id")`

## ServiceProject

Columns:
- `Id`: uuid; required
- `Name`: text; required
- `Category`: text; required
- `Description`: text; required
- `Objectives`: text; required
- `Location`: text; required
- `ManagerId`: uuid; required
- `CommitteeId`: uuid; nullable
- `BudgetId`: uuid; nullable
- `EventId`: uuid; nullable
- `ActivityId`: uuid; nullable
- `StartsOn`: date; required
- `EndsOn`: date; required
- `Status`: text; required
- `AuthorId`: uuid; required
- `ApproverId`: uuid; nullable
- `Revision`: int4; required
- `TenantId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_ServiceProject_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_ServiceProject_ActivityPost_TenantId_ActivityId`: `FOREIGN KEY ("TenantId", "ActivityId") REFERENCES "ActivityPost"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_ServiceProject_Committees_TenantId_CommitteeId`: `FOREIGN KEY ("TenantId", "CommitteeId") REFERENCES "Committees"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_ServiceProject_CommunityEvent_TenantId_EventId`: `FOREIGN KEY ("TenantId", "EventId") REFERENCES "CommunityEvent"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_ServiceProject_FinanceBudget_TenantId_BudgetId`: `FOREIGN KEY ("TenantId", "BudgetId") REFERENCES "FinanceBudget"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_ServiceProject_Memberships_TenantId_ManagerId`: `FOREIGN KEY ("TenantId", "ManagerId") REFERENCES "Memberships"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_ServiceProject_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_ServiceProject_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_ServiceProject`: `PRIMARY KEY ("Id")`
- `project_dates`: `CHECK ((("EndsOn" >= "StartsOn") AND (("Status" <> ALL (ARRAY['Approved'::text, 'Completed'::text])) OR (("ApproverId" IS NOT NULL) AND ("ApproverId" <> "AuthorId")))))`

## StudioAsset

Columns:
- `Id`: uuid; required
- `Name`: text; required
- `Kind`: text; required
- `Length`: int8; required
- `AuthorId`: uuid; required
- `At`: timestamptz; required
- `TenantId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_StudioAsset_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_StudioAsset_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_StudioAsset_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_StudioAsset`: `PRIMARY KEY ("Id")`

Application integrity triggers:
- `studio_asset_frozen`: `CREATE TRIGGER studio_asset_frozen BEFORE DELETE OR UPDATE ON public."StudioAsset" FOR EACH ROW EXECUTE FUNCTION studio_immutable()`

## StudioDownload

Columns:
- `Id`: uuid; required
- `JobId`: uuid; required
- `UserId`: uuid; required
- `TokenHash`: text; required
- `ExpiresAt`: timestamptz; required
- `TenantId`: uuid; required

Constraints:
- `AK_StudioDownload_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_StudioDownload_StudioJob_TenantId_JobId`: `FOREIGN KEY ("TenantId", "JobId") REFERENCES "StudioJob"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_StudioDownload_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_StudioDownload`: `PRIMARY KEY ("Id")`

## StudioJob

Columns:
- `Id`: uuid; required
- `Kind`: text; required
- `AuthorId`: uuid; required
- `ApproverId`: uuid; nullable
- `TemplateVersionId`: uuid; nullable
- `Key`: text; required
- `Payload`: jsonb; required
- `State`: text; required
- `Total`: int4; required
- `Processed`: int4; required
- `Failed`: int4; required
- `FileId`: uuid; nullable
- `ContentType`: text; required
- `Error`: text; required
- `At`: timestamptz; required
- `CompletedAt`: timestamptz; nullable
- `TenantId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_StudioJob_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_StudioJob_DesignVersion_TenantId_TemplateVersionId`: `FOREIGN KEY ("TenantId", "TemplateVersionId") REFERENCES "DesignVersion"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_StudioJob_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_StudioJob_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_StudioJob`: `PRIMARY KEY ("Id")`

## StudioJobItem

Columns:
- `Id`: uuid; required
- `JobId`: uuid; required
- `MembershipId`: uuid; required
- `CredentialId`: uuid; nullable
- `State`: text; required
- `Error`: text; required
- `Attempts`: int4; required
- `TenantId`: uuid; required

Constraints:
- `AK_StudioJobItem_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_StudioJobItem_Credential_TenantId_CredentialId`: `FOREIGN KEY ("TenantId", "CredentialId") REFERENCES "Credential"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_StudioJobItem_Memberships_TenantId_MembershipId`: `FOREIGN KEY ("TenantId", "MembershipId") REFERENCES "Memberships"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_StudioJobItem_StudioJob_TenantId_JobId`: `FOREIGN KEY ("TenantId", "JobId") REFERENCES "StudioJob"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_StudioJobItem_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_StudioJobItem`: `PRIMARY KEY ("Id")`

## StudioPolicy

Columns:
- `Id`: uuid; required
- `ChapterCustomization`: bool; required
- `MemberDownloads`: bool; required
- `PublicNames`: bool; required
- `Revision`: int4; required
- `TenantId`: uuid; required

Constraints:
- `AK_StudioPolicy_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_StudioPolicy_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_StudioPolicy`: `PRIMARY KEY ("Id")`

## TenantBrand

Columns:
- `Id`: uuid; required
- `DisplayName`: text; required
- `PrimaryColor`: text; required
- `LogoId`: uuid; nullable
- `FaviconId`: uuid; nullable
- `LoginTitle`: text; required
- `ChapterTerm`: text; required
- `RegionTerm`: text; required
- `Language`: text; required
- `TimeZone`: text; required
- `Currency`: text; required
- `Modules`: _text; required
- `Revision`: int4; required
- `TenantId`: uuid; required

Constraints:
- `AK_TenantBrand_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_TenantBrand_Assets_TenantId_FaviconId`: `FOREIGN KEY ("TenantId", "FaviconId") REFERENCES "Assets"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_TenantBrand_Assets_TenantId_LogoId`: `FOREIGN KEY ("TenantId", "LogoId") REFERENCES "Assets"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_TenantBrand_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_TenantBrand`: `PRIMARY KEY ("Id")`

## TenantDomain

Columns:
- `Id`: uuid; required
- `Host`: text; required
- `Challenge`: text; required
- `Status`: text; required
- `VerifiedAt`: timestamptz; nullable
- `TenantId`: uuid; required

Constraints:
- `AK_TenantDomain_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_TenantDomain_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_TenantDomain`: `PRIMARY KEY ("Id")`

## TenantSubscription

Columns:
- `Id`: uuid; required
- `Plan`: text; required
- `State`: text; required
- `ChapterLimit`: int4; required
- `MembershipLimit`: int4; required
- `StorageLimit`: int8; required
- `Features`: _text; required
- `EndsAt`: timestamptz; nullable
- `Revision`: int4; required
- `TenantId`: uuid; required

Constraints:
- `AK_TenantSubscription_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_TenantSubscription_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_TenantSubscription`: `PRIMARY KEY ("Id")`

## Tenants

Columns:
- `Id`: uuid; required
- `Name`: text; required
- `Status`: text; required
- `Profile`: jsonb; required
- `CreatedAt`: timestamptz; required

Constraints:
- `PK_Tenants`: `PRIMARY KEY ("Id")`

## UnifiedReportSchedule

Columns:
- `Id`: uuid; required
- `ReportId`: uuid; required
- `AuthorId`: uuid; required
- `Format`: text; required
- `Frequency`: text; required
- `NextRun`: timestamptz; required
- `Enabled`: bool; required
- `LastError`: text; required
- `TenantId`: uuid; required
- `UnitId`: uuid; nullable

Constraints:
- `AK_UnifiedReportSchedule_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_UnifiedReportSchedule_SavedUnifiedReport_TenantId_ReportId`: `FOREIGN KEY ("TenantId", "ReportId") REFERENCES "SavedUnifiedReport"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_UnifiedReportSchedule_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_UnifiedReportSchedule_Units_TenantId_UnitId`: `FOREIGN KEY ("TenantId", "UnitId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_UnifiedReportSchedule`: `PRIMARY KEY ("Id")`

## Units

Columns:
- `Id`: uuid; required
- `TenantId`: uuid; required
- `ParentId`: uuid; nullable
- `LevelId`: uuid; required
- `Name`: text; required
- `Code`: text; required
- `Status`: text; required
- `Details`: jsonb; required
- `CreatedAt`: timestamptz; required
- `UpdatedAt`: timestamptz; required
- `Revision`: int4; required

Constraints:
- `AK_Units_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_Units_Levels_TenantId_LevelId`: `FOREIGN KEY ("TenantId", "LevelId") REFERENCES "Levels"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_Units_Units_TenantId_ParentId`: `FOREIGN KEY ("TenantId", "ParentId") REFERENCES "Units"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_Units`: `PRIMARY KEY ("Id")`
- `unit_name_code`: `CHECK ((((length(TRIM(BOTH FROM "Name")) >= 1) AND (length(TRIM(BOTH FROM "Name")) <= 160)) AND ("Code" ~ '^[A-Z0-9_-]{2,40}$'::text)))`

Application integrity triggers:
- `unit_integrity`: `CREATE TRIGGER unit_integrity BEFORE INSERT OR UPDATE ON public."Units" FOR EACH ROW EXECUTE FUNCTION guard_unit_hierarchy()`
- `unit_no_delete`: `CREATE TRIGGER unit_no_delete BEFORE DELETE ON public."Units" FOR EACH ROW EXECUTE FUNCTION prevent_history_mutation()`
- `chapter_plan_insert`: `CREATE TRIGGER chapter_plan_insert AFTER INSERT ON public."Units" REFERENCING NEW TABLE AS units_new FOR EACH STATEMENT EXECUTE FUNCTION chapter_plan_quota()`
- `chapter_plan_update`: `CREATE TRIGGER chapter_plan_update AFTER UPDATE ON public."Units" REFERENCING OLD TABLE AS units_old NEW TABLE AS units_new FOR EACH STATEMENT EXECUTE FUNCTION chapter_plan_quota()`

## VolunteerAssignment

Columns:
- `Id`: uuid; required
- `ProjectId`: uuid; required
- `MembershipId`: uuid; required
- `Status`: text; required
- `Role`: text; required
- `CoordinatorId`: uuid; nullable
- `At`: timestamptz; required
- `TenantId`: uuid; required

Constraints:
- `AK_VolunteerAssignment_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_VolunteerAssignment_Memberships_TenantId_MembershipId`: `FOREIGN KEY ("TenantId", "MembershipId") REFERENCES "Memberships"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_VolunteerAssignment_ServiceProject_TenantId_ProjectId`: `FOREIGN KEY ("TenantId", "ProjectId") REFERENCES "ServiceProject"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_VolunteerAssignment_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_VolunteerAssignment`: `PRIMARY KEY ("Id")`

## VolunteerAttendance

Columns:
- `Id`: uuid; required
- `ShiftId`: uuid; required
- `MembershipId`: uuid; required
- `CheckedIn`: timestamptz; required
- `CheckedOut`: timestamptz; nullable
- `Hours`: numeric; required
- `Status`: text; required
- `RecorderId`: uuid; required
- `VerifierId`: uuid; nullable
- `Note`: text; required
- `TenantId`: uuid; required

Constraints:
- `AK_VolunteerAttendance_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_VolunteerAttendance_Memberships_TenantId_MembershipId`: `FOREIGN KEY ("TenantId", "MembershipId") REFERENCES "Memberships"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_VolunteerAttendance_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `FK_VolunteerAttendance_VolunteerShift_TenantId_ShiftId`: `FOREIGN KEY ("TenantId", "ShiftId") REFERENCES "VolunteerShift"("TenantId", "Id") ON DELETE RESTRICT`
- `PK_VolunteerAttendance`: `PRIMARY KEY ("Id")`
- `volunteer_time_integrity`: `CHECK ((("Hours" >= (0)::numeric) AND (("CheckedOut" IS NULL) OR ("CheckedOut" > "CheckedIn")) AND (("Status" <> 'Verified'::text) OR (("VerifierId" IS NOT NULL) AND ("VerifierId" <> "RecorderId") AND ("CheckedOut" IS NOT NULL)))))`

## VolunteerProfile

Columns:
- `Id`: uuid; required
- `MembershipId`: uuid; required
- `Skills`: text; required
- `Interests`: text; required
- `Availability`: text; required
- `PreferredTypes`: text; required
- `Active`: bool; required
- `TenantId`: uuid; required

Constraints:
- `AK_VolunteerProfile_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_VolunteerProfile_Memberships_TenantId_MembershipId`: `FOREIGN KEY ("TenantId", "MembershipId") REFERENCES "Memberships"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_VolunteerProfile_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_VolunteerProfile`: `PRIMARY KEY ("Id")`

## VolunteerShift

Columns:
- `Id`: uuid; required
- `ProjectId`: uuid; required
- `Name`: text; required
- `StartsAt`: timestamptz; required
- `EndsAt`: timestamptz; required
- `Capacity`: int4; required
- `TenantId`: uuid; required

Constraints:
- `AK_VolunteerShift_TenantId_Id`: `UNIQUE ("TenantId", "Id")`
- `FK_VolunteerShift_ServiceProject_TenantId_ProjectId`: `FOREIGN KEY ("TenantId", "ProjectId") REFERENCES "ServiceProject"("TenantId", "Id") ON DELETE RESTRICT`
- `FK_VolunteerShift_Tenants_TenantId`: `FOREIGN KEY ("TenantId") REFERENCES "Tenants"("Id") ON DELETE RESTRICT`
- `PK_VolunteerShift`: `PRIMARY KEY ("Id")`

## __EFMigrationsHistory

Columns:
- `MigrationId`: varchar; required
- `ProductVersion`: varchar; required

Constraints:
- `PK___EFMigrationsHistory`: `PRIMARY KEY ("MigrationId")`

## Shared database functions

Actual migrated public-schema function definitions:

### chapter_plan_quota

```sql
CREATE OR REPLACE FUNCTION public.chapter_plan_quota()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$ DECLARE tenant uuid; quota int; added int; removed int; BEGIN
 FOR tenant IN SELECT DISTINCT "TenantId" FROM units_new LOOP
 SELECT "ChapterLimit" INTO quota FROM "TenantSubscription" WHERE "TenantId"=tenant;
 IF quota IS NULL THEN CONTINUE;END IF;
 SELECT count(*) INTO added FROM units_new u WHERE u."TenantId"=tenant AND u."Status" NOT IN('Closed','Archived') AND EXISTS(SELECT 1 FROM "Levels" l WHERE l."Id"=u."LevelId" AND l."Kind"='Chapter');removed:=0;
 IF TG_OP='UPDATE' THEN SELECT count(*) INTO removed FROM units_old u WHERE u."TenantId"=tenant AND u."Status" NOT IN('Closed','Archived') AND EXISTS(SELECT 1 FROM "Levels" l WHERE l."Id"=u."LevelId" AND l."Kind"='Chapter');END IF;
 IF added<=removed THEN CONTINUE;END IF;
 PERFORM pg_advisory_xact_lock(hashtextextended(tenant::text,626));
 IF (SELECT count(*) FROM "Units" u WHERE u."TenantId"=tenant AND u."Status" NOT IN('Closed','Archived') AND EXISTS(SELECT 1 FROM "Levels" l WHERE l."Id"=u."LevelId" AND l."Kind"='Chapter'))>quota THEN RAISE EXCEPTION 'Chapter plan allowance exceeded' USING ERRCODE='23514';END IF;
 END LOOP;RETURN NULL;END $function$

```

### credential_guard

```sql
CREATE OR REPLACE FUNCTION public.credential_guard()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$ BEGIN
 IF TG_OP='DELETE' THEN RAISE EXCEPTION 'Credential history cannot be deleted' USING ERRCODE='23514'; END IF;
 IF TG_OP='INSERT' THEN
  IF NOT EXISTS(SELECT 1 FROM "Memberships" m WHERE m."TenantId"=NEW."TenantId" AND m."Id"=NEW."MembershipId" AND m."PersonId"=NEW."PersonId" AND m."ChapterId"=NEW."UnitId") THEN RAISE EXCEPTION 'Credential must reference its canonical membership/person/chapter' USING ERRCODE='23514';END IF;
 ELSE
  IF (to_jsonb(NEW)-ARRAY['Status','PdfId','FrontId','BackId','ApproverId','Reason']) IS DISTINCT FROM (to_jsonb(OLD)-ARRAY['Status','PdfId','FrontId','BackId','ApproverId','Reason']) THEN RAISE EXCEPTION 'Credential snapshots and identity are immutable' USING ERRCODE='23514';END IF;
  IF OLD."PdfId" IS NOT NULL AND (NEW."PdfId" IS DISTINCT FROM OLD."PdfId" OR NEW."FrontId" IS DISTINCT FROM OLD."FrontId" OR NEW."BackId" IS DISTINCT FROM OLD."BackId") THEN RAISE EXCEPTION 'Issued artifact references cannot be replaced' USING ERRCODE='23514';END IF;
  IF OLD."ApproverId" IS NOT NULL AND NEW."ApproverId" IS DISTINCT FROM OLD."ApproverId" THEN RAISE EXCEPTION 'Issuing authority is immutable after approval' USING ERRCODE='23514';END IF;
  IF OLD."Status" IN('Revoked','Replaced','Expired') AND NEW."Status" NOT IN('Revoked','Replaced','Expired') THEN RAISE EXCEPTION 'Invalid credential reactivation' USING ERRCODE='23514';END IF;
 END IF;
 IF NEW."Status"='Issued' AND (NEW."ApproverId" IS NULL OR NEW."AuthorId"=NEW."ApproverId" OR NEW."PdfId" IS NULL OR NEW."FrontId" IS NULL) THEN RAISE EXCEPTION 'Issue requires independent approval and official files' USING ERRCODE='23514';END IF;
 IF NEW."ExpiryDate"<NEW."IssueDate" THEN RAISE EXCEPTION 'Invalid credential dates' USING ERRCODE='23514';END IF;
 RETURN NEW;END $function$

```

### descendant_chapter_count

```sql
CREATE OR REPLACE FUNCTION public.descendant_chapter_count(tenant uuid, root uuid)
 RETURNS integer
 LANGUAGE sql
 STABLE
AS $function$
WITH RECURSIVE descendants AS (
 SELECT "Id","LevelId" FROM "Units" WHERE "TenantId"=tenant AND "ParentId"=root
 UNION ALL SELECT u."Id",u."LevelId" FROM "Units" u JOIN descendants d ON u."ParentId"=d."Id" WHERE u."TenantId"=tenant
) SELECT count(*)::integer FROM descendants d JOIN "Levels" l ON l."Id"=d."LevelId" AND l."TenantId"=tenant WHERE l."Kind"='Chapter';
$function$

```

### finance_currency_guard

```sql
CREATE OR REPLACE FUNCTION public.finance_currency_guard()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$
DECLARE curr text; base text; digits int; value jsonb; k text; doc jsonb;
BEGIN
 SELECT "Currency" INTO base FROM "FinancialEntity" WHERE "TenantId"=NEW."TenantId" AND "Id"=NEW."EntityId";
 doc=to_jsonb(NEW);curr=doc->>'Currency';
 IF curr IS NOT NULL AND curr<>base THEN RAISE EXCEPTION 'Operational financial documents must use entity base currency';END IF;
 digits=CASE base WHEN 'JPY' THEN 0 WHEN 'KWD' THEN 3 WHEN 'BHD' THEN 3 WHEN 'OMR' THEN 3 ELSE 2 END;
 FOREACH k IN ARRAY ARRAY['Amount','Total','Subtotal','Tax','Discount','Paid','Credited','Refunded','ChargedBack','Debit','Credit','Pledged','Settled','BaseAmount','PerMemberAmount','PerMember','LateFee','MemberPrice','GuestPrice'] LOOP
  value=doc->k;
  IF value IS NOT NULL AND value<>'null'::jsonb AND (value#>>'{}')::numeric<>round((value#>>'{}')::numeric,digits) THEN RAISE EXCEPTION 'Money violates currency minor-unit precision'; END IF;
 END LOOP;RETURN NEW;
END $function$

```

### finance_entity_guard

```sql
CREATE OR REPLACE FUNCTION public.finance_entity_guard()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$ BEGIN IF NEW."Currency" IS DISTINCT FROM OLD."Currency" THEN RAISE EXCEPTION 'Accounting entity base currency is immutable';END IF;RETURN NEW; END $function$

```

### finance_frozen_guard

```sql
CREATE OR REPLACE FUNCTION public.finance_frozen_guard()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$ BEGIN RAISE EXCEPTION 'Financial snapshot or provider evidence is immutable'; END $function$

```

### finance_invoice_guard

```sql
CREATE OR REPLACE FUNCTION public.finance_invoice_guard()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$
BEGIN
 IF OLD."Status"='Issued' AND ((to_jsonb(OLD)-ARRAY['Paid','Credited','DocumentId','Revision']) IS DISTINCT FROM (to_jsonb(NEW)-ARRAY['Paid','Credited','DocumentId','Revision'])) THEN RAISE EXCEPTION 'Issued financial document content is immutable';END IF;
 IF OLD."DocumentId" IS NOT NULL AND NEW."DocumentId" IS DISTINCT FROM OLD."DocumentId" THEN RAISE EXCEPTION 'Historical financial PDF cannot be replaced';END IF;RETURN NEW;
END $function$

```

### finance_journal_check

```sql
CREATE OR REPLACE FUNCTION public.finance_journal_check()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$
DECLARE jid uuid; t uuid; e uuid; posted boolean; dt date; period uuid; curr text; dr numeric; cr numeric; n bigint;
BEGIN
 IF TG_TABLE_NAME='JournalEntry' THEN jid=COALESCE(NEW."Id",OLD."Id"); ELSE jid=COALESCE(NEW."JournalId",OLD."JournalId"); END IF;
 SELECT "TenantId","EntityId","Posted","Date","PeriodId","Currency" INTO t,e,posted,dt,period,curr FROM "JournalEntry" WHERE "Id"=jid;
 IF posted THEN
  SELECT COALESCE(sum("Debit"),0),COALESCE(sum("Credit"),0),count(*) INTO dr,cr,n FROM "JournalLine" WHERE "JournalId"=jid AND "TenantId"=t AND "EntityId"=e;
  IF dr<>cr OR dr<=0 OR n<2 THEN RAISE EXCEPTION 'Posted journal must balance with positive totals and at least two lines'; END IF;
  IF TG_TABLE_NAME='JournalEntry' THEN
  IF NOT EXISTS(SELECT 1 FROM "AccountingPeriod" p WHERE p."Id"=period AND p."TenantId"=t AND p."EntityId"=e AND NOT p."Closed" AND p."StartsOn"<=dt AND p."EndsOn">=dt) THEN RAISE EXCEPTION 'Journal posting requires an open matching period'; END IF;
  END IF;
  IF NOT EXISTS(SELECT 1 FROM "FinancialEntity" f WHERE f."Id"=e AND f."TenantId"=t AND f."Currency"=curr) THEN RAISE EXCEPTION 'Journal currency differs from accounting entity'; END IF;
 END IF; RETURN NULL;
END $function$

```

### finance_period_guard

```sql
CREATE OR REPLACE FUNCTION public.finance_period_guard()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$
BEGIN
 PERFORM pg_advisory_xact_lock(hashtext(NEW."TenantId"::text));
 IF TG_OP='UPDATE' AND OLD."Closed" THEN RAISE EXCEPTION 'Closed periods cannot be silently reopened or changed'; END IF;
 IF NEW."EndsOn"<NEW."StartsOn" OR EXISTS(SELECT 1 FROM "AccountingPeriod" p WHERE p."TenantId"=NEW."TenantId" AND p."EntityId"=NEW."EntityId" AND p."Id"<>NEW."Id" AND p."StartsOn"<=NEW."EndsOn" AND p."EndsOn">=NEW."StartsOn") THEN RAISE EXCEPTION 'Accounting periods overlap or have invalid dates'; END IF;
 RETURN NEW;
END $function$

```

### finance_posted_guard

```sql
CREATE OR REPLACE FUNCTION public.finance_posted_guard()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$
BEGIN
 IF TG_TABLE_NAME='JournalEntry' THEN
  IF TG_OP<>'INSERT' AND OLD."Posted" THEN RAISE EXCEPTION 'Posted journals are immutable; use a source correction or reversal'; END IF;
 ELSE
  IF TG_OP='UPDATE' THEN
  IF OLD."InvoiceId" IS NULL AND NEW."InvoiceId" IS NOT NULL AND (to_jsonb(OLD)-'InvoiceId')=(to_jsonb(NEW)-'InvoiceId') AND EXISTS(
   SELECT 1 FROM "JournalEntry" j WHERE j."Id"=OLD."JournalId" AND (
    j."SourceKind"='Invoice' AND j."SourceId"=NEW."InvoiceId" OR
    j."SourceKind"='Credit' AND EXISTS(SELECT 1 FROM "FinanceInvoice" i WHERE i."Id"=j."SourceId" AND i."OriginalId"=NEW."InvoiceId") OR
    j."SourceKind"='Allocation' AND EXISTS(SELECT 1 FROM "PaymentAllocation" a WHERE a."Id"=j."SourceId" AND a."InvoiceId"=NEW."InvoiceId") OR
    j."SourceKind"='RefundAllocation' AND EXISTS(SELECT 1 FROM "PaymentAllocation" a WHERE a."InvoiceId"=NEW."InvoiceId" AND j."SourceKey" LIKE '%:'||a."Id"::text)
   )) THEN RETURN NEW; END IF;
  END IF;
  IF TG_OP<>'INSERT' AND EXISTS(SELECT 1 FROM "JournalEntry" WHERE "Id"=OLD."JournalId" AND "Posted") THEN RAISE EXCEPTION 'Posted journal lines are immutable'; END IF;
  IF TG_OP<>'DELETE' AND EXISTS(SELECT 1 FROM "JournalEntry" WHERE "Id"=NEW."JournalId" AND "Posted") THEN RAISE EXCEPTION 'Cannot append to a posted journal'; END IF;
 END IF;
 IF TG_OP='DELETE' THEN RETURN OLD; END IF;RETURN NEW;
END $function$

```

### finance_receipt_guard

```sql
CREATE OR REPLACE FUNCTION public.finance_receipt_guard()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$
BEGIN
 IF TG_OP='DELETE' THEN RAISE EXCEPTION 'Financial receipt is immutable';END IF;
 IF (to_jsonb(OLD)-'DocumentId') IS DISTINCT FROM (to_jsonb(NEW)-'DocumentId') OR OLD."DocumentId" IS NOT NULL AND OLD."DocumentId" IS DISTINCT FROM NEW."DocumentId" THEN RAISE EXCEPTION 'Financial receipt content is immutable'; END IF;RETURN NEW;
END $function$

```

### gin_extract_query_trgm

```sql
CREATE OR REPLACE FUNCTION public.gin_extract_query_trgm(text, internal, smallint, internal, internal, internal, internal)
 RETURNS internal
 LANGUAGE c
 IMMUTABLE PARALLEL SAFE STRICT
AS '$libdir/pg_trgm', $function$gin_extract_query_trgm$function$

```

### gin_extract_value_trgm

```sql
CREATE OR REPLACE FUNCTION public.gin_extract_value_trgm(text, internal)
 RETURNS internal
 LANGUAGE c
 IMMUTABLE PARALLEL SAFE STRICT
AS '$libdir/pg_trgm', $function$gin_extract_value_trgm$function$

```

### gin_trgm_consistent

```sql
CREATE OR REPLACE FUNCTION public.gin_trgm_consistent(internal, smallint, text, integer, internal, internal, internal, internal)
 RETURNS boolean
 LANGUAGE c
 IMMUTABLE PARALLEL SAFE STRICT
AS '$libdir/pg_trgm', $function$gin_trgm_consistent$function$

```

### gin_trgm_triconsistent

```sql
CREATE OR REPLACE FUNCTION public.gin_trgm_triconsistent(internal, smallint, text, integer, internal, internal, internal)
 RETURNS "char"
 LANGUAGE c
 IMMUTABLE PARALLEL SAFE STRICT
AS '$libdir/pg_trgm', $function$gin_trgm_triconsistent$function$

```

### gtrgm_compress

```sql
CREATE OR REPLACE FUNCTION public.gtrgm_compress(internal)
 RETURNS internal
 LANGUAGE c
 IMMUTABLE PARALLEL SAFE STRICT
AS '$libdir/pg_trgm', $function$gtrgm_compress$function$

```

### gtrgm_consistent

```sql
CREATE OR REPLACE FUNCTION public.gtrgm_consistent(internal, text, smallint, oid, internal)
 RETURNS boolean
 LANGUAGE c
 IMMUTABLE PARALLEL SAFE STRICT
AS '$libdir/pg_trgm', $function$gtrgm_consistent$function$

```

### gtrgm_decompress

```sql
CREATE OR REPLACE FUNCTION public.gtrgm_decompress(internal)
 RETURNS internal
 LANGUAGE c
 IMMUTABLE PARALLEL SAFE STRICT
AS '$libdir/pg_trgm', $function$gtrgm_decompress$function$

```

### gtrgm_distance

```sql
CREATE OR REPLACE FUNCTION public.gtrgm_distance(internal, text, smallint, oid, internal)
 RETURNS double precision
 LANGUAGE c
 IMMUTABLE PARALLEL SAFE STRICT
AS '$libdir/pg_trgm', $function$gtrgm_distance$function$

```

### gtrgm_in

```sql
CREATE OR REPLACE FUNCTION public.gtrgm_in(cstring)
 RETURNS gtrgm
 LANGUAGE c
 IMMUTABLE PARALLEL SAFE STRICT
AS '$libdir/pg_trgm', $function$gtrgm_in$function$

```

### gtrgm_options

```sql
CREATE OR REPLACE FUNCTION public.gtrgm_options(internal)
 RETURNS void
 LANGUAGE c
 IMMUTABLE PARALLEL SAFE
AS '$libdir/pg_trgm', $function$gtrgm_options$function$

```

### gtrgm_out

```sql
CREATE OR REPLACE FUNCTION public.gtrgm_out(gtrgm)
 RETURNS cstring
 LANGUAGE c
 IMMUTABLE PARALLEL SAFE STRICT
AS '$libdir/pg_trgm', $function$gtrgm_out$function$

```

### gtrgm_penalty

```sql
CREATE OR REPLACE FUNCTION public.gtrgm_penalty(internal, internal, internal)
 RETURNS internal
 LANGUAGE c
 IMMUTABLE PARALLEL SAFE STRICT
AS '$libdir/pg_trgm', $function$gtrgm_penalty$function$

```

### gtrgm_picksplit

```sql
CREATE OR REPLACE FUNCTION public.gtrgm_picksplit(internal, internal)
 RETURNS internal
 LANGUAGE c
 IMMUTABLE PARALLEL SAFE STRICT
AS '$libdir/pg_trgm', $function$gtrgm_picksplit$function$

```

### gtrgm_same

```sql
CREATE OR REPLACE FUNCTION public.gtrgm_same(gtrgm, gtrgm, internal)
 RETURNS internal
 LANGUAGE c
 IMMUTABLE PARALLEL SAFE STRICT
AS '$libdir/pg_trgm', $function$gtrgm_same$function$

```

### gtrgm_union

```sql
CREATE OR REPLACE FUNCTION public.gtrgm_union(internal, internal)
 RETURNS gtrgm
 LANGUAGE c
 IMMUTABLE PARALLEL SAFE STRICT
AS '$libdir/pg_trgm', $function$gtrgm_union$function$

```

### guard_communication_message

```sql
CREATE OR REPLACE FUNCTION public.guard_communication_message()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$ BEGIN
IF NEW."Internal" AND NOT EXISTS(SELECT 1 FROM "Conversation" c WHERE c."Id"=NEW."ConversationId" AND c."TenantId"=NEW."TenantId" AND c."Kind"='Team') THEN RAISE EXCEPTION 'Internal notes require an explicitly shared team inbox'; END IF;
IF NEW."ReplyTo" IS NOT NULL AND NOT EXISTS(SELECT 1 FROM "CommunicationMessage" m WHERE m."Id"=NEW."ReplyTo" AND m."TenantId"=NEW."TenantId" AND m."ConversationId"=NEW."ConversationId") THEN RAISE EXCEPTION 'Reply conversation mismatch'; END IF;
RETURN NEW; END $function$

```

### guard_communication_outbox

```sql
CREATE OR REPLACE FUNCTION public.guard_communication_outbox()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$ BEGIN
IF NEW."AccountId" IS NOT NULL AND NOT EXISTS(SELECT 1 FROM "CommunicationAccount" a WHERE a."Id"=NEW."AccountId" AND a."TenantId"=NEW."TenantId") THEN RAISE EXCEPTION 'Account tenant mismatch'; END IF;
IF NEW."CampaignId" IS NOT NULL AND NOT EXISTS(SELECT 1 FROM "CommunicationCampaign" c WHERE c."Id"=NEW."CampaignId" AND c."TenantId"=NEW."TenantId") THEN RAISE EXCEPTION 'Campaign tenant mismatch'; END IF;
RETURN NEW; END $function$

```

### guard_community_audience

```sql
CREATE OR REPLACE FUNCTION public.guard_community_audience()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$ BEGIN
 IF EXISTS(SELECT 1 FROM unnest(NEW."Audience") a WHERE NOT EXISTS(SELECT 1 FROM "Units" u WHERE u."Id"=a AND u."TenantId"=NEW."TenantId")) THEN RAISE EXCEPTION 'Audience organization is outside the society'; END IF;
 RETURN NEW; END $function$

```

### guard_event_reference

```sql
CREATE OR REPLACE FUNCTION public.guard_event_reference()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$ BEGIN
IF NOT EXISTS(SELECT 1 FROM "EventRegistration" r WHERE r."Id"=NEW."RegistrationId" AND r."TenantId"=NEW."TenantId" AND r."EventId"=NEW."EventId") THEN RAISE EXCEPTION 'Registration event mismatch'; END IF; RETURN NEW; END $function$

```

### guard_officer_term

```sql
CREATE OR REPLACE FUNCTION public.guard_officer_term()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$
BEGIN
PERFORM pg_advisory_xact_lock(hashtext(NEW."TenantId"::text));
IF NEW."Status" IN ('Pending','Approved') AND EXISTS(SELECT 1 FROM "OfficerPositions" p WHERE p."Id"=NEW."PositionId" AND NOT p."AllowConcurrent") AND EXISTS(SELECT 1 FROM "OfficerAppointments" a WHERE a."TenantId"=NEW."TenantId" AND a."Id"<>NEW."Id" AND a."UnitId" IS NOT DISTINCT FROM NEW."UnitId" AND a."PositionId"=NEW."PositionId" AND a."Status" IN ('Pending','Approved') AND a."StartsAt"<NEW."EndsAt" AND a."EndsAt">NEW."StartsAt") THEN RAISE EXCEPTION 'Overlapping officer term'; END IF;
RETURN NEW; END $function$

```

### guard_unit_hierarchy

```sql
CREATE OR REPLACE FUNCTION public.guard_unit_hierarchy()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$
DECLARE parents uuid[]; root_allowed boolean; parent_level uuid;
BEGIN
 PERFORM pg_advisory_xact_lock(hashtext(NEW."TenantId"::text));
 SELECT "AllowedParents", "AllowRoot" INTO parents,root_allowed FROM "Levels" WHERE "TenantId"=NEW."TenantId" AND "Id"=NEW."LevelId";
 IF NEW."ParentId" IS NULL AND NOT root_allowed THEN RAISE EXCEPTION 'Parent required'; END IF;
 IF NEW."ParentId" IS NOT NULL THEN
  SELECT "LevelId" INTO parent_level FROM "Units" WHERE "TenantId"=NEW."TenantId" AND "Id"=NEW."ParentId";
  IF parent_level IS NULL OR NOT(parent_level=ANY(parents)) THEN RAISE EXCEPTION 'Parent level is not permitted'; END IF;
  IF EXISTS(WITH RECURSIVE ancestors AS (SELECT "Id","ParentId" FROM "Units" WHERE "TenantId"=NEW."TenantId" AND "Id"=NEW."ParentId" UNION SELECT u."Id",u."ParentId" FROM "Units" u JOIN ancestors a ON u."Id"=a."ParentId" WHERE u."TenantId"=NEW."TenantId") SELECT 1 FROM ancestors WHERE "Id"=NEW."Id") THEN RAISE EXCEPTION 'Hierarchy cycle'; END IF;
 END IF;
 IF TG_OP='UPDATE' AND NEW."TenantId"<>OLD."TenantId" THEN RAISE EXCEPTION 'Tenant is immutable'; END IF;
 IF TG_OP='UPDATE' AND NEW."LevelId"<>OLD."LevelId" AND EXISTS(SELECT 1 FROM "Units" u JOIN "Levels" l ON l."Id"=u."LevelId" AND l."TenantId"=u."TenantId" WHERE u."ParentId"=NEW."Id" AND NOT(NEW."LevelId"=ANY(l."AllowedParents"))) THEN RAISE EXCEPTION 'Children do not permit this parent level'; END IF;
 RETURN NEW;
END $function$

```

### membership_plan_quota

```sql
CREATE OR REPLACE FUNCTION public.membership_plan_quota()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$ DECLARE tenant uuid; quota int; added int; removed int; BEGIN
 FOR tenant IN SELECT DISTINCT "TenantId" FROM membership_new WHERE "Status"='Active' LOOP
 SELECT "MembershipLimit" INTO quota FROM "TenantSubscription" WHERE "TenantId"=tenant;
 IF quota IS NULL THEN CONTINUE;END IF;
 SELECT count(*) INTO added FROM membership_new WHERE "TenantId"=tenant AND "Status"='Active';removed:=0;
 IF TG_OP='UPDATE' THEN SELECT count(*) INTO removed FROM membership_old WHERE "TenantId"=tenant AND "Status"='Active';END IF;
 IF added<=removed THEN CONTINUE;END IF;
 PERFORM pg_advisory_xact_lock(hashtextextended(tenant::text,626));
 IF (SELECT count(*) FROM "Memberships" WHERE "TenantId"=tenant AND "Status"='Active')>quota THEN RAISE EXCEPTION 'Active-status membership plan allowance exceeded' USING ERRCODE='23514';END IF;
 END LOOP;RETURN NULL;END $function$

```

### prevent_history_mutation

```sql
CREATE OR REPLACE FUNCTION public.prevent_history_mutation()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$ BEGIN RAISE EXCEPTION 'Historical records are append only'; END $function$

```

### set_limit

```sql
CREATE OR REPLACE FUNCTION public.set_limit(real)
 RETURNS real
 LANGUAGE c
 STRICT
AS '$libdir/pg_trgm', $function$set_limit$function$

```

### show_limit

```sql
CREATE OR REPLACE FUNCTION public.show_limit()
 RETURNS real
 LANGUAGE c
 STABLE PARALLEL SAFE STRICT
AS '$libdir/pg_trgm', $function$show_limit$function$

```

### show_trgm

```sql
CREATE OR REPLACE FUNCTION public.show_trgm(text)
 RETURNS text[]
 LANGUAGE c
 IMMUTABLE PARALLEL SAFE STRICT
AS '$libdir/pg_trgm', $function$show_trgm$function$

```

### similarity

```sql
CREATE OR REPLACE FUNCTION public.similarity(text, text)
 RETURNS real
 LANGUAGE c
 IMMUTABLE PARALLEL SAFE STRICT
AS '$libdir/pg_trgm', $function$similarity$function$

```

### similarity_dist

```sql
CREATE OR REPLACE FUNCTION public.similarity_dist(text, text)
 RETURNS real
 LANGUAGE c
 IMMUTABLE PARALLEL SAFE STRICT
AS '$libdir/pg_trgm', $function$similarity_dist$function$

```

### similarity_op

```sql
CREATE OR REPLACE FUNCTION public.similarity_op(text, text)
 RETURNS boolean
 LANGUAGE c
 STABLE PARALLEL SAFE STRICT
AS '$libdir/pg_trgm', $function$similarity_op$function$

```

### strict_word_similarity

```sql
CREATE OR REPLACE FUNCTION public.strict_word_similarity(text, text)
 RETURNS real
 LANGUAGE c
 IMMUTABLE PARALLEL SAFE STRICT
AS '$libdir/pg_trgm', $function$strict_word_similarity$function$

```

### strict_word_similarity_commutator_op

```sql
CREATE OR REPLACE FUNCTION public.strict_word_similarity_commutator_op(text, text)
 RETURNS boolean
 LANGUAGE c
 STABLE PARALLEL SAFE STRICT
AS '$libdir/pg_trgm', $function$strict_word_similarity_commutator_op$function$

```

### strict_word_similarity_dist_commutator_op

```sql
CREATE OR REPLACE FUNCTION public.strict_word_similarity_dist_commutator_op(text, text)
 RETURNS real
 LANGUAGE c
 IMMUTABLE PARALLEL SAFE STRICT
AS '$libdir/pg_trgm', $function$strict_word_similarity_dist_commutator_op$function$

```

### strict_word_similarity_dist_op

```sql
CREATE OR REPLACE FUNCTION public.strict_word_similarity_dist_op(text, text)
 RETURNS real
 LANGUAGE c
 IMMUTABLE PARALLEL SAFE STRICT
AS '$libdir/pg_trgm', $function$strict_word_similarity_dist_op$function$

```

### strict_word_similarity_op

```sql
CREATE OR REPLACE FUNCTION public.strict_word_similarity_op(text, text)
 RETURNS boolean
 LANGUAGE c
 STABLE PARALLEL SAFE STRICT
AS '$libdir/pg_trgm', $function$strict_word_similarity_op$function$

```

### studio_immutable

```sql
CREATE OR REPLACE FUNCTION public.studio_immutable()
 RETURNS trigger
 LANGUAGE plpgsql
AS $function$ BEGIN RAISE EXCEPTION 'Published design versions and immutable assets cannot be edited or removed' USING ERRCODE='23514'; END $function$

```

### unit_within

```sql
CREATE OR REPLACE FUNCTION public.unit_within(tenant uuid, unit uuid, ancestor uuid)
 RETURNS boolean
 LANGUAGE sql
 STABLE
AS $function$
 WITH RECURSIVE chain AS (
 SELECT u."Id",u."ParentId",ARRAY[u."Id"] path FROM "Units" u WHERE u."TenantId"=$1 AND u."Id"=$2
 UNION ALL SELECT p."Id",p."ParentId",c.path||p."Id" FROM "Units" p JOIN chain c ON c."ParentId"=p."Id" WHERE p."TenantId"=$1 AND NOT p."Id"=ANY(c.path)
 ) SELECT EXISTS(SELECT 1 FROM chain WHERE "Id"=$3)
$function$

```

### word_similarity

```sql
CREATE OR REPLACE FUNCTION public.word_similarity(text, text)
 RETURNS real
 LANGUAGE c
 IMMUTABLE PARALLEL SAFE STRICT
AS '$libdir/pg_trgm', $function$word_similarity$function$

```

### word_similarity_commutator_op

```sql
CREATE OR REPLACE FUNCTION public.word_similarity_commutator_op(text, text)
 RETURNS boolean
 LANGUAGE c
 STABLE PARALLEL SAFE STRICT
AS '$libdir/pg_trgm', $function$word_similarity_commutator_op$function$

```

### word_similarity_dist_commutator_op

```sql
CREATE OR REPLACE FUNCTION public.word_similarity_dist_commutator_op(text, text)
 RETURNS real
 LANGUAGE c
 IMMUTABLE PARALLEL SAFE STRICT
AS '$libdir/pg_trgm', $function$word_similarity_dist_commutator_op$function$

```

### word_similarity_dist_op

```sql
CREATE OR REPLACE FUNCTION public.word_similarity_dist_op(text, text)
 RETURNS real
 LANGUAGE c
 IMMUTABLE PARALLEL SAFE STRICT
AS '$libdir/pg_trgm', $function$word_similarity_dist_op$function$

```

### word_similarity_op

```sql
CREATE OR REPLACE FUNCTION public.word_similarity_op(text, text)
 RETURNS boolean
 LANGUAGE c
 STABLE PARALLEL SAFE STRICT
AS '$libdir/pg_trgm', $function$word_similarity_op$function$

```
