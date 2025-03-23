using E.Standard.Json;
using E.Standard.Localization.Abstractions;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebGIS.Tools.Serialization.Extensions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using System;
using System.Collections.Generic;

namespace E.Standard.WebGIS.Tools.Serialization;

[Export(typeof(IApiButton))]
[ToolStorageId("WebGIS.Tools.Serialization/{user}/_bookmarks")]
[ToolStorageIsolatedUser(isUserIsolated: true)]
[AdvancedToolProperties(MapBBoxDependent = true, ScaleDependent = true, ClientDeviceDependent = true)]
public class Bookmarks : IApiButton, 
                         IApiServerButtonLocalizable<Bookmarks>, 
                         IApiButtonResources
{
    const string BookmarkNameTextInputId = "webgis-bookmarks-bookmark-name";
    const string BookmarkDescriptionInputId = "webgis-bookmarks-bookmark-description";
    const string ButtonId = "webgis.tools.serialization.bookmarks";

    #region IApiButton

    public string Name => "Bookmarks";

    public string Container => "Navigation";

    public string Image => UIImageButton.ToolResourceImage(this, "bookmark");

    public string ToolTip => "Bookmarks for custom geographic extents.";

    public bool HasUI => true;

    public ToolType Type => ToolType.none;

    public ToolCursor Cursor => ToolCursor.Pointer;

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<Bookmarks> localizer)
    {
        bridge.CurrentUser.ThrowIfAnonymous(localizer);

        var bookmarkNames = bridge.Storage.GetNames();
        var uiMenu = new UIMenu()
                        .WithHeader(localizer.Localize("my-bookmarks"));

        uiMenu.AddChild(
            new UIMenuItem(this, e, UIButton.UIButtonType.servertoolcommand_ext, command: "new-bookmark")
                .WithId(ButtonId)
                .WithText(localizer.Localize("create-bookmark"))
                .WithSubText(localizer.Localize("create-or-replace-bookmark"))
                .WithValue("_new"));

        foreach (var bookmarkName in bookmarkNames)
        {
            try
            {
                var bookmark = JSerializer.Deserialize<BookmarkModel>(bridge.Storage.LoadString(bookmarkName));

                uiMenu.AddChild(new UIMenuItem(this, e, UIButton.UIButtonType.servertoolcommand_ext, command: "bookmark")
                    .WithId(ButtonId)
                    .WithText(bookmarkName.FromValidEncodedName())
                    .WithSubText(bookmark.Description)
                    .WithValue(bookmarkName)
                    .AsRemovable());
            }
            catch { }
        }

        return new ApiEventResponse()
            .AddUIElement(
                new UIDiv()
                    .AddChild(uiMenu));
    }

    public ApiEventResponse OnEvent(IBridge bridge, ApiToolEventArguments e)
    {
        return null;
    }

    #endregion

    #region IApiButtonResources Member

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("bookmark", Properties.Resources.bookmark_26);
    }

    #endregion

    #region Commands

    [ServerToolCommand("autocomplete-bookmarks")]
    public ApiEventResponse OnAutocompleteMap(IBridge bridge, ApiToolEventArguments e)
    {
        List<string> values = new List<string>();

        if (!bridge.CurrentUser.IsAnonymous)
        {
            string term = e["term"].ToLower();

            foreach (string name in bridge.Storage.GetNames().FromValidEncodedNames())
            {
                if (name.ToLower().Contains(term))
                {
                    values.Add(name);
                }
            }

            values.Sort();
        }

        return new ApiRawJsonEventResponse(values.ToArray());
    }

    [ServerToolCommand("new-bookmark")]
    public ApiEventResponse OnNewBookmark(IBridge bridge, ApiToolEventArguments e, ILocalizer<Bookmarks> localizer)
    {
        bridge.CurrentUser.ThrowIfAnonymous(localizer);

        return new ApiEventResponse()
            .AddUIElement(
                new UIDiv()
                    .AsDialog()
                    .WithDialogTitle(localizer.Localize("create-bookmark"))
                    .WithStyles(UICss.NarrowFormMarginAuto)
                    .AddChildren(
                        new UILabel()
                            .WithLabel(localizer.Localize("name")),
                        new UIInputAutocomplete(UIInputAutocomplete.MethodSource(bridge, this.GetType(), "autocomplete-bookmarks"))
                            .WithId(BookmarkNameTextInputId)
                            .AsToolParameter(),
                        new UILabel()
                            .WithLabel(localizer.Localize("description")),
                        new UIInputTextArea()
                            .WithId(BookmarkDescriptionInputId)
                            .AsToolParameter(),
                        new UIButton(UIButton.UIButtonType.servertoolcommand_ext, "set-bookmark")
                            .WithId(ButtonId)
                            .WithStyles(UICss.DefaultButtonStyle)
                            .WithText(localizer.Localize("add-or-replace"))));
    }

    [ServerToolCommand("set-bookmark")]
    public ApiEventResponse OnSetBookmark(IBridge bridge, ApiToolEventArguments e, ILocalizer<Bookmarks> localizer)
    {
        bridge.CurrentUser.ThrowIfAnonymous(localizer);

        string name = e[BookmarkNameTextInputId],
           description = e[BookmarkDescriptionInputId];

        if (String.IsNullOrEmpty(name))
        {
            throw new Exception(localizer.Localize("exception-name-required"));
        }

        var bookmark = new BookmarkModel()
        {
            Description = description,
            Scale = e.MapScale.HasValue ? e.MapScale.Value : 0,
            BBox = e.MapBBox()
        };

        if (bookmark.BBox == null || bookmark.BBox.Length != 4)
        {
            throw new Exception("Internal Error: Invalid Bounding Box!");
        }

        bridge.Storage.Save(name.ToValidEncodedName(), JSerializer.Serialize(bookmark));

        return OnButtonClick(bridge, e, localizer)
            .CloseUIDialog()
            .SetActiveTool(this);
    }

    [ServerToolCommand("bookmark")]
    public ApiEventResponse OnZoomToBookmark(IBridge bridge, ApiToolEventArguments e, ILocalizer<Bookmarks> localizer)
    {
        bridge.CurrentUser.ThrowIfAnonymous(localizer);

        var bookmarkName = e["menuitem-value"];
        var command = e["menuitem-item-command"];

        ApiEventResponse apiResponse = null;

        switch (command)
        {
            case "remove-element":
                bridge.Storage.Remove(bookmarkName);
                break;
            default:
                var bookmark = JSerializer.Deserialize<BookmarkModel>(bridge.Storage.LoadString(bookmarkName));

                apiResponse = new ApiEventResponse()
                    .SetMapBbox4326(bookmark.BBox)
                    .SetMapScale(bookmark.Scale);

                break;
        }

        if (e.UseSimpleToolsBehaviour() && apiResponse != null)
        {
            apiResponse.RemoveSecondaryToolUI = true;
        }

        return apiResponse;
    }

    #endregion

    #region Models

    class BookmarkModel
    {
        public string Description { get; set; }
        public double Scale { get; set; }
        public double[] BBox { get; set; }
    }

    #endregion
}
