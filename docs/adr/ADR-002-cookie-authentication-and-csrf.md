# ADR-002: Authentication, Session Management, and CSRF Protection Strategy

- Status: Accepted
- Date: 2026-10-16
- Decision owners: Architecture & Engineering Team

## Context
EduCore is an administrative and student portal web application with a React single-page frontend (SPA) served alongside an ASP.NET Core Web API backend. The system handles sensitive personal records, financial ledgers, and official examination results.

Selecting an authentication and session mechanism requires balancing security against operational simplicity:
- **JWTs in browser storage (`localStorage` / `sessionStorage`):** Vulnerable to Cross-Site Scripting (XSS). Any script injected into the SPA can read the bearer token and impersonate the user.
- **JWTs in Authorization headers:** Requires client-side token refresh timers, interceptors, and complex state management in plain JavaScript.
- **Cookie Authentication:** Browsers automatically manage cookie transmission. When configured with `HttpOnly`, `Secure`, and `SameSite`, cookies are inaccessible to client-side scripts, protecting against XSS token exfiltration. However, cookies require explicit defense against Cross-Site Request Forgery (CSRF).

## Decision
1. **ASP.NET Core Cookie Authentication:**
   - Standard ASP.NET Core cookie authentication (`AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)`) is adopted.
   - Session cookies will be configured with:
     - `HttpOnly = true` (blocks client script access).
     - `SecurePolicy = CookieSecurePolicy.Always` in production; `SameAsRequest` in local HTTP development.
     - `SameSite = SameSiteMode.Strict` (mitigates CSRF for top-level navigations and third-party contexts).
     - `SlidingExpiration = true` with a timeout of 60 minutes.
2. **Anti-Forgery (CSRF) Protection for State-Changing Requests:**
   - ASP.NET Core Antiforgery services (`AddAntiforgery`) will issue a non-HttpOnly CSRF token cookie (`XSRF-TOKEN`) alongside the authentication cookie upon successful login and on an initial `/api/v1/auth/csrf` handshake.
   - The React API client reads the `XSRF-TOKEN` cookie value and attaches it as an HTTP header (`X-XSRF-TOKEN`) on all state-changing requests (`POST`, `PUT`, `PATCH`, `DELETE`).
   - The ASP.NET Core middleware or endpoint filter validates `X-XSRF-TOKEN` against the server-side antiforgery token store, rejecting invalid or missing tokens with HTTP `400 Bad Request` or `403 Forbidden`.
3. **Password Hashing & Storage:**
   - Passwords will be hashed using ASP.NET Core's built-in `PasswordHasher<User>` (PBKDF2 with HMAC-SHA512, 100,000+ iterations, per-user unique salt). Plaintext passwords will never be logged, transmitted in error messages, or stored in the database.
4. **Student Account Activation Flow:**
   - Upon admission confirmation, a `users` record is provisioned with role `STUDENT`, `status = "PENDING_ACTIVATION"`, and a cryptographically secure, high-entropy one-time activation token (stored as a hash with a 72-hour expiration).
   - The activation token is provided on the official admission receipt/letter (and logged securely in local development seed output).
   - The student visits `/activate?token=...`, validates their identity (verifying birth date and admission number), and sets their permanent password.
5. **Session Revocation and Logout:**
   - `POST /api/v1/auth/logout` invalidates the server-side authentication ticket and issues expired cookies (`Expires = UnixEpoch`).
   - For administrative security, a `SecurityStamp` (GUID) will be stored on the `users` document. Password changes or administrative account suspensions update the `SecurityStamp`, immediately invalidating existing cookies on the next request.

## Alternatives Considered
- **Option A: JWT Bearer Tokens in `localStorage`:**
  Reject. Highly vulnerable to XSS; unnecessary complexity in client refresh logic for a single-institution application.
- **Option B: OAuth2 / OpenID Connect (OIDC) with Keycloak or Auth0:**
  Deferred. Adding a third-party identity broker for V1 violates the guideline against premature infrastructure complexity for a modular monolith. The design keeps `users` clean so an external OIDC provider can be introduced in V2 without breaking domain logic.

## Consequences
- **Benefits:** Immune to token theft via JavaScript; standard built-in ASP.NET Core security; clean logout and session expiration; seamless TanStack Query integration without manual bearer token attachments.
- **Trade-offs:** Requires CSRF token extraction and header injection in the frontend API client (`shared/api/client.js`).
- **Testing Implications:** Integration tests can use `WebApplicationFactory` with cookie handling or mock authentication handlers to simulate authenticated roles.

## Follow-up
- Update `docs/security-and-privacy.md`, `docs/api-contracts.md`, and `docs/screens.md`.
- Implement CSRF header forwarding in `shared/api/client.js` in Phase 1/Phase 2.
