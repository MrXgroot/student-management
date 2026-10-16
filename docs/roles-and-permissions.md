# Roles and Permissions

Authorization must be enforced authoritative on the backend. Hiding a button or menu item in the frontend is a UX aid, never security.

## Roles
EduCore defines seven standard administrative and academic roles:
- `COLLEGE_ADMIN`: System-wide oversight, institution configuration, user/role management, and emergency policy overrides.
- `ADMISSIONS_OFFICER`: Intake management, application review, eligibility checking, admission decisions, and admission confirmation.
- `ACADEMIC_ADMIN`: Department/program structure, term configuration, course offerings, faculty assignments, term registrations, and progression oversight.
- `FACULTY`: Assigned class session management, attendance register entry/corrections, and continuous internal assessment (CIA) marks entry.
- `EXAM_OFFICER`: Examination scheduling, candidate eligibility evaluation, marks moderation queue, result publication, and official result amendments.
- `ACCOUNTS_OFFICER`: Fee structure setup, student fee obligations, manual payment collection, receipt issuance, payment reversals, and adjustments.
- `STUDENT`: Authenticated student portal access to view personal profile, timetable, attendance, fee obligations/receipts, exam schedule, published results, and progress.

A user may hold multiple roles simultaneously (e.g., a Senior Professor who is both `FACULTY` and `ACADEMIC_ADMIN`). Permissions are additive across granted roles.

---

## Permission Matrix (V1)

| Capability / Workflow | Admin | Admissions | Academic Admin | Faculty | Exam Officer | Accounts | Student |
|---|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| **Institution Configuration** | Full | No | View / Limited | No | No | No | No |
| **Manage Users & Role Assignment** | Full | No | No | No | No | No | No |
| **Applications & Decisions** | Full | Full | Read | No | No | No | No (V2) |
| **Confirm Admission & Create Student** | Full | Full | No | No | No | No | No |
| **Student Directory & Profiles** | Full | View Admitted | Full | Assigned Classes Only | Read as needed | Fee Ledgers Only | Own Record Only |
| **Academic Setup (Programs/Terms/Courses)** | Full | Read | Full | Read Assigned | Read | Read | Read Relevant |
| **Course Offerings & Section Capacity** | Full | Read | Full | Read Assigned | Read | Read | No |
| **Term Registration & Course Enrollment** | Full | No | Full | No | No | No | No (V1 Staff-managed) |
| **Attendance Register Entry** | Oversight | No | Oversight | Assigned Sessions | Read | No | Own View Only |
| **Attendance Corrections** | Override w/ Audit | No | Authorized Approval | Request/Submit w/ Reason | No | No | No |
| **Fee Structure & Obligations** | Oversight | No | Read Limited | No | No | Full | Own View Only |
| **Payment Collection & Receipts** | Oversight | No | No | No | No | Full | Own View Only |
| **Payment Reversal / Adjustments** | Override w/ Audit | No | No | No | No | Full w/ Reason & Audit | No |
| **Exam Setup & Eligibility** | Oversight | No | Academic Data | Read Assigned | Full | No | Own Schedule Only |
| **Marks Entry (CIA / Exam)** | Override w/ Audit | No | No | Assigned Assessments | Oversight / Moderation | No | No |
| **Marks Moderation & Lock** | Oversight | No | No | No | Full w/ Reason & Audit | No | No |
| **Result Publication** | Oversight | No | No | No | Full w/ Authorization | No | Published Results Only |
| **Result Amendments** | Override w/ Audit | No | No | No | Full w/ Reason & Audit | No | Published Results Only |
| **Audit Log Viewing** | Full System | Admissions Scope | Academic Scope | Assigned Scope | Exam Scope | Finance Scope | No Access |

---

## Core Authorization Controls

### 1. Multi-Role Handling and Workspace Context
- When a user has multiple assigned roles, the frontend renders a **Workspace Switcher** in the top navigation bar.
- Selecting a workspace switches the active UI view (e.g., "Faculty Workspace" vs. "Academic Admin Workspace") to keep the interface focused and uncluttered.
- However, API-level authorization evaluates the union of all claims attached to the authenticated session identity. The server authorizes requests based on granted roles, not the UI workspace view.

### 2. Student Portal Identity Derivation
- Student portal endpoints (`/api/v1/students/me/...`, `/api/v1/attendance/my-summary`, `/api/v1/fees/my-ledger`, `/api/v1/results/my-published`) strictly derive the student identifier from the authenticated cookie identity claims (`studentId` claim).
- Endpoints must **never** accept or trust a `studentId` supplied in query strings or request bodies from student-authenticated sessions. Attempts to access another student's record immediately return `403 Forbidden`.

### 3. Separation of Duty and Consequential Actions
- **Self-Elevation Block:** A user cannot grant themselves new roles or edit their own administrative permissions.
- **Marks Moderation & Amendment:** Exam Officers cannot edit marks without recording a structured moderation reason, which is permanently logged alongside the original raw mark.
- **Payment Reversals:** Accounts Officers can reverse payments only with a documented reason; the reversal operates in an audited multi-document transaction and voids the associated receipt.
- **Default Deny:** Endpoints require explicit authorization policies. Unannotated routes fail closed.
