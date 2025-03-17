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
public class SaveMap : IApiServerButton, IApiButtonResources
{
    private const string CodeInputId = "webgis-save-map-resoration-code-input";
    private const string ConfigNameMaxLength = "name-maxlength";

    #region IApiServerButton Member

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        var response = new ApiEventResponse()
            .AddUIElement(new UIDiv()
                .WithDialogTitle("Karte speichern")
                .AddChildren(
                    new UILabel()
                        .WithLabel("Karten-/Projektnamen"),
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
                    new UIButtonContainer(new UICallbackButton(this, "save-map")
                        .WithText("Karte speichern..."))
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
                    .WithLabel(bridge.GetCustomTextBlock(this, "label1", "Sie sind auf dieser Seite als anonymer Anwender. Um sicher zu gehen, dass sie auf ihre Projekte auch später noch bzw. von einem anderen Gerät aus zugreifen können, speichern sie diesen Wiederherstellungscode:")),
                new UIInputText()
                    .WithId(CodeInputId)
                    .WithValue(anonymousUserGuid.GuidToBase64())
                    .AsReadonly(),
                new UILabel()
                    .WithLabel(bridge.GetCustomTextBlock(this, "label2", "Falls sie bereits einen anderen Wiederherstellungscode besitzen (von einem anderen Gerät oder aus einer älteren Sitzung), geben sie diesen hier ein:")),
                new UICallbackButton(this, "restoration-code")
                    .WithText("Wiederherstellungscode eingeben")
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

    public string Name => "Karte speichern";

    public string Container => "Karte";

    public string Image => UIImageButton.ToolResourceImage(this, "save");

    public string ToolTip => "Karte speichern";

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
    public ApiEventResponse OnSaveMap(IBridge bridge, ApiToolEventArguments e)
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
            throw new Exception("Die Eingabe eines Karten-/Projektnamens ist erforderlich!");
        }

        if (name.Length > e.GetConfigInt(ConfigNameMaxLength, 40))
        {
            throw new Exception($"Name zu lang. Maximal {e.GetConfigInt(ConfigNameMaxLength, 40)} Zeichen erlaubt");
        }

        if (!name.IsValidProjectName(out string invalidChars))
        {
            throw new Exception($"Ungültiges Zeichen im Namen. Vermeinden Sie folgende Zeichen: {invalidChars}");
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
    public ApiEventResponse RestorationCode(IBridge bridge, ApiToolEventArguments e)
    {
        return RestorationCode(bridge, e, this);
    }

    public ApiEventResponse RestorationCode(IBridge bridge, ApiToolEventArguments e, IApiServerButton callback)
    {
        return new ApiEventResponse()
        {
            //ClientCommands=new ApiClientButtonCommand[] { ApiClientButtonCommand.}
            UIElements = new IUIElement[] {
                new UIDiv(){
                    target=UIElementTarget.modaldialog.ToString(),
                    targettitle="Wiederherstellungscode",
                    css = UICss.ToClass(new string[]{ UICss.NarrowFormMarginAuto }),
                    elements= new IUIElement[]
                    {
                        new UILabel()
                        {
                            label="Wiederherstellungscode hier eingeben:"
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
                            label=bridge.GetCustomTextBlock(this, "restorationcode-label1", "Achtung: Benutzen sie den Wiederherstellungscode nur auf Geräten, auf denen ausschließich sie Zugriff haben, da sonst auch andere Benutzer ihre Projekte sehen könnten.")
                        },
                        new UIBreak(),
                        new UIBreak(),
                        new UICallbackButton(callback ?? this ,"restoration-code-commit")
                        {
                            text="Wiederherstellungscode übernehmen"
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
