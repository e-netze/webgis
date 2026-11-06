using System;

namespace E.Standard.WebGIS.Core.Reflection;

/// <summary>
/// Marks a server tool command that may only be executed by a map author of the portal page
/// the tool is writing to (ApiPortalPageDTO.MapAuthors).
/// The portal page is determined by IStorageInteractions.StoragePathFormatParameter(bridge, 0) of the tool.
/// The check is enforced by the API before the command is invoked.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class PortalMapAuthorRequiredAttribute : Attribute
{
}
