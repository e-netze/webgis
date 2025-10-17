using E.Standard.Localization.Abstractions;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Geometry.Extensions;
using System;

namespace E.Standard.WebGIS.Tools.Export.Extensions;

static internal class ApiToolEventArgumentsExtensions
{
    static public void ValidateSketch(this ApiToolEventArguments e, ILocalizer localizer)
    {
        if (e?.Sketch?.HasPoints() != true)
        {
            throw new Exception(localizer.Localize("io.exception-no-sketch-defined:body"));
        }
    }
}
