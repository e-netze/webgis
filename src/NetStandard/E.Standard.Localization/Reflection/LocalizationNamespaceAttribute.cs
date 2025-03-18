namespace E.Standard.Localization.Reflection;

[AttributeUsage(AttributeTargets.Class)]
public class LocalizationNamespaceAttribute : Attribute
{
    public LocalizationNamespaceAttribute(string @namespace)
    {
        Namespace = @namespace;
    }

    public string Namespace { get; }
}
