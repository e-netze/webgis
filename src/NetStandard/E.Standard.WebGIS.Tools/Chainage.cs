using E.Standard.CMS.Core;
using E.Standard.Platform;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebGIS.Tools.Identify.Abstractions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(ClientDeviceDependent = true)]
[ToolHelp("tools/identify/stat.html")]
public class Chainage : IApiServerToolAsync, IApiButtonDependency, IIdentifyTool, IApiButtonResources
{
    private const string ChainageTableId = "chainage-table";
    private const string ChainageCounter = "chainage-counter";

    #region IApiButton

    public string Container => "Abfragen";

    public bool HasUI => true;

    public string Image => UIImageButton.ToolResourceImage(this, "chainage");

    public string Name => "Stationierung";

    public string ToolTip => "Stationierung abfragen";

    #endregion

    #region IApiTool

    public ToolCursor Cursor => ToolCursor.Crosshair;

    public ToolType Type => ToolType.click;

    #endregion

    #region IButtonDependencies

    public VisibilityDependency ButtonDependencies => VisibilityDependency.ChainagethemestExists;


    #endregion

    #region IApiServerTool

    public Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        var response = new ApiEventResponse()
            .AddUIElements(
                new UIChainageThemeCombo()
                    .WithId("chainage-theme-id")
                    .AsToolParameter(),
                new UIHidden()
                    .WithId("chainage-map-scale")
                    .AsToolParameter(UICss.AutoSetterMapScale),
                new UIHidden()
                    .WithId(ChainageCounter)
                    .WithStyles(UICss.ToolParameterPersistent, UICss.ToolParameter, UICss.ToolResultCounter(this.GetType())))
            .AddUISetter(
                new UISetter(ChainageCounter,
                             !String.IsNullOrWhiteSpace(e[ChainageCounter]) ? (int.Parse(e[ChainageCounter]) + 1).ToString() : "0"));

        response.AddUIElement(new UIDiv()
                  .WithVisibilityDependency(VisibilityDependency.HasToolResults)
                  , out IUIElement div);

        if (e.DeviceInfo?.IsMobileDevice == false)
        {
            div.AddChild(CreateTable(bridge, e));
        }

        div.AddChild(new UIButtonContainer(
                    new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.removetoolqueryresults)
                        .WithStyles(UICss.CancelButtonStyle)
                        .WithText("Marker entfernen")));

        return Task.FromResult(response);
    }

    async public Task<ApiEventResponse> OnEvent(IBridge bridge, ApiToolEventArguments e)
    {
        var click = e.ToMapProjectedClickEvent();

        var chainageTheme = bridge.ChainageTheme(e["chainage-theme-id"]);
        if (chainageTheme == null)
        {
            throw new Exception($"Unknown theme: id={e["chainage-theme-id"]}");
        }

        var clickPointProjected = new Point(click.WorldX, click.WorldY);
        var featureSRef = chainageTheme.CalcSrefId > 0
            ? WebMapping.Core.CoreApiGlobals.SRefStore.SpatialReferences.ById(chainageTheme.CalcSrefId)
            : click.SRef;

        using (var transformer = new GeometricTransformerPro(click.SRef, featureSRef))
        {
            transformer.Transform(clickPointProjected);
        }

        double mapScale = e.GetDouble("chainage-map-scale");

        LengthUnit unit = LengthUnit.m;
        Enum.TryParse<LengthUnit>(chainageTheme.Unit, true, out unit);

        WebMapping.Core.Feature lineFeature = null, lowerPointFeature = null, nextPointFeature = null, nearestPointFeature = null;
        Point snappedClickPoint = null;

        double stat = 0D, stat_m = 0D,
                   lowerPointStat = 0.0, lowerPointStat_m = 0.0,
                   nextPointStat = double.MaxValue, nextPointStat_m = double.MaxValue,
                   nearestPointStat = 0.0, nearestPointStat_m = 0.0;

        string[] keyParamsExpression = Helper.GetKeyParameters(chainageTheme.Expression);

        #region Query Line theme

        double tol = 20.0 * mapScale / (96D / 0.0254);
        ApiSpatialFilter filter = new ApiSpatialFilter()
        {
            QueryGeometry = true,
            QueryShape = new Envelope(clickPointProjected.X - tol, clickPointProjected.Y - tol, clickPointProjected.X + tol, clickPointProjected.Y + tol),
            FilterSpatialReference = featureSRef,
            FeatureSpatialReference = featureSRef,
            Fields = QueryFields.All
        };
        var lineFeatures = await bridge.QueryLayerAsync(chainageTheme.ServiceId, chainageTheme.LineLayerId, filter);

        #region Find best fitting line (shortest distance)

        double dist = double.MaxValue;
        foreach (var feature in lineFeatures)
        {
            if (feature == null || !(feature.Shape is Polyline))
            {
                continue;
            }

            double d, s;
            Point p = SpatialAlgorithms.Point2PolylineDistance((Polyline)feature.Shape, new Point(clickPointProjected), out d, out s);
            if (d < dist)
            {
                dist = d;
                stat = s;
                snappedClickPoint = p;
                lineFeature = feature;
            }
        }

        #endregion

        if (lineFeature == null)
        {
            return null;
        }

        #endregion

        #region Point Theme

        var pointFeatures = new WebMapping.Core.Collections.FeatureCollection();

        if (!String.IsNullOrWhiteSpace(chainageTheme.PointLayerId) && !String.IsNullOrWhiteSpace(chainageTheme.PointLineRelation))
        {
            string where = chainageTheme.PointLineRelation;

            #region Query Points

            var keyParams = Helper.GetKeyParameters(where);
            if (keyParams != null)
            {
                foreach (string keyParam in keyParams)
                {
                    where = where.Replace("[" + keyParam + "]", lineFeature[keyParam]);
                }
            }

            int countPointFeatures = await bridge.QueryCountLayerAsync(chainageTheme.ServiceId, chainageTheme.PointLayerId, where);

            if (countPointFeatures >= 0 && countPointFeatures < 1000)
            {
                pointFeatures = await bridge.QueryLayerAsync(chainageTheme.ServiceId, chainageTheme.PointLayerId, where, QueryFields.All, featureSRef);
            }
            else
            {
                var clickPoint = new Point(clickPointProjected);

                for (int bufferDistance = 1000; bufferDistance < 50000; bufferDistance += 1000)
                {
                    var bufferFilter = new ApiSpatialFilter();
                    bufferFilter.FilterSpatialReference = featureSRef;
                    bufferFilter.FeatureSpatialReference = featureSRef;
                    bufferFilter.Fields = QueryFields.All;
                    bufferFilter.QueryGeometry = true;

                    using (var cts = new CancellationTokenSource())
                    {
                        bufferFilter.QueryShape = clickPoint.CalcBuffer(bufferDistance, cts);
                    }

                    pointFeatures = await bridge.QueryLayerAsync(chainageTheme.ServiceId, chainageTheme.PointLayerId, bufferFilter, appendFilterClause: where);

                    if (pointFeatures.Count > 0)
                    {
                        break;
                    }
                }
            }

            #endregion
        }
        else // Line M-Values if exists
        {
            if (lineFeature.Shape.HasM == true)
            {
                var points = SpatialAlgorithms.ShapePoints(lineFeature.Shape, false);

                if (points.Count >= 1000)
                {
                    for (int bufferDistance = 1000; bufferDistance < 50000; bufferDistance += 1000)
                    {
                        var neighborPoints = points.Where(p => p.Distance2D(clickPointProjected) < bufferDistance);
                        if (neighborPoints.Count() > 0)
                        {
                            points = new List<Point>(neighborPoints);
                        }
                    }
                }

                pointFeatures.AddRange(
                    points
                        .Where(p => p is PointM && ((PointM)p).M is double)
                        .Select(p =>
                        {
                            var feature = new WebMapping.Core.Feature()
                            {
                                Shape = p
                            };
                            feature.Attributes.Add(new WebMapping.Core.Attribute("M", ((double)((PointM)p).M).ToPlatformNumberString()));
                            return feature;
                        }));
            }
        }

        #region Find Lower, Next & Nearest Point

        if (pointFeatures.Count > 0)
        {
            foreach (var feature in pointFeatures)
            {
                if (feature == null)
                {
                    continue;
                }

                if (feature.Shape is MultiPoint && ((MultiPoint)feature.Shape).PointCount == 1)
                {
                    feature.Shape = ((MultiPoint)feature.Shape)[0];
                }

                if (!(feature.Shape is Point))
                {
                    continue;
                }

                double d, s;
                Point p = SpatialAlgorithms.Point2PolylineDistance((Polyline)lineFeature.Shape, (Point)feature.Shape, out d, out s);
                if (d >= 1.0) // Punkt liegt nicht auf der Linie, Punkte müssen auf mind 1m gesnappt sein!!!
                {
                    continue;
                }

                if (s >= lowerPointStat && s < stat)
                {
                    lowerPointStat = s;
                    lowerPointFeature = feature;
                }
                else if (s <= nextPointStat && s > stat)
                {
                    nextPointStat = s;
                    nextPointFeature = feature;
                }
                if (nearestPointFeature == null)
                {
                    nearestPointFeature = feature;
                    nearestPointStat = s;
                }
                else if (Math.Abs(nearestPointStat - stat) > Math.Abs(s - stat))
                {
                    nearestPointFeature = feature;
                    nearestPointStat = s;
                }
            }
        }

        #endregion

        #endregion

        #region Check line direction

        double lowerPointStatAttr = 0.0, nextPointStatAttr = 0.0, direction = 1.0, scale = 1.0;
        if (!String.IsNullOrEmpty(chainageTheme.PointStatField) &&
            lowerPointFeature != null && nextPointFeature != null)
        {
            lowerPointStatAttr = lowerPointFeature[chainageTheme.PointStatField].ToPlatformDouble();
            nextPointStatAttr = nextPointFeature[chainageTheme.PointStatField].ToPlatformDouble();

            try
            {
                double attrLen = Math.Abs(nextPointStatAttr - lowerPointStatAttr);
                switch (unit)
                {
                    case LengthUnit.km:
                        attrLen *= 1000.0;
                        break;
                    case LengthUnit.cm:
                        attrLen /= 100.0;
                        break;
                    case LengthUnit.dm:
                        attrLen /= 10.0;
                        break;
                }
                scale = Math.Abs(nextPointStat - lowerPointStat) / attrLen;
            }
            catch
            {
                scale = 1.0;
            }
            if (nextPointStatAttr < lowerPointStatAttr)  // Richtung der Linie ist nicht in Stationierungsrichtung digitalisiert!!
            {
                direction = -1.0;
                var lPointFeature = lowerPointFeature;
                double lPointStat = lowerPointStat;
                lowerPointFeature = nextPointFeature;
                lowerPointStat = nextPointStat;
                nextPointFeature = lPointFeature;
                nextPointStat = lPointStat;
            }
        }

        #endregion

        string message = String.Empty;

        if (snappedClickPoint != null)
        {
            #region Calc Stat

            stat_m = stat;
            lowerPointStat_m = lowerPointStat;
            nextPointStat_m = nextPointStat;
            nearestPointStat_m = nearestPointStat;


            switch (unit)
            {
                case LengthUnit.km:
                    stat /= 1000.0;
                    lowerPointStat /= 1000.0;
                    nearestPointStat /= 1000.0;
                    nextPointStat /= 1000.0;
                    break;
                case LengthUnit.cm:
                    stat *= 100.0;
                    lowerPointStat *= 100.0;
                    nearestPointStat *= 100.0;
                    nextPointStat *= 100.0;
                    break;
                case LengthUnit.dm:
                    stat *= 10.0;
                    lowerPointStat *= 10.0;
                    nearestPointStat *= 10.0;
                    nextPointStat *= 10.0;
                    break;
            }

            double lowerDiffStat = stat * direction, nearestDiffStat = stat * direction;
            double lowerDiffStat_m = stat_m * direction, nearestDiffStat_m = stat_m * direction;

            if (lowerPointFeature != null)
            {
                lowerDiffStat = (stat - lowerPointStat) * direction;
                lowerDiffStat_m = (stat_m - lowerPointStat_m) * direction;
            }
            if (nearestPointFeature != null)
            {
                nearestDiffStat = (stat - nearestPointStat) * direction;
                nearestDiffStat_m = (stat_m - nearestPointStat_m) * direction;
            }

            #endregion

            #region Evalute Expression

            message = String.Format(chainageTheme.Expression, stat, lowerDiffStat, nearestDiffStat,
                                                                                stat_m, lowerDiffStat_m, nearestDiffStat_m,
                                                                                stat_m / 1000.0, lowerDiffStat_m / 1000.0, nearestDiffStat_m / 1000.0,
                                                                                stat_m / 100.0, lowerDiffStat_m / 100.0, nearestDiffStat_m / 100.0,
                                                                                stat_m / 10.0, lowerDiffStat_m / 10.0, nearestDiffStat_m / 10.0,
                                                                                scale
                                                                                );
            if (keyParamsExpression != null)
            {
                foreach (string keyParam in keyParamsExpression)
                {
                    if (keyParam.ToLower().StartsWith("l.") && lineFeature != null)
                    {
                        message = message.Replace("[" + keyParam + "]", lineFeature[keyParam.Substring(2, keyParam.Length - 2)]);
                    }
                    else if (keyParam.ToLower().StartsWith("p.") && lowerPointFeature != null)
                    {
                        message = message.Replace("[" + keyParam + "]", lowerPointFeature[keyParam.Substring(2, keyParam.Length - 2)]);
                    }
                    else if (keyParam.ToLower().StartsWith("n.") && nearestPointFeature != null)
                    {
                        message = message.Replace("[" + keyParam + "]", nearestPointFeature[keyParam.Substring(2, keyParam.Length - 2)]);
                    }
                    else if (lowerPointFeature != null)
                    {
                        message = message.Replace("[" + keyParam + "]", lowerPointFeature[keyParam]);
                    }
                    else
                    {
                        message = message.Replace("[" + keyParam + "]", "???");
                    }
                }
            }
            message = WebMapping.Core.Eval.ParseEvalExpression(message);
            message = message.Replace("\\n", "\n");

            #endregion
        }

        if (!String.IsNullOrWhiteSpace(message))
        {
            #region Transform snapped point to WGS84 

            if (featureSRef != null && featureSRef.Id != 4326)
            {
                using (var transformer = bridge.GeometryTransformer(featureSRef.Id, 4326))
                {
                    transformer.Transform(snappedClickPoint);
                }
            }

            #endregion

            int counter = 0;
            int.TryParse(e[ChainageCounter], out counter);
            counter++;

            var features = new WebMapping.Core.Collections.FeatureCollection();


            var feature = new WebMapping.Core.Feature()
            {
                Shape = snappedClickPoint,
                GlobalOid = counter.ToCoordinatesGlobalOid()
            };

            string messageText = message;

            feature.Attributes.Add(new WebMapping.Core.Attribute("_fIndex", counter.ToString()));
            feature.Attributes.Add(new WebMapping.Core.Attribute("_fulltext", $"<strong>Kilometerwert:</strong><br/>{messageText.Replace("\n", "<br/>")}"));
            features.Add(feature);

            List<string> values = new List<string>(new string[]
            {
                counter.ToString(),
                Convert.ToBase64String(Encoding.UTF8.GetBytes(messageText))
            });
            List<IUIElement> cols = new List<IUIElement>(new IUIElement[]
            {
                new UILiteral()
                    .WithLiteral(counter.ToString()),
                new UILiteral()
                    .WithLiteral(messageText, true)
            });

            return new ApiFeaturesEventResponse(this)
                .AddFeatures(features, FeatureResponseType.Append)
                .AddClickEvent(click)
                .ZoomToFeaturesResult(false)
                .UIElementsBehavoir(AppendUIElementsMode.Append)
                .AddUIElement(
                    new UITable(new UITableRow(cols.ToArray(), values: values.ToArray())
                                        .WithStyles(UICss.ToolResultElement(this.GetType())))
                    {
                        InsertTypeValue = UITable.TableInsertType.Append
                    }.WithId(ChainageTableId))
                .AddUISetter(new UISetter(ChainageCounter, counter.ToString()));
        }

        return null;
    }

    #endregion

    #region Server Commands

    [ServerToolCommand("remove-feature")]
    public ApiEventResponse OnRemove(IBridge bridge, ApiToolEventArguments e)
    {
        var featureOid = e["feature-oid"];
        var counter = featureOid.GetCounterFromCoordiantesGlobalOid();

        int.TryParse(e[ChainageCounter], out int currentCounter);

        var response = new ApiEventResponse()
            .UIElementsBehavoir(AppendUIElementsMode.Append)
            .AddUIElement(CreateTable(bridge, e, new int[] { counter }));

        if (currentCounter == counter)
        {
            response.AddUISetter(new UISetter(ChainageCounter, (--counter).ToString()));
        }

        return response;
    }

    #endregion

    #region IApiButtonResources Member

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("chainage", E.Standard.WebGIS.Tools.Properties.Resources.chainage);
    }

    #endregion

    #region IIdenfify 

    async public Task<IEnumerable<CanIdentifyResult>> CanIdentifyAsync(IBridge bridge, Point point, double scale, string[] availableServiceIds = null, string[] availableQueryIds = null)
    {
        if (availableServiceIds != null)
        {
            List<CanIdentifyResult> result = new List<CanIdentifyResult>();

            double tol = 20.0 * scale / (96D / 0.0254), R = 6378137;
            tol = tol / R * 180D / Math.PI;  // Tolerance to 

            ApiSpatialFilter filter = new ApiSpatialFilter()
            {
                QueryShape = new Envelope(point.X - tol, point.Y - tol, point.X + tol, point.Y + tol),
                FilterSpatialReference = bridge.CreateSpatialReference(SpatialReference.ID_WGS84),
                QueryGeometry = false,
                Fields = QueryFields.Id
            };
            foreach (var serviceId in availableServiceIds)
            {
                foreach (var chainageTheme in bridge.ServiceChainageThemes(serviceId))
                {
                    var lineFeatures = await bridge.QueryLayerAsync(serviceId, chainageTheme.LineLayerId, filter);
                    if (lineFeatures.Count > 0)
                    {
                        result.Add(new CanIdentifyResult()
                        {
                            Name = chainageTheme.Name,
                            ToolParameters = "chainage-theme-id=" + chainageTheme.Id + ";chainage-map-scale=" + (int)scale,
                            Count = 1
                        });
                    }
                }
            }

            return result.Count > 0 ? result : null;
        }

        return null;
    }

    #endregion

    #region Helper

    private UITable CreateTable(IBridge bridge, ApiToolEventArguments e, IEnumerable<int> removeRows = null)
    {
        var tableData = e[ChainageTableId];

        List<IUIElement> header = new List<IUIElement>(new IUIElement[]
            {
                new UILiteral().WithLiteral("#"),
                new UILiteral().WithLiteral("Kilometerwert")
            });

        var table = new UITable(new UITableRow(header.ToArray(), isHeader: true))
        {
            InsertTypeValue = UITable.TableInsertType.Replace
        }.WithId(ChainageTableId)
         .WithStyles(UICss.ToolParameter, UICss.ToolParameterPersistent, UICss.TableAlternateRowColor);

        int colCount = header.Count();

        if (!String.IsNullOrWhiteSpace(tableData))
        {
            var data = tableData.Split(';');

            for (int row = 0; row < data.Length - (colCount - 1); row += colCount)
            {
                var number = data[row];
                var textBase64 = data[row + 1];

                if (removeRows != null && removeRows.Contains(int.Parse(number)))
                {
                    continue;
                }

                var cols = new List<IUIElement>(new UIElement[]
                {
                        new UILiteral()
                            .WithLiteral(number),
                        new UILiteral()
                            .WithLiteral(Encoding.UTF8.GetString(Convert.FromBase64String(textBase64)), true)
                });
                var values = new List<string>(new string[]{
                        number,
                        textBase64
                    });

                table.AddRow(new UITableRow(cols.ToArray(), values: values.ToArray())
                                    .WithStyles(UICss.ToolResultElement(this.GetType())));
            }
        }

        return table;
    }

    #endregion
}
