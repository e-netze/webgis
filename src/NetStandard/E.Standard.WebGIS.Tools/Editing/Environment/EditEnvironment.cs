using E.Standard.CMS.Core.Extensions;
using E.Standard.DbConnector;
using E.Standard.DbConnector.Exceptions;
using E.Standard.Extensions;
using E.Standard.Extensions.Compare;
using E.Standard.Extensions.ErrorHandling;
using E.Standard.Extensions.IO;
using E.Standard.Json;
using E.Standard.Localization.Abstractions;
using E.Standard.Platform;
using E.Standard.Security.Core;
using E.Standard.WebGIS.CMS;
using E.Standard.WebGIS.Tools.Editing.Advanced.Extensions;
using E.Standard.WebGIS.Tools.Editing.Extensions;
using E.Standard.WebGIS.Tools.Editing.Models;
using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Api.UI.Elements.Advanced;
using E.Standard.WebMapping.Core.Collections;
using E.Standard.WebMapping.Core.Editing;
using E.Standard.WebMapping.Core.Extensions;
using E.Standard.WebMapping.Core.Geometry;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace E.Standard.WebGIS.Tools.Editing.Environment;

class EditEnvironment
{
    private readonly List<HistoryItem> _historyItems = new List<HistoryItem>();
    private readonly string _fieldPrefix;

    private EditEnvironment(string appAssemblyPath, string appEtcPath, string editFieldPrefix)
    {
        _fieldPrefix = editFieldPrefix ?? "editfield";

        this.AppAssemblyPath = appAssemblyPath;
        this.EditRootPath = appEtcPath + @"/editing";
    }

    public EditEnvironment(IBridge bridge, ApiToolEventArguments e, EditThemeDefinition defaultEditThemeDefintion = null, string editFieldPrefix = null)
        : this(bridge.AppAssemblyPath, bridge.AppEtcPath, editFieldPrefix)
    {
        string editThemeDefinition = e[$"_{_fieldPrefix}_edittheme_def"];
        if (!String.IsNullOrWhiteSpace(editThemeDefinition))
        {
            EditThemeDefinition = ApiToolEventArguments.FromArgument<EditThemeDefinition>(editThemeDefinition);
        }
        else
        {
            EditThemeDefinition = defaultEditThemeDefintion;
        }

        this.Bridge = bridge;
    }

    public EditEnvironment(IBridge bridge, EditUndoableDTO editUndoable, string editFieldPrefix = null)
        : this(bridge.AppAssemblyPath, bridge.AppEtcPath, editFieldPrefix)
    {
        if (String.IsNullOrWhiteSpace(editUndoable?.EditThemeDef))
        {
            throw new ArgumentException("Inconsistant undoable: no EditThemeDef");
        }

        EditThemeDefinition = ApiToolEventArguments.FromArgument<EditThemeDefinition>(editUndoable.EditThemeDef);
        this.Bridge = bridge;
    }

    public EditEnvironment(IBridge bridge, EditThemeDefinition editThemeDefinition, string editFieldPrefix = null)
        : this(bridge.AppAssemblyPath, bridge.AppEtcPath, editFieldPrefix)
    {
        this.EditThemeDefinition = editThemeDefinition;
        this.Bridge = bridge;
    }

    internal IBridge Bridge { get; set; }

    #region Properties

    public string EditRootPath
    {
        get;
        private set;
    }

    public string AppAssemblyPath
    {
        get;
        private set;
    }

    public double CurrentMapScale
    {
        get;
        set;
    }

    public int CurrentMapSrsId { get; set; }

    #endregion

    public EditTheme this[string themeId]
    {
        get
        {
            if (String.IsNullOrEmpty(EditRootPath))
            {
                return null;
            }

            if (this.EditThemeDefinition != null)
            {
                // EditThema wurde im CMS definiert
                // XmlNode wird aus CMS erzeugt...
                var node = this.Bridge.TryGetEditThemeMaskXmlNode(this.EditThemeDefinition.ServiceId, this.EditThemeDefinition.EditThemeId);
                if (node != null)
                {
                    XmlNamespaceManager ns = new XmlNamespaceManager(node.OwnerDocument.NameTable);
                    ns.AddNamespace("webgis", "http://www.e-steiermark.com/webgis");
                    ns.AddNamespace("edit", "http://www.e-steiermark.com/webgis/edit");

                    return new EditTheme(this, node, ns);
                }
            }

            DirectoryInfo di = new DirectoryInfo(System.IO.Path.Combine(EditRootPath, "themes"));
            if (di.Exists)
            {
                foreach (FileInfo fi in di.GetFiles("*.xml"))
                {
                    try
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(fi.FullName);
                        XmlNamespaceManager ns = new XmlNamespaceManager(doc.NameTable);
                        ns.AddNamespace("webgis", "http://www.e-steiermark.com/webgis");
                        ns.AddNamespace("edit", "http://www.e-steiermark.com/webgis/edit");

                        XmlNode node = doc.SelectSingleNode("editthemes/edit:edittheme[@id='" + themeId + "']", ns);
                        // node = node.CloneNode(true);  
                        if (node != null)
                        {
                            return new EditTheme(this, node, ns);
                        }
                    }
                    catch { }
                }
            }

            return null;
        }
    }

    public EditTheme this[ApiToolEventArguments e]
    {
        get
        {
            string themeId = e[EditThemeElementId];

            if (String.IsNullOrWhiteSpace(themeId))
            {
                return null;
            }

            var editTheme = this[themeId];

            //#region Falls Editthema im CMS definiert wurde nocheinmal mit einer EditThemeDefintion probieren (die ist NULL wenn zB über Collector App editiert wird)

            //if (editTheme==null && this.EditThemeDefinition == null)
            //{
            //    this.EditThemeDefinition = new EditThemeDefinition()
            //    {
            //        ServiceId = e["serviceid"],
            //        EditThemeId = themeId
            //    };
            //    editTheme = this[themeId];

            //    this.EditThemeDefinition = null;
            //}

            //#endregion

            return editTheme;
        }
    }

    public EditThemeDefinition EditThemeDefinition
    {
        get; internal set;
    }

    public WebMapping.Core.Feature GetFeature(IBridge bridge, ApiToolEventArguments e, bool checkApplyField = false)
    {
        WebMapping.Core.Feature feature = new WebMapping.Core.Feature();

        feature.Oid = GetFeatureOid(bridge, e);
        feature.GlobalOid = e[$"_{_fieldPrefix}_globaloid"];

        // Original Verwenden (eventuell an client transformiert)?
        feature.Shape = e.Sketch;
        // Oder WGS vom Client wieder zurück transformieren...? 
        /*
        using (var transformer = bridge.GeometryTransformer(4326, this.CurrentMapSrsId))
        {
            var shape = e.SketchWgs84;
            transformer.Transform(shape);
            feature.Shape = shape;
        }
         * */

        foreach (string property in e.Properties)
        {
            if (property.StartsWith($"{_fieldPrefix}_"))
            {
                string attribute = property.Substring(_fieldPrefix.Length + 1);

                if (checkApplyField)
                {
                    if (!e[$"_{_fieldPrefix}_applyfield_{attribute}"].ApplyField())
                    {
                        continue;
                    }
                }

                string val = e[property];
                feature.Attributes.Add(new WebMapping.Core.Attribute(attribute, val));
            }
        }

        return feature;
    }

    public string EditThemeElementId => $"_{_fieldPrefix}_themeid";

    public string EditThemeDefintionElementId => $"_{_fieldPrefix}_edittheme_def";

    public string FeatureOidElementId => $"_{_fieldPrefix}_oid";

    public int GetFeatureOid(IBridge bridge, ApiToolEventArguments e)
    {
        if (!String.IsNullOrWhiteSpace(e[FeatureOidElementId]))
        {
            return int.Parse(e[FeatureOidElementId]);
        }

        return -1;
    }

    #region Database Operatoins (Insert, Udate, Delete)

    async public Task<bool> InsertFeature(EditTheme editTheme, WebMapping.Core.Feature feature)
    {
        return await CommitFeaturesAsync(editTheme, new WebMapping.Core.Feature[] { feature }, EditFeatureCommand.Insert);
    }

    async public Task<bool> InserFeatures(EditTheme editTheme, IEnumerable<WebMapping.Core.Feature> features)
    {
        return await CommitFeaturesAsync(editTheme, features, EditFeatureCommand.Insert);
    }

    async public Task<bool> UpdateFeature(EditTheme editTheme, WebMapping.Core.Feature feature)
    {
        return await CommitFeaturesAsync(editTheme, new WebMapping.Core.Feature[] { feature }, EditFeatureCommand.Update);
    }

    async public Task<bool> MassAttributeFeatures(EditTheme editTheme, WebMapping.Core.Feature featureTemplate, int[] objectIds)
    {
        return await CommitFeaturesAsync(editTheme,
                                         objectIds.Select(objectId =>
                                         {
                                             var feature = featureTemplate.Clone();
                                             feature.Oid = objectId;

                                             return feature;
                                         }),
                                         EditFeatureCommand.MassAttribution);
    }

    async public Task<bool> TransferFeatures(EditTheme editTheme,
                                             IEnumerable<WebMapping.Core.Feature> features,
                                             bool pipelineSuppressAutovalues,
                                             bool pipelineSuppressValidation)
    {
        return await CommitFeaturesAsync(editTheme, features,
                                         EditFeatureCommand.Transfer,
                                         pipelineSuppressAutovalues: pipelineSuppressAutovalues,
                                         pipelineSuppressValidation: pipelineSuppressValidation);
    }

    async public Task<bool> UpdateFeatures(EditTheme editTheme, IEnumerable<WebMapping.Core.Feature> features)
    {
        return await CommitFeaturesAsync(editTheme, features, EditFeatureCommand.Update);
    }

    internal enum EditFeatureCommand
    {
        Insert = 0,
        Update = 1,
        Delete = 2,
        MassAttribution = 3,
        Transfer = 4
    }

    async private Task<bool> CommitFeaturesAsync(EditTheme editTheme,
                                                 IEnumerable<WebMapping.Core.Feature> features,
                                                 EditFeatureCommand command,
                                                 bool pipelineSuppressAutovalues = false,
                                                 bool pipelineSuppressValidation = false)
    {
        if (features == null || features.Count() == 0)
        {
            throw new ArgumentException("No features to update");
        }

        if (editTheme == null)
        {
            throw new ArgumentException("No Edittheme. Parameter editTheme == null");
        }

        if ((command == EditFeatureCommand.Update || command == EditFeatureCommand.MassAttribution) &&
            features.Where(f => f.Oid <= 0).Count() > 0)
        {
            throw new ArgumentException("Not all features can updated. Some features don't have on OBJECTID set.");
        }

        if ((command == EditFeatureCommand.Insert ||
             command == EditFeatureCommand.Transfer) && editTheme.AutoExplodeMultipartFeatures)
        {
            features = features.ExplodeMultipartFeatures();
        }

        IFeatureWorkspace ws = null;

        try
        {
            #region DB rights

            var dbRights = editTheme.DbRights;
            editTheme.CheckEditPermissions(command, features);

            #endregion

            #region Create Workspace and Connect

            ws = await editTheme.FeatureWorkspace();
            if (ws == null)
            {
                throw new Exception("Can't create editthemes feature workspace");
            }

            if (!ws.Connect((command == EditFeatureCommand.Update || command == EditFeatureCommand.MassAttribution) ? SqlCommand.update : SqlCommand.insert))
            {
                throw new Exception(ws.LastErrorMessage);
            }

            //if (ws is IFeatureWorkspaceLayerInfo)
            //{
            //    var service = await this.Bridge.GetService(this.EditThemeDefinition?.ServiceId);
            //    if (service == null)
            //        throw new Exception("Unknown edit service");
            //    var layer = service.Layers.Where(l => l.Id == this.EditThemeDefinition.LayerId).FirstOrDefault();
            //    if (layer == null)
            //        throw new Exception("Unknown edit layer");

            //    ((IFeatureWorkspaceLayerInfo)ws).LayerIdFieldname = layer.IdFieldname;
            //}

            if (ws is IWebFeatureWorkspace)
            {
                //string _ = ((IWebFeatureWorkspace)ws).Server;
                await ((IWebFeatureWorkspace)ws).SetWebCredentials(this.Bridge.RequestContext, null);
            }

            if (ws is IFeatureWorkspaceSpatialReference)
            {
                ((IFeatureWorkspaceSpatialReference)ws).SrsId = editTheme.SrsId();
            }

            #endregion

            #region Create Undo (Update)

            if (command == EditFeatureCommand.Update || command == EditFeatureCommand.MassAttribution)
            {
                var undoable = await features.CreateUndoable(this.Bridge,
                                                             ws,
                                                             SqlCommand.update,
                                                             command == EditFeatureCommand.MassAttribution ? features.First().Attributes?.Select(a => a.Name) : null);
                if (undoable != null)
                {
                    undoable.EditThemeDef = ApiToolEventArguments.ToArgument(this.EditThemeDefinition);
                    this.AddUndoable(undoable);
                }
            }

            #endregion

            foreach (var feature in features)
            {
                #region Feature Geometry and Attributes

                if (command == EditFeatureCommand.Update || command == EditFeatureCommand.MassAttribution)
                {
                    if (feature.Oid <= 0)
                    {
                        throw new ArgumentException("No feautre to update!");
                    }

                    ws.MoveTo(feature.Oid);
                }

                if (feature.Shape != null)
                {
                    if (dbRights.Contains("g"))
                    {
                        feature.Shape.SrsId = editTheme.DatasetSrsId(feature.Shape.SrsId);   // Kompatibilität zu OLE, ESP!!!!

                        if (ws is IFeatureWorkspaceSpatialReference)
                        {
                            if (feature.Shape.SrsId > 0)
                            {
                                ((IFeatureWorkspaceSpatialReference)ws).SrsId = feature.Shape.SrsId;
                            }
                        }

                        if (ws is IFeatureWorkspaceGeometryOperations)
                        {
                            var sRef = CoreApiGlobals.SRefStore.SpatialReferences.ById(feature.Shape.SrsId);
                            var tolerance = sRef != null && sRef.IsProjective ? 5e-4 : 5e-8;

                            if (feature.Shape is Polygon)
                            {
                                if (((IFeatureWorkspaceGeometryOperations)ws).CleanRings)
                                {
                                    ((Polygon)feature.Shape).CleanRings(tolerance);
                                }

                                if (((IFeatureWorkspaceGeometryOperations)ws).ClosePolygonRings)
                                {
                                    ((Polygon)feature.Shape).CloseAllRings(tolerance);
                                }
                            }
                        }

                        ws.CurrentFeatureGeometry = feature.Shape.ArcXML(null);
                    }
                }

                List<XmlNode> attributes = editTheme.MaskAttributes;

                if (attributes != null)
                {
                    #region alle Eingabefelder schreiben

                    foreach (XmlNode attribute in attributes)
                    {
                        // auch alle Autovalues schreiben, falls sich diese nicht ändnern (zB create_user bei einem Update)

                        // Autovalues die der Anwender nicht schreiben kann "readonly=true" "visible=false" auch nicht schreiben...
                        if (!String.IsNullOrEmpty(attribute.Attributes["autovalue"]?.Value))
                        {
                            if (attribute.Attributes["readonly"]?.Value?.ToLower() == "true")
                            {
                                continue;
                            }

                            if (attribute.Attributes["visible"]?.Value?.ToLower() == "false")
                            {
                                continue;
                            }
                        }
                        else
                        {
                            // do not write readonly values to the database
                            if (attribute.Attributes["readonly"]?.Value?.ToLower() == "true")
                            {
                                continue;
                            }
                        }

                        var fieldAttribute = feature.Attributes[attribute.Attributes["field"]?.Value];
                        if (fieldAttribute != null)
                        {
                            if (!ws.SetCurrentFeatureAttribute(fieldAttribute.Name, StringFormatAttributeValue(attribute, fieldAttribute.Value)))
                            {
                                throw new Exception($"Field {fieldAttribute.Name}: {ws.LastErrorMessage}");
                            }
                        }
                    }

                    #endregion

                    #region AutoValues schreiben

                    if (pipelineSuppressAutovalues != true)
                    {
                        foreach (XmlNode attribute in attributes)
                        {
                            if (attribute.Attributes["autovalue"] == null)
                            {
                                continue;
                            }

                            string autoval = attribute.Attributes["autovalue"].Value;

                            var fieldAttribute = feature.Attributes[attribute.Attributes["field"].Value];
                            if (fieldAttribute == null)
                            {
                                fieldAttribute = new WebMapping.Core.Attribute(attribute.Attributes["field"].Value, String.Empty);
                            }


                            //bool isSpatialQuery = autoval.ToLower().Contains(" from ");
                            var autoValueResult = await GetAutoValue(command,
                                                                     fieldAttribute.Name,
                                                                     editTheme,
                                                                     autoval,
                                                                     feature,
                                                                     custom1: attribute.Attributes["autovalue_custom1"]?.Value,
                                                                     custom2: attribute.Attributes["autovalue_custom2"]?.Value);
                            autoval = autoValueResult.value;
                            bool setIt = autoValueResult.setIt;

                            //if (String.IsNullOrEmpty(autoval) && isSpatialQuery)
                            //{
                            //    IField field = layer.Fields.FindField(e.Arguments[i]);
                            //    if (field != null)
                            //    {
                            //        // für NÖ: die haben keine Daten im Bereich Wien. Es wird sonst
                            //        // eine Leerstring in eine Zahlenfeld geschreiben, was im SDEWorkspace92 eine Convert.ToInt32 fehler liefert!!
                            //        switch (field.Type)  
                            //        {
                            //            case FieldType.SmallInteger:
                            //            case FieldType.Interger:
                            //            case FieldType.BigInteger:
                            //            case FieldType.Double:
                            //            case FieldType.Float:
                            //                setIt = false;
                            //                break;
                            //        }
                            //    }
                            //}

                            if (setIt)
                            {
                                fieldAttribute.Value = autoval;
                                ws.SetCurrentFeatureAttribute(fieldAttribute.Name, StringFormatAttributeValue(attribute, autoval));
                            }
                        }
                    }

                    #endregion

                    #region Feld Validierung / Resistance

                    if (pipelineSuppressValidation != true)
                    {
                        foreach (XmlNode attribute in attributes)
                        {
                            string prompt = attribute.Attributes["prompt"]?.Value ?? attribute.Attributes["field"].Value;
                            string field = attribute.Attributes["field"].Value;

                            if (command == EditFeatureCommand.MassAttribution && feature.Attributes[field] == null)
                            {
                                // Felder die von der Massenattributierung nicht gestzt werden, nicht überprüfen
                                continue;
                            }

                            if (ws.ValidationRecommended(field, command) == false)
                            {
                                continue;
                            }

                            if (attribute.Attributes["required"]?.Value.ToLower() == "true")
                            {
                                string val = ws.GetCurrentFeatureAttributValue(field);
                                var whitespace_accepted = attribute.Attributes["whitespace_accepted"]?.Value.ToLower() == "true";

                                if (String.IsNullOrEmpty(val) ||
                                   (whitespace_accepted == false && String.IsNullOrWhiteSpace(val)))
                                {
                                    throw new Exception($"Der Wert für '{prompt}' ist erforderlich! {attribute.Attributes["validation_error"]?.Value}");
                                }
                            }
                            if (attribute.Attributes["minlen"] != null)
                            {
                                int minlen = int.Parse(attribute.Attributes["minlen"].Value);
                                if (minlen > 0)
                                {
                                    string val = ws.GetCurrentFeatureAttributValue(field);
                                    if (String.IsNullOrEmpty(val) || val.Length < minlen)
                                    {
                                        throw new Exception($"Der Wert für '{prompt}' muss mind. {minlen} Zeichen lang sein! {attribute.Attributes["validation_error"]?.Value}");
                                    }
                                }
                            }
                            if (attribute.Attributes["regex"] != null)
                            {
                                Regex regex = new Regex(attribute.Attributes["regex"].Value);

                                string val = ws.GetCurrentFeatureAttributValue(field);
                                if (val == null)
                                {
                                    val = String.Empty;
                                }

                                if (!regex.IsMatch(val))
                                {
                                    throw new Exception($"Der Wert für '{prompt}' hat das falsche Format! {attribute.Attributes["regex_message"]?.Value ?? attribute.Attributes["validation_error"]?.Value}");
                                }
                            }
                        }
                    }

                    #endregion

                    #region MaskValidations

                    foreach (var validation in editTheme.MaskValidations)
                    {
                        Bridge.ValidateMask(validation, ws.GetCurrentFeatureAttributValue(validation.FieldName));
                    }

                    #endregion
                }

                if (command == EditFeatureCommand.Transfer)
                {
                    // append all other attributes
                    foreach (var featureAttribute in feature.Attributes)
                    {
                        if (attributes.Where(a => a?.Attributes["field"]?.Value == featureAttribute.Name).Any() == false)
                        {
                            if (!ws.SetCurrentFeatureAttribute(featureAttribute.Name, featureAttribute.Value))
                            {
                                throw new Exception("Field " + featureAttribute.Name + ": " + ws.LastErrorMessage);
                            }
                        }
                    }
                }

                #endregion

                if (!await ws.StoreCurrentFeature())
                {
                    throw new Exception(ws.LastErrorMessage);
                }
            }

            if (!await ws.Commit())
            {
                throw new Exception(ws.LastErrorMessage);
            }

            if (ws is IFeatureWorkspaceUndo)
            {
                this.CommitedObjectIds = ((IFeatureWorkspaceUndo)ws).CommitedObjectIds;
            }
        }
        finally
        {
            if (ws != null)
            {
                ws.DisConnect();
            }
        }

        return true;
    }

    async public Task<bool> DeleteFeature(EditTheme editTheme, WebMapping.Core.Feature feature)
    {
        return await DeleteFeatures(editTheme, new WebMapping.Core.Feature[] { feature });
    }

    async public Task<bool> DeleteFeatures(EditTheme editTheme, IEnumerable<WebMapping.Core.Feature> features)
    {
        if (editTheme == null)
        {
            throw new ArgumentException("No Edittheme. Parameter editTheme == null");
        }

        IFeatureWorkspace ws = null;
        try
        {
            #region DB rights

            editTheme.CheckEditPermissions(EditFeatureCommand.Delete, null);

            #endregion

            ws = await editTheme.FeatureWorkspace();
            if (ws == null)
            {
                throw new Exception("Can't create editthemes feature workspace");
            }

            if (!ws.Connect(SqlCommand.update))
            {
                throw new Exception(ws.LastErrorMessage);
            }

            //if (ws is IFeatureWorkspaceLayerInfo)
            //{
            //    var service = await this.Bridge.GetService(this.EditThemeDefinition?.ServiceId);
            //    if (service == null)
            //        throw new Exception("Unknown edit service");
            //    var layer = service.Layers.Where(l => l.Id == this.EditThemeDefinition.LayerId).FirstOrDefault();
            //    if (layer == null)
            //        throw new Exception("Unknown edit layer");

            //    ((IFeatureWorkspaceLayerInfo)ws).LayerIdFieldname = layer.IdFieldname;
            //}

            if (ws is IWebFeatureWorkspace)
            {
                //string _ = ((IWebFeatureWorkspace)ws).Server;
                await ((IWebFeatureWorkspace)ws).SetWebCredentials(this.Bridge.RequestContext, null);
            }

            if (ws is IFeatureWorkspaceSpatialReference)
            {
                ((IFeatureWorkspaceSpatialReference)ws).SrsId = editTheme.SrsId();
            }

            var undoable = await features.CreateUndoable(this.Bridge, ws, SqlCommand.delete);
            if (undoable != null)
            {
                undoable.EditThemeDef = ApiToolEventArguments.ToArgument(this.EditThemeDefinition);
                this.AddUndoable(undoable);
            }

            foreach (var feature in features)
            {
                ws.MoveTo(feature.Oid);

                if (!await ws.DeleteCurrentFeature())
                {
                    throw new Exception(ws.LastErrorMessage);
                }
            }

            if (!await ws.Commit())
            {
                throw new Exception(ws.LastErrorMessage);
            }
        }
        finally
        {
            if (ws != null)
            {
                ws.DisConnect();
            }
        }
        return true;
    }

    public IEnumerable<int> CommitedObjectIds { get; private set; }

    async public Task<(bool succeeded, FeatureCollection updatedFeatures, IEnumerable<IQueryBridge> updatedFeaturesQueries)> Undo(EditTheme editTheme, EditUndoableDTO editUndoable)
    {
        if (editTheme == null)
        {
            throw new ArgumentException("No Edittheme. Parameter editTheme == null");
        }

        IFeatureWorkspace ws = null;
        FeatureCollection updatedFeatures = null;
        IEnumerable<IQueryBridge> updatedFeaturesQueries = null;

        try
        {
            ws = await editTheme.FeatureWorkspace();
            if (!(ws is IFeatureWorkspaceUndo))
            {
                throw new Exception("Can't create undoable feature workspace");
            }

            if (!ws.Connect(SqlCommand.update))
            {
                throw new Exception(ws.LastErrorMessage);
            }

            //if (ws is IFeatureWorkspaceLayerInfo)
            //{
            //    var service = await this.Bridge.GetService(this.EditThemeDefinition?.ServiceId);
            //    if (service == null)
            //        throw new Exception("Unknown edit service");
            //    var layer = service.Layers.Where(l => l.Id == this.EditThemeDefinition.LayerId).FirstOrDefault();
            //    if (layer == null)
            //        throw new Exception("Unknown edit layer");

            //    ((IFeatureWorkspaceLayerInfo)ws).LayerIdFieldname = layer.IdFieldname;
            //}

            if (ws is IFeatureWorkspaceSpatialReference)
            {
                ((IFeatureWorkspaceSpatialReference)ws).SrsId = editTheme.SrsId();
            }

            if (ws is IWebFeatureWorkspace)
            {
                await ((IWebFeatureWorkspace)ws).SetWebCredentials(this.Bridge.RequestContext, null);
            }

            var performUndoResult = await ((IFeatureWorkspaceUndo)ws).PerformUndo(this.Bridge, editUndoable);
            if (!performUndoResult.success)
            {
                throw new Exception(ws.LastErrorMessage);
            }

            if (performUndoResult.newEditundoable != null)
            {
                performUndoResult.newEditundoable.EditThemeDef = ApiToolEventArguments.ToArgument(this.EditThemeDefinition);
                AddUndoable(performUndoResult.newEditundoable);
            }

            if (performUndoResult.affectedObjectIds != null && performUndoResult.affectedObjectIds.Length > 0)
            {
                var filter = new ApiOidsFilter(performUndoResult.affectedObjectIds)
                {
                    QueryGeometry = false
                };

                updatedFeatures = await this.Bridge.QueryLayerAsync(this.EditThemeDefinition.ServiceId, this.EditThemeDefinition.LayerId, filter);
                updatedFeaturesQueries = await this.Bridge.GetLayerQueries(this.EditThemeDefinition.ServiceId, this.EditThemeDefinition.LayerId);
            }
        }
        finally
        {
            if (ws != null)
            {
                ws.DisConnect();
            }
        }

        return (true, updatedFeatures, updatedFeaturesQueries);
    }

    #endregion

    #region Undo Environment

    private readonly List<EditUndoableDTO> _undoables = new List<EditUndoableDTO>();

    private void AddUndoable(EditUndoableDTO undoable)
    {
        if (undoable != null)
        {
            _undoables.Add(undoable);
        }
    }

    public bool HasUndoables => _undoables.Count >= 0;

    public IEnumerable<EditUndoableDTO> Undoables => _undoables.ToArray();

    #endregion

    #region Classes

    public class EditTheme
    {
        private readonly EditEnvironment _editEnvironment;
        private readonly XmlNode _node;
        private readonly XmlNamespaceManager _ns;
        private string _idPrefix = String.Empty;

        public EditTheme(EditEnvironment editEnvironment, XmlNode node, XmlNamespaceManager ns)
        {
            _editEnvironment = editEnvironment;
            _node = node;
            _ns = ns;

            var helper = new EditInsertActionHelper(node, ns);
            this.AutoExplodeMultipartFeatures = helper.AutoExplodeMultipartFeatures;

        }

        async virtual public Task<IFeatureWorkspace> FeatureWorkspace()
        {
            if (_node == null)
            {
                return null;
            }

            XmlNode connectionNode = _node.SelectSingleNode("edit:connection[@id]", _ns);
            if (connectionNode == null || connectionNode.Attributes["id"].Value == "#")
            {
                #region Service is Workspaceprovoder ?

                if (_editEnvironment?.EditThemeDefinition != null)
                {
                    var workspace = await _editEnvironment.Bridge.TryGetFeatureWorkspace(
                        _editEnvironment.EditThemeDefinition.ServiceId,
                        _editEnvironment.EditThemeDefinition.LayerId);

                    if (workspace != null)
                    {
                        return workspace;
                    }
                }

                #endregion

                throw new Exception("ConnectionXmlNode is NULL!");
            }

            XmlDocument doc = new XmlDocument();
            doc.Load(_editEnvironment.EditRootPath + @"/workspaces/workspaces.xml");
            XmlNode datasetNode = doc.SelectSingleNode("//DATASET[@id='" + connectionNode.Attributes["id"].Value + "' and @connectionstring]");
            if (datasetNode == null)
            {
                throw new Exception("DatasetXmlNode is NULL!");
            }

            string connectionString = datasetNode.Attributes["connectionstring"].Value;

            #region Replace ConnectionString Placeholders from XML Values 

            XmlNode layerNode = _node.SelectSingleNode("edit:layer[@name]", _ns);
            if (layerNode != null)
            {
                connectionString = connectionString.Replace("[LAYER]", layerNode.Attributes["name"].Value);
            }

            XmlNode layerIdNode = _node.SelectSingleNode("edit:layer_id[@name]", _ns);
            if (layerIdNode != null)
            {
                connectionString = connectionString.Replace("[LAYER_ID]", layerIdNode.Attributes["name"].Value);
            }

            XmlNode layerShapeNode = _node.SelectSingleNode("edit:layer_shape[@name]", _ns);
            if (layerShapeNode != null)
            {
                connectionString = connectionString.Replace("[LAYER_SHAPE]", layerShapeNode.Attributes["name"].Value);
            }

            #endregion

            XmlNode workspaceNode = datasetNode.ParentNode;
            if (workspaceNode.Attributes["assembly"] == null || workspaceNode.Attributes["instance"] == null)
            {
                throw new Exception("No assembly- and instance-attribute in WorkspaceXmlNode!");
            }

            DirectoryInfo di = new DirectoryInfo(_editEnvironment.EditRootPath);
            try
            {
                Assembly assembly = Assembly.LoadFrom(_editEnvironment.AppAssemblyPath + @"/" + workspaceNode.Attributes["assembly"].Value);
                IFeatureWorkspace ws = assembly.CreateInstance(workspaceNode.Attributes["instance"].Value, false) as IFeatureWorkspace;

                if (ws != null)
                {
                    ws.ConnectionString = connectionString;
                    ws.VersionName = this.VersionName;
                    ws.RebuildSpatialIndex = this.RebuildSpatialIndex;

                    return ws;
                }
            }
            catch (Exception)
            {
                throw;
            }
            return null;
        }

        internal EditEnvironment EditEnvironment => _editEnvironment;

        private bool WorkspaceImplementsUndo
        {
            get
            {
                //
                // Quick & Dirty
                // Man sollte eigentlich den Workspace erzeugen probieren
                // Braucht man aber zZ nur für Save and select, was nur mit FeatureService funktioniert
                //
                if (_node == null)
                {
                    return false;
                }

                XmlNode connectionNode = _node.SelectSingleNode("edit:connection[@id]", _ns);
                if (connectionNode != null && connectionNode.Attributes["id"].Value == "#")
                {
                    return true;
                }

                return false;
            }
        }

        virtual public string DbUsername
        {
            get
            {
                if (_node == null)
                {
                    return String.Empty;
                }

                XmlNode connectionNode = _node.SelectSingleNode("edit:connection[@id]", _ns);
                if (connectionNode == null)
                {
                    return String.Empty;
                }

                XmlDocument doc = new XmlDocument();
                doc.Load(_editEnvironment.EditRootPath + @"/workspaces/workspaces.xml");
                XmlNode datasetNode = doc.SelectSingleNode("//DATASET[@id='" + connectionNode.Attributes["id"].Value + "' and @connectionstring]");
                if (datasetNode == null)
                {
                    return String.Empty;
                }

                string connectionString = datasetNode.Attributes["connectionstring"].Value;

                if (!String.IsNullOrEmpty(connectionString))
                {
                    String user = CMS.Globals.ExtractValue(connectionString, "uid");
                    if (String.IsNullOrEmpty(user))
                    {
                        user = CMS.Globals.ExtractValue(connectionString, "userid");
                    }

                    if (String.IsNullOrEmpty(user))
                    {
                        user = CMS.Globals.ExtractValue(connectionString, "user");
                    }

                    return user;
                }
                return String.Empty;
            }
        }

        public const string EditMaskContainerId = "edit-editmask-container";
        public const string EditNewFeatuerCounterId = "edit-editmask-newfeatures-counter";
        public const string EditFeatureIdsSubsetId = "edit-editmask-feature-ids-subset";
        public const string EditOriginalFeaturePreviewDataId = "edit-editmask-originalfeatures-preview-data";
        public const string EditNewFeaturePreviewDataId = "edit-editmask-newfeatures-preview-data";

        async public Task<UIEditMask> ParseMask(IBridge bridge,
                                                EditThemeDefinition editThemeDef,
                                                EditOperation editOperation,
                                                WebMapping.Core.Feature feature = null,
                                                bool useMobileBehavoir = true,
                                                string onUpdateComboCallbackToolId = null,
                                                bool editThemeNameAsCategory = false,
                                                string[] ignoreFields = null)
        {
            var target = UIElementTarget.tool_modaldialog;
            string targetTitle = null, targetWidth = null;
            var localizer = bridge.GetLocalizer<Edit>();

            switch (editOperation)
            {
                case EditOperation.Cut:
                case EditOperation.Clip:
                    target = UIElementTarget.@default;
                    break;
                case EditOperation.UpdateAttribures:
                    target = UIElementTarget.modaldialog;
                    targetTitle = localizer.Localize("mask.edit-attributes");
                    targetWidth = "640px";
                    break;
            }
            IUIElement maskDiv = new UIDiv()
            {
                id = EditMaskContainerId,
                target = (feature != null ? target : UIElementTarget.tool_modaldialog_hidden).ToString(),
                targettitle = targetTitle,
                targetwidth = targetWidth,
                css = UICss.ToClass(new string[] { UICss.ValidationContainer, UICss.FormContainer })

                //,style = "margin:0px 10px 0px 10px"
            };

            IUIElement parentElement = maskDiv;
            List<IUISetter> setters = new List<IUISetter>();

            if (!editThemeNameAsCategory)
            {
                parentElement.AddChild(new UITitle() { label = editThemeDef.EditThemeName });
            }

            var fieldPrefix = this._editEnvironment?._fieldPrefix ?? "editfield";
            var readonlyOperators = new EditOperation[] { EditOperation.Delete, EditOperation.Merge, EditOperation.Explode, EditOperation.Cut, EditOperation.Clip };

            ignoreFields = ignoreFields ?? Array.Empty<string>();

            try
            {
                if (_node == null)
                {
                    new Exception("No edit theme node found in XML definition");
                }

                XmlNode maskNode = _node.SelectSingleNode("edit:mask", _ns);

                if (maskNode == null)
                {
                    throw new Exception("No edit theme maske in XML definition");
                }

                parentElement.AddChild(new UIHidden()
                {
                    id = _editEnvironment.EditThemeElementId,
                    css = UICss.ToClass(new string[] { UICss.ToolParameter })
                });
                setters.Add(new UISetter($"_{fieldPrefix}_themeid", _node.Attributes["id"].Value));

                parentElement.AddChild(new UIHidden()
                {
                    id = $"_{fieldPrefix}_edittheme_def",
                    css = UICss.ToClass(new string[] { UICss.ToolParameter })
                });
                setters.Add(new UISetter(_editEnvironment.EditThemeDefintionElementId, ApiToolEventArguments.ToArgument(editThemeDef)));

                if (feature != null)
                {
                    parentElement.AddChild(new UIHidden()
                    {
                        id = _editEnvironment.FeatureOidElementId,
                        css = UICss.ToClass(new string[] { UICss.ToolParameter })
                    });
                    setters.Add(new UISetter(_editEnvironment.FeatureOidElementId, feature.Oid.ToString()));
                    parentElement.AddChild(new UIHidden()
                    {
                        id = $"_{fieldPrefix}_globaloid",
                        css = UICss.ToClass(new string[] { UICss.ToolParameter })
                    });
                    setters.Add(new UISetter($"_{fieldPrefix}_globaloid", feature.GlobalOid));
                }

                // Categories
                Dictionary<string, UICollapsableElement> categoryElements = new Dictionary<string, UICollapsableElement>();

                if (editThemeNameAsCategory == true)
                {
                    var categoryElement = new UICollapsableElement("div")
                    {
                        title = this.Name,
                        CollapseState = UICollapsableElement.CollapseStatus.Expanded
                    };
                    categoryElements.Add("-", categoryElement);
                    parentElement.AddChild(categoryElement);
                    parentElement = categoryElement;
                }
                else
                {
                    XmlNode categoriesNode = _node.SelectSingleNode("edit:categories", _ns);
                    if (categoriesNode != null)
                    {
                        foreach (XmlNode categoryNode in categoriesNode.SelectNodes("edit:category[@name and @id]", _ns))
                        {
                            var categoryElement = new UICollapsableElement("div")
                            {
                                title = categoryNode.Attributes["name"].Value,
                                CollapseState = categoryNode.Attributes["is_default"] != null && categoryNode.Attributes["is_default"].Value.ToLower() == "true" ?
                                    UICollapsableElement.CollapseStatus.Expanded :
                                    UICollapsableElement.CollapseStatus.Collapsed
                            };
                            categoryElements.Add(categoryNode.Attributes["id"].Value, categoryElement);
                            parentElement.AddChild(categoryElement);
                        }
                    }
                }

                if ((editOperation == EditOperation.Insert || editOperation == EditOperation.Update) && useMobileBehavoir == true)
                {
                    parentElement.AddChild(new UILabel() { label = $"{localizer.Localize("mask.label-geometry")}:" });
                    parentElement.AddChild(new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.hidetoolmodaldialog)
                    {
                        text = $"{localizer.Localize("mask.button-geometry")} »",
                        css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.OptionButtonStyle, UICss.ModalCloseElement }),
                        style = "max-width:300px"
                    });
                }

                // Nur mehr Simple möglich!!
                foreach (XmlNode fieldNode in maskNode.SelectNodes("edit:attribute", _ns))
                {
                    if (editOperation.IsMassOrTransfer() &&
                        fieldNode.Attributes["massattributable"]?.Value.ToLower() != "true")
                    {
                        continue;
                    }

                    string fieldValue = null;

                    if (editThemeNameAsCategory)
                    {
                        parentElement = categoryElements["-"];
                    }
                    else
                    {
                        if (fieldNode.Attributes["category"] != null && categoryElements.ContainsKey(fieldNode.Attributes["category"].Value))
                        {
                            parentElement = categoryElements[fieldNode.Attributes["category"].Value];
                        }
                        else
                        {
                            parentElement = maskDiv;
                        }
                    }

                    IUIElement inputElement = null;

                    if (fieldNode.Attributes["field"] != null)
                    {
                        string field = fieldNode.Attributes["field"].Value;

                        if (field.Contains("."))
                        {
                            field = field.Substring(field.LastIndexOf(".") + 1, field.Length - field.LastIndexOf(".") - 1);
                        }
                        if (ignoreFields.Where(f => field.Equals(f, StringComparison.OrdinalIgnoreCase)).Count() > 0)
                        {
                            continue;
                        }

                        string id = $"{fieldPrefix}_{field}";

                        fieldValue = feature != null ? feature[fieldNode.Attributes["field"].Value] : null;
                        bool @readonly = false;

                        string parameterType = fieldNode.Attributes["resistant"]?.Value.ToLower() == "true" ? UICss.ToolParameterPersistent : UICss.ToolParameter;
                        parameterType += fieldNode.IsRequiredField() && !editOperation.IsMassOrTransfer() ? " " + UICss.ToolParameterRequired
                                                                     : String.Empty;

                        if (fieldNode.Attributes["readonly"]?.Value?.ToLower() == "true" ||
                            fieldNode.Attributes["locked"]?.Value?.ToLower() == "true")
                        {
                            @readonly = true;
                        }

                        if (fieldNode.Attributes["visible"]?.Value?.ToLower() == "false")
                        {
                            #region Hidden

                            parentElement.AddChild(new UIHidden()
                            {
                                id = id,
                                css = UICss.ToClass(new string[] { parameterType })
                            });

                            #endregion
                        }
                        else if (fieldNode.Attributes["type"]?.Value?.ToLower() == "domain")
                        {
                            #region Combobox

                            var fieldContainer = CreateEditFieldContainer(fieldNode, editOperation, fieldPrefix);
                            parentElement.AddChild(fieldContainer);
                            parentElement = fieldContainer;
                            string[] whereKeyParameters = null;

                            parentElement.AddChild(FieldPrompt(id, fieldNode, editOperation));

                            if (@readonly == true || readonlyOperators.Contains(editOperation))
                            {
                                parentElement.AddChild(new UIInputText()
                                {
                                    @readonly = true,
                                    id = id,
                                    css = UICss.ToClass(new string[] { parameterType, UICss.InputSetBorderOnChange })
                                });
                            }
                            else
                            {
                                List<UISelect.Option> options = new List<UISelect.Option>();

                                if (fieldNode.Attributes["db_connectionstring"] != null)
                                {
                                    var connectionString = fieldNode.Attributes["db_connectionstring"].Value;

                                    if (connectionString.IsValidHttpUrl())
                                    {
                                        #region Web Serice Datalinq Query

                                        string requestParameters = fieldNode.Attributes["db_where"]?.Value;
                                        whereKeyParameters = Globals.KeyParameters(requestParameters ?? String.Empty, startingBracket: "{{", endingBracket: "}}");

                                        #region Wenn Parameter auf eigenes Feld verweist, sofort ausfüllen/ersetellen
                                        //
                                        // Eine Auswahlliste kann nicht von sich selbst abhängig sein...
                                        //
                                        if (whereKeyParameters != null && whereKeyParameters.Where(p => p.Equals(field, StringComparison.InvariantCultureIgnoreCase)).Count() > 0)
                                        {
                                            var val = feature != null ? feature[field] : await bridge.GetFieldDefaultValue(editThemeDef.ServiceId, editThemeDef.LayerId, field);

                                            foreach (var placeholder in whereKeyParameters.Where(p => p.Equals(field, StringComparison.InvariantCultureIgnoreCase)))
                                            {
                                                requestParameters = requestParameters.Replace("{{" + placeholder + "}}", System.Web.HttpUtility.UrlEncode(val ?? String.Empty));
                                            }

                                            whereKeyParameters = whereKeyParameters.Where(p => !p.Equals(field, StringComparison.InvariantCultureIgnoreCase))
                                                                                   .ToArray();
                                        }
                                        #endregion

                                        if (whereKeyParameters == null || whereKeyParameters.Length == 0)
                                        {
                                            var url = connectionString;
                                            if (!String.IsNullOrEmpty(requestParameters))
                                            {
                                                url += $"{(url.Contains("?") ? "&" : "?")}{requestParameters}";
                                            }
                                            var jsonResult = await bridge.HttpService.GetStringAsync(url);

                                            try
                                            {
                                                var domains = JSerializer.Deserialize<object[]>(jsonResult);
                                                string valueProperty = fieldNode.Attributes["db_valuefield"]?.Value.OrTake("value");
                                                string labelProperty = fieldNode.Attributes["db_aliasfield"]?.Value.OrTake("name");

                                                foreach (var domain in domains)
                                                {
                                                    if (JSerializer.IsJsonElement(domain))
                                                    {
                                                        options.Add(new UISelect.Option()
                                                        {
                                                            value = JSerializer.GetJsonElementValue(domain, valueProperty).ToStringOrEmpty(),
                                                            label = JSerializer.GetJsonElementValue(domain, labelProperty).ToStringOrEmpty()
                                                        });
                                                    }
                                                }
                                            }
                                            catch (Exception)
                                            {
                                                var exceptionResponse = JSerializer.Deserialize<ExceptionModel>(jsonResult);
                                                if (!String.IsNullOrEmpty(exceptionResponse?.Exception))
                                                {
                                                    throw new Exception(exceptionResponse.Exception);
                                                }
                                                else
                                                {
                                                    throw;
                                                }
                                            }
                                        }

                                        #endregion
                                    }
                                    else
                                    {
                                        #region Database

                                        using (DBConnection dbConn = new DBConnection())
                                        {

                                            dbConn.OleDbConnectionMDB = connectionString;

                                            var uniqueList = new UniqueList();
                                            uniqueList.Add(fieldNode.Attributes["db_valuefield"].Value);
                                            uniqueList.Add(fieldNode.Attributes["db_aliasfield"].Value);

                                            string domainSql = $"select {String.Join(",", uniqueList.ToArray())} from {fieldNode.Attributes["db_table"].Value}";

                                            if (!String.IsNullOrWhiteSpace(fieldNode.Attributes["db_where"]?.Value))
                                            {
                                                domainSql += $" where {fieldNode.Attributes["db_where"].Value}";
                                            }

                                            if (!String.IsNullOrWhiteSpace(fieldNode.Attributes["db_orderby"]?.Value))
                                            {
                                                domainSql += " order by " + fieldNode.Attributes["db_orderby"].Value;
                                            }

                                            domainSql = bridge.ReplaceUserAndSessionDependentFilterKeys(domainSql, startingBracket: "{{", endingBracket: "}}");
                                            whereKeyParameters = Globals.KeyParameters(domainSql, startingBracket: "{{", endingBracket: "}}");

                                            #region Wenn Parameter auf eigenes Feld verweist, sofort ausfüllen/ersetellen
                                            //
                                            // Eine Auswahlliste kann nicht von sich selbst abhängig sein...
                                            //
                                            if (whereKeyParameters != null && whereKeyParameters.Where(p => p.Equals(field, StringComparison.InvariantCultureIgnoreCase)).Count() > 0)
                                            {
                                                var val = feature != null ? feature[field] : await bridge.GetFieldDefaultValue(editThemeDef.ServiceId, editThemeDef.LayerId, field);

                                                foreach (var placeholder in whereKeyParameters.Where(p => p.Equals(field, StringComparison.InvariantCultureIgnoreCase)))
                                                {
                                                    domainSql = domainSql.Replace("{{" + placeholder + "}}", SqlInjection.ParsePro(val));
                                                }

                                                whereKeyParameters = whereKeyParameters.Where(p => !p.Equals(field, StringComparison.InvariantCultureIgnoreCase))
                                                                                       .ToArray();
                                            }
                                            #endregion

                                            if (whereKeyParameters == null || whereKeyParameters.Length == 0)
                                            {
                                                var tab = dbConn.Select(domainSql);
                                                if (tab == null)
                                                {
                                                    throw new DatabaseException($"Error: {dbConn.errorMessage}");
                                                }

                                                foreach (System.Data.DataRow row in tab.Rows)
                                                {
                                                    string v = CMS.Globals.FormEnc(row[fieldNode.Attributes["db_valuefield"].Value].ToString());
                                                    if (tab.Columns[fieldNode.Attributes["db_valuefield"].Value].DataType == typeof(bool))
                                                    {
                                                        v = v.ToLower() == "true" ? "1" : "0";
                                                    }
                                                    else
                                                    {
                                                        // wurde von webgis4 übernommen, da String direkt in JavaScript übergeben wurde. Jetzt als JSON übergeben.
                                                        // Beispiel: Auswahlliste mit Personen+Domain: "domain\username"
                                                        //if (v.Contains(@"\") && !v.Contains(@"\\"))
                                                        //    v = v.Replace(@"\", @"\\");
                                                    }
                                                    string label = CMS.Globals.FormEnc(row[fieldNode.Attributes["db_aliasfield"].Value].ToString());

                                                    if (label.Contains(@"\") && !label.Contains(@"\\"))
                                                    {
                                                        label = label.Replace(@"\", @"\\");
                                                    }

                                                    options.Add(new UISelect.Option()
                                                    {
                                                        value = v,
                                                        label = label
                                                    });

                                                }
                                            }
                                        }

                                        #endregion
                                    }
                                }
                                else if (fieldNode.Attributes["domain_list"] != null && !String.IsNullOrWhiteSpace(fieldNode.Attributes["domain_list"].Value))
                                {
                                    foreach (var domainItem in fieldNode.Attributes["domain_list"].Value.Split(','))
                                    {
                                        string domainValue = domainItem, domainLabel = domainItem;
                                        if (domainItem.Contains(":"))
                                        {
                                            domainValue = domainValue.Split(':')[0];
                                            domainLabel = domainItem.Split(':')[1];
                                        }

                                        options.Add(new UISelect.Option()
                                        {
                                            value = domainValue,
                                            label = domainLabel
                                        });
                                    }
                                }
                                else
                                {
                                    var domains = await bridge.GetLayerFieldDomains(editThemeDef.ServiceId, editThemeDef.LayerId, field);
                                    foreach (var domain in domains)
                                    {
                                        options.Add(new UISelect.Option()
                                        {
                                            value = domain.Key,
                                            label = domain.Value
                                        });
                                    }
                                }

                                // 
                                // fieldValue und options value müssen nicht gleich sein, weil der Value auch das Label sein kann (beim SDE Domain der Fall)
                                // Damit am Client das richtige Selektiert, hier den richtigen Value bestimmen...
                                //
                                if (fieldValue != null)
                                {
                                    if (options.Where(o => o.value == fieldValue).Count() == 0)
                                    {
                                        var option = options.Where(o => o.label == fieldValue).FirstOrDefault();
                                        if (option != null)
                                        {
                                            fieldValue = option.value;
                                        }
                                    }
                                }

                                parentElement.AddChild(inputElement = new UISelect()
                                {
                                    id = id,
                                    options = options.ToArray(),
                                    css = UICss.ToClass(new string[] { parameterType, UICss.InputSetBorderOnChange }),
                                    dependency_field_ids = whereKeyParameters?.Select(p => $"{fieldPrefix}_{p}").ToArray(),
                                    dependency_field_ids_callback_toolid = whereKeyParameters != null && whereKeyParameters.Count() > 0 ? onUpdateComboCallbackToolId : null
                                });
                            }
                            parentElement.AddChild(new UIBreak());

                            #endregion
                        }
                        else if (fieldNode.Attributes["type"]?.Value?.ToLower() == "autocomplete")
                        {
                            #region Autocomplete

                            var fieldContainer = CreateEditFieldContainer(fieldNode, editOperation, fieldPrefix);
                            parentElement.AddChild(fieldContainer);
                            parentElement = fieldContainer;

                            parentElement.AddChild(FieldPrompt(id, fieldNode, editOperation));

                            if (@readonly == true || readonlyOperators.Contains(editOperation))
                            {
                                parentElement.AddChild(inputElement = new UIInputText()
                                {
                                    @readonly = true,
                                    id = id,
                                    css = UICss.ToClass(new string[] { parameterType, UICss.InputSetBorderOnChange })
                                });
                            }
                            else
                            {
                                parentElement.AddChild(inputElement = new UIInputAutocomplete(
                                    UIInputAutocomplete.MethodSource(bridge, typeof(Edit), "autocomplete",
                                    new
                                    {
                                        themeid = _node.Attributes["id"].Value,
                                        field = fieldNode.Attributes["field"].Value,
                                        _editfield_edittheme_def = ApiToolEventArguments.ToArgument(this._editEnvironment?.EditThemeDefinition)
                                    }, true))
                                {
                                    id = id,
                                    css = UICss.ToClass(new string[] { parameterType, UICss.InputSetBorderOnChange })
                                });
                            }
                            parentElement.AddChild(new UIBreak());

                            #endregion
                        }
                        else if (fieldNode.Attributes["type"]?.Value?.ToLower() == "date")
                        {
                            #region Datepicker

                            var fieldContainer = CreateEditFieldContainer(fieldNode, editOperation, fieldPrefix);
                            parentElement.AddChild(fieldContainer);
                            parentElement = fieldContainer;

                            parentElement.AddChild(FieldPrompt(id, fieldNode, editOperation));

                            if (@readonly == true || readonlyOperators.Contains(editOperation))
                            {
                                parentElement.AddChild(new UIInputText()
                                {
                                    @readonly = true,
                                    id = id,
                                    css = UICss.ToClass(new string[] { parameterType, UICss.InputSetBorderOnChange })
                                });
                            }
                            else
                            {
                                parentElement.AddChild(inputElement = new UIDatePicker()
                                {
                                    id = id,
                                    css = UICss.ToClass(new string[] { parameterType, UICss.InputSetBorderOnChange }),
                                    date_only = fieldNode.Attributes["date_only"]?.Value.ToLower() == "true"
                                });
                            }
                            parentElement.AddChild(new UIBreak());

                            #endregion
                        }
                        else if (fieldNode.Attributes["type"]?.Value?.ToLower() == "drop-list")
                        {
                            #region drop-list

                            var fieldContainer = CreateEditFieldContainer(fieldNode, editOperation, fieldPrefix);
                            parentElement.AddChild(fieldContainer);
                            parentElement = fieldContainer;

                            parentElement.AddChild(FieldPrompt(id, fieldNode, editOperation));
                            if (@readonly == true || readonlyOperators.Contains(editOperation))
                            {
                                parentElement.AddChild(inputElement = new UIInputText()
                                {
                                    @readonly = true,
                                    id = id,
                                    css = UICss.ToClass(new string[] { parameterType, UICss.InputSetBorderOnChange })
                                });
                            }
                            else
                            {
                                parentElement.AddChild(new UIDropList()
                                {
                                    id = id
                                });
                            }

                            #endregion
                        }
                        else if (fieldNode.Attributes["type"]?.Value?.ToLower() == "file")
                        {
                            var fieldContainer = CreateEditFieldContainer(fieldNode, editOperation, fieldPrefix);
                            parentElement.AddChild(fieldContainer);
                            parentElement = fieldContainer;

                            parentElement.AddChild(FieldPrompt(id, fieldNode, editOperation));

                            if (@readonly == true || readonlyOperators.Contains(editOperation))
                            {
                                parentElement.AddChild(inputElement = new UIInputText()
                                {
                                    @readonly = true,
                                    id = id,
                                    css = UICss.ToClass(new string[] { parameterType, UICss.InputSetBorderOnChange })
                                });
                            }
                            else
                            {
                                parentElement.AddChild(inputElement = new UIUploadFileEdit(editThemeDef.ServiceId, editThemeDef.EditThemeId, field)
                                {
                                    id = id,
                                    css = UICss.ToClass(new string[] { parameterType, UICss.InputSetBorderOnChange })
                                });
                            }
                            parentElement.AddChild(new UIBreak());
                        }
                        else if (fieldNode.Attributes["type"]?.Value?.ToLower() == "textarea")
                        {
                            #region textarea

                            var fieldContainer = CreateEditFieldContainer(fieldNode, editOperation, fieldPrefix);
                            parentElement.AddChild(fieldContainer);
                            parentElement = fieldContainer;

                            parentElement.AddChild(FieldPrompt(id, fieldNode, editOperation));
                            parentElement.AddChild(inputElement = new UIInputTextArea()
                            {
                                @readonly = @readonly == true || readonlyOperators.Contains(editOperation),
                                id = id,
                                css = UICss.ToClass(new string[] { parameterType, UICss.InputSetBorderOnChange })
                            });
                            parentElement.AddChild(new UIBreak());

                            #endregion
                        }
                        else
                        {
                            #region Text

                            var fieldContainer = CreateEditFieldContainer(fieldNode, editOperation, fieldPrefix);
                            parentElement.AddChild(fieldContainer);
                            parentElement = fieldContainer;

                            parentElement.AddChild(FieldPrompt(id, fieldNode, editOperation));
                            parentElement.AddChild(inputElement = new UIInputText()
                            {
                                @readonly = @readonly == true || readonlyOperators.Contains(editOperation),
                                id = id,
                                css = UICss.ToClass(new string[] { parameterType, UICss.InputSetBorderOnChange })
                            });
                            parentElement.AddChild(new UIBreak());

                            #endregion
                        }

                        if (feature != null)
                        {
                            fieldValue = fieldValue ?? feature[fieldNode.Attributes["field"].Value];
                            setters.Add(new UISetter(id, fieldValue));
                        }
                        else if (editOperation == EditOperation.Insert)
                        {
                            {
                                var autoValue = fieldNode.Attributes["autovalue"]?.Value;
                                if (autoValue != null && autoValue.StartsWith("mask-insert-default::"))
                                {
                                    setters.Add(new UISetter(id, autoValue.Substring("mask-insert-default::".Length)));
                                }
                            }
                        }
                    }
                    else
                    {
                        if (fieldNode.Attributes["type"]?.Value?.ToLower() == "info" &&
                            fieldNode.Attributes["label"] != null)
                        {
                            #region Freier Text

                            parentElement.AddChild(new UILabel()
                            {
                                label = fieldNode.Attributes["label"].Value
                            });
                            parentElement.AddChild(new UIBreak());

                            #endregion
                        }
                    }

                    if (inputElement is UIValidation &&
                        fieldNode.Attributes["clientside_validation"]?.Value.ToLower() == "true" &&
                        editOperation.IsMassOrTransfer() == false)  // ClientValidierung nicht bei Massenattributierung
                    {
                        var uiValidation = (UIValidation)inputElement;
                        if (fieldNode.Attributes["required"]?.Value.ToLower() == "true")
                        {
                            uiValidation.IsRequired = true;
                        }
                        if (fieldNode.Attributes["minlen"]?.Value != null)
                        {
                            uiValidation.MinLength = int.Parse(fieldNode.Attributes["minlen"].Value);
                        }
                        if (!String.IsNullOrEmpty(fieldNode.Attributes["regex"]?.Value))
                        {
                            uiValidation.Regex = fieldNode.Attributes["regex"].Value;
                        }
                        if (!String.IsNullOrEmpty(fieldNode.Attributes["regex_message"]?.Value) || !String.IsNullOrEmpty(fieldNode.Attributes["validation_error"]?.Value))
                        {
                            uiValidation.ValidationErrorMessage = fieldNode.Attributes["regex_message"]?.Value ?? fieldNode.Attributes["validation_error"]?.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                parentElement.AddChild(new UILabel()
                {
                    label = ex.SecureMessage()
                });
            }

            parentElement = maskDiv;
            parentElement.AddChild(new UIBreak());

            var stickyButtonContainer = new UIStickyBottomButtonContainer();
            parentElement.AddChild(stickyButtonContainer);
            var buttonGroup = new UIButtonGroup();
            stickyButtonContainer.AddChild(buttonGroup);

            #region Remove empty categories

            if (editOperation == EditOperation.MassAttributation)
            {
                foreach (var categoryElement in parentElement.elements.Where(e => e is UICollapsableElement && (e.elements == null || e.elements.Count() == 0))
                                                                      .ToArray())  //  ToArray => creates new IEnumerable to avoid changing an existing Collection!!
                {
                    parentElement.RemoveChild(categoryElement);
                }
            }

            #endregion

            //if ((editOperation == EditOperation.Insert || editOperation == EditOperation.Udate) && useMobileBehavoir == true)
            //{
            //    parentElement.Add(new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.hidetoolmodaldialog)
            //    {
            //        text = "Lage/Geographie bearbeiten »",
            //        css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.OptionButtonStyle, UICss.ModalCloseElement })
            //    });
            //}
            if (editOperation == EditOperation.MassAttributation)
            {
                buttonGroup.AddChild(new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setparenttool)
                {
                    text = localizer.Localize("cancel"),
                    css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.OptionButtonStyle })
                });
                buttonGroup.AddChild(new UIButton(UIButton.UIButtonType.servertoolcommand, "save")
                {
                    text = localizer.Localize("mask.apply-attributes"),
                    css = UICss.ToClass(new string[] { UICss.DefaultButtonStyle, UICss.OptionButtonStyle, UICss.ValidateInputButton })
                });

                stickyButtonContainer.AddChild(new UIDiv()
                {
                    css = UICss.ToClass(new string[] { "webgis-info" }),
                    elements = new IUIElement[]
                            {
                                new UILiteral() { literal = localizer.Localize("mask.warning-massattribution1") }
                            }
                });
            }

            if (feature == null)
            {
                if (editOperation == EditOperation.Insert)
                {
                    var insertActionHelper = new EditInsertActionHelper(_node, _ns);
                    if (WorkspaceImplementsUndo)
                    {
                        if (insertActionHelper.HasDefaultSaveAndSelectButton)
                        {
                            stickyButtonContainer.InsertChildBefore(buttonGroup, new UIButton(UIButton.UIButtonType.servertoolcommand, "saveandselect")
                            {
                                text = localizer.Localize("mask.save-and-select"),
                                css = UICss.ToClass(new string[] { UICss.OptionButtonStyle })
                            });
                        }

                        foreach (var customInserActions in insertActionHelper.GetCustomInsertActions())
                        {
                            stickyButtonContainer.InsertChildBefore(buttonGroup, new UIButton(UIButton.UIButtonType.servertoolcommand, insertActionHelper.ServerCommand(customInserActions.action))
                            {
                                text = customInserActions.text,
                                css = UICss.ToClass(new string[] { UICss.OptionButtonStyle })
                            });
                        }
                    }

                    buttonGroup.AddChild(new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setparenttool)
                    {
                        text = useMobileBehavoir ? localizer.Localize("cancel") : localizer.Localize("mask.stop-editing"),
                        css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.ButtonIcon })
                    });

                    if (insertActionHelper.HasDefaultSaveButton)
                    {
                        buttonGroup.AddChild(new UIButton(UIButton.UIButtonType.servertoolcommand, "save")
                        {
                            text = localizer.Localize("save"),
                            css = UICss.ToClass(new string[] { UICss.OkButtonStyle, UICss.ButtonIcon, UICss.ValidateInputButton }),
                            ctrl_shortcut = "s"
                        });
                    }

                    #region Custom Autovalues gleich befüllen



                    #endregion
                }
            }
            else
            {
                if (editOperation == EditOperation.Update)
                {
                    buttonGroup.AddChild(new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setparenttool)
                    {
                        //text = "Bearbeitung abbrechen",
                        //css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.OptionButtonStyle })
                        text = localizer.Localize("cancel"),
                        css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.ButtonIcon })
                    });
                    buttonGroup.AddChild(new UIButton(UIButton.UIButtonType.servertoolcommand, "save")
                    {
                        //text = "Änderungen Speichern",
                        //css = UICss.ToClass(new string[] { UICss.DefaultButtonStyle, UICss.OptionButtonStyle })
                        text = localizer.Localize("save"),
                        css = UICss.ToClass(new string[] { UICss.OkButtonStyle, UICss.ButtonIcon, UICss.ValidateInputButton }),
                        ctrl_shortcut = "s"
                    });
                }
                else if (editOperation == EditOperation.UpdateAttribures)
                {
                    buttonGroup.AddChild(new UIButton(UIButton.UIButtonType.servertoolcommand_ext, "editattributes-save")
                    {
                        //text = "Änderungen Speichern",
                        //css = UICss.ToClass(new string[] { UICss.DefaultButtonStyle, UICss.OptionButtonStyle })
                        id = "webgis.tools.editing.updatefeature",  // Callback tool für servertoolcommand_ext
                        text = localizer.Localize("save"),
                        css = UICss.ToClass(new string[] { UICss.OkButtonStyle, UICss.ButtonIcon, UICss.ValidateInputButton }),
                        ctrl_shortcut = "s"
                    });
                }
                else if (editOperation == EditOperation.Delete)
                {
                    buttonGroup.AddChild(new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setparenttool)
                    {
                        text = localizer.Localize("cancel"),
                        css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.ButtonIcon })
                    });
                    buttonGroup.AddChild(new UIButton(UIButton.UIButtonType.servertoolcommand, "delete")
                    {
                        text = localizer.Localize("delete"),
                        css = UICss.ToClass(new string[] { UICss.DangerButtonStyle, UICss.ButtonIcon })
                    });
                }
                else if (editOperation == EditOperation.Explode)
                {
                    buttonGroup.AddChild(new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setparenttool)
                    {
                        text = localizer.Localize("cancel"),
                        css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.ButtonIcon })
                    });
                    buttonGroup.AddChild(new UIButton(UIButton.UIButtonType.servertoolcommand, "explode")
                    {
                        text = localizer.Localize("mask.explode"),
                        css = UICss.ToClass(new string[] { UICss.OkButtonStyle, UICss.ButtonIcon })
                    });
                }
                else if (editOperation == EditOperation.Merge)
                {
                    buttonGroup.AddChild(new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setparenttool)
                    {
                        text = localizer.Localize("cancel"),
                        css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.ButtonIcon })
                    });
                    buttonGroup.AddChild(new UIButton(UIButton.UIButtonType.servertoolcommand, "merge")
                    {
                        text = localizer.Localize("mask.merge"),
                        css = UICss.ToClass(new string[] { UICss.OkButtonStyle, UICss.ButtonIcon })
                    });
                }
                else if (new EditOperation[] { EditOperation.Cut, EditOperation.Clip }.Contains(editOperation))
                {
                    parentElement.InsertChildBefore(buttonGroup, new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.removesketch)
                    {
                        text = localizer.Localize("remove-sketch"),
                        css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.OptionButtonStyle })
                    });

                    buttonGroup.AddChild(new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setparenttool)
                    {
                        text = localizer.Localize("cancel"),
                        css = UICss.ToClass(new string[] { UICss.CancelButtonStyle, UICss.ButtonIcon })
                    });
                    buttonGroup.AddChild(new UIButton(UIButton.UIButtonType.servertoolcommand, editOperation == EditOperation.Cut ? "cut" : "clip")
                    {
                        text = editOperation == EditOperation.Cut
                            ? localizer.Localize("mask.cut")
                            : localizer.Localize("mask.clip"),
                        css = UICss.ToClass(new string[] { UICss.OkButtonStyle, UICss.ButtonIcon })
                    });
                }

                if (new EditOperation[] { EditOperation.Cut, EditOperation.Merge, EditOperation.Explode, EditOperation.Clip }.Contains(editOperation))
                {
                    buttonGroup.AddChild(new UIHidden()
                    {
                        id = EditNewFeatuerCounterId,
                        value = "0",
                        css = UICss.ToClass(new string[] { UICss.ToolParameter })
                    });
                    buttonGroup.AddChild(new UIHidden()
                    {
                        id = EditNewFeaturePreviewDataId,
                        value = "",
                        css = UICss.ToClass(new string[] { UICss.ToolParameter })
                    });
                    buttonGroup.AddChild(new UIHidden()
                    {
                        id = EditOriginalFeaturePreviewDataId,
                        value = "",
                        css = UICss.ToClass(new string[] { UICss.ToolParameter })
                    });
                    buttonGroup.AddChild(new UIHidden()
                    {
                        id = EditFeatureIdsSubsetId,
                        value = "",
                        css = UICss.ToClass(new string[] { UICss.ToolParameter })
                    });
                }
            }

            if (editOperation == EditOperation.Insert)
            {
                AppendLayerWarnings(maskDiv, editThemeDef, localizer);
            }

            return new UIEditMask()
            {
                UIElements = new IUIElement[]{
                    maskDiv
                },
                UISetters = setters
            };
        }

        public List<XmlNode> MaskAttributes
        {
            get
            {
                List<XmlNode> attributes = new List<XmlNode>();
                if (_node == null)
                {
                    return attributes;
                }

                XmlNode maskNode = _node.SelectSingleNode("edit:mask", _ns);
                if (maskNode == null)
                {
                    return attributes;
                }

                foreach (XmlNode attributeNode in maskNode.SelectNodes("edit:attribute[@field]|*//edit:attribute[@field]", _ns))
                {
                    attributes.Add(attributeNode);
                }
                return attributes;
            }
        }

        public string GetDefaultValue(IBridge bridge, string defaultValue)
        {
            if (String.IsNullOrWhiteSpace(defaultValue))
            {
                return null;
            }

            defaultValue = defaultValue.Replace("{username}", bridge.CurrentUser?.Username ?? String.Empty);
            defaultValue = defaultValue.Replace("{currentdate}", DateTime.Now.ToShortDateString());
            defaultValue = defaultValue.Replace("{currenttime}", DateTime.Now.ToShortTimeString());

            return defaultValue;
        }

        async public Task<ClassCapability> GetFeatureInfo(IBridge bridge, string serviceId)
        {
            if (_node == null)
            {
                new Exception("No edit theme node found in XML definition");
            }

            #region Mask

            XmlNode maskNode = _node.SelectSingleNode("edit:mask", _ns);
            if (maskNode == null)
            {
                throw new Exception("No edit theme maske in XML definition");
            }

            var editThemeBridge = bridge.GetEditTheme(serviceId, _node.Attributes["id"].Value);
            if (editThemeBridge == null)
            {
                throw new Exception("No edit theme bridge object found");
            }

            var service = await bridge.GetService(serviceId);
            if (service == null)
            {
                throw new Exception("No edit service bridge object found");
            }

            var layer = service.FindLayer(editThemeBridge.LayerId);
            if (layer == null)
            {
                throw new Exception("No edit layer bridge object found");
            }

            List<ClassCapability.FeatureInfoField> infoFields = new List<ClassCapability.FeatureInfoField>();
            //foreach (XmlNode fieldNode in maskNode.SelectNodes("edit:attribute[@type]", _ns))
            foreach (XmlNode fieldNode in maskNode.SelectNodes("edit:attribute", _ns))
            {
                string type = fieldNode.Attributes["type"] != null ? fieldNode.Attributes["type"].Value.ToLower() : String.Empty;

                if (type == "info")
                {
                    infoFields.Add(new ClassCapability.FeatureInfoField()
                    {
                        Type = type,
                        Visible = fieldNode.Attributes["visible"] != null ? fieldNode.Attributes["visible"].Value.ToLower() == "true" : true,
                        Category = fieldNode.Attributes["category"]?.Value,
                        Label = fieldNode.Attributes["label"]?.Value
                    });
                    continue;
                }
                if (fieldNode.Attributes["field"] == null)
                {
                    continue;
                }

                string field = fieldNode.Attributes["field"].Value;
                if (field.Contains("."))
                {
                    field = field.Substring(field.LastIndexOf(".") + 1, field.Length - field.LastIndexOf(".") - 1);
                }


                var infoField = new ClassCapability.FeatureInfoField()
                {
                    Name = field,
                    Aliasname = fieldNode.Attributes["prompt"] != null ? fieldNode.Attributes["prompt"].Value : field,
                    ReadOnly = fieldNode.Attributes["readonly"] != null ? fieldNode.Attributes["readonly"].Value.ToLower() == "true" : false,
                    Visible = fieldNode.Attributes["visible"] != null ? fieldNode.Attributes["visible"].Value.ToLower() == "true" : true,
                    Type = String.IsNullOrWhiteSpace(type) ? null : type,
                    Required = fieldNode.Attributes["required"]?.Value.ToString().ToLower() == "true" || (
                        fieldNode.Attributes["minlen"]?.Value != null && fieldNode.Attributes["minlen"]?.Value != "0"),
                    DefaultValue = GetDefaultValue(bridge, String.IsNullOrWhiteSpace(fieldNode.Attributes["defaultValue"]?.Value) ? fieldNode.Attributes["defaultvalue"]?.Value :
                                                                                                                                    fieldNode.Attributes["defaultValue"]?.Value),
                    Category = fieldNode.Attributes["category"]?.Value,
                    Label = fieldNode.Attributes["label"]?.Value
                };
                if (fieldNode.Attributes["disabled"] != null && fieldNode.Attributes["disabled"].Value.ToLower() == "true")
                {
                    infoField.ReadOnly = true;
                }

                if (infoField.Visible == false && fieldNode.Attributes["autovalue"] != null && !String.IsNullOrWhiteSpace(fieldNode.Attributes["autovalue"].Value))
                {
                    continue;
                }

                if (type == "file" && fieldNode.Attributes["file_date_field"] != null)
                {
                    infoField.FileDateField = fieldNode.Attributes["file_date_field"].Value;
                    if (infoField.FileDateField.Contains("."))
                    {
                        infoField.FileDateField = infoField.FileDateField.Substring(infoField.FileDateField.LastIndexOf(".") + 1);
                    }
                }

                if (type == "domain" || type == "radio")
                {
                    var connectionString = fieldNode.Attributes["db_connectionstring"]?.Value ?? "";
                    var domains = new List<ClassCapability.FeatureInfoField.FeatureInfoFieldDomain>();

                    if (connectionString.IsValidHttpUrl())
                    {
                        string url = connectionString;
                        if (!String.IsNullOrWhiteSpace(fieldNode.Attributes["db_where"]?.Value))
                        {
                            url += $"{(url.Contains("?") ? "&" : "?")}{fieldNode.Attributes["db_where"]?.Value}";
                        }

                        var jsonResult = await bridge.HttpService.GetStringAsync(url);
                        var results = JSerializer.Deserialize<object[]>(jsonResult);
                        string valueProperty = fieldNode.Attributes["db_valuefield"]?.Value.OrTake("value");
                        string labelProperty = fieldNode.Attributes["db_aliasfield"]?.Value.OrTake("name");

                        foreach (var result in results)
                        {
                            if (JSerializer.IsJsonElement(result))
                            {
                                string value = JSerializer.GetJsonElementValue(result, valueProperty).ToStringOrEmpty();
                                string label = JSerializer.GetJsonElementValue(result, labelProperty).ToStringOrEmpty();

                                domains.Add(new ClassCapability.FeatureInfoField.FeatureInfoFieldDomain()
                                {
                                    Value = value.ReplaceSingleBackslashesToDoubleBackslashes(),
                                    Label = label.ReplaceSingleBackslashesToDoubleBackslashes()
                                });
                            }
                        }
                    }
                    else if (!String.IsNullOrEmpty(connectionString))  // database
                    {
                        using (DBConnection dbConn = new DBConnection())
                        {
                            dbConn.OleDbConnectionMDB = fieldNode.Attributes["db_connectionstring"].Value;

                            string domainSql = "select " + fieldNode.Attributes["db_valuefield"].Value + "," + fieldNode.Attributes["db_aliasfield"].Value + " from " + fieldNode.Attributes["db_table"].Value;

                            if (!String.IsNullOrWhiteSpace(fieldNode.Attributes["db_where"]?.Value))
                            {
                                domainSql += " where " + fieldNode.Attributes["db_where"].Value;
                            }

                            if (!String.IsNullOrWhiteSpace(fieldNode.Attributes["db_orderby"]?.Value))
                            {
                                domainSql += " order by " + fieldNode.Attributes["db_orderby"].Value;
                            }

                            domainSql = bridge.ReplaceUserAndSessionDependentFilterKeys(domainSql, startingBracket: "{{", endingBracket: "}}");

                            var tab = dbConn.Select(domainSql);
                            if (tab == null)
                            {
                                throw new Exception("Error: " + dbConn.errorMessage);
                            }

                            foreach (System.Data.DataRow row in tab.Rows)
                            {
                                string v = CMS.Globals.FormEnc(row[fieldNode.Attributes["db_valuefield"].Value].ToString());
                                if (tab.Columns[fieldNode.Attributes["db_valuefield"].Value].DataType == typeof(bool))
                                {
                                    v = v.ToLower() == "true" ? "1" : "0";
                                }
                                else
                                {
                                    v = v.ReplaceSingleBackslashesToDoubleBackslashes();
                                    //if (v.Contains(@"\") && !v.Contains(@"\\"))
                                    //{
                                    //    v = v.Replace(@"\", @"\\");
                                    //}
                                }
                                string label = CMS.Globals.FormEnc(row[fieldNode.Attributes["db_aliasfield"].Value].ToString())
                                                        .ReplaceSingleBackslashesToDoubleBackslashes();
                                //if (label.Contains(@"\") && !label.Contains(@"\\"))
                                //{
                                //    label = label.Replace(@"\", @"\\");
                                //}

                                domains.Add(new ClassCapability.FeatureInfoField.FeatureInfoFieldDomain()
                                {
                                    Value = v,
                                    Label = label
                                });
                            }
                        }

                    }
                    else if (!String.IsNullOrEmpty(fieldNode.Attributes["domain_list"]?.Value))
                    {
                        foreach (var domainItem in fieldNode.Attributes["domain_list"].Value.Split(','))
                        {
                            string domainValue = domainItem, domainLabel = domainItem;
                            if (domainItem.Contains(":"))
                            {
                                domainValue = domainValue.Split(':')[0];
                                domainLabel = domainItem.Split(':')[1];
                            }

                            domains.Add(new ClassCapability.FeatureInfoField.FeatureInfoFieldDomain()
                            {
                                Value = domainValue.ReplaceSingleBackslashesToDoubleBackslashes(),
                                Label = domainLabel.ReplaceSingleBackslashesToDoubleBackslashes()
                            });
                        }
                    }

                    infoField.DomainValues = domains.ToArray();
                }

                infoFields.Add(infoField);
            }

            #endregion

            #region Categories

            XmlNode categoriesNode = _node.SelectSingleNode("edit:categories", _ns);
            List<ClassCapability.FeatureInfoCategory> categories = new List<ClassCapability.FeatureInfoCategory>();
            if (categoriesNode != null)
            {
                foreach (XmlNode categoryNode in categoriesNode.SelectNodes("edit:category[@name and @id]", _ns))
                {
                    var category = new ClassCapability.FeatureInfoCategory()
                    {
                        Id = categoryNode.Attributes["id"]?.Value,
                        Name = categoryNode.Attributes["name"]?.Value,
                        IsDefault = categoryNode.Attributes["is_default"]?.Value.ToLower() == "true" ? true : null,
                        Collapsed = categoryNode.Attributes["collapsed"]?.Value.ToLower() == "true" ? true : null,
                        QuickSearchService = categoryNode.Attributes["quick_search_service"]?.Value,
                        Description = categoryNode.Attributes["description"]?.Value,
                        QuickSearchCategory = categoryNode.Attributes["quick_search_category"]?.Value,
                        QuickSearchPlaceholder = categoryNode.Attributes["quick_search_placeholder"]?.Value,
                        QuickSearchSetGeometry = categoryNode.Attributes["quick_search_setgeometry"]?.Value.ToLower() == "true" ? true : null
                    };

                    List<ClassCapability.FeatureInfoCategory.QuickSearchMappingItem> mappingItems = new List<ClassCapability.FeatureInfoCategory.QuickSearchMappingItem>();
                    foreach (XmlNode mappingNode in categoryNode.SelectNodes("edit:quick_search_mapping/edit:map[@source and @target]", _ns))
                    {
                        mappingItems.Add(new ClassCapability.FeatureInfoCategory.QuickSearchMappingItem()
                        {
                            Source = mappingNode.Attributes["source"]?.Value,
                            Target = mappingNode.Attributes["target"]?.Value
                        });
                    }
                    category.QuickSearchMappingItems = mappingItems.Count > 0 ? mappingItems.ToArray() : null;

                    categories.Add(category);
                }
            }

            #endregion

            #region Url Parameter Mapping

            XmlNode urlParamNode = _node.SelectSingleNode("edit:url_param_mappings", _ns);
            List<ClassCapability.FeatureInfoUrlParamMapping> urlParamMappings = new List<ClassCapability.FeatureInfoUrlParamMapping>();
            if (urlParamNode != null)
            {
                foreach (XmlNode mappingNode in urlParamNode.SelectNodes("edit:map[@source and @target]", _ns))
                {
                    urlParamMappings.Add(new ClassCapability.FeatureInfoUrlParamMapping()
                    {
                        Source = mappingNode.Attributes["source"]?.Value,
                        Target = mappingNode.Attributes["target"]?.Value
                    });
                }
            }

            #endregion

            #region Geometry (Bei SQL Spatial Layer wichtig, weil dort die Geometry "unknown" ist)

            string geomType = layer.GeometryType.ToString().ToLower();
            if (layer.GeometryType == LayerGeometryType.unknown)
            {
                geomType = GeometryTypeValue;
            }

            #endregion

            return new ClassCapability()
            {
                ThemeId = _node.Attributes["id"].Value,
                Fields = infoFields.ToArray(),
                Name = editThemeBridge.Name,
                GeometryType = geomType,
                Categories = categories.Count > 0 ? categories.ToArray() : null,
                UrlParamMapping = urlParamMappings.Count > 0 ? urlParamMappings.ToArray() : null
            };
        }

        public int MaskWidth
        {
            get
            {
                if (_node == null)
                {
                    return 200;
                }

                XmlNode maskNode = _node.SelectSingleNode("edit:mask", _ns);
                if (maskNode == null || maskNode.Attributes["width"] == null)
                {
                    return 200;
                }

                return int.Parse(maskNode.Attributes["width"].Value);
            }
        }

        public int MaskHeight
        {
            get
            {
                if (_node == null)
                {
                    return 200;
                }

                XmlNode maskNode = _node.SelectSingleNode("edit:mask", _ns);
                if (maskNode == null || maskNode.Attributes["height"] == null)
                {
                    return 200;
                }

                return int.Parse(maskNode.Attributes["height"].Value);
            }
        }

        public string DbRights
        {
            get
            {
                if (_node == null || _node.Attributes["dbrights"] == null)
                {
                    return "iudg+";
                }

                return _node.Attributes["dbrights"].Value.ToLower();
            }
        }

        internal void CheckEditPermissions(EditFeatureCommand command, IEnumerable<E.Standard.WebMapping.Core.Feature> features)
        {
            var dbRights = this.DbRights;

            if (command == EditFeatureCommand.Update && !dbRights.Contains("u"))
            {
                throw new Exception($"Das Ändern von Objekten ist für '{this.Name}' nicht erlaubt!");
            }

            if (command == EditFeatureCommand.Insert && !dbRights.Contains("i"))
            {
                throw new Exception($"Das Erzeugen von Objekten ist für '{this.Name}' nicht erlaubt!");
            }

            if (command == EditFeatureCommand.MassAttribution && !dbRights.Contains("m"))
            {
                throw new Exception($"Massenattributierung von Objekten ist für '{this.Name}' nicht erlaubt!");
            }

            if (command == EditFeatureCommand.Delete && !dbRights.Contains("d"))
            {
                throw new Exception($"Das Löschen von Objekten ist für '{this.Name}' nicht erlaubt!");
            }

            if (features != null)
            {
                foreach (var feature in features)
                {
                    if (feature.Shape != null)
                    {
                        if (!dbRights.Contains("g"))
                        {
                            throw new Exception($"Geometrie bearbeiten ist für '{this.Name}' nicht erlaubt!");
                        }

                        if (feature.Shape.IsMultipart && !dbRights.Contains("g+"))
                        {
                            throw new Exception($"Multipart Geometrien sind für '{this.Name}' nicht erlaubt!");
                        }
                    }
                }
            }
        }

        internal bool HasDbRight(EditFeatureCommand command)
        {
            try
            {
                CheckEditPermissions(command, null);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public int SrsId(int defaultSrs = 4326)
        {
            if (_node == null || _node.Attributes["srs"] == null)
            {
                return defaultSrs;
            }

            var srs = int.Parse(_node.Attributes["srs"].Value);
            if (srs <= 0)
            {
                return defaultSrs;
            }
            return srs;
        }

        public int DatasetSrsId(int defaultSrs = 4326)  // Für anwendungen wie OLE, ESP war es bisher immer so, dass vom WebGIS 4 aus keine SRS_ID in die Datenbank geschreiben wurde (id=0). Das soll so beibehalten werden, damit auch in Zukunft der Verschnitt funktioniert!!! Darum kann man mit diesem Operator die SrsId vor dem speichern noch einmal überschreiben!!!
        {
            if (_node == null)
            {
                return defaultSrs;
            }

            XmlNode connectionNode = _node.SelectSingleNode("edit:connection[@id]", _ns);
            if (connectionNode == null || connectionNode.Attributes["id"].Value == "#")
            {
                return defaultSrs;
                //throw new Exception("Connection XmlNode is NULL!");
            }


            XmlDocument doc = new XmlDocument();
            doc.Load(_editEnvironment.EditRootPath + @"/workspaces/workspaces.xml");
            XmlNode datasetNode = doc.SelectSingleNode("//DATASET[@id='" + connectionNode.Attributes["id"].Value + "']");
            if (datasetNode == null)
            {
                throw new Exception("DatasetXmlNode is NULL!");
            }

            if (datasetNode.Attributes["dataset_srs"] == null)
            {
                return defaultSrs;
            }

            return int.Parse(datasetNode.Attributes["dataset_srs"].Value);
        }

        //public int GeometryDimension(out Core.Api.ToolType toolType)
        //{
        //    toolType = Core.Api.ToolType.sketch0d;

        //    string geometryType = GeometryTypeValue;

        //    if (String.IsNullOrWhiteSpace(geometryType))
        //        return 0;

        //    switch (geometryType)
        //    {
        //        case "point":
        //            toolType = Core.Api.ToolType.sketch0d;
        //            return 20;
        //        case "line":
        //        case "polyline":
        //            toolType = Core.Api.ToolType.sketch1d;
        //            return 1;
        //        case "polygon":
        //            toolType = Core.Api.ToolType.sketch2d;
        //            return 2;
        //    }
        //    return 0;
        //}

        public string Name
        {
            get
            {
                if (_node == null)
                {
                    return "???";
                }

                if (_node.Attributes["name"] == null)
                {
                    return _node.Attributes["id"].Value;
                }

                return _node.Attributes["name"].Value;
            }
        }

        public string VersionName
        {
            get
            {
                if (_node == null || _node.Attributes["version"] == null)
                {
                    return String.Empty;
                }

                return _node.Attributes["version"].Value;
            }
        }

        public bool RebuildSpatialIndex
        {
            get
            {
                if (_node == null || _node.Attributes["RebuildSpatialIndex"] == null)
                {
                    return false;
                }

                return _node.Attributes["RebuildSpatialIndex"].Value.ToLower() == "true";
            }
        }

        public string IdPrefix
        {
            get { return _idPrefix; }
            set { _idPrefix = value; }
        }

        public bool AutoExplodeMultipartFeatures { get; set; } = false;

        public string GetDbConnectionString(string fieldname)
        {
            XmlNode attributeNode = _node.SelectSingleNode(".//edit:attribute[@field='" + fieldname + "' and @db_connectionstring]", _ns);
            if (attributeNode != null)
            {
                return attributeNode.Attributes["db_connectionstring"].Value;
            }

            return String.Empty;
        }
        public (string statement, string valueFieldName, string aliasFieldName) GetDbSqlStatement(string fieldname)
        {
            XmlNode attributeNode = _node.SelectSingleNode(".//edit:attribute[@field='" + fieldname + "' and @db_sql]", _ns);
            if (attributeNode != null)
            {
                return (statement: attributeNode.Attributes["db_sql"].Value, valueFieldName: null, aliasFieldName: null);
            }

            attributeNode = _node.SelectSingleNode(".//edit:attribute[@field='" + fieldname + "' and @db_table and @db_where and @db_valuefield and @db_aliasfield]", _ns);
            if (attributeNode != null)
            {
                var uniqueList = new UniqueList();
                uniqueList.Add(attributeNode.Attributes["db_valuefield"].Value);
                uniqueList.Add(attributeNode.Attributes["db_aliasfield"].Value);

                string connectionString = GetDbConnectionString(fieldname), statement;

                if (connectionString.IsValidHttpUrl())
                {
                    statement = attributeNode.Attributes["db_where"].Value;
                }
                else
                {
                    statement = $"select {String.Join(",", uniqueList.ToArray())} from {attributeNode.Attributes["db_table"].Value} where {attributeNode.Attributes["db_where"].Value}";
                }

                return (statement: statement,
                        valueFieldName: attributeNode.Attributes["db_valuefield"].Value,
                        aliasFieldName: attributeNode.Attributes["db_aliasfield"].Value);
            }

            return (statement: String.Empty, valueFieldName: null, aliasFieldName: null);
        }

        public LayerGeometryType GeometryType
        {
            get
            {
                switch (GeometryTypeValue)
                {
                    case "point":
                        return LayerGeometryType.point;
                    case "line":
                    case "polyline":
                        return LayerGeometryType.line;
                    case "polygon":
                        return LayerGeometryType.polygon;
                    default:
                        return LayerGeometryType.unknown;

                }
            }
        }

        public IEnumerable<Validation> MaskValidations
        {
            get
            {
                if (_node == null || _node.SelectSingleNode("edit:mask", _ns) == null)
                {
                    return Array.Empty<Validation>();
                }

                List<Validation> validations = new List<Validation>();
                XmlNode maskNode = _node.SelectSingleNode("edit:mask", _ns);

                foreach (XmlNode validationNode in maskNode.SelectNodes("edit:validations/edit:validation[@field and @validator]", _ns))
                {
                    var validation = new Validation()
                    {
                        FieldName = validationNode.Attributes["field"].Value,
                        Validator = validationNode.Attributes["validator"].Value
                    };
                    if (validationNode.Attributes["operator"] != null)
                    {
                        validation.Operator = validationNode.Attributes["operator"].Value;
                    }

                    if (validationNode.Attributes["message"] != null)
                    {
                        validation.Message = validationNode.Attributes["message"].Value;
                    }

                    validations.Add(validation);
                }

                return validations;
            }
        }

        #region Helper

        private string GeometryTypeValue
        {
            get
            {
                if (_node.Attributes["geomtype"] != null)
                {
                    return _node.Attributes["geomtype"].Value.ToLower();
                }

                if (_node.Attributes["geometry_type"] != null)
                {
                    return _node.Attributes["geometry_type"].Value.ToLower();
                }

                return String.Empty;
            }
        }

        private IUIElement CreateEditFieldContainer(XmlNode fieldNode, EditOperation editOperation, string fieldPrefix)
        {
            if (editOperation.IsMassOrTransfer())
            {
                string field = fieldNode.Attributes["field"].Value;

                //bool isChecked = editOperation == 
                //    EditOperation.FeatureTransfer && fieldNode.Attributes["type"]?.Value?.ToLower() == "domain";
                bool isChecked = false;

                return new UICheckableDiv()
                {
                    id = $"_{fieldPrefix}_applyfield_{field}",
                    css = UICss.ToClass(new string[] { UICss.ToolParameter, $"webgis-editfield-container {(isChecked ? "checked" : "")}" }),
                };
            }
            else
            {
                return new UIDiv()
                {
                    css = "webgis-editfield-container"
                };
            }
        }

        private IUIElement FieldPrompt(string fieldId, XmlNode fieldNode, EditOperation editOperation)
        {
            IUIElement result = null;
            string label = fieldNode.Attributes["prompt"] != null ? fieldNode.Attributes["prompt"].Value : fieldNode.Attributes["field"].Value + ":";

            if ((editOperation == EditOperation.Insert || editOperation == EditOperation.Update || editOperation == EditOperation.MassAttributation) &&
                fieldNode.Attributes["legend_field"] != null &&
                !String.IsNullOrWhiteSpace(fieldNode.Attributes["legend_field"].Value))
            {
                result = new UIButton(UIButton.UIButtonType.servertoolcommand, "show-select-by-legend[" + fieldNode.Attributes["legend_field"].Value + "]")
                {
                    text = label,
                    css = "webgis-label"
                };
            }
            else if (fieldNode.Attributes["type"]?.Value == "angle360")
            {
                result = new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.showsketchanglehelperline)
                {
                    text = label,
                    css = "webgis-label",
                    buttoncommand_argument = $"#{fieldId}"
                };
            }
            else if (fieldNode.Attributes["type"]?.Value == "angle360_geographic")
            {
                result = new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.showsketchanglegeographichelperline)
                {
                    text = label,
                    css = "webgis-label",
                    buttoncommand_argument = $"#{fieldId}"
                };
            }
            else if (fieldNode.Attributes["type"]?.Value == "attribute_picker")
            {
                result = new UIButton(UIButton.UIButtonType.servertoolcommand_ext,
                                      "pick_attribute")
                { buttoncommand_argument = $"{fieldNode.Attributes["attribute_picker_service"]?.Value},{fieldNode.Attributes["attribute_picker_query"]?.Value},{fieldNode.Attributes["attribute_picker_field"]?.Value},{fieldId}" }
                    .WithId(typeof(Identify.IdentifyDefault).ToToolId())
                    .WithText(label)
                    .WithStyles("webgis-label");
            }
            else
            {
                result = new UILabel()
                {
                    label = label
                };
            }

            return result;
        }

        #endregion

        #region Classes


        public class Validation
        {
            public string FieldName { get; set; }
            public string Operator { get; set; }
            public string Validator { get; set; }
            public string Message { get; set; }
        }


        #endregion
    }

    public class HistoryItem
    {
        [JsonProperty(PropertyName = "theme")]
        [System.Text.Json.Serialization.JsonPropertyName("theme")]
        public string Theme { get; set; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public EditOperation Operation { get; set; }

        [JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public Feature Feature { get; set; }
    }

    public class UIEditMask
    {
        public ICollection<IUIElement> UIElements { get; set; }
        public ICollection<IUISetter> UISetters { get; set; }
    }

    public class ClassCapability
    {
        [JsonProperty(PropertyName = "themeid")]
        [System.Text.Json.Serialization.JsonPropertyName("themeid")]
        public string ThemeId { get; set; }

        [JsonProperty(PropertyName = "name")]
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "fields")]
        [System.Text.Json.Serialization.JsonPropertyName("fields")]
        public FeatureInfoField[] Fields { get; set; }

        [JsonProperty(PropertyName = "categories", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("categories")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public FeatureInfoCategory[] Categories { get; set; }

        [JsonProperty(PropertyName = "url_param_mapping", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("url_param_mapping")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public FeatureInfoUrlParamMapping[] UrlParamMapping { get; set; }

        [JsonProperty(PropertyName = "geometrytype")]
        [System.Text.Json.Serialization.JsonPropertyName("geometrytype")]
        public string GeometryType { get; set; }

        public class FeatureInfoField
        {
            [JsonProperty(PropertyName = "name")]
            [System.Text.Json.Serialization.JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "prompt")]
            [System.Text.Json.Serialization.JsonPropertyName("prompt")]
            public string Aliasname { get; set; }

            [JsonProperty(PropertyName = "visible")]
            [System.Text.Json.Serialization.JsonPropertyName("visible")]
            public bool Visible { get; set; }

            [JsonProperty(PropertyName = "label", NullValueHandling = NullValueHandling.Ignore)]
            [System.Text.Json.Serialization.JsonPropertyName("label")]
            [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
            public string Label { get; set; }

            [JsonProperty(PropertyName = "readonly")]
            [System.Text.Json.Serialization.JsonPropertyName("readonly")]
            public bool ReadOnly { get; set; }

            [JsonProperty(PropertyName = "type", NullValueHandling = NullValueHandling.Ignore)]
            [System.Text.Json.Serialization.JsonPropertyName("type")]
            [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
            public string Type { get; set; }

            [JsonProperty(PropertyName = "category", NullValueHandling = NullValueHandling.Ignore)]
            [System.Text.Json.Serialization.JsonPropertyName("category")]
            [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
            public string Category { get; set; }

            [JsonProperty(PropertyName = "filedatefield", NullValueHandling = NullValueHandling.Ignore)]
            [System.Text.Json.Serialization.JsonPropertyName("filedatefield")]
            [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
            public string FileDateField { get; set; }

            [JsonProperty(PropertyName = "domainvalues", NullValueHandling = NullValueHandling.Ignore)]
            [System.Text.Json.Serialization.JsonPropertyName("domainvalues")]
            [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
            public FeatureInfoFieldDomain[] DomainValues { get; set; }

            [JsonProperty(PropertyName = "required", NullValueHandling = NullValueHandling.Ignore)]
            [System.Text.Json.Serialization.JsonPropertyName("required")]
            [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
            public bool? Required { get; set; }

            [JsonProperty(PropertyName = "default_value", NullValueHandling = NullValueHandling.Ignore)]
            [System.Text.Json.Serialization.JsonPropertyName("default_value")]
            [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
            public string DefaultValue { get; set; }

            public class FeatureInfoFieldDomain
            {
                [JsonProperty(PropertyName = "value")]
                [System.Text.Json.Serialization.JsonPropertyName("value")]
                public string Value { get; set; }

                [JsonProperty(PropertyName = "label")]
                [System.Text.Json.Serialization.JsonPropertyName("label")]
                public string Label { get; set; }
            }
        }

        public class FeatureInfoCategory
        {
            [JsonProperty(PropertyName = "name", NullValueHandling = NullValueHandling.Ignore)]
            [System.Text.Json.Serialization.JsonPropertyName("name")]
            [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
            public string Name { get; set; }

            [JsonProperty(PropertyName = "id", NullValueHandling = NullValueHandling.Ignore)]
            [System.Text.Json.Serialization.JsonPropertyName("id")]
            [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
            public string Id { get; set; }

            [JsonProperty(PropertyName = "is_default", NullValueHandling = NullValueHandling.Ignore)]
            [System.Text.Json.Serialization.JsonPropertyName("is_default")]
            [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
            public bool? IsDefault { get; set; }

            [JsonProperty(PropertyName = "collapsed", NullValueHandling = NullValueHandling.Ignore)]
            [System.Text.Json.Serialization.JsonPropertyName("collapsed")]
            [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
            public bool? Collapsed { get; set; }

            [JsonProperty(PropertyName = "description", NullValueHandling = NullValueHandling.Ignore)]
            [System.Text.Json.Serialization.JsonPropertyName("description")]
            [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
            public string Description { get; set; }

            [JsonProperty(PropertyName = "quick_search_service", NullValueHandling = NullValueHandling.Ignore)]
            [System.Text.Json.Serialization.JsonPropertyName("quick_search_service")]
            [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
            public string QuickSearchService { get; set; }

            [JsonProperty(PropertyName = "quick_search_category", NullValueHandling = NullValueHandling.Ignore)]
            [System.Text.Json.Serialization.JsonPropertyName("quick_search_category")]
            [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
            public string QuickSearchCategory { get; set; }

            [JsonProperty(PropertyName = "quick_search_placeholder", NullValueHandling = NullValueHandling.Ignore)]
            [System.Text.Json.Serialization.JsonPropertyName("quick_search_placeholder")]
            [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
            public string QuickSearchPlaceholder { get; set; }

            [JsonProperty(PropertyName = "quick_search_setgeometry", NullValueHandling = NullValueHandling.Ignore)]
            [System.Text.Json.Serialization.JsonPropertyName("quick_search_setgeometry")]
            [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
            public bool? QuickSearchSetGeometry { get; set; }

            [JsonProperty(PropertyName = "quick_search_mapping", NullValueHandling = NullValueHandling.Ignore)]
            [System.Text.Json.Serialization.JsonPropertyName("quick_search_mapping")]
            [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
            public QuickSearchMappingItem[] QuickSearchMappingItems { get; set; }

            public class QuickSearchMappingItem
            {
                [JsonProperty(PropertyName = "source")]
                [System.Text.Json.Serialization.JsonPropertyName("source")]
                public string Source { get; set; }

                [JsonProperty(PropertyName = "target")]
                [System.Text.Json.Serialization.JsonPropertyName("target")]
                public string Target { get; set; }
            }
        }

        public class FeatureInfoUrlParamMapping
        {
            [JsonProperty(PropertyName = "source")]
            [System.Text.Json.Serialization.JsonPropertyName("source")]
            public string Source { get; set; }

            [JsonProperty(PropertyName = "target")]
            [System.Text.Json.Serialization.JsonPropertyName("target")]
            public string Target { get; set; }
        }
    }

    public IUISetter FieldSetter(string fieldName, string value)
    {
        var fieldPrefix = this._fieldPrefix ?? "editfield";
        return new UISetter($"{fieldPrefix}_" + fieldName, value);
    }

    #endregion

    #region Helpers

    async private Task<(string value, bool setIt)> GetAutoValue(EditFeatureCommand editTask,
                                                                string targetFieldName,
                                                                EditTheme editTheme,
                                                                string autoValue,
                                                                WebMapping.Core.Feature feature,
                                                                string custom1,
                                                                string custom2)
    {
        if (editTheme == null || String.IsNullOrEmpty(autoValue) || autoValue.StartsWith("mask-insert-default::"))
        {
            return (value: String.Empty, setIt: false);
        }

        autoValue = autoValue.Trim();

        #region Räumliche Abfrage

        if (feature != null && feature.Shape != null && autoValue.ToLower().Contains(" from "))
        {
            string[] args = autoValue.SplitQuotedString(); //autoValue.Split(' ');

            double bufferDist = 0.0;
            string seperator = ";", serviceId = "";
            int count = 20;
            for (int i = 3; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "bufferdist":
                        bufferDist = Convert.ToDouble(args[++i].Replace(".", ","));
                        break;
                    case "seperator":
                        seperator = args[++i];
                        seperator = seperator.Replace("space", " ");
                        break;
                    case "max":
                        count = Convert.ToInt32(args[++i]);
                        break;
                    case "service":
                        serviceId = args[++i];
                        break;
                }
            }

            ILayerBridge layer = null;

            string fieldName = String.Empty;
            if (args.Length > 2)
            {
                fieldName = args[0].Trim();
                string layerName = args[2].Trim();
                IServiceBridge service = await this.Bridge.GetService(serviceId);
                if (service == null)
                {
                    throw new ArgumentException("Autovalue: Service " + serviceId + " not found");
                }

                layer = service.Layers.Where(l => l.Name == layerName).FirstOrDefault();
                if (layer == null)
                {
                    layer = service.Layers.Where(l => l.Id == layerName).FirstOrDefault();
                }

                if (layer == null)
                {
                    throw new ArgumentException("Layer " + layerName + " not found: " + autoValue);
                }

                if (layer != null && layer.GeometryType == LayerGeometryType.point)
                {
                    bufferDist = Math.Max(bufferDist, 0.03);
                }
            }

            if (layer != null && !String.IsNullOrEmpty(fieldName))
            {
                WebMapping.Core.Filters.BufferFilter shapeBuffer = feature.Shape.Buffer;
                Shape filterShape = feature.Shape;
                SpatialReference filterSref = this.Bridge.CreateSpatialReference(feature.Shape.SrsId);

                if (bufferDist != 0.0)
                {
                    feature.Shape.Buffer = null;
                    using (var cts = new CancellationTokenSource())
                    {
                        filterShape = filterShape.CalcBuffer(bufferDist, cts) ?? filterShape;
                    }
                }

                var features = await this.Bridge.QueryLayerAsync(serviceId, layer.Id, String.Empty, QueryFields.All, filterSref, filterShape /*feature.Shape*/);
                feature.Shape.Buffer = shapeBuffer;

                int featureCounter = 0;
                StringBuilder sb = new StringBuilder();
                foreach (WebMapping.Core.Feature f in features)
                {
                    string v = f[fieldName];
                    if (v == null)
                    {
                        continue;
                    }

                    if (sb.Length > 0)
                    {
                        sb.Append(seperator);
                    }

                    sb.Append(v.ToString());

                    featureCounter++;
                    if (featureCounter >= count)  // Für AGS immer mitzählen. Da funkt der MAX-Wert für den Filter nicht mehr!!
                    {
                        break;
                    }
                }

                return (value: sb.ToString(), setIt: true);
            }
            throw new ArgumentException("Spatial-Filter-Autovalues are not supported for API: " + autoValue);
        }

        #endregion

        #region Simple Expressions

        if (autoValue.IndexOf("=") == 0)
        {
            string val = autoValue.Substring(1, autoValue.Length - 1);

            string[] keys = ExtractKeyParameters(val);

            if (keys != null)
            {
                string keyVal = String.Empty;
                foreach (string key in keys)
                {
                    if (key == ":shape_len" && feature != null && feature.Shape is Polyline)
                    {
                        keyVal = Math.Round(((Polyline)feature.Shape).Length, 2).ToPlatformNumberString();
                    }
                    else if (key == ":shape_len_int" && feature != null && feature.Shape is Polyline)
                    {
                        keyVal = Math.Round(((Polyline)feature.Shape).Length, 0).ToString();
                    }
                    else if (key == ":shape_area" && feature != null && feature.Shape is Polygon)
                    {
                        keyVal = Math.Round(((Polygon)feature.Shape).Area, 2).ToPlatformNumberString();
                    }
                    else if (key == ":shape_area_int" && feature != null && feature.Shape is Polygon)
                    {
                        keyVal = Math.Round(((Polygon)feature.Shape).Area, 0).ToString();
                    }
                    else if (feature != null && feature[key] != null)
                    {
                        keyVal = feature[key].ToString();
                    }
                    val = val.Replace("[" + key + "]", keyVal);
                }
            }
            return (value: val.Trim(), setIt: true);
        }

        #endregion

        #region Role-Parameters

        string roleParameter = String.Empty;

        if (autoValue.ToLower().StartsWith("role-parameter:"))
        {
            roleParameter = autoValue.Substring(15, autoValue.Length - 15);
        }
        else if (autoValue.ToLower().StartsWith("oninsert:role-parameter:") && editTask == EditFeatureCommand.Insert)
        {
            roleParameter = autoValue.Substring(24, autoValue.Length - 24);
        }
        else if (autoValue.ToLower().StartsWith("onupdate:role-parameter:") && editTask == EditFeatureCommand.Update)
        {
            roleParameter = autoValue.Substring(24, autoValue.Length - 24);
        }

        if (!String.IsNullOrEmpty(roleParameter))
        {
            var roleParameterValue = this.Bridge?.CurrentUser?.UserRoleParameters.ParameterValue<string>(roleParameter);
            return (value: roleParameterValue ?? String.Empty, setIt: true);
        }

        #endregion

        #region Url Parameters

        var urlParameter = String.Empty;

        if (autoValue.ToLower().StartsWith("url-parameter:"))
        {
            urlParameter = autoValue.Substring("url-parameter:".Length);
        }
        else if (autoValue.ToLower().StartsWith("oninsert:url-parameter:") && editTask == EditFeatureCommand.Insert)
        {
            urlParameter = autoValue.Substring("oninsert:url-parameter:".Length);
        }
        else if (autoValue.ToLower().StartsWith("onupdate:url-parameter:") && editTask == EditFeatureCommand.Update)
        {
            urlParameter = autoValue.Substring("onupdate:url-parameter:".Length);
        }

        if (!String.IsNullOrEmpty(urlParameter))
        {
            var urlParameterValue = this.Bridge.GetOriginalUrlParameterValue(urlParameter);
            return (value: urlParameterValue ?? String.Empty, setIt: true);
        }

        #endregion

        #region Simple Autovalues

        switch (autoValue.ToLower())
        {
            case "create_user":
                if (editTask == EditFeatureCommand.Insert)
                {
                    return (value: editTheme.DbUsername, setIt: true);
                }
                break;
            case "create_login":
                if (editTask == EditFeatureCommand.Insert)
                {
                    return (value: this.Bridge.CurrentUser.Username.RemoveUserIdentificationNamespace(), setIt: true);
                }
                break;
            case "create_login_full":
                if (editTask == EditFeatureCommand.Insert)
                {
                    return (value: this.Bridge.CurrentUser.Username, setIt: true);
                }
                break;
            case "create_login_short":
                if (editTask == EditFeatureCommand.Insert)
                {
                    string create_login_short = this.Bridge.CurrentUser.Username.RemoveUserIdentificationNamespace()
                                                                                .RemoveUserIdentificationDomain();
                    return (value: create_login_short, setIt: true);
                }
                break;
            case "create_login_domain":
                if (editTask == EditFeatureCommand.Insert)
                {
                    return (value: this.Bridge.CurrentUser.Username.UsernameDomain(), setIt: true);
                }
                break;
            case "guid":   // 4b2dd0dfeb1b40188b2583167886e886
                if (editTask == EditFeatureCommand.Insert)
                {
                    return (value: System.Guid.NewGuid().ToString("N"), setIt: true);
                }
                break;
            case "guid_sql":  // {9e2702e4-169f-41ec-b3e3-fcf786182885}
                if (editTask == EditFeatureCommand.Insert)
                {
                    return (value: System.Guid.NewGuid().ToString("B"), setIt: true);
                }
                break;
            case "guid_v7":  
                if (editTask == EditFeatureCommand.Insert)
                {
                    return (value: System.Guid.CreateVersion7().ToString("N"), setIt: true);
                }
                break;
            case "guid_v7_sql":
                if (editTask == EditFeatureCommand.Insert)
                {
                    return (value: System.Guid.CreateVersion7().ToString("B"), setIt: true);
                }
                break;
            case "sessionid":
                if (editTask == EditFeatureCommand.Insert)
                {
                    //setIt = true;
                    //return System.Web.HttpContext.Current.Session.SessionID;
                }
                break;
            case "create_date":
                if (editTask == EditFeatureCommand.Insert)
                {
                    return (value: DateTime.Now.ToShortDateString(), setIt: true);
                }
                break;
            case "create_date_yyyy.mm.dd":
                if (editTask == EditFeatureCommand.Insert)
                {
                    return (value: DateTime.Now.ToString("yyyy.MM.dd"), setIt: true);
                }
                break;
            case "create_time":
                if (editTask == EditFeatureCommand.Insert)
                {
                    return (value: DateTime.Now.ToShortTimeString(), setIt: true);
                }
                break;
            case "create_datetime_sql":
                if (editTask == EditFeatureCommand.Insert)
                {
                    return (value: DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString(), setIt: true);
                }
                break;
            case "create_datetime_sql2":
                if (editTask == EditFeatureCommand.Insert)
                {
                    return (value: DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"), setIt: true);
                }
                break;
            case "change_user":
                return (value: editTheme.DbUsername, setIt: true);
            case "change_login":
                return (value: this.Bridge.CurrentUser.Username.RemoveUserIdentificationNamespace(), setIt: true);
            case "change_login_full":
                return (value: this.Bridge.CurrentUser.Username, setIt: true);
            case "change_login_short":
                string change_login_short = this.Bridge.CurrentUser.Username.RemoveUserIdentificationNamespace()
                                                                            .RemoveUserIdentificationDomain();
                return (value: change_login_short, setIt: true);
            case "change_login_domain":
                return (value: this.Bridge.CurrentUser.Username.UsernameDomain(), setIt: true);
            case "change_date":
                return (value: DateTime.Now.ToShortDateString(), setIt: true);
            case "change_time":
                return (value: DateTime.Now.ToShortTimeString(), setIt: true);
            case "change_datetime_sql":
                return (value: DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString(), setIt: true);
            case "change_datetime_sql2":
                return (value: DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"), setIt: true);
            case "datetime":
                return (value: DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), setIt: true);
            case "scale":
                return (value: Math.Round(this.CurrentMapScale, 0).ToString(), setIt: true);
            case "shape_len":
                if (feature != null && feature.Shape is Polyline)
                {
                    return (value: Math.Round(((Polyline)feature.Shape).Length, 2).ToPlatformNumberString(), setIt: true);
                }
                break;
            case "shape_len_int":
                if (feature != null && feature.Shape is Polyline)
                {
                    return (value: Math.Round(((Polyline)feature.Shape).Length, 0).ToString(), setIt: true);
                }
                break;
            case "shape_area":
                if (feature != null && feature.Shape is Polygon)
                {
                    return (value: Math.Round(((Polygon)feature.Shape).Area, 2).ToPlatformNumberString(), setIt: true);
                }
                break;
            case "shape_area_int":
                if (feature != null && feature.Shape is Polygon)
                {
                    return (value: Math.Round(((Polygon)feature.Shape).Area, 0).ToString(), setIt: true);
                }
                break;
            case "shape_minx":
                if (feature != null && feature.Shape != null && feature.Shape.ShapeEnvelope != null)
                {
                    return (value: feature.Shape.ShapeEnvelope.MinX.ToPlatformNumberString(), setIt: true);
                }
                break;
            case "shape_miny":
                if (feature != null && feature.Shape != null && feature.Shape.ShapeEnvelope != null)
                {
                    return (value: feature.Shape.ShapeEnvelope.MinY.ToPlatformNumberString(), setIt: true);
                }
                break;
            case "shape_maxx":
                if (feature != null && feature.Shape != null && feature.Shape.ShapeEnvelope != null)
                {
                    return (value: feature.Shape.ShapeEnvelope.MaxX.ToPlatformNumberString(), setIt: true);
                }
                break;
            case "shape_maxy":
                if (feature != null && feature.Shape != null && feature.Shape.ShapeEnvelope != null)
                {
                    return (value: feature.Shape.ShapeEnvelope.MaxY.ToPlatformNumberString(), setIt: true);
                }
                break;
            case "db_select":
            case "db_select_on_insert":

                if (autoValue.ToLower() == "db_select_on_insert" && editTask != EditFeatureCommand.Insert)
                {
                    break;
                }

                if (String.IsNullOrEmpty(custom1))
                {
                    throw new ArgumentException("Autovalue db_select: ConnectionString not set! Set ConnectionString in autovalue_custom1!");
                }
                if (String.IsNullOrEmpty(custom2))
                {
                    throw new ArgumentException("Autovalue db_select: SQL Statement not set! Set SQL Select Statement in autovalue_custom2!");
                }

                try
                {
                    var sql = custom2;
                    sql = this.Bridge.ReplaceUserAndSessionDependentFilterKeys(sql, startingBracket: "{{", endingBracket: "}}");
                    var sqlKeyParameters = E.Standard.WebGIS.CMS.Globals.KeyParameters(sql, startingBracket: "{{", endingBracket: "}}") ?? Array.Empty<string>();

                    if (editTask == EditFeatureCommand.MassAttribution)
                    {
                        //
                        // Bei Massenattributierung kann dieser Wert nur neu berechent werden
                        // wenn sich ein Feld aus der Massenattributierung auf den Wert auswirkt
                        //
                        int containedAttributesCount = sqlKeyParameters.Where(s => feature.Attributes[s] != null).Count();
                        if (containedAttributesCount == 0)  // nicht betroffen
                        {
                            return (value: null, setIt: false);
                        }
                        else if (containedAttributesCount < sqlKeyParameters.Distinct().Count())  // Es fehlen Attribute
                        {
                            var missingAttributeNames = sqlKeyParameters.Where(s => feature.Attributes[s] == null).ToArray();
                            throw new Exception($"Nicht alle notewendigen Felder wurden übergeben. Folgende Attribute sind nicht in der Massenattributierung enthalten: [{String.Join(", ", missingAttributeNames)}]");
                        }
                    }

                    if (custom1.IsValidHttpUrl())
                    {
                        #region Web Serice Datalinq Query

                        var url = custom1;

                        if (!String.IsNullOrEmpty(custom2))
                        {
                            foreach (var sqlKeyParameter in sqlKeyParameters)
                            {
                                custom2 = custom2.Replace($"{{{{{sqlKeyParameter}}}}}", System.Web.HttpUtility.UrlEncode(feature.Attributes[sqlKeyParameter]?.Value ?? String.Empty));
                            }

                            url += $"{(url.Contains("?") ? "&" : "?")}{custom2}";
                        }

                        var jsonResult = await Bridge.HttpService.GetStringAsync(url);

                        var firstElement = JSerializer.Deserialize<object[]>(jsonResult).FirstOrDefault();
                        string firstElementValue = null;

                        if (firstElement != null)
                        {
                            string labelProperty = "name";  // DoTo: this is hardcoded...
                            firstElementValue = JSerializer.GetJsonElementValue(firstElement, labelProperty)?.ToString();
                        }

                        return (value: firstElementValue, setIt: true);

                        #endregion
                    }
                    else
                    {
                        #region DB

                        using (var dbFactory = new DBFactory(custom1))
                        using (var connection = dbFactory.GetConnection())
                        using (var command = dbFactory.GetCommand(connection))
                        {
                            int index = 0;
                            foreach (var sqlKeyParameter in sqlKeyParameters)
                            {
                                var parameterName = dbFactory.ParaName($"p{index++}");
                                command.Parameters.Add(dbFactory.GetParameter(parameterName, feature.Attributes[sqlKeyParameter]?.Value));
                                sql = sql.Replace("{{" + sqlKeyParameter + "}}", parameterName);
                            }

                            command.CommandText = sql;
                            await connection.OpenAsync();
                            var value = await command.ExecuteScalarAsync();

                            return (value: value?.ToString(), setIt: true);
                        }

                        #endregion
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"{targetFieldName}: Autovalue - db_select: {ex.Message}", ex);
                }
        }

        #endregion

        return (value: String.Empty, setIt: false);
    }

    private string[] ExtractKeyParameters(string commandLine)
    {
        int pos1 = 0, pos2;
        pos1 = commandLine.IndexOf("[");
        string parameters = "";

        while (pos1 != -1)
        {
            pos2 = commandLine.IndexOf("]", pos1);
            if (pos2 == -1)
            {
                break;
            }

            if (parameters != "")
            {
                parameters += ";";
            }

            parameters += commandLine.Substring(pos1 + 1, pos2 - pos1 - 1);
            pos1 = commandLine.IndexOf("[", pos2);
        }
        if (parameters != "")
        {
            return parameters.Split(';');
        }
        else
        {
            return null;
        }
    }

    private string StringFormatAttributeValue(XmlNode attribute, string val)
    {
        if (attribute.Attributes["string_format"] == null || !attribute.Attributes["string_format"].Value.Contains("{0}"))
        {
            return val;
        }

        return String.Format(attribute.Attributes["string_format"].Value, val);
    }

    #endregion

    #region Static Members

    static internal void AppendLayerWarnings(IUIElement parentElement, EditThemeDefinition editThemeDef, ILocalizer<Edit> localizer)
    {
        var globalLayerId = $"{editThemeDef.ServiceId}:{editThemeDef.LayerId}";
        // Check if layer is in scale
        var conditionDivInScale = new UIConditionDiv()
        {
            ConditionType = UIConditionDiv.ConditionTypes.LayersInScale,
            ConditionArguments = new string[] { globalLayerId },
            ConditionResult = false
        };

        conditionDivInScale.AddChild(new UIBreak());
        conditionDivInScale.AddChild(new UIDiv()
        {
            css = UICss.ToClass(new string[] { "webgis-info" }),
            elements = new IUIElement[]
            {
                        new UILiteral() { literal = localizer.Localize("warning-affected-layer1") }
            }
        });

        parentElement.InsertChild(0, conditionDivInScale);

        // Check if layer is visible


        var conditionDivVisiblity = new UIConditionDiv()
        {
            ConditionType = UIConditionDiv.ConditionTypes.LayersVisible,
            ConditionArguments = new string[] { globalLayerId },
            ConditionResult = false
        };

        conditionDivVisiblity.AddChild(new UIBreak());
        conditionDivVisiblity.AddChild(new UIDiv()
        {
            css = UICss.ToClass(new string[] { "webgis-info" }),
            elements = new IUIElement[]
            {
                        new UILiteral() { literal = localizer.Localize("warning-affected-layer2") }
            }
        });
        conditionDivVisiblity.AddChild(new UIButton(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.setlayersvisible)
        {
            //text = $"Layer { editThemeDef.EditThemeName } sichtbar schalten",
            text = localizer.Localize("button-affected-layer-visible"),
            buttoncommand_argument = globalLayerId,
            css = UICss.ToClass(new string[] { UICss.DangerButtonStyle })
        });

        parentElement.InsertChild(0, conditionDivVisiblity);
    }

    #endregion
}
