using E.Standard.ThreadSafe;
using E.Standard.WebMapping.Core.Abstraction;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Collections;

public class SelectionCollection : ThreadSafeList<Selection>, IClone<SelectionCollection, IMap>
{
    private readonly IMap _map;

    public SelectionCollection()
    {
        _map = null;
    }
    public SelectionCollection(IMap map)
    {
        _map = map;
    }

    public string[] Remove(string name)
    {
        return Remove(name, true);
    }
    public string[] Remove(string name, bool checkDependency)
    {
        List<string> removed = new List<string>();
        foreach (Selection selection in this)
        {
            if (selection.Name == name)
            {
                this.Remove(selection);
                removed.Add(selection.Name);
                break;
            }
        }
        return removed.ToArray();
    }
    public string[] RemoveStartsWith(string name)
    {
        List<string> removed = new List<string>();
        foreach (Selection selection in this)
        {
            if (selection.Name.StartsWith(name))
            {
                this.Remove(selection);
                removed.Add(selection.Name);
            }
        }
        return removed.ToArray();
    }

    new public void Add(Selection selection)
    {
        if (selection == null)
        {
            return;
        }

        Remove(selection.Name, false);
        base.Add(selection);
    }

    public Selection this[string name]
    {
        get
        {
            foreach (Selection selection in this)
            {
                if (selection != null &&
                    selection.Name == name)
                {
                    return selection;
                }
            }
            return null;
        }
    }

    public bool IsDirty
    {
        get
        {
            foreach (Selection selection in this)
            {
                if (selection.IsDirty)
                {
                    return true;
                }
            }
            return false;
        }
        set
        {
            foreach (Selection selection in this)
            {
                selection.IsDirty = value;
            }
        }
    }

    public bool HasSelection(string name)
    {
        return this[name] != null;
    }

    #region IClone Member

    public SelectionCollection Clone(IMap parent)
    {
        if (parent is null)
        {
            return null;
        }

        SelectionCollection clone = new SelectionCollection(parent);

        foreach (Selection selection in this)
        {
            if (selection == null)
            {
                continue;
            }

            clone.Add(selection.Clone(parent));
        }

        #region Bei Bufferselection auch den Targetlayer auf neune Parent Karte umlenken...
        foreach (Selection cloneSelection in clone)
        {
            if (cloneSelection != null &&
                cloneSelection.Filter != null &&
                cloneSelection.Filter.Buffer != null &&
                cloneSelection.Filter.Buffer.TargetLayer != null)
            {
                cloneSelection.Filter.Buffer.TargetLayer = parent.LayerById(cloneSelection.Filter.Buffer.TargetLayer.GlobalID);
            }
        }
        #endregion

        return clone;
    }

    #endregion
}
