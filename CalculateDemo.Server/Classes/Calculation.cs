using NCalc;
using System.Text.RegularExpressions;

namespace CalculateDemo.Server.Classes;

public class Calculation
{
    public bool CalculationSuccess { get; set; }
    public double? CalculationResult { get; set; }
    public string? ErrorMessage { get; set; } = "Calculation has failed";

    public Calculation(string expression)
    {
        CalculationSuccess = false;
        if (expression.Length > 10000)
        {
            ErrorMessage = "Expression is too long";
            return;
        }

        // while potentially we could process much more complex expressions: https://github.com/pitermarx/NCalc-Edge/wiki/Functions
        // As of now we need to support only arithmetic operations, so everything else will be excluded
        var sanitizedString = Regex.Replace(expression, @"[^\d.+*/\-()\s]", "");
        if (expression != sanitizedString)
        {
            ErrorMessage = "Invalid characters in expression";
            return;
        }

        try
        {
            var e = new Expression(expression, EvaluateOptions.IgnoreCase);
            var evalResult = e.Evaluate();
            var result = Convert.ToDouble(evalResult);
            CalculationSuccess = !double.IsInfinity(result) && !double.IsNaN(result);
            if (CalculationSuccess)
            {
                CalculationResult = result;
            }
        }
        catch
        {
            CalculationSuccess = false;
        }
    }

    public Calculation() { }

}