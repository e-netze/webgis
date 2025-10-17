using E.Standard.WebGIS.Tools.Export.Models;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.Core.Api.UI.Setters;
using E.Standard.WebMapping.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.WebGIS.Tools.Export.Extensions;

static internal class PrintSeriesModelExtensions
{
    static public ApiEventResponse CreateResponse(
        this PrintSeriesModel model,
        MapSeriesPrint tool,
        IBridge bridge,
        ApiToolEventArguments e)
    {
        var sketch = !String.IsNullOrEmpty(model.SketchWKT) ? model.SketchWKT.ShapeFromWKT() : null;

        if (sketch is null)
        {
            throw new Exception("Uploaded file contains no valid geometry data.");
        }

        sketch.SrsId = model.SketchSrs;
        sketch.HasM = true;

        e[MapSeriesPrint.MapSeriesPrintFormatId] = model.Format;
        e[MapSeriesPrint.MapSeriesPrintLayoutId] = model.LayoutId;
        e[MapSeriesPrint.MapSeriesPrintScaleId] = ((int)model.Scale).ToString();
        e[MapSeriesPrint.MapSeriesPrintQualityId] = model.Quality.ToString();

        var response = tool.OnSelectionChanged(bridge, e);

        response.AddUIElement(new UIEmpty().WithTarget(UIElementTarget.modaldialog.ToString()));
        response.AddUISetters(
            new UISetter(MapSeriesPrint.MapSeriesPrintLayoutId, model.LayoutId),
            //new UISetter(MapSeriesPrintFormatId, model.Format),
            //new UISetter(MapSeriesPrintScaleId, ((int)model.Scale).ToString()),
            new UISetter(MapSeriesPrint.MapSeriesPrintQualityId, model.Quality.ToString()),
            new UIUpdatePersistentParametersSetter(tool));
        response.Sketch = sketch;
        response.ClientCommands = [
                ApiClientButtonCommand.zoom2sketch
            ];
        return response;
    }
}
