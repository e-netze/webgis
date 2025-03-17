#nullable enable

using E.Standard.WebMapping.Core.Geometry;

namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.RequestBuilders;

public class GetFeaturesRequestBuilder : BaseRequestBuilder<GetFeaturesRequestBuilder>
{
    public GetFeaturesRequestBuilder()
    {
        _self = this;
    }

    new public GetFeaturesRequestBuilder WithFormat(string format) => base.WithFormat(format);

    new public GetFeaturesRequestBuilder WithGeometry(Shape shape, int shapeSrefId = 0)
        => base.WithGeometry(shape, shapeSrefId);

    new public GetFeaturesRequestBuilder WithSpatialRelation(string spatialRelation)
        => base.WithSpatialRelation(spatialRelation);

    public GetFeaturesRequestBuilder WithSpatialRelationIntersects()
        => base.WithSpatialRelation("esriSpatialRelIntersects");

    new public GetFeaturesRequestBuilder WithWhereClause(string where)
        => base.WithWhereClause(where);

    new public GetFeaturesRequestBuilder WithOrderByFields(string fields)
        => base.WithOrderByFields(fields);

    new public GetFeaturesRequestBuilder WithOutFields(string outFields)
        => base.WithOutFields(outFields);

    new public GetFeaturesRequestBuilder WithResultRecordCount(int? count)
        => base.WithResultRecordCount(count);

    new public GetFeaturesRequestBuilder WithInSpatialReferenceId(int sRefId)
        => base.WithInSpatialReferenceId(sRefId);

    new public GetFeaturesRequestBuilder WithOutSpatialReferenceId(int sRefId)
        => base.WithOutSpatialReferenceId(sRefId);

    new public GetFeaturesRequestBuilder WithDatumTransformation(int datumTransformationId)
        => base.WithDatumTransformation(datumTransformationId);

    public GetFeaturesRequestBuilder WithReturnZ(bool returnZ, bool ignoreIfFalse = false)
    {
        if (returnZ == false && ignoreIfFalse)
        {
            return this;
        }

        return base.WithReturnZ(returnZ);
    }

    public GetFeaturesRequestBuilder WithReturnM(bool returnM, bool ignoreIfFalse = false)
    {
        if (returnM == false && ignoreIfFalse)
        {
            return this;
        }

        return base.WithReturnM(returnM);
    }

    new public GetFeaturesRequestBuilder WithReturnGeometry(bool returnGeometry)
        => base.WithReturnGeometry(returnGeometry);

    new public GetFeaturesRequestBuilder WithReturnCountOnly(bool returnCountOnly)
        => base.WithReturnCountOnly(returnCountOnly);

    new public GetFeaturesRequestBuilder WithReturnIdsOnly(bool returnIdsOnly)
        => base.WithReturnIdsOnly(returnIdsOnly);

    new public GetFeaturesRequestBuilder WithReturnDistinctValues(bool returnDistinctValues)
        => base.WithReturnDistinctValues(returnDistinctValues);
}