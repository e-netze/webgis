using E.Standard.WebMapping.Core.Api.Bridge;
using gView.GraphicsEngine;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace E.Standard.WebMapping.Core.Api.EventResponse;

public class GraphicsResponse
{
    public GraphicsResponse(IBridge bridge)
    {
        ReplaceElements = true;
        this.Bridge = bridge;
    }

    private IBridge Bridge { get; set; }

    public GraphicsTool ActiveGraphicsTool { get; set; }

    private object _elements = null;
    public object Elements
    {
        get
        {
            return _elements;
        }
        set
        {
            _elements = value;
            if (value == null)
            {
                this.SymbolsDefinition = null;
            }
            else
            {
                string markerPath = this.Bridge.WWWRootPath + @"/content/api/img/graphics/markers";

                XmlDocument symbolsDef = new XmlDocument();
                if (new FileInfo(markerPath + @"/symbols.xml").Exists)
                {
                    symbolsDef.Load(markerPath + @"/symbols.xml");
                }

                List<object> symbolDefs = new List<object>();

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
                            XmlNode symbolNode = symbolsDef.SelectSingleNode("symbols/symbol[@name='" + name + "' and @hx and @hy]");
                            if (symbolNode != null)
                            {
                                hx = int.Parse(symbolNode.Attributes["hx"].Value);
                                hy = int.Parse(symbolNode.Attributes["hy"].Value);
                            }


                            symbolDefs.Add(new
                            {
                                id = "graphics/markers/" + name,
                                icon = "graphics/markers/" + name,
                                iconSize = new int[] { bm.Width, bm.Height },
                                iconAnchor = new int[] { hx, hy },
                                popupAnchor = new int[] { bm.Width / 2 - hx, -hy }
                            });
                        }
                        ;
                    }
                    catch { }
                }

                this.SymbolsDefinition = symbolDefs.ToArray();
            }
        }
    }

    public bool ReplaceElements { get; set; }

    public bool? SuppressZoom { get; set; }

    public object SymbolsDefinition
    {
        get;
        private set;
    }
}
