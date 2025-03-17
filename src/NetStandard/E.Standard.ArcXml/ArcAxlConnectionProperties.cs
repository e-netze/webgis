namespace E.Standard.ArcXml;

public class ArcAxlConnectionProperties
{
    public bool CheckUmlaut { get; set; }
    public string AuthUsername { get; set; } = "";
    public string AuthPassword { get; set; } = "";

    public string Token { get; set; } = "";

    public int Timeout { get; set; }

    //public string OutputPath { get; set; }
    //public string OutputUrl { get; set; }
}
