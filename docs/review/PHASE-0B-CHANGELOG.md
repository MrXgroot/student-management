# Phase 0B Specification Changelog

**Release Milestone:** Phase 0B — Approved Specification Corrections  
**Date:** 2026-10-16  
**Auditor / Architect:** Antigravity  
**Target Repository:** `d:\production\arise-craft\EduCore`  

---

## 1. Summary of Changes Applied

Phase 0B reconciled the EduCore specification with the findings in [`docs/review/PHASE-0-AUDIT.md`](file:///d:/production/arise-craft/EduCore/docs/review/PHASE-0-AUDIT.md) and the approved decisions specified in the Phase 0B charter. All documentation has been aligned to ensure internal consistency, strict domain invariants, and technical feasibility prior to code foundation.

---

## 2. Inventory of Modified and Created Files

### 2.1 Architecture Decision Records (ADRs) Created
- [`docs/adr/ADR-001-mongodb-replica-set-and-transactions.md`](file:///d:/production/arise-craft/EduCore/docs/adr/ADR-001-mongodb-replica-set-and-transactions.md)
  - Mandated a single-node MongoDB replica set (`rs0`) for local dev, testing, and CI.
  - Specified explicit multi-document ACID transactions for financial ledger operations (payment recording, reversals) and admission confirmation.
  - Documented retry safety, transient failure handling, and bounded aggregate embedding.
- [`docs/adr/ADR-002-cookie-authentication-and-csrf.md`](file:///d:/production/arise-craft/EduCore/docs/adr/ADR-002-cookie-authentication-and-csrf.md)
  - Adopted ASP.NET Core Cookie Authentication (`HttpOnly`, `SameSite=Strict`, `Secure`) with CSRF protection (`XSRF-TOKEN` cookie + `X-XSRF-TOKEN` header).
  - Defined PBKDF2 HMAC-SHA512 password hashing via `PasswordHasher<User>`.
  - Specified student account activation token handshake (hashed token with 72-hour expiry, forced password reset).
  - Defined session revocation via `SecurityStamp` invalidation.
- [`docs/adr/ADR-003-identifier-generation-and-atomic-counters.md`](file:///d:/production/arise-craft/EduCore/docs/adr/ADR-003-identifier-generation-and-atomic-counters.md)
  - Specified the dedicated `counters` collection using atomic `$inc` via `FindOneAndUpdateAsync`.
  - Standardized identifier formats: `AdmissionNumber` (`ADM-YYYY-00000`), `ReceiptNumber` (`REC-YYYY-00000`), `RollNumber` (`{YY}{PROG}{SEC}{NUM}`), and `ApplicationNumber` (`APP-YYYY-00000`).
  - Formalized that counters guarantee uniqueness (backed by unique MongoDB indexes), permitting non-gapless sequences upon transaction rollback.
  - Mandated string representation of MongoDB ObjectIds across API DTOs and JSON contracts.
- [`docs/adr/README.md`](file:///d:/production/arise-craft/EduCore/docs/adr/README.md)
  - Updated ADR index table with ADR-001, ADR-002, and ADR-003.

### 2.2 Institutional Policies Created / Updated
- [`docs/policies/demo-institutional-policies.md`](file:///d:/production/arise-craft/EduCore/docs/policies/demo-institutional-policies.md) *(Created)*
  - Codified concrete demo configuration for *Vidyadeep Institute of Technology*:
    - **Attendance:** 75% standard threshold; 65% to <75% condonation range; Present = 1.0, Late = 0.5, Excused excluded from denominator; half-up 2 decimal place rounding.
    - **Grading & Assessment:** 40% CIA / 60% SEE weighting; 10-point UGC CBCS scale (O to F); minimum pass = 40% in SEE and 40% combined; missing marks = 0 / incomplete.
    - **Fee Allocation:** FIFO priority by due date; tie-breaker: `TUITION` -> `LAB` -> `LIBRARY` -> `EXAM` -> `MISC`; authorized manual override with reason; integer paise storage; overpayment rejection.
- [`docs/policies/README.md`](file:///d:/production/arise-craft/EduCore/docs/policies/README.md) *(Updated)*
  - Linked `demo-institutional-policies.md` and clarified that policy values are institution-configurable demo presets, not statutory mandates.

### 2.3 Core Specification Documents Updated
- [`docs/SCOPE.md`](file:///d:/production/arise-craft/EduCore/docs/SCOPE.md)
  - Removed applicant self-service portal from V1 (staff-entered applications only).
  - Retained student portal in V1 for viewing timetable, attendance, fees, exams, and published results.
  - Decoupled admission confirmation from payment collection.
  - Reaffirmed single-institution scope (*Vidyadeep Institute of Technology*).
- [`docs/roles-and-permissions.md`](file:///d:/production/arise-craft/EduCore/docs/roles-and-permissions.md)
  - Removed "Own application" permission from `STUDENT` role for V1.
  - Detailed `ADMISSIONS_OFFICER` permissions for admission confirmation and student profile creation.
  - Formalized multi-role handling via the top-bar Workspace Switcher.
  - Enforced derived identity for student endpoints (derived from authenticated cookie, never client parameters).
- [`docs/business-rules.md`](file:///d:/production/arise-craft/EduCore/docs/business-rules.md)
  - Standardized identifier definitions (`Id`, `AdmissionNumber`, `RollNumber`, `UniversityRegNumber`).
  - Defined explicit `StudentLifecycleStatus` enum and permitted state transitions.
  - Documented prerequisites for admission confirmation, account activation, term registration, and exam eligibility.
  - Documented atomic seat capacity enforcement for course offerings.
  - Defined attendance register lifecycle and audited correction invariants.
  - Defined marks entry state machine (`DRAFT` -> `SUBMITTED` -> `MODERATED` -> `LOCKED` -> `APPROVED` -> `PUBLISHED`) and preservation of raw vs. moderated marks.
  - Specified Optimistic Concurrency Control (`version`) across mutable aggregate roots.
- [`docs/database-schema.md`](file:///d:/production/arise-craft/EduCore/docs/database-schema.md)
  - Consolidated aggregates: embedded `allocations` and `receipt` inside `payments`.
  - Embedded session attendance roster (`roster: [...]`) inside `classSessions`.
  - Standardized `examRegistrations` as the canonical examination eligibility snapshot collection.
  - Added embedded `curriculum` mapping to `programs`.
  - Added `semesterNumber` and `programId` to `termRegistrations`.
  - Added `enrolledCount` and `version` to `courseOfferings`.
  - Added `counters` collection for atomic sequence numbering.
  - Detailed multi-document transaction boundaries.
- [`docs/indexes.md`](file:///d:/production/arise-craft/EduCore/docs/indexes.md)
  - Added unique indexes for `counters._id`, `payments.receipt.receiptNumber`, `payments.idempotencyKey`, `students.admissionNumber`, and sparse unique indexes for `rollNumber` and `universityRegNumber`.
  - Added compound performance indexes and TTL index for `idempotencyRecords.expiresAt`.
- [`docs/architecture.md`](file:///d:/production/arise-craft/EduCore/docs/architecture.md)
  - Mandated single-node replica set (`rs0`) for local development and CI.
  - Clarified pragmatic clean architecture layering (`Api`, `Application`, `Domain`, `Infrastructure`).
  - Defined optimistic concurrency control mechanism and retry execution strategy for transient transaction exceptions.
  - Standardized string ObjectId representation across API contracts.
- [`docs/api-contracts.md`](file:///d:/production/arise-craft/EduCore/docs/api-contracts.md)
  - Formatted error responses using RFC 7807 `ProblemDetails`.
  - Specified Cookie auth, `X-XSRF-TOKEN` header, and `X-Idempotency-Key` header conventions.
  - Documented batch endpoints for attendance rosters and assessment marks.
  - Added student self-service derived identity endpoints.
- [`docs/security-and-privacy.md`](file:///d:/production/arise-craft/EduCore/docs/security-and-privacy.md)
  - Detailed cookie security flags, CSRF protection, PBKDF2 password hashing, and activation token handshake.
  - Enforced append-only audit repository semantics (`IAuditEventRepository` has only `InsertAsync` and `QueryAsync`).
  - Distinguished application-level audit protections from database-level access controls.
- [`docs/design-system.md`](file:///d:/production/arise-craft/EduCore/docs/design-system.md)
  - Specified **Tailwind CSS** with custom design tokens (`primary`, `accent`, `surface`) and **Lucide React** icons.
  - Documented multi-role **Workspace Switcher** in the top navigation bar.
  - Defined the **Tabular Batch-Entry Pattern** for attendance and marks (local grid state, keyboard navigation, dirty-cell indicators, inline validation, atomic submission, and OCC conflict warning).
- [`docs/navigation.md`](file:///d:/production/arise-craft/EduCore/docs/navigation.md) & [`docs/screens.md`](file:///d:/production/arise-craft/EduCore/docs/screens.md)
  - Realigned role workspaces and navigation links.
  - Added student account activation screen (`SCR-002`) and batch entry screens (`SCR-403`, `SCR-405`).
- [`docs/workflows/admissions.md`](file:///d:/production/arise-craft/EduCore/docs/workflows/admissions.md)
  - Staff-entered applications only; decoupled payment from confirmation; multi-document provisioning transaction.
- [`docs/workflows/student-enrollment.md`](file:///d:/production/arise-craft/EduCore/docs/workflows/student-enrollment.md)
  - Term registration prerequisites; `RollNumber` assignment; atomic seat capacity conditional increment/decrement.
- [`docs/workflows/attendance.md`](file:///d:/production/arise-craft/EduCore/docs/workflows/attendance.md)
  - Embedded session roster; batch grid entry; audited correction workflow for closed registers.
- [`docs/workflows/fees-and-payments.md`](file:///d:/production/arise-craft/EduCore/docs/workflows/fees-and-payments.md)
  - Multi-document transactions for payments and reversals; FIFO allocation; overpayment rejection; idempotency replay safety.
- [`docs/workflows/examinations-and-results.md`](file:///d:/production/arise-craft/EduCore/docs/workflows/examinations-and-results.md)
  - Candidate eligibility snapshot (`examRegistrations`); marks state progression; separation of raw vs. moderated marks; publication isolation; audited amendments.
- [`docs/workflows/progression-and-graduation.md`](file:///d:/production/arise-craft/EduCore/docs/workflows/progression-and-graduation.md)
  - Dynamic backlog clearing; carry-over promotion rules; graduation clearance audit checklist.
- [`docs/open-decisions.md`](file:///d:/production/arise-craft/EduCore/docs/open-decisions.md)
  - Partitioned into resolved decisions DEC-01 through DEC-14 and genuinely deferred V2 roadmap questions.
- [`docs/implementation-plan.md`](file:///d:/production/arise-craft/EduCore/docs/implementation-plan.md)
  - Decomposed Phases 1 through 10 into 25 micro-vertical feature slices, each detailing objective, dependencies, allowed scope, expected behavior, business rules, acceptance criteria, automated tests, and completion conditions.

---

## 3. Decisions Applied and Reconciliations

1. **Scope Reconciled:** Applicant self-service removed from V1. Student Portal retained for authenticated enrolled students. Single institution (*Vidyadeep Institute of Technology*).
2. **Admission & Finance Decoupled:** Admission confirmation generates the student record and user activation token without requiring an immediate payment transaction.
3. **MongoDB Consistency Resolved:** Mandated a single-node replica set (`rs0`) for development and testing; embedded bounded payment allocations and attendance rosters; used multi-document transactions for ledger updates and student provisioning.
4. **Identifier System Formalized:** Atomic `$inc` sequence generator in `counters` collection; unique indexes on human-readable identifiers; non-gapless invariant explicitly documented.
5. **Exam & Marks Progression Defined:** 6-state marks lifecycle with separate `rawMarks` and `moderatedMarks` tracking; publication isolation from students; audited `resultAmendments`.
6. **Authentication & Styling Locked:** ASP.NET Core Cookie authentication with CSRF protection; Tailwind CSS with custom design tokens.

---

## 4. Modified or Refined Audit Recommendations

- **Audit Recommendation C-04 (Relational Decomposition vs Single-Document Atomicity):**  
  *Audit Suggested:* Embed allocations and receipts in payments; embed attendance roster in class sessions; avoid multi-document transactions.  
  *Approved Decision Refinement:* Embedded allocations/receipts in `payments` and rosters in `classSessions` for single-document aggregate atomicity, **and** mandated a single-node replica set (`rs0`) with multi-document transactions for the critical cross-collection operations (payment recording/reversal affecting `feeObligations`, and admission confirmation affecting `students` and `users`). Embedding allocations alone does not make updating external `feeObligations` atomic; transactions are strictly necessary and now formally specified.
- **Identifier Gapless Property:**  
  *Approved Refinement:* Made explicit that sequential identifiers guarantee **uniqueness** via atomic counters and unique indexes, but cannot guarantee zero numerical gaps when a transaction rolls back.

---

## 5. Remaining Questions (Deferred to V2)

1. Online payment gateway provider integration (Razorpay / PayU / BillDesk).
2. Physical file storage provider for applicant documents (S3 / MinIO / GridFS).
3. Student-initiated online revaluation and supplementary exam applications.
4. Algorithmic conflict-free timetable generation.
5. Multi-institution SaaS tenant provisioning and database isolation.
