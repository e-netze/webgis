using E.Standard.CMS.Core;
using E.Standard.CMS.Core.UI.Abstraction;
using System;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.CmsSchema.TypeEditor;

public interface ITypeEditor
{
    object ResultValue { get; }
}

public interface IUITypeEditor : ITypeEditor
{
    IUIControl GetUIControl(ITypeEditorContext context);
}

public interface IUITypeEditorAsync : IUITypeEditor
{
    Task<IUIControl> GetUIControlAsync(ITypeEditorContext context);
}

public interface IEnumTypeEditor : ITypeEditor
{
}

public interface ITypeEditorContext
{
    CMSManager CmsManager { get; }
    string RelativePath { get; }
    object Instance { get; }
    object Value { get; }
    Type ValueType { get; }
}

public class TypeEditorContext : ITypeEditorContext
{
    public TypeEditorContext(CMSManager cmsManager, string relativePath, object instance, string propertyName)
    {
        this.CmsManager = cmsManager;
        this.RelativePath = relativePath;
        this.Instance = instance;

        if (this.Instance != null)
        {
            var propInfo = this.Instance.GetType().GetProperty(propertyName);
            this.Value = propInfo?.GetValue(this.Instance);
            this.ValueType = propInfo?.PropertyType ?? typeof(object);
        }
    }
    public CMSManager CmsManager { get; private set; }

    public string RelativePath { get; private set; }

    public object Instance { get; private set; }

    public object Value { get; private set; }

    public Type ValueType { get; private set; }
}
