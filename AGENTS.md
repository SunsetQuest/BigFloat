# AGENTS.md – BigFloat

This file is guidance for AI coding agents (especially Codex) working on this repository.

## Project overview

- This repo contains **BigFloat**, a high-precision floating-point library in C#.
- Core library: `BigFloatLibrary/` (main type is `BigFloat` in `BigFloat.cs` plus `Constants.cs`).
- Tests: `BigFloat.Tests/` (NUnit/xUnit-style unit tests for BigFloat behavior).
- Playground / samples: `BigFloatPlayground/`.
- Target: modern .NET (.NET 8 with C# 12 and later).

### Key invariants

When modifying the core library:

- `BigFloat` represents a base-2 floating-point value using:
  - `DataBits` (`BigInteger`) for the mantissa,
  - `Scale` (`int`) for the binary radix point offset,
  - a cached size field (`_size`) for fast bit-length access.
- Do not change semantics without updating `BigFloatSpecification.md` and tests.
- Preserve numerical precision and determinism. Avoid shortcuts that rely on `double`/`decimal` for core arithmetic, except where already used and tested.

## Build and test commands

- Restore and build from the repo root:

  - `dotnet restore`
  - `dotnet build BigFloat.sln`

- Run the full test suite:

  - `dotnet test BigFloat.sln`

Agents should:
- Run tests (or at least propose running them) after changing library code.
- Prefer adding or updating tests in `BigFloat.Tests` when fixing bugs or adding features.

## Code review focus

When acting as a **code review agent**, prioritize:

1. **Correctness & precision**
   - Look for changes that might alter rounding behavior, precision guarantees, or BigFloat’s comparison semantics.
   - Be especially cautious around operations that change `DataBits` or `Scale`.

2. **Performance**
   - Avoid introducing unnecessary allocations or boxing in hot paths.
   - Avoid adding heavy dependencies; this library is intended to remain lightweight.

3. **API stability**
   - Avoid breaking public APIs unless explicitly requested.
   - If an API change is suggested, call out the breaking nature clearly.

4. **Tests & docs**
   - If behavior changes, suggest updates to:
     - Unit tests in `BigFloat.Tests`.
     - `BigFloatSpecification.md` when semantics change.
   - Do not remove tests that check precision or edge cases without a strong reason.

## Style and structure

- Follow the existing C# coding style used in `BigFloatLibrary` (naming, formatting, nullability usage).
- Prefer small, focused methods over large, deeply nested ones, as long as performance is not harmed.
- Keep the library free of unnecessary external dependencies.

## When unsure

- If a request conflicts with documented behavior in `BigFloatSpecification.md` or the README, **point out the conflict** instead of silently changing behavior.
- When proposing a non-trivial refactor, outline:
  - Risk areas (precision, performance),
  - Which tests should be added or updated.
