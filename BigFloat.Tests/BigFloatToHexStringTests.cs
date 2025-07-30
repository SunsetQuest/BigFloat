using BigFloatLibrary;
using Xunit;

public class BigFloatToHexStringTests
{
    [Fact]
    public void ToHexString_BinaryWithRoundUp_Returns2AB5556()
    {
        // Arrange
        var bigFloat = new BigFloat("0b101010101|10101010101010101.100000000000000");

        // Act
        var result = bigFloat.ToHexString();

        // Assert
        Assert.Equal("2AB5556", result);
    }

    [Fact]
    public void ToHexString_BinaryWithFractionalPart_Returns2AAAAB()
    {
        // Arrange
        var bigFloat = new BigFloat("0b1010101010101010101010.|1010101010101010101010101010");

        // Act
        var result = bigFloat.ToHexString();

        // Assert
        Assert.Equal("2AAAAB", result);
    }

    [Fact]
    public void ToHexString_BinaryCase3_ReturnsValidHexFormat()
    {
        // Arrange
        var bigFloat = new BigFloat("0b101010101010101010101.0|1010101010101010101010101010");

        // Act
        var result = bigFloat.ToHexString();

        // Assert
        Assert.Equal("155555", result);
    }

    [Fact]
    public void ToHexString_BinaryCase4_ReturnsValidHexFormat()
    {
        // Arrange
        var bigFloat = new BigFloat("0b1010101010101010101.010|1010101010101010101010101010");

        // Act
        var result = bigFloat.ToHexString();

        // Assert
        Assert.Equal("55555.5", result);
    }

    [Fact]
    public void ToHexString_BinaryCase5_ReturnsValidHexFormat()
    {
        // Arrange
        var bigFloat = new BigFloat("0b10101010101010101.01010|1010101010101010101010101010");

        // Act
        var result = bigFloat.ToHexString();

        // Assert
        Assert.Equal("15555.5", result);
    }

    [Fact]
    public void ToHexString_BinaryCase6_Returns2AAAAB()
    {
        // Arrange
        var bigFloat = new BigFloat("0b101010101010101010.1010|1010101010101010101010101010");

        // Act
        var result = bigFloat.ToHexString();

        // Assert
        Assert.Equal("2AAAA.B", result);
    }

    [Fact]
    public void ToHexString_BinaryCase7_ReturnsValidHexFormat()
    {
        // Arrange
        var bigFloat = new BigFloat("0b10101010101010101010.10|1010101010101010101010101010");

        // Act
        var result = bigFloat.ToHexString();

        // Assert
        // Can be either AAAAB or AAAAA.B
        var validResults = new[] { "AAAAB", "AAAAA.B" };
        Assert.Contains(result, validResults);
    }

    [Fact]
    public void ToHexString_BinaryCase8_ReturnsValidHexFormat()
    {
        // Arrange
        var bigFloat = new BigFloat("0b10101010101010101010.101010|101010101010101010101010");

        // Act
        var result = bigFloat.ToHexString();

        // Assert
        var validResults = new[] { "AAAAA.B", "AAAAA.AB" };
        Assert.Contains(result, validResults);
    }

    [Theory]
    [InlineData("0b101010101|10101010101010101.100000000000000", "2AB5556")]
    [InlineData("0b1010101010101010101010.|1010101010101010101010101010", "2AAAAB")]
    [InlineData("0b101010101010101010.1010|1010101010101010101010101010", "2AAAA.B")]
    public void ToHexString_ExactCases_ReturnsExpectedResult(string binaryInput, string expectedHex)
    {
        // Arrange
        var bigFloat = new BigFloat(binaryInput);

        // Act
        var result = bigFloat.ToHexString();

        // Assert
        Assert.Equal(expectedHex, result);
    }

    [Theory]
    [InlineData("0b101010101010101010101.0|1010101010101010101010101010", new[] { "155555" })]
    [InlineData("0b1010101010101010101.010|1010101010101010101010101010", new[] { "55555.5" })]
    [InlineData("0b10101010101010101.01010|1010101010101010101010101010", new[] { "15555.5" })]
    [InlineData("0b10101010101010101010.10|1010101010101010101010101010", new[] { "AAAAB", "AAAAA.B" })]
    [InlineData("0b10101010101010101010.101010|101010101010101010101010", new[] { "AAAAA.B", "AAAAA.AB" })]
    public void ToHexString_RoundingCases_ReturnsValidResult(string binaryInput, string[] validResults)
    {
        // Arrange
        var bigFloat = new BigFloat(binaryInput);

        // Act
        var result = bigFloat.ToHexString();

        // Assert
        Assert.Contains(result, validResults);
    }
}