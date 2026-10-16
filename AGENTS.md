# Antigravity Instructions — EduCore

## Mission
Build EduCore incrementally as a maintainable college management system. Treat the documents in `docs/` as the project source of truth.

## Before every task
1. Read this file and the relevant documents in `docs/`.
2. Inspect the existing repository and current Git status before editing.
3. Identify the task's prerequisites and the smallest safe scope.
4. Summarize the intended changes and files before a broad or risky change.
5. Do not recreate, replace, or restructure the project without explicit need.

## Technology constraints
- Frontend uses React + Vite and plain JavaScript. Do not introduce TypeScript.
- Backend uses C# and ASP.NET Core Web API.
- Database is MongoDB.
- Use TanStack Query for remote/server state in the frontend.
- Prefer local React state for local UI state.
- Keep controllers/endpoints thin; business logic belongs in application services and policies.
- Repositories own database access. Do not put MongoDB queries in controllers or UI components.
- Avoid unnecessary abstractions, dependency injection frameworks beyond the built-in ASP.NET Core DI, and premature microservices.
- Do not add packages without a concrete reason.

## Implementation rules
- Implement one vertical feature at a time: UI, API, validation, business logic, persistence, tests, and documentation.
- Do not implement unrelated features in the same task.
- Follow existing naming and folder conventions.
- Validate input at API boundaries, and enforce business invariants in the backend.
- Use explicit DTOs for API requests and responses; do not expose database documents directly.
- Use UTC timestamps for stored instants; format dates for users using the institution's configured timezone and `dd/MM/yyyy` where appropriate.
- Store money as integer paise where amounts are denominated in INR; never use floating-point arithmetic for money.
- Use stable identifiers and explicit status values. Do not use display labels as identifiers.
- Handle duplicate requests and concurrency for important operations such as payment recording, enrollment, attendance correction, and result publication.
- Use audit records for sensitive or consequential changes.
- Do not automatically treat a monitoring flag as proof of misconduct.
- Do not silently delete financial, examination, or audit history.

## MongoDB rules
- Define collection purpose, document shape, indexes, validation expectations, and ownership before implementing a collection.
- Use unique indexes for uniqueness requirements where supported by the data model.
- Make seed scripts repeatable (idempotent); never create duplicate seed data on repeated runs.
- Never seed real personal information, real credentials, or real payment details.
- Do not claim multi-document atomicity unless the MongoDB deployment and transaction configuration support it. MongoDB multi-document transactions require an appropriate replica-set/sharded deployment.
- Use optimistic concurrency or another documented strategy where simultaneous updates could conflict.
- Keep embedded data bounded; do not embed unbounded histories or growing arrays in a single document.

## Testing and completion
- Add unit tests for business policies and services.
- Add integration tests for important workflows and persistence constraints.
- Test authorization at the API level, not only by hiding UI navigation.
- Run relevant tests and builds. Report exact commands and results.
- If tests cannot be run, state that clearly; never claim success.
- Inspect the final diff for unrelated changes, secrets, debug code, and accidental generated files.
- Update relevant documentation and the implementation checklist.
- Finish with: summary, changed files, tests executed/results, known limitations, and next step.

## Never
- Never implement the whole application from one prompt.
- Never guess an undecided policy and present it as approved.
- Never remove existing working behavior to make a test pass without explaining the tradeoff.
- Never store passwords in plain text.
- Never log passwords, tokens, private student documents, or full sensitive payment details.
- Never mark a task complete with known failing tests hidden from the user.
