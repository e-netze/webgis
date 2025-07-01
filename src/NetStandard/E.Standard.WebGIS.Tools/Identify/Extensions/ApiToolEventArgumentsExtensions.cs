using E.Standard.Extensions.Compare;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Geometry;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticAssets;
using Microsoft.Identity.Client;
using System;
using System.Linq;
using System.Threading;

namespace E.Standard.WebGIS.Tools.Identify.Extensions;

static internal class ApiToolEventArgumentsExtensions
{
    public static string IdentifyUiPrefix(this ApiToolEventArguments e) => e[IdentifyDefault.IdentifyUiPrefix].OrTake("identify");
    public static string FavoritesCategoryId(this ApiToolEventArguments e) => $"{e.IdentifyUiPrefix()}-category-favorites{(e.AsDefaultTool ? "-default-tool" : "")}";
    public static string VisibleCategoryId(this ApiToolEventArguments e) => $"{e.IdentifyUiPrefix()}-category-visible{(e.AsDefaultTool ? "-default-tool" : "")}";
    public static string InVisbileCategoryId(this ApiToolEventArguments e) => $"{e.IdentifyUiPrefix()}-category-invisible{(e.AsDefaultTool ? "-default-tool" : "")}";
    public static string AllCategoryId(this ApiToolEventArguments e) => $"{e.IdentifyUiPrefix()}-category-all{(e.AsDefaultTool ? "-default-tool" : "")}";
    public static string IdentifyToolsCategoryId(this ApiToolEventArguments e) => $"{e.IdentifyUiPrefix()}-category-identifytools{(e.AsDefaultTool ? "-default-tool" : "")}";

    public static void AddAvoidRecursiveRecallsFlag(this ApiToolEventArguments e)
    {
        e["_avoid_recursive_recalls"] = "true";
    }

    public static bool HasAvoidRecursiveRecallsFlag(this ApiToolEventArguments e)
    {
        return String.IsNullOrEmpty(e["_avoid_recursive_recalls"]) == false;
    }

    public static bool UseDesktopBehavior(this ApiToolEventArguments e)
    {
        return e.HasElement("div", new[] { "query-results-tab-control-container" });
    }

    static public Shape ApplyBuffer(this ApiToolEventArguments e,
                                    Shape queryShape,
                                    SpatialReference queryShapeSref,
                                    int? calcCrsId)
    {
        if (queryShape != null &&
            e.AsDefaultTool == false &&
            e[IdentifyDefault.SketchCanApplyBufferId] == "true")
        {
            var bufferDistance = e.GetDouble(IdentifyDefault.SketchBufferDistanceId);
            if (!double.IsNaN(bufferDistance) && bufferDistance != 0.0)
            {
                switch (e.GetString(IdentifyDefault.SketchBufferUnitId))
                {
                    case "km":
                        bufferDistance *= 1000.0;
                        break;
                }

                var useTransformation = calcCrsId.HasValue && queryShapeSref != null &&
                                        calcCrsId > 0 && calcCrsId != queryShapeSref.Id;

                using (var cts = new CancellationTokenSource())
                {
                    // transform to calc sref
                    if (useTransformation)
                    {
                        using (var transformer = new GeometricTransformerPro(queryShapeSref, CoreApiGlobals.SRefStore.SpatialReferences.ById(calcCrsId.Value)))
                        {
                            transformer.Transform(queryShape);
                        }
                    }

                    queryShape = queryShape.CalcBuffer(bufferDistance, cts);

                    // and back
                    if (useTransformation)
                    {
                        using (var transformer = new GeometricTransformerPro(CoreApiGlobals.SRefStore.SpatialReferences.ById(calcCrsId.Value), queryShapeSref))
                        {
                            transformer.Transform(queryShape);
                        }
                    }
                }
            }
        }

        return queryShape;
    }

    static public bool UseAllIdentifyTools(this ApiToolEventArguments e, bool isMultiQuery, string[] identifyOptions)
        => e.AsDefaultTool == true
        && isMultiQuery == true
        && !String.IsNullOrWhiteSpace(e["identify-map-tools"])
        && identifyOptions.Contains("all-identify-tools");
    
}
