using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.IO;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace E.Standard.WebGIS.Tools.Serialization;

abstract public class UserFavoritiesPresentations : IApiButton, IStorageInteractions
{
    protected const string InputTextNameId = "user-favorites-presentations-input-name";
    protected const string HiddenPageId = "user-favorites-presentations-hidden-page";
    protected const string HiddenMapName = "user-favorites-presentations-hidden-map";
    protected const string HiddenCurrentVisibility = "user-favorites-presentations-hidden-currentvisibility";
    protected abstract string ButtonId { get; } // = "webgis.tools.serialization.userfavoritiesmappresentations";

    #region IApiButton

    public string Name => "User Favorites Prestation";

    public string Container => String.Empty;

    public string Image => null;

    public string ToolTip => String.Empty;

    public bool HasUI => false;

    #endregion

    #region IStorageInteractions

    abstract public string StoragePathFormatParameter(IBridge bridge, int index);

    #endregion

    #region Server Commands

    [ServerToolCommand("get-favorite-presentations")]
    public ApiEventResponse OnGetFavoritePresentations(IBridge bridge, ApiToolEventArguments e)
    {
        var names = bridge.Storage.GetNames();

        var presentations = new List<Presentation>();
        foreach (var name in names)
        {
            presentations.Add(new Presentation()
            {
                Name = name.FromValidEncodedName(),
                Visiblity = JsonConvert.DeserializeObject(bridge.Storage.LoadString(name))
            });
        }

        return new ApiRawJsonEventResponse(presentations);
    }

    [ServerToolCommand("delete-favorite-presentations")]
    public ApiEventResponse OnDeleteFavoritePresenation(IBridge bridge, ApiToolEventArguments e)
    {
        if (!String.IsNullOrEmpty(e["name"]))
        {
            bridge.Storage.Remove(e["name"].ToValidEncodedName());
        }

        return null;
    }

    [ServerToolCommand("autocomplete-presentations")]
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

    [ServerToolCommand("create-new")]
    public ApiEventResponse OnCreateNew(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
        {
            UIElements = new IUIElement[] {
                new UIDiv(){
                    target=UIElementTarget.modaldialog.ToString(),
                    targettitle="Neue Darstellungsvariante erstellen",
                    css = UICss.ToClass(new string[]{ UICss.NarrowFormMarginAuto }),
                    elements= new IUIElement[]
                    {
                        new UIHidden()
                        {
                            value = e["page"],
                            css = UICss.ToClass(new[]{ UICss.ToolParameter }),
                            id=HiddenPageId
                        },
                        new UIHidden()
                        {
                            value=e["map"],
                            css = UICss.ToClass(new[]{ UICss.ToolParameter }),
                            id=HiddenMapName
                        },
                        new UIHidden()
                        {
                            css = UICss.ToClass(new[]{ UICss.ToolParameter, UICss.AutoSetterMapCurrentVisibility }),
                            id=HiddenCurrentVisibility
                        },

                        new UILabel()
                        {
                            label = "Name"
                        },
                        new UIInputAutocomplete(UIInputAutocomplete.MethodSource(bridge,this.GetType(), "autocomplete-presentations", new{ page=e["page"], map=e["map"] }))
                        {
                            css = UICss.ToClass(new[]{ UICss.ToolParameter }),
                            id=InputTextNameId
                        },

                        new UIButton(UIButton.UIButtonType.servertoolcommand_ext, "create")
                        {
                            css = UICss.ToClass(new string[] { UICss.DefaultButtonStyle }),
                            text = "Hinzufügen / Ersetzen",
                            id = ButtonId
                        }
                    }
                }
            }
        };
    }

    [ServerToolCommand("create")]
    public ApiEventResponse OnCreate(IBridge bridge, ApiToolEventArguments e)
    {
        string name = e[InputTextNameId];
        string visiblity = e[HiddenCurrentVisibility];

        if (String.IsNullOrEmpty(name))
        {
            throw new Exception("Bitte einen gültigen Namen eingeben");
        }

        if (String.IsNullOrEmpty(visiblity))
        {
            throw new Exception("Interner Fehler: Es wurde keine Layerschaltung übergeben.");
        }

        bridge.Storage.Save(name.ToValidEncodedName(), visiblity);

        return new ApiEventResponse()
        {
            UIElements = new IUIElement[] {
                new UIEmpty() {
                    target = UIElementTarget.modaldialog.ToString(),
                }
            },
            FireCustomMapEvent = "refresh-user-favorite-presenation"
        };
    }

    #endregion

    #region Classes/Models

    class Presentation
    {
        [JsonProperty("name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonProperty("visibility")]
        [System.Text.Json.Serialization.JsonPropertyName("visibility")]
        public object Visiblity { get; set; }
    }

    #endregion
}
