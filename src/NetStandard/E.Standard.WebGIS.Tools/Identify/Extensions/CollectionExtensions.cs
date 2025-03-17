using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebGIS.Tools.Identify.Extensions;

static internal class CollectionExtensions
{
    static public string AddFavoritesCategory(this Dictionary<string, UICollapsableElement> categoryElements,
                                              IApiTool tool,
                                              ApiToolEventArguments e,
                                              string queryValue = null)
    {
        string id = e.FavoritesCategoryId();

        categoryElements.Add(id, new UICollapsableElement("div")
        {
            id = id,
            title = "Favoriten",
            CollapseState = UICollapsableElement.CollapseStatus.Collapsed,
            ExpandBehavior = UICollapsableElement.ExpandBehaviorMode.Exclusive
        });

        if (!String.IsNullOrEmpty(queryValue))
        {
            categoryElements.Values.Last().AddQueryMenuItem(tool, e, queryValue);
        }

        return id;
    }

    static public string AddVisibleCategory(this Dictionary<string, UICollapsableElement> categoryElements,
                                            IApiTool tool,
                                            ApiToolEventArguments e,
                                            string queryValue = null)
    {
        string id = e.VisibleCategoryId();

        categoryElements.Add(id, new UICollapsableElement("div")
        {
            id = id,
            title = "Sichtbare Themen",
            CollapseState = UICollapsableElement.CollapseStatus.Collapsed,
            ExpandBehavior = UICollapsableElement.ExpandBehaviorMode.Exclusive
        });

        if (!String.IsNullOrEmpty(queryValue))
        {
            categoryElements.Values.Last().AddQueryMenuItem(tool, e, queryValue);
        }

        return id;
    }

    static public string AddInVisibleCategory(this Dictionary<string, UICollapsableElement> categoryElements,
                                              IApiTool tool,
                                              ApiToolEventArguments e,
                                              string queryValue = null)
    {
        string id = e.InVisbileCategoryId();

        categoryElements.Add(id, new UICollapsableElement("div")
        {
            id = id,
            title = "Nicht sichtbare Themen",
            CollapseState = UICollapsableElement.CollapseStatus.Collapsed,
            ExpandBehavior = UICollapsableElement.ExpandBehaviorMode.Exclusive
        });

        if (!String.IsNullOrEmpty(queryValue))
        {
            categoryElements.Values.Last().AddQueryMenuItem(tool, e, queryValue);
        }

        return id;
    }

    static public string AddAllCategory(this Dictionary<string, UICollapsableElement> categoryElements,
                                        IApiTool tool,
                                        ApiToolEventArguments e,
                                        bool hasExcludedQueries,
                                        string queryValue = null)
    {
        string id = e.AllCategoryId();

        categoryElements.Add(id, new UICollapsableElement("div")
        {
            id = id,
            title = hasExcludedQueries ? "Weitere Themem" : "Alle Themen",
            CollapseState = UICollapsableElement.CollapseStatus.Collapsed,
            ExpandBehavior = UICollapsableElement.ExpandBehaviorMode.Exclusive
        });

        if (!String.IsNullOrEmpty(queryValue))
        {
            categoryElements.Values.Last().AddQueryMenuItem(tool, e, queryValue);
        }

        return id;
    }

    static public string AddIdentifyToolCategory(this Dictionary<string, UICollapsableElement> categoryElements,
                                                  IEnumerable<IdentifyToolQuery> identifyToolQueries,
                                                  IBridge bridge,
                                                  IApiTool tool,
                                                  ApiToolEventArguments e)
    {
        string id = e.IdentifyToolsCategoryId();

        if (identifyToolQueries.Count() > 0)
        {
            var menuItems = new List<UIMenuItem>();

            foreach (var identifyToolQuery in identifyToolQueries)
            {
                menuItems.Add(new UIMenuItem(tool, e)
                {
                    text = $"{identifyToolQuery.Name}&nbsp;{(identifyToolQuery.CountResults > 0 ? "&nbsp;[" + identifyToolQuery.CountResults + "]" : "")}",
                    value = (identifyToolQuery).Url,
                    icon = !String.IsNullOrWhiteSpace(identifyToolQuery.Image) ? $"{bridge.AppRootUrl}/{identifyToolQuery.Image}" : null
                });
            }

            var resultElement = new UIMenu()
            {
                elements = menuItems.ToArray()
            };

            categoryElements.Add(id, new UICollapsableElement("div")
            {
                id = id,
                title = "Sonstige",
                elements = new IUIElement[] { resultElement },
                CollapseState = UICollapsableElement.CollapseStatus.Collapsed,
                ExpandBehavior = UICollapsableElement.ExpandBehaviorMode.Exclusive
            });
        }

        return id;
    }

    static private void AddQueryMenuItem(this UICollapsableElement categoryElement,
                                         IApiTool tool,
                                         ApiToolEventArguments e,
                                         string queryValue)
    {
        categoryElement.elements = new IUIElement[]
        {
            new UIMenu()
            {
                elements=new IUIElement[]
                {
                    new UIMenuItem(tool, e)
                    {
                        css = UICss.ToClass(new[] { UICss.CollapsableAutoClick }),
                        text = "Abfragen...",
                        value = queryValue
                    }
                }
            }
        };
    }


    static public ICollection<IUIElement> ToUIElements(this Dictionary<string, UICollapsableElement> categoryElements,
                                                       IEnumerable<IUIElement> menuItems,
                                                       string targetCategoryId,
                                                       string targetElementId = "",
                                                       string targetElementTitle = null)
    {
        string uiTarget = $"#{targetCategoryId}";

        if (categoryElements.ContainsKey(targetCategoryId))
        {
            uiTarget = targetElementId;
        }

        IUIElement resultElement = null;

        if (menuItems.Count() == 0)
        {
            resultElement = new UILiteralWarning("Keine Abfrageergebnisse gefunden");
        }
        else
        {
            resultElement = new UIMenu()
            {
                elements = menuItems.ToArray()
            };
        }

        if (String.IsNullOrEmpty(targetCategoryId) || !categoryElements.ContainsKey(targetCategoryId))
        {
            resultElement.target = uiTarget;

            return new IUIElement[] { resultElement };
        }

        categoryElements[targetCategoryId].elements = new IUIElement[] { resultElement };
        categoryElements[targetCategoryId].CollapseState = UICollapsableElement.CollapseStatus.Expanded;

        return new UIElement[]{
            new UIDiv()
            {
                style="max-width:320px",
                target = uiTarget,
                targettitle = targetElementTitle,
                elements = categoryElements.Values.ToArray()
            }
        };
    }
}
