using System.Linq;

namespace StringCalculator;
public static class Calculator
{
    public static int Add(string expr)
        => string.IsNullOrEmpty(expr) ? 0
        : expr.StartsWith("//") ? Sum(expr[2..3], expr[4..])
        : Sum(",\n", expr);

    private static int Sum(string d10s, string n7s) => n7s.Split(d10s.ToArray()).Sum(int.Parse);
}