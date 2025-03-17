using E.Standard.WebMapping.Core.Api.Bridge;
using gView.GraphicsEngine;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;

public class UISymbolSelector : UIOptionContainer
{
    public UISymbolSelector(
                    IBridge bridge,
                    string title,
                    UIButton.UIButtonType buttonType = UIButton.UIButtonType.clientbutton,
                    ApiClientButtonCommand buttonCommand = ApiClientButtonCommand.setgraphicssymbol,
                    string symbolId = ""
            )
        : base()
    {
        this.title = title;
        this.CollapseState = UICollapsableElement.CollapseStatus.Expanded;

        List<UIElement> uiElements = new List<UIElement>();

        string markerPath = $"{bridge.WWWRootPath}/content/api/img/graphics/markers";

        XmlDocument symbolsDef = new XmlDocument();
        if (new FileInfo($"{markerPath}/symbols.xml").Exists)
        {
            symbolsDef.Load($"{markerPath}/symbols.xml");
        }

        foreach (var fi in new DirectoryInfo(markerPath).GetFiles())
        {
            string name = fi.Name.ToLower();
            if (!name.EndsWith(".png") && !name.EndsWith(".gif"))
            {
                continue;
            }

            try
            {
                using (var bm = Current.Engine.CreateBitmap(fi.FullName))
                {
                    int hx = bm.Width / 2, hy = bm.Height / 2;
                    XmlNode symbolNode = symbolsDef.SelectSingleNode($"symbols/symbol[@name='{name}' and @hx and @hy]");
                    if (symbolNode != null)
                    {
                        hx = int.Parse(symbolNode.Attributes["hx"].Value);
                        hy = int.Parse(symbolNode.Attributes["hy"].Value);
                    }

                    string imageId = $"graphics/markers/{name}";
                    var imageButton = new UIImageButton(
                                $"content/api/img/graphics/markers/{name}",
                                buttonType,
                                buttonCommand
                            )
                    {
                        value = new
                        {
                            id = $"graphics/markers/{name}",
                            icon = $"graphics/markers/{name}",
                            iconSize = new int[] { bm.Width, bm.Height },
                            iconAnchor = new int[] { hx, hy },
                            popupAnchor = new int[] { bm.Width / 2 - hx, -hy }
                        }
                    };
                    if (imageId == symbolId)
                    {
                        this.value = imageButton.value;
                    }

                    uiElements.Add(imageButton);
                }
            }
            catch { }
        }

        this.elements = uiElements.ToArray();
    }
}
