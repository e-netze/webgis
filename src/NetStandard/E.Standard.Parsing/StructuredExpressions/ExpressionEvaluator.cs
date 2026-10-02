using System.Globalization;

namespace E.Standard.Parsing.StructuredExpressions;

public delegate ExpressionValue? ExpressionVariableResolver(string name);
public delegate ExpressionValue? ExpressionFunctionResolver(
    string name,
    IReadOnlyList<ExpressionValue> arguments);

public sealed class ExpressionEvaluator
{
    public ExpressionValue Evaluate(
        string expression,
        ExpressionVariableResolver? variableResolver = null,
        ExpressionFunctionResolver? functionResolver = null)
        => Compile(expression).Evaluate(variableResolver, functionResolver);

    public CompiledExpression Compile(string expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        var parser = new Parser(expression);
        var root = parser.Parse();
        return new CompiledExpression(
            (variableResolver, functionResolver) =>
                Evaluate(root, variableResolver, functionResolver));
    }

    private static ExpressionValue Evaluate(
        Node node,
        ExpressionVariableResolver? variableResolver,
        ExpressionFunctionResolver? functionResolver)
        => node switch
        {
            LiteralNode literal => literal.Value,
            FieldNode field => variableResolver?.Invoke(field.Name) ?? ExpressionValue.Null,
            UnaryNode unary => EvaluateUnary(unary, variableResolver, functionResolver),
            BinaryNode binary => EvaluateBinary(binary, variableResolver, functionResolver),
            FunctionNode function => EvaluateFunction(function, variableResolver, functionResolver),
            _ => throw new ExpressionEvaluationException("Unknown expression", node.Position)
        };

    private static ExpressionValue EvaluateUnary(
        UnaryNode node,
        ExpressionVariableResolver? variableResolver,
        ExpressionFunctionResolver? functionResolver)
    {
        var operand = Evaluate(node.Operand, variableResolver, functionResolver);
        return node.Operator switch
        {
            TokenKind.Minus => ExpressionValue.From(-operand.NumberValue(node.Position)),
            TokenKind.Bang => ExpressionValue.From(!operand.BooleanValue(node.Position)),
            _ => throw new ExpressionEvaluationException("Unsupported unary operator", node.Position)
        };
    }

    private static ExpressionValue EvaluateBinary(
        BinaryNode node,
        ExpressionVariableResolver? variableResolver,
        ExpressionFunctionResolver? functionResolver)
    {
        var left = Evaluate(node.Left, variableResolver, functionResolver);

        if (node.Operator == TokenKind.AndAnd)
        {
            return !left.BooleanValue(node.Position)
                ? ExpressionValue.From(false)
                : ExpressionValue.From(
                    Evaluate(node.Right, variableResolver, functionResolver)
                        .BooleanValue(node.Position));
        }

        if (node.Operator == TokenKind.OrOr)
        {
            return left.BooleanValue(node.Position)
                ? ExpressionValue.From(true)
                : ExpressionValue.From(
                    Evaluate(node.Right, variableResolver, functionResolver)
                        .BooleanValue(node.Position));
        }

        var right = Evaluate(node.Right, variableResolver, functionResolver);
        return node.Operator switch
        {
            TokenKind.Plus => ExpressionValue.From(
                left.NumberValue(node.Position) + right.NumberValue(node.Position)),
            TokenKind.Minus => ExpressionValue.From(
                left.NumberValue(node.Position) - right.NumberValue(node.Position)),
            TokenKind.Star => ExpressionValue.From(
                left.NumberValue(node.Position) * right.NumberValue(node.Position)),
            TokenKind.Slash => Divide(left, right, node.Position),
            TokenKind.Percent => Modulo(left, right, node.Position),
            TokenKind.EqualEqual => ExpressionValue.From(AreEqual(left, right)),
            TokenKind.BangEqual => ExpressionValue.From(!AreEqual(left, right)),
            TokenKind.Less => ExpressionValue.From(Compare(left, right, node.Position) < 0),
            TokenKind.LessEqual => ExpressionValue.From(Compare(left, right, node.Position) <= 0),
            TokenKind.Greater => ExpressionValue.From(Compare(left, right, node.Position) > 0),
            TokenKind.GreaterEqual => ExpressionValue.From(Compare(left, right, node.Position) >= 0),
            _ => throw new ExpressionEvaluationException("Unsupported binary operator", node.Position)
        };
    }

    private static ExpressionValue Divide(ExpressionValue left, ExpressionValue right, int position)
    {
        var divisor = right.NumberValue(position);
        if (divisor == 0)
        {
            throw new ExpressionEvaluationException("Division by zero", position);
        }

        return ExpressionValue.From(left.NumberValue(position) / divisor);
    }

    private static ExpressionValue Modulo(ExpressionValue left, ExpressionValue right, int position)
    {
        var divisor = right.NumberValue(position);
        if (divisor == 0)
        {
            throw new ExpressionEvaluationException("Modulo by zero", position);
        }

        return ExpressionValue.From(left.NumberValue(position) % divisor);
    }

    private static bool AreEqual(ExpressionValue left, ExpressionValue right)
    {
        if (left.IsNull || right.IsNull)
        {
            return left.IsNull && right.IsNull;
        }

        if (left.Kind != right.Kind)
        {
            if (left.Kind == ExpressionValueKind.Number || right.Kind == ExpressionValueKind.Number)
            {
                try
                {
                    return left.NumberValue(0) == right.NumberValue(0);
                }
                catch (ExpressionEvaluationException)
                {
                    return false;
                }
            }

            return false;
        }

        return left.Kind switch
        {
            ExpressionValueKind.String => left.StringValue(0) == right.StringValue(0),
            ExpressionValueKind.Number => left.NumberValue(0) == right.NumberValue(0),
            ExpressionValueKind.Boolean => left.BooleanValue(0) == right.BooleanValue(0),
            ExpressionValueKind.DateTime => left.DateTimeValue(0) == right.DateTimeValue(0),
            _ => true
        };
    }

    private static int Compare(ExpressionValue left, ExpressionValue right, int position)
    {
        if (left.Kind == ExpressionValueKind.Number || right.Kind == ExpressionValueKind.Number)
        {
            return left.NumberValue(position).CompareTo(right.NumberValue(position));
        }

        if (left.Kind != right.Kind || left.IsNull)
        {
            throw new ExpressionEvaluationException("Comparable values must have the same non-null type", position);
        }

        return left.Kind switch
        {
            ExpressionValueKind.String => String.CompareOrdinal(
                left.StringValue(position),
                right.StringValue(position)),
            ExpressionValueKind.Number => left.NumberValue(position).CompareTo(right.NumberValue(position)),
            ExpressionValueKind.DateTime => left.DateTimeValue(position).CompareTo(right.DateTimeValue(position)),
            _ => throw new ExpressionEvaluationException("Values are not comparable", position)
        };
    }

    private static ExpressionValue EvaluateFunction(
        FunctionNode node,
        ExpressionVariableResolver? variableResolver,
        ExpressionFunctionResolver? functionResolver)
    {
        var name = node.Name.ToLowerInvariant();

        if (name == "if")
        {
            RequireArgumentCount(node, 3, 3);
            var condition = Evaluate(node.Arguments[0], variableResolver, functionResolver)
                .BooleanValue(node.Position);
            return Evaluate(node.Arguments[condition ? 1 : 2], variableResolver, functionResolver);
        }

        if (name == "coalesce")
        {
            RequireArgumentCount(node, 1, Int32.MaxValue);
            foreach (var argument in node.Arguments)
            {
                var value = Evaluate(argument, variableResolver, functionResolver);
                if (!value.IsNull)
                {
                    return value;
                }
            }

            return ExpressionValue.Null;
        }

        var arguments = node.Arguments
            .Select(argument => Evaluate(argument, variableResolver, functionResolver))
            .ToArray();

        var builtIn = EvaluateBuiltIn(name, arguments, node.Position);
        if (builtIn.HasValue)
        {
            return builtIn.Value;
        }

        var custom = functionResolver?.Invoke(node.Name, arguments);
        return custom
            ?? throw new ExpressionEvaluationException($"Unknown function '{node.Name}'", node.Position);
    }

    private static ExpressionValue? EvaluateBuiltIn(
        string name,
        IReadOnlyList<ExpressionValue> arguments,
        int position)
        => name switch
        {
            "concat" => Concat(arguments),
            "upper" => UnaryString(arguments, position, value => value.ToUpperInvariant()),
            "lower" => UnaryString(arguments, position, value => value.ToLowerInvariant()),
            "trim" => UnaryString(arguments, position, value => value.Trim()),
            "substring" => Substring(arguments, position),
            "replace" => Replace(arguments, position),
            "length" => Length(arguments, position),
            "is_null" => IsNull(arguments, position),
            "is_empty" => IsEmpty(arguments, position),
            "null_if_empty" => NullIfEmpty(arguments, position),
            "round" => Round(arguments, position),
            "abs" => UnaryNumber(arguments, position, Math.Abs),
            "min" => MinMax(arguments, position, minimum: true),
            "max" => MinMax(arguments, position, minimum: false),
            "format_date" => FormatDate(arguments, position),
            "year" => DatePart(arguments, position, value => value.Year),
            "month" => DatePart(arguments, position, value => value.Month),
            "day" => DatePart(arguments, position, value => value.Day),
            _ => null
        };

    private static ExpressionValue Concat(IReadOnlyList<ExpressionValue> arguments)
        => ExpressionValue.From(String.Concat(arguments.Select(value => value.ToInvariantString())));

    private static ExpressionValue UnaryString(
        IReadOnlyList<ExpressionValue> arguments,
        int position,
        Func<string, string> operation)
    {
        RequireArgumentCount("function", arguments.Count, 1, 1, position);
        return ExpressionValue.From(operation(arguments[0].StringValue(position)));
    }

    private static ExpressionValue Substring(IReadOnlyList<ExpressionValue> arguments, int position)
    {
        RequireArgumentCount("substring", arguments.Count, 2, 3, position);
        var value = arguments[0].StringValue(position);
        var start = ToInteger(arguments[1], position);
        if (start < 0 || start > value.Length)
        {
            throw new ExpressionEvaluationException("Substring start is outside the string", position);
        }

        if (arguments.Count == 2)
        {
            return ExpressionValue.From(value.Substring(start));
        }

        var length = ToInteger(arguments[2], position);
        if (length < 0 || start + length > value.Length)
        {
            throw new ExpressionEvaluationException("Substring length is outside the string", position);
        }

        return ExpressionValue.From(value.Substring(start, length));
    }

    private static ExpressionValue Replace(IReadOnlyList<ExpressionValue> arguments, int position)
    {
        RequireArgumentCount("replace", arguments.Count, 3, 3, position);
        return ExpressionValue.From(
            arguments[0].StringValue(position).Replace(
                arguments[1].StringValue(position),
                arguments[2].StringValue(position)));
    }

    private static ExpressionValue Length(IReadOnlyList<ExpressionValue> arguments, int position)
    {
        RequireArgumentCount("length", arguments.Count, 1, 1, position);
        return ExpressionValue.From(arguments[0].StringValue(position).Length);
    }

    private static ExpressionValue IsNull(IReadOnlyList<ExpressionValue> arguments, int position)
    {
        RequireArgumentCount("is_null", arguments.Count, 1, 1, position);
        return ExpressionValue.From(arguments[0].IsNull);
    }

    private static ExpressionValue IsEmpty(IReadOnlyList<ExpressionValue> arguments, int position)
    {
        RequireArgumentCount("is_empty", arguments.Count, 1, 1, position);
        return ExpressionValue.From(
            arguments[0].IsNull
            || (arguments[0].Kind == ExpressionValueKind.String
                && arguments[0].StringValue(position).Length == 0));
    }

    private static ExpressionValue NullIfEmpty(IReadOnlyList<ExpressionValue> arguments, int position)
    {
        RequireArgumentCount("null_if_empty", arguments.Count, 1, 1, position);
        return arguments[0].Kind == ExpressionValueKind.String
               && arguments[0].StringValue(position).Length == 0
            ? ExpressionValue.Null
            : arguments[0];
    }

    private static ExpressionValue Round(IReadOnlyList<ExpressionValue> arguments, int position)
    {
        RequireArgumentCount("round", arguments.Count, 1, 2, position);
        var digits = arguments.Count == 2 ? ToInteger(arguments[1], position) : 0;
        if (digits is < 0 or > 15)
        {
            throw new ExpressionEvaluationException("Round digits must be between 0 and 15", position);
        }

        return ExpressionValue.From(Math.Round(arguments[0].NumberValue(position), digits));
    }

    private static ExpressionValue UnaryNumber(
        IReadOnlyList<ExpressionValue> arguments,
        int position,
        Func<double, double> operation)
    {
        RequireArgumentCount("function", arguments.Count, 1, 1, position);
        return ExpressionValue.From(operation(arguments[0].NumberValue(position)));
    }

    private static ExpressionValue MinMax(
        IReadOnlyList<ExpressionValue> arguments,
        int position,
        bool minimum)
    {
        RequireArgumentCount(minimum ? "min" : "max", arguments.Count, 1, Int32.MaxValue, position);
        var values = arguments.Select(argument => argument.NumberValue(position));
        return ExpressionValue.From(minimum ? values.Min() : values.Max());
    }

    private static ExpressionValue FormatDate(IReadOnlyList<ExpressionValue> arguments, int position)
    {
        RequireArgumentCount("format_date", arguments.Count, 2, 2, position);
        return ExpressionValue.From(
            ToDateTime(arguments[0], position).ToString(
                arguments[1].StringValue(position),
                CultureInfo.InvariantCulture));
    }

    private static ExpressionValue DatePart(
        IReadOnlyList<ExpressionValue> arguments,
        int position,
        Func<DateTime, int> selector)
    {
        RequireArgumentCount("date function", arguments.Count, 1, 1, position);
        return ExpressionValue.From(selector(ToDateTime(arguments[0], position)));
    }

    private static DateTime ToDateTime(ExpressionValue value, int position)
    {
        if (value.Kind == ExpressionValueKind.DateTime)
        {
            return value.DateTimeValue(position);
        }

        if (value.Kind == ExpressionValueKind.String)
        {
            var text = value.StringValue(position);
            if (DateTime.TryParse(
                    text,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind,
                    out var invariant)
                || DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out invariant))
            {
                return invariant;
            }
        }

        throw ExpressionEvaluationException.Type(position, "date", value.Kind);
    }

    private static int ToInteger(ExpressionValue value, int position)
    {
        var number = value.NumberValue(position);
        if (number != Math.Truncate(number) || number is < Int32.MinValue or > Int32.MaxValue)
        {
            throw new ExpressionEvaluationException("Expected an integer", position);
        }

        return (int)number;
    }

    private static void RequireArgumentCount(FunctionNode node, int minimum, int maximum)
        => RequireArgumentCount(node.Name, node.Arguments.Count, minimum, maximum, node.Position);

    private static void RequireArgumentCount(
        string name,
        int actual,
        int minimum,
        int maximum,
        int position)
    {
        if (actual < minimum || actual > maximum)
        {
            var expected = minimum == maximum
                ? minimum.ToString(CultureInfo.InvariantCulture)
                : maximum == Int32.MaxValue
                    ? $"at least {minimum}"
                    : $"{minimum} or {maximum}";
            throw new ExpressionEvaluationException(
                $"Function '{name}' expects {expected} argument(s), but got {actual}",
                position);
        }
    }

    private enum TokenKind
    {
        End,
        Number,
        String,
        Identifier,
        Field,
        LeftParen,
        RightParen,
        Comma,
        Plus,
        Minus,
        Star,
        Slash,
        Percent,
        Bang,
        BangEqual,
        EqualEqual,
        Less,
        LessEqual,
        Greater,
        GreaterEqual,
        AndAnd,
        OrOr
    }

    private sealed record Token(TokenKind Kind, string Text, int Position);

    private abstract record Node(int Position);
    private sealed record LiteralNode(ExpressionValue Value, int SourcePosition) : Node(SourcePosition);
    private sealed record FieldNode(string Name, int SourcePosition) : Node(SourcePosition);
    private sealed record UnaryNode(TokenKind Operator, Node Operand, int SourcePosition) : Node(SourcePosition);
    private sealed record BinaryNode(Node Left, TokenKind Operator, Node Right, int SourcePosition) : Node(SourcePosition);
    private sealed record FunctionNode(string Name, IReadOnlyList<Node> Arguments, int SourcePosition) : Node(SourcePosition);

    private sealed class Parser
    {
        private readonly Lexer _lexer;
        private Token _current;

        public Parser(string source)
        {
            _lexer = new Lexer(source);
            _current = _lexer.Next();
        }

        public Node Parse()
        {
            var expression = ParseOr();
            if (_current.Kind != TokenKind.End)
            {
                throw new ExpressionParseException($"Unexpected token '{_current.Text}'", _current.Position);
            }

            return expression;
        }

        private Node ParseOr()
        {
            var left = ParseAnd();
            while (_current.Kind == TokenKind.OrOr)
            {
                var token = Take();
                left = new BinaryNode(left, token.Kind, ParseAnd(), token.Position);
            }

            return left;
        }

        private Node ParseAnd()
        {
            var left = ParseEquality();
            while (_current.Kind == TokenKind.AndAnd)
            {
                var token = Take();
                left = new BinaryNode(left, token.Kind, ParseEquality(), token.Position);
            }

            return left;
        }

        private Node ParseEquality()
        {
            var left = ParseComparison();
            while (_current.Kind is TokenKind.EqualEqual or TokenKind.BangEqual)
            {
                var token = Take();
                left = new BinaryNode(left, token.Kind, ParseComparison(), token.Position);
            }

            return left;
        }

        private Node ParseComparison()
        {
            var left = ParseTerm();
            while (_current.Kind is TokenKind.Less or TokenKind.LessEqual
                   or TokenKind.Greater or TokenKind.GreaterEqual)
            {
                var token = Take();
                left = new BinaryNode(left, token.Kind, ParseTerm(), token.Position);
            }

            return left;
        }

        private Node ParseTerm()
        {
            var left = ParseFactor();
            while (_current.Kind is TokenKind.Plus or TokenKind.Minus)
            {
                var token = Take();
                left = new BinaryNode(left, token.Kind, ParseFactor(), token.Position);
            }

            return left;
        }

        private Node ParseFactor()
        {
            var left = ParseUnary();
            while (_current.Kind is TokenKind.Star or TokenKind.Slash or TokenKind.Percent)
            {
                var token = Take();
                left = new BinaryNode(left, token.Kind, ParseUnary(), token.Position);
            }

            return left;
        }

        private Node ParseUnary()
        {
            if (_current.Kind is TokenKind.Minus or TokenKind.Bang)
            {
                var token = Take();
                return new UnaryNode(token.Kind, ParseUnary(), token.Position);
            }

            return ParsePrimary();
        }

        private Node ParsePrimary()
        {
            var token = Take();
            switch (token.Kind)
            {
                case TokenKind.Number:
                    if (!Double.TryParse(
                            token.Text,
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out var number))
                    {
                        throw new ExpressionParseException(
                            $"Invalid number '{token.Text}'",
                            token.Position);
                    }

                    return new LiteralNode(ExpressionValue.From(number), token.Position);
                case TokenKind.String:
                    return new LiteralNode(ExpressionValue.From(token.Text), token.Position);
                case TokenKind.Field:
                    return new FieldNode(token.Text, token.Position);
                case TokenKind.Identifier:
                    if (token.Text.Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        return new LiteralNode(ExpressionValue.From(true), token.Position);
                    }
                    if (token.Text.Equals("false", StringComparison.OrdinalIgnoreCase))
                    {
                        return new LiteralNode(ExpressionValue.From(false), token.Position);
                    }
                    if (token.Text.Equals("null", StringComparison.OrdinalIgnoreCase))
                    {
                        return new LiteralNode(ExpressionValue.Null, token.Position);
                    }
                    return ParseFunction(token);
                case TokenKind.LeftParen:
                    var inner = ParseOr();
                    Expect(TokenKind.RightParen, "Expected ')'");
                    return inner;
                default:
                    throw new ExpressionParseException(
                        token.Kind == TokenKind.End
                            ? "Expected expression"
                            : $"Unexpected token '{token.Text}'",
                        token.Position);
            }
        }

        private Node ParseFunction(Token identifier)
        {
            Expect(TokenKind.LeftParen, $"Expected '(' after '{identifier.Text}'");
            var arguments = new List<Node>();
            if (_current.Kind != TokenKind.RightParen)
            {
                do
                {
                    arguments.Add(ParseOr());
                    if (_current.Kind != TokenKind.Comma)
                    {
                        break;
                    }
                    Take();
                }
                while (true);
            }

            Expect(TokenKind.RightParen, "Expected ')'");
            return new FunctionNode(identifier.Text, arguments, identifier.Position);
        }

        private Token Take()
        {
            var token = _current;
            _current = _lexer.Next();
            return token;
        }

        private void Expect(TokenKind kind, string message)
        {
            if (_current.Kind != kind)
            {
                throw new ExpressionParseException(message, _current.Position);
            }

            Take();
        }
    }

    private sealed class Lexer
    {
        private readonly string _source;
        private int _position;

        public Lexer(string source) => _source = source;

        public Token Next()
        {
            while (_position < _source.Length && Char.IsWhiteSpace(_source[_position]))
            {
                _position++;
            }

            if (_position >= _source.Length)
            {
                return new Token(TokenKind.End, String.Empty, _position);
            }

            var start = _position;
            var current = _source[_position++];
            if (Char.IsDigit(current) || current == '.' && IsNextDigit())
            {
                while (_position < _source.Length
                       && (Char.IsDigit(_source[_position]) || _source[_position] == '.'))
                {
                    _position++;
                }

                return new Token(TokenKind.Number, _source[start.._position], start);
            }

            if (Char.IsLetter(current) || current == '_')
            {
                while (_position < _source.Length
                       && (Char.IsLetterOrDigit(_source[_position]) || _source[_position] == '_'))
                {
                    _position++;
                }

                return new Token(TokenKind.Identifier, _source[start.._position], start);
            }

            if (current == '"')
            {
                return ReadString(start);
            }

            if (current == '[')
            {
                var end = _source.IndexOf(']', _position);
                if (end < 0)
                {
                    throw new ExpressionParseException("Unterminated field reference", start);
                }

                var field = _source[_position..end];
                _position = end + 1;
                return new Token(TokenKind.Field, field, start);
            }

            return current switch
            {
                '(' => new Token(TokenKind.LeftParen, "(", start),
                ')' => new Token(TokenKind.RightParen, ")", start),
                ',' => new Token(TokenKind.Comma, ",", start),
                '+' => new Token(TokenKind.Plus, "+", start),
                '-' => new Token(TokenKind.Minus, "-", start),
                '*' => new Token(TokenKind.Star, "*", start),
                '/' => new Token(TokenKind.Slash, "/", start),
                '%' => new Token(TokenKind.Percent, "%", start),
                '!' when Match('=') => new Token(TokenKind.BangEqual, "!=", start),
                '!' => new Token(TokenKind.Bang, "!", start),
                '=' when Match('=') => new Token(TokenKind.EqualEqual, "==", start),
                '<' when Match('=') => new Token(TokenKind.LessEqual, "<=", start),
                '<' => new Token(TokenKind.Less, "<", start),
                '>' when Match('=') => new Token(TokenKind.GreaterEqual, ">=", start),
                '>' => new Token(TokenKind.Greater, ">", start),
                '&' when Match('&') => new Token(TokenKind.AndAnd, "&&", start),
                '|' when Match('|') => new Token(TokenKind.OrOr, "||", start),
                _ => throw new ExpressionParseException($"Unexpected character '{current}'", start)
            };
        }

        private Token ReadString(int start)
        {
            var result = new System.Text.StringBuilder();
            while (_position < _source.Length)
            {
                var current = _source[_position++];
                if (current == '"')
                {
                    return new Token(TokenKind.String, result.ToString(), start);
                }

                if (current != '\\')
                {
                    result.Append(current);
                    continue;
                }

                if (_position >= _source.Length)
                {
                    break;
                }

                var escaped = _source[_position++];
                result.Append(escaped switch
                {
                    '"' => '"',
                    '\\' => '\\',
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    _ => throw new ExpressionParseException($"Unknown escape '\\{escaped}'", _position - 2)
                });
            }

            throw new ExpressionParseException("Unterminated string", start);
        }

        private bool IsNextDigit()
            => _position < _source.Length && Char.IsDigit(_source[_position]);

        private bool Match(char expected)
        {
            if (_position >= _source.Length || _source[_position] != expected)
            {
                return false;
            }

            _position++;
            return true;
        }
    }
}
