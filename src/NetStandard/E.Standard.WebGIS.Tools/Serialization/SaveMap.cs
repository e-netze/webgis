using E.Standard.Localization.Abstractions;
using E.Standard.Security.Core.Extensions;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.IO;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Api.UI.Setters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebGIS.Tools.Serialization;

[Export(typeof(IApiButton))]
[ToolStorageId("WebGIS.Tools.Serialization")]
[AdvancedToolProperties(AnonymousUserIdDependent = true, ClientDeviceDependent = true)]
[ToolConfigurationSection("savemap")]
[ToolHelp("tools/map/save.html")]
public class SaveMap : IApiServerButtonLocalizable<SaveMap>,
                       IApiButtonResources
{
    private const string CodeInputId = "webgis-save-map-resoration-code-input";
    private const string ConfigNameMaxLength = "name-maxlength";

    #region IApiServerButton Member

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<SaveMap> localizer)
    {
        var response = new ApiEventResponse()
            .AddUIElement(new UIDiv()
                .WithDialogTitle(localizer.Localize("name"))
                .AddChildren(
                    new UILabel()
                        .WithLabel($"{localizer.Localize("save-label")}:"),
                    new UIBreak(),
                    new UIInputAutocomplete(UIInputAutocomplete.MethodSource(bridge, this.GetType(), "autocomplete-maps"))
                        .WithId("serialization-save-mapname")
                        .AsToolParameter(),
                    new UIHidden()
                        .WithId("serialization-save-mapjson")
                        .AsToolParameter(UICss.AutoSetterMapSerialization),
                    new UIHidden()
                        .WithId("serialization-save-graphics-geojson")
                        .AsToolParameter(UICss.AutoSetterMapGraphicsGeoJson),
                    new UIButtonContainer(new UICallbackButton<SaveMap>(this, "save-map")
                        .WithText(localizer.Localize("save")))
                    ));

        if (bridge.CurrentUser.IsAnonymous)
        {
            Guid anonymousUserGuid;
            if (!bridge.CurrentUser.AnonymousUserId.HasValue)
            {
                anonymousUserGuid = bridge.AnonymousUserGuid(bridge.CreateNewAnoymousCliendsideUserId());
            }
            else
            {
                anonymousUserGuid = bridge.CurrentUser.AnonymousUserId.Value;
            }

            response.AddUIElements(
                new UIBreak(),
                new UILabel()
                    .WithLabel(localizer.Localize("recovery-code-label1:body")),
                new UIInputText()
                    .WithId(CodeInputId)
                    .WithValue(anonymousUserGuid.GuidToBase64())
                    .AsReadonly(),
                new UILabel()
                    .WithLabel(localizer.Localize("recovery-code-label2:body")),
                new UICallbackButton<SaveMap>(this, "restoration-code")
                    .WithText(localizer.Localize("enter-recovery-code"))
                    .WithStyles(UICss.CancelButtonStyle),
                new UIHidden()   // Immer übergeben -> braucht Autocomplete
                    .WithId("serialization-anonym-user-id")
                    .WithValue(bridge.CreateAnonymousClientSideUserId(anonymousUserGuid))
                    .AsToolParameter(UICss.ToolAutocompleteParameter, UICss.AutoSetterAnoymousUserId));
        }
        ;

        return response;
    }

    #endregion

    #region IApiButton Member

    public string Name => "Save Map";

    public string Container => "Map";

    public string Image => UIImageButton.ToolResourceImage(this, "save");

    public string ToolTip => "Save the current map.  ";

    public bool HasUI => true;

    #endregion

    #region IApiButtonResources Member

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("save", Properties.Resources.save);
    }

    #endregion

    #region Commands

    [ServerToolCommand("autocomplete-maps")]
    public ApiEventResponse OnAutocompleteMap(IBridge bridge, ApiToolEventArguments e)
    {
        List<string> values = new List<string>();

        if (bridge.CurrentUser.IsAnonymous)
        {
            string anonymousUserId = e["serialization-anonym-user-id"];
            if (!String.IsNullOrWhiteSpace(anonymousUserId))
            {
                bridge.SetAnonymousUserGuid(bridge.AnonymousUserGuid(anonymousUserId));
            }
        }

        if (!String.IsNullOrWhiteSpace(bridge.CurrentUser.Username))
        {
            string term = e["term"].ToLower();

            foreach (string name in bridge.Storage.GetNames(includeDirectories: false).Select(n => n.FromValidEncodedName()))
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

    [ServerToolCommand("save-map")]
    public ApiEventResponse OnSaveMap(IBridge bridge, ApiToolEventArguments e, ILocalizer<SaveMap> localizer)
    {
        //if (bridge.CurrentUser.IsAnonymous)
        //{
        //    string anonymousUserId = e["serialization-anonym-user-id"];
        //    if (!String.IsNullOrWhiteSpace(anonymousUserId))
        //    {
        //        bridge.SetAnonymousUserGuid(AnonymousUserGuid(anonymousUserId));
        //    }
        //    else
        //    {
        //        throw new Exception("Unauthorized");
        //    }
        //}

        if (bridge.CurrentUser.IsAnonymous && !bridge.CurrentUser.AnonymousUserId.HasValue)
        {
            throw new Exception("Unauthorized");
        }

        string name = e["serialization-save-mapname"];
        if (String.IsNullOrWhiteSpace(name))
        {
            throw new Exception(localizer.Localize("name-required"));
        }

        if (name.Length > e.GetConfigInt(ConfigNameMaxLength, 40))
        {
            throw new Exception($"{localizer.Localize("name-to-long")}: {e.GetConfigInt(ConfigNameMaxLength, 40)}");
        }

        if (!name.IsValidProjectName(out string invalidChars))
        {
            throw new Exception($"{localizer.Localize("name-invalid-char")}: {invalidChars}");
        }

        string encodedName = name.Trim().ToValidEncodedName();

        if (bridge.Storage.Exists(name))
        {
            // Older projekt names are not encoded...
            // Delete this files before store with new encoded name
            bridge.Storage.Remove(name);
            bridge.Storage.Remove($"{name}-graphics", StorageBlobType.Data);
        }

        bridge.Storage.Save(encodedName, e["serialization-save-mapjson"]);

        string geoJson = e["serialization-save-graphics-geojson"];
        if (!String.IsNullOrWhiteSpace(geoJson) && geoJson.Length > 2)
        {
            bridge.Storage.Save($"{encodedName}-graphics", geoJson, StorageBlobType.Data);
        }

        return new ApiEventResponse()
        {
            UIElements = new IUIElement[] {
                new UIEmpty(){
                    target=UIElementTarget.modaldialog.ToString(),
                }
            },
            RemoveSecondaryToolUI = e.UseSimpleToolsBehaviour()
        };
    }

    [ServerToolCommand("restoration-code")]
    public ApiEventResponse RestorationCode(IBridge bridge, ApiToolEventArguments e, ILocalizer<SaveMap> localizer)
    {
        return RestorationCode(bridge, e, this, localizer);
    }

    public ApiEventResponse RestorationCode(IBridge bridge, ApiToolEventArguments e, IApiButton callback, ILocalizer<SaveMap> localizer)
    {
        return new ApiEventResponse()
        {
            //ClientCommands=new ApiClientButtonCommand[] { ApiClientButtonCommand.}
            UIElements = new IUIElement[] {
                new UIDiv(){
                    target=UIElementTarget.modaldialog.ToString(),
                    targettitle=localizer.Localize("recovery-code"),
                    css = UICss.ToClass(new string[]{ UICss.NarrowFormMarginAuto }),
                    elements= new IUIElement[]
                    {
                        new UILabel()
                        {
                            label=$"{localizer.Localize("enter-recovery-code")}:"
                        },
                        new UIBreak(),
                        new UIInputText()
                        {
                            id="serialization-restoration-code",
                            css=UICss.ToClass(new string[]{ UICss.ToolParameter})
                        },
                        new UIBreak(),
                        new UILabel()
                        {
                            label = localizer.Localize("recovery-code-label3:body")
                        },
                        new UIBreak(),
                        new UIBreak(),
                        new UICallbackButton(callback ?? this ,"restoration-code-commit")
                        {
                            text = localizer.Localize("apply-recovery-code")
                        }
                    }
                }
            }
        };
    }

    public (ApiEventResponse response, Guid anonyomousUserGuid) HandleRestorationCodeCommit(IBridge bridge, ApiToolEventArguments e)
    {
        Guid anonyomousUserGuid = e["serialization-restoration-code"].Base64ToGuid();

        var response = new ApiEventResponse()
        {
            UIElements = new List<IUIElement>(new IUIElement[] {
                new UIEmpty(){
                    target=UIElementTarget.modaldialog.ToString(),
                }
            }),
            UISetters = new List<IUISetter>(new IUISetter[] { new UIAnonymousUserIdSetter(bridge, anonyomousUserGuid) })
        };

        return (response, anonyomousUserGuid);
    }

    [ServerToolCommand("restoration-code-commit")]
    public ApiEventResponse RestorationCodeCommit(IBridge bridge, ApiToolEventArguments e)
    {
        var handleResult = HandleRestorationCodeCommit(bridge, e);

        handleResult.response.UISetters.Add(new UISetter(CodeInputId, handleResult.anonyomousUserGuid.GuidToBase64()));

        return handleResult.response;
    }

    #endregion
}
