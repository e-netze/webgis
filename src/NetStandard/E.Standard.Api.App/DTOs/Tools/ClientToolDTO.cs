namespace E.Standard.Api.App.DTOs.Tools;

public sealed class ClientToolDTO : ToolDTO
{
    public string type { get { return "clienttool"; } }
    public string tooltype { get; set; }
}