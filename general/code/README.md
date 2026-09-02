# Shared code assets

This folder contains reusable helpers and mock data generators shared across session snapshots.

## Contents

- **Mock market data generator** - creates realistic stock prices and market mood snapshots for testing.
- **Finance model helpers** - shared types for quotes, transactions, and portfolio calculations.
- **Configuration patterns** - environment variable binding and user-secrets usage examples.

## Design principle

Each `session-0X/code` folder is designed to be fully self-contained and independently runnable. Shared code here is read-only reference material and template building blocks, not a required dependency.

**Current readiness:** Session 1 is runnable. Sessions 2–4 currently contain unsupported .NET 9 placeholder applications — they are not finished samples and are pending replacement with real .NET 10 implementations. Do not attempt to run Sessions 2–4 code or use it as reference until their implementations are published.

Never create tight coupling between session folders. If a session needs a helper, copy it into the session folder or build it independently using this folder as a reference.

## Building from shared code

Sessions do not project-reference this folder. Instead:

1. Copy any needed types or logic into the session's `code` folder.
2. Adapt as needed for the session's specific goals.
3. Treat the session folder as the canonical source of truth.

This approach keeps each snapshot independent and runnable without rebuilding dependencies.

## Adding to shared code

Update this folder only when:

- A pattern appears in multiple sessions and the duplication becomes hard to maintain.
- The pattern is stable and unlikely to change with new session requirements.
- The shared code is documented and tested.

Before adding, consult the team to ensure the pattern is truly reusable.
