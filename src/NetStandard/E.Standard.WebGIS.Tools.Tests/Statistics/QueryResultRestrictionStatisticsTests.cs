using System.Globalization;
using E.Standard.Localization.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;
using E.Standard.WebMapping.Core.Exceptions;
using E.Standard.WebMapping.Core.Models;
using Tool = E.Standard.WebGIS.Tools.QueryResultRestriction;

namespace E.Standard.WebGIS.Tools.Tests.Statistics;

public class QueryResultRestrictionStatisticsTests
{
    private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("de-AT");

    private class KeyLocalizer : ILocalizer<Tool>
    {
        public string Localize(string key, ILocalizer.LocalizeMode mode = ILocalizer.LocalizeMode.NamespaceWithFallbackToKey, ILocalizer.LocalizerDefaultValue defaultValue = ILocalizer.LocalizerDefaultValue.OriginalKey)
            => key;

        public ILocalizer<TClass> CreateFor<TClass>() => throw new NotSupportedException();
    }

    private static LayerStatisticsGroup Group(object? value, long count) => new() { Value = value!, Count = count };

    private static IReadOnlyList<Tool.StatisticsGroupRow> Rows(bool sortDescending, params LayerStatisticsGroup[] groups)
        => Tool.StatisticsGroupRows(groups, sortDescending, g => g.Value?.ToString() ?? "(null)", Culture);

    #region Ranking / sorting

    [Fact]
    public void GroupRows_Descending_UsesCompetitionRanking()
    {
        var rows = Rows(true, Group("a", 5), Group("b", 3), Group("c", 3), Group("d", 1));

        Assert.Equal(new[] { "a", "b", "c", "d" }, rows.Select(r => r.Text));
        Assert.Equal(new[] { 1, 2, 3, 4 }, rows.Select(r => r.Index));
        Assert.Equal(new[] { 1, 2, 2, 4 }, rows.Select(r => r.Rank));
        Assert.Equal(new[] { 1, 2, 2, 1 }, rows.Select(r => r.RankSize));
        Assert.Equal(new[] { false, false, true, false }, rows.Select(r => r.InheritedRank));
    }

    [Fact]
    public void GroupRows_Ascending_KeepsRankByFrequency()
    {
        var rows = Rows(false, Group("a", 5), Group("b", 3), Group("c", 3), Group("d", 1));

        Assert.Equal(new[] { "d", "b", "c", "a" }, rows.Select(r => r.Text));
        Assert.Equal(new[] { 1, 2, 3, 4 }, rows.Select(r => r.Index));
        Assert.Equal(new[] { 4, 2, 2, 1 }, rows.Select(r => r.Rank));
        Assert.Equal(new[] { false, false, true, false }, rows.Select(r => r.InheritedRank));
    }

    [Fact]
    public void GroupRows_SameCount_SortedByText()
    {
        var rows = Rows(true, Group("Zirbe", 2), Group("ahorn", 2), Group("Buche", 2));

        Assert.Equal(new[] { "ahorn", "Buche", "Zirbe" }, rows.Select(r => r.Text));
        Assert.All(rows, r => Assert.Equal(1, r.Rank));
        Assert.All(rows, r => Assert.Equal(3, r.RankSize));
        Assert.Equal(new[] { false, true, true }, rows.Select(r => r.InheritedRank));
    }

    [Fact]
    public void GroupRows_Empty_ReturnsEmpty()
        => Assert.Empty(Rows(true));

    #endregion

    #region Conditions

    [Fact]
    public void GroupCondition_String_IsQuotedAndEscaped()
        => Assert.Equal("NAME='O''Reilly'", Tool.StatisticsGroupCondition("NAME", "\"O'Reilly\""));

    [Fact]
    public void GroupCondition_Number_IsInvariant()
        => Assert.Equal("BAUJAHR=1985.5", Tool.StatisticsGroupCondition("BAUJAHR", "1985.5"));

    [Fact]
    public void GroupCondition_Null_IsNullCondition()
        => Assert.Equal($"NAME{UIQueryBuilder.OperatorIsNull}", Tool.StatisticsGroupCondition("NAME", "null"));

    [Fact]
    public void RankCondition_Strings_CreatesInList()
        => Assert.Equal("GNR in ('1','O''R')",
            Tool.StatisticsRankCondition("GNR", new[] { Group("1", 2), Group("O'R", 2) }, new KeyLocalizer()));

    [Fact]
    public void RankCondition_Numbers_CreatesInList()
        => Assert.Equal("NS in (41,42.5)",
            Tool.StatisticsRankCondition("NS", new[] { Group(41L, 2), Group(42.5D, 2) }, new KeyLocalizer()));

    [Fact]
    public void RankCondition_SingleValue_CreatesEquality()
        => Assert.Equal("GNR='1'", Tool.StatisticsRankCondition("GNR", new[] { Group("1", 2) }, new KeyLocalizer()));

    [Fact]
    public void RankCondition_CanBeParsedByQueryBuilder()
    {
        var queryBuilder = new UIQueryBuilder();
        queryBuilder.TrySetWhereClause(Tool.CombineRestriction("KG='63101'",
            Tool.StatisticsRankCondition("GNR", new[] { Group("1", 2), Group("12", 2) }, new KeyLocalizer())));

        Assert.NotNull(queryBuilder.WhereClauseParts);
        Assert.Contains(UIQueryBuilder.OperatorIn, queryBuilder.WhereClauseParts!);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("a,b")]
    public void RankCondition_NotExpressible_Throws(string? specialValue)
        => Assert.Throws<InfoException>(() =>
            Tool.StatisticsRankCondition("GNR", new[] { Group("1", 2), Group(specialValue, 2) }, new KeyLocalizer()));

    [Fact]
    public void RankCondition_TooManyValues_Throws()
        => Assert.Throws<InfoException>(() =>
            Tool.StatisticsRankCondition("GNR", Enumerable.Range(0, 1001).Select(i => Group(i.ToString(), 1)), new KeyLocalizer()));

    [Fact]
    public void CombineRestriction_Empty_ReturnsCondition()
        => Assert.Equal("A=1", Tool.CombineRestriction("", "A=1"));

    [Fact]
    public void CombineRestriction_AppendsToEveryOrTerm()
        => Assert.Equal("A=1 and C=3 or B=2 and C=3", Tool.CombineRestriction("A=1 or B=2", "C=3"));

    [Fact]
    public void CombineRestriction_IgnoresOrInsideStrings()
        => Assert.Equal("NAME='x or y' and C=3", Tool.CombineRestriction("NAME='x or y'", "C=3"));

    #endregion

    #region CSV

    [Fact]
    public void Csv_SeparatesBySemicolon()
        => Assert.Equal("a;b\r\n1;2\r\n", Tool.StatisticsCsv(new[] { new[] { "a", "b" }, new[] { "1", "2" } }));

    [Fact]
    public void Csv_QuotesSpecialCharacters()
        => Assert.Equal("\"a;b\";\"say \"\"hi\"\"\";\"x\ny\"\r\n", Tool.StatisticsCsv(new[] { new[] { "a;b", "say \"hi\"", "x\ny" } }));

    [Fact]
    public void Csv_PreventsFormulaInjection_ButKeepsNegativeNumbers()
        => Assert.Equal("'=SUM(A1);-5;-1,5;'@x\r\n", Tool.StatisticsCsv(new[] { new[] { "=SUM(A1)", "-5", "-1,5", "@x" } }));

    #endregion
}
