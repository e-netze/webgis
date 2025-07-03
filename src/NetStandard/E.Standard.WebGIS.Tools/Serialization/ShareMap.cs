using E.Standard.Json;
using E.Standard.Localization.Abstractions;
using E.Standard.Security.Cryptography.Extensions;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Core.Serialization;
using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.IO;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using QRCoder;
using System;
using System.Collections.Generic;
using static QRCoder.PayloadGenerator;

namespace E.Standard.WebGIS.Tools.Serialization;

[Export(typeof(IApiButton))]
[ToolStorageId(@"WebGIS.Tools.Portal.Maps\{0}\_sharedmaps")]
[AdvancedToolProperties(ClientDeviceDependent = true)]
[ToolStorageIsolatedUser(false)]
[ToolHelp("tools/map/share.html")]
[ToolConfigurationSection("share")]
public class ShareMap : IApiServerButtonLocalizable<ShareMap>,
                        IApiButtonResources,
                        IStorageInteractions,
                        IApiToolConfirmation
{
    #region IApiButton

    public string Name => "Share Map";

    public string Container => "Map";

    public string Image => UIImageButton.ToolResourceImage(this, "share");

    public string ToolTip => "Generate link to share the current map.";

    public bool HasUI => true;

    #endregion

    #region IApiServerButton

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<ShareMap> localizer)
    {
        var duration = e.GetConfigValue("duration");

        if (String.IsNullOrWhiteSpace(duration))
        {
            duration = localizer.Localize("durations");
        }

        var durationSelect = new UISelect()
        {
            id = "serialization-share-duration",
            css = UICss.ToClass(new string[] { UICss.ToolParameter }),
            options = new List<UISelect.Option>()
        };

        foreach (var d in duration.Split(','))
        {
            try
            {
                durationSelect.options.Add(new UISelect.Option()
                {
                    value = int.Parse(d.Split(':')[0]).ToString(),
                    label = d.Split(':')[1].Trim()
                });
            }
            catch { }
        }

        return new ApiEventResponse()
        {
            UIElements = new IUIElement[]
            {
                new UIDiv()
                {
                    //target = UIElementTarget.modaldialog.ToString(),
                    targettitle = localizer.Localize("name"),
                    elements = new IUIElement[]
                    {
                        new UILabel()
                        {
                            label = localizer.Localize("label1:body")
                        },
                        durationSelect,
                        new UIHidden(){
                            id="serialization-share-mapjson",
                            css=UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapSerialization})
                        },
                        new UIHidden(){
                            id="serialization-share-graphics-geojson",
                            css=UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapGraphicsGeoJson})
                        },
                        new UIHidden()
                        {
                            id="serialization-share-page-id",
                            css=UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapPageId})
                        },
                        new UIHidden()
                        {
                            id="serialization-share-map-name",
                            css=UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapName})
                        },
                        new UIHidden()
                        {
                            id="serialization-share-map-category",
                            css=UICss.ToClass(new string[]{UICss.ToolParameter, UICss.AutoSetterMapCategory})
                        },
                        new UIButtonContainer(new UICallbackButton(this, "share-createlink")
                        {
                            css = UICss.ToClass(new string[] { UICss.DefaultButtonStyle }),
                            text = localizer.Localize("generate-link")
                        })
                    }
                }
            }
        };
    }

    #endregion

    #region IApiButtonResources

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("share", Properties.Resources.share);
    }

    #endregion

    #region IStorageInteractions Member

    public string StoragePathFormatParameter(IBridge bridge, int index)
    {
        if (bridge != null && bridge.CurrentEventArguments != null)
        {
            switch (index)
            {
                case 0:
                    return PageId(bridge);
            }
        }
        return String.Empty;
    }

    #endregion

    #region Commands

    [ServerToolCommand("share-createlink")]
    [ToolCommandConfirmation("confirm-not-saveable-tabs", ApiToolConfirmationType.YesNo, ApiToolConfirmationEventType.ButtonClick)]
    public ApiEventResponse OnCreateLink(IBridge bridge, ApiToolEventArguments e, ILocalizer<ShareMap> localizer)
    {
        string duration = e["serialization-share-duration"];
        string pageId = e["serialization-share-page-id"];
        string mapName = e["serialization-share-map-name"];
        string mapCategory = e["serialization-share-map-category"];

        var meta = new SharedMapMeta()
        {
            MapName = mapName,
            MapCategory = mapCategory,
            Expires = DateTime.UtcNow.AddDays(int.Parse(duration))
        };

        string name = Guid.NewGuid().ToString().HashHex();

        bridge.Storage.Save(name, e["serialization-share-mapjson"]);

        string geoJson = e["serialization-share-graphics-geojson"];
        if (!String.IsNullOrWhiteSpace(geoJson) && geoJson.Length > 2)
        {
            bridge.Storage.Save($"{name}.graphics", geoJson, StorageBlobType.Data);
        }

        bridge.Storage.Save($"{name}.meta", JSerializer.Serialize(meta), StorageBlobType.Metadata);

        string url = $"{bridge.WebGisPortalUrl}/{pageId}/map/{SharedMapMeta.SharedMapsCategory}/{Uri.EscapeDataString(name)}";
        string subject = $"{mapCategory} - {mapName}";

        #region Generate QR Code

        Url generator = new Url(url);
        string payload = generator.ToString();

        QRCodeGenerator qrGenerator = new QRCodeGenerator();
        QRCodeData qrCodeData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        Base64QRCode qrCode = new Base64QRCode(qrCodeData);
        string qrCodeImageAsBase64 = qrCode.GetGraphic(20);

        #endregion

        return new ApiEventResponse()
        {
            RemoveSecondaryToolUI = e.UseSimpleToolsBehaviour(),
            UIElements = new IUIElement[] {
                new UIDiv()
                {
                    target = UIElementTarget.modaldialog.ToString(),
                    targettitle = localizer.Localize("name"),
                    css = UICss.ToClass(new string[]{ UICss.NarrowFormMarginAuto }),
                    elements = new IUIElement[]
                    {
                        new UIInputTextArea(true)
                        {
                            id="serialization-share-url",
                            value = url
                        },
                        //new UIImage(qrCodeImageAsBase64, true) {
                        //    style="width:320px;height:320px"
                        //},
                        new UIShareLinkButtons(url)
                        {
                            subject = subject,
                            qr_base64 = qrCodeImageAsBase64
                        }
                    }
                }
            }
        };
    }


    [ServerToolCommand("shared-map-meta")]
    public ApiEventResponse OnSharedMapMeta(IBridge bridge, ApiToolEventArguments e)
    {
        var name = e["name"];

        var meta = bridge.Storage.LoadString(name + ".meta", StorageBlobType.Metadata);

        return new ApiRawStringEventResponse(meta, "text/plain");
    }

    #endregion

    #region IApiToolConfirmation Member

    public ApiToolConfirmation[] ToolConfirmations
    {
        get
        {
            List<ApiToolConfirmation> confirmations = new List<ApiToolConfirmation>();
            confirmations.AddRange(ApiToolConfirmation.CommandComfirmations(typeof(ShareMap)));
            return confirmations.ToArray();
        }
    }

    #endregion

    #region Helper

    private string PageId(IBridge bridge)
    {
        if (!String.IsNullOrWhiteSpace(bridge.CurrentEventArguments["serialization-share-page-id"]))
        {
            return bridge.CurrentEventArguments["serialization-share-page-id"];
        }
        if (!String.IsNullOrWhiteSpace(bridge.CurrentEventArguments["page-id"]))
        {
            return bridge.CurrentEventArguments["page-id"];
        }

        throw new ArgumentException("Unknown page-id");
    }

    #endregion
}
