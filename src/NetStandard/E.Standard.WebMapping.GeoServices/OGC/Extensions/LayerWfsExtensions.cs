using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Filters;
using E.Standard.WebMapping.GeoServices.OGC.GML;
using E.Standard.WebMapping.GeoServices.OGC.WFS.Helper;
using System;
using System.Text;

namespace E.Standard.WebMapping.GeoServices.OGC.Extensions;

static internal class LayerWfsExtensions
{
    static public string GetFeature1_0_0(this ILayer layer, QueryFilter filter, string srsName, Filter_Capabilities filterCapabilites, int maxFeatures = 40)
    {
        string typeName = layer.ID;

        StringBuilder sb = new StringBuilder();

        if (maxFeatures <= 0)
        {
            maxFeatures = 40;
        }

        sb.Append(
$@"<?xml version=""1.0"" ?>
<GetFeature
version=""1.0.0""
service=""WFS""
maxFeatures=""{maxFeatures}""
handle=""webgis Query""
xmlns=""http://www.opengis.net/wfs""
xmlns:ogc=""http://www.opengis.net/ogc""
xmlns:gml=""http://www.opengis.net/gml""
xmlns:gv=""http://www.gview.com/server""
xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
xsi:schemaLocation=""http://www.opengis.net/wfs ../wfs/1.1.0/WFS.xsd"">");

        sb.Append($@"<Query typeName=""{typeName}""");

        if (!String.IsNullOrEmpty(srsName))
        {
            sb.Append($@" srsName=""{srsName}""");
        }
        sb.Append(">");

        if (filter.SubFields != "*" && filter.SubFields != "#ALL#")
        {
            foreach (string field in filter.SubFields.Split(' '))
            {
                sb.Append("<ogc:PropertyName>" + field + "</ogc:PropertyName>");
            }
        }
        else
        {
            foreach (IField field in layer.Fields)
            {
                sb.Append("<ogc:PropertyName>" + field.Name + "</ogc:PropertyName>");
            }
        }
        sb.Append(FilterHelper.ToWFS(layer, filter, filterCapabilites, GmlVersion.v1));

        sb.Append(
@"</Query>
</GetFeature>");

        return sb.ToString();
    }

    static public string GetFeature1_1_0(this ILayer layer, QueryFilter filter, string srsName, Filter_Capabilities filterCapabilites, int maxFeatures = 40, bool ignoreAxis = true)
    {
        string typeName = layer.ID;

        StringBuilder sb = new StringBuilder();

        if (maxFeatures <= 0)
        {
            maxFeatures = 40;
        }

        sb.Append(
$@"<?xml version=""1.0"" ?>
<GetFeature
service=""WFS""
version=""1.1.0""
outputFormat=""text/xml; subtype=gml/3.1.1""
maxFeatures=""{maxFeatures}""
handle=""webgis Query""
xmlns=""http://www.opengis.net/wfs""
xmlns:ogc=""http://www.opengis.net/ogc""
xmlns:gml=""http://www.opengis.net/gml""
xmlns:gv=""http://www.gview.com/server""
xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
xsi:schemaLocation=""http://www.opengis.net/wfs ../wfs/1.1.0/WFS.xsd"">");

        sb.Append($@"<Query typeName=""{typeName}""");
        //sb.Append($@" maxFeatures=""{maxFeatures}""");

        if (!String.IsNullOrEmpty(srsName))
        {
            sb.Append($@" srsName=""{srsName}""");
        }
        sb.Append(">");

        if (filter.SubFields != "*" && filter.SubFields != "#ALL#")
        {
            foreach (string field in filter.SubFields.Split(' '))
            {
                sb.Append("<ogc:PropertyName>" + field + "</ogc:PropertyName>");
            }
        }
        else
        {
            foreach (IField field in layer.Fields)
            {
                sb.Append("<ogc:PropertyName>" + field.Name + "</ogc:PropertyName>");
            }
        }
        sb.Append(FilterHelper.ToWFS(layer, filter, filterCapabilites, GmlVersion.v3, ignoreAxis));

        sb.Append(
@"</Query>
</GetFeature>");

        return sb.ToString();
    }

    static public string CreateDescribeFeatureType1_1_0(this ILayer layer, string outputFormat)
    {
        string typeName = layer.ID;

        StringBuilder sb = new StringBuilder();

        sb.Append(
@"<?xml version=""1.0"" ?>
<DescribeFeatureType
service=""WFS""
version=""1.1.0""
outputFormat=""" + outputFormat + @"""
xmlns=""http://www.opengis.net/wfs""
xmlns:ogc=""http://www.opengis.net/ogc""
xmlns:gml=""http://www.opengis.net/gml""
xmlns:gv=""http://www.gview.com/server""
xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance""
xsi:schemaLocation=""http://www.opengis.net/wfs ../wfs/1.1.0/WFS.xsd"">");

        sb.Append($"<TypeName>{typeName}</TypeName>");
        sb.Append("</DescribeFeatureType>");

        return sb.ToString();
    }
}
