# EduCore — Student & Academic Management Platform

EduCore is a fictional, India-focused college management system built as a serious showcase project for Arise Craft.

## Product goal
Manage the connected lifecycle of a college student: admissions, enrollment, academics, attendance, fees, examinations, results, and progression.

## Technology decisions
- Frontend: React + Vite, JavaScript only (no TypeScript)
- Backend: C# + ASP.NET Core Web API
- Database: MongoDB
- Frontend server state: TanStack Query
- Frontend UI state: local component state; use a shared store only when genuinely needed
- Backend tests: xUnit; add integration tests for API/database workflows
- API documentation: OpenAPI/Swagger
- Version control: Git
- Deployment packaging: Docker when the application is ready

## Current status
Planning and specification phase. Do not assume implementation already exists.

## Start here
1. `AGENTS.md`
2. `docs/PRODUCT.md`
3. `docs/SCOPE.md`
4. `docs/workflows/`
5. `docs/business-rules.md`
6. `docs/architecture.md`
7. `docs/database-schema.md`
8. `docs/design-system.md`
9. `docs/implementation-plan.md`

## Non-negotiable principles
- No one-shot implementation prompt for the entire application.
- Work in small, reviewable, tested increments.
- Keep business rules in the backend.
- Do not invent academic policies when the institution's policy is configurable or undecided.
- Use fictional Indian demo data only.
- Never report tests as passed unless they were actually run.
- Update documentation when an approved decision changes.
