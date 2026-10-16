# MongoDB Data Model Specification

This document defines the canonical collections, document schemas, aggregate boundaries, and consistency models for EduCore. All ObjectIds are stored as native BSON `ObjectId` and exposed as 24-character hexadecimal `string` values in C# and API DTOs.

---

## 1. Aggregate Design & Consistency Principles

1. **Bounded Embedding for Atomicity:**
   - **Payments:** Payment allocations and receipt metadata are embedded directly inside the `payments` aggregate. This enables single-document atomic persistence for the payment event itself.
   - **Class Sessions:** Student session attendance entries are embedded inside `classSessions.roster`. Because class section capacity is bounded (30 to 70 students), the entire session register is saved in a single document write.
2. **Multi-Document ACID Transactions:**
   - Multi-document operations spanning multiple collections execute within an explicit MongoDB client session transaction (`IClientSession.StartTransaction()`).
   - Requires a single-node replica set (`rs0`) for local development and CI (see [`ADR-001`](file:///d:/production/arise-craft/EduCore/docs/adr/ADR-001-mongodb-replica-set-and-transactions.md)).
   - Critical transactional workflows:
     - **Payment Recording:** Insert `payments` (with embedded allocations/receipt) + update affected `feeObligations` (balances and status) + append `auditEvents`.
     - **Payment Reversal:** Update `payments` (to `REVERSED`) + revert affected `feeObligations` + append `auditEvents`.
     - **Admission Confirmation:** Update `applications` (to `CONFIRMED`) + insert `students` + insert `users` (status `PENDING_ACTIVATION`) + atomic counter increment.
3. **Optimistic Concurrency Control:**
   - Mutable aggregate roots include an integer `version` field. Updates condition on `{ _id: id, version: currentVersion }` and atomically increment `version`.

---

## 2. Collections and Document Schemas

### 2.1 Institution and Academic Setup

#### `institutions`
- `_id`: `ObjectId`
- `code`: `string` (e.g. `"VIT-BLR"`, unique)
- `name`: `string` (`"Vidyadeep Institute of Technology"`)
- `address`: `{ line1: string, line2: string, city: string, state: string, pincode: string, country: string }`
- `timezone`: `string` (`"Asia/Kolkata"`)
- `contact`: `{ email: string, phone: string, website: string }`
- `policyConfiguration`: `{ attendanceThreshold: double, condonationMin: double, ciaWeight: double, seeWeight: double, passThreshold: double }`
- `createdAt`, `updatedAt`: `DateTime` (UTC)

#### `departments`
- `_id`: `ObjectId`
- `institutionId`: `ObjectId`
- `code`: `string` (e.g. `"CSE"`, unique within institution)
- `name`: `string` (`"Computer Science & Engineering"`)
- `status`: `string` (`"ACTIVE"` | `"INACTIVE"`)
- `version`: `int`

#### `programs`
- `_id`: `ObjectId`
- `institutionId`: `ObjectId`
- `departmentId`: `ObjectId`
- `code`: `string` (e.g. `"BCA"`, unique within institution)
- `name`: `string` (`"Bachelor of Computer Applications"`)
- `awardType`: `string` (`"UNDERGRADUATE"` | `"POSTGRADUATE"`)
- `durationYears`: `int` (`3`)
- `totalSemesters`: `int` (`6`)
- `curriculum`: `Array` of `{ semesterNumber: int, courseId: ObjectId, isElective: bool, electiveGroup: string?, credits: int }`
- `status`: `string` (`"ACTIVE"` | `"INACTIVE"`)
- `version`: `int`

#### `academicYears`
- `_id`: `ObjectId`
- `institutionId`: `ObjectId`
- `label`: `string` (`"2024-2025"`, unique within institution)
- `startDate`: `DateTime` (UTC)
- `endDate`: `DateTime` (UTC)
- `status`: `string` (`"UPCOMING"` | `"CURRENT"` | `"CONCLUDED"`)
- `version`: `int`

#### `terms`
- `_id`: `ObjectId`
- `academicYearId`: `ObjectId`
- `code`: `string` (e.g. `"2024-ODD"`, unique within academic year)
- `name`: `string` (`"Odd Semester 2024-2025"`)
- `startDate`: `DateTime` (UTC)
- `endDate`: `DateTime` (UTC)
- `registrationWindow`: `{ openDate: DateTime, closeDate: DateTime, isOpen: bool }`
- `status`: `string` (`"PLANNING"` | `"ACTIVE"` | `"EXAMS"` | `"CONCLUDED"`)
- `version`: `int`

#### `courses`
- `_id`: `ObjectId`
- `institutionId`: `ObjectId`
- `code`: `string` (e.g. `"BCA101"`, unique within institution)
- `title`: `string` (`"Programming in C"`)
- `credits`: `int` (`4`)
- `prerequisiteCourseIds`: `Array<ObjectId>`
- `status`: `string` (`"ACTIVE"` | `"INACTIVE"`)
- `version`: `int`

#### `courseOfferings`
- `_id`: `ObjectId`
- `courseId`: `ObjectId`
- `termId`: `ObjectId`
- `section`: `string` (`"A"`, `"B"`)
- `facultyAssignments`: `Array` of `{ facultyId: ObjectId, role: "PRIMARY" | "ASSISTANT" }`
- `capacity`: `int` (`60`)
- `enrolledCount`: `int` (`0`) (atomic conditional increment enforced on enrollment)
- `schedule`: `Array` of `{ dayOfWeek: string, startTime: string, endTime: string, room: string }`
- `status`: `string` (`"OPEN"` | `"CLOSED"` | `"CANCELLED"`)
- `version`: `int`

---

### 2.2 Identity, Admissions, and Student Records

#### `counters` (Atomic Sequence Generator)
- `_id`: `string` (e.g. `"ADM-2024"`, `"REC-2024"`, `"APP-2024"`)
- `sequenceValue`: `long`
- `prefix`: `string`
- `year`: `int`
- `updatedAt`: `DateTime` (UTC)

#### `users`
- `_id`: `ObjectId`
- `username`: `string` (unique, lowercase)
- `email`: `string` (unique, lowercase)
- `passwordHash`: `string` (PBKDF2 HMAC-SHA512 via ASP.NET Core PasswordHasher)
- `roles`: `Array<string>` (`["STUDENT"]`, `["FACULTY", "ACADEMIC_ADMIN"]`)
- `status`: `string` (`"PENDING_ACTIVATION"` | `"ACTIVE"` | `"SUSPENDED"` | `"DEACTIVATED"`)
- `studentId`: `ObjectId?`
- `staffProfileId`: `ObjectId?`
- `activationTokenHash`: `string?`
- `activationTokenExpiresAt`: `DateTime?`
- `securityStamp`: `string` (GUID for session revocation)
- `version`: `int`

#### `staffProfiles`
- `_id`: `ObjectId`
- `userId`: `ObjectId`
- `staffNumber`: `string` (unique within institution)
- `departmentId`: `ObjectId`
- `designation`: `string`
- `fullName`: `string`
- `contact`: `{ phone: string, email: string }`
- `status`: `string` (`"ACTIVE"` | `"INACTIVE"`)
- `version`: `int`

#### `applications`
- `_id`: `ObjectId`
- `applicationNumber`: `string` (e.g. `"APP-2024-00104"`, unique)
- `programId`: `ObjectId`
- `academicYearId`: `ObjectId`
- `applicantDetails`: `{ firstName: string, lastName: string, dateOfBirth: DateTime, gender: string, email: string, phone: string, address: object }`
- `educationHistory`: `Array` of `{ qualification: string, institution: string, boardOrUniversity: string, yearOfPassing: int, percentageOrCgpa: double }`
- `documentChecklist`: `Array` of `{ documentType: string, isVerified: bool, verifiedBy: ObjectId?, verifiedAt: DateTime?, remarks: string? }`
- `state`: `string` (`"DRAFT"` | `"SUBMITTED"` | `"UNDER_REVIEW"` | `"APPROVED"` | `"REJECTED"` | `"NEEDS_CORRECTION"` | `"WITHDRAWN"` | `"CONFIRMED"`)
- `decision`: `{ outcome: string, decidedBy: ObjectId?, decidedAt: DateTime?, reason: string? }`
- `confirmedStudentId`: `ObjectId?`
- `createdAt`, `updatedAt`: `DateTime` (UTC)
- `version`: `int`

#### `students`
- `_id`: `ObjectId`
- `admissionNumber`: `string` (e.g. `"ADM-2024-00042"`, unique)
- `rollNumber`: `string?` (e.g. `"24BCA-A015"`, assigned at cohort registration, unique when set)
- `universityRegNumber`: `string?` (optional, unique when set)
- `applicationId`: `ObjectId`
- `programId`: `ObjectId`
- `currentSemester`: `int` (`1`)
- `profile`: `{ firstName: string, lastName: string, dateOfBirth: DateTime, gender: string, email: string, phone: string, bloodGroup: string?, address: object, guardian: object }`
- `lifecycleStatus`: `string` (`"ADMITTED"` | `"ACTIVE"` | `"PROBATION"` | `"SUSPENDED"` | `"ON_LEAVE"` | `"WITHDRAWN"` | `"DISCONTINUED"` | `"COMPLETED"` | `"GRADUATED"`)
- `createdAt`, `updatedAt`: `DateTime` (UTC)
- `version`: `int`

#### `termRegistrations`
- `_id`: `ObjectId`
- `studentId`: `ObjectId`
- `termId`: `ObjectId`
- `programId`: `ObjectId`
- `semesterNumber`: `int`
- `academicStanding`: `string` (`"REGULAR"` | `"PROBATION"`)
- `status`: `string` (`"REGISTERED"` | `"WITHDRAWN"`)
- `registeredAt`: `DateTime` (UTC)
- `version`: `int`

#### `courseEnrollments`
- `_id`: `ObjectId`
- `studentId`: `ObjectId`
- `courseOfferingId`: `ObjectId`
- `termId`: `ObjectId`
- `status`: `string` (`"ENROLLED"` | `"WITHDRAWN"` | `"COMPLETED"`)
- `enrolledAt`: `DateTime` (UTC)
- `version`: `int`

---

### 2.3 Attendance

#### `classSessions` (Aggregate Root with Embedded Roster)
- `_id`: `ObjectId`
- `courseOfferingId`: `ObjectId`
- `sessionDate`: `DateTime` (UTC date)
- `startTime`: `string` (`"09:00"`)
- `endTime`: `string` (`"10:00"`)
- `topic`: `string`
- `facultyId`: `ObjectId`
- `status`: `string` (`"SCHEDULED"` | `"IN_PROGRESS"` | `"SUBMITTED"` | `"CLOSED"` | `"CANCELLED"`)
- `roster`: `Array` of:
  - `studentId`: `ObjectId`
  - `status`: `string` (`"PRESENT"` | `"ABSENT"` | `"LATE"` | `"EXCUSED"`)
  - `remarks`: `string?`
  - `recordedAt`: `DateTime`
  - `corrections`: `Array` of `{ priorStatus: string, newStatus: string, reason: string, correctedBy: ObjectId, correctedAt: DateTime, authorizedBy: ObjectId }`
- `submittedAt`: `DateTime?`
- `submittedBy`: `ObjectId?`
- `version`: `int`

---

### 2.4 Fees and Finance

#### `feeStructures`
- `_id`: `ObjectId`
- `institutionId`: `ObjectId`
- `programId`: `ObjectId`
- `academicYearId`: `ObjectId`
- `termId`: `ObjectId?`
- `chargeItems`: `Array` of `{ category: "TUITION"|"LAB"|"LIBRARY"|"EXAM"|"MISC", title: string, amountPaise: long, isMandatory: bool }`
- `totalAmountPaise`: `long`
- `dueDate`: `DateTime` (UTC)
- `status`: `string` (`"ACTIVE"` | `"INACTIVE"`)
- `version`: `int`

#### `feeObligations`
- `_id`: `ObjectId`
- `studentId`: `ObjectId`
- `termId`: `ObjectId`
- `feeStructureId`: `ObjectId`
- `category`: `string` (`"TUITION"`, `"LAB"`, `"LIBRARY"`, `"EXAM"`, `"MISC"`)
- `title`: `string`
- `amountPaise`: `long`
- `paidAmountPaise`: `long` (`0`)
- `dueDate`: `DateTime` (UTC)
- `status`: `string` (`"PENDING"` | `"PARTIALLY_PAID"` | `"PAID"` | `"WAIVED"`)
- `version`: `int`

#### `payments` (Aggregate Root with Embedded Allocations & Receipt)
- `_id`: `ObjectId`
- `studentId`: `ObjectId`
- `amountPaise`: `long`
- `paymentMethod`: `string` (`"CASH"` | `"CHEQUE"` | `"BANK_TRANSFER"` | `"POS"`)
- `paymentReference`: `string` (e.g. Cheque / UTR number)
- `idempotencyKey`: `string` (unique header value)
- `recordedBy`: `ObjectId`
- `recordedAt`: `DateTime` (UTC)
- `status`: `string` (`"CONFIRMED"` | `"REVERSED"`)
- `receipt`: `{ receiptNumber: string (unique), issuedAt: DateTime, status: "ISSUED" | "CANCELLED" }`
- `allocations`: `Array` of `{ obligationId: ObjectId, amountPaise: long, isVoid: bool }`
- `reversalMetadata`: `{ reversedBy: ObjectId?, reversedAt: DateTime?, reason: string?, authorizedBy: ObjectId? }?`
- `version`: `int`

#### `financialAdjustments`
- `_id`: `ObjectId`
- `studentId`: `ObjectId`
- `obligationId`: `ObjectId`
- `type`: `string` (`"CONCESSION"` | `"SCHOLARSHIP"` | `"WAIVER"` | `"PENALTY"`)
- `amountPaise`: `long`
- `reason`: `string`
- `approvedBy`: `ObjectId`
- `createdAt`: `DateTime` (UTC)
- `version`: `int`

---

### 2.5 Examinations and Results

#### `assessments`
- `_id`: `ObjectId`
- `courseOfferingId`: `ObjectId`
- `termId`: `ObjectId`
- `type`: `string` (`"CIA_INTERNAL"` | `"SEE_SEMESTER_END"` | `"PRACTICAL"`)
- `title`: `string` (`"Internal Assessment 1"`, `"Semester End Examination"`)
- `maxMarks`: `double`
- `weightagePercentage`: `double` (e.g. `40.0` or `60.0`)
- `schedule`: `{ examDate: DateTime, startTime: string, endTime: string, room: string? }`
- `state`: `string` (`"SCHEDULED"` | `"MARKS_ENTRY_OPEN"` | `"MARKS_SUBMITTED"` | `"MODERATED"` | `"LOCKED"` | `"CONCLUDED"`)
- `version`: `int`

#### `examRegistrations` (Canonical Candidate Eligibility Snapshot)
- `_id`: `ObjectId`
- `assessmentId`: `ObjectId`
- `studentId`: `ObjectId`
- `termId`: `ObjectId`
- `isEligible`: `bool`
- `attendancePercentage`: `double`
- `feeCleared`: `bool`
- `condonationGranted`: `bool`
- `condonationReason`: `string?`
- `hallTicketIssued`: `bool`
- `evaluatedAt`: `DateTime` (UTC)
- `version`: `int`

#### `marks`
- `_id`: `ObjectId`
- `assessmentId`: `ObjectId`
- `studentId`: `ObjectId`
- `rawMarks`: `double`
- `moderatedMarks`: `double?`
- `moderationReason`: `string?`
- `isAbsent`: `bool`
- `status`: `string` (`"DRAFT"` | `"SUBMITTED"` | `"MODERATED"` | `"LOCKED"`)
- `enteredBy`: `ObjectId`
- `enteredAt`: `DateTime` (UTC)
- `moderatedBy`: `ObjectId?`
- `moderatedAt`: `DateTime?`
- `version`: `int`

#### `results`
- `_id`: `ObjectId`
- `studentId`: `ObjectId`
- `termId`: `ObjectId`
- `courseOfferingId`: `ObjectId`
- `componentMarks`: `Array` of `{ assessmentId: ObjectId, assessmentType: string, maxMarks: double, marksObtained: double, isAbsent: bool }`
- `totalMarksObtained`: `double`
- `maxMarksTotal`: `double`
- `percentage`: `double`
- `letterGrade`: `string` (`"O"`, `"A+"`, `"A"`, `"B+"`, `"B"`, `"C"`, `"P"`, `"F"`, `"Ab"`)
- `gradePoint`: `int`
- `creditsEarned`: `int`
- `outcome`: `string` (`"PASS"` | `"FAIL"`)
- `status`: `string` (`"DRAFT_COMPILED"` | `"PENDING_APPROVAL"` | `"APPROVED"` | `"PUBLISHED"`)
- `publishedAt`: `DateTime?`
- `version`: `int`

#### `resultAmendments`
- `_id`: `ObjectId`
- `resultId`: `ObjectId`
- `studentId`: `ObjectId`
- `priorMarks`: `double`
- `newMarks`: `double`
- `priorGrade`: `string`
- `newGrade`: `string`
- `reason`: `string`
- `requestedBy`: `ObjectId`
- `approvedBy`: `ObjectId`
- `timestamp`: `DateTime` (UTC)

---

### 2.6 Governance & Audit

#### `auditEvents` (Strictly Append-Only)
- `_id`: `ObjectId`
- `actorId`: `ObjectId`
- `actorRole`: `string`
- `action`: `string` (e.g. `"ADMISSION_CONFIRMED"`, `"PAYMENT_REVERSED"`, `"RESULT_AMENDED"`)
- `targetType`: `string` (`"Payment"`, `"Result"`, `"ClassSession"`, `"Student"`)
- `targetId`: `string`
- `timestamp`: `DateTime` (UTC)
- `correlationId`: `string`
- `reason`: `string?`
- `changeSummary`: `string` (sanitized JSON summary, strictly omitting passwords, tokens, full PII)

#### `idempotencyRecords`
- `_id`: `ObjectId`
- `scope`: `string` (e.g. `"PAYMENT_RECORD"`, `"ADMISSION_CONFIRM"`)
- `key`: `string`
- `requestHash`: `string`
- `responseStatus`: `int`
- `responseBody`: `string`
- `createdAt`: `DateTime` (UTC)
- `expiresAt`: `DateTime` (TTL index 24 hours)
