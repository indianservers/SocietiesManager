# 2027 login and workspace experience

The members homepage is now the sign-in screen. `/login` remains a compatibility redirect to `/`, preserving query parameters and fragments. Authenticated visitors to `/` return to their appropriate workspace. Account creation, verification, recovery, magic links, passkeys and MFA retain their original services.

## Scope: enhancements 1–44

1. Focused two-column sign-in composition with a dedicated form surface.
2. Smaller brand headline and stronger form hierarchy.
3. Compact, quieter community illustration without floating decorative elements.
4. Full society name in the footer; no full-width slogan banner.
5. One primary sign-in action with secondary alternatives.
6. Visible passkey/email-link entry points; configured social providers retained.
7. Explicit opt-in remembered email, with a returning-account summary and switch action. Passwords are never stored.
8. Consistent sign-in/create-account/recovery wording.
9. Shared focus, validation, disabled, autofill and loading states.
10. Existing field-level validation and generic authentication errors retained.
11. Existing code autofill/paste retained; resend feedback and a request-again countdown added. Server rate limits remain authoritative.
12. English, Hindi and Telugu selectors remain available before sign-in.
13. Authentication-first mobile layout, followed by supporting brand content.
14. One account-creation invitation instead of repeated competing controls.
15. Separate member, organization representative, staff and platform workspaces, with role-aware quick actions and default destinations.
16. A real attention API for permission-scoped application reviews, overdue invoices and expiring/expired registers; members see their own renewal needs.
17. Upcoming authorized events and resumable member applications replace decorative dashboard content when available.
18. Simplified staff homepage without nested summary tabs or a welcome banner.
19. Grouped workspace, member, community and administration navigation.
20. Personal membership distinguished from administrative member directories.
21. Keyboard-accessible Ctrl/Cmd+K search for available pages/actions and authorized member, chapter, event and invoice records. Event search uses the calendar API's current date window.
22. Visible society, scoped chapter and role context.
23. Role-aware quick actions; platform Add member opens the actual member dialog.
24. Dashboard metrics link to corresponding directories and registers.
25. Current totals explicitly distinguished from activity-period metrics, with update timestamps; no fabricated trends.
26. Unread-first notifications, category filters, capped badge counts and the latest 100 matching records; existing read actions retained.
27. Favorites/recent pages scoped to account, society and chapter. Stored destinations are checked against current role navigation before rendering.
28. Mobile bottom navigation with searchable and complete More navigation.
29. First-class digital membership summary with identity, validity and permitted downloads; issued-card policies remain enforced.
30. Explicit personal versus represented-organization context and correct membership-kind switching.
31. Membership/renewal invoice/payment/validity progress with original confirmed-payment activation rules.
32. Onboarding progress and one recommended next step, with optional full checklist and dismissal.
33. Sticky table headers and contextual bulk action styling; existing saved views, column controls and selection retained.
34. Existing member quick-view drawers retained and styled consistently.
35. Optional personal/professional sections progressively disclosed without unmounting submitted controls.
36. Opt-in browser-tab draft recovery for a restricted set of basic application fields, keyed by user, society, kind and application. Sensitive fields, documents and passwords are excluded. Existing server-side Save draft remains available.
37. Member-directory empty states offer a working clear-filters action; module-specific empty states retained.
38. Existing explicit success/error feedback retained with live-region and consistent control styling. Application saves clear the corresponding local draft.
39. Shared typography, surface, spacing, radius, control and status tokens across existing modules.
40. Restrained purple used for selected states and primary actions; calmer neutral backgrounds.
41. More legible labels and supporting text.
42. Persisted comfortable/compact density for tables and workspace controls.
43. Short interaction transitions and stable skeleton states, respecting reduced motion.
44. Authentication forms are explicitly excluded from unsaved-work warnings; genuine workspace edits retain leave/cancel protection. Keyboard focus, drawer focus management, touch controls, visible labels, password-manager support and reduced-motion behavior.

## Validation and deployment boundaries

Use `npm run build --prefix frontend`, `npm run test:navigation --prefix frontend` and `dotnet build backend`. Browser/API verification lives in `tests/experience_browser.py` and `tests/experience_api.py`; set `EXPERIENCE_URL` to the current preview. Existing sign-in, passkey/MFA, individual self-service and organization-portal integration/browser tests cover retained workflows.

No schema migration is required. The new attention endpoint reads original services and permission scopes. Membership expiry filters add `Expired` and `Expiring` alongside `Active`; the latter means within 30 days. Application review queries now honor the selected workspace. Existing ownership, payment, MFA, download and independent-approval checks remain authoritative.

SMTP transport, real biometric hardware and external-provider consent are configuration-dependent and are not certified by browser emulation. Enhancement 45 (new contextual AI assistance) is outside this release.
