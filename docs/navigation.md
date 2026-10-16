# Navigation and Role Workspaces

## 1. Shared Application Shell
- **Top Bar:** Contains institution brand (`Vidyadeep Institute of Technology`), page breadcrumbs, **Active Workspace Switcher**, system status badge, and user profile/logout dropdown.
- **Sidebar:** Contextual navigation links reflecting the currently selected Workspace (or the union of authorized areas for administrators).
- **Security Invariant:** Client-side navigation rendering is driven by granted user roles and permissions. The backend API independently validates all route accesses and data operations.

---

## 2. Role-Specific Workspaces

### 2.1 Workspace Switcher Mechanics
- When a user possesses multiple roles (e.g. `FACULTY` + `ACADEMIC_ADMIN`), the Workspace Switcher dropdown in the top bar allows one-click switching between functional workspaces.
- Switching workspace changes the sidebar menu to show the relevant operational tools, avoiding cognitive overload and 25+ link sidebars.

### 2.2 Workspace Navigation Menus

#### College Admin Workspace
- **Dashboard:** Overview metrics, active term status, system alerts.
- **Institution Settings:** Profile, departments, programs, policy configurations.
- **Academics:** Academic years, terms, course catalog.
- **User & Access Management:** User directory, role assignments, staff profiles.
- **Student Directory:** Searchable institutional student registry and profiles.
- **Audit Viewer:** Restricted, filterable system audit trail.

#### Admissions Workspace
- **Dashboard:** Application pipeline metrics (Draft, Submitted, Under Review, Approved).
- **Applications:** List, search, and filter applications by program and status.
- **Application Review:** Document verification checklist and eligibility panel.
- **Admission Confirmation:** Final verification and student record creation.

#### Academic Admin Workspace
- **Programs & Curricula:** Program structure, semester course allocations, credit requirements.
- **Terms & Offerings:** Term registration windows, course offerings, and section capacity.
- **Faculty Assignments:** Faculty course mapping and section timetable.
- **Student Enrollment:** Term registrations and student course enrollments.
- **Academic Progression:** Term progression rules, credit audits, and backlog monitoring.

#### Faculty Workspace
- **My Classes:** Assigned course offerings and class schedule.
- **Attendance Register:** Session attendance marking (batch grid mode) and register submission.
- **Attendance Corrections:** Submit correction requests for closed registers.
- **Assessment & Marks:** Continuous Internal Assessment (CIA) marks entry (batch grid mode).
- **Student Progress:** Class performance summaries and attendance status.

#### Examination Officer Workspace
- **Exam Calendar:** Examination schedule by term and offering.
- **Eligibility Evaluation:** Candidate eligibility snapshots (`examRegistrations`) and hall-ticket generation.
- **Marks Moderation Queue:** Review submitted faculty marks, apply authorized adjustments with reasons, and lock marks.
- **Result Publication:** Compile term results, preview grade distribution, and publish results.
- **Result Amendments:** Authorized, audited post-publication grade amendments.

#### Accounts Officer Workspace
- **Fee Structures:** Configure fee templates by program and academic term.
- **Student Fee Ledgers:** Individual student obligation balances and payment histories.
- **Payment Collection:** Record manual fee payments, calculate FIFO allocations, issue official receipts.
- **Receipts:** Search, view, and reprint issued receipts.
- **Adjustments & Concessions:** Record fee concessions, waivers, or scholarships.
- **Payment Reversals:** Audited reversal of dishonored cheques or erroneous payments.
- **Reconciliation:** Daily collection summaries and overdue obligation reports.

#### Student Portal (Personal Self-Service)
- **Overview:** Active term snapshot, attendance alert status, pending dues indicator.
- **My Profile:** Personal demographic, contact, and enrollment details.
- **My Timetable:** Weekly schedule for enrolled course offerings.
- **My Attendance:** Subject-wise percentage, session details, and condonation status.
- **My Fees & Receipts:** Outstanding fee obligations, payment history, and downloadable receipts.
- **My Exam Schedule:** Exam timetable and hall-ticket eligibility status.
- **My Published Results:** Official term grades, SGPA, CGPA, and earned credits.
- **My Academic Progress:** Degree completion status and backlog tracking.

---

## 3. UX Rules
- Never expose another student's information in student navigation or client state.
- Keep primary actions contextual: table rows link directly to detail tabs rather than nested drawer chains.
- Consequential actions (reversals, amendments, dismissals) require confirmation dialogs with mandatory reason fields.
