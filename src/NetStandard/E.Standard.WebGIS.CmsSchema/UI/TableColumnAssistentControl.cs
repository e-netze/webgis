using E.Standard.ArcXml;
using E.Standard.CMS.Core;
using E.Standard.CMS.Core.Extensions;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.UI.Controls;
using E.Standard.Web.Models;
using E.Standard.WebGIS.CmsSchema.Extensions;
using E.Standard.WebGIS.CmsSchema.Legacy;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema.UI;

public class TableColumnAssistentControl : UserControl
{
    private string _relPath = String.Empty;
    private CMSManager _cms = null;
    private ISchemaNode _node = null;

    private ListBox lstFields = new ListBox("lstFields") { Label = "Felder" };

    private readonly CmsItemTransistantInjectionServicePack _servicePack;

    public TableColumnAssistentControl(CmsItemTransistantInjectionServicePack servicePack,
                                       ISchemaNode schemaNode)
    {
        _servicePack = servicePack;

        if (schemaNode != null)
        {
            _cms = schemaNode.CmsManager;
            _relPath = schemaNode.RelativePath;
            if (!_relPath.EndsWith("/") && !_relPath.EndsWith(@"\"))
            {
                _relPath += "/";
            }
        }
        _node = schemaNode;

        this.AddControl(lstFields);

        FillGridView().Wait();
    }

    async private Task FillGridView()
    {
        if (_cms != null || !String.IsNullOrEmpty(_relPath))
        {
            #region Felder aus Dienst auslesen

            object[] objects = null;
            if (_node is TableColumnAssistent)
            {
                objects = _cms.SchemaNodeInstances(_servicePack, _relPath.TrimAndAppendSchemaNodePath(2, "QueryTheme"), true);
            }
            else if (_node is LabellingFieldAssistent)
            {
                objects = _cms.SchemaNodeInstances(_servicePack, _relPath.TrimAndAppendSchemaNodePath(2, "LabellingTheme"), true);
            }

            List<(string name, string aliasname)> fields = new List<(string, string)>();

            if (objects != null)
            {
                foreach (object obj in objects)
                {
                    if (obj is Link)
                    {
                        Link themeLink = (Link)obj;
                        ServiceLayer layer = _cms.SchemaNodeInstance(_servicePack, themeLink.LinkUri, true) as ServiceLayer;
                        if (layer != null)
                        {
                            if (themeLink.LinkUri.StartsWith("services/ims"))
                            {
                                #region IMS Dienst abfragen
                                IMSService service = _cms.SchemaNodeInstance(_servicePack, themeLink.LinkUri.TrimRightRelativeCmsPath(2), true) as IMSService;
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
                                                fields.Add((name, name));
                                            }
                                        }
                                    }
                                }
                                #endregion
                            }
                            else if (themeLink.LinkUri.StartsWith("services/arcgisserver/mapserver"))
                            {
                                #region ArcGIS Dienst abfragen

                                ArcServerService service = _cms.SchemaNodeInstance(_servicePack, themeLink.LinkUri.TrimRightRelativeCmsPath(2), true) as ArcServerService;
                                fields.AddRange(await service.GetLayerFieldsAndAliasesAsync(layer.Id));

                                #endregion
                            }
                            else if (themeLink.LinkUri.StartsWith("services/ogc/wfs"))
                            {
                                #region WFS Dienst abfragen

                                WFSService service = _cms.SchemaNodeInstance(_servicePack, themeLink.LinkUri.TrimRightRelativeCmsPath(2), true) as WFSService;

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

                                string url = service.Server;
                                url = WMSService.AppendToUrl(url, "VERSION=1.0.0&SERVICE=WFS&REQUEST=DescribeFeatureType&TYPENAME=" + layer.Id);

                                string xml = await _servicePack.HttpService.GetStringAsync(url, requestAuthorization);
                                OGC.Schema.wfs.DescribeFeatureHelper dfh = new OGC.Schema.wfs.DescribeFeatureHelper(xml);

                                foreach (OGC.Schema.wfs.DescribeFeatureHelper.Field field in dfh.TypeFields(layer.Id))
                                {
                                    fields.Add((field.Name, field.Name));
                                }

                                #endregion
                            }
                        }
                    }
                }
            }

            #endregion

            #region Datatable erzeugen

            foreach (var field in fields)
            {
                lstFields.Options.Add(new ListBox.Option(field.name, field.aliasname));
            }

            #endregion
        }
    }

    public Field[] SelectedFields
    {
        get
        {
            List<Field> fields = new List<Field>();
            foreach (var option in lstFields.SelectedOptions)
            {
                fields.Add(new Field(option.Value, option.Label));
            }

            return fields.ToArray();
        }
    }

    #region Classes

    public class Field
    {
        public string FieldName, AliasName;

        public Field(string fieldName, string aliasName)
        {
            FieldName = fieldName;
            if (!String.IsNullOrEmpty(aliasName))
            {
                AliasName = aliasName;
            }
            else
            {
                AliasName = fieldName;
            }
        }
    }

    #endregion
}
