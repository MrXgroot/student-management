# Academic Progression and Graduation Workflow Specification

## 1. Actors and Roles
- **Academic Administrator (`ACADEMIC_ADMIN`):** Evaluates cohort term progression, reviews promotion eligibility reports, and handles student status advancement.
- **College Administrator (`COLLEGE_ADMIN`):** Authorizes progression overrides, degree conferrals, and program completions.
- **Student (`STUDENT`):** Views personal earned credits, active backlogs, and degree completion progress in the Student Portal.

---

## 2. Term Progression and Backlog Tracking

### 2.1 Backlog Definition and Lifecycle
- A **Backlog** is an unsatisfied course requirement resulting from a published course result with grade `F` (Fail) or `Ab` (Absent).
- Backlogs are not separate standalone status flags on a student; they are derived dynamically from the student's historical course results:
  - If a student subsequently repeats the course offering or passes a supplementary exam, a new `results` document is published for that attempt.
  - The backlog is considered **cleared** once a passing grade (`P` through `O`) is recorded for that course. Attempt history is permanently preserved.

### 2.2 Demo Promotion Policy (Vidyadeep Institute of Technology)
Progression from one academic year/term to the next is evaluated using the following demo policy rules:
1. **Semester-to-Semester Progression:** A student is permitted to advance from an odd semester to an even semester (e.g. Semester 1 to Semester 2) regardless of backlogs, provided they have not been placed on disciplinary suspension.
2. **Year-to-Year Progression Threshold (Carry-Over Rule):**
   - To advance to Year 2 (Semester 3), a student must have accumulated **no more than 4 active backlogs** from Year 1 (Semesters 1 and 2).
   - To advance to Year 3 (Semester 5), a student must have cleared **all** Year 1 courses and have no more than 4 active backlogs from Year 2.
3. **Academic Probation (`PROBATION`):** If a student exceeds the permitted backlog threshold, their `lifecycleStatus` transitions to `PROBATION`. They are detained from taking new advanced courses until backlogs are reduced below the threshold.

---

## 3. Graduation Eligibility Clearance

When a student reaches the final semester of their program:
1. **Curriculum Audit:** The system verifies that the student has passed every compulsory course listed in the program's `curriculum` mapping.
2. **Total Earned Credits:** Total credits earned ($\sum \text{Credits of Passed Courses}$) must meet or exceed the program's degree requirement (e.g., 140 credits for a 3-year BCA).
3. **Zero Active Backlogs:** The student must have 0 outstanding failed/absent course attempts.
4. **Administrative & Financial Clearance:** All term `feeObligations` must be in status `PAID` or `WAIVED`.
5. **Clearance Report:**
   - If eligible, the system generates a Graduation Clearance certificate, and status transitions from `ACTIVE` to `COMPLETED`, then to `GRADUATED` upon formal degree conferral.
   - If ineligible, the system outputs an explicit checklist of unmet requirements (e.g. *"Missing 4 credits in Elective Group B; Outstanding backlog in BCA203; Unpaid fee obligation of ₹12,000"*).

---

## 4. Testing and Verification Requirements

- **Backlog Resolution Test:** Create a student with grade `F` in Semester 1. Enroll the student in Semester 3 repeat offering; publish grade `B`. Verify that active backlogs count drops to 0 while attempt history remains intact.
- **Year-to-Year Carry-Over Test:** Verify that a student with 5 backlogs attempting to register for Semester 3 is blocked and transitioned to `PROBATION`.
- **Graduation Audit Checklist:** Evaluate a student missing 4 credits; verify the graduation API returns `isEligible = false` and lists the specific missing course.
