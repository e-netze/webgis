using E.Standard.Json;
using E.Standard.Platform;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace E.Standard.WebMapping.Core.Api;

public class ApiToolEventArguments
{
    private readonly Dictionary<string, string> _configuration;
    private string _rawEventString;

    public ApiToolEventArguments(string eventString, string toolOptions = "", Dictionary<string, string> configuration = null)
    {
        this.RawEventString = eventString;

        this.ToolOptions = toolOptions;

        _configuration = configuration;
    }

    public string ToArgumentValue(string val)
    {
        return val.Replace("~~~&PIPE;~~~", "|");
    }

    public ApiToolEventArguments(IBridge bridge, NameValueCollection nvc, string[] ignoreParametersInLowerCase = null, Dictionary<string, string> configuration = null)
    {
        StringBuilder rawEventStringBuilder = new StringBuilder();
        this.RawEvent = new ExpandoObject();
        var objDict = (IDictionary<string, object>)this.RawEvent;

        bool decode = (nvc["_encparameters"] == "true");

        List<string> ignoredParameters = ignoreParametersInLowerCase != null ? new List<string>(ignoreParametersInLowerCase) : new List<string>();

        foreach (string key in nvc.AllKeys)
        {
            if (key == null)
            {
                continue;
            }

            if (key.StartsWith("hmac") || key == "_encparameters")
            {
                continue;
            }

            if (ignoredParameters.Contains(key.ToLower()))
            {
                continue;
            }

            var val = nvc[key];
            if (decode && val.StartsWith("0x"))
            {
                try
                {
                    val = bridge.SecurityDecryptString(val);
                }
                catch { }
            }

            if (key == "_sketchWgs84")
            {
                this.SketchWgs84 = val.ShapeFromWKT();
            }
            if (key == "_sketch")
            {
                this.Sketch = val.ShapeFromWKT();
            }
            if (key == "_as-default-tool" && val == "true")
            {
                this.AsDefaultTool = true;
            }
            if (key == "_selectioninfo")
            {
                try
                {
                    this.SelectionInfo = JSerializer.Deserialize<SelectionInfoClass>(val);
                }
                catch { }
            }
            if (key == "_mapscale")
            {
                this.MapScale = val.ToPlatformDouble();
            }

            objDict[key] = val;


            if (rawEventStringBuilder.Length > 0)
            {
                rawEventStringBuilder.Append("|");
            }

            rawEventStringBuilder.Append(key);
            rawEventStringBuilder.Append("=");
            rawEventStringBuilder.Append(val);
        }

        if (this.SketchWgs84 != null)
        {
            this.SketchWgs84.SrsId = 4326;
        }

        if (this.Sketch != null)
        {
            this.Sketch.SrsId = this.GetInt("_sketchSrs");
        }

        this.RawEventString = rawEventStringBuilder.ToString();

        _configuration = configuration;
    }

    public string RawEventString
    {
        get
        {
            return _rawEventString;
        }
        set
        {
            _rawEventString = value;

            this.RawEvent = new ExpandoObject();
            var objDict = (IDictionary<string, object>)this.RawEvent;

            if (!String.IsNullOrWhiteSpace(_rawEventString))
            {
                foreach (string p in _rawEventString.Split('|'))
                {
                    if (String.IsNullOrWhiteSpace(p))
                    {
                        continue;
                    }

                    int pos = p.IndexOf("=");
                    if (pos > 0)
                    {
                        string key = p.Substring(0, pos);
                        string val = ToArgumentValue(p.Substring(pos + 1, p.Length - pos - 1));

                        if (key == "_sketchWgs84")
                        {
                            this.SketchWgs84 = val.ShapeFromWKT();
                        }
                        if (key == "_sketch")
                        {
                            this.Sketch = val.ShapeFromWKT();
                        }
                        if (key == "_sketchInfo")
                        {
                            this.SketchInfo = JSerializer.Deserialize<SketchInfoClass>(val);
                        }
                        if (key == "_as-default-tool" && val == "true")
                        {
                            this.AsDefaultTool = true;
                        }
                        if (key == "_selectioninfo")
                        {
                            try
                            {
                                this.SelectionInfo = JSerializer.Deserialize<SelectionInfoClass>(val);
                            }
                            catch { }
                        }
                        if (key == "_mapscale")
                        {
                            this.MapScale = val.ToPlatformDouble();
                        }
                        if (key == "_mapcrs")
                        {
                            try
                            {
                                this.MapCrs = int.Parse(val);
                            }
                            catch { }
                        }
                        if (key == "_calccrs")
                        {
                            try
                            {
                                this.CalcCrs = int.Parse(val);
                            }
                            catch { }
                        }
                        if (key == "_calccrs_is_dynamic")
                        {
                            try
                            {
                                this.CalcCrsIsDynamic = bool.Parse(val);
                            }
                            catch { }
                        }
                        if (key == "_deviceinfo")
                        {
                            try
                            {
                                this.DeviceInfo = JSerializer.Deserialize<DeviceInfoClass>(val);
                            }
                            catch { }
                        }
                        if (key == "_ui_elements")
                        {
                            try
                            {
                                this.UIElements = new List<UIElementsClass>();
                                foreach (var nodeInfo in val.Split(','))
                                {
                                    var parts = val.Split('.');
                                    this.UIElements.Add(new UIElementsClass()
                                    {
                                        NodeName = parts.First(),
                                        Classes = parts.Skip(1).ToArray()
                                    });
                                }
                            }
                            catch { }
                        }
                        objDict[key] = val;
                    }
                    else
                    {
                        objDict[p] = null;
                    }
                }
            }

            if (this.SketchWgs84 != null)
            {
                this.SketchWgs84.SrsId = 4326;
            }

            if (this.Sketch != null)
            {
                SpatialAlgorithms.SetSpatialReferenceAndProjectPoints(this.Sketch, this.GetInt("_sketchSrs"), CoreApiGlobals.SRefStore.SpatialReferences);
            }

            ReprojectModifiedVertices(this.GetInt("_sketchSrs"));
        }
    }

    public dynamic RawEvent
    {
        get;
        private set;
    }

    public string this[string property]
    {
        get
        {
            try
            {
                var objDict = (IDictionary<string, object>)this.RawEvent;
                if (objDict.ContainsKey(property) && objDict[property] != null)
                {
                    return objDict[property].ToString();
                }
            }
            catch { }

            return String.Empty;
        }
        set
        {
            try
            {
                var objDict = (IDictionary<string, object>)this.RawEvent;
                objDict[property] = value;
            }
            catch { }
        }
    }

    public string[] Properties
    {
        get
        {
            try
            {
                var objDict = (IDictionary<string, object>)this.RawEvent;
                return objDict.Keys.ToArray();
            }
            catch { }

            return new string[0];
        }
    }

    public object GetValue(string propertyName, object defaultValue = null)
    {
        string val = this[propertyName];
        if (String.IsNullOrWhiteSpace(val))
        {
            return defaultValue;
        }

        return val;
    }

    public bool IsEmpty(string propertyName)
    {
        return String.IsNullOrWhiteSpace(this[propertyName]);
    }

    public string GetString(string propertyName)
    {
        return this[propertyName];
    }

    public double GetDouble(string propertyName)
    {
        string val = this[propertyName];
        if (String.IsNullOrWhiteSpace(val))
        {
            return double.NaN;
        }

        return val.ToPlatformDouble();
    }

    public double[] GetDoubleArray(string propertyName)
    {
        string val = this[propertyName];
        if (String.IsNullOrWhiteSpace(val))
        {
            return null;
        }

        List<double> ret = new List<double>();
        foreach (var v in val.Split(','))
        {
            ret.Add(v.ToPlatformDouble());
        }

        return ret.ToArray();
    }

    public int GetInt(string propertyName)
    {
        string val = this[propertyName];
        if (string.IsNullOrWhiteSpace(val))
        {
            return 0;
        }

        return int.Parse(val);
    }

    public bool GetBoolean(string propertyName)
    {
        return this[propertyName]?.ToLower() == "true";
    }

    public T[] GetArray<T>(string propertyName)
    {
        string val = this[propertyName];
        if (String.IsNullOrWhiteSpace(val))
        {
            return null;
        }

        List<T> ret = new List<T>();
        foreach (var v in val.Split(','))
        {
            if (typeof(T) == typeof(double))
            {
                ret.Add((T)Convert.ChangeType(v.ToPlatformDouble(), typeof(T)));
            }
            else if (typeof(T) == typeof(float))
            {
                ret.Add((T)Convert.ChangeType(v.ToPlatformFloat(), typeof(T)));
            }
            else
            {
                ret.Add((T)Convert.ChangeType(v, typeof(T)));
            }
        }

        return ret.ToArray();
    }

    public T[] TryGetArray<T>(string propertyName)
    {
        try
        {
            return GetArray<T>(propertyName);
        }
        catch { return null; }
    }

    public T[] GetArrayOrEmtpy<T>(string propertyName)
    {
        var ret = GetArray<T>(propertyName);
        if (ret == null)
        {
            return new T[0];
        }

        return ret;
    }

    public T[] TryGetArrayOrEmtpy<T>(string propertyName)
    {
        var ret = TryGetArray<T>(propertyName);
        if (ret == null)
        {
            return new T[0];
        }

        return ret;
    }

    public T GetEnumValue<T>(string propertyName)
        where T : Enum
    {
        return (T)(object)this.GetInt(propertyName);
    }

    public string MenuItemValue
    {
        get
        {
            return this["menuitem-value"];
        }
    }

    public void ClearMenuItemValue()
    {
        this["menuitem-value"] = String.Empty;
    }

    public string CommandIndexValue
    {
        get
        {
            return this["_commandIndexValue"];
        }
    }

    public bool IsBoxEvent
    {
        get
        {
            return !String.IsNullOrEmpty(this["box"]) &&
                   !String.IsNullOrEmpty(this["boxsize"]);
        }
    }

    public bool IsOverlayGeoRefDefintionEvent
    {
        get { return !String.IsNullOrEmpty(this["overlay_georef_def"]); }
    }

    public OverlayGeoRefDefintionClass OverlayGeoRefDefintion
    {
        get
        {
            if (IsOverlayGeoRefDefintionEvent)
            {
                return JSerializer.Deserialize<OverlayGeoRefDefintionClass>(this["overlay_georef_def"]);
            }

            return null;
        }
    }

    public string ServerCommandMethod => this["_method"];
    public string ServerCommandArgument => this["_servercommand_argument"];
    public void SetServerCommandArgument(string argument)
    {
        this["_servercommand_argument"] = argument;
    }

    public bool AsDefaultTool { get; set; }

    public ApiToolEventClick ToClickEvent(int sRefId)
    {
        return ToClickEvent(CoreApiGlobals.SRefStore.SpatialReferences.ById(sRefId));
    }
    public ApiToolEventClick ToClickEvent(SpatialReference sRef = null)
    {
        IDictionary<string, object> rawEventDict = (IDictionary<string, object>)this.RawEvent;
        Point worldPoint = null;
        int sourceSrefId = 4326;
        Shape sketch = null;
        double lat = 0D, lng = 0D;
        int[] size = null;

        if (rawEventDict.ContainsKey("lng") && rawEventDict.ContainsKey("lat") && rawEventDict.ContainsKey("crs"))
        {
            string lngString = this.RawEvent.lng.ToString();
            string latString = this.RawEvent.lat.ToString();

            worldPoint = new Point(lng = lngString.ToPlatformDouble(),
                                   lat = latString.ToPlatformDouble());
            sourceSrefId = Convert.ToInt32(this.RawEvent.crs);
        }
        else if (rawEventDict.ContainsKey("box"))
        {
            string[] box = this.RawEvent.box.ToString().Split(',');
            sketch = new Envelope(box[0].ToPlatformDouble(), box[1].ToPlatformDouble(), box[2].ToPlatformDouble(), box[3].ToPlatformDouble());
            worldPoint = ((Envelope)sketch).CenterPoint;
            lng = worldPoint.X; lat = worldPoint.Y;
            sourceSrefId = int.Parse(this.RawEvent.crs);

            if (rawEventDict.ContainsKey("boxsize"))
            {
                size = new[] {
                            int.Parse(rawEventDict["boxsize"].ToString().Split(',')[0]),
                            int.Parse(rawEventDict["boxsize"].ToString().Split(',')[1])
                     };
            }
        }
        else if (SketchWgs84 != null)
        {
            worldPoint = SketchWgs84.ShapeEnvelope.CenterPoint;
            lng = worldPoint.X; lat = worldPoint.Y;
            sketch = SketchWgs84;
        }

        if (sRef != null && sRef.Id != sourceSrefId)
        {
            using (GeometricTransformer transformer = new GeometricTransformer())
            {
                transformer.FromSpatialReference(CoreApiGlobals.SRefStore.SpatialReferences.ById(sourceSrefId).Proj4, true);
                transformer.ToSpatialReference(sRef.Proj4, !sRef.IsProjective);

                transformer.Transform2D(worldPoint);

                if (sketch != null)
                {
                    transformer.Transform(sketch);
                }
            }
        }

        string xString = rawEventDict.ContainsKey("x") ? this.RawEvent.x.ToString() : null;
        string yString = rawEventDict.ContainsKey("y") ? this.RawEvent.y.ToString() : null;

        if (worldPoint == null)
        {
            throw new Exception("Es wurde keine Geometrie übergeben.");
        }

        return new ApiToolEventClick()
        {
            WorldX = worldPoint.X,
            WorldY = worldPoint.Y,
            Latitude = lat,
            Longitude = lng,
            ContainerX = !String.IsNullOrEmpty(xString) ? Convert.ToInt32(xString.ToPlatformDouble()) : 0,
            ContainerY = !String.IsNullOrEmpty(yString) ? Convert.ToInt32(yString.ToPlatformDouble()) : 0,
            SRef = sRef != null ? sRef : CoreApiGlobals.SRefStore.SpatialReferences.ById(sourceSrefId),
            Sketch = sketch,
            SketchInfo = this.SketchInfo,
            Size = size,
            EventScale = rawEventDict.ContainsKey("event_scale") ? rawEventDict["event_scale"].ToString().ToPlatformDouble() : 0D
        };
    }

    public ApiToolEventClick ToMapProjectedClickEvent()
    {
        try
        {
            return ToClickEvent(int.Parse(this.RawEvent.mapcrs));
        }
        catch (Exception)
        {
            return ToClickEvent();
        }
    }

    public Shape Sketch { get; private set; }
    public Shape SketchWgs84 { get; private set; }

    public SketchInfoClass SketchInfo { get; private set; }

    public SelectionInfoClass SelectionInfo { get; set; }

    public DeviceInfoClass DeviceInfo { get; set; }

    public string CurrentKeyPressed => this["_current_key_pressed"];

    #region UIElements

    public ICollection<UIElementsClass> UIElements { get; set; }

    public bool HasElement(string nodeName, IEnumerable<string> classNames)
    {
        if (UIElements == null || UIElements.Count() == 0)
        {
            return false;
        }

        foreach (var uiElement in UIElements.Where(e => nodeName.Equals(e.NodeName, StringComparison.OrdinalIgnoreCase)))
        {
            bool containsAll = true;
            foreach (var className in classNames)
            {
                if (!uiElement.Classes.Contains(className))
                {
                    containsAll = false;
                    break;
                }
            }

            if (containsAll)
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    public double? MapScale { get; set; }
    public int? MapCrs { get; set; }
    public int? CalcCrs { get; set; }
    public bool CalcCrsIsDynamic { get; set; }

    public double[] MapBBox() => this.GetArray<double>("_mapbbox");
    public int[] MapSize() => this.GetArray<int>("_mapsize");

    public bool QueryMarkersVisible() => this.GetBoolean("_query_markers_visible");
    public bool CoordinateMarkersVisible() => this.GetBoolean("_coordinate_markers_visible");
    public bool ChainageMarkersVisible() => this.GetBoolean("_chainage_markers_visible");

    private ICollection<string> _mapOverlayServices = null;
    public ICollection<string> MapOverlayServices
    {
        get
        {
            if (_mapOverlayServices == null)
            {
                _mapOverlayServices = String.IsNullOrEmpty(this["_overlay_services"]) ?
                    new List<string>() :
                    new List<string>(this["_overlay_services"].Split(','));
            }

            return _mapOverlayServices;
        }
    }

    private string ToolOptions { get; set; }

    public T GetToolOptions<T>()
    {
        if (String.IsNullOrWhiteSpace(this.ToolOptions))
        {
            return default(T);
        }

        return JSerializer.Deserialize<T>(this.ToolOptions);
    }

    Dictionary<string, ApiToolEventFile> _files = null;
    public ApiToolEventFile GetFile(string name)
    {
        if (_files == null || !_files.ContainsKey(name))
        {
            return null;
        }

        return _files[name];
    }
    public void AddFile(string name, ApiToolEventFile file)
    {
        if (_files == null)
        {
            _files = new Dictionary<string, ApiToolEventFile>();
        }

        _files[name] = file;
    }

    #region Access Tool Config

    public bool HasConfigValue(string key)
    {
        return _configuration != null && _configuration.ContainsKey(key);
    }

    public string GetConfigValue(string key)
    {
        if (!HasConfigValue(key))
        {
            return null;
        }

        return _configuration[key];
    }

    public bool GetConfigBool(string key, bool defaultValue = false)
    {
        var configValue = GetConfigValue(key);
        switch (configValue?.ToLower())
        {
            case "true":
                return true;
            case "false":
                return false;
            default:
                return defaultValue;
        }
    }

    public int GetConfigInt(string key, int defaultValue = 0)
    {
        var configValue = GetConfigValue(key);
        if (String.IsNullOrEmpty(configValue))
        {
            return defaultValue;
        }

        return int.Parse(configValue);
    }

    public double GetConfigDouble(string key, double defaultValue = 0)
    {
        var configValue = GetConfigValue(key);
        if (String.IsNullOrEmpty(configValue))
        {
            return defaultValue;
        }

        return configValue.ToPlatformDouble();
    }

    public Dictionary<TKey, TValue> GetConfigDictionay<TKey, TValue>(string key)
    {
        var configValue = GetConfigValue(key);
        if (String.IsNullOrEmpty(configValue))
        {
            return null;
        }

        var result = new Dictionary<TKey, TValue>();

        foreach (var keyValue in configValue.Split(','))
        {
            try
            {
                if (keyValue.Contains(":"))
                {
                    var dictKey = keyValue.Substring(0, keyValue.IndexOf(':'));
                    var dictValue = keyValue.Substring(keyValue.IndexOf(':') + 1);

                    result.Add((TKey)Convert.ChangeType(dictKey, typeof(TKey)), (TValue)Convert.ChangeType(dictValue, typeof(TValue)));
                }
            }
            catch { }
        }

        return result;
    }

    public IEnumerable<T> GetConfigArray<T>(string key)
    {
        var configValue = GetConfigValue(key);
        if (String.IsNullOrEmpty(configValue))
        {
            return null;
        }

        return configValue.Split(',')
                          .Select(v => (T)Convert.ChangeType(v, typeof(T)));
    }

    #endregion

    #region Private Members

    private void ReprojectModifiedVertices(int originalSketchSrs)
    {
        if (originalSketchSrs == 0 || this.Sketch == null || this.SketchWgs84 == null)
        {
            return;
        }

        using (var transformer = new GeometricTransformerPro(CoreApiGlobals.SRefStore, 4326, originalSketchSrs))
        {
            if (this.Sketch is Point)
            {
                Point point = (Point)this.Sketch;
                if (ReprojectionRecommended(point))
                {
                    Point point84 = new Point((Point)this.SketchWgs84);
                    transformer.Transform(point84);
                    point.FromPoint(point84);
                }
            }
            else if (this.Sketch is Polyline)
            {
                Polyline polyline = (Polyline)this.Sketch;
                Polyline polyline84 = (Polyline)this.SketchWgs84;

                for (int p = 0; p < polyline.PathCount; p++)
                {
                    for (int i = 0; i < polyline[p].PointCount; i++)
                    {
                        if (ReprojectionRecommended(polyline[p][i]))
                        {
                            Point point84 = new Point(polyline84[p][i]);
                            transformer.Transform(point84);
                            polyline[p][i].FromPoint(point84);
                        }
                    }
                }
            }
            else if (this.Sketch is Polygon)
            {
                Polygon polygon = (Polygon)this.Sketch;
                Polygon polygon84 = (Polygon)this.SketchWgs84;

                for (int r = 0; r < polygon.RingCount; r++)
                {
                    for (int i = 0; i < polygon[r].PointCount; i++)
                    {
                        if (ReprojectionRecommended(polygon[r][i]))
                        {
                            Point point84 = new Point(polygon84[r][i]);
                            transformer.Transform(point84);
                            polygon[r][i].FromPoint(point84);
                        }
                    }
                }
            }
        }
    }

    private bool ReprojectionRecommended(Point point)
    {
        return (point is PointM3 && ((PointM3)point).M3 != null && ((PointM3)point).M3.ToString().StartsWith("meta:"));
    }

    #endregion

    #region Static Members

    static public string ToArgumentArray(string[] values)
    {
        return String.Join(",", values);
    }

    static public string[] FromArgumentArray(string argument)
    {
        return argument.Split(',');
    }

    static public T FromArgument<T>(string argument)
    {
        string[] arguments = FromArgumentArray(argument);

        T t = (T)Activator.CreateInstance(typeof(T));

        foreach (var prop in t.GetType().GetProperties())
        {
            foreach (ArgumentObjectPropertyAttribute a in prop.GetCustomAttributes(typeof(ArgumentObjectPropertyAttribute), false))
            {
                int index = a.Index;
                if (index < 0 || index >= arguments.Length)
                {
                    continue;
                }

                object val = Convert.ChangeType(arguments[index], prop.PropertyType);
                prop.SetValue(t, val);
            }
        }

        return t;
    }

    static public string ToArgument(object obj)
    {
        if (obj == null)
        {
            return String.Empty;
        }

        StringBuilder sb = new StringBuilder();
        Dictionary<int, PropertyInfo> properties = new Dictionary<int, PropertyInfo>();
        int maxIndex = 0;

        foreach (var prop in obj.GetType().GetProperties())
        {
            foreach (ArgumentObjectPropertyAttribute a in prop.GetCustomAttributes(typeof(ArgumentObjectPropertyAttribute), false))
            {
                int index = a.Index;
                properties.Add(index, prop);
                maxIndex = Math.Max(maxIndex, index);
            }
        }

        for (int i = 0; i <= maxIndex; i++)
        {
            if (i > 0)
            {
                sb.Append(",");
            }

            if (properties.ContainsKey(i) && properties[i].GetValue(obj) != null)
            {
                sb.Append(properties[i].GetValue(obj).ToString());
            }
        }

        return sb.ToString();
    }

    #endregion

    #region SubClasses

    public class SketchInfoClass
    {
        [JsonProperty("geometryType")]
        [System.Text.Json.Serialization.JsonPropertyName("geometryType")]
        public string GeometryType { get; set; }

        [JsonProperty("radius")]
        [System.Text.Json.Serialization.JsonPropertyName("radius")]
        public double Radius { get; set; }

        [JsonProperty("center")]
        [System.Text.Json.Serialization.JsonPropertyName("center")]
        public LatLng Center { get; set; }

        #region Classes

        public class LatLng
        {
            [JsonProperty("lat")]
            [System.Text.Json.Serialization.JsonPropertyName("lat")]
            public double Lat { get; set; }

            [JsonProperty("lng")]
            [System.Text.Json.Serialization.JsonPropertyName("lng")]
            public double Lng { get; set; }
        }

        #endregion
    }

    public class ApiToolEventClick
    {
        public double WorldX { get; set; }
        public double WorldY { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public int ContainerX { get; set; }
        public int ContainerY { get; set; }

        public Shape Sketch { get; set; }
        public ApiToolEventArguments.SketchInfoClass SketchInfo { get; set; }

        public SpatialReference SRef { get; set; }

        public int[] Size { get; set; }

        public double EventScale { get; set; }
    }

    public class ApiToolEventFile
    {
        public byte[] Data { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
    }

    public class SelectionInfoClass
    {
        public SelectionInfoClass() { }
        public SelectionInfoClass(SelectionInfoClass sic, int[] objectIds)
        {
            this.ServiceId = sic.ServiceId;
            this.LayerId = sic.LayerId;
            this.QueryId = sic.QueryId;
            this.GeometryType = sic.GeometryType;
            this.ObjectIds = objectIds;
        }

        [JsonProperty(PropertyName = "serviceid")]
        [System.Text.Json.Serialization.JsonPropertyName("serviceid")]
        public string ServiceId { get; set; }

        [JsonProperty(PropertyName = "layerid")]
        [System.Text.Json.Serialization.JsonPropertyName("layerid")]
        public string LayerId { get; set; }

        [JsonProperty(PropertyName = "queryid")]
        [System.Text.Json.Serialization.JsonPropertyName("queryid")]
        public string QueryId { get; set; }

        [JsonProperty(PropertyName = "geometrytype")]
        [System.Text.Json.Serialization.JsonPropertyName("geometrytype")]
        public string GeometryType { get; set; }

        [JsonProperty(PropertyName = "ids")]
        [System.Text.Json.Serialization.JsonPropertyName("ids")]
        public int[] ObjectIds { get; set; }

        public static SelectionInfoClass ClearSelection => new SelectionInfoClass();
    }

    public class DeviceInfoClass
    {
        [JsonProperty("is_mobile_device")]
        [System.Text.Json.Serialization.JsonPropertyName("is_mobile_device")]
        public bool IsMobileDevice { get; set; }

        [JsonProperty("screen_width")]
        [System.Text.Json.Serialization.JsonPropertyName("screen_width")]
        public int ScreenWidth { get; set; }
        [JsonProperty("screen_height")]
        [System.Text.Json.Serialization.JsonPropertyName("screen_height")]
        public int ScreenHeight { get; set; }

        [JsonProperty("advanced_tool_behaviour")]
        [System.Text.Json.Serialization.JsonPropertyName("advanced_tool_behaviour")]
        public bool AdvancedToolBehaviour { get; set; }
    }

    public class OverlayGeoRefDefintionClass
    {
        [JsonProperty(PropertyName = "passPoints")]
        [System.Text.Json.Serialization.JsonPropertyName("passPoints")]
        public IEnumerable<PassPointClass> PassPoints { get; set; }

        [JsonProperty(PropertyName = "topLeft")]
        [System.Text.Json.Serialization.JsonPropertyName("topLeft")]
        public PosClass TopLeft { get; set; }

        [JsonProperty(PropertyName = "topRight")]
        [System.Text.Json.Serialization.JsonPropertyName("topRight")]
        public PosClass TopRight { get; set; }

        [JsonProperty(PropertyName = "bottomLeft")]
        [System.Text.Json.Serialization.JsonPropertyName("bottomLeft")]
        public PosClass BottomLeft { get; set; }

        [JsonProperty(PropertyName = "size")]
        [System.Text.Json.Serialization.JsonPropertyName("size")]
        public float[] ImageRectSize { get; set; }

        public class PosClass
        {
            [JsonProperty(PropertyName = "lng")]
            [System.Text.Json.Serialization.JsonPropertyName("lng")]
            public double X { get; set; }

            [JsonProperty(PropertyName = "lat")]
            [System.Text.Json.Serialization.JsonPropertyName("lat")]
            public double Y { get; set; }
        }

        public class PassPointClass
        {
            [JsonProperty(PropertyName = "vector")]
            [System.Text.Json.Serialization.JsonPropertyName("vector")]
            public VectorClass Vector { get; set; }

            [JsonProperty(PropertyName = "pos")]
            [System.Text.Json.Serialization.JsonPropertyName("pos")]
            public PosClass Pos { get; set; }

            public class VectorClass
            {
                public double x { get; set; }
                public double y { get; set; }
            }
        }
    }

    public class UIElementsClass
    {
        public string NodeName { get; set; }
        public IEnumerable<string> Classes { get; set; }
    }

    #endregion
}
