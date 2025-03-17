using Api.Core.Models.DataLinq;
using E.DataLinq.Core;
using E.DataLinq.Core.Reflection;
using E.Standard.Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Api.Core.AppCode.Extensions.Razor;

static public class DataLinqHelperExtensions
{
    [HelpDescription("Erstellt ein Kartenelement in einem DIV-Tag")]
    static public object Map(this IDataLinqHelper dlh,
        [HelpDescription("CMS-Dienste, die angezeigt werden sollen")]
        string services,
        [HelpDescription("Kartenausdehnung aus CMS, bspw. \"stmk_m34@ccgis_...\"")]
        string extent,
        [HelpDescription("Ein anonymes Ojekt mit HTML-Attributen für das Karten-Element <div>, bspw. ((new { style=\"width:300px;height:300px\" @class=\"meine-klasse\" } ))")]
        object htmlAttributes = null,
        [HelpDescription("Darstellungsvarianten, bspw. \"dvg1,dv_2=on\"")]
        string presentations = "")
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("<div");
        //AppendHtmlAttribute(sb, "class", "datalinq-map");
        dlh.AppendHtmlAttribute(sb, "data-map-extent", extent);
        dlh.AppendHtmlAttribute(sb, "data-map-services", services);
        dlh.AppendHtmlAttribute(sb, "data-map-presentations", presentations);
        dlh.AppendHtmlAttributes(sb, htmlAttributes, "datalinq-map");
        sb.Append("></div>");

        return dlh.ToRawString(sb.ToString());
    }

    [HelpDescription("Erstellt einen HTML-Tag, der mit einem Geoobjekt verbunden ist. Bei Klick auf diesen Tag wird in der Karte auf dieses Objekt gezoomt.")]
    static public object BeginGeoElementFor(this IDataLinqHelper dlh,
        [HelpDescription("DataLinq-Datensatz, der eine (Punkt-)Geometrie enthält. Zu diesen Koordinaten wird in der Karte hingezoomt.")]
        object record,
        [HelpDescription("Der Name des Koordinatenfeldes, nur bei speziellem Name notwendig, sonst leerer String.")]
        string name = "",
        [HelpDescription("HTML-Tag, der erstellt werden soll")]
        string tag = "div",
        [HelpDescription("Ein anonymes Ojekt mit HTML-Attributen für das HTML-Element, bspw. ((new { style=\"height:300px\", @class=\"meine-klasse\" } ))")]
        object htmlAttributes = null,
        [HelpDescription("Der Text, der beim Popup erscheinen soll. Dieser kann sich auch auf den Datensatz beziehen, bspw. \"Hallo <br/>\" + record[\"STOERKAT\"] ")]
        string popup = "Geo Objekt",
        [HelpDescription("Falls über das Popup zurück in die Liste gesprungen werden soll, kann der Text des Link definiert werden, bspw.: \"zur Listenansicht\". Die Liste springt (scrollt) zum gewünschten Listeneintrag. Um das Scrollen auf einen Bereich einzuschränken, wird (vom Listeneintrag ausgehend) nach übergeordneten DOM-Elementen gesucht, die entweder die Klasse 'datalinq-scroll' oder eine 'overflow'/'overflow-y' CSS-Eigenschaft mit dem Wert 'auto' bzw. 'scroll' besitzen. Ansonsten wird nicht der gesamte Fenster gescrollt.")]
        string link2list = ""//,
                             //[HelpDescription("Ein anonymes Ojekt mit 3 Attributen (Dienst, Abfrage, ID) um das Feature in der Karte zu selektieren (highlight),  ((new { service=\"meindienst@cms\", query=\"meineAbfrage\",  id=@record[\"ID-Feld\"]} ))")]
                             //object selection = null
        , string[] geoJsonFeatureProperties = null
        )
    {
        var recordDictionary = (IDictionary<string, object>)record;
        RecordLocation location = null;
        if (String.IsNullOrWhiteSpace(name))
        {
            location = (RecordLocation)recordDictionary.Values.Where(v => v is RecordLocation).FirstOrDefault();
        }
        else if (recordDictionary.ContainsKey(name))
        {
            location = recordDictionary[name] as RecordLocation;
        }

        StringBuilder sb = new StringBuilder();
        sb.Append("<" + tag);
        dlh.AppendHtmlAttributes(sb, htmlAttributes, location != null ? "datalinq-geo-element" : "");
        if (location != null)
        {
            dlh.AppendHtmlAttribute(sb, "data-geo-lat", location.Latitude.ToPlatformNumberString());
            dlh.AppendHtmlAttribute(sb, "data-geo-lng", location.Longitude.ToPlatformNumberString());
            dlh.AppendHtmlAttribute(sb, "data-geo-id", Guid.NewGuid().ToString("N"));
            dlh.AppendHtmlAttribute(sb, "data-geo-popup", popup);
            dlh.AppendHtmlAttribute(sb, "data-geo-link2list", link2list);

            if (location.BBoxValid)
            {
                dlh.AppendHtmlAttribute(sb, "data-geo-bbox", String.Join(",", location.BBox.Select(c => c.ToPlatformNumberString())));
            }
        }

        if (geoJsonFeatureProperties != null)
        {
            foreach (var geoJsonFeatureProperty in geoJsonFeatureProperties)
            {
                if (recordDictionary.ContainsKey(geoJsonFeatureProperty))
                {
                    dlh.AppendHtmlAttribute(sb, $"data-geo-feature-attribute-_{geoJsonFeatureProperty}", recordDictionary[geoJsonFeatureProperty]?.ToString());
                }
            }
        }
        sb.Append(">");
        return dlh.ToRawString(sb.ToString());
    }

    [HelpDescription("Beendet den HTML-Tag, der zuvor mit \"BeginGeoElementFor\" begonnen hat.")]
    static public object EndGeoElement(this IDataLinqHelper dlh,
        [HelpDescription("HTML-Tag, der beendet werden soll")]
        string tag = "div")
    {
        return dlh.ToRawString("</" + tag + ">");
    }

    [HelpDescription("Ruft die Methode \"BeginGeoElementFor\" auf und gibt als Tag-Art <div> (Division) mit.")]
    static public object BeginGeoDivFor(this IDataLinqHelper dlh,
        [HelpDescription("DataLinq-Datensatz, der eine (Punkt-)Geometrie enthält. Zu diesen Koordinaten wird in der Karte hingezoomt.")]
        object record,
        [HelpDescription("Der Name des Koordinatenfeldes, nur bei speziellem Name notwendig")]
        string name = "",
        [HelpDescription("Ein anonymes Ojekt mit HTML-Attributen für die Tabellenzeile <tr>, bspw. ((new { style=\"height:300px\" @class=\"meine-klasse\" } ))")]
        object htmlAttributes = null,
        [HelpDescription("Text, der beim Popup erscheinen soll")]
        string popup = "Geo Objekt")
    {
        return dlh.BeginGeoElementFor(record, name, "div", htmlAttributes, popup);
    }

    [HelpDescription("Ruft die Methode \"EndGeoElement\" auf und gibt als Tag-Art <div> (Division) mit.")]
    static public object EndGeoDiv(this IDataLinqHelper dlh) { return dlh.EndGeoElement("div"); }

    [HelpDescription("Ruft die Methode \"BeginGeoElementFor\" auf und gibt als Tag-Art <tr> (Tabellenzeile) mit.")]
    static public object BeginGeoTableRowFor(this IDataLinqHelper dlh,
        [HelpDescription("DataLinq-Datensatz, der eine (Punkt-)Geometrie enthält. Zu diesen Koordinaten wird in der Karte hingezoomt.")]
        object record,
        [HelpDescription("Der Name des Koordinatenfeldes, nur bei speziellem Name notwendig")]
        string name = "",
        [HelpDescription("Ein anonymes Ojekt mit HTML-Attributen für die Tabellenzeile <tr>, bspw. ((new { style=\"height:300px\" @class=\"meine-klasse\" } ))")]
        object htmlAttributes = null,
        [HelpDescription("Text, der beim Popup erscheinen soll")]
        string popup = "Geo Objekt")
    {
        return dlh.BeginGeoElementFor(record, name, "tr", htmlAttributes, popup);
    }

    [HelpDescription("Ruft die Methode \"EndGeoElement\" auf und gibt als Tag-Art <tr> (Tabellenzelle) mit.")]
    static public object EndGeoTableRow(this IDataLinqHelper dlh) { return dlh.EndGeoElement("tr"); }

    [HelpDescription("Ruft die Methode \"BeginGeoElementFor\" auf und gibt als Tag-Art <td> (Tabellenzeile) mit.")]
    static public object BeginGeoTableCellFor(this IDataLinqHelper dlh,
        [HelpDescription("DataLinq-Datensatz, der eine (Punkt-)Geometrie enthält. Zu diesen Koordinaten wird in der Karte hingezoomt.")]
        object record,
        [HelpDescription("Der Name des Koordinatenfeldes, nur bei speziellem Name notwendig")]
        string name = "",
        [HelpDescription("Ein anonymes Ojekt mit HTML-Attributen für die Tabellenzeile <tr>, bspw. ((new { style=\"height:300px\" @class=\"meine-klasse\" } ))")]
        object htmlAttributes = null,
        [HelpDescription("Text, der beim Popup erscheinen soll")]
        string popup = "Geo Objekt")
    {
        return dlh.BeginGeoElementFor(record, name, "td", htmlAttributes, popup);
    }

    [HelpDescription("Ruft die Methode \"EndGeoElement\" auf und gibt als Tag-Art <td> (Tabellenzelle) mit.")]
    static public object EndGeoTableCell(this IDataLinqHelper dlh) { return dlh.EndGeoElement("td"); }
}
