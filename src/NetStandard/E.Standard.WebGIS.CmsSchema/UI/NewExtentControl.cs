using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using E.Standard.OGC.Schema;
using E.Standard.OGC.Schema.wmts_1_0_0;
using E.Standard.Platform;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace E.Standard.WebGIS.CmsSchema.UI;

public class NewExtentControl : NameUrlUserConrol, IInitParameter, ISubmit
{
    private readonly NameUrlControl _nameUrlControl = new NameUrlControl("nameUrlControl");
    private readonly GroupBox _gbOptional = new GroupBox() { Label = "Optional" };
    private readonly Input _txtWMTS = new Input("WMTS") { Label = "Link zu WMTS Capabilities", Placeholder = "https://..." };

    private readonly CmsItemTransistantInjectionServicePack _servicePack;
    private readonly ISchemaNode _node = null;

    public NewExtentControl(CmsItemTransistantInjectionServicePack servicePack, ISchemaNode schemaNode)
    {
        _servicePack = servicePack;
        _node = schemaNode;

        this.AddControl(_nameUrlControl);
        this.AddControl(_gbOptional);

        _gbOptional.AddControl(_txtWMTS);
        _servicePack = servicePack;
    }

    #region IInitParameter

    public object InitParameter { set => _nameUrlControl.InitParameter = value; }

    #endregion

    #region ISubmit

    public void Submit(NameValueCollection secrets)
    {
        if (_node is Extent extent)
        {
            string wmtsUrl = _txtWMTS.Value?.Trim();

            if (!String.IsNullOrEmpty(wmtsUrl))
            {
                if (wmtsUrl.EndsWith("?") || wmtsUrl.EndsWith("&"))
                {
                    wmtsUrl += "SERVICE=WMTS&VERSION=1.0.0&REQUEST=GetCapabilities";
                }
                else if (!wmtsUrl.ToLower().EndsWith(".xml"))
                {
                    wmtsUrl += "/1.0.0/wmtscapabilities.xml";
                }

                Serializer<Capabilities> ser = new Serializer<Capabilities>();
                string xml = _servicePack.HttpService.GetStringAsync(wmtsUrl).Result;

                xml = xml.Replace("<Layer>", @"<ows:DatasetDescriptionSummary xsi:type=""LayerType"">");
                xml = xml.Replace("</Layer>", @"</ows:DatasetDescriptionSummary>");

                var capabilities = ser.FromString(xml, Encoding.UTF8);
                var boundingBox = capabilities
                                    .Contents
                                    .TileMatrixSet.FirstOrDefault()?
                                    .BoundingBox ??
                                  capabilities
                                    .Contents
                                    .DatasetDescriptionSummary?.FirstOrDefault()
                                    .BoundingBox?.FirstOrDefault();

                int xIndex = 0, yIndex = 1;

                if (boundingBox != null)
                {
                    if (int.TryParse(boundingBox.crs.Split(":").Last(), out int crsId))
                    {
                        extent.ProjId = crsId;

                        if (new int[] { 31254, 31255, 31256, 31257, 31258, 31259 }.Contains(crsId))
                        {
                            xIndex = 1;
                            yIndex = 0;
                        }
                    }

                    extent.MinX = boundingBox.LowerCorner.Split(' ')[xIndex].ToPlatformDouble();
                    extent.MinY = boundingBox.LowerCorner.Split(' ')[yIndex].ToPlatformDouble();
                    extent.MaxX = boundingBox.UpperCorner.Split(' ')[xIndex].ToPlatformDouble();
                    extent.MaxY = boundingBox.UpperCorner.Split(' ')[yIndex].ToPlatformDouble();
                }

                var tileMatrixes = capabilities
                                        .Contents
                                        .TileMatrixSet.FirstOrDefault()?
                                        .TileMatrix;

                double dpi = 25.4D / 0.28D;  // WMTS -> 1Pixel is 0.28mm;

                if (tileMatrixes is not null && tileMatrixes.Length > 0)
                {
                    extent.OriginX = tileMatrixes[0].TopLeftCorner.Split(' ')[xIndex].ToPlatformDouble();
                    extent.OriginY = tileMatrixes[0].TopLeftCorner.Split(' ')[yIndex].ToPlatformDouble();
                    extent.Resolutions = tileMatrixes
                                            .Select(tm => tm.ScaleDenominator / (dpi / 0.0254))
                                            .ToArray();
                }
            }
        }
    }

    #endregion

    #region Overrides

    public override NameUrlControl NameUrlControlInstance => _nameUrlControl;

    #endregion
}
