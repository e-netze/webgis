namespace E.Standard.Api.App.DTOs.Tools;

public sealed class ServerToolDTO : ToolDTO
{
    public string type { get { return "servertool"; } }
    public string tooltype { get; set; }
}