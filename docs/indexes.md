# MongoDB Index Plan and Constraint Specification

Unique indexes in EduCore are primary correctness controls, not merely query optimizations. All indexes are validated at application startup during Phase 1 foundation setup.

---

## 1. Unique Constraints (Correctness Controls)

| Collection | Index Keys | Options | Invariant Enforced |
|---|---|---|---|
| `institutions` | `{ code: 1 }` | `unique: true` | Prevents duplicate institution codes. |
| `departments` | `{ institutionId: 1, code: 1 }` | `unique: true` | Unique department code within institution. |
| `programs` | `{ institutionId: 1, code: 1 }` | `unique: true` | Unique program code within institution. |
| `academicYears` | `{ institutionId: 1, label: 1 }` | `unique: true` | Unique academic year label within institution. |
| `terms` | `{ academicYearId: 1, code: 1 }` | `unique: true` | Unique term code within an academic year. |
| `courses` | `{ institutionId: 1, code: 1 }` | `unique: true` | Unique catalog course code within institution. |
| `courseOfferings` | `{ termId: 1, courseId: 1, section: 1 }` | `unique: true` | Prevents duplicate section offerings for a course in a term. |
| `users` | `{ username: 1 }` | `unique: true` | Unique login username (case-insensitive collation). |
| `users` | `{ email: 1 }` | `unique: true` | Unique user email address. |
| `staffProfiles` | `{ staffNumber: 1 }` | `unique: true` | Unique staff employee number. |
| `staffProfiles` | `{ userId: 1 }` | `unique: true` | One staff profile per user account. |
| `applications` | `{ applicationNumber: 1 }` | `unique: true` | Unique sequential application identifier. |
| `students` | `{ admissionNumber: 1 }` | `unique: true` | Unique sequential admission number. |
| `students` | `{ rollNumber: 1 }` | `unique: true, sparse: true` | Unique class roll number when assigned. |
| `students` | `{ universityRegNumber: 1 }` | `unique: true, sparse: true` | Unique external university registration number when assigned. |
| `students` | `{ applicationId: 1 }` | `unique: true` | Prevents creating multiple students from the same application. |
| `termRegistrations` | `{ studentId: 1, termId: 1 }` | `unique: true` | Exactly one term registration per student per term. |
| `courseEnrollments` | `{ studentId: 1, courseOfferingId: 1 }` | `unique: true` | Prevents duplicate enrollments in the same offering section. |
| `payments` | `{ idempotencyKey: 1 }` | `unique: true` | Replay protection for payment creation. |
| `payments` | `{ "receipt.receiptNumber": 1 }` | `unique: true` | Unique sequential receipt identifier. |
| `marks` | `{ assessmentId: 1, studentId: 1 }` | `unique: true` | Exactly one mark record per student per assessment event. |
| `examRegistrations` | `{ assessmentId: 1, studentId: 1 }` | `unique: true` | Exactly one eligibility snapshot per student per assessment. |
| `results` | `{ studentId: 1, courseOfferingId: 1, termId: 1 }` | `unique: true` | Exactly one compiled result per student per course offering. |
| `counters` | `{ _id: 1 }` | `unique: true` (Primary Key) | Single atomic sequence generator per named counter. |
| `idempotencyRecords` | `{ scope: 1, key: 1 }` | `unique: true` | API endpoint idempotency tracking. |

---

## 2. Query and Performance Indexes

### Academic Structure & Enrollments
- `courseOfferings`: `{ termId: 1, status: 1 }` — Term course listing and filtering.
- `courseOfferings`: `{ "facultyAssignments.facultyId": 1, termId: 1 }` — Faculty timetable and assigned classes lookup.
- `courseEnrollments`: `{ courseOfferingId: 1, status: 1 }` — Roster generation and section capacity queries.
- `courseEnrollments`: `{ studentId: 1, termId: 1 }` — Student current term course enrollments.

### Class Sessions & Attendance
- `classSessions`: `{ courseOfferingId: 1, sessionDate: 1 }` — Class calendar and timetable lookup.
- `classSessions`: `{ facultyId: 1, sessionDate: -1 }` — Faculty class dashboard.
- `classSessions`: `{ "roster.studentId": 1, courseOfferingId: 1 }` — Student attendance aggregation across sessions.

### Fees & Financial Ledgers
- `feeObligations`: `{ studentId: 1, status: 1 }` — Student active unpaid obligations and ledger view.
- `feeObligations`: `{ termId: 1, dueDate: 1 }` — Financial due-date reconciliation and overdue reporting.
- `payments`: `{ studentId: 1, recordedAt: -1 }` — Student payment history and receipt retrieval.
- `payments`: `{ status: 1, recordedAt: -1 }` — Daily accounts collection reconciliation.

### Examinations & Results
- `assessments`: `{ courseOfferingId: 1, state: 1 }` — Faculty assessment listing.
- `marks`: `{ assessmentId: 1, status: 1 }` — Moderation queue and submission verification.
- `results`: `{ studentId: 1, termId: 1, status: 1 }` — Student published result lookup.
- `results`: `{ termId: 1, status: 1 }` — Term progression evaluation queries.

### Governance, Security, and Idempotency
- `auditEvents`: `{ targetType: 1, targetId: 1, timestamp: -1 }` — Entity audit history timeline.
- `auditEvents`: `{ actorId: 1, timestamp: -1 }` — Actor activity monitoring.
- `idempotencyRecords`: `{ expiresAt: 1 }` with `expireAfterSeconds: 0` (TTL Index) — Automatic cleanup of expired idempotency keys after 24 hours.

---

## 3. Operational Rules
1. **Startup Index Creation:** An index initialization routine executes at backend startup (`EduCore.Infrastructure.Persistence.IndexConfigurator`), asserting and ensuring all unique and query indexes exist.
2. **Seed Data Invariant:** Seed data fixtures must strictly respect all unique indexes; duplicate key errors during seeding are treated as fatal configuration failures.
3. **No Unindexed Regular Expressions:** Case-insensitive queries must use standard MongoDB collations or normalized search fields rather than leading-wildcard regex expressions.
