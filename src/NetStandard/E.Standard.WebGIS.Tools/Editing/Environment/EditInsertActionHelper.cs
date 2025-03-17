using E.Standard.WebGIS.CMS;
using System;
using System.Collections.Generic;
using System.Xml;

namespace E.Standard.WebGIS.Tools.Editing.Environment;

class EditInsertActionHelper
{
    private readonly XmlNode _insertActionsNode;
    private readonly XmlNamespaceManager _ns;

    public EditInsertActionHelper(XmlNode themeNode, XmlNamespaceManager ns)
    {
        _insertActionsNode = themeNode.SelectSingleNode("edit:insertactions", ns);
        _ns = ns;
    }

    public bool HasDefaultSaveButton
    {
        get
        {
            return HasDefaultButton(EditingInsertAction.Save);
        }
    }

    public bool HasDefaultSaveAndSelectButton
    {
        get
        {
            return HasDefaultButton(EditingInsertAction.SaveAndSelect);
        }
    }

    public bool HasDefaultButton(EditingInsertAction insertAction)
    {
        if (_insertActionsNode == null)
        {
            return true;
        }

        return _insertActionsNode.SelectSingleNode($"edit:insertaction[@is_default='true' and @action='{insertAction}']", _ns) != null;
    }

    public IEnumerable<(EditingInsertAction action, string text)> GetCustomInsertActions()
    {
        List<(EditingInsertAction, string)> actions = new List<(EditingInsertAction, string)>();

        if (_insertActionsNode != null)
        {
            foreach (XmlNode insertActionNode in _insertActionsNode.SelectNodes("edit:insertaction[@action and @action_text]", _ns))
            {
                if (insertActionNode.Attributes["is_default"]?.Value == "true")
                {
                    continue;
                }

                actions.Add(((EditingInsertAction)Enum.Parse(typeof(EditingInsertAction), insertActionNode.Attributes["action"].Value, true), insertActionNode.Attributes["action_text"].Value));
            }
        }

        return actions;
    }

    public string ServerCommand(EditingInsertAction insertAction)
    {
        switch (insertAction)
        {
            case EditingInsertAction.Save:
                return "save";
            case EditingInsertAction.SaveAndSelect:
                return "saveandselect";
            case EditingInsertAction.SaveAndKeepAllAttributes:
                return "save-and-keep-attributes";
            case EditingInsertAction.SaveAndContinueAtLatestestSketchVertex:
                return "save-and-continue-sketch";
            case EditingInsertAction.SaveAndContinueAtLatestestSketchVertexAndKeepAllAttributes:
                return "save-and-continue-sketch-keep-attributes";
        }

        return string.Empty;
    }

    public bool AutoExplodeMultipartFeatures
    {
        get
        {
            if (_insertActionsNode == null)
            {
                return false;
            }

            var autoExplodeMultipartFeatureValue = _insertActionsNode?.Attributes["auto_explode_multipart_features"]?.Value;

            if (!String.IsNullOrEmpty(autoExplodeMultipartFeatureValue))
            {
                return bool.Parse(autoExplodeMultipartFeatureValue);
            }

            return false;
        }
    }
}
