using E.Standard.ArcXml;
using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using E.Standard.Web.Models;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.CmsSchema.Legacy;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema.TypeEditor;

public class ThemeFieldsEditor : UserControl, IUITypeEditorAsync
{
    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public ThemeFieldsEditor(CmsItemTransistantInjectionServicePack servicePack)
    {
        _servicePack = servicePack;

        this.AddControl(_lstFields);
    }

    #region UI

    ListBox _lstFields = new ListBox("lstFields") { Label = "Felder" };

    #endregion

    public IUIControl GetUIControl(ITypeEditorContext context)
    {
        throw new Exception("Use async method: GetUiControlAsync");
    }

    async public Task<IUIControl> GetUIControlAsync(ITypeEditorContext context)
    {
        await PerformQuery(context);

        return this;
    }

    async internal Task PerformQuery(ITypeEditorContext context)
    {
        object[] objects = null;
        if (context.Instance is LabellingField)
        {
            objects = context.CmsManager.SchemaNodeInstances(_servicePack, Helper.TrimPathRight(context.RelativePath, 2) + "/LabellingTheme", true);
        }
        else if (context.Instance is DocumentManagement)
        {
            objects = context.CmsManager.SchemaNodeInstances(_servicePack, Helper.TrimPathRight(context.RelativePath, 1) + "/QueryTheme", true);
        }
        else if (context.Instance is EditingField)
        {
            objects = context.CmsManager.SchemaNodeInstances(_servicePack, Helper.TrimPathRight(context.RelativePath, 3) + "/EditingTheme", true);
        }
        else
        {
            objects = context.CmsManager.SchemaNodeInstances(_servicePack, Helper.TrimPathRight(context.RelativePath, 2) + "/QueryTheme", true);
        }
        Dictionary<string, string> fields = new Dictionary<string, string>();

        foreach (object obj in objects)
        {
            if (obj is Link)
            {
                Link themeLink = (Link)obj;
                ServiceLayer layer = context.CmsManager.SchemaNodeInstance(_servicePack, themeLink.LinkUri, true) as ServiceLayer;
                if (layer != null)
                {
                    if (themeLink.LinkUri.StartsWith("services/ims"))
                    {
                        #region IMS Dienst abfragen

                        IMSService service = context.CmsManager.SchemaNodeInstance(_servicePack, Helper.TrimPathRight(themeLink.LinkUri, 2), true) as IMSService;
                        try
                        {

                            if (service != null)
                            {
                                var connectionProperties = new ArcAxlConnectionProperties()
                                {
                                    AuthUsername = service.Username,
                                    AuthPassword = service.Password,
                                    Token = service.Token,
                                    CheckUmlaut = true,
                                    Timeout = 25
                                };

                                IMSServerInfo serviceInfo = new IMSServerInfo(_servicePack.HttpService,
                                                                              connectionProperties,
                                                                              service.Server, Encoding.Default);

                                serviceInfo.UseShortNames = true;

                                if (await serviceInfo.GetFieldInfoByIdAsync(service.Service, layer.Id))
                                {
                                    for (int i = 0; i < serviceInfo.FieldCount; i++)
                                    {
                                        string name = String.Empty;
                                        int type = 0;
                                        if (serviceInfo.GetField(i, ref name, ref type))
                                        {
                                            if (fields.ContainsKey(name))
                                            {
                                                fields[name] += "," + (obj is Link ? ((Link)obj).LinkUri : "");
                                            }
                                            else
                                            {
                                                fields.Add(name, (obj is Link ? ((Link)obj).LinkUri : ""));
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(service.Server + "\n" + ex.Message);
                        }

                        #endregion
                    }
                    else if (themeLink.LinkUri.StartsWith("services/arcgisserver/mapserver"))
                    {
                        #region ArcGIS Dienst abfragen

                        ArcServerService service = context.CmsManager.SchemaNodeInstance(_servicePack, Helper.TrimPathRight(themeLink.LinkUri, 2), true) as ArcServerService;
                        try
                        {
                            foreach (string field in await service.GetLayerFieldNamesAsync(layer.Id))
                            {
                                if (fields.ContainsKey(field))
                                {
                                    fields[field] += "," + (obj is Link ? ((Link)obj).LinkUri : "");
                                }
                                else
                                {
                                    fields.Add(field, (obj is Link ? ((Link)obj).LinkUri : ""));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(service.ServiceUrl + "\n" + ex.Message);
                        }

                        #endregion
                    }
                    else if (themeLink.LinkUri.StartsWith("services/ogc/wfs"))
                    {
                        #region WFS Dienst abfragen

                        //dotNETConnector conn = new dotNETConnector(
                        //    System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"/dotNETConnector.xml", String.Empty, String.Empty);

                        WFSService service = context.CmsManager.SchemaNodeInstance(_servicePack, Helper.TrimPathRight(themeLink.LinkUri, 2), true) as WFSService;

                        RequestAuthorization requestAuthorization = null;

                        if ((!String.IsNullOrEmpty(service.Username) &&
                             !String.IsNullOrEmpty(service.Password)) ||
                            !String.IsNullOrEmpty(service.Token))
                        {
                            requestAuthorization = new RequestAuthorization()
                            {
                                Username = service.Username,
                                Password = service.Password,
                                UrlToken = service.Token
                            };
                        }


                        string version = "1.0.0";
                        switch (service.Version)
                        {
                            case WFS_Version.version_1_1_0:
                                version = "1.1.0";
                                break;
                        }
                        try
                        {
                            string url = service.Server;
                            url = WMSService.AppendToUrl(url, "VERSION=" + version + "&SERVICE=WFS&REQUEST=DescribeFeatureType&TYPENAME=" + layer.Id);
                            string xml = await _servicePack.HttpService.GetStringAsync(url, requestAuthorization);

                            OGC.Schema.wfs.DescribeFeatureHelper dfh = new OGC.Schema.wfs.DescribeFeatureHelper(xml);

                            foreach (OGC.Schema.wfs.DescribeFeatureHelper.Field field in dfh.TypeFields(layer.Id))
                            {
                                if (fields.ContainsKey(field.Name))
                                {
                                    fields[field.Name] += "," + (obj is Link ? ((Link)obj).LinkUri : "");
                                }
                                else
                                {
                                    fields.Add(field.Name, (obj is Link ? ((Link)obj).LinkUri : ""));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception(service.Server + "\n" + ex.Message);
                        }

                        #endregion
                    }
                }
            }
        }

        string[] selected = new string[0];
        if (context.Value is string[])
        {
            selected = (string[])context.Value;
        }
        else if (context.Value is string)
        {
            selected = new string[] { (string)context.Value };
        }

        if (context.ValueType == typeof(string))
        {
            _lstFields.MultiSelect = false;
        }

        _lstFields.Options.AddRange(fields.Keys
            .Select(f => new ListBox.Option(f)
            {
                Selected = selected.Contains(f) ? true : null
            })
            /*.OrderBy(o => o.Label)*/);
    }

    public IEnumerable<string> GetFields()
    {
        return _lstFields.Options.Select(o => o.Value);
    }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public object ResultValue
    {
        get
        {
            return _lstFields.SelectedItems ?? new string[0];
        }
    }
}
