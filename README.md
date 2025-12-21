# BigFloat Library for C#

BigFloat is a C# struct/class library for arbitrary-precision floating-point numbers, backed by `System.Numerics.BigInteger`. It combines a flexible mantissa, a wide exponent range, and a 32-bit guard-bit region to deliver high-precision arithmetic for very large or very small values. 

Ryan Scott White / [MIT License](http://www.opensource.org/licenses/mit-license.php "The MIT License")  
Updated: December 21st, 2025  
Current BigFloat version: **4.2.0**

- GitHub repo: https://github.com/SunsetQuest/BigFloat
- Documentation site: https://bigfloat.org
- NuGet package (`BigFloatLibrary`): https://www.nuget.org/packages/BigFloatLibrary
- Original CodeProject article: “A BigFloat Library in C#”

---

## Contents

- [What is BigFloat?](#what-is-bigfloat)
- [Key features](#key-features)
- [Use cases](#use-cases)
- [Requirements](#requirements)
- [Installation](#installation)
- [Quick start](#quick-start)
- [Generic math compatibility](#generic-math-compatibility)
- [Core concepts: representation & guard bits](#core-concepts-representation--guard-bits)
- [Precision, accuracy, and guard-bit notation](#precision-accuracy-and-guard-bit-notation)
- [Accuracy & precision APIs (2025+)](#accuracy--precision-apis-2025)
- [Legacy APIs & Migration Guide](#legacy-apis--migration-guide)
- [Math functions & constants](#math-functions--constants)
- [Working with very large / very small numbers](#working-with-very-large--very-small-numbers)
- [Project layout](#project-layout)
- [Background & related work](#background--related-work)
- [License](#license)

---
## What is BigFloat? 
BigFloat is an arbitrary-precision floating-point type implemented as a `readonly partial struct` in the `BigFloatLibrary` namespace. Internally, a `BigFloat` value is stored as:

- A `BigInteger` mantissa (`_mantissa`)
- An integer binary scale (`Scale`)
- A cached bit count (`_size`), which includes guard bits

Mathematically, values are represented in the form:

> `value = mantissa × 2^(Scale - GuardBits)`

Most users can treat `BigFloat` as “a very large `double` with configurable precision,” but it exposes more information and control than standard IEEE types:

- You can inspect the effective precision (`Size`)
- You can tune how many effective working bits you want for integers and conversions
- You can grow or shrink the working accuracy explicitly without silently losing information

---

## Key features

**Arbitrary precision**

- Mantissa backed by `System.Numerics.BigInteger` with a design target of up to billions of bits of precision (limited by memory and patience). 

**Guard bits for stability**

- A fixed **32 hidden guard bits** live in the least-significant portion of the mantissa. These bits are not counted as “in-precision,” but they carry extra context so chained operations round more accurately.

**Binary-first design**

- BigFloat is fundamentally base-2; decimal formatting/parsing is layered on top.
- Decimal input like `0.1` is converted to repeating binary internally; the API makes these trade-offs explicit rather than hiding them.

**Accuracy & precision controls (2025 refresh)**

Modern APIs are available for explicitly managing the accuracy “budget”:

- `AdjustAccuracy`, `SetAccuracy`
- `SetPrecisionWithRound`, `AdjustPrecision` (and compatibility helpers around `ExtendPrecision` / `ReducePrecision`)
- Rounding helpers like `ToNearestInt` and truncation helpers that preserve accuracy metadata

**Rich operator and helper set**

- Full arithmetic and comparison operators (`+ - * / %`, `==`, `<`, `>`, etc.)
- Constructors from `int`, `long`, `ulong`, `double`, `decimal`, `BigInteger`, and string
- Conversion helpers such as `FitsInADouble`, explicit casts to primitive numeric types, and formatting with standard and custom format strings

**Math functions & algorithms**

- Extended math library (exponential, log, roots, powers, trig, etc.) implemented in `BigFloatMath` and related partials.
- High-performance algorithms such as Newton-Plus-style square root, Karatsuba multiplication, and Burnikel–Ziegler-style division for very large operands.

**Constants library**

- `Constants.cs` exposes fundamental constants such as π and e with configurable accuracy.
- Pre-computed decimal value files in the `values` folder extend constants up to roughly 1,000,000 digits.

---

## What's new since v4.0.0 (2025-12-03)

The 4.1.x line (Sep–Dec 2025) introduced the most significant updates since v4.0.0. This v4.2.0 documentation refresh consolidates those changes in one place:

- Precision-aware constructor refactor with `binaryPrecision` knobs, guard-bit flags, and stronger zero handling (including fixed-width tuning and `Int128.MinValue` coverage).
- Expanded rounding & truncation helpers (`Round`, `TruncateByAndRound`, `RoundToInteger`, `ToNearestInt`) with guard-bit-aware behaviors.
- `IConvertible` support and safer primitive casts with explicit overflow handling.
- Comparison and ordering updates (`CompareTotalOrderBitwise`, `CompareTotalPreorder`, enhanced `Min`/`Max`, and ULP helpers).
- Parsing/formatting enhancements, including guard-bit separators in binary output and improved decimal precision parsing.
- Arithmetic refinements (optimized remainder, strict-zero multiply short-circuit) plus playground/benchmark code separation for clearer samples.

---

## Use cases

BigFloat is designed for scenarios where IEEE `double`/`decimal` are not accurate enough:

- Scientific and engineering simulations that accumulate error over long compute chains
- Financial or actuarial models where rounding behavior must be controlled and auditable
- Cryptographic or number-theoretic experimentation (huge exponents, large primes)
- Teaching / research into floating-point and arbitrary-precision arithmetic

---

## Requirements

BigFloat follows the modern .NET toolchain.

**Runtime / SDK**

- .NET **8.0** SDK or later (the library targets `net8.0`, `net9.0` or `net10.0`). 

**Language**

- C# 12 or later (the version shipped with the .NET 8 SDK at the time of writing). 

**Dependencies**

- `System.Numerics.BigInteger` (ships with .NET) – no external library dependencies.

Check your SDKs with:

```bash
dotnet --list-sdks
````

and confirm a `8.x`, `9.x`, or `10.x` entry is available.

---

## Installation

### Option 1 – NuGet (recommended)

Add the library via NuGet:

```bash
dotnet add package BigFloatLibrary
```

or via `PackageReference`:

```xml
<ItemGroup>
  <PackageReference Include="BigFloatLibrary" Version="*" />
</ItemGroup>
```

Refer to NuGet for the current package version (the README describes the 4.2.0 library APIs; NuGet version numbers may differ). ([NuGet][1])

### Option 2 – From source

1. Clone the repository:

   ```bash
   git clone https://github.com/SunsetQuest/BigFloat.git
   ```

2. Add the `BigFloatLibrary` project to your solution.

3. Reference it from your main project.

4. Optionally include `Constants.cs` and the `values` folder for extended constant precision. ([GitHub][2])

---

## Quick start

A minimal console application using BigFloat:

```csharp
using System;
using BigFloatLibrary;

class Program
{
    static void Main()
    {
        // Standard double loses precision
        double d = 0.1 + 0.2;
        Console.WriteLine($"Double: 0.1 + 0.2 = {d}");
        Console.WriteLine($"Equals 0.3? {d == 0.3}");

        // BigFloat maintains a stable representation
        BigFloat x = new("0.1");
        BigFloat y = new("0.2");
        BigFloat sum = x + y;

        Console.WriteLine();
        Console.WriteLine($"BigFloat: {x} + {y} = {sum}");
        Console.WriteLine($"Equals 0.3? {sum == new BigFloat(\"0.3\")}");

        // Use a constant and perform higher-precision math
        BigFloat radius = new("10.123456789");
        BigFloat pi = Constants.Fundamental.Pi;
        BigFloat area = pi * radius * radius;

        Console.WriteLine();
        Console.WriteLine($"Area with radius {radius} ≈ {area}");
        Console.WriteLine($"Nearest integer (guard-bit aware): {BigFloat.ToNearestInt(area)}");
    }
}
```

The examples in the documentation site’s **Getting Started** and **Examples** pages are kept in sync with the current APIs and are suitable for copy/paste into your projects. ([bigfloat.org][3])

---

## Generic math compatibility

`BigFloat` participates in .NET’s generic math ecosystem via `System.Numerics.INumberBase<BigFloat>` and the operator interfaces it pulls in. The type intentionally **does not** claim IEEE-754-specific contracts such as `IFloatingPointIeee754<T>` because BigFloat has no `NaN`/`∞` payloads or subnormal ranges. At a glance:

- Implements `INumberBase<BigFloat>`, providing `Zero`, `One`, parsing/formatting, conversions, and magnitude helpers.
- Supports the standard arithmetic and comparison operator interfaces transitively (`IAdditionOperators`, `IMultiplicationOperators`, etc.).

This enables reusable numeric code that works for both built-in floating types and BigFloat when the semantics align. Example (dot-product style accumulation):

```csharp
using System;
using System.Numerics;
using BigFloatLibrary;

static T Dot<T>(ReadOnlySpan<T> values, ReadOnlySpan<T> weights) where T : INumberBase<T>
{
    if (values.Length != weights.Length)
        throw new ArgumentException("Lengths must match", nameof(weights));

    T acc = T.Zero;
    for (int i = 0; i < values.Length; i++)
    {
        acc += values[i] * weights[i];
    }
    return acc;
}

var bf = Dot(new BigFloat[] { 1.5, 2, 3 }, new BigFloat[] { 2, 2, 2 });
var dbl = Dot(new double[] { 1.5, 2, 3 }, new double[] { 2, 2, 2 });
```

`bf` and `dbl` will both accumulate using the same generic code path; the BigFloat variant keeps high precision and the built-in doubles keep IEEE-754 behavior.

---

## Core concepts: representation & guard bits

### DataBits, Scale, and Size

Internally, each `BigFloat` has three important fields/properties: ([GitHub][4])

* **DataBits** (`_mantissa : BigInteger`)
  Holds the signed binary representation, including guard bits.

* **Scale** (`int`)
  A binary offset that effectively slides the radix point left or right. Positive scale moves the point left (larger values), negative scale moves it right (more fractional bits).

* **Size** (`int`)
  The number of *in-precision* bits, excluding guard bits (`SizeWithGuardBits` includes them). For zero, size is zero.

![BigFloatParts](https://raw.githubusercontent.com/SunsetQuest/BigFloat/master/docs/Images/BigFloatParts.png)

The public API exposes these either directly or via helper properties/methods (`Size`, `Scale`, `BinaryExponent`, etc.).

### GuardBits

BigFloat reserves:

```csharp
public const int GuardBits = 32;
```

as a fixed number of least-significant bits dedicated to guard precision. ([GitHub][4])

These bits:

* Are not counted as in-precision, but they retain “extra evidence” from intermediate calculations.
* Make long chains of additions, multiplications, and transcendental operations behave more predictably than a pure fixed-precision representation.

Conceptually:

```text
precise_bits | guard_bits
```

For example (conceptually):

```text
101.01100|110011001100...   ≈ 5.4
100.01001|100110011001...   ≈ 4.3
---------------------------------
1001.1011|001100110011...   ≈ 9.7
```

If we discarded the right side of the `|`, the final rounding decision would be based on much less information. With guard bits preserved, BigFloat can round closer to the “true” value in the final representation. ([GitHub][2])

---

## Precision, accuracy, and guard-bit notation

### “X” digits and `|` separator

The library uses several conventions to show what is in-precision versus out-of-precision: ([GitHub][2])

* Decimal outputs may show trailing `X` characters (e.g., `232XXXXXXX`) where digits are beyond the guaranteed precision.
* Binary diagnostics can include a `|` between in-precision bits and guard bits.
* Some parsing/formatting helpers accept or emit the `|` separator to round-trip accuracy hints.

For example:

```csharp
// "123.456|789" - '789' bits are treated as guard / accuracy hints
BigFloat value = BigFloat.Parse("123.456|789");
```

### Decimal vs. binary precision

Most decimal fractions cannot be represented exactly in binary:

* `5.4` → `101.0110011001100…` (repeating)
* `4.3` → `100.0100110011001…` (repeating)
* `0.25` → `0.01` (exact) ([GitHub][2])

BigFloat is explicit about this:

* Conversions are always done in binary.
* Guard bits exist specifically to preserve more of the repeating tail when it is helpful.
* Precision and accuracy APIs let you control how many *effective* bits you want to keep.

---

## Accuracy & precision APIs (2025+)

The 2025 releases focus on making precision control explicit and less error-prone. Preferred, accuracy-first APIs:

- `AdjustAccuracy` / `SetAccuracy`
- `AdjustPrecision` (delta-based) and `SetPrecisionWithRound` (target size with rounding)
- `ZeroWithAccuracy` / `OneWithAccuracy` for context-aware identity values

Older helpers remain for compatibility but are now marked as legacy (see [Legacy APIs & Migration Guide](#legacy-apis--migration-guide)). New APIs are described in the docs site’s **Accuracy Controls** section. ([bigfloat.org][3])

### Growing or shrinking accuracy

```csharp
BigFloat x = new("123.456");

// Add 64 working bits without changing the represented value
BigFloat wider = BigFloat.AdjustAccuracy(x, +64);

// Remove 32 bits and round before dropping them
BigFloat trimmed = BigFloat.AdjustPrecision(x, -32);

// Set a specific accuracy target (including guard bits)
BigFloat withBudget = BigFloat.SetAccuracy(x, 256);
Console.WriteLine(withBudget.Size); // around 256 - GuardBits
```

Use these helpers instead of manually manipulating scale or trying to emulate precision changes yourself.

### Rounding and truncation

```csharp
BigFloat v = new("9876.54321");

// Round to nearest integer value without burning all guard bits
int nearest = BigFloat.ToNearestInt(v);

// Truncate to an integer but keep accuracy metadata in the BigFloat
BigFloat integer = v.TruncateToIntegerKeepingAccuracy();

// Reduce size with rounding at the cut point
BigFloat compact = BigFloat.SetPrecisionWithRound(v, 80);
```

The older `ExtendPrecision` / `ReducePrecision` helpers are still present for backward compatibility, but the newer APIs are preferred because they encode intent (accuracy vs. bare precision) more clearly. ([bigfloat.org][3])

### Running calculations with `BigFloatContext` (opt-in ambient accuracy)

`BigFloatContext` provides a convenience wrapper for grouping calculations under a shared accuracy budget, rounding hint, and constants configuration without changing the deterministic core APIs.

```csharp
using var ctx = BigFloatContext.WithAccuracy(256);

// Compound interest without repeated AdjustAccuracy calls
BigFloat principal = 10_000;
BigFloat rate = BigFloat.Parse("0.0425");
BigFloat future = ctx.Run(() =>
{
    BigFloat monthly = rate / 12;
    BigFloat growth = BigFloat.Pow(BigFloat.One + monthly, 120);
    return principal * growth;
});
```

```csharp
// Machin-like π with a simple arctan series
using var ctx = BigFloatContext.WithAccuracy(512, constantsPrecisionBits: 512);

BigFloat Arctan(BigFloat x, int terms)
{
    BigFloat sum = 0;
    BigFloat power = x;
    for (int n = 0; n < terms; n++)
    {
        int k = (2 * n) + 1;
        BigFloat term = power / k;
        sum = (n % 2 == 0) ? sum + term : sum - term;
        power = BigFloatContext.ApplyCurrent(power * x * x);
    }
    return ctx.Apply(sum);
}

BigFloat pi = ctx.Run(() => 4 * (4 * Arctan(BigFloat.One / 5, 18) - Arctan(BigFloat.One / 239, 8)));
```

```csharp
// Mandelbrot escape test that keeps every iteration on-budget
using var ctx = BigFloatContext.WithAccuracy(192, roundingMode: BigFloatContextRounding.TowardZero);

bool IsInsideMandelbrot(BigFloat cx, BigFloat cy, int iterations)
{
    return ctx.Run(() =>
    {
        BigFloat x = 0;
        BigFloat y = 0;

        for (int i = 0; i < iterations; i++)
        {
            BigFloat x2 = BigFloatContext.ApplyCurrent(x * x);
            BigFloat y2 = BigFloatContext.ApplyCurrent(y * y);
            if (x2 + y2 > 4)
            {
                return false;
            }

            BigFloat xy = BigFloatContext.ApplyCurrent(x * y);
            x = ctx.Apply(x2 - y2 + cx);
            y = ctx.Apply(xy + xy + cy);
        }

        return true;
    });
}
```

---

## Legacy APIs & Migration Guide

Legacy members remain available for callers that have not yet updated, but they are now marked `[Obsolete]` and forward to the newer helpers so behavior is centralized.

| Legacy member | Recommended replacement | Notes |
| --- | --- | --- |
| `BigFloat.Zero`, `BigFloat.One` | `0` / `1` literals or `ZeroWithAccuracy(...)` / `OneWithAccuracy(...)` | Keeps explicit accuracy when needed; literals avoid obsolete warnings. |
| `SetPrecision` (static or instance) | `AdjustPrecision(deltaBits)` for raw resizing; `SetPrecisionWithRound` when reducing with rounding | Legacy behavior truncates when shrinking; new helpers make rounding strategy explicit. |
| `ExtendPrecision` | `AdjustPrecision(value, +bitsToAdd)` | Modern API uses the same delta-based helper. |
| `ReducePrecision` | `AdjustPrecision(value, -bitsToRemove)` | Modern API keeps rounding/accuracy rules consistent. |

Compatibility tests remain to ensure these members continue to behave, but new code should migrate to the accuracy-first surface area listed above.

---

## Math functions & constants

### Math functions

The core struct is extended via partial classes:

* `BigFloatMath.cs` – higher-order math (log, exp, powers, trig, roots, etc.) ([GitHub][4])
* `BigFloatRoundShiftTruncate.cs` – shifting, rounding, truncation, splitting
* `BigFloatParsing.cs` – string/Span parsing, binary/decimal utilities
* `BigFloatStringsAndSpans.cs` – string/Span formatting

These modules use algorithms such as:

* Newton-Plus variants for square root on large inputs
* Karatsuba multiplication and Burnikel–Ziegler-style division once operands cross certain thresholds ([GitHub][4])

### Constants

`Constants.cs` exposes a hierarchy of constants collections, e.g.:

```csharp
using BigFloatLibrary;

var constants = new BigFloat.Constants(
    requestedAccuracyInBits: 1000,
    onInsufficientBitsThenSetToZero: true,
    cutOnTrailingZero: true);

BigFloat pi = constants.Pi;
BigFloat e  = constants.E;
```

* `Constants.cs` provides constants with thousands of decimal digits.
* Text files in the `values` folder can be included to extend certain constants close to 1,000,000 digits. ([GitHub][2])

---

## Working with very large / very small numbers

### Basic arithmetic

```csharp
BigFloat a = new("123456789.012345678901234");
BigFloat b = new(1234.56789012345678); // from double

BigFloat sum       = a + b;
BigFloat diff      = a - b;
BigFloat product   = a * b;
BigFloat quotient  = a / b;

Console.WriteLine($"Sum: {sum}");
Console.WriteLine($"Difference: {diff}");
Console.WriteLine($"Product: {product}");
Console.WriteLine($"Quotient: {quotient}");
```

The examples in the current README show how results may end with `X` characters in decimal form when you have exceeded the guaranteed precision range for that value. ([GitHub][2])

### Comparisons and base-10 vs. base-2 intuition

Because BigFloat is base-2, decimal rounding intuition can be misleading. For example:

```csharp
BigFloat num1 = new("12345.6789");
BigFloat num2 = new("12345.67896");

bool equal   = num1 == num2;
bool bigger  = num1 > num2;
```

Even though `12345.6789` and `12345.67896` are different in base-10, their binary encodings may compare equal at the available precision. The README includes extended examples of this behavior and how guard bits mitigate surprises by preserving more of the repeating tail. ([GitHub][2])

### Extreme scales

BigFloat is designed to represent values with very large positive or negative exponents:

```csharp
BigFloat large      = new("1234e+7");
BigFloat veryLarge  = new("1e+300");
BigFloat verySmall  = new("1e-300");
```

* Very large values naturally move towards exponential notation (`1 * 10^300` style strings) for readability.
* Very small values may output as long rows of zeros followed by a small non-zero tail; the guard bits and accuracy metadata still track how meaningful that tail is. ([GitHub][2])

---

## Project layout

The repository is organized roughly as follows: ([GitHub][2])

* **`BigFloatLibrary/`**

  * Core `BigFloat` struct (`BigFloat.cs`)
  * Partial classes:

    * `BigFloatCompareTo.cs`
    * `BigFloatExtended.cs`
    * `BigFloatMath.cs`
    * `BigFloatParsing.cs`
    * `BigFloatRandom.cs`
    * `BigFloatRoundShiftTruncate.cs`
    * `BigFloatStringsAndSpans.cs`
  * `Constants.cs` and supporting constant builder/catalog files

* **`BigFloat.Tests/`**

  * Unit tests covering arithmetic, conversions, parsing, rounding, and edge cases.
  * Use `dotnet test` at the solution level to exercise the test suite.

* **`BigFloatPlayground/`**

  * Small exploratory console projects for:

    * Benchmarking (`Benchmarks.cs`)
    * Showcases / sample scenarios
    * Experimental testing of new APIs or algorithms ([GitHub][2])

* **`ChangeLog.md`**

  * High-level history of releases and notable changes (consult this file for detailed version history, including 4.2.0 notes).

* **`BigFloatSpecification.md`**

  * A deeper technical specification describing the format, invariants, and accuracy rules for BigFloat values.

For curated, user-oriented documentation (architecture notes, examples, and API overviews), refer to [https://bigfloat.org](https://bigfloat.org). ([bigfloat.org][5])

---

## Background & related work

BigFloat grew out of earlier work on high-precision numerics and the Newton-Plus square-root algorithm for large `BigInteger` values. ([GitHub][6])

* Original write-up: **“A BigFloat Library in C#”** on CodeProject.
* Companion project: **NewtonPlus – Fast BigInteger and BigFloat Square Root**, which explores performance-optimized sqrt for very large integers and shows how the algorithm is adapted for BigFloat. ([GitHub][6])

BigFloat is designed as an educational tool as much as a production-quality numeric type: the source is heavily commented, the specification is public, and the tests and playground make it easier to experiment with tricky numerics.

---

## License

This project is distributed under the [MIT License](http://www.opensource.org/licenses/mit-license.php). See the `MIT License` file in this repository for details. ([GitHub][2])

## Provenance & acknowledgements

* Documentation refresh for v4.2.0 prepared with assistance from OpenAI’s GPT‑5.2‑Codex‑Max.

[1]: https://www.nuget.org/packages/bigfloatlibrary?utm_source=chatgpt.com "BigFloatLibrary 4.2.0"
[2]: https://github.com/SunsetQuest/BigFloat "GitHub - SunsetQuest/BigFloat: A floating point library for large numbers."
[3]: https://bigfloat.org/getting-started.html "Getting Started - BigFloat Library"
[4]: https://raw.githubusercontent.com/SunsetQuest/BigFloat/master/BigFloatLibrary/BigFloat.cs "raw.githubusercontent.com"
[5]: https://bigfloat.org/?utm_source=chatgpt.com "BigFloat - High-Precision Arithmetic Library for C#"
[6]: https://github.com/SunsetQuest/NewtonPlus-Fast-BigInteger-and-BigFloat-Square-Root?utm_source=chatgpt.com "A fast, possibly the fastest, square root function for large ..."
