namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest;

class ParentLayer
{
    public int? Id { get; set; }
    public string Name { get; set; }
    public ParentLayer ParentParentLayer { get; set; }
}
