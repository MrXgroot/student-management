# Scope and Release Boundaries

## V1 Target
A single fictional institution—**Vidyadeep Institute of Technology, Bengaluru, Karnataka**—with semester-based undergraduate and postgraduate programs. System architecture must not block future expansion, but V1 does not implement multi-tenant SaaS isolation.

## In Scope for V1
- **Authentication & Security:** ASP.NET Core cookie authentication (`HttpOnly`, `SameSite=Strict`), CSRF protection, role-based authorization, and student account activation token flow.
- **Institution Configuration:** Single institution profile, academic departments, programs, curricula, academic years, terms/semesters, and courses/subjects.
- **Staff & User Management:** User accounts, staff profiles, and role assignments (`COLLEGE_ADMIN`, `ADMISSIONS_OFFICER`, `ACADEMIC_ADMIN`, `FACULTY`, `EXAM_OFFICER`, `ACCOUNTS_OFFICER`, `STUDENT`).
- **Admissions Management (Staff-Entered):** Staff entry of paper/walk-in applications, document checklist review, eligibility checks, admission decisions, and admission confirmation.
- **Student Lifecycle & Records:** Transitioning confirmed applications to canonical student records, assigning admission and roll numbers, tracking lifecycle states (`ADMITTED`, `ACTIVE`, `PROBATION`, `SUSPENDED`, etc.), and student directory.
- **Academic Enrollment:** Term registrations, course offerings with section capacity enforcement, faculty assignments, and course enrollments.
- **Attendance Management:** Class sessions, batch attendance registers with local grid editing, status weightings (Present, Absent, Late, Excused), authorized audited corrections, and attendance reports.
- **Fee Management:** Fee structures, student fee obligations, staff payment recording, receipt generation, FIFO allocations, concessions, authorized audited payment reversals, and student ledgers.
- **Examinations & Results:** Exam scheduling, candidate eligibility evaluation (`examRegistrations`), marks entry with raw vs. moderated tracking, publication approval, audited amendments, and transcript generation.
- **Student Portal:** Dedicated student web view for authenticated students to view their own profile, timetable, attendance, fee obligations/receipts, examination hall-ticket status, published results, and academic progress.
- **Audit & Governance:** Append-only audit history for sensitive and consequential domain operations.
- **Testing & Data:** Fictional Indian seed data, repeatable seed scripts, unit and integration tests, and OpenAPI documentation.

## Explicitly Out of Scope for V1
- **Public Applicant Self-Service Portal:** Public applicant registration and online self-submission of applications are deferred to V2. In V1, applications are entered and managed solely by authorized staff.
- **Online Payment Gateway Integration:** In V1, payments are collected and recorded manually by authorized Accounts staff (cash, cheque, bank transfer/challan, POS); direct integration with external payment gateways (e.g. Razorpay, BillDesk) is out of scope.
- **Multi-Institution SaaS:** Multi-tenant database partitioning, automated tenant provisioning, and billing are deferred.
- **Native Mobile Apps:** Native Android/iOS mobile applications are out of scope. The web application is responsive across desktop, tablet, and mobile browsers.
- **Biometric Hardware & RFID Integration:** Hardware-based attendance terminals are out of scope.
- **AI Cheating Detection & Automated Misconduct:** No automated video proctoring, AI proctoring, or algorithmic misconduct determinations.
- **Ancillary Modules:** Hostel/dormitory management, transportation, library circulation, payroll, alumni management, placement cell, and full learning management systems (LMS).
- **External University / Government API Sync:** Direct automated submission to state higher education councils, universities, or scholarship portals.
- **Digital Signatures:** Legally binding cryptographic digital signatures (DSC tokens).

## Key Workflow Decoupling Rules for V1
1. **Admission Confirmation vs. Fee Collection:**
   Admission confirmation creates the official student record in `ADMITTED` status and provisions the student user account. Admission confirmation is **not** conditionally blocked by payment collection in V1. Fee obligations are assessed upon term registration, and unpaid dues affect examination hall-ticket eligibility rather than initial student onboarding.
2. **Student Self-Service Boundaries:**
   Enrolled students have read-only access to their own academic records, attendance records, fee ledgers, and published results. Students cannot submit admission applications or directly alter records.

## Configurable Policies (Demo Defaults Defined)
Variable institutional policies must not be hardcoded in application logic. Initial demo defaults are codified in [`docs/policies/demo-institutional-policies.md`](file:///d:/production/arise-craft/EduCore/docs/policies/demo-institutional-policies.md):
- Attendance threshold: 75% standard eligibility; 65% to <75% condonation range.
- Assessment weighting: 40% CIA (Internal) / 60% SEE (Semester End Exam).
- 10-Point UGC CBCS grading scale and minimum 40% pass criteria.
- FIFO fee allocation order by due date and overpayment rejection invariant.

## Change Control
Any proposed expansion of V1 scope must state:
1. Business justification and user value.
2. Affected collections, domain invariants, and API contracts.
3. Security, authorization, and data privacy implications.
4. Test impact and changes to the definition of done.
5. Impact on delivery milestones.
