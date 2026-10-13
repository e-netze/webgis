/*
 * WebGIS graphics for Leaflet
 *
 * This file contains no code. It only groups the webgis-graphics.*.js files
 * in Visual Studio (file nesting).
 *
 * The graphics are Leaflet layers used by the viewer for redlining, measuring and
 * sketch construction (dimension lines, labels, symbols, helper geometries, ...).
 * Most of them derive from L.LayerCollection, a layer composed of several sub-layers:
 *
 *   webgis-graphics.layer_collection.js             L.LayerCollection (common base class)
 *   webgis-graphics.distance_circle.js              L.DistanceCircle / L.distanceCircle
 *   webgis-graphics.compass_rose.js                 L.CompassRose / L.compassRose
 *   webgis-graphics.circle_pro.js                   L.CirclePro / L.circlePro
 *   webgis-graphics.angle_graphics.js               L.AngleGraphics / L.angleGraphics
 *   webgis-graphics.svg_text.js                     L.SVGText / L.svgText
 *   webgis-graphics.svg_label.js                    L.SVGLabel / L.svgLabel
 *   webgis-graphics.point_symbol.js                 L.PointSymbol / L.pointSymbol
 *   webgis-graphics.dim_line.js                     L.DimLine / L.dimLine
 *   webgis-graphics.dim_polygon.js                  L.DimPolygon / L.dimPolygon
 *   webgis-graphics.hecto_line.js                   L.HectoLine / L.hectoLine
 *   webgis-graphics.pass_point.js                   L.PassPoint / L.passPoint
 *   webgis-graphics.label_marker.js                 L.LabelMarker / L.labelMarker
 *   webgis-graphics.direction_line.js               L.DirectionLine / L.directionLine
 *   webgis-graphics.sketch_construction_results.js  L.SketchConstructionResults / L.sketchConstructionResults
 *   webgis-graphics.rotatable_rectangle.js          L.RotatableRectangle / L.rotatableRectangle
 *   webgis-graphics.graphics_element_series.js      L.GraphicsElementSeries / L.graphicsElementSeries
 *
 * The files are bundled (in this order) into leaflet-plugins.min.js and
 * leaflet-plugins-gesture-handling.min.js, see bundleconfig.json.
 */
