using System.Globalization;

namespace E.Standard.Parsing.StructuredExpressions;

public enum ExpressionValueKind
{
    Null,
    String,
    Number,
    Boolean,
    DateTime
}

public readonly struct ExpressionValue
{
    private readonly object? _value;

    private ExpressionValue(ExpressionValueKind kind, object? value)
    {
        Kind = kind;
        _value = value;
    }

    public ExpressionValueKind Kind { get; }

    public bool IsNull => Kind == ExpressionValueKind.Null;

    public static ExpressionValue Null { get; } = new(ExpressionValueKind.Null, null);

    public static ExpressionValue From(string? value)
        => value is null ? Null : new(ExpressionValueKind.String, value);

    public static ExpressionValue From(double value)
        => new(ExpressionValueKind.Number, value);

    public static ExpressionValue From(bool value)
        => new(ExpressionValueKind.Boolean, value);

    public static ExpressionValue From(DateTime value)
        => new(ExpressionValueKind.DateTime, value);

    public static ExpressionValue FromObject(object? value)
        => value switch
        {
            null => Null,
            string text => From(text),
            bool boolean => From(boolean),
            byte number => From(number),
            short number => From(number),
            int number => From(number),
            long number => From(number),
            float number => From(number),
            double number => From(number),
            decimal number => From((double)number),
            DateTime dateTime => From(dateTime),
            _ => From(Convert.ToString(value, CultureInfo.InvariantCulture))
        };

    public string StringValue(int position)
        => Kind == ExpressionValueKind.String
            ? (string)_value!
            : throw ExpressionEvaluationException.Type(position, "string", Kind);

    public double NumberValue(int position)
    {
        if (Kind == ExpressionValueKind.Number)
        {
            return (double)_value!;
        }

        if (Kind == ExpressionValueKind.String
            && Double.TryParse(
                ((string)_value!).Replace(",", "."),
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var number))
        {
            return number;
        }

        throw ExpressionEvaluationException.Type(position, "number", Kind);
    }

    public bool BooleanValue(int position)
    {
        if (Kind == ExpressionValueKind.Boolean)
        {
            return (bool)_value!;
        }

        if (Kind == ExpressionValueKind.String
            && Boolean.TryParse((string)_value!, out var boolean))
        {
            return boolean;
        }

        throw ExpressionEvaluationException.Type(position, "boolean", Kind);
    }

    public DateTime DateTimeValue(int position)
        => Kind == ExpressionValueKind.DateTime
            ? (DateTime)_value!
            : throw ExpressionEvaluationException.Type(position, "date", Kind);

    public string ToInvariantString()
        => Kind switch
        {
            ExpressionValueKind.Null => String.Empty,
            ExpressionValueKind.String => (string)_value!,
            ExpressionValueKind.Number => ((double)_value!).ToString("G15", CultureInfo.InvariantCulture),
            ExpressionValueKind.Boolean => (bool)_value! ? "true" : "false",
            ExpressionValueKind.DateTime => ((DateTime)_value!).ToString("O", CultureInfo.InvariantCulture),
            _ => String.Empty
        };

    public override string ToString() => ToInvariantString();
}
