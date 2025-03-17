namespace E.Standard.Api.App.DTOs.Tools;

public sealed class ToolConfirmMessageDTO
{
    public string message { get; set; }

    public string command { get; set; }
    public string type { get; set; }
    public string eventtype { get; set; }
}