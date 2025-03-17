using E.Standard.Extensions.Compare;
using E.Standard.Extensions.Credentials;
using E.Standard.Json;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace E.Standard.WebGIS.Tools.Serialization;

[Export(typeof(IApiButton))]
[ToolConfigurationSection("liveshare")]
[ToolHelp("tools/map/liveshare/index.html")]
[AdvancedToolProperties(AsideDialogExistsDependent = true, LiveShareClientnameDependent = true)]
//[InDevelopment]
public class LiveShareMap : IApiClientTool, IApiButtonResources
{
    private const string DivInitSessionId = "liveshare-init-group-div";
    private const string DivShowSessionDivId = "livshare-show-goup-div";
    private const string SessionIdHiddenId = "liveshare-sessionid";
    private const string JoinSessionIdInputTextId = "livshare-joinsessionid";
    private const string PageIdHiddenId = "liveshare-page-id";
    private const string MapNameHiddenId = "liveshare-map-name";
    private const string MapCategoryHiddenId = "liveshare-map-category";
    private const string ButtonId = "webgis.tools.serialization.livesharemap";  // Tool id für servertoolcommand_ext
    private const string ClientNameInputId = "liveshare_clientname_text";


    #region IApiServerButton

    public string Name => "Live Share";

    public string Container => "Karte";

    public string Image => UIImageButton.ToolResourceImage(this, "liveshare");

    public string ToolTip => "Karte live teilen";

    public bool HasUI => true;

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        var clientname = e["_liveshare_clientname"];

        if (String.IsNullOrEmpty(clientname?.Trim())/* || clientname.Contains("::")*/) // :: => for debugging
        {
            return SetClientName(bridge, e);
        }

        var uiElements = new List<IUIElement>();

        if (!e.GetBoolean("_asidedialog_exists"))
        {
            uiElements.AddRange(new IUIElement[]
            {
                new UIDiv()
                {
                    target = "tool_aside",
                    elements = new IUIElement[]
                    {
                        new UIHidden()
                        {
                            id = SessionIdHiddenId,
                            css = UICss.ToClass(new string[]{ UICss.ToolParameter })
                        },
                        new UIHidden()
                        {
                            id = PageIdHiddenId,
                            css=UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapPageId})
                        },
                        new UIHidden()
                        {
                            id = MapNameHiddenId,
                            css = UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapName})
                        },
                        new UIHidden()
                        {
                            id = MapCategoryHiddenId,
                            css = UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapCategory})
                        },
                        new UIDiv()
                        {
                            id = DivInitSessionId,
                            VisibilityDependency = VisibilityDependency.HasLiveShareHubConnection | VisibilityDependency.IsNotInLiveShareSession,
                            elements = new UIElement[] {
                                     new UIButton(UIButton.UIButtonType.servertoolcommand_ext, "create-session")
                                     {
                                        css = UICss.ToClass(new string[] {UICss.CancelButtonStyle, UICss.OptionRectButtonStyle }),
                                        text = "Neue Session beginnen",
                                        id = ButtonId,
                                        icon = UIButton.ToolResourceImage(typeof(LiveShareMap), "create-session")
                                    },
                                    new UIButton(UIButton.UIButtonType.servertoolcommand_ext, "join-session")
                                    {
                                        css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.OptionRectButtonStyle, UICss.JoinLiveshareSessionButton }),
                                        text = "an Session teilnehmen",
                                        id = ButtonId,
                                        icon = UIButton.ToolResourceImage(typeof(LiveShareMap), "join-session")
                                    },
                                    new UIButton(UIButton.UIButtonType.servertoolcommand_ext, "exit")
                                    {
                                        css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.OptionRectButtonStyle }),
                                        text = "Liveshare schließen",
                                        id = ButtonId,
                                        icon = UIButton.ToolResourceImage(typeof(LiveShareMap), "exit")
                                    },
                                    new UIDiv()
                                    {
                                        css = UICss.ToClass(new string[] { "webgis-info" }),
                                        elements = new IUIElement[]
                                        {
                                            new UILiteral() { literal="Hier kann eine neue LiveShare Session erstellt werden, um seine Inhalte mit anderen zu teilen. Nach dem Beginnen einer Session, kann in der Folge mit dem Button 'Session teilen' ein Link erzeugt und weiter gegeben werden."}
                                        }
                                    },
                                    new UIBreak(1),
                                    new UIDiv()
                                    {
                                        css = UICss.ToClass(new string[] { "webgis-info" }),
                                        elements = new IUIElement[]
                                        {
                                            new UILiteral() { literal="Um an einer bestehenden Session teilzunehmen, erhalten sie in der Regel von einem Anwender einen Link bzw. eine Session-Id. Möchten sie an einer Session in dieser Karte teilnehmen, verwenden sie den Button 'An Session teilnehmen' und geben dort die Session-Id ein."}
                                        }
                                    }
                                }
                        },
                        new UIDiv()
                        {
                            id = DivShowSessionDivId,
                            VisibilityDependency = VisibilityDependency.IsInLiveShareSession,
                            elements = new IUIElement[]
                            {
                                        new UIButton(UIButton.UIButtonType.servertoolcommand_ext, "share-session")
                                        {
                                            css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.OptionRectButtonStyle }),
                                            text = "Session teilen",
                                            id = ButtonId,
                                            icon = UIButton.ToolResourceImage(typeof(LiveShareMap), "share-session"),
                                            VisibilityDependency = VisibilityDependency.IsLiveShareSessionOwner
                                        },
                                        new UIButton(UIButton.UIButtonType.servertoolcommand_ext, "leave-session")
                                        {
                                            css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.OptionRectButtonStyle }),
                                            text = "Session verlassen",
                                            id = ButtonId,
                                            icon = UIButton.ToolResourceImage(typeof(LiveShareMap), "leave-session"),
                                            VisibilityDependency = VisibilityDependency.IsNotLiveShareSessionOwner,
                                        },
                                        new UIButton(UIButton.UIButtonType.servertoolcommand_ext, "leave-session")
                                        {
                                            css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.OptionRectButtonStyle }),
                                            text = "Session beenden",
                                            id = ButtonId,
                                            icon = UIButton.ToolResourceImage(typeof(LiveShareMap), "leave-session"),
                                            VisibilityDependency = VisibilityDependency.IsLiveShareSessionOwner
                                        }
                                    }
                        },
                        new UILiveShareElement()
                        {
                            VisibilityDependency = VisibilityDependency.IsInLiveShareSession
                        }
                    }
                }
            });
        }

        uiElements.AddRange(new IUIElement[]
        {
            new UIDiv()
            {
                css = UICss.ToClass(new string[] { "webgis-info" }),
                elements = new IUIElement[]
                {
                    new UILiteral() { literal="Mit LiveShare kann man Karten Live mit anderern Anwendern teilen. Geteilt werden der Karteauschnitt, Sichbarkeit von Datenebenen, Zeichnungen (Redlining). Zum Starten oder zum Teilnehmen an LiveShare verwenden sie das Live Share Connect Fenster (rechts)." }
                }
            },
            new UIBreak(2),
            new UILabel() { label=$"Hallo, { clientname.PureUsername() }" },
            new UIBreak(),
            new UIImage($"{ bridge.AppRootUrl }/rest/usermarkerimage?id={ HttpUtility.UrlEncode(clientname.PureUsername()) }&width=60"),
            new UIDiv()
            {
                //css = UICss.ToClass(new string[] { "webgis-info" }),
                elements = new IUIElement[]
                {
                    new UILiteral() { literal="In die Karte klicken, um einen Marker über LiveShare zu teilen" }
                }
            },
            new UIButtonContainer()
            {
                 elements=new IUIElement[]
                 {
                     new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.removelivesharemarker)
                     {
                         css=UICss.ToClass(new []{ UICss.CancelButtonStyle }),
                         text="Marker entfernen"
                     },
                     new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.currentpos)
                     {
                         text="Aktuelle Position"
                     }
                 }
            }
        });

        return new ApiEventResponse()
        {
            UIElements = uiElements,

            InitLiveshareConnection = $"{e.GetConfigValue("hub")}"
        };
    }

    #endregion

    #region IApiServerTool

    public ToolType Type => ToolType.click;

    public ToolCursor Cursor => ToolCursor.Crosshair;

    //public ApiEventResponse OnEvent(IBridge bridge, ApiToolEventArguments e)
    //{
    //    var click = e.ToClickEvent();

    //    return new ApiEventResponse()
    //    {
    //        Marker = new MarkerDefinition()
    //        {
    //            Action = MarkerAction.AddOrReplace,
    //            Id = "liveshare-user-marker",
    //            Group = "liveshare",
    //            Longitude = click.Longitude,
    //            Latitude = click.Latitude,
    //            Icon = "liveshare-user",
    //            Options = new
    //            {

    //            }
    //        }
    //    };
    //}

    #endregion

    #region IApiButtonResources

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("liveshare", Properties.Resources.play);
        toolResourceManager.AddImageResource("create-session", Properties.Resources.play);
        toolResourceManager.AddImageResource("leave-session", Properties.Resources.stop);
        toolResourceManager.AddImageResource("join-session", Properties.Resources.join);
        toolResourceManager.AddImageResource("share-session", Properties.Resources.share);
        toolResourceManager.AddImageResource("exit", Properties.Resources.shutdown);
    }

    #endregion

    #region Commands

    [ServerToolCommand("create-session")]
    async public Task<ApiEventResponse> OnCreateSession(IBridge bridge, ApiToolEventArguments e)
    {
        var hubUrl = e.GetConfigValue("hub");
        var simplifySessionIds = e.GetConfigBool("simplify-session-ids");

        using (HttpClient httpClient = GetHttpClient(bridge, e))
        {
            var response = await httpClient.GetAsync($"{hubUrl}/hubgroup{(simplifySessionIds ? "?simplify=true" : "")}");
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var group = JSerializer.Deserialize<HubSessionModel>(await response.Content.ReadAsStringAsync());

                var groupId = group.simpleGroupId.OrTake(group.groupId);

                return new ApiEventResponse()
                {
                    UIElements = ShareSessionDialog(bridge, e, groupId),
                    UISetters = new IUISetter[] {
                        new UISetter(SessionIdHiddenId, groupId)
                    },
                    JoinLiveshareSession = responseString
                };
            }
            else
            {
                throw new Exception($"Cant create LiveShare Session: Hub response is '{response.StatusCode}'");
            }
        }
    }

    [ServerToolCommand("join-session")]
    public ApiEventResponse OnJoinSession(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
        {
            UIElements = new IUIElement[] {
                new UIDiv()
                {
                    target = UIElementTarget.modaldialog.ToString(),
                    targettitle="Liveshare Session beitreten",
                    css = UICss.ToClass(new string[]{ UICss.NarrowFormMarginAuto }),
                    elements = new IUIElement[]
                    {
                        new UIInputText()
                        {
                            id=JoinSessionIdInputTextId,
                            css = UICss.ToClass(new string[]{ UICss.ToolParameter }),
                            value = e[SessionIdHiddenId]
                        },
                        new UIButton(UIButton.UIButtonType.servertoolcommand_ext, "join-existing-session")
                        {
                            css = UICss.ToClass(new string[] { UICss.DefaultButtonStyle }),
                            text = "An Session teilnehmen...",
                            id = ButtonId
                        }
                    }
                }
            }
        };
    }

    [ServerToolCommand("join-existing-session")]
    async public Task<ApiEventResponse> OnJoinExistingSession(IBridge bridge, ApiToolEventArguments e)
    {
        string sessionId = e[JoinSessionIdInputTextId];
        string simpleSessionId = sessionId;

        var simplifySessionIds = e.GetConfigBool("simplify-session-ids");

        if (String.IsNullOrEmpty(sessionId))
        {
            return null;
        }

        if (simplifySessionIds)
        {
            var hubUrl = e.GetConfigValue("hub");

            #region Get Complex Session Id

            using (HttpClient httpClient = GetHttpClient(bridge, e))
            {
                var response = await httpClient.GetAsync($"{hubUrl}/hubgroup/{simpleSessionId}");
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var session = JSerializer.Deserialize<HubSessionModel>(await response.Content.ReadAsStringAsync());

                    sessionId = responseString;
                }
                else
                {
                    throw new Exception($"Cant create LiveShare Session: Hub response is '{response.StatusCode}'");
                }
            }

            #endregion
        }

        return new ApiEventResponse()
        {
            UIElements = new IUIElement[] {
                new UIEmpty() {
                     target = UIElementTarget.modaldialog.ToString(),
                }
            },
            UISetters = new IUISetter[] {
                        new UISetter(SessionIdHiddenId, simpleSessionId)
                    },
            JoinLiveshareSession = sessionId
        };
    }

    [ServerToolCommand("leave-session")]
    public ApiEventResponse OnLeaveSession(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
        {
            UISetters = new IUISetter[] {
                new UISetter(SessionIdHiddenId, String.Empty)
            },
            LeaveLiveshareSession = e[SessionIdHiddenId]
        };
    }

    [ServerToolCommand("share-session")]
    public ApiEventResponse OnShareSession(IBridge bridge, ApiToolEventArguments e)
    {
        var sessionId = e[SessionIdHiddenId];

        return new ApiEventResponse()
        {
            UIElements = ShareSessionDialog(bridge, e, sessionId)
        };
    }

    [ServerToolCommand("exit")]
    public ApiEventResponse OnExit(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
        {
            UIElements = new IUIElement[] {
                new UIEmpty() {
                     target = "tool_aside",
                }
            },
            ExitLiveShare = true
        };
    }

    public ApiEventResponse SetClientName(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
        {
            UIElements = new IUIElement[]
            {
                new UIDiv()
                {
                    target = UIElementTarget.modaldialog.ToString(),
                    targettitle="Liveshare Benutzername",
                    css = UICss.ToClass(new string[]{ UICss.NarrowFormMarginAuto }),
                    elements = new IUIElement[]
                    {
                        new UIDiv()
                        {
                            css = UICss.ToClass(new string[] { "webgis-info" }),
                            elements = new IUIElement[]
                            {
                                new UILiteral() { literal="Um an WebGIS LiveShare teilzunemen, ist ein (anonymer) Anzeigename festzulegen. Dieser wird den anderen Benutzern in der Session angezeigt. Der Name kann frei vergeben werden und muss aus mindestens 4 Zeichen bestehen."}
                            }
                        },
                        new UILabel() { label = "Anzeigename:"},
                        new UIInputText()
                        {
                            id=ClientNameInputId,
                            css=UICss.ToClass(new []{ UICss.ToolParameter })
                        },
                        new UIButton(UIButton.UIButtonType.servertoolcommand, "set-client-name")
                        {
                            css=UICss.ToClass(new[]{ UICss.DefaultButtonStyle }),
                            text = "OK"
                        }
                    }
                }
            }
        };
    }

    [ServerToolCommand("set-client-name")]
    public ApiEventResponse OnSetClientName(IBridge bridge, ApiToolEventArguments e)
    {
        var clientName = e[ClientNameInputId];
        if (clientName == null || clientName.Trim().Length < 4)
        {
            throw new Exception("Benutzername muss aus mindesten 4 Zeichen bestehen");
        }

        e["_liveshare_clientname"] = clientName;

        var response = OnButtonClick(bridge, e);

        response.SetLiveShareClientname = clientName.Trim();
        response.UIElements.Add(
                new UIEmpty()
                {
                    target = UIElementTarget.modaldialog.ToString(),
                });

        return response;
    }

    #region Helper

    private ICollection<IUIElement> ShareSessionDialog(IBridge bridge, ApiToolEventArguments e, string sessionId)
    {
        string pageId = e[PageIdHiddenId];
        string mapName = e[MapNameHiddenId];
        string mapCategory = e[MapCategoryHiddenId];

        string url = $"{bridge.WebGisPortalUrl}/{pageId}/map/{Uri.EscapeDataString(mapCategory)}/{Uri.EscapeDataString(mapName)}?tool=webgis.tools.serialization.livesharemap&liveshare_session={sessionId}";
        string subject = $"LiveShare: {mapCategory} - {mapName}";

        #region Generate QR Code

        QRCoder.PayloadGenerator.Url generator = new QRCoder.PayloadGenerator.Url(url);
        string payload = generator.ToString();

        QRCodeGenerator qrGenerator = new QRCodeGenerator();
        QRCodeData qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        Base64QRCode qrCode = new Base64QRCode(qrCodeData);
        string qrCodeImageAsBase64 = qrCode.GetGraphic(20);

        #endregion

        return new IUIElement[] {
                new UIDiv()
                {
                    target = UIElementTarget.modaldialog.ToString(),
                    targettitle="Liveshare Session teilen",
                    css = UICss.ToClass(new string[]{ UICss.NarrowFormMarginAuto }),
                    elements = new IUIElement[]
                    {
                        new UILabel()
                        {
                            label="Liveshare Session Id:"
                        },
                        new UIInputText()
                        {
                            @readonly=true,
                            value = sessionId,
                        },
                        new UILabel()
                        {
                            label="Link zur Session:"
                        },
                        new UIInputTextArea(true)
                        {
                            value = url
                        },
                        new UIShareLinkButtons(url)
                        {
                            subject = subject,
                            qr_base64 = qrCodeImageAsBase64
                        }
                    }
                }
            };
    }

    private HttpClient GetHttpClient(IBridge bridge, ApiToolEventArguments e)
    {
        var hubUrl = e.GetConfigValue("hub");

        var httpClientHandler = new HttpClientHandler
        {
            Proxy = bridge.GetWebProxy(hubUrl),
        };

        var httpClient = new HttpClient(handler: httpClientHandler);

        if (!String.IsNullOrEmpty(e.GetConfigValue("clientId")) &&
            !String.IsNullOrEmpty(e.GetConfigValue("clientSecret")))
        {
            var authToken = Encoding.ASCII.GetBytes($"{e.GetConfigValue("clientId")}:{e.GetConfigValue("clientSecret")}");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(authToken));
        }

        return httpClient;
    }

    #endregion

    #endregion

    #region Models

    private class HubSessionModel
    {
        public string groupId { get; set; }
        public string groupOwnerPassword { get; set; }
        public string groupClientPassword { get; set; }
        public string simpleGroupId { get; set; }
    }

    #endregion
}
