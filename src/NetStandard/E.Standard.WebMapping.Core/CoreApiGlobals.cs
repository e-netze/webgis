using System;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core;

public class CoreApiGlobals
{
    static public ISpatialReferenceStore SRefStore { get; set; }

    static public Dictionary<string, string> StorageToolIdTranslation = new Dictionary<string, string>();

    public static double WorldRadius = 6378137D;
    public static double ToRad = Math.PI / 180.0;
    public static double ToDeg = 180.0 / Math.PI;

    public const string DefaultToolPrefix = "__default__";
}
