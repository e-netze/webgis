using System;

namespace E.Standard.WebMapping.Core.Api;

public enum ToolType
{
    none = 0,
    click = 1,
    box = 2,
    current_extent = 3,
    sketch0d = 10,
    sketch1d = 11,
    sketch2d = 12,
    sketchany = 13,
    sketchcircle = 14,
    circlemarker = 15,
    bubble = 16,
    graphics = 20,
    print_preview = 40,
    watch_position = 80,
    overlay_georef_def = 101
}

public enum ToolCursor
{
    Grab = 0,
    Crosshair = 1,
    Pointer = 2,
    Custom_Pen = 3,
    Custom_Info = 4,
    Custom_Pan_Info = 5,
    Custom_Rectangle = 6,
    Custom_Selector = 7,
    Custom_ZoomIn = 8,
    Custom_ZoomOut = 9,
    Custom_Selector_Highlight = 10
}

public enum GraphicsTool
{
    None = 0,

    Symbol = 1,
    Line = 2,
    Polygon = 3,
    Freehand = 4,
    Text = 5,
    Rectangle = 6,
    Circle = 7,
    Distance_Circle = 8,
    DimLine = 9,
    Pointer = 10,
    HectoLine = 11,
    Point = 12,
    Compass_Rose = 13
}

public enum LookupType
{
    None = 0,
    ComboBox = 1,
    Autocomplete = 2
}

[Flags]
public enum VisibilityDependency
{
    None = 0,
    ServicesExists = 1,
    QueriesExists = 2,
    QueryResultsExists = 4,
    EditthemesExists = 8,
    ChainagethemestExists = 16,
    HasSelection = 32,
    HasMoreThanOneSelected = 64,
    HasToolResults = 128,
    HasMarkerInfo = 256,
    HasFilters = 512,
    ConnectingToLiveShareHub = 1024,
    HasLiveShareHubConnection = 2048,
    IsInLiveShareSession = 4096,
    IsNotInLiveShareSession = 8192,
    IsLiveShareSessionOwner = 16384,
    IsNotLiveShareSessionOwner = 32768,
    HasToolResults_Coordinates = 65536,
    HasToolResults_Chainage = 131072,
    HasToolResults_Coordinates_or_QueryResults = 262144,
    HasToolResults_Coordinates_or_Chainage_or_QueryResults = 524288,
    QueryFeaturesHasTableProperties = 1048576,
    ToolSketchesExists = 2097152,
    HasGraphicsStagedElement = 4194304
}

public enum ApiClientButtonCommand
{
    unknown = 0,
    refresh = 1,
    back = 2,
    fullextent = 3,
    removesketch = 4,
    queryresults = 5,
    currentpos = 6,
    showtoolmodaldialog = 7,
    hidetoolmodaldialog = 8,
    setparenttool = 9,
    serviceorder = 10,
    setgraphicssymbol = 20,
    setgraphicslinecolor = 21,
    setgraphicsfillcolor = 22,
    setgraphicslineweight = 23,
    setgraphicslinestyle = 24,
    setgraphicsdistancecircleradius = 25,
    setgraphicsdistancecirclesteps = 26,
    setgraphicshectolineunit = 27,
    setgraphicshectolineinterval = 28,
    assumecurrentgraphicselement = 29,
    removecurrentgraphicselement = 30,
    setgraphicstextcolor = 31,
    setgraphicstextstyle = 32,
    setgraphicstextsize = 33,
    setgraphicsfillopacity = 34,
    setgraphicspointcolor = 35,
    setgraphicspointsize = 36,
    boxzoomin = 40,
    print = 41,
    showprinttasks = 42,
    zoom2sketch = 43,
    stopcontinousposition = 44,
    snapping = 45,
    removecirclemarker = 46,
    clearselection = 47,
    removequeryresults = 58,
    addtoselection = 59,
    removefromselection = 60,
    removetoolqueryresults = 61,
    showlegend = 62,
    showmarkerinfo = 63,
    visfilterremoveall = 64,
    setlayersvisible = 65,
    showsketchanglehelperline = 66,
    showsketchanglegeographichelperline = 67,
    downloadmapimage = 68,
    removelivesharemarker = 69,
    setgraphics_symbol_and_apply_to_selected = 70,
    setgraphics_stroke_color_and_apply_to_selected = 71,
    setgraphics_stroke_weight_and_apply_to_selected = 72,
    setgraphics_stroke_style_and_apply_to_selected = 73,
    setgraphics_fill_color_and_apply_to_selected = 74,
    setgraphics_fill_opacity_and_apply_to_selected = 75,
    //setgraphicsdistancecircleradius_and_apply_to_selected = 76,  // not used
    //setgraphicsdistancecirclesteps_and_apply_to_selected = 77,
    //setgraphicshectolineunit_and_apply_to_selected = 78,
    //setgraphicshectolineinterval_and_apply_to_selected = 79,
    setgraphics_text_color_and_apply_to_selected = 81,
    setgraphics_text_style_and_apply_to_selected = 82,
    setgraphics_text_size_and_apply_to_selected = 83,
    setgraphics_point_color_and_apply_to_selected = 84,
    setgraphics_point_size_and_apply_to_selected = 85,
    refreshgraphicsui = 86
}

public enum ApiToolEvents
{
    Graphics_ElementSelected
}

public enum ServerEventHandlers
{
    OnChangeVertex,
    OnVertexAdded,
    OnUpdateCombo
}

public enum ApiToolConfirmationType
{
    Ok = 0,
    YesNo = 1
}
public enum ApiToolConfirmationEventType
{
    ButtonClick = 0
}

public enum AppendUIElementsMode
{
    ReplaceCurrent = 0,
    Append = 1
}
