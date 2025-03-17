using System;

namespace E.Standard.WebMapping.Core;

public enum BasemapType { Normal = 0, Overlay = 1 }

public enum ServiceProjectionMethode
{
    none = 0,
    Map = 1,
    Userdefined = 2
}

public enum LegendOptimization
{
    None = 0,
    Themes = 1,
    Symbols = 2
}

public enum ServiceDiagnosticState
{
    Ok = 0,
    LayersNameConfusion = 1,
    LayersMissing = 100,
}

public enum LayerType
{
    unknown = 0,
    point = 1,
    line = 2,
    polygon = 3,
    image = 4,
    annotation = 5,
    network = 6
}

public enum FieldType
{
    ID = -99,
    Shape = -98,
    Boolean = -7,
    BigInteger = -5,
    Char = 1,
    Interger = 4,
    SmallInteger = 5,
    Float = 6,
    Double = 8,
    String = 12,
    Date = 91,
    Unknown = 999,
    GUID = 1001,   //  esriFieldTypeGUID
    GlobalId = 1002   // esriFieldTypeGlobalID?
}

public enum ServiceResponseType
{
    Image = 0,
    Html = 1,
    Javascript = 2,
    VectorService = 3,
    Collection = 4,
    StaticOverlay = 5
}

public enum ToolType
{
    Null = -1,
    Click = 0,
    Box = 1,
    Pan = 2,
    Custom = 3,
    Sketch0D = 4,
    Sketch1D = 5,
    Sketch2D = 6,
    SketchCircle = 7,
    SketchRectangle = 8,
    SketchPoint = 9,
    MovePrintBox = 10,
    Overlay = 11,
    RotateMap = 12,
    Rotate = 13
}

[Flags]
public enum DrawPhase
{
    None = 0,
    Map = 1,
    Graphics = 2,
    Toc = 4,
    Selection = 8,
    QueryResult = 16,
    BufferSelection = 32,
    ShowPrintBox = 64,
    HidePrintBox = 128,
    GeoJuhu = 256,
    ModalDialog = 512,
    NetworkResult = 1024,
    SuppressToolDialog = 2048,
    QueryResultOptionalThemesAvailable = 4096,
    MarkerResult = 8192
}

public enum TocGroupCheckMode
{
    CheckBox = 0,
    OptionBox = 1,
    Lock = 2
}

public enum ServiceDiagnosticsWarningLevel
{
    Never = 0,
    Error = 1,
    Warning = 2
}

public enum ServiceDynamicPresentations
{
    Manually = 0,
    Auto = 1,
    AutoMaxLevel1 = 2,
    AutoMaxLevel2 = 3,
    AutoMaxLevel3 = 4
}

public enum ServiceDynamicQueries
{
    Manually = 0,
    Auto = 1
}

public enum ImageServiceType
{
    Normal = 0,
    Watermark = 1
}

public enum DynamicDehavior
{
    AutoAppendNewLayers = 0,
    UseStrict = 1,
    SealedLayers_UseServiceDefaults = 2
}

public enum ServiceLayerVisibility
{
    Invisible = 0,
    Visible = 1
}

public enum MapServiceLayerVisibility
{
    ServiceDefaults = 0,
    AllInvisible = 1,
    AllVisible = 2,
}
