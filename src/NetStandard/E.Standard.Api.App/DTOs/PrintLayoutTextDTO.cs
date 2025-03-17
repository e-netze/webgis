using E.Standard.WebMapping.Core.Api.Bridge;

namespace E.Standard.Api.App.DTOs;

public sealed class PrintLayoutTextDTO : IPrintLayoutTextBridge
{
    public string AliasName { get; set; }

    public string Default { get; set; }

    public int MaxLength { get; set; }

    public string Name { get; set; }

    public bool Visible { get; set; }
}