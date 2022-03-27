using System;
using Xunit;

namespace StringCalculator.Test;
public class StringCalculatorTests
{
    [Theory]
    [InlineData("A")]
    [InlineData("1,\n")]
    [InlineData("//;\n1,2")]
    public void GivenInvalidNumbers_ThenThrowFormatException(string numbers)
        => Assert.Throws<FormatException>(() => StringCalculator.Calculator.Add(numbers));

    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    [InlineData("1", 1)]
    [InlineData("2", 2)]
    [InlineData("1,1", 2)]
    [InlineData("1,2", 3)]
    [InlineData("2,3,5,7,11", 28)]
    [InlineData("1\n2,3", 6)]
    [InlineData("//;\n1;2", 3)]
    [InlineData("//,\n1,2", 3)]
    public void GivenValidNumbers_ThenReturnSumOfNumbers(string numbers, int expected)
        => Assert.Equal(expected, StringCalculator.Calculator.Add(numbers));
}