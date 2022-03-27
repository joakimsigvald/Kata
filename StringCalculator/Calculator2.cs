using System;
using System.Linq;

namespace StringCalculator
{
    public class Calculator2
    {
        private readonly char[] defaultDelimeters = new char[] { ',', '\n' };

        public int Add(string expr)
        {
            if (string.IsNullOrEmpty(expr))
                return 0;

            ParseExpression(expr, out int[] numbers);

            return numbers.Sum();
        }

        private void ParseExpression(string expr, out int[] numbers)
        {
            var delimeters = HasCustomerDelimeters(expr) ?
            GetCustomDelimeters(expr) : defaultDelimeters;
            var numbersString = HasCustomerDelimeters(expr) ?
            expr[4..] : expr;
            numbers = GetNumbers(numbersString, delimeters);
        }

        private bool HasCustomerDelimeters(string expr)
        {
            return expr.StartsWith("//");
        }

        private char[] GetCustomDelimeters(string expr)
        {
            return new char[] { expr[2] };
        }

        private int[] GetNumbers(string numbers, params char[] delimeters)
        {
            return numbers.Split(delimeters).Select(Parse).ToArray();
        }

        private int Parse(string number)
        {
            return int.TryParse(number, out var val) ? val
            : throw new ArgumentException($"Not a number: {number}");
        }
    }
}