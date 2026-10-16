# Policies

Policies implement variable rules, thresholds, and permitted state transitions. Keep them pure, highly testable, and strictly separated from API controllers and frontend components.

## Available Policies

- [`demo-institutional-policies.md`](file:///d:/production/arise-craft/EduCore/docs/policies/demo-institutional-policies.md) — Comprehensive demo configuration for *Vidyadeep Institute of Technology*, including:
  - Attendance policy (75% threshold, 65% condonation range, session weighting, formula).
  - Assessment and grading policy (40% CIA / 60% SEE weighting, 10-point UGC CBCS scale, pass rules).
  - Fee allocation policy (FIFO due-date priority, category tie-breaker, overpayment prohibition).

## Planned Policy Modules in Code

As features are implemented, policies are housed in the `EduCore.Domain` layer:
- `AdmissionPolicy`: application transitions, eligibility rules, and duplicate detection.
- `StudentLifecyclePolicy`: permitted student status transitions and invariants.
- `EnrollmentPolicy`: registration windows, seat capacity decrement/increment, prerequisite checks.
- `AttendancePolicy`: percentage calculations, condonation validation, and correction rules.
- `FeePolicy`: obligation generation, payment allocation distribution, overpayment prevention, reversal logic.
- `ExamEligibilityPolicy`: evaluation of attendance and fee clearance for hall-ticket issuance.
- `GradingPolicy`: component mark validation, grade point conversion, SGPA/CGPA calculation.
- `ResultPublicationPolicy`: moderation, result locking, publication safety, and audited amendments.
- `ProgressionPolicy`: term advancement, backlog accumulation, and graduation clearance.
- `AuthorizationPolicy`: fine-grained permissions for high-impact actions.

## Policy Development Rules

1. Each policy has a written contract and dedicated unit tests.
2. Policy inputs and outputs are explicit C# domain objects or primitives; policies do not query the database.
3. Services fetch required records and feed relevant facts into domain policies.
4. Do not duplicate domain business logic in frontend JavaScript; the backend is the authoritative decision-maker.
5. Institutions configure policies; unknown institutional rules must be flagged for configuration rather than guessed or hardcoded.
