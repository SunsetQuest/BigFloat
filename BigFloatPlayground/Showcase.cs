// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

using BigFloatLibrary;
using static BigFloatLibrary.BigFloat;

namespace PlaygroundAndShowCase;

internal static partial class Showcase
{
    ////////////// Play Area & Examples //////////////
    public static void Main()
    {
        //////////////////// TEST AREA ////////////////////


        //Console.WriteLine(ceil0.ToBinaryString(false));
        //Console.WriteLine(ceil1.ToBinaryString(false));

        /////// Author experimentation area - Please make sure to comment this top area out! ///////
        // Console.WriteLine("2320000 -> " + BigFloat.Parse("2320000"));
        // Console.WriteLine("232XXXX -> " + BigFloat.Parse("232XXXX"));
        // Console.WriteLine("232XX -> " + BigFloat.Parse("232XX"));
        // Console.WriteLine("232X.X -> " + BigFloat.Parse("232X.X"));
        // Console.WriteLine("232 -> " + BigFloat.Parse("232"));
        // Console.WriteLine("23.2 -> " + BigFloat.Parse("23.2"));
        // Console.WriteLine("2.32 -> " + BigFloat.Parse("2.32"));
        // Console.WriteLine("0.232 -> " + BigFloat.Parse("0.232"));
        // Console.WriteLine("0.00232 -> " + BigFloat.Parse("0.00232"));
        // Console.WriteLine("0.00000000232 -> " + BigFloat.Parse("0.00232"));
        // Console.WriteLine("0.000000000232 -> " + BigFloat.Parse("0.00000000000232"));
        // Console.WriteLine("0.0000000000000000000000000000232 -> " + BigFloat.Parse("0.0000000000000000000000000000232"));

        // BigFloat randNumber = BigFloat.RandomWithMantissaBits(1024, 0, 0);
        // Console.WriteLine($"The number {randNumber} is {randNumber.Size} bits.");

        // BigFloat x = new("1.1234567890123456"); Console.WriteLine(ConstantVisualization.GetContinuedFraction(x));
        // Console.WriteLine(ConstantVisualization.GetConstantInfo("Pi"));
        // var allConstants = BigFloat.Constants.WithConfig(precisionInBits: 20000).GetAll();
        // Console.WriteLine(ConstantVisualization.CreateComparisonTable(allConstants));

        // TestingArea.CastingFromFloatAndDouble_Stuff(); return;
        // TestingArea.Constant_Stuff(); return;
        // Benchmarks.Inverse_Benchmark(); return;
        // Benchmarks.NthRoot_Benchmark(); return;
        // Benchmarks.NthRoot_Benchmark2(); return;
        // TestingArea.Pow_Stuff(); return;
        // Benchmarks.Pow_Benchmark(); return;
        // TestingArea.Pow_Stuff4(); return;
        // Benchmarks.PowMostSignificantBits_Benchmark(); return;
        // TestingArea.ToStringHexScientific_Stuff(); return;
        // TestingArea.Compare_Stuff(); return;
        // Benchmarks.GeneratePi_Benchmark(); return;
        // TestingArea.Parse_Stuff(); return;
        // TestingArea.TryParse_Stuff(); return;
        // TestingArea.TruncateToAndRound_Stuff(); return;
        // TestingArea.Remainder_Stuff(); return;
        // TestingArea.Sqrt_Stuff(); return;


        //////////////////// Initializing and Basic Arithmetic: ////////////////////
        // Initialize BigFloat numbers
        BigFloat a = new("123456789.012345678901234"); // Initialize by String
        BigFloat b = new(1234.56789012345678);         // Initialize by Double

        // Basic arithmetic
        BigFloat sum = a + b;
        BigFloat difference = a - b;
        BigFloat product = a * b;
        BigFloat quotient = a / b;

        // Display results
        Console.WriteLine($"Sum: {sum}");
        // Output: Sum: 123458023.5802358024

        Console.WriteLine($"Difference: {difference}");
        // Output: Difference: 123455554.4444555554

        Console.WriteLine($"Product: {product}");
        // Output: Product: 152415787532.39

        Console.WriteLine($"Quotient: {quotient}");
        // Output: Quotient: 100000.000000000


        //////////////////// Working with Mathematical Constants: ////////////////////
        // Access constants like Pi or E from Constants
        Dictionary<string, BigFloat> bigConstants = Constants.WithConfig(precisionInBits: 1000).GetAll();
        BigFloat pi = bigConstants["Pi"];
        BigFloat e = bigConstants["E"];

        Console.WriteLine($"e to 1000 binary digits: {e.ToString()}");
        // Output:
        // e to 1000 binary digits: 2.718281828459045235360287471352662497757247093699959574966967
        // 627724076630353547594571382178525166427427466391932003059921817413596629043572900334295
        // 260595630738132328627943490763233829880753195251019011573834187930702154089149934884167
        // 509244761460668082264800168477411853742345442437107539077744992070

        // Use Pi in a calculation (Area of a circle with r = 100)
        BigFloat radius = new("100.0000000000000000");
        BigFloat area = pi * radius * radius;

        Console.WriteLine($"Area of the circle: {area}");
        // Output: Area of the circle: 31415.9265358979324


        //////////////////// Precision Manipulation: ////////////////////
        // Initialize a number with high precision
        BigFloat preciseNumber = new("123.45678901234567890123");
        BigFloat morePreciseNumber = AdjustPrecision(preciseNumber, deltaBits: 50);

        Console.WriteLine($"Extend Precision result: {morePreciseNumber}");
        // Output: Extend Precision result: 123.45678901234567890123000000000102788

        // Initialize an integer with custom precision
        BigFloat c = IntWithAccuracy(10, 100);
        Console.WriteLine($"Int with specified accuracy: {c}");
        // Output: Int with specified accuracy: 10.000000000000000000000000000000


        //////////////////// Comparing Numbers: ////////////////////
        // Initialize two BigFloat numbers

        BigFloat num1 = new("12345.6790");
        BigFloat num2 = new("12345.6789");

        // Lets compare the numbers that are not equal...
        bool areEqual = num1 == num2;
        bool isFirstBigger = num1 > num2;

        Console.WriteLine($"Are the numbers equal? {areEqual}");
        // Output: Are the numbers equal? False

        Console.WriteLine($"Is the first number bigger? {isFirstBigger}");
        // Output: Is the first number bigger? True

        // Due to the nuances of decimal to binary conversion, we end up with some undesired rounding.
        BigFloat num3 = new("12345.6789");
        BigFloat num4 = new("12345.67896");

        areEqual = num3 == num4;
        isFirstBigger = num3 > num4;

        Console.WriteLine($"Are the numbers equal? {areEqual}");
        // Output: Are the numbers equal? True

        Console.WriteLine($"Is the first number bigger? {isFirstBigger}");
        // Output: Is the first number bigger? False


        //////////////////// Handling Very Large or Small Exponents: ////////////////////
        // Creating a large number
        BigFloat largeNumber = new("1234e+7");

        Console.WriteLine($"Large Number: {largeNumber}");
        // Output: Large Number: 1.234e+10

        Console.WriteLine($"Large Number: {ToStringDecimal(largeNumber, digitMaskingForm: true)}");
        // Output: Large Number: 1234XXXXXXX

        // Creating a very large number
        BigFloat veryLargeNumber = new("1234e+300");

        Console.WriteLine($"Large Number: {veryLargeNumber}");
        // Output: Large Number: 123 * 10^301

        // Creating a very small number
        BigFloat smallNumber = new("1e-300");

        Console.WriteLine($"Small Number: {smallNumber}");
        // Output: Small Number: 1e-300

        BigFloat num5 = new("12121212.1212");
        BigFloat num6 = new("1234");
        Console.WriteLine($"{num5} * {num6} = {num5 * num6}");
        // Output: 12121212.1212 * 1234 = 1.496e+10

        num5 = new("12121212.1212");
        num6 = new("3");
        BigFloat result = num5 * num6;
        Console.WriteLine($"{num5} * {num6} = {result}");
        // Output: 12121212.1212 * 3 = 4e+7

        num5 = new("121212.1212");
        num6 = new("1234567");

        Console.WriteLine($"{num5} * {num6} = {num5 * num6}");
        // Output: 121212.1212 * 1234567 = 1.496445e+11

        Console.WriteLine($"GetPrecision: {num6.Precision}");


        // Output: GetPrecision: 21
    }
}
