using System;
using System.Numerics;
using Xunit;
using BigFloatLibrary;


//todo: check and convert to unit tests

namespace BigFloatLibrary.Tests;

public class BigFloatBinaryStringExtensiveTests
{
    private const int GuardBits = 32; // BigFloat.GuardBits

    public static void RunAllTests()
    {
        Console.WriteLine("=== BigFloat Binary String Comprehensive Test Suite ===\n");

        // Run all test categories
        TestEdgeCasesAroundGuardBits();
        TestScalingBoundaryConditions();
        TestPrecisionLimits();
        TestSignHandling();
        TestOverflowUnderflowConditions();
        TestBinaryRepresentationEdgeCases();
        TestLargeScaleValues();
        TestSubnormalAndDenormalizedNumbers();
        TestRoundingBehavior();
        TestSpecialPatterns();

        Console.WriteLine("\n=== Test Suite Complete ===");
    }

    static void TestEdgeCasesAroundGuardBits()
    {
        Console.WriteLine("--- Testing Edge Cases Around Guard Bits ---");

        // Test exactly at guard bit boundary
        Test("1" + new string('0', GuardBits - 1), 0, includeGuard: false, "1");
        Test("1" + new string('0', GuardBits), 0, includeGuard: false, "10");
        Test("1" + new string('0', GuardBits + 1), 0, includeGuard: false, "100");

        // Test guard bit boundary with includeGuard true
        Test("1" + new string('0', GuardBits - 1), 0 + GuardBits, includeGuard: true, "1" + new string('0', GuardBits - 1));
        Test("1" + new string('0', GuardBits), 0 + GuardBits, includeGuard: true, "1" + new string('0', GuardBits));

        // Test fractional parts at guard bit boundary
        Test("0." + new string('0', GuardBits - 2) + "1", 0, includeGuard: false, "0");
        Test("0." + new string('0', GuardBits - 1) + "1", 0, includeGuard: false, "0");
        Test("0." + new string('0', GuardBits) + "1", 0, includeGuard: false, "0");

        // With guard bits included
        Test("0." + new string('0', GuardBits - 2) + "1", 0 + GuardBits, includeGuard: true, "0." + new string('0', GuardBits - 2) + "1");
    }

    static void TestScalingBoundaryConditions()
    {
        Console.WriteLine("--- Testing Scaling Boundary Conditions ---");

        // Test large positive scales
        for (int scale = 1; scale <= 10; scale++)
        {
            Test("1", scale + GuardBits, includeGuard: true, "1" + new string('0', scale));
            Test("101", scale + GuardBits, includeGuard: true, "101" + new string('0', scale));
        }

        // Test large negative scales
        for (int scale = -1; scale >= -10; scale--)
        {
            Test("1000", scale + GuardBits, includeGuard: true,
                 scale == -1 ? "100.0" :
                 scale == -2 ? "10.00" :
                 scale == -3 ? "1.000" :
                 "0." + new string('0', Math.Abs(scale) - 4) + "1000");
        }

        // Test at the transition point where numbers become too small
        Test("1", -GuardBits - 1, includeGuard: false, "0");
        Test("1", -GuardBits, includeGuard: false, "0");
        Test("1", -GuardBits + 1, includeGuard: false, "0");
    }

    static void TestPrecisionLimits()
    {
        Console.WriteLine("--- Testing Precision Limits ---");

        // Test maximum reasonable precision
        string longBinary = "1" + new string('0', 100) + "1";
        Test(longBinary, 0 + GuardBits, includeGuard: true, longBinary);
        Test(longBinary, 10 + GuardBits, includeGuard: true, longBinary + new string( '0', 10));

        // Test alternating patterns for maximum entropy
        string alternating64 = string.Join("", Enumerable.Range(0, 64).Select(i => (i % 2).ToString()));
        Test(alternating64, 0 + GuardBits, includeGuard: true, alternating64);

        // Test all ones patterns
        string allOnes32 = new('1', 32);
        string allOnes64 = new('1', 64);
        Test(allOnes32, 0 + GuardBits, includeGuard: true, allOnes32);
        Test(allOnes64, 0 + GuardBits, includeGuard: true, allOnes64);
    }

    static void TestSignHandling()
    {
        Console.WriteLine("--- Testing Sign Handling ---");

        // Test negative numbers with various scales
        Test("-1", 0 + GuardBits, includeGuard: true, "-1");
        Test("-101", 0 + GuardBits, includeGuard: true, "-101");
        Test("-1111", 0 + GuardBits, includeGuard: true, "-1111");

        // Test negative with scaling
        Test("-1", 5 + GuardBits, includeGuard: true, "-100000");
        Test("-1", -5 + GuardBits, includeGuard: true, "-0.00001");

        // Test negative numbers that become zero due to precision
        Test("-1", -GuardBits - 1, includeGuard: false, "0");
        Test("-1", -GuardBits, includeGuard: false, "0");

        // Test negative fractional
        Test("-0.1", 0 + GuardBits, includeGuard: true, "-0.1");
        Test("-0.001", 0 + GuardBits, includeGuard: true, "-0.001");
    }

    static void TestOverflowUnderflowConditions()
    {
        Console.WriteLine("--- Testing Overflow/Underflow Conditions ---");

        // Test numbers that are too large for normal representation
        string veryLarge = "1" + new string('0', 200);
        Test(veryLarge, 0, includeGuard: false,
             veryLarge.Length > GuardBits ? "1" + new string('0', veryLarge.Length - GuardBits) : veryLarge);

        // Test very small numbers
        string verySmall = "0." + new string('0', 200) + "1";
        Test(verySmall, 0, includeGuard: false, "0");
        Test(verySmall, 0 + GuardBits, includeGuard: true, verySmall);

        // Test at the boundary of representable range
        Test("1", 1000, includeGuard: false, "0"); // Too large, no guard bits
        Test("1", -1000, includeGuard: false, "0"); // Too small, no guard bits
    }

    static void TestBinaryRepresentationEdgeCases()
    {
        Console.WriteLine("--- Testing Binary Representation Edge Cases ---");

        // Test powers of 2
        for (int power = 0; power < 10; power++)
        {
            string powerOf2 = "1" + new string('0', power);
            Test(powerOf2, 0 + GuardBits, includeGuard: true, powerOf2);
            Test(powerOf2, 3 + GuardBits, includeGuard: true, powerOf2 + "000");
            Test(powerOf2, -3 + GuardBits, includeGuard: true,
                 power >= 3 ? powerOf2[..(power - 2)] + "." + powerOf2[(power - 2)..] :
                 "0." + new string('0', 3 - power - 1) + powerOf2);
        }

        // Test numbers with single bit set at various positions
        for (int pos = 0; pos < 20; pos++)
        {
            string singleBit = new string('0', pos) + "1" + new string('0', 5);
            Test(singleBit, 0 + GuardBits, includeGuard: true, singleBit);
        }

        // Test all combinations of short binary strings
        for (int val = 1; val < 16; val++)
        {
            string binary = Convert.ToString(val, 2);
            Test(binary, 0 + GuardBits, includeGuard: true, binary);
            Test(binary, 2 + GuardBits, includeGuard: true, binary + "00");
            Test(binary, -2 + GuardBits, includeGuard: true,
                 binary.Length > 2 ? binary[..^2] + "." + binary[^2..] :
                 "0." + new string('0', 2 - binary.Length) + binary);
        }
    }

    static void TestLargeScaleValues()
    {
        Console.WriteLine("--- Testing Large Scale Values ---");

        // Test extreme positive scales
        int[] largeScales = [50, 100, 500, 1000];
        foreach (int scale in largeScales)
        {
            Test("1", scale + GuardBits, includeGuard: true, "1" + new string('0', scale));
            Test("11", scale + GuardBits, includeGuard: true, "11" + new string('0', scale));

            // Without guard bits, these should become 0
            Test("1", scale, includeGuard: false, "0");
        }

        // Test extreme negative scales
        int[] largeNegativeScales = [-50, -100, -500, -1000];
        foreach (int scale in largeNegativeScales)
        {
            Test("1" + new string('0', Math.Abs(scale) + 10), scale + GuardBits, includeGuard: true,
                 "0." + new string('0', Math.Abs(scale) - 1) + "1" + new string('0', 10));

            // Without guard bits, these should become 0
            Test("1", scale, includeGuard: false, "0");
        }
    }

    static void TestSubnormalAndDenormalizedNumbers()
    {
        Console.WriteLine("--- Testing Subnormal/Denormalized Numbers ---");

        // Test numbers that are at the edge of representability
        string leadingZeros = "0." + new string('0', GuardBits - 1) + "1";
        Test(leadingZeros, 0, includeGuard: false, "0");
        Test(leadingZeros, 0 + GuardBits, includeGuard: true, leadingZeros);

        // Test progressive reduction in magnitude
        for (int zeros = GuardBits - 5; zeros <= GuardBits + 5; zeros++)
        {
            string smallNum = "0." + new string('0', zeros) + "1";
            Test(smallNum, 0, includeGuard: false, "0");
            Test(smallNum, 0 + GuardBits, includeGuard: true, smallNum);
        }
    }

    static void TestRoundingBehavior()
    {
        Console.WriteLine("--- Testing Rounding Behavior ---");

        // Test rounding at various precision boundaries
        Test("1.1", 0, includeGuard: false, "0");
        Test("1.1", 0 + GuardBits, includeGuard: true, "1.1");

        Test("1.0001", 0, includeGuard: false, "0");
        Test("1.0001", 0 + GuardBits, includeGuard: true, "1.0001");

        // Test rounding with scaling
        Test("1111.1111", 2 + GuardBits, includeGuard: true, "111111.11");
        Test("1111.1111", -2 + GuardBits, includeGuard: true, "11.111111");
    }

    static void TestSpecialPatterns()
    {
        Console.WriteLine("--- Testing Special Patterns ---");

        // Test Fibonacci-like patterns
        Test("11011", 0 + GuardBits, includeGuard: true, "11011");
        Test("110110110", 0 + GuardBits, includeGuard: true, "110110110");

        // Test palindromic patterns
        Test("10101", 0 + GuardBits, includeGuard: true, "10101");
        Test("1100110011", 0 + GuardBits, includeGuard: true, "1100110011");

        // Test patterns that might trigger edge cases in binary arithmetic
        Test("111111111111111111111111111111111", 0 + GuardBits, includeGuard: true, "111111111111111111111111111111111");
        Test("100000000000000000000000000000001", 0 + GuardBits, includeGuard: true, "100000000000000000000000000000001");

        // Test repeating patterns
        string repeating = string.Join("", Enumerable.Range(0, 20).Select(_ => "110"));
        Test(repeating, 0 + GuardBits, includeGuard: true, repeating);
    }

    public static void Test(string bin, int scale, bool includeGuard, string shouldBe)
    {
        try
        {
            BigFloat val = BigFloat.ParseBinary(bin, scale - GuardBits, 0, 32);

            //int bufferSize = val.CalculateBinaryStringLength(includeGuard ? 32 : 0);
            //if (bufferSize < shouldBe.Length || bufferSize > shouldBe.Length + 3)
            //{
            //    Console.WriteLine($"BUFFER SIZE MISMATCH: {bin,40} scale={scale,3} guard={includeGuard} => bufferSize={bufferSize}, expected={shouldBe.Length}-{shouldBe.Length + 3}");
            //}

            string res = val.ToBinaryString(includeGuard);
            if (res != shouldBe)
            {
                Console.WriteLine($"MISMATCH: {bin,40} scale={scale,3} guard={includeGuard} => '{res}' (expected: '{shouldBe}')");
            }
            else
            {
                Console.WriteLine($"PASS:     {bin,40} scale={scale,3} guard={includeGuard} => '{res}'");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR:    {bin,40} scale={scale,3} guard={includeGuard} => Exception: {ex.Message}");
        }
    }
}

// Helper class for generating test patterns
public static class TestPatternGenerator
{
    public static IEnumerable<string> GenerateBinaryPatterns(int minLength, int maxLength)
    {
        for (int length = minLength; length <= maxLength; length++)
        {
            // Powers of 2
            yield return "1" + new string('0', length - 1);

            // All ones
            yield return new string('1', length);

            // Alternating patterns
            yield return string.Join("", Enumerable.Range(0, length).Select(i => (i % 2).ToString()));
            yield return string.Join("", Enumerable.Range(0, length).Select(i => ((i + 1) % 2).ToString()));

            // Single bit patterns
            for (int pos = 0; pos < length; pos++)
            {
                char[] pattern = new char[length];
                for (int i = 0; i < length; i++) pattern[i] = '0';
                pattern[pos] = '1';
                yield return new string(pattern);
            }
        }
    }

    public static IEnumerable<int> GenerateScaleValues()
    {
        // Test around guard bit boundaries
        for (int i = -BigFloat.GuardBits - 5; i <= BigFloat.GuardBits + 5; i++)
            yield return i;

        // Test larger values
        int[] largeValues = [-1000, -500, -100, -50, 50, 100, 500, 1000];
        foreach (int val in largeValues)
            yield return val;
    }
}


// Extension methods for comprehensive testing
public static class BigFloatTestExtensions
{
    public static void RunComprehensiveTest(this string binaryInput, int scale, bool includeGuard, string expected)
    {
        BigFloatBinaryStringExtensiveTests.Test(binaryInput, scale, includeGuard, expected);
    }
}