using E.Standard.Extensions.Credentials;
using E.Standard.Localization.Abstractions;
using E.Standard.Security.Core.Extensions;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.IO;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Api.UI.Setters;
using E.Standard.WebMapping.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace E.Standard.WebGIS.Tools.Serialization;

[Export(typeof(IApiButton))]
[ToolStorageId("WebGIS.Tools.Serialization")]
[AdvancedToolProperties(AnonymousUserIdDependent = true, ClientDeviceDependent = true)]
[ToolConfigurationSection("loadmap")]
[ToolHelp("tools/map/load.html")]
public class LoadMap : IApiServerButtonLocalizable<LoadMap>, 
                       IApiButtonResources, 
                       IApiToolConfirmation
{
    private const string ConfigAllowCollaboration = "allow-collaboration";
    private const string ConfigAllowAnonymousCollaboration = "allow-anonymous-collaboration";

    #region IApiServerButton Member

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<LoadMap> localizer)
    {
        return CreateUI(bridge, e, localizer);
    }

    #endregion

    #region IApiButton Member

    public string Name => "Load Map";

    public string Container => "Map";

    public string Image => UIImageButton.ToolResourceImage(this, "open");

    public string ToolTip => "Load saved map.";

    public bool HasUI => true;

    #endregion

    #region IApiButtonResources Member

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("open", Properties.Resources.open);
    }

    #endregion

    private ApiEventResponse CreateUI(IBridge bridge, ApiToolEventArguments e, ILocalizer<LoadMap> localizer)
    {
        List<IUIElement> uiElements = new List<IUIElement>();

        if (!String.IsNullOrWhiteSpace(bridge.CurrentUser.Username))
        {
            uiElements.AddRange(new IUIElement[] {
                new UIDiv(){
                    //target=UIElementTarget.modaldialog.ToString(),
                    targettitle = localizer.Localize("name"),
                    elements=new UIElement[]{
                        new UILabel(){label = localizer.Localize("load-label")},
                        new UIBreak(),
                        new UISelect(bridge.Storage.GetNames(includeDirectories: false)?
                                                   .Select(n => HttpUtility.HtmlEncode( n.FromValidEncodedName()))
                                                   .ToArray()) {
                              id="serialization-load-mapname",
                              css=UICss.ToClass(new string[]{UICss.ToolParameter}),
                        },
                        new UIButtonContainer(new UIElement[]{
                            new UICallbackButton(this, "delete-map") {
                                text = localizer.Localize("delete"),
                                css=UICss.ToClass(new string[]{ UICss.DangerButtonStyle })
                            },
                            new UICallbackButton(this, "load-map") {
                                text = localizer.Localize("load")
                            }
                        })
                    }
                }
            });
        }
        if (bridge.CurrentUser.IsAnonymous) // Benutzer ist anonym und hat noch keine Projekte gespeichert bzw. hat seinen LocalStorage gelöscht.
        {
            uiElements.AddRange(new IUIElement[] {
                new UIDiv(){
                    //target=UIElementTarget.modaldialog.ToString(),
                    targettitle="Karte laden",
                    elements=new UIElement[]{
                        new UILabel()
                        {
                            label= localizer.Localize("recovery-code-label1:body")
                        },
                        new UIBreak(),
                        new UIBreak(),
                        new UICallbackButton(this, "restoration-code")
                        {
                            text = localizer.Localize("enter-recovery-code"),
                            css = UICss.ToClass(new string[] { UICss.CancelButtonStyle })
                        }
                    }
                }
            });
        }

        return new ApiEventResponse()
        {
            UIElements = uiElements.ToArray()
        };
    }

    #region IApiToolConfirmation Member

    public ApiToolConfirmation[] ToolConfirmations
    {
        get
        {
            List<ApiToolConfirmation> confirmations = new List<ApiToolConfirmation>();
            confirmations.AddRange(ApiToolConfirmation.CommandComfirmations(typeof(LoadMap)));
            return confirmations.ToArray();
        }
    }

    #endregion

    #region Commands

    [ServerToolCommand("load-map")]
    public ApiEventResponse OnLoadMap(IBridge bridge, ApiToolEventArguments e)
    {
        if (String.IsNullOrWhiteSpace(bridge.CurrentUser.Username))
        {
            throw new Exception("Unauthorized");
        }

        string name = e["serialization-load-mapname"];
        string encodedName = name.ToValidEncodedName();

        string mapJson = String.Empty, geoJson = String.Empty;

        if (bridge.Storage.Exists(encodedName))
        {
            mapJson = bridge.Storage.LoadString(encodedName).ReplaceLegacyMapJsonItems();
            geoJson = bridge.Storage.LoadString($"{encodedName}-graphics", StorageBlobType.Data);
        }
        else if (bridge.Storage.Exists(name))
        {
            // Old projects do not has an encoded name
            mapJson = bridge.Storage.LoadString(name).ReplaceLegacyMapJsonItems();
            geoJson = bridge.Storage.LoadString($"{name}-graphics", StorageBlobType.Data);
        }

        if (String.IsNullOrEmpty(mapJson))
        {
            throw new Exception("Can't find project...");
        }

        return new MapJsonResponse()
        {
            SerializationMapJson = mapJson,
            Graphics = new GraphicsResponse(bridge) { Elements = geoJson, SuppressZoom = true },
            MapTitle = $"{(bridge.CurrentUser.IsAnonymous ? String.Empty : bridge.CurrentUser.Username.PureUsername() + ": ")}{name}",
            UIElements = new IUIElement[] {
                new UIEmpty(){
                    target=UIElementTarget.modaldialog.ToString(),
                }
            },
            RemoveSecondaryToolUI = e.UseSimpleToolsBehaviour()
        };
    }

    [ServerToolCommand("delete-map")]
    [ToolCommandConfirmation("confirm-delete", ApiToolConfirmationType.YesNo, ApiToolConfirmationEventType.ButtonClick)]
    public ApiEventResponse OnDeleteMap(IBridge bridge, ApiToolEventArguments e, ILocalizer<LoadMap> localizer)
    {
        if (String.IsNullOrWhiteSpace(bridge.CurrentUser.Username))
        {
            throw new Exception("Unauthorized");
        }

        string name = e["serialization-load-mapname"];
        if (String.IsNullOrWhiteSpace(name))
        {
            throw new Exception("No map selected...");
        }
        var encodedName = name.ToValidEncodedName();
        if (bridge.Storage.Exists(encodedName))
        {
            // New project names are encoded
            bridge.Storage.Remove(encodedName);
            bridge.Storage.Remove($"{encodedName}-graphics", StorageBlobType.Data);
        }
        else if (bridge.Storage.Exists(name))
        {
            // Older projekt names are not encoded...
            bridge.Storage.Remove(name);
            bridge.Storage.Remove($"{name}-graphics", StorageBlobType.Data);
        }

        return CreateUI(bridge, e, localizer);
    }

    [ServerToolCommand("mapjson")]
    public ApiEventResponse MapJson(IBridge bridge, ApiToolEventArguments e, ILocalizer<LoadMap> localizer)
    {
        string project = e["project"];

        string owner = bridge.Storage.OwnerName(project);

        if (!owner.Equals(bridge.CurrentUser.Username, StringComparison.OrdinalIgnoreCase))
        {
            if (!e.GetConfigBool(ConfigAllowCollaboration, false))
            {
                throw new Exception(localizer.Localize("exception-collaboration-forbidden:body"));
            }
            if (bridge.CurrentUser.IsAnonymous && !e.GetConfigBool(ConfigAllowAnonymousCollaboration, false))
            {
                throw new Exception(localizer.Localize("exception-anonymous-collaboration-forbidden:body"));
            }
        }

        string mapJson = bridge.Storage.LoadString(project).ReplaceLegacyMapJsonItems();
        string geoJson = bridge.Storage.LoadString(
            bridge.Storage.AppendToName(project, "-graphics"), StorageBlobType.Data);

        string name = bridge.Storage is IStorage2 ?
            ((IStorage2)bridge.Storage).GetDecodedName(project).Replace("\\", "/").Split('/').Last() :
            project;

        //return new ApiRawStringEventResponse("{\"mapjson\":" + mapJson + ",\"geojson\":" + geoJson + "}", "application/json");
        return new MapJsonResponse()
        {
            Graphics = new GraphicsResponse(bridge) { Elements = geoJson, SuppressZoom = true },
            SerializationMapJson = mapJson,
            ActiveTool = null,
            MapTitle = $"{(bridge.CurrentUser.IsAnonymous ? String.Empty : bridge.CurrentUser.Username.PureUsername() + ": ")}{name.FromValidEncodedName()}"
        };
    }

    [ServerToolCommand("list-user-projects")]
    public ApiEventResponse OnListUserProjects(IBridge bridge, ApiToolEventArguments e)
    {
        List<ProjectInfo> projectInfos = new List<ProjectInfo>();

        if (!bridge.CurrentUser.IsAnonymous)
        {
            foreach (string name in bridge.Storage.GetNames(includeDirectories: false))
            {
                if (name.Contains("."))
                {
                    continue;
                }

                var clearName = name.FromValidEncodedName();
                projectInfos.Add(new ProjectInfo()
                {
                    name = clearName,
                    urlname = bridge.Storage.CreateEncryptedName(bridge.CurrentUser.Username, name)
                });
            }
        }
        return new ApiRawJsonEventResponse(new { projects = projectInfos.ToArray() });
    }

    [ServerToolCommand("restoration-code")]
    public ApiEventResponse RestorationCode(IBridge bridge, ApiToolEventArguments e, ILocalizer<SaveMap> localizer)
    {
        return new SaveMap().RestorationCode(bridge, e, this, localizer);
    }

    [ServerToolCommand("restoration-code-commit")]
    public ApiEventResponse RestorationCodeCommit(IBridge bridge, ApiToolEventArguments e)
    {
        var handleResult = HandleRestorationCodeCommit(bridge, e);

        return handleResult.response;
    }

    public (ApiEventResponse response, Guid anonyomousUserGuid) HandleRestorationCodeCommit(IBridge bridge, ApiToolEventArguments e)
    {
        Guid anonyomousUserGuid = e["serialization-restoration-code"].Base64ToGuid();

        var response = new ApiEventResponse()
        {
            UIElements = new List<IUIElement>(new IUIElement[] {
                new UIEmpty() {
                    target = UIElementTarget.modaldialog.ToString(),
                }
            }),
            UISetters = new List<IUISetter>(new IUISetter[] { new UIAnonymousUserIdSetter(bridge, anonyomousUserGuid) }),
            TriggerToolButtonClick = this.GetType().ToToolId()
        };

        return (response, anonyomousUserGuid);
    }

    #region Json Item Classes

    private class ProjectInfo
    {
        public string name { get; set; }
        public string urlname { get; set; }
    }

    #endregion

    #endregion
}
