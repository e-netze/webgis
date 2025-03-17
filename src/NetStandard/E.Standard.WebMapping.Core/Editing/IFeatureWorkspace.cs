using E.Standard.Security.Cryptography.Abstractions;
using E.Standard.WebMapping.Core.Abstraction;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace E.Standard.WebMapping.Core.Editing;

public enum SqlCommand { update, insert, delete }

public interface IFeatureWorkspace
{
    string LastErrorMessage { get; }
    /// <summary>
    /// Connectionstring f�r das jeweilige Format wird �bergeben
    /// </summary>
    string ConnectionString { set; }
    /// <summary>
    /// Verbinding wird eingerichtet
    /// </summary>
    /// <returns>wahr, wenn erfolgreich</returns>
    bool Connect(SqlCommand mode);
    /// <summary>
    /// Setzt den Cursor auf den Objekt
    /// </summary>
    /// <param name="ID">ID des Objektes</param>
    /// <returns></returns>
    bool MoveTo(int ID);
    /// <summary>
    /// Die Geometry wird in Form eines XML (ArcXML) �bergeben bzw. abgeholt
    /// </summary>
    string CurrentFeatureGeometry { set; get; }
    /// <summary>
    /// Setzt ein Attribut
    /// </summary>
    /// <param name="name">Name des Attributes</param>
    /// <param name="value">Wert des Attributes</param>
    /// <returns>wahr, wenn erfolgreich</returns>
    bool SetCurrentFeatureAttribute(string name, string value);
    string GetCurrentFeatureAttributValue(string name);
    bool CurrentFeatureHasAttribute(string name);

    Task<bool> StoreCurrentFeature();
    //Task<bool> Update(string where);
    Task<bool> DeleteCurrentFeature();
    //Task<bool> Delete(string where);
    Task<bool> Commit();

    void DisConnect();
    List<string> FieldNames { get; }

    string VersionName
    {
        get;
        set;
    }

    bool RebuildSpatialIndex { get; set; }
}

public interface IFeatureWorkspaceSpatialReference : IFeatureWorkspace
{
    int SrsId { get; set; }
}

public interface IWebFeatureWorkspace : IFeatureWorkspace
{
    Task SetWebCredentials(IRequestContext requestContext, ICredentials credentials);

    //string Server { get; }
}

public interface IFeatureWorkspaceGeometryOperations : IFeatureWorkspace
{
    bool ClosePolygonRings { get; }
    bool CleanRings { get; }
}

//public interface IFeatureWorkspaceLayerInfo
//{
//    string LayerIdFieldname { get;  set; }
//}

public interface IFeatureWorkspaceUndo
{
    Task<EditUndoableDTO> CreateUndo(IAppCryptography crytpo, IEnumerable<long> objectIds, SqlCommand command, string[] fields = null);

    Task<(bool success, EditUndoableDTO newEditundoable, long[] affectedObjectIds)> PerformUndo(IAppCryptography crytpo, EditUndoableDTO editUndoable);

    IEnumerable<int> CommitedObjectIds { get; }
}

public class EditUndoableDTO
{
    public EditUndoableDTO(SqlCommand command, Geometry.Shape shape)
    {
        this.Command = command;
        this.Shape = shape;
    }

    [JsonProperty(PropertyName = "count")]
    [System.Text.Json.Serialization.JsonPropertyName("count")]
    public int FeatureCount { get; set; }

    [JsonProperty(PropertyName = "command")]
    [System.Text.Json.Serialization.JsonPropertyName("command")]
    public SqlCommand Command { get; set; }

    [JsonProperty(PropertyName = "etd")]
    [System.Text.Json.Serialization.JsonPropertyName("etd")]
    public string EditThemeDef { get; set; }

    [JsonProperty(PropertyName = "data")]
    [System.Text.Json.Serialization.JsonPropertyName("data")]
    public string Data { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public Geometry.Shape Shape { get; set; }
}

public interface IFeatureLayerConnectionString
{
    string ConnectionString { get; set; }
}
