using System.Text.RegularExpressions;

namespace CalculateDemo.Server.Classes;

public class Calculation
{
    public bool CalculationSuccess { get; set; }
    public float? CalculationResult { get; set; }
    public string? ErrorMessage { get; set; }

    public Calculation(string expression)
    {
        var sanitizedString = Regex.Replace(expression, @"[^\d.+*/\-()\s]", "");
        if (expression != sanitizedString)
        {
            ErrorMessage = "Invalid characters found in expression";
        }
        var ops = new List<string> { "+", "-", "*", "/", "(", ")" };
        ops.ForEach(x => expression = expression.Replace(x, $" {x} "));
        var tokens = expression.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        CalculationSuccess = TryEvaluateExpression(tokens, out var result);
        if (CalculationSuccess)
        {
            CalculationResult = result;
        }
        else
        {
            ErrorMessage = "Calculation has failed";
        }
    }

    private static int GetPrecedence(string op)
    {
        switch (op)
        {
            case "+":
            case "-":
                return 1;
            case "*":
            case "/":
                return 2;
            default:
                return 0;
        }
    }

    private static IEnumerable<string> InfixToPostfix(List<string> tokens)
    {
        var stack = new Stack<string>();
        var output = new List<string>();

        foreach (var token in tokens)
        {
            if (float.TryParse(token, out _))  // If the token is a number
            {
                output.Add(token);
            }
            else if (token == "(")
            {
                stack.Push(token);
            }
            else if (token == ")")
            {
                while (stack.Count > 0 && stack.Peek() != "(")
                {
                    output.Add(stack.Pop());
                }
                stack.Pop();  // Pop the '('
            }
            else  // The token is an operator
            {
                while (stack.Count > 0 && GetPrecedence(token) <= GetPrecedence(stack.Peek()))
                {
                    output.Add(stack.Pop());
                }
                stack.Push(token);
            }
        }

        while (stack.Count > 0)
        {
            output.Add(stack.Pop());
        }

        return output;
    }

    public static bool TryEvaluateExpression(List<string> tokens, out float result)
    {
        if (tokens.Count == 0)
        {
            result = 0;
            return false;
        }
        // if it starts with a sign first (unary op), we simply add "0" at the beginning and process as binary op
        if ("+-".Contains(tokens.First()))
        {
            tokens.Insert(0, "0");
        }
        var postfix = InfixToPostfix(tokens);
        var stack = new Stack<float>();

        foreach (var token in postfix)
        {
            if (float.TryParse(token, out var num))
            {
                stack.Push(num);
            }
            else
            {
                if (stack.Count < 2)
                {
                    result = 0;
                    return false;
                }

                var second = stack.Pop();
                var first = stack.Pop();

                switch (token)
                {
                    case "+":
                        stack.Push(first + second);
                        break;
                    case "-":
                        stack.Push(first - second);
                        break;
                    case "*":
                        stack.Push(first * second);
                        break;
                    case "/":
                        if (second == 0)
                        {
                            result = 0;
                            return false;
                        }
                        stack.Push(first / second);
                        break;
                }
            }
        }

        if (stack.Count != 1)
        {
            result = 0;
            return false;
        }

        result = stack.Pop();
        return true;
    }
}