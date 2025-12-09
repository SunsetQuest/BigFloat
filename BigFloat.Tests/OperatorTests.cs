// Copyright(c) 2020 - 2025 Ryan Scott White
// Licensed under the MIT License. See LICENSE.txt in the project root for details.

namespace BigFloatLibrary.Tests;

/// <summary>
/// Tests for arithmetic and unary operators
/// </summary>
public class OperatorTests
{
    #region Addition Tests

    [Theory]
    [InlineData("0", "0", "0")]
    [InlineData("1", "0", "1")]
    [InlineData("0", "1", "1")]
    [InlineData("1", "1", "2")]
    [InlineData("-1", "1", "0")]
    [InlineData("1", "-1", "0")]
    [InlineData("-1", "-1", "-2")]
    [InlineData("123.456", "789.012", "912.468")]
    [InlineData("-123.456", "789.012", "665.556")]
    [InlineData("123.456", "-789.012", "-665.556")]
    [InlineData("-123.456", "-789.012", "-912.468")]
    public void Addition_ReturnsCorrectResult(string a, string b, string expected)
    {
        var bfA = new BigFloat(a);
        var bfB = new BigFloat(b);
        var expectedResult = new BigFloat(expected);
        
        var result = bfA + bfB;
        Assert.True(result.EqualsUlp(expectedResult, 1));
        
        // Test commutativity
        var reverseResult = bfB + bfA;
        Assert.True(reverseResult.EqualsUlp(expectedResult, 1));
    }

    [Theory]
    [InlineData("1e100", "1e100", "2e100")]
    [InlineData("1e-100", "1e-100", "2e-100")]
    [InlineData("1.23456789e50", "9.87654321e50", "1.111111110e51")]
    public void Addition_LargeAndSmallNumbers(string a, string b, string expectedApprox)
    {
        var bfA = new BigFloat(a);
        var bfB = new BigFloat(b);
        var expectedResult = new BigFloat(expectedApprox);
        
        var result = bfA + bfB;
        Assert.True(result.EqualsUlp(expectedResult, 10));
    }

    #endregion

    #region Subtraction Tests

    [Theory]
    [InlineData("0", "0", "0")]
    [InlineData("1", "0", "1")]
    [InlineData("0", "1", "-1")]
    [InlineData("1", "1", "0")]
    [InlineData("-1", "1", "-2")]
    [InlineData("1", "-1", "2")]
    [InlineData("-1", "-1", "0")]
    [InlineData("123.456", "789.012", "-665.556")]
    [InlineData("-123.456", "789.012", "-912.468")]
    [InlineData("123.456", "-789.012", "912.468")]
    [InlineData("-123.456", "-789.012", "665.556")]
    public void Subtraction_ReturnsCorrectResult(string a, string b, string expected)
    {
        var bfA = new BigFloat(a);
        var bfB = new BigFloat(b);
        var expectedResult = new BigFloat(expected);
        
        var result = bfA - bfB;
        Assert.True(result.EqualsUlp(expectedResult, 1));
    }

    [Theory]
    [InlineData("2e100", "1e100", "1e100")]
    [InlineData("2e-100", "1e-100", "1e-100")]
    [InlineData("1.111111110e51", "9.87654321e50", "1.23456789e50")]
    public void Subtraction_LargeAndSmallNumbers(string a, string b, string expectedApprox)
    {
        var bfA = new BigFloat(a);
        var bfB = new BigFloat(b);
        var expectedResult = new BigFloat(expectedApprox);
        
        var result = bfA - bfB;
        Assert.True(result.EqualsUlp(expectedResult, 10));
    }

    #endregion

    #region Multiplication Tests

    [Theory]
    [InlineData("0", "0", "0")]
    [InlineData("1", "0", "0")]
    [InlineData("0", "1", "0")]
    [InlineData("1", "1", "1")]
    [InlineData("-1", "1", "-1")]
    [InlineData("1", "-1", "-1")]
    [InlineData("-1", "-1", "1")]
    [InlineData("2", "3", "6")]
    [InlineData("-2", "3", "-6")]
    [InlineData("2", "-3", "-6")]
    [InlineData("-2", "-3", "6")]
    [InlineData("123.456", "789.012", "97408.265472")]
    [InlineData("0.5", "0.5", "0.25")]
    [InlineData("0.1", "0.1", "0.01")]
    public void Multiplication_ReturnsCorrectResult(string a, string b, string expected)
    {
        var bfA = new BigFloat(a);
        var bfB = new BigFloat(b);
        var expectedResult = new BigFloat(expected);
        
        var result = bfA * bfB;
        Assert.True(result.EqualsUlp(expectedResult, 1, true));
        
        // Test commutativity
        var reverseResult = bfB * bfA;
        Assert.True(reverseResult.EqualsUlp(expectedResult, 1, true));
    }

    [Theory]
    [InlineData("1e50", "1e50", "1e100")]
    [InlineData("1e-50", "1e-50", "1e-100")]
    [InlineData("2e30", "3e40", "6e70")]
    [InlineData("1.23456789e20", "9.87654321e30", "1.219326311e51")]
    public void Multiplication_LargeAndSmallNumbers(string a, string b, string expectedApprox)
    {
        var bfA = new BigFloat(a);
        var bfB = new BigFloat(b);
        var expectedResult = new BigFloat(expectedApprox);
        
        var result = bfA * bfB;
        Assert.True(result.EqualsUlp(expectedResult, 10));
    }

    #endregion

    #region Division Tests

    [Theory]
    [InlineData("0", "1", "0")]
    [InlineData("1", "1", "1")]
    [InlineData("-1", "1", "-1")]
    [InlineData("1", "-1", "-1")]
    [InlineData("-1", "-1", "1")]
    [InlineData("6", "2", "3")]
    [InlineData("6", "3", "2")]
    [InlineData("-6", "2", "-3")]
    [InlineData("6", "-2", "-3")]
    [InlineData("-6", "-2", "3")]
    [InlineData("1", "2", "0.5")]
    [InlineData("1", "4", "0.25")]
    [InlineData("1", "8", "0.125")]
    [InlineData("3", "4", "0.75")]
    public void Division_ReturnsCorrectResult(string a, string b, string expected)
    {
        var bfA = new BigFloat(a);
        var bfB = new BigFloat(b);
        var expectedResult = new BigFloat(expected);
        
        var result = bfA / bfB;
        Assert.True(result.EqualsZeroExtended(expectedResult));
    }

    [Theory]
    [InlineData("1", "3", "0.3")]
    [InlineData("2", "3", "1")]
    [InlineData("1", "7", "0.1")]
    [InlineData("1", "9", "0.1")]
    public void Division_RepeatingDecimals(string a, string b, string expectedPrefix)
    {
        var bfA = new BigFloat(a);
        var bfB = new BigFloat(b);
        
        var result = bfA / bfB;
        var resultStr = result.ToString();
        
        Assert.Equal(expectedPrefix, resultStr);
    }

#if !DEBUG
    [Fact]
    public void Division_ByZero_ThrowsException()
    {
        var numerator = new BigFloat(1);
        var zero = BigFloat.ZeroWithAccuracy(0);
        
        Assert.Throws<DivideByZeroException>(() => numerator / zero);
    }
#endif

    [Theory]
    [InlineData("1e100", "1e50", "1e50")]
    [InlineData("1e-50", "1e-25", "1e-25")]
    [InlineData("6e70", "2e30", "3e40")]
    public void Division_LargeAndSmallNumbers(string a, string b, string expected)
    {
        var bfA = new BigFloat(a);
        var bfB = new BigFloat(b);
        var expectedResult = new BigFloat(expected);
        
        var result = bfA / bfB;
        Assert.True(result.EqualsUlp(expectedResult, 10));
    }

    #endregion

    #region Modulo Tests

    [Theory]
    [InlineData("7", "3", "1")]
    [InlineData("8", "3", "2")]
    [InlineData("9", "3", "0")]
    [InlineData("10", "3", "1")]
    [InlineData("-7", "3", "-1")]
    [InlineData("7", "-3", "1")]
    [InlineData("-7", "-3", "-1")]
    [InlineData("100", "7", "2")]
    [InlineData("123.456", "10", "3.456")]
    public void Modulo_ReturnsCorrectResult(string a, string b, string expected)
    {
        var bfA = new BigFloat(a);
        var bfB = new BigFloat(b);
        var expectedResult = new BigFloat(expected);
        
        var result = bfA % bfB;
        Assert.True(result.EqualsUlp(expectedResult, 1));
    }

#if !DEBUG
    [DebuggerHidden]
    [Fact]
    public void Modulo_ByZero_ThrowsException()
    {
        var dividend = new BigFloat(7);
        var zero = BigFloat.ZeroWithAccuracy(10);
        Assert.Throws<DivideByZeroException>(() => dividend % zero);
    }
#endif


    #endregion

    #region Unary Operators Tests

    [Theory]
    [InlineData("0", "0")]
    [InlineData("1", "-1")]
    [InlineData("-1", "1")]
    [InlineData("123.456", "-123.456")]
    [InlineData("-123.456", "123.456")]
    [InlineData("1e100", "-1e100")]
    [InlineData("-1e100", "1e100")]
    public void UnaryNegation_ReturnsNegatedValue(string input, string expected)
    {
        var bf = new BigFloat(input);
        var expectedResult = new BigFloat(expected);
        
        var result = -bf;
        Assert.Equal(expectedResult, result);
        
        // Double negation should return original
        var doubleNegation = -(-bf);
        Assert.Equal(bf, doubleNegation);
    }

    [Theory]
    [InlineData("0", "0")]
    [InlineData("1", "1")]
    [InlineData("-1", "-1")]
    [InlineData("123.456", "123.456")]
    [InlineData("-123.456", "-123.456")]
    public void UnaryPlus_ReturnsSameValue(string input, string expected)
    {
        var bf = new BigFloat(input);
        var expectedResult = new BigFloat(expected);
        
        var result = +bf;
        Assert.Equal(expectedResult, result);
    }

    #endregion

    #region Increment and Decrement Tests

    [Theory]
    [InlineData("0", "1")]
    [InlineData("1", "2")]
    [InlineData("-1", "0")]
    [InlineData("99", "100")]
    [InlineData("999", "1000")]
    [InlineData("123.456", "124.456")]
    [InlineData("-123.456", "-122.456")]
    public void PreIncrement_AddsOne(string input, string expected)
    {
        var bf = new BigFloat(input);
        var expectedResult = new BigFloat(expected);
        
        var result = ++bf;
        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedResult, bf); // bf should also be modified
    }

    [Theory]
    [InlineData("0", "1")]
    [InlineData("1", "2")]
    [InlineData("-1", "0")]
    [InlineData("99", "100")]
    [InlineData("999", "1000")]
    [InlineData("123.456", "124.456")]
    [InlineData("-123.456", "-122.456")]
    public void PostIncrement_AddsOne(string input, string expected)
    {
        var bf = new BigFloat(input);
        var original = new BigFloat(input);
        var expectedResult = new BigFloat(expected);
        
        var result = bf++;
        Assert.Equal(original, result); // Should return original value
        Assert.Equal(expectedResult, bf); // bf should be incremented
    }

    [Theory]
    [InlineData("0", "-1")]
    [InlineData("1", "0")]
    [InlineData("-1", "-2")]
    [InlineData("100", "99")]
    [InlineData("1000", "999")]
    [InlineData("123.456", "122.456")]
    [InlineData("-123.456", "-124.456")]
    public void PreDecrement_SubtractsOne(string input, string expected)
    {
        var bf = new BigFloat(input);
        var expectedResult = new BigFloat(expected);
        
        var result = --bf;
        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedResult, bf); // bf should also be modified
    }

    [Theory]
    [InlineData("0", "-1")]
    [InlineData("1", "0")]
    [InlineData("-1", "-2")]
    [InlineData("100", "99")]
    [InlineData("1000", "999")]
    [InlineData("123.456", "122.456")]
    [InlineData("-123.456", "-124.456")]
    public void PostDecrement_SubtractsOne(string input, string expected)
    {
        var bf = new BigFloat(input);
        var original = new BigFloat(input);
        var expectedResult = new BigFloat(expected);
        
        var result = bf--;
        Assert.Equal(original, result); // Should return original value
        Assert.Equal(expectedResult, bf); // bf should be decremented
    }

    #endregion

    #region Compound Assignment Operators

    [Theory]
    [InlineData("5", "3", "8")]
    [InlineData("-5", "3", "-2")]
    [InlineData("5", "-3", "2")]
    [InlineData("123.456", "789.012", "912.468")]
    public void AddAssignment_AddsAndAssigns(string initial, string addend, string expected)
    {
        var bf = new BigFloat(initial);
        var toAdd = new BigFloat(addend);
        var expectedResult = new BigFloat(expected);
        
        bf += toAdd;
        Assert.True(bf.EqualsUlp(expectedResult, 1));
    }

    [Theory]
    [InlineData("5", "3", "2")]
    [InlineData("-5", "3", "-8")]
    [InlineData("5", "-3", "8")]
    [InlineData("123.456", "789.012", "-665.556")]
    public void SubtractAssignment_SubtractsAndAssigns(string initial, string subtrahend, string expected)
    {
        var bf = new BigFloat(initial);
        var toSubtract = new BigFloat(subtrahend);
        var expectedResult = new BigFloat(expected);
        
        bf -= toSubtract;
        Assert.True(bf.EqualsUlp(expectedResult, 1));
    }

    [Theory]
    [InlineData("5", "3", "15")]
    [InlineData("-5", "3", "-15")]
    [InlineData("5", "-3", "-15")]
    [InlineData("2.5", "4", "10")]
    public void MultiplyAssignment_MultipliesAndAssigns(string initial, string multiplier, string expected)
    {
        var bf = new BigFloat(initial);
        var toMultiply = new BigFloat(multiplier);
        var expectedResult = new BigFloat(expected);
        
        bf *= toMultiply;
        Assert.True(bf.EqualsZeroExtended(expectedResult));
    }

    [Theory]
    [InlineData("15", "3", "5")]
    [InlineData("-15", "3", "-5")]
    [InlineData("15", "-3", "-5")]
    [InlineData("10", "4", "2.5")]
    public void DivideAssignment_DividesAndAssigns(string initial, string divisor, string expected)
    {
        var bf = new BigFloat(initial);
        var toDivide = new BigFloat(divisor);
        var expectedResult = new BigFloat(expected);
        
        bf /= toDivide;
        Assert.True(bf.EqualsZeroExtended(expectedResult));
    }

    [Theory]
    [InlineData("10", "3", "1")]
    [InlineData("-10", "3", "-1")]
    [InlineData("10", "-3", "1")]
    [InlineData("123", "10", "3")]
    public void ModuloAssignment_ModulosAndAssigns(string initial, string divisor, string expected)
    {
        var bf = new BigFloat(initial);
        var toMod = new BigFloat(divisor);
        var expectedResult = new BigFloat(expected);
        
        bf %= toMod;
        Assert.True(bf.EqualsUlp(expectedResult, 1));
    }

    #endregion

    #region Mathematical Properties Tests

    [Theory]
    [InlineData("2", "3", "4")]
    [InlineData("1.5", "2.5", "3.5")]
    [InlineData("-5", "10", "-15")]
    public void Addition_CommutativeProperty(string a, string b, string c)
    {
        var bfA = new BigFloat(a);
        var bfB = new BigFloat(b);
        var bfC = new BigFloat(c);
        
        // Commutative: a + b = b + a
        Assert.Equal(bfA + bfB, bfB + bfA);
        
        // Associative: (a + b) + c = a + (b + c)
        Assert.Equal((bfA + bfB) + bfC, bfA + (bfB + bfC));
    }

    [Theory]
    [InlineData("2", "3", "4")]
    [InlineData("1.5", "2.5", "3.5")]
    [InlineData("-5", "10", "-15")]
    public void Multiplication_CommutativeProperty(string a, string b, string c)
    {
        var bfA = new BigFloat(a);
        var bfB = new BigFloat(b);
        var bfC = new BigFloat(c);
        
        // Commutative: a * b = b * a
        Assert.Equal(bfA * bfB, bfB * bfA);
        
        // Associative: (a * b) * c = a * (b * c)
        Assert.True(((bfA * bfB) * bfC).EqualsUlp(bfA * (bfB * bfC), 2));
    }

    [Theory]
    [InlineData("2", "3", "4")]
    [InlineData("5", "10", "15")]
    public void Multiplication_DistributiveProperty(string a, string b, string c)
    {
        var bfA = new BigFloat(a);
        var bfB = new BigFloat(b);
        var bfC = new BigFloat(c);
        
        // Distributive: a * (b + c) = (a * b) + (a * c)
        var left = bfA * (bfB + bfC);
        var right = (bfA * bfB) + (bfA * bfC);
        Assert.True(left.EqualsUlp(right, 2));
    }

    #endregion

    #region Edge Cases for Division

    [Theory]
    [InlineData("1", "1e-100", "1e100")]
    [InlineData("1", "1e-200", "1e200")]
    [InlineData("123", "1e-50", "123e50")]
    public void Division_ByVerySmallNumber_ProducesLargeResult(string numerator, string denominator, string expected)
    {
        var num = new BigFloat(numerator);
        var denom = new BigFloat(denominator);
        var expectedResult = new BigFloat(expected);
        
        var result = num / denom;
        Assert.True(result.EqualsUlp(expectedResult, 10));
    }

    [Theory]
    [InlineData("1e100", "1e100", "1")]
    [InlineData("1e-100", "1e-100", "1")]
    [InlineData("5e200", "5e200", "1")]
    public void Division_SameLargeNumbers_ReturnsOne(string a, string b, string expected)
    {
        var bfA = new BigFloat(a);
        var bfB = new BigFloat(b);
        var expectedResult = new BigFloat(expected);
        
        var result = bfA / bfB;
        Assert.True(result.EqualsUlp(expectedResult, 1));
    }

    #endregion

    #region Chained Operations Tests

    [Theory]
    [InlineData("10", "20", "30", "40", "100")]
    [InlineData("1.5", "2.5", "3.5", "4.5", "12")]
    [InlineData("-1", "-2", "-3", "-4", "-10")]
    public void ChainedAddition_CalculatesCorrectly(string a, string b, string c, string d, string expected)
    {
        var bfA = new BigFloat(a);
        var bfB = new BigFloat(b);
        var bfC = new BigFloat(c);
        var bfD = new BigFloat(d);
        var expectedResult = new BigFloat(expected);
        
        var result = bfA + bfB + bfC + bfD;
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData("100", "10", "2", "5", "1")]
    [InlineData("1000", "10", "10", "10", "1")]
    public void ChainedDivision_CalculatesCorrectly(string a, string b, string c, string d, string expected)
    {
        var bfA = new BigFloat(a);
        var bfB = new BigFloat(b);
        var bfC = new BigFloat(c);
        var bfD = new BigFloat(d);
        var expectedResult = new BigFloat(expected);
        
        var result = bfA / bfB / bfC / bfD;
        Assert.True(result.EqualsUlp(expectedResult, 2));
    }

    [Fact]
    public void MixedOperations_FollowPrecedence()
    {
        var a = new BigFloat(2);
        var b = new BigFloat(3);
        var c = new BigFloat(4);
        var d = new BigFloat(5);
        
        // Should be: 2 + (3 * 4) - 5 = 2 + 12 - 5 = 9
        var result = a + b * c - d;
        var expected = new BigFloat(9);
        Assert.Equal(expected, result);
        
        // Should be: ((2 * 3) + 4) / 5 = (6 + 4) / 5 = 10 / 5 = 2
        var result2 = (a * b + c) / d;
        var expected2 = new BigFloat(2);
        Assert.Equal(expected2, result2);
    }

    #endregion

    #region Identity and Inverse Tests

    [Theory]
    [InlineData("123")]
    [InlineData("0.456")]
    [InlineData("-789")]
    [InlineData("1e100")]
    [InlineData("1e-100")]
    public void AdditionIdentity_ZeroHasNoEffect(string value)
    {
        var bf = new BigFloat(value);
        var zero = BigFloat.ZeroWithAccuracy(bf.Accuracy); // addition uses accuracy

        Assert.Equal(bf, bf + zero);
        Assert.Equal(bf, zero + bf);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("0.456")]
    [InlineData("-789")]
    [InlineData("1e100")]
    [InlineData("1e-100")]
    public void MultiplicationIdentity_OneHasNoEffect(string value)
    {
        var bf = new BigFloat(value);
        var one = BigFloat.OneWithAccuracy(bf.Precision); // multiply uses precision
        
        Assert.Equal(bf, bf * one);
        Assert.Equal(bf, one * bf);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("0.456")]
    [InlineData("-789")]
    public void AdditiveInverse_SumsToZero(string value)
    {
        var bf = new BigFloat(value);
        var inverse = -bf;
        
        var sum = bf + inverse;
        Assert.True(sum.IsZero);
    }

    [Theory]
    [InlineData("2")]
    [InlineData("0.5")]
    [InlineData("-4")]
    [InlineData("123.456")]
    public void MultiplicativeInverse_ProductIsOne(string value)
    {
        var bf = new BigFloat(value);
        var inverse = 1 / bf;
        
        var product = bf * inverse;
        Assert.True(product.EqualsUlp(1, 2));
    }

    #endregion
}