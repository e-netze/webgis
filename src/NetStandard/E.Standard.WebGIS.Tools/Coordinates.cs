using E.Standard.Extensions.Compare;
using E.Standard.Extensions.Security;
using E.Standard.GeoJson.Extensions;
using E.Standard.Localization.Abstractions;
using E.Standard.Platform;
using E.Standard.WebGIS.Core;
using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebGIS.Tools.Helpers;
using E.Standard.WebGIS.Tools.Identify.Abstractions;
using E.Standard.WebGIS.Tools.MapMarkup.Export;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;
using E.Standard.WebMapping.Core.Api.UI.Setters;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static E.Standard.WebMapping.Core.Api.ApiToolEventArguments;
using static E.Standard.WebMapping.Core.CoreApiGlobals;

namespace E.Standard.WebGIS.Tools;

[Export(typeof(IApiButton))]
[AdvancedToolProperties(ClientDeviceDependent = true, MapCrsDependent = true, MapBBoxDependent = true)]
[ToolConfigurationSection("coordinates")]
[ToolHelp("tools/identify/coords.html")]
public class Coordinates : IApiServerToolLocalizableAsync<Coordinates>, 
                           IApiButtonResources, 
                           IIdentifyTool
{
    internal const string CoordinatesTableId = "coordinates-table";
    internal const string CoordinatesCounter = "coordinates-counter";

    #region IApiServerTool Member

    public Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e, ILocalizer<Coordinates> localizer)
    {
        var response = new ApiEventResponse();

        //List<IUIElement> uiElements = new List<IUIElement>();
        //List<IUISetter> uiSetters = new List<IUISetter>();

        response.AddUIElement(
            new UIHidden()
                .WithId("coordinates-map-srs")
                .WithStyles(UICss.AutoSetterMapCrsId, UICss.ToolParameter));

        if (!e.DeviceInfo.IsMobileDevice)
        {
            response.AddUIElements(
                new UIHidden()
                    .WithId(CoordinatesCounter)
                    .WithStyles(UICss.ToolParameterPersistent, UICss.ToolParameter, UICss.ToolResultCounter(this.GetType())),
                new UIButton(UIButton.UIButtonType.servertoolcommand, "inputcoordinates-dialog")
                    .WithStyles(UICss.OptionRectButtonStyle)
                    .WithText(localizer.Localize("enter-coordinates"))
                    .WithIcon(UIButton.ToolResourceImage(this.GetType(), "calculator")),
                new UIButton(UIButton.UIButtonType.servertoolcommand, "upload")
                    .WithStyles(UICss.OptionRectButtonStyle)
                    .WithText(localizer.Localize("upload-coordinates"))
                    .WithIcon(UIButton.ToolResourceImage(this.GetType(), "upload")),
                new UIButton(UIButton.UIButtonType.servertoolcommand, "download")
                    .WithStyles(UICss.OptionRectButtonStyle)
                    .WithText(localizer.Localize("download-coordinates"))
                    .WithIcon(UIButton.ToolResourceImage(this.GetType(), "download"))
                    .WithVisibilityDependency(VisibilityDependency.HasToolResults));
        }
        else
        {
            response.AddUIElement(
                new UIButtonContainer()
                    .AddChild(new UIButton(UIButton.UIButtonType.servertoolcommand, "inputcoordinates-dialog")
                                    .WithStyles(UICss.DefaultButtonStyle)
                                    .WithText(localizer.Localize("enter-coordinates"))));
        }

        response.AddUIElement(new UIDiv()
                  .WithVisibilityDependency(VisibilityDependency.HasToolResults)
                  , out IUIElement div);

        if (!e.DeviceInfo.IsMobileDevice)
        {
            var defaultProjCombo = ProjectionsComboElement(bridge, "coordinates-default-proj", true);
            defaultProjCombo.changetype = UIButton.UIButtonType.servertoolcommand.ToString();
            defaultProjCombo.changecommand = "change-default-projection";

            div.AddChild(defaultProjCombo);
            response.AddUISetters(
                new UISetter("coordinates-default-proj",
                             !String.IsNullOrWhiteSpace(e["coordinates-default-proj"]) ? e["coordinates-default-proj"] : (e.MapCrs.HasValue ? e.MapCrs.Value : 0).ToString()),
                new UISetter(CoordinatesCounter,
                             !String.IsNullOrWhiteSpace(e[CoordinatesCounter]) ? (int.Parse(e[CoordinatesCounter]) + 1).ToString() : "0"));

            div.AddChild(CreateTable(bridge, e));
        }

        div.AddChild(new UIButtonContainer()
                .AddChild(new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.removetoolqueryresults)
                                .WithStyles(UICss.CancelButtonStyle)
                                .WithText(localizer.Localize("remove-markers"))));

        response.AddUIElement(new UICollapsableHelp(
                                    localizer.Localize("tip-label"), 
                                    localizer.Localize("tip:body")
                              ));

        return Task.FromResult(response);
    }

    async public Task<ApiEventResponse> OnEvent(IBridge bridge, ApiToolEventArguments e, ILocalizer<Coordinates> localizer)
    {
        return await ClickEventResponse(bridge, e, e.ToClickEvent());
    }

    #endregion

    #region IApiTool Member

    public ToolType Type
    {
        get
        {
            return ToolType.sketch0d; // ToolType.click;
        }
    }

    public ToolCursor Cursor
    {
        get
        {
            return ToolCursor.Custom_Pen;
        }
    }

    #endregion

    #region IApiButton Member

    public string Name => "Coordinates / Elevation";

    public string Image => UIImageButton.ToolResourceImage(this, "coords-xyz");

    public string Container => "Query";

    public string ToolTip => "Query coordinates and elevation values";

    public bool HasUI { get { return true; } }

    #endregion

    #region Server Commands

    [ServerEventHandler(ServerEventHandlers.OnVertexAdded)]  // wird aufgerufen wenn ToolType = Sektch0d => beim setzen eines Vertex
    async public Task<ApiEventResponse> OnVertexAdded(IBridge bridge, ApiToolEventArguments e, ILocalizer<Coordinates> localizer)
    {
        var response = await OnEvent(bridge, e, localizer);
        response.RemoveSketch = true;

        return response;
    }

    [ServerToolCommand("inputcoordinates-dialog")]
    public ApiEventResponse OnInputCoordinatesDialog(IBridge bridge, ApiToolEventArguments e, ILocalizer<Coordinates> localizer)
    {
        var lastTableCoord = e.GetWGSFormCoordinatesTable(bridge)
            .LastOrDefault();

        if (lastTableCoord == null)
        {
            var bbox = e.GetArray<double>("_mapbbox");
            lastTableCoord = new Point((bbox[0] + bbox[2]) * 0.5, (bbox[1] + bbox[3]) * 0.5);
        }

        var sRefId = int.Parse(!String.IsNullOrWhiteSpace(e["coordinates-input-proj"]) ? e["coordinates-input-proj"] : e["coordinates-map-srs"]);

        var sRef = bridge.CreateSpatialReference(sRefId);
        var sRef4326 = bridge.CreateSpatialReference(4326);

        using (var transform = new GeometricTransformerPro(sRef4326, sRef))
        {
            transform.Transform(lastTableCoord);
        }

        RoundCoordiantes(lastTableCoord, sRef);

        var projCombo = ProjectionsComboElement(bridge, "coordinates-input-proj");
        projCombo.changetype = UIButton.UIButtonType.servertoolcommand.ToString();
        projCombo.changecommand = "inputcoordinates-dialog-change-projection";

        return new ApiEventResponse()
            .AddUIElement(
                new UIDiv()
                    .WithTarget(UIElementTarget.modaldialog)
                    .WithTargetTitle(localizer.Localize("enter-coordinates"))
                    .WithStyles(UICss.NarrowFormMarginAuto)
                    .AddChildren(
                        new UILabel().WithLabel(localizer.Localize("coordinate-system")),
                        projCombo,
                        new UIBreak(),
                        new UILabel().WithLabel(localizer.Localize("easting")),
                        new UIInputText()
                            .WithId("coordinates-input-x-value")
                            .WithStyles(UICss.ToolParameter/*, UICss.ToolParameterPersistent*/),
                        //.WithValue(e["coordinates-input-x-value"]),
                        new UIBreak(),
                        new UILabel().WithLabel(localizer.Localize("northing")),
                        new UIInputText()
                            .WithId("coordinates-input-y-value")
                            .WithStyles(UICss.ToolParameter/*, UICss.ToolParameterPersistent*/),
                        //.WithValue(e["coordinates-input-y-value"]),
                        new UIBreak(),
                        new UIDiv()
                            .WithId("coordinates-input-validation-error")
                            .WithStyles("webgis-info", "error", "webgis-display-none"),
                        new UIBreak(),
                        new UIButton(UIButton.UIButtonType.servertoolcommand, "showcoordinates")
                                .WithText(localizer.Localize("show-coordinates")),
                        new UIHidden()
                                .AsToolParameter()
                                .WithId("coordinates-input-current-srs")
                ))
            .AddUISetter(new UISetter("coordinates-input-x-value", lastTableCoord.X.ToString()))
            .AddUISetter(new UISetter("coordinates-input-y-value", lastTableCoord.Y.ToString()))
            .AddUISetter(new UISetter("coordinates-input-proj", sRefId.ToString()))
            .AddUISetter(new UISetter("coordinates-input-current-srs", sRefId.ToString()));
    }

    [ServerToolCommand("inputcoordinates-dialog-change-projection")]
    public ApiEventResponse OnChangeInputCombo(IBridge bridge, ApiToolEventArguments e)
    {
        if (string.IsNullOrWhiteSpace(e["coordinates-input-x-value"]) ||
            string.IsNullOrWhiteSpace(e["coordinates-input-x-value"]))
        {
            return null;
        }

        var newSrs = bridge.CreateSpatialReference(int.Parse(e["coordinates-input-proj"]));
        var currentSrs = bridge.CreateSpatialReference(int.Parse(e["coordinates-input-current-srs"]));

        try
        {
            var currentPoint = new Point(ParseCoordinateValue(e["coordinates-input-x-value"]), ParseCoordinateValue(e["coordinates-input-y-value"]));

            using (var transformer = new GeometricTransformerPro(currentSrs, newSrs))
            {
                transformer.Transform(currentPoint);
            }

            RoundCoordiantes(currentPoint, newSrs);

            return new ApiEventResponse()
                .AddUISetter(new UISetter("coordinates-input-x-value", currentPoint.X.ToString()))
                .AddUISetter(new UISetter("coordinates-input-y-value", currentPoint.Y.ToString()))
                .AddUISetter(new UISetter("coordinates-input-current-srs", newSrs.Id.ToString()))
                .AddUISetter(new UISetter("coordinates-input-validation-error", ""))
                .AddUISetter(new UICssSetter(UICssSetter.SetterType.AddClass, "coordinates-input-validation-error", "webgis-display-none"));
        }
        catch (Exception ex)
        {
            return new ApiEventResponse()
                .AddUISetter(new UISetter("coordinates-input-proj", currentSrs.Id.ToString()))
                .AddUISetter(new UISetter("coordinates-input-validation-error", ex.Message))
                .AddUISetter(new UICssSetter(UICssSetter.SetterType.RemoveClass, "coordinates-input-validation-error", "webgis-display-none"));
        }
    }

    [ServerToolCommand("showcoordinates")]
    async public Task<ApiEventResponse> OnShowCoordinates(IBridge bridge, ApiToolEventArguments e)
    {
        try
        {
            double x = ParseCoordinateValue(e["coordinates-input-x-value"]);
            double y = ParseCoordinateValue(e["coordinates-input-y-value"]);
            var sRef = bridge.CreateSpatialReference(int.Parse(e["coordinates-input-proj"]));
            double lng = x, lat = y;

            if (sRef.Id != 4326)
            {
                var sRef4326 = bridge.CreateSpatialReference(4326);

                GeometricTransformer.Transform2D(
                    ref lng, ref lat,
                    sRef.Proj4, !sRef.IsProjective,
                    sRef4326.Proj4, !sRef4326.IsProjective);
            }

            var click = new ApiToolEventClick()
            {
                Longitude = lng,
                Latitude = lat,
                WorldX = x,
                WorldY = y,
                SRef = sRef
            };

            return (await ClickEventResponse(bridge, e, click, true))
                           .CloseUIDialog(UIElementTarget.modaldialog);
        }
        catch (Exception ex)
        {
            return new ApiEventResponse()
                .AddUISetter(new UISetter("coordinates-input-validation-error", ex.Message))
                .AddUISetter(new UICssSetter(UICssSetter.SetterType.RemoveClass, "coordinates-input-validation-error", "webgis-display-none"));
        }
    }

    [ServerToolCommand("inputcoordinates-sketch-xyabsolute")]
    public ApiEventResponse OnInputCoordinates(IBridge bridge, ApiToolEventArguments e, ILocalizer<Coordinates> localizer)
    {
        return new ApiEventResponse()
            .AddUIElements(
                new UILabel()
                    .WithLabel(localizer.Localize("coordinate-system")),
                ProjectionsComboElement(bridge, "coordinates-input-proj"),
                new UIBreak(),
                new UILabel()
                    .WithLabel(localizer.Localize("easting")),
                new UIInputText()
                    .WithId("coordinates-input-x-value")
                    .AsPersistantToolParameter()
                    .WithValue(e["coordinates-input-x-value"]),
                new UIBreak(),
                new UILabel()
                    .WithLabel(localizer.Localize("northing")),
                new UIInputText()
                    .WithId("coordinates-input-y-value")
                    .AsPersistantToolParameter()
                    .WithValue(e["coordinates-input-y-value"]),
                new UIBreak(2),
                new UIButton(UIButton.UIButtonType.servertoolcommand_ext, "setsketchvertex")
                    .WithId("webgis.tools.coordinates")
                    .WithText(localizer.Localize("apply-coordinates")))
            .AddUISetter(
                new UISetter("coordinates-input-proj",
                             !String.IsNullOrWhiteSpace(e["coordinates-input-proj"]) ? e["coordinates-input-proj"] : e["coordinates-map-srs"]));
    }

    [ServerToolCommand("setsketchvertex")]
    public ApiEventResponse OnSetSketchVertex(IBridge bridge, ApiToolEventArguments e)
    {
        double x = ParseCoordinateValue(e["coordinates-input-x-value"]);
        double y = ParseCoordinateValue(e["coordinates-input-y-value"]);
        var sRef = bridge.CreateSpatialReference(int.Parse(e["coordinates-input-proj"]));
        double lng = x, lat = y;

        if (sRef.Id != 4326)
        {
            var sRef4326 = bridge.CreateSpatialReference(4326);

            GeometricTransformer.Transform2D(
                ref lng, ref lat,
                sRef.Proj4, !sRef.IsProjective,
                sRef4326.Proj4, !sRef4326.IsProjective);
        }

        return new ApiEventResponse()
            .AddVertexToCurrentSketch(new Point(lng, lat))
            .CloseUIDialog(UIElementTarget.modaldialog);
    }

    [ServerToolCommand("change-default-projection")]
    public ApiEventResponse OnChangeDefaultProjection(IBridge bridge, ApiToolEventArguments e)
    {
        return new ApiEventResponse()
            .UIElementsBehavoir(AppendUIElementsMode.Append)
            .AddUIElement(CreateTable(bridge, e));
    }

    [ServerToolCommand("download")]
    public ApiEventResponse OnDownload(IBridge bridge, ApiToolEventArguments e, ILocalizer<Coordinates> localizer)
    {
        var defaultCrs = !String.IsNullOrWhiteSpace(e["coordinates-default-proj"]) ? e["coordinates-default-proj"] : (e.MapCrs.HasValue ? e.MapCrs.Value : 0).ToString();
        var tableData = e[CoordinatesTableId];
        string separator = ";";

        if (!String.IsNullOrWhiteSpace(tableData))
        {
            var data = tableData.Split(';');
            var projection = Projections(bridge).Where(p => $"{p.Id}{p.DisplayStyle}" == defaultCrs)
                                                .FirstOrDefault();

            var heightColumns = new RasterQueryHelper().HeightNameNodes($"{bridge.AppEtcPath}/coordinates/h.xml");
            var colCount = 3 + heightColumns.Count();

            StringBuilder csv = new StringBuilder();
            csv.Append($"#{separator}Rechtwert{separator}Hochwert");

            #region Zusätzliche Spalten (Höhen)

            foreach (string heightColumn in heightColumns)
            {
                csv.Append($"{separator}{heightColumn}");
            }

            #endregion

            csv.Append(Environment.NewLine);

            if (data.Length % colCount == 0 && projection != null)
            {
                var sRef = SRefStore.SpatialReferences.ById(4326);
                var toSRef = SRefStore.SpatialReferences.ById(projection.Id);

                for (int row = 0; row < data.Length - (colCount - 1); row += colCount)
                {
                    var number = data[row];
                    double worldX = data[row + 1].ToPlatformDouble();
                    double worldY = data[row + 2].ToPlatformDouble();

                    var projectResult = Project(projection, defaultCrs, sRef, toSRef, worldX, worldY);

                    csv.Append($"{number}{separator}{projectResult.xString}{separator}{projectResult.yString}");

                    #region Zusätzliche Spalten (Höhen)

                    for (int i = 3; i < colCount; i++)
                    {
                        csv.Append($"{separator}{data[row + i]}");
                    }

                    #endregion

                    csv.Append(Environment.NewLine);
                }

                var encoding = bridge.DefaultTextEncoding;  // UTF8-BOM => Zeichen wie (Gradzeichen) werden richtig ins Excel übernommen     
                return new ApiRawDownloadEventResponse("coordinates.csv", encoding.GetBytes(csv.ToString()));
            }
        }

        throw new Exception(localizer.Localize("exception-no-points-found"));
    }

    [ServerToolCommand("upload")]
    public ApiEventResponse OnUpload(IBridge bridge, ApiToolEventArguments e, ILocalizer<Coordinates> localizer)
    {
        return new ApiEventResponse()
            .AddUIElement(new UIDiv()
                .AsDialog()
                .WithDialogTitle("Hochladen (CSV)")
                .WithStyles(UICss.NarrowFormMarginAuto)
                .AddChildren(
                    new UILabel()
                        .WithLabel(localizer.Localize("upload.label1:body")),
                    new UIBreak(2),
                    new UILabel()
                        .WithLabel($"{localizer.Localize("upload.label2:body")}:"),
                    ProjectionsComboElement(bridge, "coordinates-upload-projection", false),
                    new UIBreak(2),
                    new UIUploadFile(this.GetType(), "upload-file")
                        .WithId("upload-file")
                        .AsToolParameter()))
            .AddUISetter(new UISetter("coordinates-upload-projection",
                                      !String.IsNullOrWhiteSpace(e["coordinates-default-proj"]) ? e["coordinates-default-proj"] : (e.MapCrs.HasValue ? e.MapCrs.Value : 0).ToString()));
    }

    [ServerToolCommand("upload-file")]
    async public Task<ApiEventResponse> OnUploadFile(IBridge bridge, ApiToolEventArguments e, ILocalizer<Coordinates> localizer)
    {
        var file = e.GetFile("upload-file");
        int uploadCrs = e.GetInt("coordinates-upload-projection");

        string defaultProject = e.DeviceInfo.IsMobileDevice ? "" : e["coordinates-default-proj"];

        int counter = 0;

        if (file != null)
        {
            var encoding = file.Data.DetectTextByteArrayEncoding() ?? bridge.DefaultTextEncoding;
            var csv = encoding.GetString(file.Data);
            var rows = csv.Replace("\r", "").Split('\n')
                                            .Where(r => !String.IsNullOrWhiteSpace(r))
                                            .ToArray();

            int maxRows = e.GetConfigInt("allow-upload-max-rows", 10);
            if (rows.Length - 1 > maxRows)  // -1 weil erste Zeile Titelzeile
            {
                throw new Exception(localizer.Localize(String.Format("upload.exception-too-many-points", maxRows)));
            }

            var sRef = SRefStore.SpatialReferences.ById(uploadCrs);
            var sRef4326 = SRefStore.SpatialReferences.ById(4326);

            var projections = Projections(bridge);
            var features = new WebMapping.Core.Collections.FeatureCollection();
            StringBuilder tableData = new StringBuilder();

            for (int i = 1; i < rows.Length; i++)
            {
                if (String.IsNullOrWhiteSpace(rows[i]))
                {
                    continue;
                }

                string[] cells = null;

                foreach (var c in new char[] { ';', '\t', ' ' })
                {
                    cells = rows[i].Split(c);
                    if (cells.Length >= 2)
                    {
                        break;
                    }
                }

                // Avoid Reflected XSS ... <script... etc
                cells.CheckSecurity();

                string xString, yString, number;
                if (cells.Length == 2)
                {
                    number = i.ToString();
                    xString = cells[0];
                    yString = cells[1];
                }
                else if (cells.Length >= 3)
                {
                    number = cells[0];
                    xString = cells[1];
                    yString = cells[2];
                }
                else
                {
                    throw new Exception(String.Format(localizer.Localize("exception-invalid-row"), rows[i]));
                }

                if (int.TryParse(number, out int numberCounter))
                {
                    counter = numberCounter;
                }
                else
                {
                    counter = Math.Max(counter, i);
                }

                double x = ParseCoordinateValue(xString), lng = x;
                double y = ParseCoordinateValue(yString), lat = y;

                GeometricTransformer.Transform2D(
                        ref lng, ref lat,
                        sRef.Proj4, !sRef.IsProjective,
                        sRef4326.Proj4, !sRef4326.IsProjective);

                var point = new Point(lng, lat);
                var feature = new WebMapping.Core.Feature()
                {
                    Shape = point,
                    GlobalOid = counter.ToCoordinatesGlobalOid()
                };

                string defaultXString = String.Empty, defaultYString = String.Empty;
                foreach (var projection in projections)
                {
                    var toSRef = SRefStore.SpatialReferences.ById(projection.Id);
                    if (toSRef == null)
                    {
                        continue;
                    }

                    var projectionResult = Project(projection, defaultProject, sRef, toSRef, x, y);
                    defaultXString = projectionResult.defaultXString ?? defaultXString;
                    defaultYString = projectionResult.defaultYString ?? defaultYString;

                    feature.Attributes.Add(new WebMapping.Core.Attribute(projection.DisplayName, new string[] { projectionResult.xString, projectionResult.yString }));
                }

                feature.Attributes.Add(new WebMapping.Core.Attribute("_fIndex", counter.ToString()));

                features.Add(feature);

                if (tableData.Length > 0)
                {
                    tableData.Append(";");
                }

                tableData.Append($"{number};{lng};{lat}");

                foreach (var heightResult in await new RasterQueryHelper().PerformHeightQueryAsync(bridge, point, $"{bridge.AppEtcPath}/coordinates/h.xml"))
                {
                    feature.Attributes.Add(new WebMapping.Core.Attribute(heightResult.Name, new string[] { heightResult.ResultString, String.Empty }));
                    tableData.Append($";{heightResult.ResultString}");
                }
            }

            e[CoordinatesTableId] = tableData.ToString();

            var tableElement = CreateTable(bridge, e);
            tableElement.InsertTypeValue = UITable.TableInsertType.Replace;

            return new ApiFeaturesEventResponse(this)
                .AddFeatures(features, FeatureResponseType.New)
                .UIElementsBehavoir(AppendUIElementsMode.Append)
                .AddUIElement(tableElement)
                .AddUISetter(new UISetter(CoordinatesCounter, counter.ToString()))
                .CloseUIDialog(UIElementTarget.modaldialog);
        }

        return null;
    }

    [ServerToolCommand("upload-sketch")]
    public ApiEventResponse OnUploadSketch(IBridge bridge, ApiToolEventArguments e, ILocalizer<Coordinates> localizer)
    {
        return new ApiEventResponse()
            .AddUIElements(
                new UIHidden()
                    .WithId("upload-sketch-geometry-type")
                    .AsToolParameter()
                    .WithValue(e["sketch-geometry-type"]),
                new UILabel()
                    .WithLabel(localizer.Localize("upload.sketch.label1")),
                new UIBreak(2),
                new UIUploadFile(this.GetType(), "upload-sketch-file")
                    .WithId("upload-sketch-file")
                    .AsToolParameter());
    }

    [ServerToolCommand("upload-sketch-file")]
    public ApiEventResponse OnUploadSketchFile(IBridge bridge, ApiToolEventArguments e)
    {
        var file = e.GetFile("upload-sketch-file");
        string geometryType = e["upload-sketch-geometry-type"];
        string[] acceptedGeoJsonGeometryTypes = new string[0];
        int sketchCrs = e.CalcCrs.OrTake(e.MapCrs).Value;

        switch (geometryType)
        {
            case "line":
            case "polyline":
            case "hectoline":
            case "dimline":
                acceptedGeoJsonGeometryTypes = new string[] { "linestring", "multilinestring" };
                break;
            case "polygon":
                acceptedGeoJsonGeometryTypes = new string[] { "polygon", "multipolygon" };
                break;
        }

        var geoJsonFeatures = file.GetFeatures(e, coordinatesToDoubleArray: true, setNameProperty: true);
        geoJsonFeatures.Features = geoJsonFeatures?
                                            .Features?
                                            .Where(f => f.Geometry?.coordinates != null &&
                                                       acceptedGeoJsonGeometryTypes.Contains(f.Geometry.type?.ToLower()))
                                            .ToArray();

        if (geoJsonFeatures?.Features?.Length.OrTake(0) == 0)
        {
            throw new Exception(String.Format("upload.sketch.exception-no-geometry-candidates", geometryType));
        }

        var epsg = geoJsonFeatures.Crs.TryGetEpsg().OrTake(Epsg.WGS84);

        return new ApiEventResponse()
            .AddNamedSketches(
                geoJsonFeatures.Features
                    .Select(f => new WebMapping.Core.Api.EventResponse.Models.NamedSketch()
                        .WithName(f.GetPropery<string>("name"))
                        .WithSubText($"{geometryType} aus {file.FileName} übernehmen")
                        .DoZoomToPreview()
                        .WithShape(f.ToShape()?.Project(sketchCrs,
                                                        defaultSourceSrs: epsg,
                                                        appendShapeProperterties: ShapeSrsProperties.SrsId | ShapeSrsProperties.SrsProj4Parameters))))
            .CloseUIDialog(UIElementTarget.modaldialog);
    }

    [ServerToolCommand("download-sketch")]
    public ApiEventResponse OnDownloadSketch(IBridge bridge, ApiToolEventArguments e, ILocalizer<Coordinates> localizer)
    {
        return new ApiEventResponse()
            .AddGraphicsResponse(new GraphicsResponse(bridge)
                .SetActiveGraphicsTool(GraphicsTool.Pointer))
            .AddUIElements(
                new UISelect()
                    .WithId("sketch-download-format")
                    .WithStyles(UICss.ToolParameter)
                    .AddOption(new UISelect.Option()
                        .WithValue("gpx")
                        .WithLabel("GPX Datei"))
                    .AddOption(new UISelect.Option()
                        .WithValue("json")
                        .WithLabel("GeoJson Datei")),
                new UILabel()
                    .WithLabel(localizer.Localize("download.sketch.label1")),
                new UIButtonContainer(new UIButton(UIButton.UIButtonType.servertoolcommand_ext, "download-sketch-file")
                    .WithId("webgis.tools.coordinates")
                    .WithStyles(UICss.DefaultButtonStyle)
                    .WithText("Herunterladen")),
                new UIHidden()
                    .WithId("sketch-download-wkt")
                    .WithStyles(UICss.ToolParameter, UICss.AutoSetterCurrentToolSketch));
    }

    [ServerToolCommand("download-sketch-file")]
    public ApiEventResponse OnDownloadSketchFile(IBridge bridge, ApiToolEventArguments e)
    {
        var format = e["sketch-download-format"];
        var sketchWkt = e["sketch-download-wkt"];

        var sketch = sketchWkt.ShapeFromWKT();

        var geoJsonFeature = new GeoJson.GeoJsonFeature();
        geoJsonFeature.FromShape(sketch);
        geoJsonFeature.Properties = new Dictionary<string, object>()
        {
            { "_meta.tool", sketch.GetToolName() },
            { "_meta.text", $"Sketch { sketch.GetGeometryName() }" }
        };
        var geoJsonFeatures = new GeoJson.GeoJsonFeatures()
        {
            Features = new GeoJson.GeoJsonFeature[] { geoJsonFeature }
        };

        IExport export = null;
        string name;

        if (format == "gpx")
        {
            export = new GpxExport();
            name = "sketch.gpx";
        }
        else if (format == "json")
        {
            export = new GeoJsonExport();
            name = "sketch.json";
        }
        else
        {
            throw new Exception($"Unsuported download formant {format}");
        }

        export.AddFeatures(geoJsonFeatures);

        return new ApiRawDownloadEventResponse(name, export.GetBytes(true));
    }

    [ServerToolCommand("remove-feature")]
    public ApiEventResponse OnRemove(IBridge bridge, ApiToolEventArguments e)
    {
        var featureOid = e["feature-oid"];
        var counter = featureOid.GetCounterFromCoordiantesGlobalOid();
        int.TryParse(e[CoordinatesCounter], out int currentCounter);

        var response = new ApiEventResponse()
            .UIElementsBehavoir(AppendUIElementsMode.Append)
            .AddUIElement(CreateTable(bridge, e, new int[] { counter }));

        if (currentCounter == counter)
        {
            response.AddUISetter(new UISetter(CoordinatesCounter, (--counter).ToString()));
        }

        return response;
    }

    #endregion

    #region Helper

    class Projection
    {
        public int Id { get; set; }
        public string DisplayName { get; set; }
        public string UIName { get; set; }
        public string DisplayStyle { get; set; }
        public int Digits { get; set; }
    }

    private Projection[] Projections(IBridge bridge)
    {
        FileInfo fi = new FileInfo($"{bridge.AppEtcPath}/coordinates/proj/xy/default.xml");
        if (!fi.Exists)
        {
            fi = new FileInfo($"{bridge.AppEtcPath}/coordinates/proj/xy/default_.xml");
        }

        if (!fi.Exists)
        {
            throw new Exception("/etc/coordinates/proj/xy/default.xml not exists");
        }

        XmlDocument doc = new XmlDocument();
        doc.Load(fi.FullName);

        List<Projection> projections = new List<Projection>();
        List<string> uinames = new List<string>();
        foreach (XmlNode node in doc.SelectNodes("projections/projection[@id and @displayname]"))
        {
            var projection = new Projection()
            {
                Id = int.Parse(node.Attributes["id"].Value),
                DisplayName = node.Attributes["displayname"].Value,
                DisplayStyle = node.Attributes["displaystyle"]?.Value?.ToLower().Trim() ?? String.Empty,
                Digits = !String.IsNullOrWhiteSpace(node.Attributes["digits"]?.Value) ? int.Parse(node.Attributes["digits"]?.Value) : -1
            };

            var uiname = projection.Id.ToString();

            #region Find Unique UIName
            if (uinames.Contains(uiname))
            {
                int i = 0;
                while (true)
                {
                    if (!uiname.Contains(uiname + "_" + (++i)))
                    {
                        break;
                    }
                }
                uiname = uiname + "_" + i;
            }
            uinames.Add(uiname);
            #endregion

            projection.UIName = uiname;
            projections.Add(projection);
        }

        return projections.ToArray();
    }

    private double ParseCoordinateValue(string val)
    {
        try
        {
            val = val.Trim().Replace(",", ".");

            #region Sonderzeichen für Grad, Minuten, Sekunden durch Leerzeichen ersetzen und doppelte Leererzeichen entfernen

            foreach (var s in new string[] { "°", "\"", "'", "g", "´", "`" })
            {
                val = val.Replace(s, " ").Trim();
            }

            while (val.Contains("  "))
            {
                val = val.Replace("  ", " ");
            }

            int sign = 1;
            if (val.Contains(":") || val.Split(':').Length == 2)
            {
                if (val.ToLower().StartsWith("w:") || val.ToLower().StartsWith("s:"))   // West/South -> negative Koordinaten
                {
                    sign = -1;
                }
                val = val.Split(':')[1].Trim();
            }

            #endregion

            string[] v = val.Split(' ');

            switch (v.Length)
            {
                case 1:
                    return v[0].ToPlatformDouble() * sign;
                case 2:
                    return (v[0].ToPlatformDouble() + v[1].ToPlatformDouble() / 60D) * sign;
                case 3:
                    return (v[0].ToPlatformDouble() + v[1].ToPlatformDouble() / 60D + v[2].ToPlatformDouble() / 3600D) * sign;
                default:
                    throw new Exception("Koordinatenwert kann nicht ermittelt werden.");
            }
        }
        catch (Exception ex)
        {
            throw new Exception("Fehler bei der Eingabe: " + val + "\n" + ex.Message);
        }
    }

    private void RoundCoordiantes(Point coord, SpatialReference sRef)
    {
        if (sRef.IsProjective)
        {
            coord.X = Math.Round(coord.X, 2);
            coord.Y = Math.Round(coord.Y, 2);
        }
        else
        {
            coord.X = Math.Round(coord.X, 7);
            coord.Y = Math.Round(coord.Y, 7);
        }
    }

    private string Deg2GMS(double deg, int digits)
    {
        int g = (int)Math.Floor(deg);
        deg -= g;
        int m = (int)Math.Floor(deg * 60);
        deg -= m / 60.0;
        double s = Math.Round(deg * 3600.0, 3);

        if (s >= 60.0) { m++; s = 0.0; }
        if (m == 60.0) { g++; m = 0; }

        //int digits=getCoordDigits();
        string digs = "";
        if (digits > 0)
        {
            digs = ".";
        }

        for (int i = 0; i < digits; i++)
        {
            digs += "0";
        }

        return String.Format("{0}°{1:00}'{2:00" + digs + "}''", g, m, s);
        //return g.ToString()+"°"+m.ToString()+"'"+s.ToString()+"''";
    }
    private string Deg2GM(double deg, int digits)
    {
        int g = (int)Math.Floor(deg);
        deg -= g;
        double m = deg * 60;
        if (m >= 60.0) { g++; m = 0.0; }

        //int digits=getCoordDigits();
        string digs = "";
        if (digits > 0)
        {
            digs = ".";
        }

        for (int i = 0; i < digits; i++)
        {
            digs += "0";
        }

        return String.Format("{0}°{1:00" + digs + "}'", g, m);
    }

    async private Task<ApiEventResponse> ClickEventResponse(IBridge bridge, ApiToolEventArguments e, ApiToolEventClick click, bool zoomToResult = false)
    {
        var projections = Projections(bridge);

        string defaultProject = (e.DeviceInfo != null && e.DeviceInfo.IsMobileDevice) || e.AsDefaultTool ? "" : e["coordinates-default-proj"];
        string defaultXString = String.Empty, defaultYString = String.Empty;

        var clickPoint = new Point(click.Longitude, click.Latitude);

        var features = new WebMapping.Core.Collections.FeatureCollection();

        int counter = 0;
        int.TryParse(e[CoordinatesCounter], out counter);
        counter++;

        var feature = new WebMapping.Core.Feature()
        {
            Shape = clickPoint,
            GlobalOid = counter.ToCoordinatesGlobalOid()
        };

        foreach (var projection in projections)
        {
            double x = click.WorldX, y = click.WorldY;
            SpatialReference toSRef = SRefStore.SpatialReferences.ById(projection.Id);
            if (toSRef == null)
            {
                continue;
            }

            var projectionResult = Project(projection, defaultProject, click.SRef, toSRef, x, y);
            defaultXString = projectionResult.defaultXString ?? defaultXString;
            defaultYString = projectionResult.defaultYString ?? defaultYString;

            feature.Attributes.Add(new WebMapping.Core.Attribute(projection.DisplayName, new string[] { projectionResult.xString, projectionResult.yString }));
        }

        var heightResults = await new RasterQueryHelper().PerformHeightQueryAsync(bridge, clickPoint, $"{bridge.AppEtcPath}/coordinates/h.xml");

        foreach (var heightResult in heightResults)
        {
            feature.Attributes.Add(new WebMapping.Core.Attribute(heightResult.Name, new string[] { heightResult.ResultString, String.Empty }));
        }

        features.Add(feature);

        var response = new ApiFeaturesEventResponse(this)
            .AddClickEvent(click)
            .UIElementsBehavoir(AppendUIElementsMode.Append)
            .AddFeatures(features, e.UseMobileBehavior() ? FeatureResponseType.New : FeatureResponseType.Append)
            .ZoomToFeaturesResult(zoomToResult);

        if (!e.AsDefaultTool && !(e.DeviceInfo != null && e.DeviceInfo.IsMobileDevice))
        {
            feature.Attributes.Add(new WebMapping.Core.Attribute("_fIndex", counter.ToString()));

            List<string> values = new List<string>(new string[]{
                    counter.ToString(),
                    click.Longitude.ToPlatformNumberString(),
                    click.Latitude.ToPlatformNumberString()
                });
            List<IUIElement> cols = new List<IUIElement>(new IUIElement[]
                {
                    new UILiteral() { literal = counter.ToString()},
                    new UILiteral() { literal = defaultXString },
                    new UILiteral() { literal = defaultYString },
                });

            foreach (var heightResult in heightResults)
            {
                values.Add(heightResult.ResultString);
                cols.Add(new UILiteral() { literal = heightResult.ResultString });
            }

            response.AddUIElement(new UITable(new UITableRow(cols.ToArray(), values: values.ToArray())
                                                       .WithStyles(UICss.ToolResultElement(this.GetType())))
            {
                InsertTypeValue = UITable.TableInsertType.Append
            }.WithId(CoordinatesTableId));

            response.AddUISetter(new UISetter(CoordinatesCounter, counter.ToString()));
        }
        else
        {
            feature.Attributes.Add(new WebMapping.Core.Attribute("_fIndex", "1"));
        }

        return response;
    }

    private (string xString, string yString, string defaultXString, string defaultYString) Project(Projection projection, string defaultProject, SpatialReference sRef, SpatialReference toSRef, double x, double y)
    {
        GeometricTransformer.Transform2D(
                ref x, ref y,
                sRef.Proj4, !sRef.IsProjective,
                toSRef.Proj4, !toSRef.IsProjective);

        string projectionId = projection.Id.ToString();

        string xString, yString, defaultXString = null, defaultYString = null;
        if (toSRef.IsProjective)
        {
            xString = /*"R:&nbsp;" +*/ Math.Round(x, projection.Digits >= 0 ? projection.Digits : 2).ToString();
            yString = /*"H:&nbsp;" +*/ Math.Round(y, projection.Digits >= 0 ? projection.Digits : 2).ToString();

            if (projectionId == defaultProject)
            {
                defaultXString = xString;
                defaultYString = yString;
            }
        }
        else
        {
            xString = (x > 0 ? "E: " : "W: ");
            yString = (y > 0 ? "N: " : "S: ");

            switch (projection.DisplayStyle)
            {
                case "dm":
                    xString += Deg2GM(Math.Abs(x), projection.Digits >= 0 ? projection.Digits : 2);
                    yString += Deg2GM(Math.Abs(y), projection.Digits >= 0 ? projection.Digits : 2);
                    if ($"{projectionId}dm" == defaultProject)
                    {
                        defaultXString = xString;
                        defaultYString = yString;
                    }
                    break;
                case "dms":
                    xString += Deg2GMS(Math.Abs(x), projection.Digits >= 0 ? projection.Digits : 1);
                    yString += Deg2GMS(Math.Abs(y), projection.Digits >= 0 ? projection.Digits : 1);
                    if ($"{projectionId}dms" == defaultProject)
                    {
                        defaultXString = xString;
                        defaultYString = yString;
                    }
                    break;
                default:
                    xString += Math.Round(Math.Abs(x), projection.Digits >= 0 ? projection.Digits : 2).ToString();
                    yString += Math.Round(Math.Abs(y), projection.Digits >= 0 ? projection.Digits : 2).ToString();
                    if (projectionId == defaultProject)
                    {
                        defaultXString = xString;
                        defaultYString = yString;
                    }
                    break;
            }
        }

        return (xString, yString, defaultXString, defaultYString);
    }

    private UISelect ProjectionsComboElement(IBridge bridge, string id, bool appendDisplayStyles = false)
    {
        var projCombo = new UISelect()
                            .WithId(id)
                            .WithStyles(UICss.ToolParameter, UICss.ToolParameterPersistent);

        foreach (var projection in Projections(bridge))
        {
            if (appendDisplayStyles == true)
            {
                projCombo.AddOption(new UISelect.Option()
                            .WithLabel(projection.DisplayName)
                            .WithValue(projection.Id.ToString() + projection.DisplayStyle));
            }
            else
            {
                if (projCombo.HasOptionsWhere(o => o.value == projection.Id.ToString()) == false)
                {
                    projCombo.AddOption(new UISelect.Option()
                                .WithLabel(projection.DisplayName)
                                .WithValue(projection.Id.ToString()));
                }
            }
        }

        return projCombo;
    }

    private UITable CreateTable(IBridge bridge, ApiToolEventArguments e, IEnumerable<int> removeRows = null)
    {
        var defaultCrs = !String.IsNullOrWhiteSpace(e["coordinates-default-proj"]) ? e["coordinates-default-proj"] : (e.MapCrs.HasValue ? e.MapCrs.Value : 0).ToString();
        var tableData = e[CoordinatesTableId];

        List<IUIElement> header = new List<IUIElement>(new IUIElement[]
            {
                new UILiteral() { literal = "#" },
                new UILiteral() { literal = "Rechtswert" },
                new UILiteral() { literal = "Hochwert" },
            });

        var heightColumns = new RasterQueryHelper().HeightNameNodes($"{bridge.AppEtcPath}/coordinates/h.xml");
        heightColumns.ToList().ForEach(c => header.Add(new UILiteral() { literal = c }));

        var table = new UITable(new UITableRow(header.ToArray(), isHeader: true))
        {
            InsertTypeValue = UITable.TableInsertType.Replace
        }.WithId(CoordinatesTableId)
         .WithStyles(UICss.ToolParameter, UICss.ToolParameterPersistent, UICss.TableAlternateRowColor)
         .WithParameterForServerCommands("download", "change-default-projection", "remove-feature", "inputcoordinates-dialog");

        int colCount = header.Count();

        if (!String.IsNullOrWhiteSpace(tableData))
        {
            var data = tableData.Split(';');
            var projection = Projections(bridge).Where(p => $"{p.Id}{p.DisplayStyle}" == defaultCrs).FirstOrDefault();

            if (data.Length % colCount == 0 && projection != null)
            {
                var sRef = SRefStore.SpatialReferences.ById(4326);
                var toSRef = SRefStore.SpatialReferences.ById(projection.Id);

                for (int row = 0; row < data.Length - (colCount - 1); row += colCount)
                {
                    var number = data[row];

                    if (removeRows != null && removeRows.Contains(int.Parse(number)))
                    {
                        continue;
                    }

                    double worldX = data[row + 1].ToPlatformDouble();
                    double worldY = data[row + 2].ToPlatformDouble();

                    var projectResult = Project(projection, defaultCrs, sRef, toSRef, worldX, worldY);

                    var cols = new List<IUIElement>(new UIElement[]
                    {
                        new UILiteral() { literal = number },
                        new UILiteral() { literal = projectResult.xString },
                        new UILiteral() { literal = projectResult.yString },
                    });
                    var values = new List<string>(new string[]{
                        number,
                        worldX.ToPlatformNumberString(),
                        worldY.ToPlatformNumberString()
                    });

                    #region Restiliche Spalten (Höhen) übertragen

                    for (int i = 3; i < colCount; i++)
                    {
                        cols.Add(new UILiteral() { literal = data[row + i] });
                        values.Add(data[row + i]);
                    }

                    #endregion

                    table.AddRow(new UITableRow(cols.ToArray(), values: values.ToArray())
                                        .WithStyles(UICss.ToolResultElement(this.GetType())));
                }
            }
        }

        return table;
    }

    #endregion

    #region IApiButtonResources Member

    public void RegisterToolResources(IToolResouceManager toolResourceManager)
    {
        toolResourceManager.AddImageResource("coords-xyz", Properties.Resources.coordinate_xyz);
        toolResourceManager.AddImageResource("calculator", Properties.Resources.calculator);
        toolResourceManager.AddImageResource("remove", Properties.Resources.edit_delete);
        toolResourceManager.AddImageResource("download", Properties.Resources.download);
        toolResourceManager.AddImageResource("upload", Properties.Resources.upload);
    }

    #endregion

    #region IIdenfifyTool 

    public Task<IEnumerable<CanIdentifyResult>> CanIdentifyAsync(IBridge bridge, Point point, double scale, string[] availableServiceIds = null, string[] availableQueryIds = null)
    {
        return Task.FromResult<IEnumerable<CanIdentifyResult>>(new CanIdentifyResult[] {
            new CanIdentifyResult() { Count=1 }
        });
    }

    #endregion
}
