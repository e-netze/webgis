namespace E.Standard.Parsing.StructuredExpressions;

public sealed class CompiledExpression
{
    private readonly Func<
        ExpressionVariableResolver?,
        ExpressionFunctionResolver?,
        ExpressionValue> _evaluate;

    internal CompiledExpression(
        Func<
            ExpressionVariableResolver?,
            ExpressionFunctionResolver?,
            ExpressionValue> evaluate)
    {
        _evaluate = evaluate;
    }

    public ExpressionValue Evaluate(
        ExpressionVariableResolver? variableResolver = null,
        ExpressionFunctionResolver? functionResolver = null)
        => _evaluate(variableResolver, functionResolver);
}
