# Definition of Done

A task is complete only when applicable items below are satisfied.

## Scope and code
- Matches the approved feature prompt and acceptance criteria.
- Changes are limited to the feature and required shared infrastructure.
- Existing conventions are followed.
- No dead code, accidental debug logging, secrets, or unrelated rewrites.
- Business rules live in the backend, not only in the UI.

## Data and API
- DTOs and validation are explicit.
- Authorization and resource ownership are checked server-side.
- MongoDB indexes and uniqueness requirements are respected.
- Duplicate/retry/concurrency behavior is defined for important writes.
- Errors are safe and consistent.
- Audit history is present for required sensitive operations.

## UI
- Loading, empty, error, success and unauthorized states are handled where applicable.
- Forms have validation and useful error messages.
- Controls work; no fake buttons.
- Responsive and keyboard-accessible behavior is considered.
- Consequential actions have appropriate confirmation.

## Verification
- Relevant build and lint commands run.
- Relevant tests run and results are reported honestly.
- Any unrun test or known limitation is stated.
- Final diff is inspected.
- Relevant docs and implementation checklist are updated.

## Completion report
Include summary, files changed, commands run, test results, known limitations, and next task.
