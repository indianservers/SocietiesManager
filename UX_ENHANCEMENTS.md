# Membership portal UX enhancements

Implemented for the AIMER membership portal. Existing membership IDs, validity, organization/individual separation, and authorization remain authoritative.

1. Distinct individual, institutional, and corporate navigation entries.
2. Organization summary with holder, number, validity, primary representative, email and mobile.
3. Email and telephone links on contact cards and profile summaries.
4. Authorized directory search by contact name, email or mobile.
5. Explicit organization-holder, individual-person and contact-person labels.
6. Primary contact badge; first contact is the canonical representative.
7. Inline personal and organization contact editors.
8. Saved feedback includes a timestamp and refreshes current data.
9. Unsaved drawer, link navigation and browser-close protection.
10. Missing source details and not-entered details have explanatory empty states.
11. Profile completeness shows five key fields and a completion action.
12. Readable dates throughout the portal, including inclusive IST membership expiry.
13. Membership state badges distinguish active, expiring, expired and payment-required states.
14. Opt-in reminder timing: 7, 14, 30, 60 or 90 days before expiry.
15. Owned membership document downloads beside the profile summary; access checks retained.
16. Person and organization forms have different fields.
17. Contact add, remove, move and primary selection; synchronized to affiliated chapters.
18. Adjacent accessible native validation and server-side contact validation.
19. Possible duplicate name, email and membership number warnings within permitted scope.
20. Spreadsheet/CSV column inspection, automatic matching and sample mapping preview.
21. Existing/proposed import comparison. Matching records are excluded from this create-only workflow; their profile opens for reviewed edits. Imports do not overwrite existing memberships.
22. Downloadable CSV row errors with correction instructions and spreadsheet-formula escaping.
23. Authorized actor, timestamp, and before/after profile/contact history. Older entries without snapshots are labelled as recorded register events.
24. Links between institution memberships, affiliated chapters and managed clubs.
25. Human-only role selector guidance, with existing API enforcement.
26. Persistent directory columns including city, primary contact and validity.
27. URL-backed search, filters, sorting and pagination, plus restored scroll position.
28. Mobile contact cards, large touch targets and expandable row actions.
29. Skip link, focus visibility, labelled errors, keyboard focus management, reduced motion and improved contact-label contrast.
30. Platform, society administrator, officer, organization representative and member home actions.

## Validation

- PostgreSQL/HTTP membership regression suite, including UX authorization, contact concurrency/synchronization, search, import protection, and reminder settings.
- Real local browser workflows: contact editing, affiliation synchronization, navigation persistence, import sample mapping, validation and unsaved changes.
- Whole-app role/theme/mobile browser audit and navigation tests.
- Production read-only browser verification and source-data reconciliation after deployment.

No schema migration is required. Contact edits change profile information and chapter details; they do not silently change account identity or governance grants. Unverified contact addresses are not subscribed to notifications.
