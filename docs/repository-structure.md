# Proposed Repository Structure

The exact tree is finalized after foundation setup. This is a target, not a demand to create every file immediately.

```text
educore/
  AGENTS.md
  README.md
  docs/
    PRODUCT.md
    SCOPE.md
    roles-and-permissions.md
    business-rules.md
    architecture.md
    repository-structure.md
    database-schema.md
    indexes.md
    api-contracts.md
    design-system.md
    screens.md
    navigation.md
    seed-data.md
    testing.md
    implementation-plan.md
    policies/
    workflows/
    adr/
    prompts/
  frontend/
    src/
      app/
      layouts/
      pages/
      features/
      shared/
        api/
        components/
        hooks/
        utils/
  backend/
    EduCore.sln
    src/
      EduCore.Api/
      EduCore.Application/
      EduCore.Domain/
      EduCore.Infrastructure/
    tests/
      EduCore.UnitTests/
      EduCore.IntegrationTests/
  scripts/
    seed/
  .gitignore
```

## Structure rules
- Do not create empty folders just to match the tree.
- Feature modules own their feature-specific UI and hooks.
- Shared folders contain genuinely shared code only.
- Keep frontend and backend independently buildable.
- Keep test projects close to the backend solution.
- Keep seed scripts and fixture data version-controlled, excluding secrets and personal data.
- If a different structure is approved, update this document and record why.
