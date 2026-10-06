using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace E.Standard.WebGIS.CMS.Expressions;

public sealed record RegexPlaygroundGroup(string Name, bool Success, string Value);

public sealed record RegexPlaygroundMatch(
    int Index,
    int Length,
    string Value,
    IReadOnlyList<RegexPlaygroundGroup> Groups,
    bool GroupsTruncated);

public sealed class RegexPlaygroundRequest
{
    public bool ValidationMode { get; set; }
    public string Pattern { get; set; } = String.Empty;
    public string Input { get; set; } = String.Empty;
    public string Replacement { get; set; } = String.Empty;
    public bool IgnoreCase { get; set; }
    public bool Multiline { get; set; }
    public bool Singleline { get; set; }
    public bool PreviewReplacement { get; set; }
}

public sealed record RegexValidationResult(string Input, bool Valid);
public sealed record RegexValidationExample(string Id, string Pattern, string Input);

public sealed record RegexPlaygroundResult(
    int MatchCount,
    bool MatchesTruncated,
    IReadOnlyList<RegexPlaygroundMatch> Matches,
    bool ReplacementPreviewEnabled,
    string ReplacementPreview,
    bool ReplacementTruncated,
    IReadOnlyList<RegexValidationResult> Validations);

public static class RegexPlaygroundService
{
    public static IReadOnlyList<RegexValidationExample> Examples { get; } =
    [
        new("username", @"^[A-Za-z][A-Za-z0-9_.-]{2,31}$", "ada.lovelace\nuser_42\nab\n42user\nuser name"),
        new("email", @"^[^\s@]+@[^\s@.]+(?:\.[^\s@.]+)+$", "ada@example.org\nuser+tag@example.co.at\nnot-an-email\nuser@\nuser@example"),
        new("number", @"^[+-]?[0-9]+(?:[.,][0-9]+)?$", "42\n-12\n+3,14\n3.14\n12abc\n1.234,56"),
        new("kg", @"^[0-9]{5}$", "01001\n60101\n1234\n123456\nABCDE"),
        new("kg_styria", @"^6[0-9]{4}$", "60101\n69999\n01001\n6010\n601010"),
        new("parcel", @"^\.?[0-9]{1,5}(?:/[0-9]{1,5})?$", "123\n123/4\n.123\n.123/4\n123/\n123/4/5\nABC"),
        new("postcode", @"^[1-9][0-9]{3}$", "1010\n8010\n0100\n123\n12345")
    ];

    public const int MaxPatternLength = 2048;
    public const int MaxInputLength = 10_000;
    public const int MaxReplacementLength = 2048;

    private const int MaxReturnedMatches = 100;
    private const int MaxReturnedGroups = 100;
    private const int MaxReplacementPreviewLength = 100_000;
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan TotalExecutionLimit = TimeSpan.FromSeconds(2);

    public static RegexPlaygroundResult Evaluate(RegexPlaygroundRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validate(request);

        var options = RegexOptions.None;
        if (request.IgnoreCase)
        {
            options |= RegexOptions.IgnoreCase;
        }
        if (request.Multiline)
        {
            options |= RegexOptions.Multiline;
        }
        if (request.Singleline)
        {
            options |= RegexOptions.Singleline;
        }

        var regex = new Regex(request.Pattern, options, RegexTimeout);
        var groupNames = regex.GetGroupNames();
        var timer = Stopwatch.StartNew();
        if (request.ValidationMode)
        {
            var inputs = request.Input.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
            if (inputs.Length > 100)
            {
                throw new ArgumentException("No more than 100 test inputs can be validated.");
            }
            var validations = new List<RegexValidationResult>();
            foreach (var input in inputs)
            {
                CheckExecutionTime(timer);
                validations.Add(new(input, regex.IsMatch(input)));
            }
            return new(0, false, Array.Empty<RegexPlaygroundMatch>(), false,
                String.Empty, false, validations);
        }
        var matches = new List<RegexPlaygroundMatch>();
        var match = regex.Match(request.Input);
        var matchesTruncated = false;
        while (match.Success)
        {
            CheckExecutionTime(timer);
            if (matches.Count >= MaxReturnedMatches)
            {
                matchesTruncated = true;
                break;
            }

            matches.Add(new RegexPlaygroundMatch(
                match.Index,
                match.Length,
                match.Value,
                groupNames
                    .Take(MaxReturnedGroups)
                    .Select(name =>
                    {
                        var group = match.Groups[name];
                        return new RegexPlaygroundGroup(name, group.Success, group.Value);
                    })
                    .ToArray(),
                groupNames.Length > MaxReturnedGroups));
            match = match.NextMatch();
        }

        var replacementPreview = String.Empty;
        var replacementTruncated = false;
        if (request.PreviewReplacement)
        {
            (replacementPreview, replacementTruncated) = CreateReplacementPreview(
                regex,
                request.Input,
                request.Replacement,
                timer);
        }

        return new RegexPlaygroundResult(
            matches.Count,
            matchesTruncated,
            matches,
            request.PreviewReplacement,
            replacementPreview,
            replacementTruncated,
            Array.Empty<RegexValidationResult>());
    }

    private static void Validate(RegexPlaygroundRequest request)
    {
        if (String.IsNullOrEmpty(request.Pattern))
        {
            throw new ArgumentException("A regular expression pattern is required.", nameof(request));
        }
        if (request.Pattern.Length > MaxPatternLength)
        {
            throw new ArgumentException($"The pattern must not exceed {MaxPatternLength} characters.", nameof(request));
        }
        if (request.Input is null || request.Input.Length > MaxInputLength)
        {
            throw new ArgumentException($"The test text must not exceed {MaxInputLength} characters.", nameof(request));
        }
        if (request.Replacement is null || request.Replacement.Length > MaxReplacementLength)
        {
            throw new ArgumentException($"The replacement must not exceed {MaxReplacementLength} characters.", nameof(request));
        }
    }

    private static (string Preview, bool Truncated) CreateReplacementPreview(
        Regex regex,
        string input,
        string replacement,
        Stopwatch timer)
    {
        var output = new StringBuilder(Math.Min(input.Length, MaxReplacementPreviewLength));
        var match = regex.Match(input);
        var copiedThrough = 0;
        while (match.Success)
        {
            CheckExecutionTime(timer);
            if (!AppendLimited(output, input.AsSpan(copiedThrough, match.Index - copiedThrough))
                || !AppendLimited(output, match.Result(replacement)))
            {
                return (output.ToString(), true);
            }

            copiedThrough = match.Index + match.Length;
            match = match.NextMatch();
        }

        if (!AppendLimited(output, input.AsSpan(copiedThrough)))
        {
            return (output.ToString(), true);
        }

        return (output.ToString(), false);
    }

    private static bool AppendLimited(StringBuilder output, ReadOnlySpan<char> value)
    {
        var remaining = MaxReplacementPreviewLength - output.Length;
        if (value.Length <= remaining)
        {
            output.Append(value);
            return true;
        }

        output.Append(value[..remaining]);
        return false;
    }

    private static bool AppendLimited(StringBuilder output, string value)
        => AppendLimited(output, value.AsSpan());

    private static void CheckExecutionTime(Stopwatch timer)
    {
        if (timer.Elapsed > TotalExecutionLimit)
        {
            throw new TimeoutException("Regex evaluation exceeded the total time limit.");
        }
    }
}
