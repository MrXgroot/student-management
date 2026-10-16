# Testing Strategy

## Test levels
1. Unit tests: policies, validators, calculations, state transitions.
2. Integration tests: API + MongoDB behavior, indexes, authorization, persistence.
3. Frontend tests: key components and user workflows where test infrastructure is in place.
4. End-to-end smoke tests: critical journeys when feasible.
5. Manual acceptance: review responsive UI and role-based flows.

## High-priority scenarios
- Admission transitions and duplicate enrollment prevention
- Authorization by role and resource ownership
- Course enrollment constraints under concurrent requests
- Duplicate attendance prevention and audited corrections
- Exact paise calculations for partial payments and reversals
- Payment idempotency
- Marks range validation and grading-policy boundaries
- Result publication and audited amendments
- Student isolation in the student portal
- Seed script repeatability
- Consistent API error responses
- Empty/loading/error states in UI

## Quality gates for each feature
- Backend builds.
- Frontend builds/lints as configured.
- Relevant unit tests pass.
- Relevant integration tests pass or any blocker is explicitly documented.
- No known security issue is hidden.
- API contract and business-rule docs match implementation.
- Manual acceptance criteria are checked.

## Test data
Use isolated test database/collections and fictional records. Tests must not depend on production or a developer's personal database.
