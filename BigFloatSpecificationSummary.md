# BigFloat Specification

The `BigFloat` struct is a sophisticated implementation of an arbitrary-precision floating-point number in C#, designed to provide high-precision arithmetic with virtually unlimited precision, limited only by available memory. It leverages the `System.Numerics.BigInteger` type for its mantissa, ensuring robust handling of large numbers, and incorporates a base-2 exponent system for flexible scaling. This specification provides a comprehensive overview of the `BigFloat` type, detailing its structure, properties, methods, operators, type conversions, and additional features such as mathematical constants and utility functions. Below, we expand on the core concepts, combining the best elements from the provided summaries to create a detailed and authoritative reference.

---

## Core Concept

`BigFloat` is a **readonly partial struct** that represents a floating-point number with arbitrary precision. Unlike traditional floating-point types like `float` or `double`, which are constrained by fixed bit widths (e.g., 32 or 64 bits), `BigFloat` uses a `BigInteger` for its mantissa (`DataBits`) and an integer (`Scale`) for its base-2 exponent. This design allows it to handle numbers of any size and precision, making it ideal for applications requiring exact arithmetic, such as scientific computing, cryptography, or financial calculations.

A key feature of `BigFloat` is its use of **extra guard bits** (defined as a constant `GuardBits`, typically 32), which are included in the `DataBits` to enhance precision and control rounding during operations. These guard bits act as a buffer, ensuring that arithmetic results remain accurate even when precision is adjusted or numbers are converted.

---

## Core Structure

The `BigFloat` struct is defined as follows in C#:

```csharp
public readonly partial struct BigFloat
{
    public const int GuardBits = 32;
    public readonly BigInteger DataBits { get; }
    internal readonly int _size;
    public readonly int Scale { get; init; }
}
```

### Key Internal State

- **`DataBits` (readonly BigInteger)**:
  - The mantissa of the number, storing the significant digits in a `BigInteger`.
  - Includes `GuardBits` to provide additional precision beyond the visible significant bits.
  - Can be positive, negative, or zero, with the sign preserved in the `BigInteger`.

- **`Scale` (readonly int)**:
  - The base-2 exponent offset, determining the position of the radix point (binary point).
  - A positive `Scale` shifts the radix point to the right, effectively multiplying the value by \(2^{\text{Scale}}\).
  - A negative `Scale` shifts the radix point to the left, dividing by \(2^{|\text{Scale}|}\).
  - When `Scale` is zero, the radix point is positioned immediately after the main bits but before the guard bits.

- **`_size` (internal readonly int)**:
  - Represents the bit length of the absolute value of `DataBits`, including the guard bits.
  - Equals zero only if `DataBits` is exactly zero.
  - Used internally to track the total size of the number in bits.

---

## Core Properties

The `BigFloat` struct exposes a rich set of properties to describe its state and behavior:

- **`Size` (int)**:
  - The precision of the number in bits, excluding the `GuardBits`.
  - Calculated as `Math.Max(0, _size - GuardBits)`.
  - Represents the number of significant bits available for computation after accounting for the guard bits.

- **`BinaryExponent` (int)**:
  - The overall base-2 exponent of the number.
  - Computed as `Scale + _size - GuardBits - 1`.
  - Indicates the effective exponent when considering both the scale and the bit length.

- **`IsZero` (bool)**:
  - Returns `true` if the value is effectively zero, taking into account precision and scale.
  - Considers rounding effects from guard bits.

- **`IsStrictZero` (bool)**:
  - Returns `true` only if `DataBits` is exactly zero (no rounding considered).

- **`IsOutOfPrecision` (bool)**:
  - Returns `true` if the number has less than one bit of precision (`_size < GuardBits`).
  - Indicates that the number’s significant bits are entirely within the guard bits range.

- **`IsPositive` / `IsNegative` (bool)**:
  - Determines the sign of the number based on the `Sign` property after rounding.

- **`Sign` (int)**:
  - Returns `-1` (negative), `0` (zero), or `1` (positive).
  - For numbers with insufficient precision, rounds based on the most significant guard bit.

- **`UnscaledValue` (BigInteger)**:
  - The integer value of the number after removing and rounding the `GuardBits`.
  - Represents the mantissa as it would appear without the scale applied.

- **`SizeWithGuardBits` (int)**:
  - An alias for `_size`, representing the total bit length of `DataBits`, including guard bits.

- **`Precision` (int)**:
  - The precision in bits, excluding guard bits (`_size - GuardBits`).
  - Can be negative if `_size` is less than `GuardBits`.

- **`Accuracy` (int)**:
  - The negative of the scale (`-Scale`).
  - Represents the number of bits to the right of the radix point.

- **`IsInteger` (bool)**:
  - Returns `true` if the fractional part is effectively zero or rounds to an integer.
  - Considers up to half of the guard bits for rounding purposes.

### Bit Extraction Properties

- **`Lowest64BitsWithGuardBits` (ulong)**:
  - The least significant 64 bits of `DataBits`, including hidden guard bits.

- **`Lowest64Bits` (ulong)**:
  - The least significant 64 bits after rounding and removing guard bits.

- **`Highest64Bits` (ulong)**:
  - The most significant 64 bits of the magnitude of `DataBits`.

- **`Highest128Bits` (UInt128)**:
  - The most significant 128 bits of the magnitude of `DataBits`.

---

## Static Properties

`BigFloat` provides predefined instances for common values:

- **`Zero` (BigFloat)**:
  - Represents the value 0 with zero size, precision, scale, and accuracy.

- **`One` (BigFloat)**:
  - Represents the value 1 with minimal precision plus `GuardBits`.

- **`NegativeOne` (BigFloat)**:
  - Represents the value -1 with minimal precision plus `GuardBits`.

---

## Constructors

`BigFloat` offers a variety of constructors to instantiate numbers from different numeric types:

- **`BigFloat(BigInteger value, int binaryScaler = 0, bool valueIncludesGuardBits = false)`**:
  - Creates a `BigFloat` from a `BigInteger`.
  - If `valueIncludesGuardBits` is `false` (default), shifts the input left by `GuardBits` to include guard bits.
  - `binaryScaler` adjusts the `Scale`.

- **Integer Constructors**:
  - **`BigFloat(long value, int binaryScaler = 0)`**: From a signed 64-bit integer.
  - **`BigFloat(ulong value, int binaryScaler = 0)`**: From an unsigned 64-bit integer.
  - **`BigFloat(int value, int binaryScaler = 0)`**: From a signed 32-bit integer.
  - **`BigFloat(uint value, int binaryScaler = 0)`**: From an unsigned 32-bit integer.
  - **`BigFloat(byte value, int binaryScaler = 0)`**: From an unsigned 8-bit integer.
  - **`BigFloat(char value, int binaryScaler = 0)`**: From a character (treated as an integer).
  - **`BigFloat(Int128 value, int binaryScaler = 0)`**: From a signed 128-bit integer.

- **Floating-Point Constructors**:
  - **`BigFloat(double value, int binaryScaler = 0)`**: From a double-precision float.
  - **`BigFloat(float value, int binaryScaler = 0)`**: From a single-precision float.
  - Handles normal and subnormal values; throws `OverflowException` for NaN or Infinity.

- **Internal Constructor**:
  - **`BigFloat(BigInteger rawValue, int binaryScaler, int rawValueSize)`**:
    - Constructs a `BigFloat` from raw parts, assuming `rawValue` includes guard bits.
    - Intended for advanced or internal use.

---

## Static Factory Methods

Additional methods provide specialized ways to create `BigFloat` instances:

- **`ZeroWithSpecifiedLeastPrecision(int pointOfLeastPrecision)`**:
  - Creates a zero value with a specified minimum scale.

- **`OneWithAccuracy(int accuracy)`**:
  - Creates the value 1 with the specified accuracy (`-Scale`).

- **`IntWithAccuracy(BigInteger intVal, int precisionInBits)`**:
  - Creates an integer value with additional precision bits beyond `GuardBits`.

- **`IntWithAccuracy(int intVal, int precisionInBits)`**:
  - Similar to above, but for 32-bit integers.

---

## Operators

### Arithmetic Operators

- **Unary**:
  - **`+`**: Returns the value unchanged.
  - **`-`**: Negates the value by flipping the sign of `DataBits`.

- **Increment/Decrement**:
  - **`++`**: Adds 1 to the value.
  - **`--`**: Subtracts 1 from the value.

- **Binary**:
  - **`+`**: Adds two `BigFloat` values.
  - **`-`**: Subtracts one `BigFloat` from another.
  - **`*`**: Multiplies two `BigFloat` values.
  - **`/`**: Divides one `BigFloat` by another.
  - **`%`**: Computes the remainder of division.

  Results are rounded based on the precision of the operands and the `GuardBits`.

### Comparison Operators

- **`==`**: Checks equality, considering precision and guard bits.
- **`!=`**: Checks inequality.
- **`<`**: Less than.
- **`>`**: Greater than.
- **`<=`**: Less than or equal to.
- **`>=`**: Greater than or equal to.

Supported between `BigFloat` and `BigFloat`, `BigInteger`, `long`, or `ulong`. Equality with `BigInteger` also checks if the `BigFloat` is an integer.

### Bitwise Operators

- **`~`**: Bitwise NOT on `DataBits`.
- **`<<`**: Left shifts `DataBits`, increasing precision; adjusts `_size`, not `Scale`.
- **`>>`**: Right shifts `DataBits`, reducing precision; adjusts `_size`, not `Scale`.

---

## Type Conversions

### Implicit Conversions

From integer types to `BigFloat`:
- `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `Int128`, `UInt128`.

### Explicit Conversions

- **To `BigFloat`**:
  - From `float`, `double`, `BigInteger`.
- **From `BigFloat`**:
  - To `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `Int128`, `UInt128`, `float`, `double`, `BigInteger`.
  - Integer conversions apply the scale and round/truncate guard/fractional bits.
  - Floating-point conversions may lose precision or overflow.

---

## Comparison and Equality Methods

- **`CompareTo(BigFloat other)`**:
  - Returns `-1`, `0`, or `1` based on comparison.
  - Aligns scales and considers guard bits for rounding.

- **`CompareTo(object obj)`**:
  - Compares with another `BigFloat` or `BigInteger`.

- **`Equals(BigFloat other)`**:
  - True if `CompareTo` returns 0.

- **`Equals(BigInteger/long/ulong other)`**:
  - Checks equality with integer types, rounding `BigFloat` if necessary.

- **`CompareInPrecisionBitsTo(BigFloat other)`**:
  - Stricter comparison; differences within precision tolerance are equal.

- **`CompareToExact(BigFloat other)`**:
  - Exact bit comparison after scale alignment, ignoring rounding.

- **`CompareToIgnoringLeastSigBits(BigFloat a, BigFloat b, int leastSignificantBitsToIgnore)`**:
  - Compares while ignoring specified least significant bits.

- **`GetHashCode()`**:
  - Based on `UnscaledValue` and `Scale`.

---

## Rounding and Precision Methods

- **`Floor()` / `Ceiling()`**:
  - Rounds to the nearest integer towards negative/positive infinity.

- **`SetPrecision(BigFloat x, int newSize)`**:
  - Adjusts precision to `newSize`, padding or truncating without rounding.

- **`SetPrecisionWithRound(BigFloat x, int requestedNewSizeInBits)`**:
  - Reduces precision with rounding.

- **`ReducePrecision(BigFloat x, int reduceBy)`**:
  - Reduces precision by shifting right, adjusting `Scale`.

- **`ExtendPrecision(BigFloat x, int bitsToAdd)`**:
  - Increases precision by adding trailing zeros, adjusting `Scale`.

- **`TruncateByAndRound(BigFloat x, int targetBitsToRemove)`**:
  - Reduces precision with rounding based on removed bits.

- **`AdjustScale(BigFloat x, int changeScaleAmount)`**:
  - Adjusts the `Scale` by the specified amount.

- **`WouldRoundUp()` / `WouldRoundUp(int bottomBitsRemoved)`**:
  - Checks if removing bits would round away from zero.

---

## Parsing and Formatting

- **`Parse(string numericString, int binaryScaler = 0)`**:
  - Parses decimal, hexadecimal (`0x`), or binary (`0b`) strings.
  - Supports signs, radix points, exponents (e.g., `e` for decimal), and separators.

- **`TryParse(string numericString, out BigFloat result, int binaryScaler = 0)`**:
  - Non-throwing parse attempt.

- **`ToString()`**:
  - Decimal string representation, rounding guard bits.

- **`ToString(bool includeOutOfPrecisionBits)`**:
  - Includes guard bit digits if `true`.

- **`ToString(string format)`**:
  - Supports `"X"` (hex), `"B"` (binary).

- **`ToString(string format, IFormatProvider provider)`**:
  - Implements `IFormattable` with formats like `"G"`, `"R"`, `"X"`, `"B"`.

- **`TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider provider)`**:
  - Implements `ISpanFormattable` for efficient formatting.

---

## Utility Methods

- **`FitsInADouble()`**:
  - Checks if the value fits within a `double` without overflow/underflow.

- **`GetMostSignificantBits(int numberOfBits)`**:
  - Returns a string of the specified most significant bits.

- **`GetAllBitsAsString(bool twosComplement)`**:
  - Returns all bits as a string, optionally in two’s complement.

- **`DebugPrint(string varName)`**:
  - Outputs detailed internal state for debugging.

---

## Conclusion

The `BigFloat` struct is a powerful tool for arbitrary-precision arithmetic in C#, offering a robust and flexible framework for handling high-precision floating-point numbers. Its use of `BigInteger` for the mantissa, combined with a scalable base-2 exponent and guard bits for precision control, makes it suitable for a wide range of applications. This specification has outlined its core structure, properties, operators, and methods, providing a comprehensive reference for developers seeking to leverage its capabilities.