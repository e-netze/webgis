using E.Standard.Api.App.DTOs;
using E.Standard.Extensions.Compare;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.Api.App.Extensions;

static public class DynamicServiceExtensions
{
    #region Presentations

    static public bool UseDynamicPresentations(this IMapService service)
    {
        return service is IDynamicService && ((IDynamicService)service).CreatePresentationsDynamic != ServiceDynamicPresentations.Manually;
    }

    static public PresentationDTO[] DynamicPresentations(
                this IDynamicService service,
                IMapService originalService,
                IEnumerable<ServiceInfoDTO.LayerInfo> layers)
    {
        List<PresentationDTO> presentations = new List<PresentationDTO>();
        char groupSeparator = service.DynamicGroupSeparator();
        var ignoreParents = service.IgnoreDynamicParents();

        if (service.UseDynamicPresentations())
        {
            int maxLevel = 0;
            switch (service.CreatePresentationsDynamic)
            {
                case ServiceDynamicPresentations.AutoMaxLevel1:
                    maxLevel = 1;
                    break;
                case ServiceDynamicPresentations.AutoMaxLevel2:
                    maxLevel = 2;
                    break;
                case ServiceDynamicPresentations.AutoMaxLevel3:
                    maxLevel = 3;
                    break;
            }

            presentations.Add(new PresentationDTO()
            {
                id = $"dv_{service.Name}-off".ToValidCmsUrl(),
                name = service.DynamicName($"-- {service.Name} aus"),
                layers = new string[0],
                items = new PresentationDTO.GdiProperties[]
                {
                    new PresentationDTO.GdiProperties()
                    {
                        container = service.Name,
                        name = $"alle aus: {service.Name}",
                        visible = true,
                        visible_with_service = true,
                        style = "button"
                    }
                }
            });

            if (originalService?.Layers?.Any(l => l.Visible) == true)
            {
                presentations.Add(new PresentationDTO()
                {
                    id = $"dv_{service.Name}-default".ToValidCmsUrl(),
                    name = service.DynamicName($"{service.Name} Standard"),
                    layers = originalService.Layers.Where(l => l.Visible).Select(l => l.Name).ToArray(),
                    items = new PresentationDTO.GdiProperties[]
                    {
                        new PresentationDTO.GdiProperties()
                        {
                            container = service.Name,
                            name = $"Standard: {service.Name}",
                            visible = true,
                            visible_with_service = true,
                            style = "button"
                        }
                    }
                });
            }

            Dictionary<string, PresentationDTO> groupPresentations = new Dictionary<string, PresentationDTO>();

            foreach (var layer in layers)
            {
                var presentation = new PresentationDTO();

                string layerFullname = layer.name;

                foreach (var ignoreParent in ignoreParents)
                {
                    if (!String.IsNullOrEmpty(ignoreParent) && layerFullname.StartsWith(ignoreParent, StringComparison.InvariantCultureIgnoreCase))
                    {
                        layerFullname = layerFullname.Substring(ignoreParent.Length);
                        break;
                    }
                }

                string[] nameParts = layerFullname.Split(groupSeparator);

                string name = layerFullname;
                string groupName = null, uiGroupName = null;

                if (maxLevel > 0 && nameParts.Length > maxLevel)
                {
                    nameParts = nameParts.Take(maxLevel).ToArray();
                    string groupKey = String.Join("/", nameParts);

                    var groupPresentation = groupPresentations.ContainsKey(groupKey) ? groupPresentations[groupKey] : null;
                    if (groupPresentation == null)
                    {
                        name = nameParts.Last();

                        if (nameParts.Length > 1)
                        {
                            groupName = nameParts.First();
                            if (nameParts.Length > 2)
                            {
                                uiGroupName = String.Join("/", nameParts.Take(nameParts.Length - 1).Skip(1));
                            }
                        }

                        groupPresentation = new PresentationDTO();
                        groupPresentation.name = service.DynamicName(name);
                        groupPresentation.layers = new string[] { layer.name };
                        groupPresentation.items = new PresentationDTO.GdiProperties[]
                        {
                            new PresentationDTO.GdiProperties()
                            {
                                container = service.Name,
                                groupstyle = !String.IsNullOrEmpty(groupName) ? "dropdown" : null,
                                name = groupName,
                                ui_groupname = uiGroupName,
                                visible = true,
                                visible_with_service = true,
                                style = "checkbox"
                            }
                        };

                        presentations.Add(groupPresentation);
                        groupPresentations.Add(groupKey, groupPresentation);
                    }
                    else
                    {
                        List<string> groupLayers = new List<string>(groupPresentation.layers);
                        groupLayers.Add(layer.name);
                        groupPresentation.layers = groupLayers.ToArray();
                    }
                }
                else
                {
                    #region Add Simple Presentation

                    if (nameParts.Length > 1)
                    {
                        name = nameParts.Last();
                        groupName = nameParts.First();
                        if (nameParts.Length > 2)
                        {
                            uiGroupName = String.Join("/", nameParts.Take(nameParts.Length - 1).Skip(1));
                        }
                    }

                    var presentationName = service.DynamicName(name);
                    if (service is IMapService2)
                    {
                        presentationName = (((IMapService2)service).LayerProperties.GetLayerProperties(layer.id)?.Aliasname).OrTake(presentationName);
                    }

                    presentation.id = $"dv_{layerFullname}".ToValidCmsUrl();
                    presentation.name = presentationName;
                    presentation.layers = new string[] { layer.name };
                    presentation.items = new PresentationDTO.GdiProperties[]
                    {
                        new PresentationDTO.GdiProperties()
                        {
                            container = service.Name,
                            groupstyle = !String.IsNullOrEmpty(groupName) ? "dropdown" : null,
                            name = groupName,
                            ui_groupname = uiGroupName,
                            visible = true,
                            visible_with_service = true,
                            style = "checkbox",
                            metadata = layer.metadata,
                            metadata_button_style = String.IsNullOrEmpty(layer.metadata) ? null : layer.MetadataButtonStyle.ToString().ToLower(),
                            metadata_target = String.IsNullOrEmpty(layer.metadata) ? null :  layer.MetadataTarget.ToString().ToLower(),
                            metadata_title =  String.IsNullOrEmpty(layer.metadata) ? null : layer.MetadataTitle?.ToString().ToLower()
                        }
                    };

                    presentations.Add(presentation);

                    #endregion
                }
            }
        }
        return presentations.ToArray();
    }

    static public bool AllowPresentations(this IMapService service)
    {
        if (service is IImageServiceType && ((IImageServiceType)service).ImageServiceType == ImageServiceType.Watermark)
        {
            return false;
        }

        return true;
    }

    #endregion

    #region Queries

    static public bool UseDynamicQueries(this IMapService service)
    {
        return service is IDynamicService && ((IDynamicService)service).CreateQueriesDynamic != ServiceDynamicQueries.Manually;
    }

    static public QueryDTO[] GetDynamicQueries(this IDynamicService service/*, IEnumerable<ServiceInfo.LayerInfo> layers*/)
    {
        List<QueryDTO> queries = new List<QueryDTO>();
        char groupSeparator = service.DynamicGroupSeparator();

        if (service.UseDynamicQueries())
        {
            foreach (var layer in service.Layers?.Where(l => l.Queryable == true))
            {
                string queryName = layer.Name.Split(groupSeparator).Last(), queryId = queryName.ToValidCmsUrl();

                var query = queries.Where(q => q.id == queryId).FirstOrDefault();

                if (query != null)
                {

                }
                else
                {
                    queryName = service.DynamicName(queryName);
                    if (service is IMapService2)
                    {
                        queryName = (((IMapService2)service).LayerProperties.GetLayerProperties(layer.ID)?.Aliasname).OrTake(queryName);
                    }

                    query = new QueryDTO()
                    {
                        id = queryId,
                        name = service.DynamicName(queryName),
                        associatedlayers = new QueryDTO.AssociatedLayer[] { new QueryDTO.AssociatedLayer() { id = layer.ID } },
                        LayerId = layer.ID
                    };

                    queries.Add(query);
                }
            }
        }

        return queries.ToArray();
    }

    static public QueryDTO GetDynamicQuery(this IDynamicService service, string queryId)
    {
        var query = service.GetDynamicQueries().Where(q => q.id == queryId).FirstOrDefault();
        if (query != null)
        {
            var layer = service.Layers.Where(l => l.ID == query.LayerId).FirstOrDefault();
            if (layer == null)
            {
                return null;
            }

            if (layer.Fields != null && layer.Fields.Count() > 0)
            {
                query.Fields = layer.Fields
                            .Where(f => f.Type != FieldType.Shape && f.Type != FieldType.ID)
                            .Select(f => new TableFieldData()
                            {
                                FieldName = f.Name,
                                ColumnName = f.Alias.OrTake(f.Name),
                                Visible = true
                            })
                            .ToArray();
            }
            else
            {
                query.Fields = new TableFieldDTO[] { new TableFieldData() { FieldName = "*", Visible = true, ColumnName = "All" } };
            }

            query.Init(service);
        }

        return query;

        //var layer = service.Layers.Where(l => l.Name.Split('\\').Last().ToValidCmsUrl() == queryId).FirstOrDefault();
        //if (layer != null)
        //{
        //    var query = new Query()
        //    {
        //        name = layer.Name.Split('\\').Last(),
        //        id = queryId,
        //        LayerId = layer.ID,
        //        Fields = layer.Fields
        //                .Select(f => new TableFieldData()
        //                {
        //                    FieldName = f.Name,
        //                    ColumnName = f.Name,
        //                    Visible = true
        //                })
        //                .ToArray()
        //    };
        //    query.Init(service);
        //    return query;
        //}

        //return null;
    }

    static public QueryDTO GetDynamicQueryTemplate(this IDynamicService service, string queryId)
    {
        var query = service.GetDynamicQueries().Where(q => q.id == queryId).FirstOrDefault();
        if (query != null)
        {
            var layer = service.Layers.Where(l => l.ID == query.LayerId).FirstOrDefault();
            if (layer == null)
            {
                return null;
            }

            if (layer.Fields != null && layer.Fields.Count() > 0)
            {
                query.Fields = layer.Fields
                            .Select(f => new TableFieldData()
                            {
                                FieldName = f.Name,
                                ColumnName = f.Name,
                                Visible = true
                            })
                            .ToArray();
            }
            else
            {
                query.Fields = new TableFieldDTO[] { new TableFieldData() { FieldName = "*", Visible = true, ColumnName = "All" } };
            }
        }

        return query;
    }

    #endregion

    #region Service Type

    static public ImageServiceType? CustomImageServiceType(this IMapService service)
    {
        if (service is IImageServiceType && ((IImageServiceType)service).ImageServiceType != ImageServiceType.Normal)
        {
            return ((IImageServiceType)service).ImageServiceType;
        }

        return null;
    }

    #endregion

    #region General

    static public char DynamicGroupSeparator(this IDynamicService service)
    {
        if (service is E.Standard.WebMapping.GeoServices.OGC.WMS.WmsService)
        {
            return '/';
        }

        return '\\';
    }

    static public IEnumerable<string> IgnoreDynamicParents(this IDynamicService service)
    {
        if (service is E.Standard.WebMapping.GeoServices.OGC.WMS.WmsService)
        {
            return new string[]
            {
                $"Layers{ DynamicGroupSeparator(service) }",
                $"GeoServer Web Map Service{ DynamicGroupSeparator(service) }"
            };
        }

        return new string[0];
    }

    static public string DynamicName(this IDynamicService service, string name)
    {
        if (service is E.Standard.WebMapping.GeoServices.OGC.WMS.WmsService)
        {
            if (name.IndexOf("(") > 0)
            {
                name = name.Substring(0, name.LastIndexOf("(")).Trim();
            }
        }

        return name;
    }

    #endregion
}
