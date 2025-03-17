using E.Standard.ThreadSafe;
using E.Standard.WebMapping.Core.Abstraction;
using System;
using System.Collections.Generic;

namespace E.Standard.WebMapping.Core.Collections;

public class GraphicsContainer : ThreadSafeList<IGraphicElement>, IGraphicsContainer
{
    #region IGraphicsContainer Member

    public void Remove(Type type)
    {
        List<IGraphicElement> remove = new List<IGraphicElement>();
        foreach (IGraphicElement element in this)
        {
            if (element == null)
            {
                continue;
            }

            if (element.GetType().Equals(type))
            {
                remove.Add(element);
            }
        }

        foreach (IGraphicElement element in remove)
        {
            this.Remove(element);
        }
    }

    public bool Contains(Type type)
    {
        foreach (IGraphicElement element in this)
        {
            if (element == null)
            {
                continue;
            }

            if (element.GetType().Equals(type))
            {
                return true;
            }
        }
        return false;
    }

    public IEnumerable<IGraphicElement> GetElements(Type type)
    {
        List<IGraphicElement> elements = new List<IGraphicElement>();

        foreach (IGraphicElement element in this)
        {
            if (element == null)
            {
                continue;
            }

            if (element.GetType().Equals(type))
            {
                elements.Add(element);
            }
        }

        return elements;
    }

    #endregion
}
