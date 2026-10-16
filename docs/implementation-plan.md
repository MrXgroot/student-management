# Phased Implementation Plan — Micro-Vertical Slices

Every task in EduCore follows the **vertical-slice rule**: an implementation unit includes its UI, API, domain policy/validation, persistence/repository, automated tests, and documentation. Never build purely horizontal layers (e.g. all backend tables first).

---

## Phase 0: Project Bible & Decisions (Current Phase)
- **Status:** Complete / Approved.
- **Outcome:** Consistent, unambiguous specification with approved architectural decisions (ADR-001, ADR-002, ADR-003) and concrete demo policies.
- **Exit Condition:** Review and validation reports approved; Project Bible declared ready for foundation.

---

## Phase 1: Repository Foundation & Core Infrastructure

### Slice 1A: Backend Solution Setup
- **Objective & Dependencies:** Initialize `backend/EduCore.sln` with 4 source projects (`Api`, `Application`, `Domain`, `Infrastructure`) and 2 test projects (`UnitTests`, `IntegrationTests`). No functional dependencies.
- **Allowed Scope:** Project files, NuGet packages (MongoDB.Driver, FluentValidation, Serilog.AspNetCore), dependency injection setup, and string ObjectId BSON convention.
- **Expected UI/API/Data Behavior:** `GET /health` returns `{ "status": "Healthy" }`. Centralized exception filter returns RFC 7807 `ProblemDetails`. OpenAPI Swagger UI accessible at `/swagger`.
- **Business Rules & Auth:** No business logic; environment-based configuration; secrets loaded from user secrets/env vars.
- **Acceptance Criteria:** `dotnet build` succeeds with zero warnings; Swagger renders cleanly.
- **Automated Tests:** Unit test verifying exception handling; integration test verifying health endpoint.
- **Completion Condition:** Clean solution builds and tests pass via CLI.

### Slice 1B: Frontend Project Setup
- **Objective & Dependencies:** Initialize `frontend/` using Vite with React (plain JavaScript only). Depends on Slice 1A.
- **Allowed Scope:** Tailwind CSS setup with custom design tokens (`primary`, `accent`, `surface`), Lucide React icons, TanStack Query provider, and centralized API client (`shared/api/client.js`) with CSRF header injection.
- **Expected UI/API/Data Behavior:** Vite dev server starts instantly; displays baseline application shell layout; API client verifies connection to backend `/health`.
- **Business Rules & Auth:** Plain JS only; no TypeScript.
- **Acceptance Criteria:** `npm run build` and `npm run lint` succeed; Tailwind utility classes render accurately.
- **Automated Tests:** Vitest component test verifying API client error normalization.
- **Completion Condition:** Frontend builds cleanly and connects to backend.

### Slice 1C: Atomic Counters & Index Startup Infrastructure
- **Objective & Dependencies:** Implement the `counters` collection, `ISequenceGeneratorService`, and the application startup index configurator. Depends on Slice 1A.
- **Allowed Scope:** `counters` repository with atomic `$inc`, startup index creation assertions, and single-node replica set test harness.
- **Expected UI/API/Data Behavior:** Backend startup ensures all unique and compound indexes exist in MongoDB; sequence generator atomically returns unique formatted numbers (`ADM-YYYY-00001`, `REC-YYYY-00001`).
- **Business Rules & Auth:** Sequence numbers are atomic; gaps are permitted on rollback; uniqueness guaranteed by unique indexes.
- **Acceptance Criteria:** Multi-threaded counter increment test produces 100% unique sequence values under concurrency.
- **Automated Tests:** Unit tests for counter formatting; integration tests verifying MongoDB index creation and concurrent sequence generation.
- **Completion Condition:** Index initializer and sequence service verified on replica set.

---

## Phase 2: Authentication & User Management

### Slice 2A: Authentication Core & Seeded Users
- **Objective & Dependencies:** Implement ASP.NET Core Cookie authentication, password hashing, and user credential verification. Depends on Phase 1.
- **Allowed Scope:** `POST /api/v1/auth/login`, `POST /api/v1/auth/logout`, `GET /api/v1/auth/csrf`, Antiforgery middleware, and repeatable seed script generating demo users for all 7 roles.
- **Expected UI/API/Data Behavior:** Login returns `200 OK` with `.EduCore.Auth` (HttpOnly) and `XSRF-TOKEN` cookies. Mutating requests reject missing/invalid CSRF tokens.
- **Business Rules & Auth:** Passwords hashed with `PasswordHasher<User>`; no plaintext passwords logged or stored; sliding expiration 60 min.
- **Acceptance Criteria:** Unauthenticated requests to protected endpoints return `401 Unauthorized`; invalid credentials return `400 Bad Request`.
- **Automated Tests:** Unit tests for password hashing; integration tests verifying login, cookie issuance, and CSRF rejection.
- **Completion Condition:** Auth endpoints and seeded demo accounts operational.

### Slice 2B: Application Shell & Workspace Switcher
- **Objective & Dependencies:** Build the frontend application shell, login screen (`SCR-001`), and multi-role Workspace Switcher. Depends on Slice 2A and Slice 1B.
- **Allowed Scope:** Login screen, top navigation bar, Workspace Switcher dropdown, role-specific sidebar menus, and logout trigger.
- **Expected UI/API/Data Behavior:** Logging in stores user claims in React auth context; multi-role users can toggle workspaces; sidebar updates dynamically.
- **Business Rules & Auth:** UI navigation reflects active workspace; backend continues to enforce role claims authoritatively.
- **Acceptance Criteria:** Switching from "Faculty" to "Academic Admin" workspace instantly re-renders sidebar; logout clears session and redirects to `/login`.
- **Automated Tests:** Component tests for Workspace Switcher and protected route navigation guard.
- **Completion Condition:** User can log in, switch workspaces, and log out via UI.

### Slice 2C: Student Account Activation Handshake
- **Objective & Dependencies:** Implement student account activation token flow and setup screen (`SCR-002`). Depends on Slice 2A and Slice 2B.
- **Allowed Scope:** `POST /api/v1/auth/activate` endpoint and `/activate?token=...` UI view.
- **Expected UI/API/Data Behavior:** Student validates identity, enters activation token, and sets new password; status transitions `PENDING_ACTIVATION` -> `ACTIVE`.
- **Business Rules & Auth:** Tokens expire after 72 hours; tokens are hashed; first login forces password reset.
- **Acceptance Criteria:** Expired or mismatched token is rejected; successful activation allows immediate login with new password.
- **Automated Tests:** Integration test verifying token validation and password change.
- **Completion Condition:** Student onboarding activation works end-to-end.

---

## Phase 3: Institution & Academic Setup

### Slice 3A: Institution & Department Management
- **Objective & Dependencies:** Implement institution profile and department setup (`SCR-102`, `SCR-103`). Depends on Phase 2.
- **Allowed Scope:** `/api/v1/institution`, `/api/v1/departments` CRUD endpoints and Admin settings UI.
- **Expected UI/API/Data Behavior:** Admin can view/edit college settings and create/deactivate departments.
- **Business Rules & Auth:** Only `COLLEGE_ADMIN` can edit; department codes must be unique within the institution.
- **Acceptance Criteria:** Unique constraint prevents duplicate department codes; optimistic concurrency prevents lost updates.
- **Automated Tests:** Repository and API authorization tests.
- **Completion Condition:** Department catalog fully manageable.

### Slice 3B: Programs & Embedded Curriculum Mapping
- **Objective & Dependencies:** Implement degree programs and embedded curriculum course mappings (`SCR-104`). Depends on Slice 3A.
- **Allowed Scope:** `/api/v1/programs` CRUD and Program setup UI with semester curriculum editor.
- **Expected UI/API/Data Behavior:** Define program award type, duration, total semesters, and assign courses to semesters with core/elective flags.
- **Business Rules & Auth:** Unique program code; semester numbers bounded by program duration; authorized for `COLLEGE_ADMIN` and `ACADEMIC_ADMIN`.
- **Acceptance Criteria:** Adding duplicate course to same semester is blocked; curriculum structure persists accurately.
- **Automated Tests:** Unit tests for curriculum model validation; integration tests for program persistence.
- **Completion Condition:** Program and curriculum structure operational.

### Slice 3C: Academic Years & Terms
- **Objective & Dependencies:** Implement academic year calendar and term registration windows (`SCR-105`). Depends on Slice 3A.
- **Allowed Scope:** `/api/v1/academic-years`, `/api/v1/terms` CRUD and Calendar UI.
- **Expected UI/API/Data Behavior:** Academic Admin can create academic years, add terms (Odd/Even), and open/close registration windows.
- **Business Rules & Auth:** Date boundaries validated (startDate < endDate); term codes unique within academic year.
- **Acceptance Criteria:** Active term registration window can be opened/closed via API and UI.
- **Automated Tests:** Domain validation tests for calendar dates.
- **Completion Condition:** Academic terms configurable.

### Slice 3D: Course Catalog Management
- **Objective & Dependencies:** Implement course catalog and prerequisite definitions (`SCR-106`). Depends on Slice 3A.
- **Allowed Scope:** `/api/v1/courses` CRUD and Course catalog UI.
- **Expected UI/API/Data Behavior:** Create courses with code, title, credits, and prerequisite course selections.
- **Business Rules & Auth:** Course codes unique per institution; prerequisite course cannot be the course itself (no cyclic prerequisites).
- **Acceptance Criteria:** Prerequisite validation blocks self-reference; credit values must be positive integers.
- **Automated Tests:** Unit tests for prerequisite graph cycles.
- **Completion Condition:** Course catalog operational.

---

## Phase 4: Admissions & Student Records

### Slice 4A: Application Intake & Document Verification
- **Objective & Dependencies:** Implement staff application submission and document checklist verification (`SCR-202`, `SCR-203`). Depends on Phase 3.
- **Allowed Scope:** `/api/v1/admissions/applications` endpoints and Admissions Officer UI.
- **Expected UI/API/Data Behavior:** Staff enters applicant details; system generates `APP-{YYYY}-{00000}`; staff reviews and checks off documents.
- **Business Rules & Auth:** Staff-only entry in V1; duplicate detection warning; document checklist records verifier ID and timestamp.
- **Acceptance Criteria:** Submitting application sets state to `SUBMITTED`; verifications update checklist atomically.
- **Automated Tests:** Policy tests for application validation and duplicate detection.
- **Completion Condition:** Applications can be submitted and verified.

### Slice 4B: Admission Decision & Transactional Confirmation
- **Objective & Dependencies:** Implement admission eligibility checks, approval/rejection dialog (`SCR-204`), and transactional admission confirmation (`SCR-205`). Depends on Slice 4A and Slice 1C.
- **Allowed Scope:** `/api/v1/admissions/applications/{id}/decision`, `/api/v1/admissions/applications/{id}/confirm`.
- **Expected UI/API/Data Behavior:** Confirmation triggers MongoDB multi-document transaction creating `Student` (`ADMITTED`), `User` (`PENDING_ACTIVATION`), sequential `AdmissionNumber`, and prints admission slip.
- **Business Rules & Auth:** In V1, admission confirmation is decoupled from payment; requires `ADMISSIONS_OFFICER` or `COLLEGE_ADMIN`; transaction rollback on failure.
- **Acceptance Criteria:** Confirming admission creates student and user accounts atomically; duplicate confirmation returns `409 Conflict`.
- **Automated Tests:** Integration tests verifying replica set transaction commit and rollback behaviors.
- **Completion Condition:** End-to-end admission to student onboarding verified.

### Slice 4C: Student Directory & Master Profile
- **Objective & Dependencies:** Implement searchable student directory and student profile view (`SCR-108`). Depends on Slice 4B.
- **Allowed Scope:** `/api/v1/students` query endpoints and Directory UI with filters (program, semester, lifecycle status).
- **Expected UI/API/Data Behavior:** High-performance paginated table; row click navigates to tabbed student master profile.
- **Business Rules & Auth:** Admissions Officer sees admitted; Academic Admin and College Admin have full view; Faculty restricted to assigned classes.
- **Acceptance Criteria:** Pagination and search by admission number, roll number, or name work with sub-second response times.
- **Automated Tests:** Query index tests and authorization boundary tests.
- **Completion Condition:** Student directory operational.

### Slice 4D: Student Lifecycle Management
- **Objective & Dependencies:** Implement lifecycle status transitions (`PROBATION`, `SUSPENDED`, `ON_LEAVE`, `WITHDRAWN`, `DISCONTINUED`). Depends on Slice 4C.
- **Allowed Scope:** `/api/v1/students/{id}/lifecycle-status` endpoint and status update modal.
- **Expected UI/API/Data Behavior:** Authorized admin changes status with mandatory reason; updates student document under optimistic concurrency and logs audit event.
- **Business Rules & Auth:** Transitions strictly adhere to `StudentLifecyclePolicy`; only `COLLEGE_ADMIN` or `ACADEMIC_ADMIN` authorized.
- **Acceptance Criteria:** Invalid transitions (e.g. `WITHDRAWN` -> `ACTIVE`) are rejected with `400 Bad Request`.
- **Automated Tests:** Unit tests covering all permitted and forbidden state transitions.
- **Completion Condition:** Student lifecycle transitions audited and enforced.

---

## Phase 5: Term Registration & Course Enrollment

### Slice 5A: Cohort Term Registration & Roll Number Assignment
- **Objective & Dependencies:** Implement term registration for eligible students (`SCR-302`). Depends on Phase 4.
- **Allowed Scope:** `/api/v1/enrollments/terms/register` and Term registration UI.
- **Expected UI/API/Data Behavior:** Academic Admin registers students for upcoming term; for Semester 1, generates institutional `RollNumber` and moves status to `ACTIVE`.
- **Business Rules & Auth:** Student must be `ADMITTED`, `ACTIVE`, or `PROBATION`; term registration window must be open; exactly one registration per student per term.
- **Acceptance Criteria:** Duplicate registration rejected with `409 Conflict`; roll number assigned accurately.
- **Automated Tests:** Integration test for registration idempotency and roll number generation.
- **Completion Condition:** Cohort term registration operational.

### Slice 5B: Course Offerings & Faculty Assignment
- **Objective & Dependencies:** Implement term course offerings, sections, faculty mapping, and capacity limits (`SCR-303`). Depends on Phase 3 and Phase 5A.
- **Allowed Scope:** `/api/v1/course-offerings` CRUD and Offerings management UI.
- **Expected UI/API/Data Behavior:** Create section offerings (Section A, B) with room, timetable schedule, capacity, and assigned faculty.
- **Business Rules & Auth:** Offering section unique per course per term; capacity must be positive integer.
- **Acceptance Criteria:** Faculty timetable view reflects assigned offerings accurately.
- **Automated Tests:** Unit tests for schedule conflict validation.
- **Completion Condition:** Course offerings and sections ready for enrollment.

### Slice 5C: Course Enrollment & Atomic Capacity Enforcement
- **Objective & Dependencies:** Implement student course enrollment with atomic seat capacity checks (`SCR-304`). Depends on Slice 5B.
- **Allowed Scope:** `/api/v1/enrollments/courses/enroll`, `/api/v1/enrollments/courses/withdraw`.
- **Expected UI/API/Data Behavior:** Enroll students in offerings; UI displays live capacity bar; withdrawals decrement count.
- **Business Rules & Auth:** Atomic seat decrement; prerequisite verification; withdrawal preserves history and sets status `WITHDRAWN`.
- **Acceptance Criteria:** Concurrent requests cannot exceed section capacity.
- **Automated Tests:** Multi-threaded integration test verifying section capacity limits under concurrency.
- **Completion Condition:** Course enrollment operational.

---

## Phase 6: Attendance Management

### Slice 6A: Class Sessions & Batch Attendance Register
- **Objective & Dependencies:** Implement scheduled class sessions and the tabular batch attendance register (`SCR-402`, `SCR-403`). Depends on Phase 5.
- **Allowed Scope:** `GET /api/v1/attendance/sessions`, `PUT /api/v1/attendance/sessions/{id}/roster` and Faculty attendance grid UI.
- **Expected UI/API/Data Behavior:** Faculty opens session; roster loads; keyboard shortcuts (P/A/L/E) mark status; dirty indicators highlight modifications; atomic single-document submission.
- **Business Rules & Auth:** Faculty can mark only assigned sessions; session register embedded in `classSessions.roster`; optimistic concurrency enforced.
- **Acceptance Criteria:** Submitting 60 student statuses persists in a single atomic update; version mismatch returns `409 Conflict`.
- **Automated Tests:** Integration tests for batch roster persistence and OCC collisions.
- **Completion Condition:** Faculty can mark and submit attendance registers.

### Slice 6B: Attendance Corrections & Percentage Reports
- **Objective & Dependencies:** Implement audited attendance corrections and percentage calculations (`SCR-404`, `SCR-406`). Depends on Slice 6A.
- **Allowed Scope:** `/api/v1/attendance/sessions/{id}/corrections`, `/api/v1/attendance/reports/offering/{id}`.
- **Expected UI/API/Data Behavior:** Correction modal appends correction record without overwriting history; reports calculate percentages with 75% threshold indicators.
- **Business Rules & Auth:** Weightings: Present = 1.0, Late = 0.5, Excused excluded from denominator; corrections require mandatory reason and audit entry.
- **Acceptance Criteria:** Condonation alerts highlight students between 65.0% and 74.9%; calculations exact to 2 decimal places.
- **Automated Tests:** Unit tests for attendance formulas and edge cases (zero sessions, cancelled sessions).
- **Completion Condition:** Attendance reporting and audited corrections verified.

---

## Phase 7: Fees and Financial Ledgers

### Slice 7A: Fee Structures & Obligation Generation
- **Objective & Dependencies:** Implement fee structure templates and automatic student obligation assessment (`SCR-602`). Depends on Phase 4 and Phase 5.
- **Allowed Scope:** `/api/v1/fees/structures`, `/api/v1/fees/obligations/generate` and Fee setup UI.
- **Expected UI/API/Data Behavior:** Accounts staff creates fee structure by program/term; obligations generated for registered students with integer paise amounts.
- **Business Rules & Auth:** Amounts stored as integer paise; categories: `TUITION`, `LAB`, `LIBRARY`, `EXAM`, `MISC`; obligations linked to term and student.
- **Acceptance Criteria:** Obligation generation is repeat-safe (idempotent); no floating-point arithmetic.
- **Automated Tests:** Unit tests for paise calculation and obligation generation.
- **Completion Condition:** Student fee obligations assessed.

### Slice 7B: Payment Collection & Receipt Issuance
- **Objective & Dependencies:** Implement manual payment recording, FIFO allocation, and official receipt generation (`SCR-604`, `SCR-605`). Depends on Slice 7A and Slice 1C.
- **Allowed Scope:** `POST /api/v1/payments`, `GET /api/v1/payments/receipts/{id}` and Payment collection UI.
- **Expected UI/API/Data Behavior:** Enter payment amount; server calculates FIFO allocations (or authorized manual override); executes multi-document transaction; issues printable receipt.
- **Business Rules & Auth:** Overpayments rejected with `EXCESS_PAYMENT_NOT_PERMITTED`; `X-Idempotency-Key` prevents replay; sequential `REC-YYYY-00001` generated.
- **Acceptance Criteria:** Transaction updates `payments` and `feeObligations` atomically; partial payments reduce outstanding balance accurately.
- **Automated Tests:** Integration test verifying transaction commit, idempotency replay, and overpayment rejection.
- **Completion Condition:** Fee collection and receipting operational.

### Slice 7C: Financial Adjustments & Concessions
- **Objective & Dependencies:** Implement approved fee concessions, scholarships, and waivers (`SCR-606`). Depends on Slice 7A.
- **Allowed Scope:** `/api/v1/fees/adjustments` and Concession modal.
- **Expected UI/API/Data Behavior:** Record concession against student obligation; reduces outstanding balance; logs approver and reason.
- **Business Rules & Auth:** Concessions require `ACCOUNTS_OFFICER` or `COLLEGE_ADMIN`; cannot exceed obligation balance; audited.
- **Acceptance Criteria:** Concession adjusts obligation balance and prevents over-allocation.
- **Automated Tests:** Unit tests for adjustment calculation and validation.
- **Completion Condition:** Fee adjustments operational.

### Slice 7D: Payment Reversal Workflow
- **Objective & Dependencies:** Implement transactional payment reversals for bounced cheques or entry errors (`SCR-607`). Depends on Slice 7B.
- **Allowed Scope:** `POST /api/v1/payments/{id}/reverse` and Reversal modal.
- **Expected UI/API/Data Behavior:** Reversal transaction marks payment `REVERSED`, receipt `CANCELLED`, voids allocations, restores obligation balances, and appends audit event.
- **Business Rules & Auth:** Payments are never hard-deleted; mandatory reason required; executed within a MongoDB transaction.
- **Acceptance Criteria:** Reversal restores exact previous outstanding balances on affected obligations; receipt watermark reflects `CANCELLED`.
- **Automated Tests:** Integration test verifying reversal rollback of multiple obligations and audit trail creation.
- **Completion Condition:** Payment reversals verified.

---

## Phase 8: Examinations & Results

### Slice 8A: Assessment Scheduling & Eligibility Evaluation
- **Objective & Dependencies:** Implement assessment configuration and candidate eligibility evaluation (`SCR-502`, `SCR-503`). Depends on Phase 6 and Phase 7.
- **Allowed Scope:** `/api/v1/assessments`, `/api/v1/exams/eligibility/evaluate`, and Eligibility UI.
- **Expected UI/API/Data Behavior:** Exam Officer schedules assessments; runs eligibility check; creates `examRegistrations` snapshot; grants condonations; issues hall-tickets.
- **Business Rules & Auth:** Eligibility requires attendance $\ge 75\%$ (or approved condonation $65-74.9\%$) and cleared fee obligations; hall-ticket blocked if ineligible.
- **Acceptance Criteria:** Ineligible students clearly flagged with reason codes (`ATTENDANCE_SHORTAGE`, `FEE_DUES`).
- **Automated Tests:** Policy tests for eligibility rules and condonation overrides.
- **Completion Condition:** Exam eligibility evaluation operational.

### Slice 8B: Batch Marks Entry & Moderation State Machine
- **Objective & Dependencies:** Implement tabular marks entry and the Exam Officer moderation queue (`SCR-405`, `SCR-504`). Depends on Slice 8A.
- **Allowed Scope:** `/api/v1/assessments/{id}/marks/batch`, `/api/v1/assessments/{id}/marks/moderate`, `/api/v1/assessments/{id}/lock`.
- **Expected UI/API/Data Behavior:** Faculty enters marks in batch grid; locks register; Exam Officer reviews moderation queue; adjustments preserve `rawMarks` and record `moderatedMarks` with reason.
- **Business Rules & Auth:** Marks bounded by `maxMarks`; raw marks never overwritten; moderation requires justification; locked marks cannot be edited.
- **Acceptance Criteria:** Moderation records both raw and adjusted marks; locked assessment rejects edits.
- **Automated Tests:** Unit tests for marks validation; integration tests for state machine progression (`DRAFT` -> `SUBMITTED` -> `MODERATED` -> `LOCKED`).
- **Completion Condition:** Marks entry and moderation queue operational.

### Slice 8C: Result Compilation & Official Publication
- **Objective & Dependencies:** Implement result compilation (40/60 weighting, 10-point scale, pass rules) and official publication (`SCR-505`). Depends on Slice 8B.
- **Allowed Scope:** `/api/v1/results/compile`, `/api/v1/results/publish` and Publication preview UI.
- **Expected UI/API/Data Behavior:** Exam Officer compiles results; reviews grade distribution preview; approves and publishes.
- **Business Rules & Auth:** Minimum pass requires $\ge 40\%$ in SEE and $\ge 40\%$ aggregate; results hidden from students until `status == "PUBLISHED"`; publication is repeat-safe.
- **Acceptance Criteria:** Grade point, SGPA, and earned credits calculated accurately according to CBCS rules.
- **Automated Tests:** Unit tests for grading calculations and pass/fail boundaries; integration test for publication isolation.
- **Completion Condition:** Result publication operational.

### Slice 8D: Result Amendment Workflow
- **Objective & Dependencies:** Implement audited post-publication grade amendments (`SCR-506`). Depends on Slice 8C.
- **Allowed Scope:** `POST /api/v1/results/{id}/amend` and Amendment dialog.
- **Expected UI/API/Data Behavior:** Authorized officer submits amendment with mandatory reason; updates result document under OCC; appends `resultAmendments` record and audit log.
- **Business Rules & Auth:** Published results cannot be directly overwritten; amendment requires `COLLEGE_ADMIN` or Dean approval; recalculates SGPA/CGPA.
- **Acceptance Criteria:** Amendment preserves previous marks and grade in amendment history.
- **Automated Tests:** Integration test for result amendment audit logging and SGPA recalculation.
- **Completion Condition:** Audited result amendments verified.

---

## Phase 9: Academic Progression & Student Portal

### Slice 9A: Academic Progression & Graduation Clearance
- **Objective & Dependencies:** Implement term progression evaluation, backlog clearing, and graduation clearance audit (`SCR-306`). Depends on Phase 8.
- **Allowed Scope:** `/api/v1/progression/evaluate/{termId}`, `/api/v1/progression/graduation-audit/{studentId}` and Progression UI.
- **Expected UI/API/Data Behavior:** System evaluates cohort progression; flags students with $> 4$ backlogs for `PROBATION`; graduation audit checks compulsory curriculum and earned credits.
- **Business Rules & Auth:** Passing a repeat course clears active backlog while retaining attempt history; graduation requires 0 active backlogs, complete credits, and zero fee dues.
- **Acceptance Criteria:** Graduation audit returns actionable checklist of unmet requirements when ineligible.
- **Automated Tests:** Unit tests for backlog carry-over rules and graduation audit logic.
- **Completion Condition:** Progression and graduation evaluation operational.

### Slice 9B: Student Portal Implementation
- **Objective & Dependencies:** Implement dedicated student self-service endpoints and views (`SCR-701` through `SCR-708`). Depends on Phase 6, Phase 7, and Phase 8.
- **Allowed Scope:** `/api/v1/students/me/...` endpoints and Student Portal UI (Dashboard, Profile, Timetable, Attendance, Fees/Receipts, Exams, Published Results, Progress).
- **Expected UI/API/Data Behavior:** Authenticated student accesses personal dashboard; can download receipts and view official published grades.
- **Business Rules & Auth:** Derived identity strictly from cookie claims; attempts to query other student IDs return `403 Forbidden`; draft marks hidden.
- **Acceptance Criteria:** Student sees only personal records; responsive on mobile and tablet screens.
- **Automated Tests:** Security integration tests verifying cross-student access is strictly blocked.
- **Completion Condition:** Student Portal fully functional and verified.

---

## Phase 10: Hardening, Polish & Release

### Slice 10A: Security Verification & Audit Trail Inspection
- **Objective & Dependencies:** Perform end-to-end security audits, rate-limiting verification, and audit trail inspection. Depends on all previous phases.
- **Allowed Scope:** Rate limiting middleware on `/auth`, security headers, audit repository verification, and end-to-end integration test suite.
- **Expected UI/API/Data Behavior:** App responds with strict security headers; audit timeline shows full history of consequential changes.
- **Business Rules & Auth:** Audit logs are append-only; no sensitive tokens or passwords in logs.
- **Acceptance Criteria:** Full automated test suite passes; no critical security vulnerabilities.
- **Automated Tests:** Complete regression test run.
- **Completion Condition:** Security and quality gates pass.

### Slice 10B: Demo Packaging & Operational Documentation
- **Objective & Dependencies:** Package application for demonstration and document setup. Depends on Slice 10A.
- **Allowed Scope:** Docker Compose configuration (MongoDB single-node replica set `rs0` + backend API + frontend), idempotent seed script runner, and setup README.
- **Expected UI/API/Data Behavior:** Running `docker compose up` brings up complete environment with seeded demo data for *Vidyadeep Institute of Technology*.
- **Business Rules & Auth:** Seed data strictly fictional Indian demo records; idempotent execution.
- **Acceptance Criteria:** Clean git checkout can be configured, built, and tested from documentation in under 10 minutes.
- **Automated Tests:** Seed idempotency verification test (running seed script twice produces zero duplicate key errors).
- **Completion Condition:** Project V1 ready for demonstration.
