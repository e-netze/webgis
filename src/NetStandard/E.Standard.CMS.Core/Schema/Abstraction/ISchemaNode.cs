namespace E.Standard.CMS.Core.Schema.Abstraction;

public interface ISchemaNode
{
    string RelativePath { get; set; }
    CMSManager CmsManager { get; set; }
}
