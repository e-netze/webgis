using E.Standard.ArcXml;
using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Extensions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.Web.Models;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.CmsSchema.Legacy;
using E.Standard.WebGIS.CmsSchema.Models;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema.Extensions;

static class SchemaNodesExtensions
{
    async static public Task<IEnumerable<LayerField>> FieldsNames(this object[] objects,
                                                                  CMSManager cmsManager,
                                                                  CmsItemTransistantInjectionServicePack servicePack,
                                                                  bool excludeShape = false,
                                                                  bool excludeObjectId = false,
                                                                  bool onlyEditable = false)
    {
        List<LayerField> fields = new List<LayerField>();

        if (objects != null)
        {
            foreach (object obj in objects)
            {
                if (obj is Link)
                {
                    Link themeLink = (Link)obj;
                    ServiceLayer layer = cmsManager.SchemaNodeInstance(servicePack, themeLink.LinkUri, true) as ServiceLayer;
                    if (layer != null)
                    {
                        if (themeLink.LinkUri.StartsWith("services/ims"))
                        {
                            #region IMS Dienst abfragen

                            IMSService service = cmsManager.SchemaNodeInstance(servicePack, themeLink.LinkUri.TrimRightRelativeCmsPath(2), true) as IMSService;

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

                                    IMSServerInfo serviceInfo = new IMSServerInfo(servicePack.HttpService,
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
                                                fields.Add(new LayerField(name));
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

                            ArcServerService service = cmsManager.SchemaNodeInstance(servicePack, themeLink.LinkUri.TrimRightRelativeCmsPath(2), true) as ArcServerService;
                            try
                            {
                                foreach (JsonField field in await service.GetLayerFieldsAsync(layer.Id))
                                {
                                    if ((excludeObjectId == true || onlyEditable == true) && "esriFieldTypeOID".Equals(field.Type, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        continue;
                                    }

                                    if ((excludeShape == true || onlyEditable == true) && "esriFieldTypeGeometry".Equals(field.Type, StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        continue;
                                    }

                                    if (onlyEditable && field.Name.Contains("("))  // Shape.STArea(), ...
                                    {
                                        continue;
                                    }

                                    fields.Add(new LayerField(field.Name, field.Alias)
                                    {
                                        HasDomain = field.Domain?.CodedValues != null && field.Domain.CodedValues.Length > 0
                                    });
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

                            WFSService service = cmsManager.SchemaNodeInstance(servicePack, themeLink.LinkUri.TrimRightRelativeCmsPath(2), true) as WFSService;

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

                                string xml = await servicePack.HttpService.GetStringAsync(url, requestAuthorization);
                                OGC.Schema.wfs.DescribeFeatureHelper dfh = new OGC.Schema.wfs.DescribeFeatureHelper(xml);

                                foreach (OGC.Schema.wfs.DescribeFeatureHelper.Field field in dfh.TypeFields(layer.Id))
                                {
                                    fields.Add(new LayerField(field.Name));
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
        }

        return fields;
    }

    async static public Task<int> ServiceSrs(this string relPath,
                                             CMSManager cmsManager,
                                             CmsItemTransistantInjectionServicePack servicePack)
    {
        if (relPath.StartsWith("services/arcgisserver/mapserver"))
        {
            #region ArcGIS Dienst abfragen

            ArcServerService service = cmsManager.SchemaNodeInstance(servicePack, relPath, true) as ArcServerService;
            try
            {
                var sRef = await service.GetServiceSRef();
                if (sRef != 0)
                {
                    return sRef;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(service.ServiceUrl + "\n" + ex.Message);
            }

            #endregion
        }

        return 0;
    }

    static public bool CanImportFieldNames(this ISchemaNode schemaNode)
    {
        string relPath = schemaNode?.RelativePath;

        if (!String.IsNullOrEmpty(relPath))
        {
            if (relPath.ToLower().StartsWith("services/ims") ||
               relPath.ToLower().StartsWith("services/arcgisserver/mapserver") ||
               relPath.ToLower().StartsWith("services/ogc/wfs"))
            {
                return true;
            }
        }

        return false;
    }
}
