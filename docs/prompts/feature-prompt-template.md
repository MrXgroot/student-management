# Antigravity Feature Prompt Template

## Task
Implement: [one specific feature]

## Required reading
- `AGENTS.md`
- Relevant product/scope/architecture/workflow/business-rule/policy/API/database/UI documents

## Before editing
- Inspect repository structure, Git status, and existing related code.
- Summarize the current implementation and identify the smallest safe change.
- Identify assumptions or conflicting docs; ask before inventing a major policy.

## Scope
### Included
- [explicit outcomes]

### Excluded
- [explicit non-goals]

## Behavior and rules
- [approved workflow]
- [business invariants]
- [authorization requirements]
- [failure and retry behavior]

## Implementation expectations
- Frontend: React + Vite, JavaScript only, TanStack Query for server state.
- Backend: ASP.NET Core API with thin endpoints, services/policies, repositories.
- MongoDB: follow documented collection and index design.
- API: follow versioning, DTO and error conventions.
- No unrelated refactors or new packages without justification.

## Acceptance criteria
- [observable user behavior]
- [API behavior]
- [database behavior]
- [permissions]
- [edge cases]

## Tests
- Unit tests:
- Integration tests:
- UI/manual acceptance:

## Documentation
Update the relevant workflow, policy, API contract, schema/index, screen, and implementation checklist documents.

## Completion report required
List changed files, tests/commands actually run and results, known limitations, and next recommended task. Do not claim tests passed if they were not run.
