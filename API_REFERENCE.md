# API reference

Generated from the running Phase 6 application OpenAPI on 12 November 2026. Full request/response schemas are in `backend/openapi.json`.

Base path `/api/v1`. Cookie authentication; obtain `GET /api/v1/auth/csrf` and supply `X-CSRF-TOKEN` for writes. Webhooks use their provider signature validation instead. Scope enforcement is server-side for every tenant route. A role name alone does not imply society-wide authority.

Errors: 400 validation/domain or CSRF failure; 401 no authenticated session; 403 current authority/tenant/feature denial; 409 revision/integrity conflict; 429 throttled. Refresh after 409. Preserve stable request keys for retried durable operations.

## Phase 6 workflows

1. Create a typed template, save its draft, publish an immutable version. Official generation uses a published version and independently approved request.
2. Bulk issuance persists up to 100,000 authorized membership IDs. Approve as a different authorized officer, then inspect job progress and per-record errors. Retry failures as the original requester.
3. POST studio/exports with `ids`, `layout`, `key`, or `ids: []`, `sourceJobId`, `offset`, `limit` (1–2,000). Completed exports produce a user-bound five-minute link while files remain eligible for 24 hours. Current permissions are rechecked.
4. Propose a service project with canonical participating chapters. Independently approve, assign volunteers, record actual shift times and independently verify hours/outcomes.
5. Query unified reports with domain read/export authority. Scheduled jobs retain the current author identity and produce private files.
6. Assistant uses scoped, read-only evidence. External mode additionally requires explicit processing consent, configured provider/model and plan entitlement. No external call was made during local verification.

## Routes

### GET /health

### GET /health/live

### GET /health/ready

### GET /api/v1/auth/csrf

### POST /api/v1/auth/two-factor

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/branding/resolve

Parameters: `society` (query optional).

### GET /api/v1/branding/assets/{tenant}/{id}

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/studio/fonts/{face}

Parameters: `face` (path required).

### GET /api/v1/credentials/verify/{token}

Parameters: `token` (path required).

### POST /api/v1/finance/webhooks/{id}

Parameters: `id` (path required).

### GET /api/v1/communications/webhooks/{id}

Parameters: `id` (path required).

### POST /api/v1/communications/webhooks/{id}

Parameters: `id` (path required).

### POST /api/v1/communications/unsubscribe

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/membership/catalog

Parameters: `tenant` (query optional), `search` (query optional), `page` (query optional).

### GET /api/v1/membership/applications

### POST /api/v1/membership/applications

Request body: application/json. See OpenAPI schema for field types.

### PUT /api/v1/membership/applications/{id}

Parameters: `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/membership/applications/{id}/withdraw

Parameters: `id` (path required).

### GET /api/v1/membership/me

### POST /api/v1/membership/invitations/accept

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/assets

Parameters: `tenant` (path required), `unit` (query optional).

### GET /api/v1/societies/{tenant}/assets/{id}

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/users/me

### PUT /api/v1/users/me

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/users/me/password

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/applications

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/applications

Parameters: `page` (query optional).

### POST /api/v1/societies/applications/{id}/review

Parameters: `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/workspaces

### GET /api/v1/platform/societies

Parameters: `page` (query optional).

### POST /api/v1/platform/societies/{id}/status

Parameters: `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/invitations

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/invitations/accept

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/security/status

### POST /api/v1/security/mfa/setup

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/security/mfa/enable

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/security/mfa/disable

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/branding

Parameters: `tenant` (path required).

### PUT /api/v1/societies/{tenant}/branding

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/branding/domains

Parameters: `tenant` (path required).

### POST /api/v1/societies/{tenant}/branding/domains

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/branding/domains/{id}/verify

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/branding/readiness

Parameters: `tenant` (path required).

### GET /api/v1/platform/tenants

Parameters: `search` (query optional), `page` (query optional).

### POST /api/v1/platform/tenants/{tenant}/status

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### PUT /api/v1/platform/tenants/{tenant}/subscription

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/platform/tenants/{tenant}/storage

Parameters: `tenant` (path required).

### GET /api/v1/platform/audit

Parameters: `page` (query optional).

### GET /api/v1/platform/health

### GET /api/v1/societies/{tenant}/intelligence/summary

Parameters: `tenant` (path required), `workspace` (query optional), `from` (query optional), `to` (query optional).

### GET /api/v1/societies/{tenant}/intelligence/setup

Parameters: `tenant` (path required).

### POST /api/v1/societies/{tenant}/intelligence/reports/query

Parameters: `tenant` (path required), `page` (query optional).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/intelligence/reports/export

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/intelligence/reports/saved

Parameters: `tenant` (path required).

### POST /api/v1/societies/{tenant}/intelligence/reports/saved

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/intelligence/reports/saved/{id}/schedule

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/intelligence/reports/schedules

Parameters: `tenant` (path required).

### POST /api/v1/societies/{tenant}/intelligence/reports/schedules/{id}/toggle

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/intelligence/reports/jobs

Parameters: `tenant` (path required).

### GET /api/v1/societies/{tenant}/intelligence/reports/jobs/{id}/download

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/intelligence/assistant/status

### POST /api/v1/societies/{tenant}/intelligence/assistant/ask

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/service/setup

Parameters: `tenant` (path required).

### GET /api/v1/societies/{tenant}/service/members

Parameters: `tenant` (path required), `search` (query optional).

### POST /api/v1/societies/{tenant}/service/achievements/certificates/{id}/share

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/service/achievements

Parameters: `tenant` (path required), `membership` (query optional).

### GET /api/v1/societies/{tenant}/service/projects

Parameters: `tenant` (path required), `workspace` (query optional), `status` (query optional), `search` (query optional), `page` (query optional).

### POST /api/v1/societies/{tenant}/service/projects

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/service/projects/{id}

Parameters: `tenant` (path required), `id` (path required).

### PUT /api/v1/societies/{tenant}/service/projects/{id}

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/service/projects/{id}/decision

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/service/projects/{id}/complete

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/service/projects/{id}/tasks

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### PUT /api/v1/societies/{tenant}/service/tasks/{id}

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/service/volunteers

Parameters: `tenant` (path required), `workspace` (query optional).

### PUT /api/v1/societies/{tenant}/service/volunteers/{membership}

Parameters: `tenant` (path required), `membership` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/service/projects/{id}/volunteers

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/service/assignments/{id}/decision

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/service/projects/{id}/shifts

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/service/shifts/{id}/attendance

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/service/hours

Parameters: `tenant` (path required), `workspace` (query optional).

### POST /api/v1/societies/{tenant}/service/hours/{id}/decision

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### PUT /api/v1/societies/{tenant}/service/hours/{id}/correction

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/service/metrics

Parameters: `tenant` (path required).

### POST /api/v1/societies/{tenant}/service/metrics

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/service/projects/{id}/impact

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/service/impact/{id}/decision

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### PUT /api/v1/societies/{tenant}/service/impact/{id}/correction

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/service/impact

Parameters: `tenant` (path required), `workspace` (query optional).

### POST /api/v1/societies/{tenant}/service/projects/{id}/documents

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/service/projects/{id}/documents/{file}

Parameters: `tenant` (path required), `id` (path required), `file` (path required).

### GET /api/v1/societies/{tenant}/studio/setup

Parameters: `tenant` (path required).

### PUT /api/v1/societies/{tenant}/studio/policy

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/studio/templates

Parameters: `tenant` (path required), `kind` (query optional), `workspace` (query optional).

### POST /api/v1/societies/{tenant}/studio/templates

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/studio/templates/{id}

Parameters: `tenant` (path required), `id` (path required).

### PUT /api/v1/societies/{tenant}/studio/templates/{id}

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/studio/starter

Parameters: `kind` (query required).

### GET /api/v1/societies/{tenant}/studio/templates/{id}/thumbnail

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/studio/templates/{id}/publish

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/studio/templates/{id}/archive

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/studio/assets

Parameters: `tenant` (path required), `unit` (query optional).

### GET /api/v1/societies/{tenant}/studio/assets

Parameters: `tenant` (path required).

### GET /api/v1/societies/{tenant}/studio/assets/{id}

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/studio/members

Parameters: `tenant` (path required), `search` (query optional).

### POST /api/v1/societies/{tenant}/studio/selection

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/studio/preview

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/studio/credentials

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/studio/credentials

Parameters: `tenant` (path required), `kind` (query optional), `status` (query optional), `page` (query optional), `workspace` (query optional), `mine` (query optional).

### GET /api/v1/societies/{tenant}/studio/credentials/{id}

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/studio/credentials/{id}/decision

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/studio/credentials/{id}/revoke

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/studio/credentials/{id}/file

Parameters: `tenant` (path required), `id` (path required), `side` (query optional), `format` (query optional).

### POST /api/v1/societies/{tenant}/studio/credentials/{id}/requests

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/studio/requests

Parameters: `tenant` (path required).

### POST /api/v1/societies/{tenant}/studio/jobs

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/studio/jobs

Parameters: `tenant` (path required).

### GET /api/v1/societies/{tenant}/studio/jobs/{id}

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/studio/jobs/{id}/decision

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/studio/jobs/{id}/retry

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/studio/exports

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/studio/jobs/{id}/download-link

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/studio/downloads/{token}

Parameters: `tenant` (path required), `token` (path required).

### GET /api/v1/societies/{tenant}/finance/setup

Parameters: `tenant` (path required).

### GET /api/v1/societies/{tenant}/finance/entities

Parameters: `tenant` (path required).

### POST /api/v1/societies/{tenant}/finance/entities

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### PUT /api/v1/societies/{tenant}/finance/entities/{id}

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/finance/accounts

Parameters: `tenant` (path required), `entity` (query required).

### POST /api/v1/societies/{tenant}/finance/accounts

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/finance/periods

Parameters: `tenant` (path required), `entity` (query required).

### POST /api/v1/societies/{tenant}/finance/periods

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/finance/periods/{id}/close

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/finance/dues-plans

Parameters: `tenant` (path required), `entity` (query optional), `workspace` (query optional).

### POST /api/v1/societies/{tenant}/finance/dues-plans

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### PUT /api/v1/societies/{tenant}/finance/dues-plans/{id}

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/finance/exemptions

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/finance/exemptions

Parameters: `tenant` (path required), `entity` (query optional).

### POST /api/v1/societies/{tenant}/finance/exemptions/{id}/decision

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/finance/assessments

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/finance/jobs

Parameters: `tenant` (path required), `entity` (query optional).

### GET /api/v1/societies/{tenant}/finance/invoices

Parameters: `tenant` (path required), `entity` (query optional), `workspace` (query optional), `member` (query optional), `chapter` (query optional), `status` (query optional), `kind` (query optional), `search` (query optional), `page` (query optional).

### POST /api/v1/societies/{tenant}/finance/invoices

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/finance/invoices/{id}

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/finance/invoices/{id}/submit

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/finance/invoices/{id}/issue

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/finance/invoices/{id}/decision

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/finance/invoices/{id}/cancel

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/finance/invoices/{id}/late-fee

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/finance/invoices/{id}/credit

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/finance/payments

Parameters: `tenant` (path required), `entity` (query optional), `workspace` (query optional), `status` (query optional), `page` (query optional).

### POST /api/v1/societies/{tenant}/finance/payments

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/finance/payments/{id}/decision

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/finance/payments/{id}/allocate

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/finance/receipts

Parameters: `tenant` (path required), `entity` (query optional), `workspace` (query optional), `page` (query optional).

### POST /api/v1/societies/{tenant}/finance/payments/{id}/refunds

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/finance/journals

Parameters: `tenant` (path required), `entity` (query optional), `workspace` (query optional), `from` (query optional), `to` (query optional), `page` (query optional).

### POST /api/v1/societies/{tenant}/finance/journals

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/finance/journals/{id}

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/finance/journals/requests/{id}/decision

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/finance/journals/{id}/reverse

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/finance/inter-entity

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/finance/inter-entity

Parameters: `tenant` (path required), `entity` (query optional).

### POST /api/v1/societies/{tenant}/finance/inter-entity/{id}/decision

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/finance/inter-entity/{id}/settlements

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/finance/settlements

Parameters: `tenant` (path required), `entity` (query optional).

### POST /api/v1/societies/{tenant}/finance/settlements/{id}/decision

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/finance/exchange-rates

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/finance/exchange-rates

Parameters: `tenant` (path required), `entity` (query required).

### GET /api/v1/societies/{tenant}/finance/budgets

Parameters: `tenant` (path required), `entity` (query optional), `workspace` (query optional).

### POST /api/v1/societies/{tenant}/finance/budgets

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/finance/budgets/{id}/revision

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/finance/budgets/{id}/history

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/finance/budgets/{id}/decision

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/finance/expenses/{id}

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/finance/expenses

Parameters: `tenant` (path required), `entity` (query optional), `workspace` (query optional), `status` (query optional), `page` (query optional).

### POST /api/v1/societies/{tenant}/finance/expenses

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/finance/expenses/{id}/decision

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/finance/expenses/{id}/pay

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/finance/banks

Parameters: `tenant` (path required), `entity` (query optional).

### POST /api/v1/societies/{tenant}/finance/banks

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/finance/bank-transactions

Parameters: `tenant` (path required), `entity` (query optional), `bank` (query optional), `status` (query optional), `page` (query optional).

### POST /api/v1/societies/{tenant}/finance/banks/{id}/import

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/finance/bank-transactions/{id}/match

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/finance/donors

Parameters: `tenant` (path required), `entity` (query optional).

### POST /api/v1/societies/{tenant}/finance/donors

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/finance/contributions

Parameters: `tenant` (path required), `entity` (query optional), `kind` (query optional).

### POST /api/v1/societies/{tenant}/finance/contributions

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/finance/contributions/{id}/invoice

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/finance/expenses/{id}/attachments

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/finance/files/{id}

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/finance/conversions

Parameters: `tenant` (path required), `entity` (query required), `target` (query required), `on` (query required), `workspace` (query optional).

### GET /api/v1/societies/{tenant}/finance/dashboard

Parameters: `tenant` (path required), `entity` (query optional), `workspace` (query optional), `from` (query optional), `to` (query optional).

### GET /api/v1/societies/{tenant}/finance/reports/catalog

Parameters: `tenant` (path required).

### GET /api/v1/societies/{tenant}/finance/reports

Parameters: `tenant` (path required), `report` (query required), `entity` (query optional), `workspace` (query optional), `member` (query optional), `chapter` (query optional), `eventId` (query optional), `from` (query optional), `to` (query optional), `page` (query optional).

### GET /api/v1/societies/{tenant}/finance/statements/member/{id}

Parameters: `tenant` (path required), `id` (path required), `entity` (query optional), `from` (query optional), `to` (query optional).

### GET /api/v1/societies/{tenant}/finance/statements/chapter/{id}

Parameters: `tenant` (path required), `id` (path required), `entity` (query optional), `from` (query optional), `to` (query optional).

### GET /api/v1/societies/{tenant}/finance/ledger

Parameters: `tenant` (path required), `entity` (query required), `workspace` (query optional), `account` (query optional), `from` (query optional), `to` (query optional), `beforeId` (query optional), `before` (query optional), `page` (query optional).

### GET /api/v1/societies/{tenant}/finance/providers

Parameters: `tenant` (path required), `entity` (query optional).

### POST /api/v1/societies/{tenant}/finance/providers

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/finance/providers/{id}/verify

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/finance/providers/{id}/disable

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/finance/invoices/{id}/payment-intents

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/finance/invoices/{id}/checkout-options

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/finance/refunds

Parameters: `tenant` (path required), `entity` (query optional).

### POST /api/v1/societies/{tenant}/finance/refunds/{id}/decision

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/finance/refunds/{id}/dispatch

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/finance/invoices/{id}/document

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/finance/receipts/{id}/document

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/finance/invoices/{id}/pdf

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/finance/receipts/{id}/pdf

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/finance/reports/export

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/finance/jobs/{id}/download

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/finance/my/summary

Parameters: `tenant` (path required).

### GET /api/v1/societies/{tenant}/finance/my/invoices

Parameters: `tenant` (path required), `page` (query optional).

### GET /api/v1/societies/{tenant}/finance/my/payments

Parameters: `tenant` (path required), `page` (query optional).

### GET /api/v1/societies/{tenant}/finance/my/receipts

Parameters: `tenant` (path required).

### GET /api/v1/societies/{tenant}/finance/my/statements/{member}/export

Parameters: `tenant` (path required), `member` (path required), `format` (query required), `entity` (query optional), `from` (query optional), `to` (query optional).

### GET /api/v1/societies/{tenant}/finance/event-policies

Parameters: `tenant` (path required), `entity` (query optional).

### POST /api/v1/societies/{tenant}/finance/event-policies

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/finance/event-policies/{id}/decision

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/finance/events/{id}/pricing

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/finance/events/{id}/registration-invoice

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/finance/reminders

Parameters: `tenant` (path required), `entity` (query optional).

### POST /api/v1/societies/{tenant}/finance/reminders

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/finance/reminders/{id}/toggle

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/finance/reminders/{id}/runs

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/finance/invoices/{id}/send

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/communications/setup

Parameters: `tenant` (path required).

### GET /api/v1/societies/{tenant}/communications/team

Parameters: `tenant` (path required), `unit` (query optional).

### GET /api/v1/societies/{tenant}/communications/preferences

Parameters: `tenant` (path required).

### PUT /api/v1/societies/{tenant}/communications/preferences

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### PUT /api/v1/societies/{tenant}/communications/policy

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/communications/inbox

Parameters: `tenant` (path required), `workspace` (query optional), `channel` (query optional), `status` (query optional), `filter` (query optional), `search` (query optional), `before` (query optional), `beforeId` (query optional).

### POST /api/v1/societies/{tenant}/communications/conversations

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/communications/conversations/{id}

Parameters: `tenant` (path required), `id` (path required).

### PUT /api/v1/societies/{tenant}/communications/conversations/{id}

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/communications/conversations/{id}/messages

Parameters: `tenant` (path required), `id` (path required), `before` (query optional), `beforeId` (query optional), `search` (query optional).

### POST /api/v1/societies/{tenant}/communications/conversations/{id}/messages

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/communications/conversations/{id}/read

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/communications/conversations/{id}/mute

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/communications/groups/{id}/members

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### DELETE /api/v1/societies/{tenant}/communications/groups/{id}/members/{user}

Parameters: `tenant` (path required), `id` (path required), `user` (path required).

### POST /api/v1/societies/{tenant}/communications/messages/{id}/report

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/communications/messages/{id}/moderate

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/communications/messages/{id}/decision

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/communications/automations/member-greetings

Parameters: `tenant` (path required).

### GET /api/v1/societies/{tenant}/communications/received/{id}

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/communications/audiences/preview

Parameters: `tenant` (path required), `unit` (query optional), `channel` (query required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/communications/audiences

Parameters: `tenant` (path required).

### POST /api/v1/societies/{tenant}/communications/audiences

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/communications/templates

Parameters: `tenant` (path required), `workspace` (query optional).

### POST /api/v1/societies/{tenant}/communications/templates

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### PUT /api/v1/societies/{tenant}/communications/templates/{id}

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/communications/templates/{id}/preview

Parameters: `tenant` (path required), `id` (path required), `membershipId` (query required).

### GET /api/v1/societies/{tenant}/communications/campaigns

Parameters: `tenant` (path required), `workspace` (query optional), `kind` (query optional), `status` (query optional).

### POST /api/v1/societies/{tenant}/communications/campaigns

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/communications/campaigns/{id}

Parameters: `tenant` (path required), `id` (path required).

### PUT /api/v1/societies/{tenant}/communications/campaigns/{id}

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/communications/campaigns/{id}/decision

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/communications/campaigns/{id}/cancel

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/communications/campaigns/{id}/duplicate

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/communications/announcements

Parameters: `tenant` (path required).

### POST /api/v1/societies/{tenant}/communications/announcements/{id}/acknowledge

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/communications/automations

Parameters: `tenant` (path required).

### POST /api/v1/societies/{tenant}/communications/automations

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/communications/automations/{id}/toggle

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/communications/automations/{id}/executions

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/communications/accounts

Parameters: `tenant` (path required).

### POST /api/v1/societies/{tenant}/communications/accounts

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### PUT /api/v1/societies/{tenant}/communications/accounts/{id}

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/communications/accounts/{id}/verify

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/communications/accounts/{id}/templates/sync

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/communications/delivery

Parameters: `tenant` (path required), `campaign` (query optional), `workspace` (query optional), `status` (query optional), `page` (query optional).

### POST /api/v1/societies/{tenant}/communications/delivery/{id}/retry

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/communications/analytics

Parameters: `tenant` (path required), `workspace` (query optional), `campaign` (query optional), `from` (query optional), `to` (query optional).

### GET /api/v1/societies/{tenant}/communications/notifications

Parameters: `tenant` (path required), `kind` (query optional), `unread` (query optional).

### POST /api/v1/societies/{tenant}/communications/notifications/read

Parameters: `tenant` (path required).

### POST /api/v1/societies/{tenant}/communications/notifications/{id}/read

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/communications/messages/{id}/send

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/communications/messages/{id}/attachments

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/communications/attachments/{id}

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/public/societies/{tenant}/community/posts/{id}

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/public/societies/{tenant}/community/media/{id}

Parameters: `tenant` (path required), `id` (path required).

### DELETE /api/v1/societies/{tenant}/community/posts/{id}

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/community/posts/{id}

Parameters: `tenant` (path required), `id` (path required).

### PUT /api/v1/societies/{tenant}/community/posts/{id}

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/community/authors

Parameters: `tenant` (path required).

### GET /api/v1/societies/{tenant}/community/setup

Parameters: `tenant` (path required).

### PUT /api/v1/societies/{tenant}/community/policy

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/community/categories

Parameters: `tenant` (path required), `kind` (query optional).

Request body: application/json. See OpenAPI schema for field types.

### PUT /api/v1/societies/{tenant}/community/categories/{id}

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/community/feed

Parameters: `tenant` (path required), `workspace` (query optional), `chapter` (query optional), `category` (query optional), `author` (query optional), `relatedEvent` (query optional), `status` (query optional), `search` (query optional), `from` (query optional), `to` (query optional), `before` (query optional), `beforeId` (query optional), `bookmarks` (query optional).

### POST /api/v1/societies/{tenant}/community/posts

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/community/posts/{id}/decision

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/community/posts/{id}/history

Parameters: `tenant` (path required), `id` (path required).

### PUT /api/v1/societies/{tenant}/community/posts/{id}/reaction

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/community/posts/{id}/comments

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### PUT /api/v1/societies/{tenant}/community/posts/{id}/bookmark

Parameters: `tenant` (path required), `id` (path required).

### DELETE /api/v1/societies/{tenant}/community/posts/{id}/bookmark

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/community/posts/{id}/report

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/community/reports

Parameters: `tenant` (path required).

### POST /api/v1/societies/{tenant}/community/posts/{id}/moderate

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/community/comments/{id}/hide

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### PUT /api/v1/societies/{tenant}/community/blocks/{user}

Parameters: `tenant` (path required), `user` (path required).

### DELETE /api/v1/societies/{tenant}/community/blocks/{user}

Parameters: `tenant` (path required), `user` (path required).

### GET /api/v1/societies/{tenant}/community/my/events

Parameters: `tenant` (path required), `status` (query optional), `page` (query optional).

### GET /api/v1/societies/{tenant}/community/events

Parameters: `tenant` (path required), `from` (query optional), `to` (query optional), `workspace` (query optional), `category` (query optional), `search` (query optional), `status` (query optional), `mine` (query optional), `page` (query optional).

### POST /api/v1/societies/{tenant}/community/events

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/community/events/{id}

Parameters: `tenant` (path required), `id` (path required).

### PUT /api/v1/societies/{tenant}/community/events/{id}

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/community/events/{id}/decision

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/community/events/{id}/cancel

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/community/events/{id}/duplicate

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/community/events/{id}/audience

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/community/events/{id}/invitations

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/community/invitations

Parameters: `tenant` (path required).

### POST /api/v1/societies/{tenant}/community/invitations/accept

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/community/invitations/{id}/revoke

Parameters: `tenant` (path required), `id` (path required).

### PUT /api/v1/societies/{tenant}/community/events/{id}/rsvp

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/community/events/{id}/registrations

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/community/events/{id}/registrations

Parameters: `tenant` (path required), `id` (path required), `search` (query optional), `page` (query optional).

### POST /api/v1/societies/{tenant}/community/events/{id}/registrations/{registration}/decision

Parameters: `tenant` (path required), `id` (path required), `registration` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/community/events/{id}/passes

Parameters: `tenant` (path required), `id` (path required), `registration` (query required).

### POST /api/v1/societies/{tenant}/community/events/{id}/passes/{pass}/revoke

Parameters: `tenant` (path required), `id` (path required), `pass` (path required).

### POST /api/v1/societies/{tenant}/community/events/{id}/checkins

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/community/events/{id}/attendance

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/community/events/{id}/attendance/{attendance}/correct

Parameters: `tenant` (path required), `id` (path required), `attendance` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/community/events/{id}/analytics

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/community/media

Parameters: `tenant` (path required), `resource` (query required), `kind` (query required).

### GET /api/v1/societies/{tenant}/community/media/{id}

Parameters: `tenant` (path required), `id` (path required).

### PUT /api/v1/societies/{tenant}/community/media/{id}

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### DELETE /api/v1/societies/{tenant}/community/media/{id}

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/community/members/{id}/activity

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/community/intelligence

Parameters: `tenant` (path required), `workspace` (query optional), `from` (query optional), `to` (query optional).

### GET /api/v1/societies/{tenant}/community/chapters/{id}

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/membership/categories

Parameters: `tenant` (path required).

### POST /api/v1/societies/{tenant}/membership/categories

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### PUT /api/v1/societies/{tenant}/membership/categories/{id}

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/membership/policy

Parameters: `tenant` (path required), `chapter` (query optional).

### PUT /api/v1/societies/{tenant}/membership/policy

Parameters: `tenant` (path required), `chapter` (query optional).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/membership/members

Parameters: `tenant` (path required), `chapter` (query optional), `workspace` (query optional), `search` (query optional), `statuses` (query optional), `categories` (query optional), `sort` (query optional), `descending` (query optional), `page` (query optional), `pageSize` (query optional).

### POST /api/v1/societies/{tenant}/membership/members

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/membership/members/{id}

Parameters: `tenant` (path required), `id` (path required).

### PUT /api/v1/societies/{tenant}/membership/members/{id}

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/membership/applications

Parameters: `tenant` (path required), `page` (query optional).

### POST /api/v1/societies/{tenant}/membership/applications/{id}/decision

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/membership/applications/{id}/documents

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/membership/applications/{id}/documents

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/membership/applications/{id}/documents/{document}

Parameters: `tenant` (path required), `id` (path required), `document` (path required).

### GET /api/v1/societies/{tenant}/membership/applications/{id}/history

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/membership/invitations

Parameters: `tenant` (path required).

### POST /api/v1/societies/{tenant}/membership/invitations

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/membership/invitations/{id}/revoke

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/membership/intelligence

Parameters: `tenant` (path required), `workspace` (query optional).

### GET /api/v1/societies/{tenant}/membership/views

Parameters: `tenant` (path required).

### POST /api/v1/societies/{tenant}/membership/views

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/membership/export

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/membership/positions

Parameters: `tenant` (path required).

### POST /api/v1/societies/{tenant}/membership/positions

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### PUT /api/v1/societies/{tenant}/membership/positions/{id}

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/membership/appointments

Parameters: `tenant` (path required), `unit` (query optional), `member` (query optional), `page` (query optional).

### POST /api/v1/societies/{tenant}/membership/appointments

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/membership/appointments/{id}/decision

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/membership/appointments/{id}/revoke

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/membership/handovers

Parameters: `tenant` (path required), `page` (query optional).

### POST /api/v1/societies/{tenant}/membership/handovers

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/membership/handovers/{id}/decision

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/membership/committees

Parameters: `tenant` (path required), `unit` (query optional), `active` (query optional), `page` (query optional).

### POST /api/v1/societies/{tenant}/membership/committees

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### PUT /api/v1/societies/{tenant}/membership/committees/{id}

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/membership/committees/{id}/members

Parameters: `tenant` (path required), `id` (path required), `page` (query optional).

### POST /api/v1/societies/{tenant}/membership/committees/{id}/members

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### DELETE /api/v1/societies/{tenant}/membership/committees/{id}/members/{membership}

Parameters: `tenant` (path required), `id` (path required), `membership` (path required).

### GET /api/v1/societies/{tenant}/membership/transfers

Parameters: `tenant` (path required).

### POST /api/v1/societies/{tenant}/membership/transfers

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/membership/transfers/{id}/decision

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/societies/{tenant}/membership/imports/preview

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/membership/imports

Parameters: `tenant` (path required).

### GET /api/v1/societies/{tenant}/membership/imports/{id}

Parameters: `tenant` (path required), `id` (path required), `page` (query optional).

### POST /api/v1/societies/{tenant}/membership/imports/{id}/confirm

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/membership/imports/{id}/retry

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/membership/members/{id}/photo

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/membership/members/{id}/photo

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/membership/members/{id}/documents

Parameters: `tenant` (path required), `id` (path required).

### POST /api/v1/societies/{tenant}/membership/members/{id}/documents

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/membership/members/{id}/documents/{document}

Parameters: `tenant` (path required), `id` (path required), `document` (path required).

### POST /api/v1/auth/signup

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/auth/verify

Parameters: `userId` (query required), `token` (query required).

### POST /api/v1/auth/login

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/auth/logout

### POST /api/v1/auth/forgot

Request body: application/json. See OpenAPI schema for field types.

### POST /api/v1/auth/reset

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/organizations/levels

Parameters: `tenant` (path required).

### POST /api/v1/societies/{tenant}/organizations/levels

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### PUT /api/v1/societies/{tenant}/organizations/levels/{id}

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/organizations/units

Parameters: `tenant` (path required), `parentId` (query optional), `workspace` (query optional), `kind` (query optional), `search` (query optional), `status` (query optional), `sort` (query optional), `descending` (query optional), `page` (query optional), `pageSize` (query optional).

### POST /api/v1/societies/{tenant}/organizations/units

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/organizations/units/{id}

Parameters: `tenant` (path required), `id` (path required).

### PUT /api/v1/societies/{tenant}/organizations/units/{id}

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/role-candidates

Parameters: `tenant` (path required), `search` (query optional).

### GET /api/v1/societies/{tenant}/permissions

Parameters: `tenant` (path required).

### GET /api/v1/societies/{tenant}/roles

Parameters: `tenant` (path required).

### POST /api/v1/societies/{tenant}/roles

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### PUT /api/v1/societies/{tenant}/roles/{id}

Parameters: `tenant` (path required), `id` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/role-assignments

Parameters: `tenant` (path required), `page` (query optional).

### POST /api/v1/societies/{tenant}/role-assignments

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### DELETE /api/v1/societies/{tenant}/role-assignments/{id}

Parameters: `tenant` (path required), `id` (path required).

### GET /api/v1/societies/{tenant}/dashboard

Parameters: `tenant` (path required), `workspace` (query optional).

### GET /api/v1/societies/{tenant}/settings

Parameters: `tenant` (path required).

### PUT /api/v1/societies/{tenant}/settings

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/onboarding

Parameters: `tenant` (path required).

### PUT /api/v1/societies/{tenant}/onboarding

Parameters: `tenant` (path required).

Request body: application/json. See OpenAPI schema for field types.

### GET /api/v1/societies/{tenant}/audit-logs

Parameters: `tenant` (path required), `page` (query optional).
