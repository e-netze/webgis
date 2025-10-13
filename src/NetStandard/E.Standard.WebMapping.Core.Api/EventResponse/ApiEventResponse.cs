using E.Standard.Extensions.Collections;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse.Models;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Geometry;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Api.EventResponse;

public class ApiEventResponse
{
    public ApiEventResponse() { }

    public ApiEventResponse(ApiEventResponse response)
    {
        if (response != null)
        {
            response.CloneTo(this);
        }
    }

    public ICollection<IUIElement> UIElements { get; set; }
    public ICollection<UI.IUISetter> UISetters { get; set; }
    public bool? AppendUIElements { get; set; }

    public IApiToolEvent[] Events { get; set; }
    public IApiButton ActiveTool { get; set; }
    public ToolType? ActiveToolType { get; set; }
    public Shape Sketch { get; set; }
    public bool? CloseSketch { get; set; }
    public bool? FocusSketch { get; set; }
    public bool? RemoveSketch { get; set; }
    public bool? SketchReadonly { get; set; }

    public IEnumerable<NamedSketch> NamedSketches { get; set; }

    public bool? SketchHasZ { get; set; }
    public string SketchGetZCommand { get; set; }

    public string InitLiveshareConnection { get; set; }
    public string JoinLiveshareSession { get; set; }
    public string LeaveLiveshareSession { get; set; }
    public bool? ExitLiveShare { get; set; }
    public string SetLiveShareClientname { get; set; }

    public double[] ZoomTo4326 { get; set; }
    public double? ZoomToScale { get; set; }

    public WebMapping.Core.Api.Drawing.ChartBridge Chart { get; set; }

    public GraphicsResponse Graphics { get; set; }

    public Lense MapViewLense { get; set; }

    public string[] RefreshServices { get; set; }

    public Dictionary<string, Dictionary<string, bool>> SetLayerVisility { get; set; }

    public bool RefreshSelection { get; set; }
    public bool RefreshSnapping { get; set; }

    public ApiToolEventArguments.SelectionInfoClass ToolSelection { get; set; }

    public SketchVertexTooltip[] SketchVertexTooltips { get; set; }

    public E.Standard.WebMapping.Core.Geometry.Point SketchAddVertex { get; set; }

    public SketchPropertiesDTO SketchProperties { get; set; }

    public IEnumerable<ApiClientButtonCommand> ClientCommands { get; set; }
    public object ClientCommandData { get; set; }

    public FilterDefinitionDTO[] SetFilters { get; set; }
    public FilterDefinitionDTO[] UnsetFilters { get; set; }

    public LabelingDefinitionDTO[] SetLabeling { get; set; }
    public LabelingDefinitionDTO[] UnsetLabeling { get; set; }

    public StaticOverlayServiceDefinitionDTO[] AddStaticOverlayServices { get; set; }
    public StaticOverlayServiceDefinitionDTO[] RemoveStaticOverlayServices { get; set; }

    public ToolUndoDTO[] ToolUndos { get; set; }
    public IApiButton UndoTool { get; set; }

    public EditingThemeDefDTO ApplyEditingTheme { get; set; }

    public ToolCursor? ToolCursor { get; set; }

    public FeatureCollection ReplaceQueryFeatures { get; set; }
    public IEnumerable<IQueryBridge> ReplaceFeaturesQueries { get; set; }
    public SpatialReference ReplaceFeatureSpatialReference { get; set; }

    public int[] RemoveQueryFeaturesById { get; set; }
    public IEnumerable<IQueryBridge> RemoveFeaturesQueries { get; set; }

    public string ErrorMessage { get; set; }
    //public MarkerDefinition Marker { get; set; }

    public ApiEventResponse Append(ApiEventResponse response)
    {
        if (response == null)
        {
            return this;
        }

        this.UIElements = this.UIElements.TryAppendItems(response.UIElements);
        this.UISetters = this.UISetters.TryAppendItems(response.UISetters);

        if (this.ActiveTool == null)
        {
            this.ActiveTool = response.ActiveTool;
        }

        if (this.Sketch == null)
        {
            this.Sketch = response.Sketch;
        }

        return this;
    }

    public ApiEventResponse Prepend(ApiEventResponse response)
    {
        if (response == null)
        {
            return this;
        }

        return response.Append(this);
    }

    public string FireCustomMapEvent { get; set; }

    public bool RemoveSecondaryToolUI { get; set; }

    virtual public void CloneTo(ApiEventResponse to)
    {
        to.UIElements = this.UIElements;
        to.UISetters = this.UISetters;
        to.Events = this.Events;
        to.ActiveTool = this.ActiveTool;
        to.ActiveToolType = this.ActiveToolType;
        to.Sketch = this.Sketch;
        to.CloseSketch = this.CloseSketch;
        to.Chart = this.Chart;
        to.Graphics = this.Graphics;
        to.MapViewLense = this.MapViewLense;
        to.RefreshServices = this.RefreshServices;
        to.RefreshSelection = this.RefreshSelection;
        to.RefreshSnapping = this.RefreshSnapping;
        to.ToolSelection = this.ToolSelection;
        to.SketchVertexTooltips = this.SketchVertexTooltips;
        to.ClientCommands = this.ClientCommands;
        to.SetFilters = this.SetFilters;
        to.UnsetFilters = this.UnsetFilters;
        to.SetLabeling = this.SetLabeling;
        to.UnsetLabeling = this.UnsetLabeling;
        to.AddStaticOverlayServices = this.AddStaticOverlayServices;
        to.ToolUndos = this.ToolUndos;
        to.UndoTool = this.UndoTool;
        to.ApplyEditingTheme = this.ApplyEditingTheme;
        to.ToolCursor = this.ToolCursor;
        to.ErrorMessage = this.ErrorMessage;
        to.RemoveSecondaryToolUI = this.RemoveSecondaryToolUI;
    }

    public string TriggerToolButtonClick { get; set; }

    #region Static Members

    static public ApiEventResponse Completed => null;

    #endregion
}
