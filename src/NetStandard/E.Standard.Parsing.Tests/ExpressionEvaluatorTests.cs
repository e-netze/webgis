using E.Standard.Parsing.StructuredExpressions;

namespace E.Standard.Parsing.Tests;

public class ExpressionEvaluatorTests
{
    private readonly ExpressionEvaluator _evaluator = new();

    [Theory]
    [InlineData("1 + 2 * 3", "7")]
    [InlineData("(1 + 2) * 3", "9")]
    [InlineData("10 / 4", "2.5")]
    [InlineData("10 % 4", "2")]
    [InlineData("-2 + 5", "3")]
    [InlineData("1 < 2 && 3 >= 3", "true")]
    [InlineData("1 == 1 || 1 / 0 == 0", "true")]
    [InlineData("false && 1 / 0 == 0", "false")]
    [InlineData("\"a\" == \"a\"", "true")]
    [InlineData("null == null", "true")]
    public void Evaluate_UsesTypedOperatorsAndPrecedence(string expression, string expected)
    {
        Assert.Equal(expected, _evaluator.Evaluate(expression).ToInvariantString());
    }

    [Fact]
    public void Evaluate_ResolvesFieldsAndCustomFunctions()
    {
        var result = _evaluator.Evaluate(
            "double([VALUE]) + [OFFSET]",
            name => name switch
            {
                "VALUE" => ExpressionValue.From(5),
                "OFFSET" => ExpressionValue.From(2),
                _ => null
            },
            (name, arguments) => name == "double"
                ? ExpressionValue.From(arguments[0].NumberValue(0) * 2)
                : null);

        Assert.Equal("12", result.ToInvariantString());
        Assert.True(_evaluator.Evaluate("is_null([MISSING])").BooleanValue(0));
    }

    [Fact]
    public void Evaluate_PreservesNumericTextUntilANumberIsRequired()
    {
        ExpressionValue? Resolve(string name)
            => name == "CODE" ? ExpressionValue.From("001") : null;

        Assert.Equal(
            "Code 001",
            _evaluator.Evaluate("concat(\"Code \", [CODE])", Resolve).ToInvariantString());
        Assert.Equal(
            "2",
            _evaluator.Evaluate("[CODE] + 1", Resolve).ToInvariantString());
        Assert.True(_evaluator.Evaluate("[CODE] == 1", Resolve).BooleanValue(0));
    }

    [Fact]
    public void Compile_ReusesParsedExpressionWithDifferentResolvers()
    {
        var expression = _evaluator.Compile("[VALUE] + 1");

        Assert.Equal(
            "2",
            expression.Evaluate(name => ExpressionValue.From("1")).ToInvariantString());
        Assert.Equal(
            "6",
            expression.Evaluate(name => ExpressionValue.From("5")).ToInvariantString());
    }

    [Theory]
    [InlineData("concat(\"A\", null, 2, true)", "A2true")]
    [InlineData("upper(\"Text\")", "TEXT")]
    [InlineData("lower(\"Text\")", "text")]
    [InlineData("trim(\" x \")", "x")]
    [InlineData("substring(\"abcdef\", 2)", "cdef")]
    [InlineData("substring(\"abcdef\", 2, 3)", "cde")]
    [InlineData("replace(\"a-b\", \"-\", \"/\")", "a/b")]
    [InlineData("length(\"abc\")", "3")]
    [InlineData("is_empty(\"\")", "true")]
    [InlineData("is_empty(null)", "true")]
    [InlineData("is_null(null_if_empty(\"\"))", "true")]
    [InlineData("round(1.235, 2)", "1.24")]
    [InlineData("abs(-3)", "3")]
    [InlineData("min(3, 1, 2)", "1")]
    [InlineData("max(3, 1, 2)", "3")]
    [InlineData("format_date(\"2025-03-04T10:20:30Z\", \"yyyy-MM-dd\")", "2025-03-04")]
    [InlineData("year(\"2025-03-04\")", "2025")]
    [InlineData("month(\"2025-03-04\")", "3")]
    [InlineData("day(\"2025-03-04\")", "4")]
    [InlineData("coalesce(null, \"\", \"fallback\")", "")]
    public void Evaluate_ProvidesBuiltInFunctions(string expression, string expected)
    {
        Assert.Equal(expected, _evaluator.Evaluate(expression).ToInvariantString());
    }

    [Fact]
    public void Evaluate_LazilyEvaluatesConditionalFunctions()
    {
        Assert.Equal("safe", _evaluator.Evaluate("if(true, \"safe\", 1 / 0)").ToInvariantString());
        Assert.Equal("safe", _evaluator.Evaluate("coalesce(\"safe\", 1 / 0)").ToInvariantString());
    }

    [Theory]
    [InlineData("1 / 0", "Division by zero")]
    [InlineData("\"a\" + \"b\"", "Expected number")]
    [InlineData("unknown()", "Unknown function 'unknown'")]
    [InlineData("round()", "expects 1 or 2")]
    public void Evaluate_ReportsTypedErrors(string expression, string message)
    {
        var exception = Assert.Throws<ExpressionEvaluationException>(
            () => _evaluator.Evaluate(expression));

        Assert.Contains(message, exception.Message);
        Assert.True(exception.Position >= 0);
    }

    [Theory]
    [InlineData("\"unterminated", 0)]
    [InlineData("[FIELD", 0)]
    [InlineData("1 +", 3)]
    [InlineData("concat(1", 8)]
    [InlineData("1.2.3", 0)]
    public void Evaluate_ReportsParsePositions(string expression, int position)
    {
        var exception = Assert.Throws<ExpressionParseException>(
            () => _evaluator.Evaluate(expression));

        Assert.Equal(position, exception.Position);
        Assert.Contains($"position {position + 1}", exception.Message);
    }
}
