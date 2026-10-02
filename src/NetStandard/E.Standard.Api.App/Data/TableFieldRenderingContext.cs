using System;
using System.Collections.Generic;
using System.Collections.Specialized;

using E.Standard.WebMapping.Core;

namespace E.Standard.Api.App.Data;

public class TableFieldRenderingContext
{
    public TableFieldRenderingContext(NameValueCollection requestHeaders)
    {
        RequestHeaders = requestHeaders;
    }

    public NameValueCollection RequestHeaders { get; }
}

public sealed class QueryFieldRenderingContext
{
    private readonly IReadOnlyDictionary<TableField, TableFieldRenderingContext> _fieldContexts;
    private readonly NameValueCollection _requestHeaders;

    internal QueryFieldRenderingContext(
        IReadOnlyDictionary<TableField, TableFieldRenderingContext> fieldContexts,
        NameValueCollection requestHeaders)
    {
        _fieldContexts = fieldContexts;
        _requestHeaders = requestHeaders;
    }

    public string RenderField(TableField field, Feature feature)
        => field.RenderField(
            feature,
            _fieldContexts.TryGetValue(field, out var context)
                ? context
                : field.CreateRenderingContext(_requestHeaders));

    public TableFieldRenderingContext GetFieldContext(TableField field)
        => _fieldContexts.TryGetValue(field, out var context)
            ? context
            : throw new InvalidOperationException(
                $"No rendering context was initialized for field '{field?.ColumnName}'.");
}
