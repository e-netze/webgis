#nullable enable

using E.Standard.Json;
using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Editing;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Exceptions;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Extensions;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json.FeatureServer;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json.Geometry;
using E.Standard.WebMapping.GeoServices.ArcServer.Services;
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
    private string _errMsg = String.Empty;
    private JsonFeatureLayer? _featureLayer;
    private EsriFeature _currentFeature;
    private List<EsriFeature> _addFeatures = new List<EsriFeature>();
    private List<EsriFeature> _updateFeatures = new List<EsriFeature>();
    private List<string> _deleteFeatureIds = new List<string>();
    private IRequestContext? _requestContext;
    private readonly IMapServiceAuthentication _mapServiceAuth;

    public FeatureService(MapService mapService)
    {
        _mapServiceAuth = new MapService()
        {
            TokenExpiration = mapService.TokenExpiration
        };

        _currentFeature = new EsriFeature();
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

    public string? CurrentFeatureGeometry
    {
        get
        {
            return _currentFeature.Geometry?.ToString();
        }

        set
        {
            ArgumentNullException.ThrowIfNull(_featureLayer, nameof(_featureLayer));

            XmlDocument doc = new XmlDocument();

            if (String.IsNullOrEmpty(value))
            {
                _currentFeature.Geometry = null;
                return;
            }

            doc.LoadXml(value);

            Shape shape = Shape.FromArcXML(doc.ChildNodes[0], null);
            string jsonGeometry = RestHelper.ConvertGeometryToJson(shape, this.SrsId, _featureLayer.HasZ, _featureLayer.HasM);
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
        ArgumentNullException.ThrowIfNull(_featureLayer, nameof(_featureLayer));

        if (_currentFeature.Attributes.ContainsKey(_featureLayer.IdFieldName())
            && !String.IsNullOrEmpty(_currentFeature.Attributes[_featureLayer.IdFieldName()]?.ToString()))
        {
            _deleteFeatureIds.Add(_currentFeature.Attributes[_featureLayer.IdFieldName()]?.ToString()!);

            return Task.FromResult(true);
        }
        else
        {
            _errMsg = "No ObjectID available for the feature to be deleted.";

            return Task.FromResult(false);
        }
    }

    public void DisConnect()
    {
    }

    public string? GetCurrentFeatureAttributValue(string name)
    {
        string fieldname = name.Substring(name.LastIndexOf(".") + 1);

        if (_currentFeature?.Attributes?.ContainsKey(fieldname) == true)
        {
            return _currentFeature.Attributes[fieldname]?.ToString();
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
            ArgumentNullException.ThrowIfNull(_featureLayer, nameof(_featureLayer));

            _currentFeature.Attributes[_featureLayer.IdFieldName()] = ID;

            return true;
        }
        else
        {
            _errMsg = "No Feature found to update";

            return false;
        }
    }

    public bool SetCurrentFeatureAttribute(string name, string value)
    {
        try
        {
            string fieldname = name.Substring(name.LastIndexOf(".") + 1);

            var layerField = _featureLayer?
                            .Fields?
                            .FirstOrDefault(f => fieldname.Equals(f.Name, StringComparison.OrdinalIgnoreCase));

            if (layerField == null)
            {
                throw new Exception($"Layer does not contails a fileld {fieldname}");
            }

            object? typedValue = layerField.TypedValueOrDefault(value);

            _currentFeature.Attributes[fieldname] = typedValue;

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
        ArgumentNullException.ThrowIfNull(_featureLayer, nameof(_featureLayer));

        if (_currentFeature.Attributes.ContainsKey(_featureLayer.IdFieldName()))
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

    async public Task<bool> Commit()
    {
        if (new int[] { _addFeatures.Count, _updateFeatures.Count, _deleteFeatureIds.Count }.Where(c => c > 0).Count() > 1)
        {
            throw new Exception("Not allowed: you can only insert, update or delete features in the same commit.");
        }

        if (_addFeatures.Count > 0)
        {
            string jsonFeature = ConvertFeatures2JsonString(_addFeatures.ToArray());
            string postData = "f=json&features=" + System.Net.WebUtility.UrlEncode(jsonFeature);
            _addFeatures.Clear();

            return await SendRequest("addFeatures", postData);
        }
        else if (_updateFeatures.Count > 0)
        {
            string jsonFeature = ConvertFeatures2JsonString(_updateFeatures.ToArray());
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

    private async Task<bool> GetFieldAndGeometryTypes()
    {
        try
        {
            ArgumentNullException.ThrowIfNull(_requestContext, nameof(_requestContext));

            var authHandler = _requestContext.GetRequiredService<AgsAuthenticationHandler>();

            // Feldtypen auslesen
            string response = await authHandler.TryPostAsync(
                        _mapServiceAuth,
                        _mapServiceAuth.Service, "f=json");

            _featureLayer = JSerializer.Deserialize<JsonFeatureLayer>(response);

            if (_featureLayer?.Fields is null)
            {
                var error = JSerializer.Deserialize<JsonError>(response);

                if (error?.error is not null)
                {
                    _errMsg = String.Format("{0}. Details: {1}", error.error.message, error.error.details);
                    throw new Exception("Internal Server Error: Can't get edit layer information: " + _errMsg);
                }

                throw new Exception("Internal Server Error: Can't get get layer field information!");
            }

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

    async public Task<EditUndoableDTO?> CreateUndo(IAppCryptography crypto,
                                               IEnumerable<long> objectIds,
                                               SqlCommand command,
                                               string[]? fields = null)
    {
        ArgumentNullException.ThrowIfNull(_featureLayer, nameof(_featureLayer));

        string outFields = "*";
        bool returnGeometry = true;

        if (fields != null)
        {
            var outFieldsList = new List<string>(fields);

            if (!outFieldsList.Contains(_featureLayer.IdFieldName()))
            {
                outFieldsList.Insert(0, _featureLayer.IdFieldName());
            }

            if (!outFieldsList.Contains(_featureLayer.ShapeFileName()))
            {
                returnGeometry = false;
            }

            outFields = String.Join(",", outFieldsList);
        }

        string where = $"{_featureLayer.IdFieldName()} in ({String.Join(",", objectIds)})";

        var postData = new StringBuilder($"f=json&where={where}&outFields={outFields}&returnGeometry={returnGeometry.ToString().ToLower()}");
        if (returnGeometry)
        {
            postData.Append($"&returnM={(_featureLayer.HasM ? "true" : "")}&returnZ={(_featureLayer.HasZ ? "true" : "")}");
        }

        if (this.SrsId > 0)
        {
            postData.Append($"&outSR={this.SrsId}");
        }

        var sendRequestResult = await SendRequestPro("query", postData.ToString());

        if (sendRequestResult.success)
        {
            string response = sendRequestResult.response;
            var esriFeatures = JSerializer.Deserialize<EsriFeatures>(response)!;

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
                        // Darum sollte man hier das OBJECTID Feld nicht speichern,
                        // weil man das später beim Insert nicht übergeben darf
                        //

                        feature.Attributes.Remove(_featureLayer.IdFieldName());
                    }

                    //
                    // Spatial Reference auf Feature setzten,
                    // damit später beim speichern die richtige Projektion an den
                    // FeatureServer übergeben wird.
                    //
                    if (feature.Geometry != null && feature.Geometry.SpatialReference == null)
                    {
                        feature.Geometry.SpatialReference = esriFeatures.SpatialReference;
                    }
                }
            }

            var previewShape = esriFeatures?.AsAggregatedShape(this.SrsId, _featureLayer.HasZ, _featureLayer.HasM);
            return new EditUndoableDTO(command, previewShape)
            {
                FeatureCount = esriFeatures?.Features?.Count() ?? 0,
                Data = crypto.SecurityEncryptString(
                            JSerializer.Serialize(esriFeatures!.Features)/*, CryptoResultStringType.Base64*/
                        )
            };
        }
        else
        {
            return null;
        }
    }

    async public Task<(bool success, EditUndoableDTO? newEditundoable, long[] affectedObjectIds)> PerformUndo(IAppCryptography crypto, EditUndoableDTO editUndoable)
    {
        EditUndoableDTO? newEditUndoable = null;
        var data = crypto.SecurityDecryptString(editUndoable.Data);
        string action = "";
        long[]? objectIds = null;

        switch (editUndoable.Command)
        {
            case SqlCommand.delete:
                await GetFieldAndGeometryTypes();
                action = "addFeatures";
                break;
            case SqlCommand.update:
                await GetFieldAndGeometryTypes();
                var features = JSerializer.Deserialize<IEnumerable<EsriFeature>>(data);

                if (features is null)
                {
                    throw new Exception("Unknown Error: No feature stored inside this undo entity");
                }

                objectIds = features?
                                .Select(f => Convert.ToInt64(f.Attributes[_featureLayer.IdFieldName()]))
                                .ToArray() ?? [];

                if (features!.FirstOrDefault()?.Geometry == null)
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

        return (success: await SendRequest(action, postData),
                newEditundoable: newEditUndoable,
                affectedObjectIds: objectIds ?? []);
    }

    public IEnumerable<int> CommitedObjectIds { get; set; } = [];

    #endregion

    #region Helpers

    private string ConvertFeatures2JsonString(EsriFeature[] features)
    {
        //return JsonConvert.SerializeObject(obj);
        return JSerializer.Serialize(features);
    }

    async private Task<bool> SendRequest(string action, string postData)
    {
        return (await SendRequestPro(action, postData)).success;
    }

    async private Task<(bool success, string response)> SendRequestPro(string action, string postData)
    {
        ArgumentNullException.ThrowIfNull(_requestContext, nameof(_requestContext));

        var authHandler = _requestContext.GetRequiredService<AgsAuthenticationHandler>();
        string response = await authHandler.TryPostAsync(_mapServiceAuth, _mapServiceAuth.Service + action, postData);

        var jsonResponse = JSerializer.Deserialize<JsonFeatureServerResponse>(response)!;

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
