using MongoDB.Bson;
using MongoDB.Driver;

namespace E.Standard.WebGIS.SubscriberDatabase.Extensions;
internal static class MongoDbExtensions
{
    static public FilterDefinition<TDocument> EqId<TDocument>(this FilterDefinitionBuilder<TDocument> builder, string id) where TDocument : class
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return builder.Empty;
        }

        if (ObjectId.TryParse(id, out ObjectId objectId))
        {
            return builder.Eq("_id", objectId);
        }

        return builder.Eq("_id", id);
    }
}
