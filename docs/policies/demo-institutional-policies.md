# Demo Institutional Policies — Vidyadeep Institute of Technology

**Policy Version:** 1.0 (Demo Configuration)  
**Effective Date:** 2026-10-16  
**Institution:** Vidyadeep Institute of Technology, Bengaluru, Karnataka (Fictional)  

> [!NOTE]
> The thresholds, weighting, grading scales, and fee rules defined in this document represent the concrete demo configuration for Vidyadeep Institute of Technology. They are configurable policies in the EduCore platform and must **not** be treated as universal Indian statutory mandates for all institutions.

---

## 1. Attendance Policy (`attendancePolicy`)

### 1.1 Purpose
Defines session attendance weighting, percentage calculations, examination eligibility thresholds, and authorized condonation rules.

### 1.2 Session Weighting and Status Values
When attendance is marked for an enrolled student in a scheduled class session:
- `PRESENT`: Student attended the session. Weight = **1.0**.
- `ABSENT`: Student did not attend the session without sanctioned leave. Weight = **0.0**.
- `LATE`: Student arrived after the scheduled start time but within the permitted grace period (15 minutes). Weight = **0.5**.
- `EXCUSED`: Student was absent due to officially sanctioned college duty, sports representation, or approved medical leave. Excluded from calculation (reduces denominator).
- `CANCELLED`: Session was officially cancelled (e.g., faculty emergency or holiday). Excluded entirely from calculations for all students.

### 1.3 Mathematical Formula
For any enrolled student in a course offering:
$$\text{Total Valid Sessions} = \text{Conducted Sessions} - \text{Excused Sessions}$$
$$\text{Attended Sessions} = \text{Count}(\text{PRESENT}) \times 1.0 + \text{Count}(\text{LATE}) \times 0.5$$
$$\text{Attendance Percentage} = \begin{cases} 100.00\% & \text{if Total Valid Sessions} = 0 \\ \text{Round}\left(\frac{\text{Attended Sessions}}{\text{Total Valid Sessions}} \times 100, 2\right) & \text{otherwise} \end{cases}$$

### 1.4 Examination Eligibility & Condonation Rules
- **Standard Eligibility Threshold:** $\ge 75.00\%$. Student is automatically cleared on attendance for examination hall-ticket issuance.
- **Condonation Range:** $65.00\% \le \text{Percentage} < 75.00\%$. Student is ineligible by default, but may be granted a condonation by an authorized official (`COLLEGE_ADMIN` or Principal/Dean) upon receipt of an approved medical certificate or formal application.
- **Strict Detention:** $< 65.00\%$. No condonation is permitted under institution rules. The student is detained in the course offering, receives an attendance bar, and must re-register for the course offering in a subsequent term.

---

## 2. Assessment and Examination Policy (`gradingPolicy`)

### 2.1 Purpose
Defines the component weighting, pass criteria, missing-mark treatment, and grade point calculation for undergraduate and postgraduate programs.

### 2.2 Assessment Weighting
Every course offering conducts Continuous Internal Assessment (CIA) and a Semester End Examination (SEE):
- **Continuous Internal Assessment (CIA):** **40%** of total course marks. Comprises periodic internal tests, laboratory records, and assignments.
- **Semester End Examination (SEE):** **60%** of total course marks. Formal summative examination conducted at the end of the academic term.

### 2.3 Pass Criteria and Thresholds
To pass a course offering and earn its allocated credits, a candidate must satisfy **both** of the following conditions:
1. **SEE Minimum Component Pass:** $\ge 40.00\%$ of the maximum marks allocated to SEE.
2. **Aggregate Course Minimum Pass:** $\ge 40.00\%$ of the combined total marks (CIA + SEE).

*Example:* In a 100-mark course (40 CIA + 60 SEE):
- Student must score at least 24/60 in SEE.
- Student's total score (CIA + SEE) must be at least 40/100.
- If a student scores 35/40 in CIA but only 20/60 in SEE, total is 55/100, but the student **fails** due to failing the SEE component threshold.

### 2.4 Missing Mark & Absence Treatment
- If a student is marked `ABSENT` in SEE, the component mark is recorded as **0**, and the course result outcome is flagged as `FAILED_ABSENT`.
- If an internal assessment mark is missing at the time of marks submission, the system prevents marks lock until either a mark is entered or an authorized zero/exemption flag is supplied.

### 2.5 10-Point Grading Scale (UGC Choice Based Credit System - CBCS)
Overall marks are converted to letter grades and grade points using the institution's 10-point scale:

| Percentage Range | Letter Grade | Grade Point | Qualitative Description | Outcome |
|---|---|---|---|---|
| $90.00\% - 100.00\%$ | **O** | 10 | Outstanding | Pass |
| $80.00\% - 89.99\%$ | **A+** | 9 | Excellent | Pass |
| $70.00\% - 79.99\%$ | **A** | 8 | Very Good | Pass |
| $60.00\% - 69.99\%$ | **B+** | 7 | Good | Pass |
| $55.00\% - 59.99\%$ | **B** | 6 | Above Average | Pass |
| $50.00\% - 54.99\%$ | **C** | 5 | Average | Pass |
| $40.00\% - 49.99\%$ | **P** | 4 | Pass | Pass |
| $< 40.00\%$ | **F** | 0 | Fail | Backlog |
| Absent in Exam | **Ab** | 0 | Absent | Backlog |

### 2.6 Grade Point Calculations
- **Course Credit Points:** $\text{Course Credits} \times \text{Grade Point Earned}$
- **Semester Grade Point Average (SGPA):**
  $$\text{SGPA} = \frac{\sum (\text{Course Credits} \times \text{Grade Point})}{\sum \text{Course Credits in Term}}$$
- **Cumulative Grade Point Average (CGPA):** Calculated across all completed terms weighted by term credits.
- **Rounding Rule:** SGPA and CGPA are rounded to two decimal places using standard half-up rounding.

---

## 3. Fee Allocation and Financial Policy (`feePolicy`)

### 3.1 Purpose
Governs the creation of fee obligations, payment allocation priority, overpayment constraints, and payment reversal invariants.

### 3.2 Currency and Integer Arithmetic
All monetary obligations, payments, concessions, and balances in EduCore are denominated in Indian National Rupees (INR) and stored strictly as **integer paise** ($1 \text{ INR} = 100 \text{ paise}$). Floating-point arithmetic for currency calculations is strictly prohibited.

### 3.3 Default Fee Allocation Strategy
When a payment is received that is less than or equal to the student's total unpaid obligations:
1. **Primary Sort:** Obligations are sorted strictly in **FIFO** order by `dueDate` (earliest due date allocated first).
2. **Category Tie-Breaker:** When multiple obligations share the exact same `dueDate`, funds are allocated in the following priority order:
   1. `TUITION` (Tuition Fees)
   2. `LAB` (Laboratory & Practical Fees)
   3. `LIBRARY` (Library Fees)
   4. `EXAM` (Examination & Hall-ticket Fees)
   5. `MISC` (Identity Card, Sports, Student Welfare, and Other Fees)
3. **Manual Override:** Authorized Accounts staff (`ACCOUNTS_OFFICER`, `COLLEGE_ADMIN`) may specify a custom allocation distribution across obligations during payment recording. The custom allocation is validated server-side to ensure the sum of allocations equals the payment amount, and a reason for the manual override must be recorded.

### 3.4 Overpayment Policy (V1 Invariant)
Payments exceeding a student's total outstanding obligation balance are **strictly rejected** by the API boundary in V1 with error code `EXCESS_PAYMENT_NOT_PERMITTED`. Student advance-credit deposits and unallocated credit ledgers are deferred to V2.

### 3.5 Payment Reversal Invariants
- Payments are append-only. Recorded payment documents are never hard-deleted.
- Reversing a payment requires explicit `ACCOUNTS_OFFICER` or `COLLEGE_ADMIN` authorization, an audit reason, and execution within a MongoDB transaction.
- When reversed:
  - `payments.status` changes to `REVERSED`.
  - `payments.reversalMetadata` captures `{ reversedBy, reversedAt, reason, authorizedBy }`.
  - All embedded allocation lines are marked `VOID`.
  - Linked receipt status updates to `CANCELLED`.
  - Linked `feeObligations` have their `paidAmountPaise` decremented by the voided allocation amount, and obligation status reverts automatically to `PENDING` or `PARTIALLY_PAID`.

---

## 4. Policy Versioning and Applicability

1. Every student's academic results are evaluated against the specific policy version active during their academic term.
2. Updates to institutional policies must not retroactively recalculate published historical results or archived financial receipts without a formal amendment event.
