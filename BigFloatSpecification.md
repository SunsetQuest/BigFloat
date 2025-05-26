# BigFloat Library Specification

## Overview

The BigFloat library is a comprehensive C# implementation of arbitrary-precision floating-point arithmetic, designed to handle numerical computations requiring precision beyond standard IEEE floating-point types. The library uses `System.Numerics.BigInteger` as its underlying mantissa representation and implements a base-2 scaling system for flexible precision control.

## Architecture and Design Philosophy

### Core Design Principles

1. **Arbitrary Precision**: Unlimited precision constrained only by available memory
2. **Guard Bits Strategy**: 32 least-significant guard bits for enhanced accuracy during chained operations
3. **Base-2 Internal Representation**: All operations performed in binary for efficiency
4. **Immutable Struct Design**: Thread-safe, value-type semantics
5. **IEEE-Compatible Interface**: Familiar operators and methods for ease of adoption

### Key Innovations

- **Newton-Plus Algorithm**: Optimized square root computation significantly faster than traditional methods
- **Intelligent Precision Management**: Automatic precision adjustment based on operation requirements
- **Comprehensive Constants Library**: Pre-computed mathematical constants with up to 1M decimal digits
- **Multi-Format Parsing**: Support for decimal, hexadecimal, and binary input formats

## File Organization and Components

### Core Implementation Files

#### `BigFloat.cs` (Primary Structure)
- **Purpose**: Main struct definition and core functionality
- **Key Components**:
  - Struct definition with `Mantissa` (BigInteger), `Scale` (int), and `_size` (int)
  - Constructors for various numeric types (int, long, double, BigInteger, etc.)
  - Fundamental arithmetic operators (+, -, *, /, %)
  - Comparison operators and equality semantics
  - Type conversion operators (explicit/implicit)
  - Core properties: `Size`, `BinaryExponent`, `IsZero`, `Sign`, etc.

#### `BigFloatMath.cs` (Mathematical Functions)
- **Purpose**: Advanced mathematical operations and functions
- **Key Functions**:
  - `Inverse()`: High-precision reciprocal calculation
  - `Pow()`: Integer exponentiation with optimization for small exponents
  - `Sqrt()`: Newton-Plus square root algorithm
  - `NthRoot()`: General nth root computation
  - `CubeRoot()`: Optimized cube root
  - Trigonometric functions: `Sin()`, `Cos()`, `Tan()`
  - `Log2()`: Base-2 logarithm computation

#### `BigFloatStringsAndSpans.cs` (String Representation)
- **Purpose**: String formatting and display functionality
- **Key Features**:
  - `ToString()` overloads with various formatting options
  - `IFormattable` and `ISpanFormattable` implementations
  - Support for decimal, hexadecimal ('X'), and binary ('B') formats
  - Precision-aware formatting (showing vs. hiding guard bits)
  - Scientific notation for very large/small numbers
  - Debug visualization with `DebugPrint()` and `DebuggerDisplay`

#### `BigFloatParsing.cs` (Input Processing)
- **Purpose**: Parse strings into BigFloat representations
- **Supported Formats**:
  - Decimal: `"123.456"`, `"1.23e+10"`
  - Hexadecimal: `"0xABC.DEF"`
  - Binary: `"0b1101.1011"`
  - Precision separator: `"123.456|789"` (precision vs. guard bits)
- **Key Functions**:
  - `Parse()` and `TryParse()` methods
  - `ParseBinary()`, `TryParseBinary()`
  - `TryParseDecimal()`, `TryParseHex()`

#### `BigFloatCompareTo.cs` (Comparison Operations)
- **Purpose**: Comprehensive comparison and equality operations
- **Key Methods**:
  - `CompareTo()`: Standard IComparable implementation
  - `CompareInPrecisionBitsTo()`: Precision-aware comparison
  - `StrictCompareTo()`: Exact bit-level comparison
  - `FullPrecisionCompareTo()`: Comparison accounting for all precision bits
  - `CompareToIgnoringLeastSigBits()`: Comparison with tolerance

#### `BigFloatExtended.cs` (Additional Functionality)
- **Purpose**: Extended properties, conversions, and utility functions
- **Key Features**:
  - Additional constructors (UInt128, Int128, etc.)
  - Extended properties: `Precision`, `Accuracy`, `SizeWithGuardBits`
  - Utility functions: `ZeroWithSpecifiedLeastPrecision()`, `IntWithAccuracy()`
  - Additional implicit/explicit conversion operators

#### `BigFloatRandom.cs` (Random Number Generation)
- **Purpose**: Generate random BigFloat values with specified characteristics
- **Key Functions**:
  - `RandomInRange()`: Uniform or logarithmic distribution in range
  - `RandomWithMantissaBits()`: Random value with specific mantissa size
  - Support for both linear and logarithmic distributions

### Supporting Infrastructure Files

#### `BigIntegerTools.cs` (BigInteger Utilities)
- **Purpose**: Extended BigInteger operations and utilities
- **Key Components**:
  - `NewtonPlusSqrt()`: World's fastest BigInteger square root
  - `NewtonNthRoot()`: General nth root for BigInteger
  - `Inverse()`: High-precision BigInteger reciprocal
  - `RightShiftWithRound()`: Precision-preserving bit shifting
  - `ToBinaryString()`: Binary string representation with formatting options
  - `TryParseBinary()`: Parse binary strings to BigInteger
  - `RandomBigInteger()`: Generate random BigInteger values
  - Precision management: `TruncateToAndRound()`, `SetPrecisionWithRound()`

### Mathematical Constants System

#### `Constants.cs` (Constants Access Interface)
- **Purpose**: Primary interface for accessing mathematical constants
- **Architecture**:
  - Hierarchical organization: `Fundamental`, `NumberTheory`, `Analysis`, `Physics`, `Derived`, `Trigonometric`, `Misc`
  - Configurable precision: `WithConfig()` method
  - Caching system for performance optimization
  - Parallel loading support for large constant sets
- **Key Constants Categories**:
  - Fundamental: π, e, √2, φ (Golden Ratio), γ (Euler-Mascheroni)
  - Number Theory: Twin Prime constant, Apéry's constant, Conway's constant
  - Analysis: Catalan constant, Khintchine's constant, Omega constant
  - Derived: π², e², π^e, e^π, π^π, etc.

#### `ConstantInfo.cs` (Constant Metadata)
- **Purpose**: Metadata and access methods for individual constants
- **Key Features**:
  - Constant metadata: name, formula, source URLs, precision information
  - `TryGetAsBigFloat()`: Convert to BigFloat with specified precision
  - External file loading for ultra-high precision (1M+ digits)
  - Base64 encoding for efficient storage of pre-computed digits
  - Precision cutoff on trailing zeros for optimal accuracy

#### `ConstantsCatalog.cs` (Constants Registry)
- **Purpose**: Central registry mapping constant IDs to ConstantInfo
- **Key Features**:
  - String-based constant identification system
  - Case-insensitive constant lookup
  - Comprehensive catalog of 50+ mathematical constants
  - Integration with ConstantBuilder for initialization

#### `ConstantVisualization.cs` (Display Utilities)
- **Purpose**: Visualization and formatting tools for constants
- **Key Functions**:
  - `FormatConstant()`: Formatted display with digit grouping
  - `CreateComparisonTable()`: Tabular comparison of multiple constants
  - `GetContinuedFraction()`: Continued fraction representation
  - `GetConstantInfo()`: Comprehensive constant information display

#### `ConstantBuilder.cs` (Constants Data) [Reduced Size]
- **Purpose**: Pre-computed constant data and array generation
- **Key Features**:
  - ConstantInfo definitions for all supported constants
  - `GenerateArrayOfCommonConstants()`: Generate sorted arrays of constants
  - Base64-encoded high-precision constant data
  - External file references for ultra-high precision constants

## Core Structure and Properties

### BigFloat Struct Definition

```csharp
public readonly partial struct BigFloat
{
    public const int GuardBits = 32;
    public readonly BigInteger Mantissa { get; }     // Includes guard bits
    internal readonly int _size;                     // Total bit length including guard bits
    public readonly int Scale { get; init; }         // Base-2 exponent offset
}
```

### Key Properties

- **`Size`**: Precision in bits (excluding guard bits) = `_size - GuardBits`
- **`BinaryExponent`**: Overall base-2 exponent = `Scale + _size - GuardBits - 1`
- **`IsZero`**: True if effectively zero (considering precision and scale)
- **`IsInteger`**: True if fractional part rounds to zero
- **`Sign`**: -1, 0, or 1 based on rounded value
- **`Precision`**: Same as Size (alternative name)
- **`Accuracy`**: Number of bits right of radix point = `-Scale`

### Value Representation

A BigFloat value is computed as: `(Mantissa >> GuardBits) * 2^Scale`

The guard bits provide sub-precision information that improves accuracy in chained operations but are not considered part of the "precise" representation.

## Mathematical Operations

### Arithmetic Operations

- **Addition/Subtraction**: Scale alignment with precision-aware rounding
- **Multiplication**: Size-based optimization with guard bit management
- **Division**: Precision-preserving algorithm with remainder handling
- **Modulus**: Supports both mathematical and programming semantics

### Advanced Mathematical Functions

- **Power Functions**: Optimized for integer exponents, uses binary exponentiation
- **Root Functions**: Newton-Plus algorithm for square roots, general nth roots
- **Trigonometric**: High-precision sin/cos/tan with Payne-Hanek reduction
- **Logarithmic**: Base-2 logarithm with hardware acceleration for smaller values

### Precision Management

- **Automatic Precision**: Operations automatically determine appropriate output precision
- **Guard Bits**: 32 extra bits maintain accuracy through operation chains
- **Rounding**: Proper rounding to nearest, with tie-breaking rules
- **Precision Control**: Manual precision adjustment with `SetPrecision()`, `TruncateByAndRound()`

## Parsing and Formatting

### Input Formats Supported

1. **Decimal**: `"123.456"`, `"1.23e+10"`, `"-456.789e-20"`
2. **Hexadecimal**: `"0xABC.DEF"`, `"-0x123.456"`
3. **Binary**: `"0b1101.1011"`, `"0b-110.101"`
4. **Precision Separator**: `"123.456|789"` (precise|guard digits)

### Output Formats

1. **Decimal**: Default format with precision indicators (`XXXXX` for out-of-precision)
2. **Scientific**: For very large/small numbers (`1.23e+100`)
3. **Hexadecimal**: `ToString("X")` - hexadecimal with radix point
4. **Binary**: `ToString("B")` - binary with radix point
5. **Debug**: Detailed internal state visualization

### Special Formatting Features

- **Precision Masking**: Out-of-precision digits shown as 'X' or scientific notation
- **Digit Grouping**: Optional grouping for readability
- **Guard Bit Display**: Optional inclusion of guard bits in output
- **IFormattable/ISpanFormattable**: Standard .NET formatting interfaces

## Constants System Architecture

### Hierarchical Organization

The constants system is organized into logical categories:

```
Constants
├── Fundamental (π, e, √2, φ, γ)
├── NumberTheory (Twin Prime, Apéry, Conway)
├── Analysis (Catalan, Khintchine, Omega)
├── Physics (Fine Structure)
├── Derived (π², e², π^e, etc.)
├── Trigonometric (sin/cos values)
└── Misc (Plastic number, etc.)
```

### Precision Management

- **Base64 Encoding**: Efficient storage of pre-computed digits
- **External Files**: Support for ultra-high precision (1M+ digits)
- **Caching System**: Performance optimization for repeated access
- **Precision Cutoff**: Intelligent truncation at trailing zeros

### Usage Patterns

```csharp
// Direct access
BigFloat pi = Constants.Fundamental.Pi;

// Configured precision
BigFloat pi = Constants.WithConfig(precisionInBits: 5000).Get("Pi");

// Category access
var fundamentals = Constants.WithConfig(2000).GetCategory(["Pi", "E", "Sqrt2"]);

// Computational generation
BigFloat pi = Constants.GeneratePi(accuracyInBits: 10000);
```

## Performance Considerations

### Algorithmic Optimizations

1. **Newton-Plus Square Root**: 2-10x faster than traditional methods
2. **Binary Exponentiation**: Efficient integer power computation
3. **Scale Alignment**: Minimal BigInteger operations for arithmetic
4. **Size-Based Optimization**: Different algorithms based on operand sizes

### Memory Management

- **Immutable Structs**: No heap allocation for the struct itself
- **BigInteger Efficiency**: Leverages .NET's optimized BigInteger implementation
- **Caching**: Constants cached to avoid recomputation
- **Stackalloc**: Used for temporary operations where possible

### Precision Trade-offs

- **Guard Bits**: Balance between accuracy and performance (32 bits chosen as optimal)
- **Operation Chaining**: Gradual precision loss over ~10²¹ operations
- **Early Termination**: Algorithms stop when precision requirements are met

## Special Considerations and Limitations

### Precision Loss Scenarios

1. **Decimal Conversion**: Most decimal numbers have infinite binary representations
2. **Operation Chaining**: Gradual precision loss over many operations
3. **Mixed Sizes**: Operations between very different sized numbers
4. **Rounding Accumulation**: Non-perfect rounding in some mathematical functions

### Known Issues and Workarounds

1. **Base-10 vs Base-2**: `5.4` decimal ≠ exact binary representation
2. **Rounding Inconsistencies**: Some functions don't implement perfect rounding
3. **Performance**: Very high precision operations can be slow
4. **Memory Usage**: Large precision numbers consume significant memory

### Browser/Storage Limitations

- **No localStorage**: Browser storage APIs not supported in Claude.ai artifacts
- **Memory-Only**: All state must be maintained in JavaScript variables/React state
- **Session Persistence**: No cross-session data persistence in web environments

## Usage Examples and Common Patterns

### Basic Arithmetic

```csharp
BigFloat a = new("123456789.012345678901234");
BigFloat b = new(1234.56789012345678);
BigFloat result = a + b * Constants.Fundamental.Pi;
```

### High-Precision Computation

```csharp
BigFloat pi = Constants.WithConfig(50000).Get("Pi");
BigFloat area = pi * radius * radius;
```

### Precision Control

```csharp
BigFloat value = BigFloat.SetPrecisionWithRound(largeValue, 1000);
BigFloat extended = BigFloat.ExtendPrecision(smallValue, 500);
```

### Random Number Generation

```csharp
BigFloat random = BigFloat.RandomInRange(min: 0, max: 1, logarithmic: false);
BigFloat logRandom = BigFloat.RandomWithMantissaBits(1000, -100, 100, logarithmic: true);
```

## Implementation Quality and Testing

### Code Quality Features

- **Comprehensive Documentation**: XML documentation for all public APIs
- **Debug Assertions**: Validation of internal state consistency
- **Error Handling**: Appropriate exceptions for invalid operations
- **Thread Safety**: Immutable design ensures thread safety

### Validation and Testing

- **Mathematical Correctness**: Algorithms validated against known mathematical results
- **Precision Verification**: Guard bit effectiveness verified through operation chains
- **Performance Benchmarking**: Optimizations validated through comprehensive testing
- **Edge Case Handling**: Special values (zero, infinity, very large/small) properly handled

This specification provides a comprehensive understanding of the BigFloat library's architecture, capabilities, and usage patterns, suitable for both human developers and AI systems working with the codebase.
This document was generated by Anthropic's Claude 4 on 5/25/2025.