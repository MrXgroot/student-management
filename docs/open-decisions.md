# Decision Log and Open Questions

This document records the official resolution of architectural, policy, and domain decisions for Phase 0B, and tracks genuine open questions reserved for future milestones.

---

## 1. Resolved Decisions (Approved in Phase 0B)

| Decision ID | Topic | Resolution | Reference |
|---|---|---|---|
| **DEC-01** | Multi-Tenancy Scope | **Single-Institution for V1** (*Vidyadeep Institute of Technology*). No multi-tenant partitioning in V1. | [`docs/SCOPE.md`](file:///d:/production/arise-craft/EduCore/docs/SCOPE.md) |
| **DEC-02** | Student Portal Timing | **Included in V1** (Phases 9 & 10) for self-service viewing of timetable, attendance, fees, exams, and results. | [`docs/SCOPE.md`](file:///d:/production/arise-craft/EduCore/docs/SCOPE.md), [`docs/screens.md`](file:///d:/production/arise-craft/EduCore/docs/screens.md) |
| **DEC-03** | Admission Intake Mode | **Staff-Entered Applications Only for V1**. Public applicant self-service portal is deferred to V2. | [`docs/workflows/admissions.md`](file:///d:/production/arise-craft/EduCore/docs/workflows/admissions.md) |
| **DEC-04** | Attendance Policy & Condonation | **75% minimum threshold** for exam eligibility; **65% to <75% condonation range** requiring authorized approval; Late = 0.5, Excused excluded from denominator. | [`docs/policies/demo-institutional-policies.md`](file:///d:/production/arise-craft/EduCore/docs/policies/demo-institutional-policies.md#L1) |
| **DEC-05** | Grading Scale & Pass Rules | **UGC 10-Point CBCS Scale** (O to F); 40% minimum in SEE and 40% aggregate required to pass. | [`docs/policies/demo-institutional-policies.md`](file:///d:/production/arise-craft/EduCore/docs/policies/demo-institutional-policies.md#L2) |
| **DEC-06** | Assessment Weightings | **40% CIA (Internal) / 60% SEE (Semester End Examination)**. | [`docs/policies/demo-institutional-policies.md`](file:///d:/production/arise-craft/EduCore/docs/policies/demo-institutional-policies.md#L2) |
| **DEC-07** | Fee Allocation Order | **FIFO by due date**, with category tie-breaker: `TUITION` -> `LAB` -> `LIBRARY` -> `EXAM` -> `MISC`. Manual staff override permitted with reason. | [`docs/policies/demo-institutional-policies.md`](file:///d:/production/arise-craft/EduCore/docs/policies/demo-institutional-policies.md#L3) |
| **DEC-08** | Overpayments & Credit Balances | **Overpayments rejected at API boundary in V1**. Student advance-credit deposit ledger deferred to V2. | [`docs/workflows/fees-and-payments.md`](file:///d:/production/arise-craft/EduCore/docs/workflows/fees-and-payments.md) |
| **DEC-09** | Authentication Mechanism | **ASP.NET Core Cookie Authentication** (`HttpOnly`, `SameSite=Strict`, `Secure`), with CSRF token header and password hashing via `PasswordHasher<T>`. | [`ADR-002`](file:///d:/production/arise-craft/EduCore/docs/adr/ADR-002-cookie-authentication-and-csrf.md) |
| **DEC-10** | Database Identifier Strategy | **MongoDB ObjectId exposed as 24-character hex string** in DTOs and API JSON contracts. | [`ADR-003`](file:///d:/production/arise-craft/EduCore/docs/adr/ADR-003-identifier-generation-and-atomic-counters.md) |
| **DEC-11** | Human-Readable Identifiers | **Dedicated `counters` collection with atomic `$inc`** (`ADM-YYYY-XXXXX`, `REC-YYYY-XXXXX`, `APP-YYYY-XXXXX`). Uniqueness guaranteed via unique indexes. | [`ADR-003`](file:///d:/production/arise-craft/EduCore/docs/adr/ADR-003-identifier-generation-and-atomic-counters.md) |
| **DEC-12** | Database Deployment & Transactions | **Single-node MongoDB replica set (`rs0`) mandated for dev/test**, enabling multi-document ACID transactions for payments, reversals, and admissions. | [`ADR-001`](file:///d:/production/arise-craft/EduCore/docs/adr/ADR-001-mongodb-replica-set-and-transactions.md) |
| **DEC-13** | Frontend Styling Framework | **Tailwind CSS (v3+)** with custom design tokens and **Lucide React** icons. | [`docs/design-system.md`](file:///d:/production/arise-craft/EduCore/docs/design-system.md) |
| **DEC-14** | Concurrency Strategy | **Optimistic Concurrency Control (OCC)** using integer `version` field on all mutable aggregate roots. | [`docs/architecture.md`](file:///d:/production/arise-craft/EduCore/docs/architecture.md) |

---

## 2. Genuinely Unresolved / Future Scope Questions (V2 Roadmap)

The following items are not blockers for V1 and are formally deferred to subsequent product milestones:

1. **Online Payment Gateway Integration:**
   - *Question:* Which gateway provider (Razorpay, PayU, BillDesk) should be integrated for student online payments?
   - *Target:* V2. In V1, all payments are recorded manually by authorized Accounts staff.
2. **Physical Document Storage Strategy:**
   - *Question:* Should student application identity documents be stored locally on disk, in MongoDB GridFS, or on an S3-compatible object store (MinIO/AWS S3)?
   - *Target:* V2. For V1, the system records document verification metadata and checklist flags; binary file uploads are optional/simulated.
3. **Supplementary Examination Scheduling & Revaluation Portal:**
   - *Question:* Should students apply and pay for revaluations and supplementary exam attempts directly online?
   - *Target:* V2. In V1, grade amendments and repeat attempts are entered and scheduled by the Examination Cell.
4. **Automated Timetable Generation:**
   - *Question:* Should EduCore include an algorithmic timetable generator resolving room and faculty constraints?
   - *Target:* V2. In V1, timetable schedules are entered manually by the Academic Administrator.
5. **Multi-Institution SaaS Tenant Partitioning:**
   - *Question:* Will multi-tenancy use database-per-tenant, schema-per-tenant, or discriminator keys?
   - *Target:* V3. V1 is strictly single-institution.
