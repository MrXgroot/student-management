# Screen Inventory Specification

Build screens only when their corresponding backend API contracts, policies, and workflows are implemented.

---

## 1. Shared Screens
- `SCR-001` **Login:** Username/email and password entry, authentication error messaging, redirect to designated workspace.
- `SCR-002` **Student Account Activation (`/activate`):** Token validation, date of birth / admission number verification, and initial password setup.
- `SCR-003` **Access Denied (403) & Not Found (404):** Contextual guidance and safe return routes.
- `SCR-004` **User Profile & Security Settings:** Personal account information, password change, active sessions.
- `SCR-005` **Application Shell:** Responsive layout, top header with Workspace Switcher, contextual sidebar, breadcrumbs, user status.

---

## 2. College Admin Screens
- `SCR-101` **Institution Dashboard:** Metric overview (total students, active offerings, attendance averages, fee collection percentage).
- `SCR-102` **Institution Settings:** College profile, address, timezone, and configurable demo policy thresholds.
- `SCR-103` **Department Management:** Department catalog, HOD assignments, status toggles.
- `SCR-104` **Program Management:** Degree programs, duration, and embedded curriculum course mapping.
- `SCR-105` **Academic Calendar & Terms:** Academic year setup, term date boundaries, registration windows.
- `SCR-106` **Course Catalog:** Course repository, prerequisite graph, credit assignments.
- `SCR-107` **User & Access Management:** User list, role assignments, staff profile links, account status management.
- `SCR-108` **Student Directory & Master Profile:** Master directory with advanced filters (program, semester, status); student profile view.
- `SCR-109` **Audit Viewer:** Restricted, immutable audit timeline with actor, action, and target filters.

---

## 3. Admissions Officer Screens
- `SCR-201` **Admissions Dashboard:** Intake funnel statistics by program.
- `SCR-202` **Applications List:** Filterable table by program, status, submission date, and search.
- `SCR-203` **Application Detail & Document Checklist:** Comprehensive applicant history, qualification review, and document verification checklist.
- `SCR-204` **Admission Decision Dialog:** Approve, reject, request corrections with mandatory reason.
- `SCR-205` **Admission Confirmation & Onboarding:** Issue official admission number, create student record, generate activation token slip.

---

## 4. Academic Admin Screens
- `SCR-301` **Academic Administration Dashboard:** Term registration progress, class capacity warnings.
- `SCR-302` **Term Registration Management:** Open/close registration windows, bulk term registration for cohorts.
- `SCR-303` **Course Offerings & Section Capacity:** Setup term offerings, sections, faculty assignments, schedule, and seat capacity limits.
- `SCR-304` **Student Course Enrollment:** Enroll students in offerings, enforce prerequisite and capacity invariants.
- `SCR-305` **Timetable Grid:** Program and section weekly schedule visualizer.
- `SCR-306` **Academic Progression & Backlog Overview:** Student progression status by cohort, credit tracking, backlog reports.

---

## 5. Faculty Screens
- `SCR-401` **Faculty Dashboard / My Classes:** Weekly timetable and quick-action links to upcoming class sessions.
- `SCR-402` **Class Session Details:** Topic, date/time, and enrolled student roster.
- `SCR-403` **Attendance Register (Batch Grid Mode):** High-density tabular grid with keyboard navigation, dirty-cell markers, status buttons (P/A/L/E), and atomic submission.
- `SCR-404` **Attendance Correction Request:** Request modal for closed registers with reason capture.
- `SCR-405` **Internal Assessment Marks Entry (Batch Grid Mode):** Tabular marks grid, inline range validation (0 to maxMarks), dirty cell markers, and submission lock.
- `SCR-406` **Class Performance Summary:** Class attendance percentages, at-risk student warnings (< 75%).

---

## 6. Examination Officer Screens
- `SCR-501` **Examination Dashboard:** Term exam schedules, marks submission status across offerings.
- `SCR-502` **Exam & Assessment Configuration:** Schedule CIA and SEE assessment dates, maximum marks, and weightings.
- `SCR-503` **Candidate Eligibility & Hall-Ticket List:** Attendance percentage and fee clearance evaluation snapshots (`examRegistrations`), condonation grant modal, hall-ticket issuance.
- `SCR-504` **Marks Moderation Queue:** Review faculty-submitted marks, apply moderated marks with mandatory justification, lock marks registers.
- `SCR-505` **Result Compilation & Publication:** SGPA/CGPA compilation, grade distribution preview, official publication trigger.
- `SCR-506` **Result Amendment Workflow:** Authorized post-publication grade/mark amendment dialog with before/after audit capture.

---

## 7. Accounts Officer Screens
- `SCR-601` **Accounts Dashboard:** Fee collection totals, outstanding term balances, overdue metrics.
- `SCR-602` **Fee Structure Setup:** Configure fee categories (Tuition, Lab, Library, Exam, Misc) for programs and terms.
- `SCR-603` **Student Fee Ledgers:** Individual student obligation breakdown, payment history, and current outstanding balance.
- `SCR-604` **Record Payment (Receipt Generator):** Enter payment amount (paise precision), method, reference, calculate FIFO allocations (or authorized manual override), issue receipt.
- `SCR-605` **Receipt Details & Reprint:** Printable official payment receipt with watermark.
- `SCR-606` **Concessions & Adjustments:** Record approved fee concessions, waivers, or scholarships.
- `SCR-607` **Payment Reversal Workflow:** Reversal modal with mandatory reason; transactional rollback of allocations and receipt cancellation.
- `SCR-608` **Financial Reconciliation Report:** Daily collection summaries grouped by payment method and program.

---

## 8. Student Portal Screens (Personal Self-Service)
- `SCR-701` **Student Dashboard:** Welcome banner, active semester summary, timetable highlights, attendance alert badge.
- `SCR-702` **My Profile:** Personal demographic, contact, and enrolled program details.
- `SCR-703` **My Timetable:** Interactive weekly timetable for enrolled course offerings.
- `SCR-704` **My Attendance:** Subject-wise percentage, total conducted vs. attended sessions, condonation notices.
- `SCR-705` **My Fees & Receipts:** Term fee obligations, payment history, outstanding balance, downloadable official receipts.
- `SCR-706` **My Exam Schedule & Hall-Ticket:** Examination timetable, seating details, eligibility status, printable hall-ticket.
- `SCR-707` **My Published Results:** Official published term grades, grade points, SGPA, CGPA, and earned credits.
- `SCR-708` **My Academic Progress:** Cumulative credit meter, degree requirements checklist, active backlogs list.
