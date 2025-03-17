using E.Standard.WebGIS.Tools.Editing.Environment;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.Reflection;
using System;

namespace E.Standard.WebGIS.Tools.Editing.Models;

public class EditThemeDefinition
{
    [ArgumentObjectProperty(0)]
    public string ServiceId { get; set; }

    [ArgumentObjectProperty(1)]
    public string LayerId { get; set; }

    [ArgumentObjectProperty(2)]
    public string EditThemeId { get; set; }

    [ArgumentObjectProperty(3)]
    public string EditThemeName { get; set; }

    internal void Init(IBridge bridge)
    {
        if (String.IsNullOrWhiteSpace(this.EditThemeName))  // From CMS EditTheme
        {
            var editTheme = bridge.GetEditTheme(this.ServiceId, this.EditThemeId);
            if (editTheme != null)
            {
                this.EditThemeName = editTheme.Name;
            }
        }
        if (String.IsNullOrWhiteSpace(this.EditThemeName))  // From XML Edit Mask (sollte eigentlich nie 
        {
            EditEnvironment editEnvironment = new EditEnvironment(bridge, this);
            var theme = editEnvironment[this.EditThemeId];
            if (theme != null)
            {
                this.EditThemeName = theme.Name;
            }
        }
    }

    public override bool Equals(object obj)
    {
        if (obj is EditThemeDefinition)
        {
            var canditate = (EditFeatureDefinition)obj;

            return canditate.ServiceId == this.ServiceId &&
                   canditate.LayerId == this.LayerId &&
                   canditate.EditThemeId == this.EditThemeId;
        }
        else if (obj is EditFeatureDefinition)
        {
            var canditate = (EditFeatureDefinition)obj;

            return canditate.ServiceId == this.ServiceId &&
                   canditate.LayerId == this.LayerId &&
                   canditate.EditThemeId == this.EditThemeId;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
