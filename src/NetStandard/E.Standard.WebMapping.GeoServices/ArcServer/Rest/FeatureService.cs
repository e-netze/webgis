using E.Standard.Json;
using E.Standard.Platform;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Editing;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Exceptions;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json.Geometry;
using E.Standard.WebMapping.GeoServices.ArcServer.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest;

public class FeatureService : IFeatureWorkspaceSpatialReference,
                              IWebFeatureWorkspace,
                              IFeatureWorkspaceUndo,
                              IFeatureWorkspaceGeometryOperations
{
    //private string _service = String.Empty;
    private string _errMsg = String.Empty;
    private IDictionary<string, string> _fieldTypes;
    // ESRI Feldtypen, die "" (Leerstring) als Wert annehmen (vgl. andere: null)
    // mögliche Feldtypen: "esriFieldTypeBlob" | "esriFieldTypeDate" | "esriFieldTypeDouble" | "esriFieldTypeGeometry" | "esriFieldTypeGlobalID" | "esriFieldTypeGUID" | 
    // "esriFieldTypeInteger" | "esriFieldTypeOID" | "esriFieldTypeRaster" | "esriFieldTypeSingle" | "esriFieldTypeSmallInteger" | "esriFieldTypeString" | "esriFieldTypeXML"
    private bool _hasZ = false, _hasM = false;
    private List<string> _emptyStringFieldTypes = new List<string> { "esriFieldTypeGlobalID", "esriFieldTypeGUID", "esriFieldTypeString", "esriFieldTypeXML" };
    private string _idFieldName = "OBJECTID";
    private string _shapeFieldName = "SHAPE";
    private EsriFeature _currentFeature;
    private List<EsriFeature> _addFeatures = new List<EsriFeature>();
    private List<EsriFeature> _updateFeatures = new List<EsriFeature>();
    private List<string> _deleteFeatureIds = new List<string>();
    private IRequestContext _requestContext;
    private readonly IMapServiceAuthentication _mapServiceAuth;

    public FeatureService(MapService mapService)
    {
        _mapServiceAuth = new MapService()
        {
            TokenExpiration = mapService.TokenExpiration
        };

        ResetCurrentFeature();
    }

    private void ResetCurrentFeature()
    {
        _currentFeature = new EsriFeature();
    }

    #region IFeatureWorkspace

    public string ConnectionString
    {
        set
        {
            string[] conn = value.Split(';');
            string service = ExtractValue(value, "service");

            //_user = ExtractValue(value, "user");
            //_pwd = ExtractValue(value, "pwd");
            //Server = new Uri(service).Host;

            if (String.IsNullOrEmpty(service))
            {
                throw new ArgumentException("Parameter 'service' in ConnectionString bei Workspace-Definition fehlt.");
            }

            string server = service.Contains("/rest/", StringComparison.OrdinalIgnoreCase)
                ? service.Substring(0, service.IndexOf("/rest/", StringComparison.OrdinalIgnoreCase))
                : new Uri(service).Host;

            _mapServiceAuth.PreInit("",
                server, service,
                ExtractValue(value, "user"),
                ExtractValue(value, "pwd"),
                ExtractValue(value, "static_token"),
                "", []);
        }
    }

    public string LastErrorMessage
    {
        get
        {
            return _errMsg; ;
        }
    }

    public List<string> FieldNames
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public string CurrentFeatureGeometry
    {
        get
        {
            return _currentFeature.Geometry.ToString();
        }

        set
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(value);

            Shape shape = Shape.FromArcXML(doc.ChildNodes[0], null);
            string jsonGeometry = RestHelper.ConvertGeometryToJson(shape, this.SrsId, _hasZ, _hasM);
            _currentFeature.Geometry = JSerializer.Deserialize<JsonGeometry>(jsonGeometry);
        }
    }

    public int SrsId
    {
        get; set;
    }
    public bool RebuildSpatialIndex
    {
        get
        {
            return false;
        }

        set
        {
        }
    }

    public string VersionName
    {
        get
        {
            return String.Empty;
        }

        set
        {
        }
    }

    public bool Connect(SqlCommand mode)
    {
        return true;
    }

    public Task<bool> DeleteCurrentFeature()
    {
        if (_currentFeature.Attributes.ContainsKey(_idFieldName) && !String.IsNullOrEmpty(_currentFeature.Attributes[_idFieldName]?.ToString()))
        {
            //return await SendRequest("deleteFeatures", "f=json&objectIds=" + _currentFeature.Attributes[_idFieldName]);
            _deleteFeatureIds.Add(_currentFeature.Attributes[_idFieldName]?.ToString());
            return Task.FromResult(true);
        }
        else
        {
            _errMsg = "Keine ObjectID für das zu löschende Feature vorhanden.";
            return Task.FromResult(false);
        }
    }

    //public Task<bool> Delete(string where)
    //{
    //    throw new NotImplementedException();
    //}

    public void DisConnect()
    {
        //throw new NotImplementedException();
    }

    public string GetCurrentFeatureAttributValue(string name)
    {
        string fieldname = name.Substring(name.LastIndexOf(".") + 1);

        if (_currentFeature.Attributes.ContainsKey(fieldname))
        {
            return _currentFeature.Attributes[fieldname].ToString();
        }
        else
        {
            return String.Empty;
        }
    }

    public bool CurrentFeatureHasAttribute(string name) =>
        _currentFeature?.Attributes != null
        && !String.IsNullOrEmpty(name)
        && _currentFeature.Attributes.ContainsKey(name.Substring(name.LastIndexOf(".") + 1));

    public bool MoveTo(int ID)
    {
        if (ID >= 0)
        {
            //_myFeature.Attributes[this.LayerIdFieldname] = ID + 1;

            _currentFeature.Attributes[_idFieldName] = ID;
            return true;
        }
        else
        {
            _errMsg = "Kein Feature zum Updaten gefunden";
            return false;
        }
    }

    public bool SetCurrentFeatureAttribute(string name, string value)
    {
        try
        {
            string fieldname = name.Substring(name.LastIndexOf(".") + 1);

            // Wenn nichts eingetragen wurde und kein Autovalue => "" wird übergeben
            // "" darf aber nur bei Text und GUID sein. Date, Long, etc. liefert Fehler => richtig: Feld weglassen
            if (String.IsNullOrEmpty(value))
            {
                if (_fieldTypes.ContainsKey(fieldname.ToLower()) && _emptyStringFieldTypes.Any(ft => _fieldTypes[fieldname.ToLower()].Contains(ft)))
                {
                    _currentFeature.Attributes[fieldname] = value;
                }
            }
            else
            {
                object typedValue = value;
                if (_fieldTypes.ContainsKey(fieldname.ToLower()))
                {
                    switch (_fieldTypes[fieldname.ToLower()])
                    {
                        case "esriFieldTypeDouble":
                            typedValue = value.ToPlatformDouble();
                            break;
                        case "esriFieldTypeSingle":
                            typedValue = value.ToPlatformFloat();
                            break;
                        case "esriFieldTypeSmallInteger":
                            typedValue = Convert.ToInt16(value.Replace(",", "."));
                            break;
                        case "esriFieldTypeInteger":
                            typedValue = Convert.ToInt32(value.Replace(",", "."));
                            break;
                    }
                }
                _currentFeature.Attributes[fieldname] = typedValue;
            }

            return true;
        }
        catch (Exception ex)
        {
            _errMsg = $"{value ?? ""} => {name ?? ""}: {ex.Message}";
            return false;
        }
    }

    public Task<bool> StoreCurrentFeature()
    {
        if (_currentFeature.Attributes.ContainsKey(_idFieldName))
        {
            _updateFeatures.Add(_currentFeature);
        }
        else
        {
            _addFeatures.Add(_currentFeature);
        }

        ResetCurrentFeature();

        return Task.FromResult(true);
    }

    //async public Task<bool> Update(string where)
    //{
    //    List<EsriFeature> features = new List<EsriFeature>();

    //    // where = "OBJECTID in (3545,3547)"
    //    string objectIds = System.Text.RegularExpressions.Regex.Match(where, @"\(([^)]*)\)").Groups[1].Value;

    //    foreach (string objectId in objectIds.Split(','))
    //    {
    //        EsriFeature updateFeature = _currentFeature.Clone();
    //        updateFeature.Attributes[_idFieldName] = Convert.ToInt64(objectId);
    //        features.Add(updateFeature);
    //    }

    //    string jsonFeatures = Convert2JsonObject(features);
    //    string postData = "f=json&features=" + System.Net.WebUtility.UrlEncode(jsonFeatures);

    //    return await SendRequest("updateFeatures", postData);
    //}

    async public Task<bool> Commit()
    {
        if (new int[] { _addFeatures.Count, _updateFeatures.Count, _deleteFeatureIds.Count }.Where(c => c > 0).Count() > 1)
        {
            throw new Exception("Not allowed: you can only insert, update or delete features in the same commit.");
        }

        if (_addFeatures.Count > 0)
        {
            string jsonFeature = Convert2JsonObject(_addFeatures.ToArray());
            string postData = "f=json&features=" + System.Net.WebUtility.UrlEncode(jsonFeature);
            _addFeatures.Clear();

            return await SendRequest("addFeatures", postData);
        }
        else if (_updateFeatures.Count > 0)
        {
            string jsonFeature = Convert2JsonObject(_updateFeatures.ToArray());
            string postData = "f=json&features=" + System.Net.WebUtility.UrlEncode(jsonFeature);
            _updateFeatures.Clear();

            return await SendRequest("updateFeatures", postData);
        }
        else if (_deleteFeatureIds.Count > 0)
        {
            string objectIds = String.Join(",", _deleteFeatureIds);
            _deleteFeatureIds.Clear();

            return await SendRequest("deleteFeatures", $"f=json&objectIds={objectIds}");
        }
        else
        {
            throw new Exception("Nothing to insert or update in commit");
        }
    }

    async public Task<bool> GetFieldAndGeometryTypes()
    {
        try
        {
            var authHandler = _requestContext.GetRequiredService<AgsAuthenticationHandler>();

            // Feldtypen auslesen
            string response = await authHandler.TryPostAsync(
                        _mapServiceAuth,
                        _mapServiceAuth.Service, "f=json");
            JObject responseJson = (JObject)JsonConvert.DeserializeObject(response);
            if (responseJson["error"] != null)
            {
                _errMsg = String.Format("{0}. Details: {1}", responseJson["error"]["message"], responseJson["error"]["details"]);
                throw new ArgumentException("Layerinfo konnte nicht ausgelesen werden: " + _errMsg);
            }
            if (responseJson["fields"] == null)
            {
                throw new ArgumentException("Feldtypen aus Layerinfo konnten nicht ausgelesen werden.");
            }
            else
            {
                _fieldTypes = new Dictionary<string, string>();

                _shapeFieldName = responseJson["geometryField"]?["name"]?.ToString();

                foreach (var field in responseJson["fields"])
                {
                    if (field["name"] != null && field["type"] != null)
                    {
                        _fieldTypes[field["name"].ToString().ToLower()] = field["type"].ToString();
                        if (field["type"].ToString() == "esriFieldTypeOID")
                        {
                            _idFieldName = field["name"].ToString();
                        }
                        if (String.IsNullOrEmpty(_shapeFieldName) && field["type"].ToString() == "esriFieldTypeGeometry")
                        {
                            _shapeFieldName = field["name"].ToString();
                        }
                    }

                }
            }

            _hasM = responseJson["hasM"]?.ToString().ToLower() == "true";
            _hasZ = responseJson["hasZ"]?.ToString().ToLower() == "true";

            return true;
        }
        catch (OperationException)
        {
            throw;
        }
        catch (GatewayTimeoutException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _errMsg = ex.Message;
            return false;
        }
    }

    #endregion

    #region IFeatureWorkspaceGeometryOperations

    public bool ClosePolygonRings => false;

    public bool CleanRings => false; //!String.IsNullOrEmpty(_service) && _service.ToLower().Contains("/geoservices/rest/services/");  // gView Services

    #endregion

    #region IWebFeatureWorkspace

    //public string Server { get; private set; }

    async public Task SetWebCredentials(IRequestContext requestContext, ICredentials credentials)
    {
        _requestContext = requestContext;
        _mapServiceAuth.HttpCredentials = credentials;

        if (!await GetFieldAndGeometryTypes())
        {
            throw new Exception($"FeatureService: Can't set web-credentials: {_errMsg}");
        }
    }

    #endregion

    #region IFeatureWorkspaceUndo

    async public Task<EditUndoableDTO> CreateUndo(IAppCryptography crypto,
                                               IEnumerable<long> objectIds,
                                               SqlCommand command,
                                               string[] fields = null)
    {
        string outFields = "*";
        bool returnGeometry = true;

        if (fields != null)
        {
            var outFieldsList = new List<string>(fields);

            if (!outFieldsList.Contains(_idFieldName))
            {
                outFieldsList.Insert(0, _idFieldName);
            }

            if (!outFieldsList.Contains(_shapeFieldName))
            {
                returnGeometry = false;
            }

            outFields = String.Join(",", outFieldsList);
        }

        string where = $"{_idFieldName} in ({String.Join(",", objectIds)})";

        var postData = new StringBuilder($"f=json&where={where}&outFields={outFields}&returnGeometry={returnGeometry.ToString().ToLower()}");
        if (returnGeometry)
        {
            postData.Append($"&returnM={(_hasM ? "true" : "")}&returnZ={(_hasZ ? "true" : "")}");
        }

        if (this.SrsId > 0)
        {
            postData.Append($"&outSR={this.SrsId}");
        }

        var sendRequestResult = await SendRequestPro("query", postData.ToString());

        if (sendRequestResult.success)
        {
            string response = sendRequestResult.response;
            var esriFeatures = JSerializer.Deserialize<EsriFeatures>(response);

            if (esriFeatures.Features != null)
            {
                foreach (var feature in esriFeatures.Features)
                {
                    if (feature == null)
                    {
                        continue;
                    }

                    if (command == SqlCommand.delete)
                    {
                        //
                        // Undo eines Deletes ist später ein Insert:
                        // Darum sollte man hier das OBJECTID Feld nicht speichern, weil man das später beim Insert nicht übergeben darf
                        //

                        feature.Attributes.Remove(_idFieldName);
                    }

                    //foreach(var attributeName in feature.Attributes.Keys.ToArray())  // FeatureServer should not return Function Fields (STArea, STLength)
                    //{
                    //    if(attributeName.Contains("(") && attributeName.Contains(")"))
                    //    {
                    //        feature.Attributes.Remove(attributeName);
                    //    }
                    //}

                    //
                    // Spatial Reference auf Feature setzten, damit später beim speichern die richtige Projektion an den FeatureServer übergeben wird.
                    //
                    if (feature.Geometry != null && feature.Geometry.SpatialReference == null)
                    {
                        feature.Geometry.SpatialReference = esriFeatures.SpatialReference;
                    }
                }
            }

            var shape = esriFeatures?.GetShape(this.SrsId, _hasZ, _hasM);
            return new EditUndoableDTO(command, shape)
            {
                FeatureCount = esriFeatures?.Features?.Count() ?? 0,
                Data = crypto.SecurityEncryptString(JsonConvert.SerializeObject(esriFeatures.Features)/*, CryptoResultStringType.Base64*/)
            };
        }
        else
        {
            return null;
        }
    }

    async public Task<(bool success, EditUndoableDTO newEditundoable, long[] affectedObjectIds)> PerformUndo(IAppCryptography crypto, EditUndoableDTO editUndoable)
    {
        EditUndoableDTO newEditUndoable = null;
        var data = crypto.SecurityDecryptString(editUndoable.Data);
        string action = "";
        long[] objectIds = null;

        switch (editUndoable.Command)
        {
            case SqlCommand.delete:
                await GetFieldAndGeometryTypes();
                action = "addFeatures";
                break;
            case SqlCommand.update:
                await GetFieldAndGeometryTypes();
                var features = JSerializer.Deserialize<IEnumerable<EsriFeature>>(data);
                if (features == null)
                {
                    throw new Exception("Unknown Error: No feature stored inside this undo entity");
                }

                objectIds = features?.Select(f => Convert.ToInt64(f.Attributes[_idFieldName])).ToArray();

                if (features.FirstOrDefault().Geometry == null)
                {
                    // ToDo: No Geometry => undo form mass attributatiom
                    // is there a reason to craete an undo from the undo?
                }
                else
                {
                    if (objectIds.Length > 0)
                    {
                        newEditUndoable = await CreateUndo(crypto, objectIds, SqlCommand.update);
                    }
                }

                action = "updateFeatures";
                break;
            default:
                throw new ArgumentException();
        }

        string postData = "f=json&features=" + System.Net.WebUtility.UrlEncode(data);

        return (success: await SendRequest(action, postData), newEditundoable: newEditUndoable, affectedObjectIds: objectIds);
    }

    public IEnumerable<int> CommitedObjectIds { get; set; }

    #endregion

    #region Helpers

    private string Convert2JsonObject(object obj)
    {
        return JsonConvert.SerializeObject(obj);

        //System.IO.MemoryStream ms = new System.IO.MemoryStream();
        //var jw = new JsonTextWriter(new System.IO.StreamWriter(ms));
        //jw.Formatting = Newtonsoft.Json.Formatting.Indented;
        //var serializer = new JsonSerializer();
        //serializer.Serialize(jw, obj);
        //jw.Flush();
        //ms.Position = 0;

        //string json = System.Text.Encoding.UTF8.GetString(ms.GetBuffer());
        //json = json.Trim('\0');
        //return json;
    }

    async private Task<bool> SendRequest(string action, string postData)
    {
        return (await SendRequestPro(action, postData)).success;
    }

    async private Task<(bool success, string response)> SendRequestPro(string action, string postData)
    {
        var authHandler = _requestContext?.GetRequiredService<AgsAuthenticationHandler>();
        //Request request = new Request();
        //string response = request.DoPost(_server + action, postData);
        string response = await authHandler.TryPostAsync(_mapServiceAuth, _mapServiceAuth.Service + action, postData);

        var jsonResponse = JSerializer.Deserialize<JsonFeatureServerResponse>(response);

        if (jsonResponse.CheckSuccess(response) == true)
        {
            CommitedObjectIds = jsonResponse.ObjectIds;
            return (success: true, response);
        }

        _errMsg = jsonResponse.GetErrorMessage();
        return (success: false, response);
    }

    public static string ExtractValue(string Params, string Param)
    {
        Param = Param.Trim();

        foreach (string a in Params.Split(';'))
        {
            string aa = a.Trim();
            if (aa.ToLower().IndexOf(Param.ToLower() + "=") == 0)
            {
                if (aa.Length == Param.Length + 1)
                {
                    return "";
                }

                return aa.Substring(Param.Length + 1, aa.Length - Param.Length - 1).Trim();
            }
        }
        return String.Empty;
    }

    //private string _tokenParam = String.Empty;
    //private int _ticketExpiration = 60;

    //private ICredentials _credentials;
    //private IHttpService _httpService;

    //async internal Task<string> TryPostAsync(string requestUrl, string postBodyData)
    //{
    //    int i = 0;
    //    while (true)
    //    {
    //        try
    //        {
    //            string tokenParameter = String.Empty;
    //            if (!String.IsNullOrWhiteSpace(_tokenParam))
    //            {
    //                tokenParameter = (String.IsNullOrWhiteSpace(postBodyData) ? "" : "&") + "token=" + _tokenParam;
    //            }

    //            string ret = String.Empty;
    //            try
    //            {
    //                //ret = await _httpRequest.DoPostAsync(requestUrl, postBodyData + tokenParameter, Proxy, Credentials);
    //                ret = await _httpService.PostFormUrlEncodedStringAsync(requestUrl,
    //                                                                      $"{postBodyData}{tokenParameter}",
    //                                                                      new Web.Models.RequestAuthorization() { Credentials = _credentials });
    //            }
    //            catch (System.Net.WebException ex)
    //            {
    //                if (ex.Message.Contains("(403)") ||
    //                    ex.Message.Contains("(498)") ||
    //                    ex.Message.Contains("(499)"))
    //                {
    //                    throw new TokenRequiredException();
    //                }

    //                throw;
    //            }
    //            catch (HttpServiceException httpEx)
    //            {
    //                if (httpEx.StatusCode == HttpStatusCode.Forbidden /* 403 */ ||
    //                    (int)httpEx.StatusCode == 498 ||
    //                    (int)httpEx.StatusCode == 499)
    //                {
    //                    throw new TokenRequiredException();
    //                }

    //                throw;
    //            }
    //            if (ret.Contains("\"error\":"))
    //            {
    //                JsonError error = JSerializer.Deserialize<JsonError>(ret);
    //                //if (error.error == null)
    //                //    throw new Exception("Unknown error");
    //                if (error.error != null)
    //                {
    //                    if (error.error.code == 499 || error.error.code == 498 || error.error.code == 403) // Token Required (499), Invalid Token (498), No user Persmissions (403)
    //                    {
    //                        throw new TokenRequiredException(error.error.message);
    //                    }

    //                    if (error.error.code == 500)
    //                    {
    //                        throw new OperationException("Error:" + error.error.code + "\n" + error.error.message);
    //                    }

    //                    if (error.error.code == 504)
    //                    {
    //                        throw new GatewayTimeoutException("Error:" + error.error.code + "\n" + error.error.message + " - Check service status.");
    //                    }
    //                }
    //                //throw new Exception("Error:" + error.error.code + "\n" + error.error.message);
    //            }
    //            return ret;
    //        }
    //        catch (TokenRequiredException ex)
    //        {
    //            await HandleTokenExceptionAsync(i, ex);
    //        }
    //        i++;
    //    }
    //}

    //async private Task HandleTokenExceptionAsync(int i, TokenRequiredException ex)
    //{
    //    if (i < 3)  // drei mal probieren lassen
    //    {
    //        await RefreshTokenAsync();
    //    }
    //    else
    //    {
    //        throw ex;
    //    }
    //}

    //private static object _refreshTokenLocker = new object();
    //private bool _tokenRequired = false;
    //protected static ThreadSafe.ThreadSafeDictionary<string, string> _tokenParams = new ThreadSafe.ThreadSafeDictionary<string, string>();
    //async private Task RefreshTokenAsync()
    //{
    //    string currentParameter = _tokenParam;
    //    //lock (_refreshTokenLocker)
    //    {
    //        int pos = _service.ToLower().IndexOf("/rest/");
    //        string dictKey = _service.ToLower().Substring(0, pos) + "/" + this._user;

    //        if (_tokenParams.ContainsKey(dictKey) && _tokenParams[dictKey] != currentParameter)
    //        {
    //            _tokenParam = _tokenParams[dictKey];
    //        }
    //        else
    //        {
    //            string tokenServiceUrl = _service.ToLower().Substring(0, pos) + "/tokens/generateToken";
    //            string tokenParams = "request=gettoken&username=" + _user + "&password=" + _pwd + "&expiration=" + _ticketExpiration + "&f=json";

    //            string tokenResponse = String.Empty;
    //            while (true)
    //            {
    //                try
    //                {
    //                    //tokenResponse = await this._httpRequest.DoPostAsync(tokenServiceUrl, tokenParams, Proxy, Credentials);
    //                    tokenResponse = await _httpService.PostFormUrlEncodedStringAsync(tokenServiceUrl,
    //                                                                                    tokenParams,
    //                                                                                    new Web.Models.RequestAuthorization() { Credentials = _credentials });
    //                    break;
    //                }
    //                catch (System.Net.WebException we)
    //                {
    //                    if (we.Message.Contains("(502)") && tokenServiceUrl.StartsWith("http://"))
    //                    {
    //                        tokenServiceUrl = "https:" + tokenServiceUrl.Substring(5);
    //                        continue;
    //                    }
    //                    throw we;
    //                }
    //            }
    //            if (tokenResponse.Contains("\"error\":"))
    //            {
    //                JsonError error = JSerializer.Deserialize<JsonError>(tokenResponse);
    //                throw new Exception("GetToken-Error:" + error.error.code + "\n" + error.error.message + "\n" +
    //                    (error.error.details != null ? String.Empty : error.error.details.ToString()) +
    //                    "\nUser=" + _user);
    //            }
    //            else
    //            {
    //                JsonSecurityToken jsonToken = JSerializer.Deserialize<JsonSecurityToken>(tokenResponse);
    //                if (jsonToken.token != null)
    //                {
    //                    _tokenParam = jsonToken.token;
    //                    _tokenParams.Add(dictKey, _tokenParam);

    //                    //if (_dummyConnector != null)
    //                    //    _dummyConnector.LogString("RefreshToken: " + dictKey + ": " + _tokenParam);
    //                }
    //            }
    //        }

    //        _tokenRequired = !String.IsNullOrEmpty(_tokenParam);


    //        //if (!String.IsNullOrEmpty(_user) && _tokenRequired == false)
    //        //    _credentials = new NetworkCredential(_user, _pwd);
    //        //else
    //        //    _credentials = System.Net.CredentialCache.DefaultNetworkCredentials;
    //    }
    //}

    #endregion
}

public class EsriFeature
{
    public EsriFeature()
    {
        Attributes = new Dictionary<string, object>();
    }

    [JsonProperty(PropertyName = "attributes", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("attributes")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, object> Attributes { get; set; }

    [JsonProperty(PropertyName = "geometry", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("geometry")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public JsonGeometry Geometry { get; set; }

    public EsriFeature Clone()
    {
        var clone = new EsriFeature()
        {
            Attributes = new Dictionary<string, object>(this.Attributes),
            Geometry = this.Geometry
        };
        return clone;
    }
}

public class EsriFeatures
{
    [JsonProperty(PropertyName = "features", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("features")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<EsriFeature> Features { get; set; }

    [JsonProperty("spatialReference", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("spatialReference")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public JsonSpatialReference SpatialReference { get; set; }

    public Shape GetShape(int srsId, bool hasZ, bool hasM)
    {
        if (Features == null || Features.Count() == 0 || Features.First().Geometry == null)
        {
            return null;
        }

        Shape shape = null;

        #region Geometry Type aus erstem Feature

        var firstShape = Features.First().Geometry;
        if (firstShape.Rings != null)
        {
            shape = new Polygon()
            {
                SrsId = srsId
            };
        }
        else if (firstShape.Paths != null)
        {
            shape = new Polyline()
            {
                SrsId = srsId
            };
        }
        else if (firstShape.X.HasValue && firstShape.Y.HasValue)
        {
            shape = new Point()
            {
                SrsId = srsId
            };
        }

        #endregion

        if (shape is Point)
        {
            ((Point)shape).X = firstShape.X.Value;
            ((Point)shape).Y = firstShape.Y.Value;
            if (hasZ)
            {
                ((Point)shape).Z = firstShape.Z.HasValue ? firstShape.Z.Value : 0;
            }
            if (hasM)
            {
                shape = new PointM((Point)shape, firstShape.M);
            }
        }
        else if (shape is Polyline)
        {
            foreach (var feature in Features)
            {
                var geometry = feature.Geometry;
                if (geometry == null || geometry.Paths == null)
                {
                    continue;
                }

                for (var p = 0; p < geometry.Paths.Length; p++)
                {
                    var path = geometry.Paths[p];
                    var shapePath = new Path();
                    for (int i = 0; i < path.GetLength(0); i++)
                    {
                        if (!path[i, 0].HasValue || !path[i, 1].HasValue)
                        {
                            throw new Exception("Invalid geometry");
                        }

                        //shapePath.AddPoint(new Point(path[i, 0].Value, path[i, 1].Value));
                        shapePath.AddPoint(CreatePoint(path, i, hasZ, hasM));
                    }
                    ((Polyline)shape).AddPath(shapePath);
                }
            }
        }
        else if (shape is Polygon)
        {
            foreach (var feature in Features)
            {
                var geometry = feature.Geometry;
                if (geometry == null || geometry.Rings == null)
                {
                    continue;
                }

                for (var p = 0; p < geometry.Rings.Length; p++)
                {
                    var ring = geometry.Rings[p];
                    var shapeRing = new Ring();
                    for (int i = 0; i < ring.GetLength(0); i++)
                    {
                        if (!ring[i, 0].HasValue || !ring[i, 1].HasValue)
                        {
                            throw new Exception("Invalid geometry");
                        }

                        //shapeRing.AddPoint(new Point(ring[i, 0].Value, ring[i, 1].Value));
                        shapeRing.AddPoint(CreatePoint(ring, i, hasZ, hasM));
                    }
                    ((Polygon)shape).AddRing(shapeRing);
                }
            }
        }

        return shape;
    }

    #region Helper

    private Point CreatePoint(double?[,] coords, int coordIndex, bool hasZ, bool hasM)
    {
        int index = 0;

        var point = new Point(coords[coordIndex, index++].Value, coords[coordIndex, index++].Value);

        if (hasZ && index < coords.GetLength(1))
        {
            point.Z = coords[coordIndex, index].HasValue
                ? coords[coordIndex, index].Value
                : 0D;

            index++;
        }

        if (hasM && index < coords.GetLength(1))
        {
            point = new PointM(point, coords[coordIndex, index++]);
        }

        return point;
    }

    #endregion
}
