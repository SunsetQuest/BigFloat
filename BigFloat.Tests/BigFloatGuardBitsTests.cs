// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

using System.Numerics;

namespace BigFloatLibrary.Tests;

public class BigFloatGuardBitsTests
{
    [Theory]
    [InlineData(4)]    // Scale=4, include guard bits
    [InlineData(16)]   // Scale=16, include guard bits  
    [InlineData(31)]   // Scale=31, include guard bits (boundary)
    [InlineData(2)]    // Scale=2, include guard bits
    public void ToBinaryString_WithVariousScales_ShouldHandleGuardBitsCorrectly(int scale)
    {
        // Arrange - Create a BigFloat with a known bit pattern that has guard bits set
        BigInteger mantissa = BigInteger.Parse("340282366920938463463374607431768211455"); // Large number with mixed bit pattern
        var bigFloat = new BigFloat(mantissa, scale - BigFloat.GuardBits, true);

        // Act
        string withGuard = bigFloat.ToBinaryString(true);
        string withoutGuard = bigFloat.ToBinaryString(false);

        // Assert
        Assert.True(withoutGuard.Length + 32 == withGuard.Length);

        if (scale > 0 && scale < 32)
        {
            // Should potentially show guard bits in trailing positions
            // At minimum, should be same length as traditional result
            Assert.True(withGuard.Length >= withoutGuard.Length);

            // The trailing section should contain bits from the original mantissa
            string trailingSection = withGuard[^scale..];
            // With our large test number, we expect some guard bits to be non-zero
            if (scale <= BigFloat.GuardBits)
            {
                // Should see some actual bits, not all zeros
                string shouldBe = new('0', scale);
                Assert.NotEqual(shouldBe, trailingSection);
            }
        }
        else if (scale >= 32)
        {
            // Should use traditional zero-fill behavior for large scales
            string trailingZeros = withGuard[^scale..];
            Assert.Equal(new string('0', scale), trailingZeros);
        }

        // Should be identical to traditional result
        Assert.Equal(withGuard[..^BigFloat.GuardBits], withoutGuard);
    }

    [Theory]
    [InlineData(35)]  // Scale > 32, should use traditional zero-fill
    [InlineData(64)]  // Much larger scale
    [InlineData(100)] // Very large scale
    public void ToBinaryString_WithLargeScale_ShouldUseTraditionalBehavior(int scale)
    {
        // Arrange
        BigInteger mantissa = BigInteger.Parse("123456789012345678901234567890");
        var bigFloat = new BigFloat(mantissa, scale, true);

        // Act
        string resultWithGuardBits = bigFloat.ToBinaryString(includeGuardBits: true);
        string resultWithoutGuardBits = bigFloat.ToBinaryString(includeGuardBits: false);

        // Assert - Should be identical since Scale >= 32
        Assert.Equal(resultWithoutGuardBits, resultWithGuardBits);
    }

    [Theory]
    [InlineData(true, 4)]   // includeGuardBits=true, Scale=4
    [InlineData(true, 16)]  // includeGuardBits=true, Scale=16
    [InlineData(true, 31)]  // includeGuardBits=true, Scale=31 (boundary)
    [InlineData(false, 4)]  // includeGuardBits=false, Scale=4
    [InlineData(false, 16)] // includeGuardBits=false, Scale=16
    public void ToBinaryString_WithIncludeGuardBits_ShouldWorkCorrectly(bool includeGuardBits, int scale)
    {
        // Arrange - Create BigFloat with known pattern
        BigInteger mantissa = BigInteger.Parse("1208925819614629174706175"); // Binary: 1111111111111111111111111111111111111111111111111111111111111111111111111111111
        var bigFloat = new BigFloat(mantissa, scale - BigFloat.GuardBits, true);

        // Act
        string result = bigFloat.ToBinaryString(includeGuardBits);
        string traditionalResult = bigFloat.ToBinaryString(false);

        // Assert
        Assert.True(result.Length > 0);

        if (includeGuardBits && scale < 32 && scale > 0)
        {
            // With guard bits, result should be longer than traditional
            Assert.True(result.Length >= traditionalResult.Length);

            // Should contain more '1' bits in trailing positions
            int expectedGuardBits = Math.Min(BigFloat.GuardBits, scale);
            string guardBitSection = result.Substring(result.Length - scale, expectedGuardBits);
            Assert.Contains("1", guardBitSection); // Our test pattern should show '1' bits
        }
        else
        {
            // Should behave like traditional binary string
            if (!includeGuardBits || scale >= 32)
            {
                Assert.Equal(traditionalResult, result);
            }
        }
    }

    [Theory]
    [InlineData("11111111111111111111111111111111", 8, 4)]  // All 1s mantissa
    [InlineData("10101010101010101010101010101010", 6, 3)]  // Alternating pattern
    [InlineData("11110000111100001111000011110000", 4, 2)]  // Block pattern
    [InlineData("00000000000000000000000000000001", 2, 1)]  // Single bit set
    public void ToBinaryString_WithSpecificPatterns_ShouldPreserveGuardBits(
        string mantissaBinary, int scale, int nTrailingGuardBits)
    {
        // Arrange
        Assert.True(BigIntegerTools.TryParseBinary(mantissaBinary, out BigInteger mantissa));
        var bigFloat = new BigFloat(mantissa, scale, true);

        // Extract the last few guard bits from original mantissa
        BigInteger guardBitMask = (BigInteger.One << nTrailingGuardBits) - 1;
        BigInteger expectednTrailingGuardBitValue = mantissa & guardBitMask;

        // Act
        string result = bigFloat.ToBinaryString(includeGuardBits: true);

        // Assert
        Assert.True(result.Length > 0);

        // Extract the trailing GuardBits from result
        string actualTrailingGuardBits = result[^nTrailingGuardBits..];

        // Convert expected guard bits to string (MSB first)
        string expectedGuardBitsString = "";
        for (int i = nTrailingGuardBits - 1; i >= 0; i--)
        {
            bool bitSet = !((expectednTrailingGuardBitValue >> i) & 1).IsZero;
            expectedGuardBitsString += bitSet ? '1' : '0';
        }

        Assert.Equal(expectedGuardBitsString, actualTrailingGuardBits);
    }

    [Theory]
    [InlineData(32)]
    [InlineData(33)]
    public void ToBinaryString_WithNegativeScale_ShouldNotExposeGuardBits(int scale)
    {
        // Arrange
        BigInteger mantissa = BigInteger.Parse("1208925819614629174706175");
        var bigFloat = new BigFloat(mantissa, scale, true);

        // Act
        string resultWithGuardBits = bigFloat.ToBinaryString(includeGuardBits: true);
        string resultWithoutGuardBits = bigFloat.ToBinaryString(includeGuardBits: false);

        // Assert - Should be identical since scales larger than GuardBit count show GuardBits anyway.
        Assert.Equal(resultWithoutGuardBits, resultWithGuardBits);
    }

    [Fact]
    public void ToBinaryString_WithZeroValue_ShouldHandleGuardBitsCorrectly()
    {
        // Arrange
        BigFloat bigFloat = BigFloat.ZeroWithAccuracy(0);

        // Act
        string resultWithGuardBits = bigFloat.ToBinaryString(includeGuardBits: true);
        string resultWithoutGuardBits = bigFloat.ToBinaryString(includeGuardBits: false);

        // Assert
        Assert.Equal("0.00000000000000000000000000000000", resultWithGuardBits);
        Assert.Equal("0", resultWithoutGuardBits);
    }

    [Fact]
    public void ToBinaryString_WithZeroWithAccuracyValue_ShouldHandleGuardBitsCorrectly()
    {
        // Arrange
        var bigFloat = BigFloat.ZeroWithAccuracy(10);

        // Act
        string resultWithGuardBits = bigFloat.ToBinaryString(includeGuardBits: true);
        string resultWithoutGuardBits = bigFloat.ToBinaryString(includeGuardBits: false);

        // Assert
        Assert.Equal("0." + new string('0', 10 + BigFloat.GuardBits), resultWithGuardBits);
        Assert.Equal("0." + new string('0', 10), resultWithoutGuardBits);
    }

    [Theory]
    [InlineData(32)]  // Exactly at boundary
    [InlineData(33)]  // Just over boundary
    [InlineData(50)]  // Well over boundary
    public void ToBinaryString_AtScaleBoundary_ShouldUseTraditionalBehavior(int scale)
    {
        // Arrange
        BigInteger mantissa = BigInteger.Parse("1208925819614629174706175");
        var bigFloat = new BigFloat(mantissa, scale, true);

        // Act
        string resultWithGuardBits = bigFloat.ToBinaryString(includeGuardBits: true);
        string resultWithoutGuardBits = bigFloat.ToBinaryString(includeGuardBits: false);

        // Assert - Should be identical since Scale >= 32
        Assert.Equal(resultWithoutGuardBits, resultWithGuardBits);

        // Should end with all zeros
        string trailingSection = resultWithGuardBits[^(scale-BigFloat.GuardBits)..];
        Assert.Equal(new string('0', (scale - BigFloat.GuardBits)), trailingSection);
    }
}