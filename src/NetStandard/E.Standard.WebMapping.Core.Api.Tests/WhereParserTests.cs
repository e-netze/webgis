using E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;

namespace E.Standard.WebMapping.Core.Api.Tests;

public class WhereParserTests
{
    private static void AssertTokens(string where, string[] expected)
    {
        var p = new UIQueryBuilder();
        p.TrySetWhereClause(where);
        Assert.Equal(expected, p.WhereClauseParts);
    }

    [Fact]
    public void ReturnsEmptyArray_OnNullOrWhitespace()
    {
        AssertTokens(null!, null!);
        AssertTokens("", null!);
        AssertTokens("   ", null!);
    }

    [Fact]
    public void Parses_SimpleEquality_And_Number()
    {
        // KG='63101' and NS=41 => ["KG", "=", "63101", "and", "NS", "=", "41" ]
        AssertTokens(
            "KG='63101' and NS=41",
            new[] { "KG", "=", "63101", "and", "NS", "=", "41" }
        );
    }

    [Fact]
    public void Parses_InList_AsSingleToken()
    {
        // KG='63101' and NS in (41,42) => ["KG","=","63101","and","NS"," in ","41,42"]
        AssertTokens(
            "KG='63101' and NS in (41,42)",
            new[] { "KG", "=", "63101", "and", "NS", " in ", "41,42" }
        );
    }

    [Fact]
    public void Parses_Like_WithWildcard()
    {
        // KG like '6%' => ["KG", " like ", "6%"]
        AssertTokens(
            "KG like '6%'",
            new[] { "KG", " like ", "6%" }
        );
    }

    [Fact]
    public void Normalizes_LogicalOperators_ToLower()
    {
        AssertTokens(
            "A=1 OR B=2 aNd C=3",
            new[] { "A", "=", "1", "or", "B", "=", "2", "and", "C", "=", "3" }
        );
    }

    [Fact]
    public void Normalizes_Like_And_In_WithSpacesAndLower()
    {
        AssertTokens(
            "col LIKE 'x%' AND id IN (1,2,3)",
            new[] { "col", " like ", "x%", "and", "id", " in ", "1,2,3" }
        );
    }

    [Fact]
    public void Handles_EscapedSingleQuotes_InStringLiteral()
    {
        // '' inside string should become a single '
        AssertTokens(
            "Name like 'O''Reilly%'",
            new[] { "Name", " like ", "O'Reilly%" }
        );
    }

    [Fact]
    public void Parses_AllComparisonOperators()
    {
        AssertTokens(
            "a>=1 and b<=2 and c<>3 and d>4 and e<5 and f=6",
            new[]
            {
                "a", ">=", "1", "and",
                "b", "<=", "2", "and",
                "c", "<>", "3", "and",
                "d", ">",  "4", "and",
                "e", "<",  "5", "and",
                "f", "=",  "6"
            }
        );
    }

    [Fact]
    public void Parses_DecimalNumbers()
    {
        AssertTokens(
            "price>=10.5 and discount<0.25",
            new[] { "price", ">=", "10.5", "and", "discount", "<", "0.25" }
        );
    }

    [Fact]
    public void Ignores_ExtraWhitespace()
    {
        AssertTokens(
            "   KG   like    '6%'   ",
            new[] { "KG", " like ", "6%" }
        );
    }

    [Fact]
    public void Trims_ParensContent_OnlyAtEnds()
    {
        // internal spaces kept as-is after Trim()
        AssertTokens(
            "id in (  1 , 2  )",
            new[] { "id", " in ", "1,2" });

        AssertTokens(
            "id in (  '1' , '2'  )",
            new[] { "id", " in ", "1,2" });
    }

    [Fact]
    public void CaseInsensitive_Keywords_AreRecognized()
    {
        AssertTokens(
            "field LiKe 'ABC%' Or code In (10,20)",
            new[] { "field", " like ", "ABC%", "or", "code", " in ", "10,20" }
        );
        AssertTokens(
            "field LiKe 'ABC%' Or code In ('10','20')",
            new[] { "field", " like ", "ABC%", "or", "code", " in ", "10,20" }
        );
    }
}
