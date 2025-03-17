using E.Standard.WebGIS.CMS;
using System;
using System.Text;

namespace E.Standard.Api.App.Extensions;

static public class EnumExtensions
{
    static public string ToDbRightsString(this EditingRights editingRights)
    {
        StringBuilder sb = new StringBuilder(5);

        if (editingRights.HasFlag(EditingRights.Insert))
        {
            sb.Append("i");
        }

        if (editingRights.HasFlag(EditingRights.Update))
        {
            sb.Append("u");
        }

        if (editingRights.HasFlag(EditingRights.Delete))
        {
            sb.Append("d");
        }

        if (editingRights.HasFlag(EditingRights.Geometry))
        {
            sb.Append("g");

            if (editingRights.HasFlag(EditingRights.MultipartGeometries))
            {
                sb.Append("+");
            }
        }

        if (editingRights.HasFlag(EditingRights.MassAttributeable))
        {
            sb.Append("m");
        }

        return sb.ToString();
    }

    static public EditingRights ToEditingRights(this string dbRights)
    {
        EditingRights editingRights = EditingRights.Unknown;

        if (!String.IsNullOrEmpty(dbRights))
        {
            if (dbRights.Contains("i"))
            {
                editingRights |= EditingRights.Insert;
            }

            if (dbRights.Contains("u"))
            {
                editingRights |= EditingRights.Update;
            }

            if (dbRights.Contains("d"))
            {
                editingRights |= EditingRights.Delete;
            }

            if (dbRights.Contains("g"))
            {
                editingRights |= EditingRights.Geometry;
            }

            if (dbRights.Contains("m"))
            {
                editingRights |= EditingRights.MassAttributeable;
            }
        }

        return editingRights;
    }

    static public bool IsAllowed(this AppRoles appRole, AppRoles candidate)
    {
        return appRole.HasFlag(candidate) || appRole.HasFlag(AppRoles.All);
    }
}
