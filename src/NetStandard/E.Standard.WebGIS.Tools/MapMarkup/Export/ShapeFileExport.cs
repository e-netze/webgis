using E.Standard.Esri.Shapefile;
using E.Standard.Esri.Shapefile.IO;
using E.Standard.GeoJson;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace E.Standard.WebGIS.Tools.MapMarkup.Export;

class ShapeFileExport : IExport
{
    private readonly MemoryStream _memoryStream;
    private readonly IStreamProvider _streamProvider;
    private readonly IBridge _bridge;
    private int _targetEpsgCode;
    private int _featuresCount = 0;

    public ShapeFileExport(IBridge bridge, int targetEpsgCode)
    {
        _memoryStream = new MemoryStream();
        _streamProvider = new ZipArchiveStreamProvider(_memoryStream);

        _bridge = bridge;
        _targetEpsgCode = targetEpsgCode;
    }

    public int FeatureCount => _featuresCount;

    public void AddFeatures(GeoJsonFeatures features)
    {
        var toolTypes = features.Features
                                .Select(f => f.GetPropery<string>("_meta.tool"))
                                .Distinct();

        #region Read Prj File

        byte[] prjFileBytes = null;
        try
        {
            prjFileBytes = System.Text.Encoding.Default.GetBytes(File.ReadAllText($"{_bridge.AppEtcPath}/prj/{_targetEpsgCode}.prj"));
        }
        catch { }

        #endregion

        using (var transformer = _bridge.GeometryTransformer(4326, _targetEpsgCode))
        {
            foreach (var toolType in toolTypes)
            {
                if (!Mapping.ShapeGeometryMapping.ContainsKey(toolType) ||
                   !Mapping.ShapePropertyMapping.ContainsKey(toolType))
                {
                    continue;
                }

                ShapeFile.geometryType geomtryType = Mapping.ShapeGeometryMapping[toolType];
                var propertymapping = Mapping.ShapePropertyMapping[toolType];

                List<IField> fields = new List<IField>();
                fields.Add(new Field("ID", FieldType.ID));
                fields.AddRange(propertymapping.Keys
                                               .Select(k => new Field(k)));

                var shapeFile = ShapeFile.Create(_streamProvider, toolType, geomtryType, fields);
                if (shapeFile == null)
                {
                    throw new Exception($"Can't create shape file {toolType}");
                }

                foreach (var feature in features.Features
                                                .Where(f => f.GetPropery<string>("_meta.tool") == toolType))
                {
                    Feature shapeFeature = new Feature();

                    var shape = feature.ToShape();
                    transformer.Transform(shape);
                    shapeFeature.Shape = shape;

                    foreach (var fieldName in propertymapping.Keys)
                    {
                        shapeFeature.Attributes.Add(new WebMapping.Core.Attribute(fieldName, feature.GetPropery<string>(propertymapping[fieldName])));
                    }

                    shapeFile.WriteShape(shapeFeature);
                    _featuresCount++;
                }

                #region Prj File

                if (prjFileBytes != null)
                {
                    _streamProvider[$"{toolType}.prj"].Write(prjFileBytes, 0, prjFileBytes.Length);
                }

                #endregion
            }
        }
    }

    public byte[] GetBytes(bool throwExcetionIfEmpty)
    {
        if (throwExcetionIfEmpty && _featuresCount == 0)
        {
            throw new Exception("Für den Shape Export wurden keine Objekte gefunden.");
        }

        _streamProvider.FlushAndReleaseStreams();

        return _memoryStream.ToArray();
    }
}
