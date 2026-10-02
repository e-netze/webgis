namespace E.Standard.Parsing.StructuredExpressions;

public abstract class ExpressionException : Exception
{
    protected ExpressionException(string message, int position)
        : base($"{message} at position {position + 1}")
    {
        Position = position;
    }

    public int Position { get; }
}

public sealed class ExpressionParseException : ExpressionException
{
    public ExpressionParseException(string message, int position)
        : base(message, position)
    {
    }
}

public sealed class ExpressionEvaluationException : ExpressionException
{
    public ExpressionEvaluationException(string message, int position)
        : base(message, position)
    {
    }

    internal static ExpressionEvaluationException Type(
        int position,
        string expected,
        ExpressionValueKind actual)
        => new($"Expected {expected}, but got {actual.ToString().ToLowerInvariant()}", position);
}
