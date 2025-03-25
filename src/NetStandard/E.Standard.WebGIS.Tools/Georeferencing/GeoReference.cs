using E.Standard.Localization.Abstractions;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.GeoReferencing;

[Export(typeof(IApiButton))]
[ToolStorageIsolatedUser(isUserIsolated: false)]
[ToolHelp("tools/general/georeference.html")]
public class GeoReference : IApiServerButtonLocalizable<GeoReference>, 
                            IApiButtonResources
{
    #region IApiServerButton Member

    public ApiEventResponse OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<GeoReference> localizer)
    {
        return new ApiEventResponse
        {
            UIElements = new IUIElement[] {
                new UIDiv()
                {
                    //target=UIElementTarget.modaldialog.ToString(),
                    targettitle = localizer.Localize("georeference-document"),
                    elements=new IUIElement[]
                    {
                        new UILabel()
                        {
                            label = localizer.Localize("label1:body")
                        },
                        new UIBreak(2),
                        new UIUploadFile(this.GetType(), "upload-file") {
                            id = "upload-file",
                            css = UICss.ToClass(new string[]{UICss.ToolParameter})
                        }
                    }
                }
            }
        };
    }

    #endregion

    #region IApiButton Member

    public string Name => "Georeference";

    public string Container => "Tools";

    public string Image => UIImageButton.ToolResourceImage(this, "georeference");

    public string ToolTip => "Upload documents for georeferencing.";

    public bool HasUI => true;

    #endregion

    #region Server Commands

    [ServerToolCommand("upload-file")]
    public ApiEventResponse OnUploadFile(IBridge bridge, ApiToolEventArguments e, ILocalizer<GeoReference> localizer)
    {
        var file = e.GetFile("upload-file");
        if (file != null)
        {
            var dataset = new DocumentReader.DocumentReader().ReadDocument(file.FileName, new MemoryStream(file.Data));

            if (dataset != null && dataset.Tables != null)
            {
                MemoryStream ms = new MemoryStream();
                dataset.WriteXml(ms);

                string tempId = bridge.Storage.SaveTempDataString(System.Text.Encoding.UTF8.GetString(ms.ToArray()), 60 * 24);

                List<string> tables = new List<string>();
                List<string> fields1 = new List<string>(), fields2 = new List<string>();

                fields1.Add(localizer.Localize("choose-field"));
                fields2.Add(localizer.Localize("choose-another-field"));
                foreach (DataTable table in dataset.Tables)
                {
                    tables.Add(table.TableName);

                    foreach (DataColumn column in table.Columns)
                    {
                        fields1.Add(column.ColumnName);
                        fields2.Add(column.ColumnName);
                    }
                }
                if (fields1.Count == 2)
                {
                    fields1.RemoveAt(0);
                }

                return new ApiEventResponse
                {
                    UIElements = new IUIElement[] {
                        new UIDiv()
                        {
                            target=UIElementTarget.modaldialog.ToString(),
                            targettitle = localizer.Localize("georeference-document"),
                            css = UICss.ToClass(new string[]{ UICss.NarrowFormMarginAuto }),
                            elements=new IUIElement[]
                            {
                                new UIHidden()
                                {
                                    id="data-id",
                                    value=tempId,
                                    css=UICss.ToClass(new string[]{UICss.ToolParameter})
                                },
                                new UILabel()
                                {
                                    label = $"{localizer.Localize("label-name")}:"
                                },
                                new UIBreak(),
                                new UIInputText()
                                {
                                    value=file.FileName,
                                    id="document-name",
                                    css=UICss.ToClass(new string[]{UICss.ToolParameter}),
                                },
                                new UIDiv()
                                {
                                    visible=tables.Count>1,
                                    elements=new IUIElement[]
                                    {

                                        new UIBreak(2),
                                        new UILabel()
                                        {
                                            label = $"{localizer.Localize("data-sheet")}:",
                                        },
                                        new UIBreak(),
                                        new UISelect(tables.ToArray())
                                        {
                                            id="dataset-table",
                                            visible=tables.Count>1,
                                            css=UICss.ToClass(new string[]{UICss.ToolParameter}),
                                        },
                                    }
                                },
                                new UIDiv()
                                {
                                    visible=fields1.Count>1,
                                    elements=new UIElement[]
                                    {
                                        new UIBreak(2),
                                        new UILabel()
                                        {
                                            label = $"{localizer.Localize("geo-fields")}:"
                                        },
                                        new UIBreak(),
                                        new UISelect(fields1.ToArray())
                                        {
                                            id="dataset-field-1",
                                            css=UICss.ToClass(new string[]{UICss.ToolParameter})
                                        },
                                        new UIBreak(),
                                        new UISelect(fields2.ToArray())
                                        {
                                            visible=fields1.Count>2,
                                            id="dataset-field-2",
                                            css=UICss.ToClass(new string[]{UICss.ToolParameter})
                                        },
                                        new UIBreak(),
                                        new UISelect(fields2.ToArray())
                                        {
                                            visible=fields1.Count>3,
                                            id="dataset-field-3",
                                            css=UICss.ToClass(new string[]{UICss.ToolParameter})
                                        },
                                    }
                                },
                                new UIBreak(2),
                                new UICallbackButton(this, "georef")
                                {
                                    text = localizer.Localize("apply")
                                }
                            }
                        }
                    }
                };
            }
        }

        return null;
    }

    [ServerToolCommand("georef")]
    async public Task<ApiEventResponse> OnGeoreference(IBridge bridge, ApiToolEventArguments e)
    {
        string dataString = bridge.Storage.LoadTempDataString(e["data-id"]);
        MemoryStream dataStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(dataString));

        var dataset = new DataSet();
        dataset.ReadXml(dataStream);

        var table = dataset.Tables[e["dataset-table"]];
        string columnName1 = e["dataset-field-1"];
        string columnName2 = e["dataset-field-2"];
        string columnName3 = e["dataset-field-3"];

        var features = new WebMapping.Core.Collections.FeatureCollection();

        List<string> terms = new List<string>();
        foreach (DataRow row in table.Rows)
        {
            string term = GetTerm(row, columnName1, columnName2, columnName3);

            if (String.IsNullOrWhiteSpace(term))
            {
                continue;
            }

            terms.Add(term);
        }

        var geoRefItemsDict = await bridge.GeoReferenceAsync(terms);

        foreach (DataRow row in table.Rows)
        {
            string term = GetTerm(row, columnName1, columnName2, columnName3);

            if (String.IsNullOrWhiteSpace(term))
            {
                continue;
            }

            var geoRefItems = geoRefItemsDict[term];
            if (geoRefItems != null && geoRefItems.Count() > 0)
            {
                var feature = new WebMapping.Core.Feature();
                feature.Shape = new Point(geoRefItems.First().Coords[0], geoRefItems.First().Coords[1]);
                feature.GlobalOid = "id" + Guid.NewGuid().ToString("N").ToLower();

                foreach (DataColumn column in table.Columns)
                {
                    object val = row[column.ColumnName];
                    if (val != null)
                    {
                        feature.Attributes.Add(new WebMapping.Core.Attribute(column.ColumnName, val.ToString()));
                    }
                }

                features.Add(feature);
            }
        }

        string geoJson = bridge.ToGeoJson(features);
        string jsonId = bridge.Storage.SaveTempDataString(geoJson, 24 * 60);

        return new ApiDynamicContentEventResponse()
        {
            Name = e["document-name"],
            Url = bridge.ToolCommandUrl("geojson", new { geojsonid = jsonId }),
            Type = DynaimcContentType.GeoJson,
            UIElements = new IUIElement[] {
                        new UIDiv()
                        {
                            target=UIElementTarget.modaldialog_hidden.ToString(),
                        }
            },
            ActiveTool = null  // ToTo: Active Button wieder setzen
        };
    }

    [ServerToolCommand("geojson")]
    public ApiEventResponse OnGeoJson(IBridge bridge, ApiToolEventArguments e)
    {
        string dataString = bridge.Storage.LoadTempDataString(e["geojsonid"]);

        return new ApiRawStringEventResponse(dataString, "application/json");
    }

    #endregion

    #region IApiButtonResources

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("georeference", Properties.Resources.georeference);
    }

    #endregion

    #region Helper

    private string GetTerm(DataRow row, string columnName1, string columnName2, string columnName3)
    {
        string term = Convert.ToString(row[columnName1]);
        if (!String.IsNullOrWhiteSpace(columnName2) && !columnName2.StartsWith("--"))
        {
            term += " " + Convert.ToString(row[columnName2]);
        }

        if (!String.IsNullOrWhiteSpace(columnName3) && !columnName3.StartsWith("--"))
        {
            term += " " + Convert.ToString(row[columnName3]);
        }

        return term;
    }

    #endregion
}
