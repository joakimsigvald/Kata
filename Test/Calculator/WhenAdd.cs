using Applique.WhenGivenThen.Core;
using System;
using Xunit;

namespace StringCalculator.Test.Calculator;
public abstract class WhenAdd : TestStatic<int>
{
    protected string Numbers;

    protected override void Act() => CollectResult(() => StringCalculator.Calculator.Add(Numbers));

    public class GivenInvalidNumbers : WhenAdd
    {
        [Theory]
        [InlineData("A")]
        [InlineData("1,\n")]
        [InlineData("//;\n1,2")]
        public void ThenThrowFormatException(string numbers)
        {
            Numbers = numbers;
            Assert.Throws<FormatException>(ArrangeAndAct);
        }
    }

    public class GivenValidNumbers : WhenAdd
    {
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
        public void ThenReturnSum(string numbers, int expected)
        {
            Numbers = numbers;
            ArrangeAndAct();
            Assert.Equal(expected, Result);
        }
    }
}