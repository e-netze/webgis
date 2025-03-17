namespace E.Standard.CMS.Core.Schema.Abstraction;

public interface IAutoCreatable : ICreatable  // Objekt ereugt die Knoten selbst (zB TableColoumnsAssistent)
{
    bool AutoCreate();
}
