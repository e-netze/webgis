using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;

using E.Standard.CMS.Core;
using E.Standard.Parsing;
using E.Standard.Parsing.StructuredExpressions;
using E.Standard.Web.Abstractions;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.CMS.Expressions;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Geometry;

namespace E.Standard.Api.App.Data;

public sealed class TableFieldExpression : TableField
{
    public string Expression { get; set; }

    public ColumnDataType ColDataType { get; set; }

    public override string RenderField(
        WebMapping.Core.Feature feature,
        TableFieldRenderingContext context)
    {
        var expressionContext = context as ExpressionRenderingContext
            ?? throw new ArgumentException(
                $"Invalid rendering context for {nameof(TableFieldExpression)}.",
                nameof(context));

        if (expressionContext.CompiledExpression is not null)
        {
            return CmsExpressionEvaluator.EvaluateStructuredExpression(
                expressionContext.CompiledExpression,
                feature,
                (targetSRefId, functionName) => TransformShape(
                    feature.Shape,
                    targetSRefId,
                    functionName));
        }

        return CmsExpressionEvaluator.EvaluateLegacyTableColumn(
            feature,
            expressionContext.Expression,
            expressionContext.LegacyParameters,
            expressionContext.ContainsLegacyEvalExpression);
    }

    public override TableFieldRenderingContext CreateRenderingContext(
        NameValueCollection requestHeaders)
    {
        if (ExpressionClassifier.Classify(Expression) == ExpressionSyntax.StructuredExpression)
        {
            return new ExpressionRenderingContext(
                requestHeaders,
                Expression,
                new ExpressionEvaluator().Compile(Expression),
                legacyParameters: null,
                containsLegacyEvalExpression: false);
        }

        return new ExpressionRenderingContext(
            requestHeaders,
            Expression,
            compiledExpression: null,
            legacyParameters: Helper.GetKeyParameters(Expression),
            containsLegacyEvalExpression: Expression?.Contains("$") == true);
    }

    private static Shape TransformShape(
        Shape source,
        int targetSRefId,
        string functionName)
    {
        if (source.SrsId <= 0)
        {
            throw new ExpressionEvaluationException(
                $"Function '{functionName}' cannot transform a shape without a source SRefId",
                0);
        }

        return source.TransformedCopy(
            targetSRefId,
            (fromSRefId, toSRefId) => new GeometricTransformerPro(
                ApiGlobals.SRefStore.SpatialReferences,
                fromSRefId,
                toSRefId));
    }

    public override IEnumerable<string> FeatureFieldNames
    {
        get
        {
            return Helper.GetKeyParameterFields(this.Expression);
        }
    }

    private sealed class ExpressionRenderingContext(
        NameValueCollection requestHeaders,
        string expression,
        CompiledExpression compiledExpression,
        IReadOnlyList<string> legacyParameters,
        bool containsLegacyEvalExpression)
        : TableFieldRenderingContext(requestHeaders)
    {
        public string Expression { get; } = expression;
        public CompiledExpression CompiledExpression { get; } = compiledExpression;
        public IReadOnlyList<string> LegacyParameters { get; } = legacyParameters;
        public bool ContainsLegacyEvalExpression { get; } = containsLegacyEvalExpression;
    }
}
