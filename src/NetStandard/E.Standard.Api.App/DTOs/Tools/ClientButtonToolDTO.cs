namespace E.Standard.Api.App.DTOs.Tools;

public sealed class ClientButtonToolDTO : ToolDTO
{
    public string type { get { return "clientbutton"; } }
    public string command { get; set; }
}