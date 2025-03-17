namespace E.Standard.CMS.Core.Schema.Abstraction;

public interface IEditableWrapper
{
    IEditable WrappedObject { get; set; }
}
