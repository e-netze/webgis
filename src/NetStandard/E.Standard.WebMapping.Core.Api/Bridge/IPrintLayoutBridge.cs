namespace E.Standard.WebMapping.Core.Api.Bridge;

public interface IPrintLayoutBridge : IApiObjectBridge
{
    string Id { get; }

    string Name { get; }
}

public interface IPrintLayoutTextBridge : IApiObjectBridge
{
    string Name { get; }
    string AliasName { get; }
    string Default { get; }
    int MaxLength { get; }
    bool Visible { get; }
}
