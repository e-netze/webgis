using System;
using System.Text.RegularExpressions;

namespace E.Standard.WebMapping.Core.Api.Reflection;


[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class ToolStorageIdAttribute : System.Attribute
{
    public ToolStorageIdAttribute(string toolStorageId)
    {
        _toolStorageId = toolStorageId.ToLower().Replace(@"\", "/");
    }

    private string _toolStorageId;

    public string ToolStorageId
    {
        get
        {
            var toolStorageId = _toolStorageId;
            if (CoreApiGlobals.StorageToolIdTranslation != null && CoreApiGlobals.StorageToolIdTranslation.Count > 0)
            {
                foreach (var key in CoreApiGlobals.StorageToolIdTranslation.Keys)
                {
                    if (String.IsNullOrWhiteSpace(CoreApiGlobals.StorageToolIdTranslation[key]))
                    {
                        continue;
                    }

                    if (toolStorageId.StartsWith(key, StringComparison.InvariantCultureIgnoreCase))
                    {
                        toolStorageId = Regex.Replace(toolStorageId, key, CoreApiGlobals.StorageToolIdTranslation[key], RegexOptions.IgnoreCase);
                    }
                }
            }
            return toolStorageId;
        }
    }
}

public class ToolPolicyAttribute : System.Attribute
{
    public ToolPolicyAttribute() { }
    public bool RequireAuthentication { get; set; }
}
