using E.Standard.Extensions.Collections;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using Microsoft.AspNetCore.Mvc.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
using static E.Standard.WebMapping.Core.Api.UI.Elements.UICollapsableElement;

namespace E.Standard.WebMapping.Core.Api.Extensions;

static public class UIElementExtensions
{
    #region Add Child Elements

    static public T AddChild<T>(this T element, IUIElement newElement)
        where T : IUIElement
    {
        if (element.elements == null)
        {
            element.elements = new List<IUIElement>();
        }

        element.elements.Add(newElement);

        return element;
    }

    static public T RemoveChild<T>(this T element, IUIElement removeElement)
         where T : IUIElement
    {
        if (element.elements != null)
        {
            element.elements.Remove(removeElement);
        }

        return element;
    }

    static public T InsertChild<T>(this T element, int index, IUIElement newElement)
         where T : IUIElement
    {
        if (element.elements == null)
        {
            element.elements = new List<IUIElement>();
        }
        else if (!(element.elements is List<IUIElement>))
        {
            element.elements = new List<IUIElement>(element.elements);
        }

        if (index > element.elements.Count() - 1)
        {
            index = element.elements.Count() - 1;
        }

        if (index < 0)
        {
            index = 0;
        }

        ((List<IUIElement>)element.elements).Insert(index, newElement);

        return element;
    }

    static public T InsertChildBefore<T>(this T element, IUIElement insertBefore, IUIElement newElement)
         where T : IUIElement
    {
        if (element.elements == null)
        {
            element.elements = new List<IUIElement>();
        }
        else if (!(element.elements is List<IUIElement>))
        {
            element.elements = new List<IUIElement>(element.elements);
        }

        int index = ((List<IUIElement>)element.elements).IndexOf(insertBefore);
        if (index < 0)
        {
            element.elements.Add(newElement);
        }
        else
        {
            ((List<IUIElement>)element.elements).Insert(Math.Max(0, index), newElement);
        }

        return element;
    }

    static public T AddChildren<T>(this T element, params IUIElement[] elements)
         where T : IUIElement
           => element.AddChildren((IEnumerable<IUIElement>)elements);

    static public T AddChildren<T>(this T element, IEnumerable<IUIElement> elements)
         where T : IUIElement
    {
        if (element.elements == null)
        {
            element.elements = new List<IUIElement>();
        }
        else if (!(element.elements is List<IUIElement>))
        {
            element.elements = new List<IUIElement>(element.elements);
        }

        ((List<IUIElement>)element.elements).AddRange(elements);

        return element;
    }

    #endregion

    #region Find Child Elements

    static public IEnumerable<T> FindChildrenWithType<T>(this IUIElement element) where T : IUIElement
    {
        if (element.elements == null)
        {
            return Array.Empty<T>();
        }

        var elementsOfT = new List<IUIElement>();
        var type = typeof(T);

        foreach (var childElement in element.elements)
        {
            FindRecursive(childElement, type, elementsOfT);
        }

        return elementsOfT.ToArray().Select(f => (T)f);
    }

    #region Helper

    static private void FindRecursive(IUIElement element, Type type, List<IUIElement> elementsOfType)
    {
        if (element == null)
        {
            return;
        }

        if (element.GetType() == type)
        {
            elementsOfType.Add(element);
            return;
        }

        foreach (var childElement in element.elements.OrEmptyArray())
        {
            FindRecursive(childElement, type, elementsOfType);
        }
    }

    #endregion

    static public IEnumerable<IUIElement> FindChildrenWhere(this IUIElement element, Func<IUIElement, bool> predicate)
    {
        if (element?.elements == null)
        {
            return Array.Empty<IUIElement>();
        }

        return element.elements.Where(e => predicate(e));
    }

    static public int CountChildrenWhere(this IUIElement element, Func<IUIElement, bool> predicate)
        => element.FindChildrenWhere(predicate).Count();

    static public bool HasChildrenWhere(this IUIElement element, Func<IUIElement, bool> predicate)
        => element.CountChildrenWhere(predicate) > 0;

    static public IEnumerable<UISelect.Option> FindOptionsWhere(this UISelect element, Func<UISelect.Option, bool> predicate)
    {
        if (element?.options == null)
        {
            return Array.Empty<UISelect.Option>();
        }

        return element.options.Where(o => predicate(o));
    }

    static public int CountOptionsWhere(this UISelect element, Func<UISelect.Option, bool> predicate)
        => element.FindOptionsWhere(predicate).Count();

    static public bool HasOptionsWhere(this UISelect element, Func<UISelect.Option, bool> predicate)
        => element.CountOptionsWhere(predicate) > 0;

    #endregion

    #region SetProperties

    static public T WithId<T>(this T element, string id)
        where T : IUIElement
    {
        if (element != null)
        {
            element.id = id;
        }
        return element;
    }

    static public T WithValue<T>(this T element, object value)
        where T : IUIElement
    {
        if (element != null)
        {
            element.value = value;
        }
        return element;
    }

    static public T WithStyles<T>(this T element, params string[] styles)
        where T : IUIElement
    {
        if (element != null && styles != null && styles.Length > 0)
        {
            element.css = UICss.ToClass(element.css, styles);
        }
        return element;
    }

    static public T AsToolParameter<T>(this T element, params string[] styles)
        where T : IUIElement
    {
        if (element != null)
        {
            element.css = UICss.ToClass(UICss.ToClass(UICss.ToolParameter, element.css), styles);
        }

        return element;
    }

    static public T AsPersistantToolParameter<T>(this T element, params string[] styles)
        where T : IUIElement
    {
        if (element != null)
        {
            var toolParameterClass = UICss.ToClass(UICss.ToolParameter, UICss.ToolParameterPersistent);
            element.css = UICss.ToClass(UICss.ToClass(toolParameterClass, element.css), styles);
        }

        return element;
    }

    static public T WithTarget<T>(this T element, UIElementTarget target)
        where T : IUIElement
    {
        return element.WithTarget(target.ToString());
    }

    static public T AsDialog<T>(this T element, UIElementTarget target = UIElementTarget.modaldialog)
        where T : IUIElement
        => element.WithTarget(target);

    static public T WithTarget<T>(this T element, string target)
        where T : IUIElement
    {
        if (element != null)
        {
            element.target = target;
        }
        return element;
    }

    static public T WithSize<T>(this T element, int width = 0, int height = 0)
        where T : UIElement
    {
        if(width > 0)
        {
            element.targetwidth = $"{width}px";
        }
        if(height > 0)
        {
            element.targetheight = $"{height}px";
        }
        return element;
    }

    static public T WithTargetTitle<T>(this T element, string tragetTitle)
        where T : IUIElement
    {
        if (element != null)
        {
            element.targettitle = tragetTitle;
        }
        return element;
    }

    static public T WithTargetOnClose<T>(this T element, string onCloseCommand, string onCloseCommandType = CommandType.ClientButton)
        where T : IUIElement
    {
        if (element != null)
        {
            element.targetonclosetype = onCloseCommandType;
            element.targetonclosecommand = onCloseCommand;
        }
        return element;
    }

    static public T WithDialogTitle<T>(this T element, string dialogTitle)
        where T : IUIElement
        => element.WithTargetTitle(dialogTitle);

    static public T WithVisibilityDependency<T>(this T element, VisibilityDependency visDependendency)
        where T : IUIElement
    {
        if (element != null)
        {
            element.VisibilityDependency = visDependendency;
        }
        return element;
    }

    static public T WithParameterForServerCommands<T>(this T element, params string[] parameterServerCommands)
        where T : IUIElement
    {
        if (element != null)
        {
            element.ParameterServerCommands = parameterServerCommands;
        }
        return element;
    }

    static public T WithText<T>(this T element, string text)
        where T : IUIElementText
    {
        if (element != null)
        {
            element.text = text;
        }
        return element;
    }

    static public T WithIcon<T>(this T element, string icon)
        where T : IUIElementIcon
    {
        if (element != null)
        {
            element.icon = icon;
        }
        return element;
    }

    static public T WithLabel<T>(this T element, string label)
        where T : IUIElementLabel
    {
        if (element != null)
        {
            element.label = label;
        }
        return element;
    }

    #region IUIInputField

    static public T WithLabelText<T>(this T element, string label)
        where T : IUIInputField
    {
        if (element != null && element.UILabel != null)
        {
            element.UILabel.label = label;
        }

        return element;
    }

    static public T WithInputStyles<T>(this T element, params string[] styles)
        where T : IUIInputField
    {
        if (element != null && element.Input != null && styles != null && styles.Length > 0)
        {
            element.Input.css = UICss.ToClass(element.Input.css, styles);
        }
        return element;
    }

    #endregion

    static public T WithLiteral<T>(this T element, string literal, bool asMarkdown = false)
        where T : IUIElementLiteral
    {
        if (element != null)
        {
            element.literal = literal;
            element.as_markdown = asMarkdown;
        }

        return element;
    }

    static public T AsReadonly<T>(this T element, bool readOnly = true)
        where T : IUIElementReadonly
    {
        if (element != null)
        {
            element.@readonly = readOnly;
        }

        return element;
    }

    static public T WithExpandBehavior<T>(this T element, ExpandBehaviorMode expandBehavior)
        where T : UICollapsableElement
    {
        if (element != null)
        {
            element.ExpandBehavior = expandBehavior;
        }

        return element;
    }

    static public T WithText2<T>(this T element, string text2)
        where T : UIMenuItem
    {
        if (element != null)
        {
            element.text2 = text2;
        }

        return element;
    }

    static public T WithSubText<T>(this T element, string subText)
        where T : UIMenuItem
    {
        if (element != null)
        {
            element.subtext = subText;
        }

        return element;
    }

    static public T AsRemovable<T>(this T element, bool removable = true)
        where T : UIMenuItem
    {
        if (element != null)
        {
            element.removable = removable;
        }

        return element;
    }

    static public T WithHeader<T>(this T element, string header)
        where T : UIMenu
    {
        if (element != null)
        {
            element.header = header;
        }

        return element;
    }

    #endregion
}
