// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

using BigFloatLibrary;
using System;
using System.Collections.Generic;
using static BigFloatLibrary.BigFloat;

namespace PlaygroundAndShowCase;

public static class Showcase
{
    public static void Main()
    {
        Console.WriteLine("==== BigFloat Showcase ====");
        Console.WriteLine();

        BasicConstructionAndArithmetic();
        PrecisionControl();
        ConstantsAndHighPrecisionMath();
        ParsingAndFormatting();
        TranscendentalAndPowerFunctions();
    }

    private static void BasicConstructionAndArithmetic()
    {
        Console.WriteLine("-- Basic construction and arithmetic");

        BigFloat fromString = new("123456789.012345678901234");
        BigFloat fromDouble = new(1234.56789012345678);

        Console.WriteLine($"fromString: {fromString}");
        Console.WriteLine($"fromDouble: {fromDouble}");

        Console.WriteLine($"Sum:        {fromString + fromDouble}");
        Console.WriteLine($"Difference: {fromString - fromDouble}");
        Console.WriteLine($"Product:    {fromString * fromDouble}");
        Console.WriteLine($"Quotient:   {fromString / fromDouble}");
        Console.WriteLine();
    }

    private static void PrecisionControl()
    {
        Console.WriteLine("-- Precision control and integer accuracy");

        BigFloat preciseNumber = new("123.45678901234567890123");
        BigFloat extended = AdjustPrecision(preciseNumber, deltaBits: 64);
        BigFloat reduced = AdjustPrecision(preciseNumber, deltaBits: -32);

        Console.WriteLine($"Original precision: {preciseNumber} (bits: {preciseNumber.Precision})");
        Console.WriteLine($"Extended by 64 bits: {extended} (bits: {extended.Precision})");
        Console.WriteLine($"Reduced by 32 bits:  {reduced} (bits: {reduced.Precision})");

        BigFloat integerWithAccuracy = IntWithAccuracy(42, 128);
        Console.WriteLine($"Integer with declared accuracy: {integerWithAccuracy}");
        Console.WriteLine();
    }

    private static void ConstantsAndHighPrecisionMath()
    {
        Console.WriteLine("-- Constants and high precision math");

        Dictionary<string, BigFloat> constants = Constants.WithConfig(precisionInBits: 2048).GetAll();
        BigFloat pi = constants["Pi"];
        BigFloat e = constants["E"];

        BigFloat radius = new("50.0");
        BigFloat circumference = 2 * pi * radius;
        BigFloat exponential = BigFloat.Pow(e, 3);

        Console.WriteLine($"Pi (2048-bit precision): {pi}");
        Console.WriteLine($"e^3 with matching precision: {exponential}");
        Console.WriteLine($"Circumference for r=50: {circumference}");
        Console.WriteLine();
    }

    private static void ParsingAndFormatting()
    {
        Console.WriteLine("-- Parsing and formatting");

        BigFloat decimalValue = Parse("98765.4321");
        BigFloat hexValue = Parse("0x1.fffffffffffffp+10");
        BigFloat binaryValue = Parse("0b1010.0011", binaryScaler: -4);

        Console.WriteLine($"Parsed decimal: {decimalValue}");
        Console.WriteLine($"Parsed hex:     {hexValue}");
        Console.WriteLine($"Parsed binary:  {binaryValue}");

        Console.WriteLine($"Digit-masked output: {ToStringDecimal(decimalValue, digitMaskingForm: true)}");
        Console.WriteLine();
    }

    private static void TranscendentalAndPowerFunctions()
    {
        Console.WriteLine("-- Powers, roots, and trigonometry");

        BigFloat baseValue = new("1.0000000000001");
        BigFloat squared = Pow(baseValue, 2);
        BigFloat reciprocal = Pow(baseValue, -1);
        BigFloat squareRoot = Sqrt(new("2.0"), wantedPrecision: 256);

        BigFloat angle = Constants.WithConfig(precisionInBits: 512).GetAll()["Pi"] / 6; // π/6
        BigFloat sine = Sin(angle);
        BigFloat cosine = Cos(angle);

        Console.WriteLine($"base^2:       {squared}");
        Console.WriteLine($"1/base:        {reciprocal}");
        Console.WriteLine($"sqrt(2):       {squareRoot}");
        Console.WriteLine($"sin(π/6):      {sine}");
        Console.WriteLine($"cos(π/6):      {cosine}");
        Console.WriteLine();
    }
}
