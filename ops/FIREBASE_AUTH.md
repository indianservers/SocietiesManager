# Portal authentication

The portal keeps ASP.NET Identity cookies, password login, lockout and MFA. Google and Microsoft use the Firebase web SDK without Analytics or persistent Firebase sessions. The server verifies ID tokens with Google account lookup, checks project, issuer, provider, subject, expiry and revocation, and stores the stable Firebase UID in Identity external logins.

Copy the public launch settings from `ops/aimer-firebase.json` to `api/firebase-auth.json` alongside the deployed executable. Other organizations can supply their own `Authentication:Firebase` configuration; environment variables override the file. Enable the corresponding providers and authorize the portal domain in Firebase. Microsoft OAuth configuration stays in Firebase; no client secret is shipped to the browser.

Existing users sign in once with their current password or a connected provider to link a matching provider email. MFA remains required when enabled. Email matching alone never signs into an existing account. New social accounts receive no membership, organization role or platform privileges. Unverified provider emails require local verification.

Password login accepts existing email addresses and optional signup usernames. Existing accounts retain their current usernames. Recovery continues to use email.

Validation: `dotnet run --project tests/FirebaseAuthHarness -- --verifier-tests`, then build the harness to `.local/firebase-auth-tests` and run `tests/firebase_auth_integration.py` and `tests/firebase_auth_browser.py` against the isolated test PostgreSQL database. The harness alone injects a fake upstream identity verifier; production accepts only Google-verified tokens. Browser tests mock only the provider SDK boundary. Real provider consent requires the user's Google or Microsoft account.
