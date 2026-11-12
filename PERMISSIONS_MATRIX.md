# Permissions matrix

Reviewed 12 November 2026. Server policy, current scoped Assignments and effective Memberships are authoritative; UI navigation is not a security boundary. Permission catalog: backend/Security.cs and backend/StudioService.cs. Defaults: backend/RoleTemplates.cs and migration upgrades. Custom roles may differ.

## Scope rules

- Platform administrator: platform tenant/plan/status/aggregate health controls; no implicit private society membership or document access.
- Root society administrator: all declared society permissions, usually a society-wide assignment. High-risk production actions also require recent MFA.
- President, Secretary, Governor, Coordinator and Manager defaults: studio read/design/issue/approve; service read/manage/approve/coordinate/verify; intelligence read, report export and assistant use. Scope remains the assigned unit and descendants. These defaults do not include studio publish/bulk/settings or branding management.
- Treasurer defaults additionally receive Phase 6 studio/service/intelligence read and assistant use; their financial rights follow the existing finance role template.
- Member: own permitted issued files, own achievements/volunteer profile/applications and own assistant answers; does not gain staff search, issuance, bulk exports or society-wide analytics.

## Phase 6 action mapping

- studio.read: staff gallery/register/detail and private issued artifacts in current scope. Published society/ancestor standards are discoverable by scoped readers; their unpublished draft content is hidden.
- studio.design: author/update owned-scope drafts, private design assets. Local customization also requires society policy.
- studio.publish: publish immutable versions/archive in owning scope. Issuance never renders an unpublished draft.
- studio.issue: search/select canonical members, preview and request individual credentials, act on holder requests.
- studio.approve: independently approve/reject issuance. Author cannot approve their own request; worker rechecks scope.
- studio.bulk: durable bulk issuance/export, current authorization for every selected credential, job access and private downloads. File link remains bound to requester.
- studio.settings: society issuance/download/public-name policy.
- service.read: project/volunteer/impact staff views in scope.
- service.manage: canonical project proposal/update/tasks/metrics and evidence, subject to scope of participating chapters.
- service.approve: independent project approval; author cannot approve own project.
- service.coordinate: volunteer assignment, shifts, actual attendance operations.
- service.verify: independent hours/outcome review; recorder and member cannot verify their own hours.
- intelligence.read: authorized aggregate projections; financial sections additionally require finance.read.
- reports.export: private reports only with the report family's domain permission. finance reports reuse finance.export/source rules.
- reports.schedule: saved schedules, current-author revalidation on every execution.
- assistant.use: staff scoped evidence; members only their own supported evidence. External mode additionally needs plan entitlement, configured server credentials and explicit processing consent.
- branding.manage: society branding/domain configuration; high-risk production MFA applies.

## Existing permissions and separation

Organization and membership operations retain units.*, members.*, roles.manage, settings.manage and their existing reviewed scope checks. Community, events and communications retain their existing per-action policies and independent approval/consent checks. Finance uses finance.read/configure/invoice/collect/approve/dues/refund/reconcile/export/providers/periods/highvalue and legal-entity scope. Existing Phase 1–5 regressions are rerun by tests/studio_integration.py.

High-risk production policy covers role/settings changes, credential issue/approve/bulk/publish/policy, branding, sensitive finance actions and report schedules. The MFA freshness window is 30 minutes. Development test fixtures do not prove the full production HTTPS challenge flow; that remains a release gate.

## No privilege shortcuts

Tenant identifiers, member IDs, template versions, job IDs, document IDs and signed download URLs are references, never authorization. Permissions are rechecked at API execution, queue execution and download. Revoked/expired assignments lose future authority. Expiring artifacts do not grant access outside the user's current scope. Host routing and plan features are additional restrictions, not grants.
