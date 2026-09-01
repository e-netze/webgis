using System.Xml;

using E.Standard.WebGIS.Tools.Editing.Extensions;

using static E.Standard.WebGIS.Tools.Editing.Environment.EditEnvironment;

namespace E.Standard.WebGIS.Tools.Tests.Editing.Extensions;

public class EditThemeExtensionsTests
{
    private const string EditNamespace = "http://www.e-steiermark.com/webgis/edit";
    private const string WebGisNamespace = "http://www.e-steiermark.com/webgis";

    /// <summary>
    /// Builds an <see cref="EditTheme"/> instance directly from a snippet of
    /// "edit:commit_action" xml elements, mirroring the real cms xml structure
    /// (editthemes/edit:edittheme/edit:mask/edit:commit_actions/edit:commit_action).
    /// </summary>
    private static EditTheme BuildEditTheme(string commitActionsXml)
    {
        string xml = $"""
            <editthemes xmlns:edit="{EditNamespace}" xmlns:webgis="{WebGisNamespace}">
              <edit:edittheme id="theme1">
                <edit:mask>
                  <edit:commit_actions>
                    {commitActionsXml}
                  </edit:commit_actions>
                </edit:mask>
              </edit:edittheme>
            </editthemes>
            """;

        var doc = new XmlDocument();
        doc.LoadXml(xml);

        var ns = new XmlNamespaceManager(doc.NameTable);
        ns.AddNamespace("webgis", WebGisNamespace);
        ns.AddNamespace("edit", EditNamespace);

        var themeNode = doc.SelectSingleNode("editthemes/edit:edittheme", ns);

        return new EditTheme(null!, themeNode, ns);
    }

    private static string CommitAction(string timing, string? successMessage = null, string name = "action") =>
        $"""<edit:commit_action name="{name}" target="http://example.com" timing="{timing}" protocol="0" {(successMessage != null ? $@"success_message=""{successMessage}""" : "")} />""";

    [Fact]
    public void NoCommitActionsNode_ReturnsFalse()
    {
        var editTheme = BuildEditTheme("");

        Assert.False(editTheme.HasCommitSuccessMessagePlaceholders(EditFeatureCommand.Delete));
    }

    [Fact]
    public void DeleteAction_WithPlaceholder_ReturnsTrue()
    {
        // timing=4 -> Before_Delete
        var editTheme = BuildEditTheme(CommitAction("4", "Feature [NAME] deleted"));

        Assert.True(editTheme.HasCommitSuccessMessagePlaceholders(EditFeatureCommand.Delete));
    }

    [Fact]
    public void DeleteAction_WithoutPlaceholder_ReturnsFalse()
    {
        var editTheme = BuildEditTheme(CommitAction("4", "Feature deleted"));

        Assert.False(editTheme.HasCommitSuccessMessagePlaceholders(EditFeatureCommand.Delete));
    }

    [Fact]
    public void DeleteAction_WithoutSuccessMessage_ReturnsFalse()
    {
        var editTheme = BuildEditTheme(CommitAction("4"));

        Assert.False(editTheme.HasCommitSuccessMessagePlaceholders(EditFeatureCommand.Delete));
    }

    [Fact]
    public void PlaceholderOnlyInUnrelatedTiming_ReturnsFalse()
    {
        // timing=0 -> Before_Insert, irrelevant for Delete command
        var editTheme = BuildEditTheme(CommitAction("0", "Feature [NAME] inserted"));

        Assert.False(editTheme.HasCommitSuccessMessagePlaceholders(EditFeatureCommand.Delete));
    }

    [Theory]
    [InlineData("4")] // Before_Delete
    [InlineData("5")] // After_Delete
    public void PlaceholderInBeforeOrAfterDelete_ReturnsTrue(string timing)
    {
        var editTheme = BuildEditTheme(CommitAction(timing, "Feature [NAME] deleted"));

        Assert.True(editTheme.HasCommitSuccessMessagePlaceholders(EditFeatureCommand.Delete));
    }

    [Fact]
    public void MultipleActions_OnlyOneWithPlaceholder_ReturnsTrue()
    {
        var editTheme = BuildEditTheme(
            CommitAction("4", "no placeholder here", name: "a1") + "\n" +
            CommitAction("5", "Feature [OID] deleted", name: "a2"));

        Assert.True(editTheme.HasCommitSuccessMessagePlaceholders(EditFeatureCommand.Delete));
    }

    [Theory]
    [InlineData("2")] // Before_Update
    [InlineData("3")] // After_Update
    public void UpdateAndMassAttribution_ShareTiming(string timing)
    {
        var editTheme = BuildEditTheme(CommitAction(timing, "Feature [NAME] updated"));

        Assert.True(editTheme.HasCommitSuccessMessagePlaceholders(EditFeatureCommand.Update));
        Assert.True(editTheme.HasCommitSuccessMessagePlaceholders(EditFeatureCommand.MassAttribution));
        Assert.False(editTheme.HasCommitSuccessMessagePlaceholders(EditFeatureCommand.Delete));
    }

    [Theory]
    [InlineData("0")] // Before_Insert
    [InlineData("1")] // After_Insert
    public void InsertAndTransfer_ShareTiming(string timing)
    {
        var editTheme = BuildEditTheme(CommitAction(timing, "Feature [NAME] inserted"));

        Assert.True(editTheme.HasCommitSuccessMessagePlaceholders(EditFeatureCommand.Insert));
        Assert.True(editTheme.HasCommitSuccessMessagePlaceholders(EditFeatureCommand.Transfer));
        Assert.False(editTheme.HasCommitSuccessMessagePlaceholders(EditFeatureCommand.Update));
    }

    [Fact]
    public void NullEditTheme_ReturnsFalse()
    {
        EditTheme? editTheme = null;

        Assert.False(editTheme.HasCommitSuccessMessagePlaceholders(EditFeatureCommand.Delete));
    }
}
