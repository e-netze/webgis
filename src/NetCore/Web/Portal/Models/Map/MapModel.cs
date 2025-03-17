using System;
using System.Collections.Generic;

namespace Portal.Core.Models.Map;

public class MapModel
{
    public string HMACObject { get; set; }
    public string PageId { get; set; }

    public string Category { get; set; }
    public string MapName { get; set; }
    public string SerializationCategory { get; set; }
    public string SerializationMapName { get; set; }

    public string Description { get; set; }

    public string PageName { get; set; }
    public bool IsPortalMapAuthor { get; set; }

    public string ProjectName { get; set; }

    public MapParameters Parameters { get; set; }

    public int CalcCrs { get; set; }

    public string Credits { get; set; }

    public bool QueryLayout { get; set; }
    public bool QueryMaster { get; set; }

    public string PortalUrl { get; set; }

    public string GdiCustomScheme { get; set; }

    public string CurrentUsername { get; set; }

    public string MapMessage { get; set; }

    public Version ShowNewsTipsSinceVesion { get; set; }

    public string HtmlMetaTags { get; set; }

    public IEnumerable<string> AddCustomCss { get; set; }
    public IEnumerable<string> AddCustomJavascript { get; set; }

    public string Language { get; set; }
}