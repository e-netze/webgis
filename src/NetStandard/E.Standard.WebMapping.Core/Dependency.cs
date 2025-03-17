using System.Collections.Generic;

namespace E.Standard.WebMapping.Core;

public interface IDependency
{
    void AddDependency(string dependency);
    void RemoveDependency(string dependency);
    bool HasDependency(string dependency);
}

public class Dependency : IDependency
{
    protected List<string> _dependencies = new List<string>();

    #region IDependency Member

    public void AddDependency(string dependency)
    {
        if (!_dependencies.Contains(dependency))
        {
            _dependencies.Add(dependency);
        }
    }

    public void RemoveDependency(string dependency)
    {
        _dependencies.Remove(dependency);
    }

    public bool HasDependency(string dependency)
    {
        return _dependencies.Contains(dependency);
    }

    public List<string> DependencyNames
    {
        get
        {
            return new List<string>(_dependencies);
        }
    }

    #endregion
}
