namespace E.Standard.Parsing.Tests;

public class ExpressionClassifierTests
{
    [Theory]
    [InlineData("Objekt [NAME]")]
    [InlineData("[FIRSTNAME] [LASTNAME]")]
    [InlineData("[FIELD]")]
    [InlineData("Object-ID [FIELD]")]
    [InlineData("")]
    public void Classify_PreservesLegacyTemplates(string expression)
    {
        Assert.Equal(ExpressionSyntax.LegacyTemplate, ExpressionClassifier.Classify(expression));
    }

    [Theory]
    [InlineData("concat([FIRSTNAME], \" \", [LASTNAME])")]
    [InlineData("unknown()")]
    [InlineData("[AREA] / 10000")]
    [InlineData("\"literal\"")]
    [InlineData("42")]
    [InlineData("true")]
    [InlineData("(1 + 2)")]
    [InlineData("![ACTIVE]")]
    [InlineData("true && false")]
    [InlineData("null == [MISSING]")]
    public void Classify_RecognizesStructuredExpressions(string expression)
    {
        Assert.Equal(ExpressionSyntax.StructuredExpression, ExpressionClassifier.Classify(expression));
    }
}
