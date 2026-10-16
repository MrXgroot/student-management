# API Contract Conventions

EduCore exposes a RESTful HTTP API versioned under `/api/v1/...`. All contracts are documented with OpenAPI / Swagger.

---

## 1. General Request & Response Rules

1. **Content Type:** All requests and responses use `application/json` with camelCase property formatting.
2. **Identifier Serialization:** All MongoDB `ObjectId` values are represented strictly as 24-character hexadecimal `string` values. No native BSON or complex driver objects are exposed.
3. **Monetary Values:** All currency values are denominated in INR and represented as **integer paise** (e.g. ₹5,000 is represented as `500000`). Floating-point numbers are prohibited.
4. **Timestamps:** All stored instants and API response timestamps are in UTC ISO-8601 format (`YYYY-MM-DDTHH:mm:ssZ`).
5. **Bounded Pagination:** List endpoints support `page` (default 1) and `pageSize` (default 20, maximum 100).
   Response pagination envelope:
   ```json
   {
     "items": [ ... ],
     "page": 1,
     "pageSize": 20,
     "totalCount": 154,
     "totalPages": 8
   }
   ```
6. **Error Format (RFC 7807 ProblemDetails):** All errors return standard `application/problem+json`:
   ```json
   {
     "type": "https://educore.vit.edu/errors/excess-payment",
     "title": "Excess Payment Not Permitted",
     "status": 400,
     "detail": "Payment amount of 500000 paise exceeds total outstanding balance of 350000 paise.",
     "instance": "/api/v1/payments",
     "errorCode": "EXCESS_PAYMENT_NOT_PERMITTED",
     "errors": {
       "amountPaise": ["Amount exceeds remaining obligations."]
     }
   }
   ```

---

## 2. Authentication, CSRF, and Idempotency Headers

- **Authentication:** Managed via secure `HttpOnly` session cookie (`.EduCore.Auth`). No client-side `Authorization: Bearer <token>` is required for the web application.
- **CSRF Token Header:** Mutating state requests (`POST`, `PUT`, `PATCH`, `DELETE`) must forward the `X-XSRF-TOKEN` header matching the issued `XSRF-TOKEN` cookie.
- **Idempotency Key Header:** Critical operations (`POST /api/v1/payments`, `POST /api/v1/admissions/{id}/confirm`, `POST /api/v1/attendance/batch`, `POST /api/v1/marks/batch`) accept an optional/required `X-Idempotency-Key` UUID header.
- **Concurrency Control Header:** Mutating requests on aggregate roots must supply the `version` field in the payload or via an `If-Match: "{version}"` header to enforce optimistic concurrency.

---

## 3. Standard HTTP Status Codes

- `200 OK`: Successful retrieval or synchronous update.
- `201 Created`: Resource successfully created (includes `Location` header).
- `400 Bad Request`: Validation failure, missing required fields, business rule violation.
- `401 Unauthorized`: Missing or expired authentication cookie.
- `403 Forbidden`: Authenticated user lacks permission or attempted to access another student's record.
- `404 Not Found`: Target resource does not exist.
- `409 Conflict`: Concurrency conflict (record updated by another user) or duplicate unique key violation.
- `429 Too Many Requests`: Rate limit exceeded on authentication endpoints.
- `500 Internal Server Error`: Unhandled server exception (stack traces and internal details omitted).

---

## 4. Key Specialized Endpoint Contracts

### 4.1 Authentication & Session
- `POST /api/v1/auth/login` — Authenticate credentials, set session cookie + CSRF cookie.
- `POST /api/v1/auth/logout` — Revoke session and clear cookies.
- `GET /api/v1/auth/me` — Return authenticated user profile, roles, and active workspace permissions.
- `POST /api/v1/auth/activate` — Student account activation with token verification.
- `GET /api/v1/auth/csrf` — Handshake to obtain fresh CSRF token cookie.

### 4.2 Batch Tabular Data Entry Endpoints
- `PUT /api/v1/attendance/sessions/{sessionId}/roster`
  Submits an entire session attendance register in a single atomic payload:
  ```json
  {
    "version": 1,
    "roster": [
      { "studentId": "651a1f...", "status": "PRESENT", "remarks": null },
      { "studentId": "651a2a...", "status": "LATE", "remarks": "10 min late" },
      { "studentId": "651a3b...", "status": "ABSENT", "remarks": null }
    ]
  }
  ```
- `PUT /api/v1/assessments/{assessmentId}/marks/batch`
  Submits or moderates a class marks register in a single atomic payload:
  ```json
  {
    "version": 1,
    "marksEntries": [
      { "studentId": "651a1f...", "rawMarks": 34.5, "isAbsent": false },
      { "studentId": "651a2a...", "rawMarks": 0, "isAbsent": true }
    ]
  }
  ```

### 4.3 Student Self-Service Endpoints (Derived Identity)
- `GET /api/v1/students/me` — Student profile.
- `GET /api/v1/students/me/timetable` — Personal class schedule.
- `GET /api/v1/students/me/attendance` — Personal attendance percentages and session breakdown.
- `GET /api/v1/students/me/fees` — Personal fee obligations, payment history, and receipt details.
- `GET /api/v1/students/me/exams` — Personal exam timetable and hall-ticket eligibility status.
- `GET /api/v1/students/me/results` — Published course results, SGPA, and cumulative credits.
