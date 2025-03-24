using System;

namespace E.Standard.WebGIS.CMS;

public enum QueryMethod : int
{
    Exact = 0,
    EndingWildcard = 1,
    BeginningWildcard = 2,
    BothWildcards = 3,
    ExactOrWildcards = 4,
    LowerThan = 5,
    LowerOrEqualThan = 6,
    GreaterThan = 7,
    GreaterOrEqualThan = 8,
    SpacesToWildcard = 9,
    SpacesToWildcardWithEndingWildcard = 10,
    SpacesToWildcardWithBeginningWildcard = 11,
    SpacesToWildcardWithBeginningAndEndingWildcard = 12,
    Not = 13,
    In = 14,
    NotIn = 15,
    ExactNoSplit = 1000,
    EndingWildcardNoSplit = 1001,
    BeginningWildcardNoSplit = 1002,
    BothWildcardsNoSplit = 1003,
    ExactOrWildcardsNoSplit = 1004,
    LowerThanNoSplit = 1005,
    LowerOrEqualThanNoSplit = 1006,
    GreaterThanNoSplit = 1007,
    GreaterOrEqualThanNoSplit = 1008,
    SpacesToWildcardNoSplit = 1009,
    SpacesToWildcardWithEndingWildcardNoSplit = 1010,
    SpacesToWildcardWithBeginningWildcardNoSplit = 1011,
    SpacesToWildcardWithBeginningAndEndingWildcardNoSplit = 1012,
}

public enum ColumnType : int
{
    Field = 0,
    Hotlink = 1,
    Expression = 2,
    ImageExpression = 3,
    MultiField = 4,
    EmailAddress = 5,
    PhoneNumber = 6,
    DateTime = 7
}

public enum ColumnDataType
{
    String = 0,
    Number = 1
}

public enum FeatureTableType
{
    Default = 0,
    Table = 1,
    Html = 2,
    SingleFeature = 3
}

public enum DateFieldDisplayType
{
    Normal = 0,
    ShortDate = 1,
    LongDate = 2,
    ShortTime = 3,
    LongTime = 4
}

public enum ServiceProjection
{
    none = 0,
    Map = 1,
    Userdefined = 2
}

public enum MouseOverMode
{
    ThemeIsVisible = 0,
    QueryIsActive = 1
}

public enum DefaultToolId
{
    None = -1,
    ZoomIn = 0,
    ZoomOut = 1,
    Pan = 2,
    Identify = 3,
    GeoJuhuIdentify = 4,
    Hotlink = 5,
    Select = 6,
    Measure = 7,
    XY = 8,
    MapMarkup = 9,
    Email = 10,
    Editing = 11
}

public enum WMS_Version
{
    version_1_1_1 = 0,
    version_1_3_0 = 1
}

public enum WMS_LayerOrder
{
    Up = 0,
    Down = 1
}

public enum WFS_Version
{
    version_1_0_0 = 0,
    version_1_1_0 = 1
}

public enum VisFilterType
{
    visible = 0,
    locked = 1,
    invisible = 2
}

public enum LengthUnit
{
    m = 0,
    km = 1,
    cm = 2,
    dm = 3
}

public enum ServiceImageFormat
{
    Default = 0,
    PNG8 = 1,
    PNG24 = 2,
    PNG32 = 3,
    JPG = 4,
    GIF = 5
}

public enum CustomToolType
{
    Button = 0,
    MapTool_Click = 1,
    MapTool_Rectangle = 2,
    Button_Guid = 3
}

public enum YesNo { ignore = 0, no = 1, yes = 2 }

public enum TileGridOrientation
{
    LowerLeft = 0,
    UpperLeft = 1
}

public enum TileGridRendering
{
    Quality = 0,
    Readability = 1
}

public enum CommaFormat
{
    Default = 0,
    ForceComma = 1,
    ForcePoint = 2
}

public enum GeoRssLabelMethod
{
    None = 0,
    Numbers = 1,
    Letters = 2
}

public enum PresentationGroupStyle
{
    Button = 0,
    Checkbox = 1,
    Dropdown = 2
}

public enum PresentationCheckMode
{
    Button = 0,
    CheckBox = 1,
    OptionBox = 2
}

public enum PresentationLinkCheckMode
{
    Button = 0,
    CheckBox = 1,
    OptionBox = 2,
    DynamicContentMarker = 3
}

public enum PresentationAffecting
{
    Service = 0,
    Map = 1
}

public enum ClientVisibility
{
    Any = 0,
    Mobile = 1,
    Desktop = 2
}

public enum BrowserWindowTarget
{
    _blank = 0,
    _self = 1,
    opener = 2,
    _parent = 3,
    _top = 4,
    dialog = 5
}

public enum BrowserWindowTarget2
{
    tab = 0,
    dialog = 5
}

public enum MetadataButtonStyle
{
    i_button = 0,
    link_button = 1
}

public enum SearchServiceType
{
    None = 0,
    GeoJuhu = 1,
    Google = 2,
    gDSS_not_supported = 3,
    webGIS_SearchService = 4
}

// Never Change
public enum SearchServiceTarget
{
    Solr = 1,
    ElasticSearch_5 = 2,
    SolrMeta = 3,
    ElasticSearch_7 = 4,
    //SolrMetaSpatial = 5,
    PassThrough = 9,
    LuceneServerNET = 10,
    LuceneServerNET_Phonetic = 11,
}

public enum Lookuptype { ComboBox = 0, Autocomplete = 1 }

#region ArcImage Server

public enum ArcIS_ImageFormat
{
    jpgpng = 0,
    png = 1,
    png8 = 2,
    png24 = 3,
    jpg = 4,
    bmp = 5,
    gif = 6,
    tiff = 7
}

public enum ArcIS_PixelType
{
    UNKNOWN = 0,
    C128 = 1,
    C64 = 2,
    F32 = 3,
    F64 = 4,
    S16 = 5,
    S32 = 6,
    S8 = 7,
    U1 = 8,
    U16 = 9,
    U2 = 10,
    U32 = 11,
    U4 = 12,
    U8 = 13
}

public enum ArcIS_NoDataInterpretation
{
    esriNoDataMatchAny = 0,
    esriNoDataMatchAll = 1
}

public enum ArcIS_Interpolation
{
    RSP_BilinearInterpolation = 0,
    RSP_CubicConvolution = 1,
    RSP_Majority = 2,
    RSP_NearestNeighbor = 3
}

#endregion

public enum AGSGetSelectionMothod { Modern = 0, OldSchool = 1 }

public enum AGSExportMapFormat
{
    Json = 1,
    Image = 2
}

// Never change the values!!
public enum EditingFieldAutoValue
{
    none = 0,
    custom = 1,

    guid = 101,
    guid_sql = 102,

    create_login = 201,
    create_login_full = 202,
    create_login_short = 203,
    create_login_domain = 208,
    create_date = 204,
    create_time = 205,
    create_datetime_sql = 206,
    create_datetime_sql2 = 207,

    change_login = 301,
    change_login_full = 302,
    change_login_short = 303,
    change_login_domain = 308,
    change_date = 304,
    change_time = 305,
    change_datetime_sql = 306,
    change_datetime_sql2 = 307,

    scale = 401,

    shape_len = 501,
    shape_len_int = 502,
    shape_area = 503,
    shape_area_int = 505,
    shape_minx = 506,
    shape_miny = 507,
    shape_maxx = 508,
    shape_maxy = 509,

    db_select = 601,
    db_select_on_insert = 602
}

// Never change the values!!
public enum EditingFieldType
{
    Text = 0,
    Domain = 1,
    TextArea = 2,
    AutoComplete = 3,

    Date = 10,

    File = 20,

    Angle360 = 30,
    Angle360_Geographic = 31,

    Attribute_Picker = 50,

    Info = 999
}

public enum EditingInsertAction
{
    None = 0,
    Save = 1,
    SaveAndSelect = 2,
    SaveAndKeepAllAttributes = 3,
    SaveAndContinueAtLatestestSketchVertex = 4,
    SaveAndContinueAtLatestestSketchVertexAndKeepAllAttributes = 5
}

[Flags]
public enum EditingRights
{
    Unknown = 0,
    Insert = 1,
    Update = 2,
    Delete = 4,
    Geometry = 8,
    MassAttributeable = 16,
    MultipartGeometries = 32
}

public enum FieldAutoSortMethod
{
    None = 0,
    Ascending = 1,
    Descending = 2
}

public enum FeatureTransferMethod
{
    Copy = 0,
    Move = 1
}

public enum MaskValidationOperators
{
    Ident = 0,
    Equals = 1,
    @in = 2,
    IN = 3,
    inside = 4,
    INSIDE = 5
};
