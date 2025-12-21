# BigFloat Library Specification

*Updated: December 21, 2025 — technical reference for developers and AI systems*

> **Scope.** This document specifies the behavior, data model, key APIs, rounding/comparison semantics, parsing/formatting, math functions, precision control, and range/convertibility characteristics of **BigFloat** as implemented in the current sources. It supersedes the August 2025 draft. 

---

## What’s new since v4.0.0 (2025‑12‑03)

* **Documentation consolidated for v4.2.0.** This spec now compacts the 4.1.x feature wave into a single reference, aligning the README and docs site with the 2025-09 through 2025-12 code changes.
* **Package metadata & release labeling.** NuGet metadata and public-facing documentation now reference **v4.2.0** while preserving the underlying behavior described below.
* **Provenance note.** The v4.2.0 documentation refresh credits assistance from OpenAI’s GPT‑5.2‑Codex‑Max in the provenance section for transparency.

---

## What’s new since 2025‑08‑05 (high level)

* **Constructor refactor & precision knobs.** All primitive‑type constructors expose precision controls (`binaryPrecision`) and a `valueIncludesGuardBits` switch; zero‑inputs short‑circuit with correct metadata. Fixed‑width overloads and `Int128.MinValue` handling were cleaned up, and the BigInteger‑to‑BigFloat zero path now offsets `binaryScaler` by the requested precision.
* **Rounding/Truncation suite.** Canonical `Round`, `TruncateByAndRound`, `TruncateToIntegerKeepingAccuracy`, and related helpers; `Ceiling/CeilingPreservingAccuracy` were rewritten to avoid lowering values when only guard bits are set. `IsInteger` delegates to `Ceiling()==Floor()` for consistency with these paths.
* **Comparison overhaul.** Clear split of value equality (`CompareTo/Equals`), ULP‑tolerant comparisons (`CompareUlp`/`EqualsUlp`), and deterministic total orders (bitwise vs. zero‑extension). New comparer types included.
* **Binary/hex/decimal formatting.** Binary writer gained optional guard‑bit separator (`|`) and exact buffer sizing; hex/scientific paths reworked for correctness and performance. Formatting of zero binary strings without guard bits was fixed.
* **Parsing.** Decimal parsing maps precision delimiter `|` to guard bits; supports `X` placeholders, exponential forms, brackets/quotes, and `0x…` / `0b…`.
* **Range helpers.** Introduced `FitsInADouble`, `FitsInADoubleWithDenormalization`, `FitsInAFloat`, `FitsInAFloatWithDenormalization`, and `FitsInADecimal`. *(Note: these are properties, not a method with parameters.)*
* **IConvertible.** `BigFloat` implements `IConvertible` with safe overflow checks and a `ToType` dispatcher.
* **Arithmetic touch‑ups.** Safer/faster remainder path; multiply short‑circuits strict zero while preserving the tighter accuracy; divide keeps hooks for adaptive algorithms while using the standard path.
* **Playground & benchmarks.** Playground sample, benchmark, and testing utilities are separated into dedicated files to keep demos organized and repeatable (library behavior unaffected).
* **Packaging.** NuGet metadata bumped **v4.2.0** in Dec 2025; ongoing fixes through Oct–Dec per change log.

---

## Data model & invariants

**Representation.** A `BigFloat` encodes an integer mantissa `BigInteger _mantissa` **including** guard bits, a base‑2 **Scale** (shift of the radix point), and `_size` (bit‑length of the mantissa, including guard bits). Guard bit width is a library constant:

```text
GuardBits = 32
```

* **Mantissa (with guard).** `_mantissa` stores data plus 32 guard bits (LSBs). 
* **Scale / Accuracy.** `Scale` is the base‑2 exponent applied to `_mantissa`; **Accuracy** is defined as `-Scale` (fractional‑bit budget).
* **Size & precision.** `Size` = `max(0, _size − GuardBits)`; `SizeWithGuardBits` = `_size`; **Precision** = `_size − GuardBits`.
* **Binary exponent.** `BinaryExponent = Scale + _size − GuardBits − 1`. 
* **Zero semantics.** Guard-bit-aware zero detection treats values with `_size == 0` or with `_size < GuardBits` **and** `_size + Scale < GuardBits` as zero, ensuring denormalized “near-zero” encodings collapse to zero. `IsStrictZero` checks `_mantissa.IsZero` only, while `Sign`, `IsPositive`, and `IsNegative` all reuse the same near-zero rule to collapse sign when the tolerance is met.
* **Canonicalization hook.** Rounding that removes guard bits is done via `BigIntegerTools.RoundingRightShift(…, GuardBits)` and is the basis for comparisons and many conversions. (See Formatting and Comparison sections.)

---

## Construction & conversion

### Constructors (selected)

Each primitive overload supports optional precision configuration; zero inputs return early with correct scale/size. Examples:

* `BigFloat(int value, int binaryScaler = 0, bool valueIncludesGuardBits = false, int binaryPrecision = 31)`
* `BigFloat(long value, …, int binaryPrecision = 63)`; `BigFloat(ulong value, …, int binaryPrecision = 64)`
* `BigFloat(Int128 value, …, int binaryPrecision = 127)`; `BigFloat(UInt128 value, …, int binaryPrecision = 128)`
* `BigFloat(double value, int binaryScaler = 0, int addedBinaryPrecision = 24)` *(subnormals handled; NaN/∞ rejected)*
* `BigFloat(BigInteger value, int binaryScaler = 0, bool valueIncludesGuardBits = false, int addedBinaryPrecision = 0)`
  Zero BigInteger inputs adjust `binaryScaler` downward by the requested precision before returning, keeping zeros canonical at the intended accuracy. All constructors ensure `_size` matches the real bit length and call `AssertValid()` in DEBUG.

**Accuracy context helpers.**
`ZeroWithAccuracy(int accuracy)` and `OneWithAccuracy(int accuracy)` produce canonical zeros/ones carrying an explicit accuracy (least‑precision) context. 

### Casts and `IConvertible`

* **Implicit from** `sbyte`,`byte`,`short`,`ushort`,`int`,`uint`,`long`,`ulong`,`Int128`,`UInt128`,`decimal`. 
* **Explicit to** integral types and `BigInteger`; casts remove guard bits with rounding at the guard boundary and truncate working fraction as required. The explicit `int` path documents that rounding at the guard boundary precedes truncation; a convenience `ToNearestInt` is provided. 
* **Explicit to double/float** synthesize IEEE‑754 bits directly. Normal values round the mantissa to 53/24 bits using
  `ShiftRightRoundEven` (round‑to‑nearest, ties‑to‑even) and re‑bias the exponent; carry may promote the significand by one bit
  to handle 1.111… → 10.000… transitions. Subnormals compute <code>n = round(|x| · 2<sup>1074</sup>)</code> (or 2<sup>149</sup> for
  <code>float</code>) with the same tie‑to‑even helper; <code>n = 0</code> returns ±0 with the original sign, <code>n ≥ 2<sup>52</sup></code>
  (<code>2<sup>23</sup></code> for <code>float</code>) rounds to the smallest normal, otherwise <code>n</code> is emitted as a subnormal significand. Overflow
  returns ±∞. Values representable in the target type round‑trip through <code>double → BigFloat → double</code> (or <code>float</code>) without
  changing the bit pattern.
* **`IConvertible`**:

  * `ToDouble/ToSingle` check exponent ranges (`biasedExp`), throwing `OverflowException` on overflow; subnormals/underflow reach ±0. `ToDecimal` delegates the decimal cast; unsupported casts (Boolean/Char/DateTime) throw. `ToType` handles known primitives and otherwise delegates through `Convert.ChangeType((double)this, …)`.

### Range/fit helpers

* `FitsInADouble`, `FitsInADoubleWithDenormalization`, `FitsInAFloat`, `FitsInAFloatWithDenormalization`, `FitsInADecimal` provide quick range checks (precision loss ignored). *(These are boolean properties.)* 

---

## Arithmetic & precision management

### Core operators

* **Addition/Subtraction.** Scales are aligned; smaller operand may be rounded right to avoid huge shifts. Several fast‑paths short‑circuit when one operand is below the other’s precision window. 
* **Multiplication.** Adaptive strategy with small size‑difference elision; strict‑zero short‑circuit returns a zero that **preserves the tighter input accuracy**. 
* **Division.** Hooks for size‑adaptive algorithms (`DivideSmallNumbers`/`DivideLargeNumbers`) exist; the **standard path** is used currently, with output size targeted from operand sizes. Division by strict zero throws. 
* **Remainder/Mod.** Remainder is scale‑aware and uses modular arithmetic to avoid oversized shifts (trailing‑zero optimizations included). `Mod` adjusts sign semantics from remainder as expected. 
* **Bit shifts.** `<<`/`>>` adjust only `Scale` (precision unchanged). Mantissa‑only shifts are exposed as `LeftShiftMantissa`/`RightShiftMantissa`.

### Precision/accuracy APIs

* **Set/adjust.** `SetAccuracy`, `AdjustAccuracy` (aliases for precision control), `SetPrecision`, `SetPrecisionWithRound`, `AdjustPrecision(deltaBits)` with documented semantics (left shift to extend; rounded drop to reduce). 
* **Rounding & truncation.** `Round()`, `Truncate()`, `TruncateByAndRound(x, bits)`, `TruncateToIntegerKeepingAccuracy()`. `Ceiling()` and `CeilingPreservingAccuracy()` were rewritten so values with *only guard‑fraction* do not move down; floors are expressed via negation for canonical form.
* **Next/Prev ULP.** `NextUp/NextDown` step the guard area by ±1; `NextUpInPrecisionBit/NextDownInPrecisionBit` step by one **in‑precision** unit; half‑unit helpers also provided. 

---

## Comparisons & equality

BigFloat exposes three distinct semantics:

1. **Canonical value semantics** (`CompareTo`, `Equals`, operators): guard bits are rounded away using the library rule, carry is handled, and magnitudes are aligned. Use for .NET value semantics and hashing. 
2. **ULP‑tolerant numerics** (`CompareUlp`, `EqualsUlp`, plus `Is*Ulp` helpers): align scales, then ignore a caller‑specified number of LSBs (optionally counting guard bits). Use for stopping criteria and “close enough” tests. A faster, coarser `CompareUlpFast` is available. 
3. **Deterministic ordering**:

   * `CompareTotalOrderBitwise` is a strict total order over the **encoding** (distinguishes 2.5 vs 2.50).
   * `CompareTotalPreorder` collapses **zero‑extensions** of the same value (2.5 ≡ 2.50) for stable sort keys.
     Comparer types (`ValueComparer`, `TotalOrderComparer`, `UlpToleranceComparer`, `BitwiseEqualityComparer`) are provided. 

Integer equality overloads are available (e.g., `== long/ulong/BigInteger`) and route through the canonicalized paths or integer‑rounding helpers. 

---

## Parsing

`Parse`/`TryParse` accept decimal/hex/binary, optional sign, radix point, exponent (`e`/`E`), and a **precision separator** `|` that splits in‑precision vs guard bits (e.g., `1.01|101`). Inputs can include spaces/commas/underscores and be wrapped in quotes/brackets. Decimal parsing supports **`X` placeholders** as out‑of‑precision decimal digits (e.g., `123XXX` behaves like 123×10³). Errors throw or return `false` depending on API. 

* `TryParseDecimal` computes guard bits from `|`, maps `X` to base‑10 scaling, and preserves accuracy on zeros (`ZeroWithAccuracy`). 
* `TryParseHex` tolerates `0x` prefixes and uses hex‑nibble math for precise scaling; `|` counts hex guard nibbles. 
* `TryParseBinary` accepts `0b` inputs, separators, and `|`; negative values use two’s‑complement bit building internally before converting to `BigInteger`. 

---

## Formatting & spans

`BigFloat` implements `IFormattable` and `ISpanFormattable`. Supported format specifiers:

* **Decimal** (`"G"`/`"R"`/default): precision‑aware decimal with optional *digit‑masking* (out‑of‑precision tail as `X…`). 
* **Hex** (`"X"`): optimized radix‑point placement; optional inclusion of guard bits; trailing zeros trimmed. 
* **Binary** (`"B"`): efficient streaming writer with optional **guard‑bit separator** `|` and exact buffer sizing; helper `ToBinaryString(int numberOfGuardBitsToInclude, bool showPrecisionSeparator)`. 
* **Scientific** (`"E"`): normalized mantissa × 10^exp; used for very large/small magnitudes and by explicit call `ToStringExponential`. 

Debug views (`DebuggerDisplay`, `DebugPrint()`) show combined decimal/hex/binary snapshots and guard rounding direction. 

---

## Mathematical functions

* **Inverse / Abs.** High‑precision reciprocal and absolute value. 
* **Exponentiation.** `Pow(BigFloat, int)` uses binary exponentiation; for small precisions it opportunistically leverages `double` for a seed and then restores scale/precision. `PowerOf2` has a capped‑precision variant for performance.
* **Roots.** `Sqrt` implements a Newton‑Plus integer‑root core with carefully normalized inputs; `NthRoot`/`CubeRoot` include scaling and fast double‑seeded starts, iterating to convergence under precision control. 
* **Logarithms.** `Log2(BigFloat)` returns `double`, combining exponent with a normalized mantissa; `Log2Int` returns the integer exponent and rejects non‑positive inputs. 
* **Trig.** `Sin`, `Cos` use Payne–Hanek‑style range reduction with π from the constants subsystem, then select either a **Taylor** kernel for tiny angles or a **halve‑and‑double** scheme; `Tan` is `Sin/Cos`. A small‑precision, hardware‑accelerated `SinAprox` is available. 

**Constants subsystem.** Math functions call `Constants.GetConstant(Catalog.Pi, precision)` for π (and derivative values), with constants defined in the `Constants*.cs` set. 

---

## Constants system (overview)

The constants module is organized via a catalog and builder/types to materialize values (π, e, √2, etc.) at requested precision. Access patterns:

```csharp
var pi = Constants.GetConstant(Catalog.Pi, wantedBits);
```

See `Constants.cs`, `ConstantInfo.cs`, `ConstantsCatalog.cs`, `ConstantBuilder.cs` for cataloging and assembly. 

---

## Edge cases & notes

* **Overflow/underflow (casts).** IEEE‑754 casts to double/float synthesize exponent and mantissa fields directly; overflow yields ±∞; too small → signed zeros; NaN/∞ inputs to constructors are rejected. 
* **Integer detection.** `IsInteger` now delegates to `Ceiling()==Floor()` for consistency with rounding paths. 
* **Min/Max.** `Min`/`Max` prefer higher‑precision encodings on ties after canonical comparison. 
* **Bitwise operators.** `~` complements bits within the stored precision window and shrinks size at least one bit; use with care. 

---

## Examples

**Precision control & rounding**

```csharp
var x = new BigFloat(12345, binaryScaler: -10);   // 12.056…
var y = BigFloat.SetPrecisionWithRound(x, 20);    // clamp to 20 bits
var z = BigFloat.TruncateByAndRound(y, 3);        // remove 3 LSBs (rounded)
```



**ULP‑aware compare**

```csharp
bool equalWithin2Ulps = a.EqualsUlp(b, ulpTolerance: 2, ulpScopeIncludeGuardBits: false);
```



**Parse with precision separator and hex/binary**

```csharp
var d = BigFloat.Parse("1.2345|678");     // decimal; guard from '|'
var h = BigFloat.Parse("0xAB.CD|EF");     // hex; guard nibbles from '|'
var b = BigFloat.Parse("0b1.01|101");     // binary; guard bits from '|'
```



**Range‑safe cast**

```csharp
if (value.FitsInADouble) {
    double v = (double)value;
}
```



**Binary formatting with guard separator**

```csharp
string bits = value.ToBinaryString(numberOfGuardBitsToInclude: 32, showPrecisionSeparator: true);
// e.g., "101.001|1100"
```



## Future ideas

* **Repeating fractional metadata:** A former `_extraPrecOrRepeat` field explored marking repeating trailing bits or virtual zeros to preserve pattern intent without storing additional data. The concept remains unimplemented; any revival should document encoding and rounding rules before resurfacing in code.
* **Exact small-integer division:** The `int / BigFloat` operator currently routes through `new BigFloat(a) / b`; a future enhancement could preserve the exact numerator bits to avoid redundant rounding paths for small integers.

---

## Versioning & provenance

* Current NuGet metadata in repo indicates **v4.2.0** (December 2025), with ongoing fixes (Sep–Dec). See `ChangeLog.md` for dated entries (constructor precision, remainder optimization, `IConvertible`, rounding suite, zero handling, formatting).
* Documentation refresh for v4.2.0 prepared with assistance from OpenAI’s GPT‑5.2‑Codex‑Max.

---

## Appendix — Key properties & methods (index)

* **Core:** `GuardBits`, `Scale`, `Size`, `SizeWithGuardBits`, `BinaryExponent`, `Precision`, `Accuracy`, `IsZero`, `IsStrictZero`, `Sign`.
* **Arithmetic:** `+ - * / %`, `Mod`, `PowerOf2`, `Min/Max`, `SplitIntegerAndFractionalParts`. 
* **Precision/Rounding:** `Round`, `Truncate`, `TruncateByAndRound`, `TruncateToIntegerKeepingAccuracy`, `SetPrecision`, `SetPrecisionWithRound`, `AdjustPrecision`, `ZeroWithAccuracy`, `OneWithAccuracy`.
* **Comparisons:** `CompareTo`, `Equals`, `CompareUlp*`, `CompareTotalOrderBitwise`, `CompareTotalPreorder`, comparer classes. 
* **Parsing/Formatting:** `Parse/TryParse` (decimal/hex/binary with `|` & `X`), `ToString` (`"G","R","X","B","E"`), `ToBinaryString`, `ToHexString`, span writers.
* **Math:** `Inverse`, `Pow`, `Sqrt`, `NthRoot`, `CubeRoot`, `Sin`, `Cos`, `Tan`, `Log2`, `Log2Int`. 
* **Conversions:** implicit/explicit casts, `IConvertible`, `FitsIn*` (range checks).

---

### Notes for maintainers

* This spec intentionally mirrors the **code‑as‑written** (not future comments in stubs). E.g., division has adaptation hooks but defaults to the standard algorithm today. 
* The August spec’s “`FitsInADouble(allowDenormalized: …)`” was corrected to the **property** pattern (`FitsInADouble`, `FitsInADoubleWithDenormalization`). 

---

*End of specification.*
