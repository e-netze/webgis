namespace E.Standard.CMS.Core.Schema.Abstraction;

public interface IUrlNode
{
    string Url { get; }
    bool ValidateUrl { get; }
}
