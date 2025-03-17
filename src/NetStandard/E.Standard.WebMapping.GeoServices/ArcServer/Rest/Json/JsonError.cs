namespace E.Standard.WebMapping.GeoServices.ArcServer.Rest.Json;

public class JsonError
{
    public Error error { get; set; }

    public Result[] addResults { get; set; }
    public Result[] updateResults { get; set; }
    public Result[] deleteResults { get; set; }

    #region Classes

    public class Error
    {
        public int code { get; set; }
        public string message { get; set; }
        public object details { get; set; }
        public string description { get; set; }
    }

    public class Result
    {
        public Error error { get; set; }
    }

    #endregion
}
