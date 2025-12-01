# Change Log *(newest first)*

### 2025-12-03

* **Playground separation and cleanup**
  Demo, benchmark, and testing utilities now live in their own files (`Showcase.cs`, `Benchmarks.cs`, `TestingArea.cs`), so sample code stays organized and reuse-ready without toggling unrelated regions.
* **Benchmark correctness and clarity tweaks**
  Benchmarks track miss distances with `BigInteger`, initialize exponents deterministically, and align variable naming (`answer`, `shifted`) with the surrounding calculations to avoid silent overflow and make pow approximation checks easier to audit.

### 2025-11-01

* **BigInteger → BigFloat zero-constructor follow-up**
  Zero values now subtract the requested precision directly from `binaryScaler` in the BigInteger-to-BigFloat constructor.

### 2025-10-24

* **Fixed-width constructor & zero-output fixes**
  Adjusted fixed-width constructors to align with new precision defaults, repaired zero binary-string output when no guard bits are present, and added explicit handling for `Int128.MinValue`.

### 2025-10-23

* **Constructor refinements**
  Renamed `addedBinaryPrecision` to `binaryPrecision`, tightened the default precision for the `int` overload, and removed redundant size validation checks.

### 2025-10-12

* **Remainder optimization and bug fix**
  Remainder avoids oversized shifts by splitting paths on scale differences, leverages modulus properties for trailing-zero divisors, and uses modular exponentiation to keep work logarithmic in the shift amount.
* **Formatting cleanup**
  Minor indentation corrections were applied to the optimized remainder path to maintain style consistency.
* **Diagnostic comment for zero formatting**
  A commented-out guard in `ToStringDecimal` documents a potential early return for zero mantissas, clarifying future formatting considerations.

### 2025-10-09

* **Added binary precision hooks**
  Additional constructors (e.g., for `short`, `ushort`) expose `addedBinaryPrecision` so extra guard bits can be reserved without manual shifts, aligning with the broader constructor refactor.

### 2025-10-06

* **Comprehensive constructor overhaul**
  Most primitive-type constructors gained `valueIncludesGuardBits` and precision knobs so callers can control guard-bit application, while zero inputs now short-circuit with correct scale/size metadata before returning.
* **New `IConvertible` surface**
  `BigFloat` implements `IConvertible`, enabling direct conversions to .NET primitives (with overflow guards) and centralized handling for unsupported casts.
* **Rounding and truncation suite**
  New helpers (`Round`, `RoundToInteger`, `TruncateByAndRound`, and related accuracy-preserving truncators) allow integer rounding with guard-bit awareness, falling back to `ZeroWithAccuracy`/`OneWithAccuracy` when precision is exhausted.
* **Guard-aware multiplication**
  The `*` operator now short-circuits whenever either operand is strictly zero, returning a zero value that carries the tighter of the input accuracies, before delegating to the existing adaptive multiplier.

### 2025-09-23

* **Optimized `BigFloat.Min`/`BigFloat.Max`**
  Helpers select results without reallocating when operands share representation, fall back to a comparison with canonical tie-breaking, and prefer higher-precision encodings when values compare equal.

### 2025-09-21

* **Simplified integer check**
  `IsInteger` now depends solely on `Ceiling()` and `Floor()` equality, ensuring consistency with the reworked rounding routines.
* **Rebuilt rounding helpers**
  Introduced `HasWorkingFractionBits`; rewrote `Ceiling`/`CeilingPreservingAccuracy` to avoid lowering values when only guard bits are set; expressed `Floor` variants via negation, yielding canonical integer encodings while preserving accuracy metadata.
* **Strengthened truncation paths**
  `TruncateToIntegerKeepingAccuracy` now zeroes values whose fractional window exceeds stored precision, and `Truncate` clears guard bits while recomputing scale/size so canonical form is maintained.
* **Streamlined operator `+(BigFloat, int)`**
  Short-circuits negligibly small addends and aligns the integer operand directly with the mantissa so the result inherits the original accuracy context.
* **Introduced `ToNearestInt`**
  Round-to-nearest conversion (ignoring guard bits); updated the explicit `int` cast to round at the guard boundary before truncating, guaranteeing casts always round toward zero while providing an opt-in nearest helper.
* **Expanded binary formatting**
  `WriteBinaryToSpan` and `ToBinaryString` gained optional guard-bit separators (`|`), improved zero handling, and precise buffer sizing logic so callers can expose or omit guard bits without reallocations.
* **Overhauled equality/comparison infrastructure**
  Extracted canonical components, aligned mantissas via `CmpAligned`, simplified integer equality overloads, marked `IsExactMatchOf` obsolete in favor of `IsBitwiseEqual`, and added `RoundsToNearest(BigInteger)` for integer-part checks.

### 2025-08-10

* **Refactored precision/accuracy management**
  New `AdjustAccuracy`, `SetAccuracy`, `SetPrecision`, `SetPrecisionWithRound`, and `AdjustPrecision` APIs centralize mantissa shifting and rounding while deprecating `ExtendPrecision`/`ReducePrecision`, giving callers explicit control over precision growth or reduction.
* **Rounding helper rename & docs**
  Renamed and documented core rounding helpers in `BigIntegerTools` (`RoundingRightShift`, `RoundingRightShiftWithCarry`) while leaving obsolete aliases for backward compatibility, clarifying away-from-zero semantics and carry handling.

### 2025-08-05

* **Decimal parsing accuracy adjustments**
  `TryParseDecimal` now computes guard bits from the `|` delimiter, returns `ZeroWithAccuracy` for zero mantissas, and nudges unary inputs for better precision, ensuring zero results preserve requested accuracy.
* **Icon refresh for NuGet package**
  The package icon under `BigFloatLibrary/images/icon.png` was replaced with a new asset, matching the refreshed branding noted in the commit log.
* **NuGet metadata update (v2.2.0)**
  `BigFloatLibrary.nuspec` bumps the package to version **2.2.0**, points to [https://BigFloat.org](https://BigFloat.org), and clarifies that BigFloat is a “C# struct library.”
* **Improved multiplication’s zero handling**
  `operator *` short-circuits when either operand is a strict zero and returns `ZeroWithAccuracy` aligned to the tighter operand, preserving context information for downstream calculations.
* **Updated parsing accuracy rules**
  Decimal parsing now translates the precision delimiter (`|`) into guard-bit counts more directly and emits `ZeroWithAccuracy(-scaleAmt)` for zero literals so the resulting value carries the intended accuracy.
* **NuGet metadata bump (summary)**
  Bumped the NuGet metadata to version **2.2.0**, pointing the package at [https://BigFloat.org](https://BigFloat.org) and clarifying the description as a “C# struct library.”
