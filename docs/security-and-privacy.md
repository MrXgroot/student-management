# Security and Privacy Baseline

This document specifies the security, authentication, authorization, audit immutability, and privacy controls enforced across EduCore.

---

## 1. Authentication and Session Management

### 1.1 Cookie-Based Authentication
EduCore uses standard **ASP.NET Core Cookie Authentication** (`CookieAuthenticationDefaults.AuthenticationScheme`) for session management:
- **Cookie Flags:**
  - `HttpOnly = true`: Mitigates XSS token exfiltration; cookie cannot be read by JavaScript.
  - `SecurePolicy = CookieSecurePolicy.Always`: Enforced in production/HTTPS; `SameAsRequest` permitted only for local non-SSL development.
  - `SameSite = SameSiteMode.Strict`: Protects against Cross-Site Request Forgery (CSRF) in cross-site navigations.
  - `SlidingExpiration = true`: Session timeout is 60 minutes; reset on active use.
- **CSRF Defense:**
  - An Anti-Forgery token cookie (`XSRF-TOKEN`) is issued alongside the session cookie.
  - The React frontend API client reads this cookie and passes it in the `X-XSRF-TOKEN` HTTP header on all mutating requests (`POST`, `PUT`, `PATCH`, `DELETE`).
  - Mutating endpoints validate this header via ASP.NET Core `AutoValidateAntiforgeryTokenAttribute` or endpoint filters.

### 1.2 Password Hashing
- Passwords are hashed using ASP.NET Core's built-in `PasswordHasher<User>`, implementing PBKDF2 with HMAC-SHA512 and a high iteration count (100,000+ iterations) with cryptographically unique salts.
- Plaintext passwords are never logged, persisted, or returned in API responses.

### 1.3 Student Account Activation Handshake
1. When an admissions officer confirms an admission, a corresponding `users` document is created with role `["STUDENT"]` and status `PENDING_ACTIVATION`.
2. The system generates a cryptographically secure, high-entropy 32-byte activation token. The SHA-256 hash of this token is stored in `activationTokenHash` with a 72-hour expiration in `activationTokenExpiresAt`.
3. The raw activation token is output on the official printed admission slip (or securely logged in local demo output).
4. The student navigates to `/activate?token=...`, validates their identity (date of birth and admission number), and sets their permanent password.
5. Upon successful activation, `activationTokenHash` is cleared, and `status` transitions to `ACTIVE`.

### 1.4 Session Revocation and Invalidation
- **Explicit Logout:** `POST /api/v1/auth/logout` revokes the authentication ticket and returns expired cookies.
- **Security Stamp Invalidation:** The `users` document stores a `securityStamp` (GUID). When a user changes their password, or when an administrator suspends or terminates an account, the `securityStamp` is regenerated. The cookie validation event checks this stamp on each sliding renewal, immediately invalidating any active sessions associated with the old stamp.

---

## 2. Authorization and Resource Ownership

- **Default Deny:** All controllers and endpoints require authentication and explicit role or policy authorization unless marked with `[AllowAnonymous]`.
- **Derived Identity for Students:** Student portal endpoints derive the target `studentId` strictly from the claims embedded in the authenticated cookie. Any client-supplied student ID in query parameters or request bodies is disregarded.
- **Separation of Duties:**
  - Administrators cannot edit marks directly without an approved audit override.
  - Faculty can enter marks and attendance only for their assigned course offerings.
  - Exam Officers cannot publish unapproved marks.
  - Accounts Officers can reverse payments only with an explicit audit reason.

---

## 3. Audit Immutability & Operational Logging

### 3.1 Application-Level Audit Protections
- All high-impact operations (`PAYMENT_RECORDED`, `PAYMENT_REVERSED`, `ADMISSION_CONFIRMED`, `MARKS_MODERATED`, `RESULT_AMENDED`, `ROLE_GRANTED`) append an entry to the `auditEvents` collection.
- The `IAuditEventRepository` interface exposes **only** `InsertAsync` and `QueryAsync` methods. It does not contain `Update`, `Replace`, or `Delete` methods.

### 3.2 Database-Level Access Controls vs. Application Controls
- Application code accesses MongoDB via dedicated credentials.
- In production deployments, the database role assigned to the application should restrict operations on the `auditEvents` collection to `insert` and `find`, disallowing `update` and `delete` privileges at the MongoDB RBAC level.
- Audit records must never be purged during standard archiving or soft-delete routines.

### 3.3 Log Sanitization
Operational logs (structured Serilog logs) must **never** contain:
- Passwords or temporary activation tokens.
- Authentication cookies or CSRF tokens.
- Complete bank account numbers, UPI IDs, or payment details.
- Full unmasked identity document copies or student PII.

---

## 4. Local Development and Seed Account Safety

- Development environments use pre-seeded fictional accounts with documented demo credentials (e.g. `admin@vit.edu`, `faculty1@vit.edu`, `accounts@vit.edu`).
- Seed scripts are version-controlled but are strictly restricted to local development environments (`ASPNETCORE_ENVIRONMENT=Development`).
- Production deployments require explicit first-run administrator provisioning through a secure CLI command or environment-injected credentials.
- No production secrets or production student data may ever be committed to Git.
