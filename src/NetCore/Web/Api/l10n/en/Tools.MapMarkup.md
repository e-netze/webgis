# name: Drawing (Map-Markup)

Simple drawing on the map.

# container: Tools

# tools:
## select: Select
## symbol: Symbol
## text: Text
## point: Point
## freehand: Freehand
## line: Line
## polygon: Polygon
## rectangle: Rectangle
## circle: Circle
## distance_circle: Distance Circle
## compass_rose: Compass Rose
## dimline: Dimensioning
## hectoline: Hectometer Line

## save: Save Drawing
## open: Load Drawing

## share: Share
## upload: Upload (GPX, ...)
## download: Download (GPX, Shape)

# symbology:
## set: Set Symbology

## symbol: Symbol

## point-color: Point color
## point-size: Point size

## font-size: Font size
## font-style: Font style
## font-color: Font color

## line-color: Line color
## line-weight: Line weight
## line-style: Line style

## fill-color: Fill color
## fill-opacity: Fill opacity

## properties: Properties
## num-circles: Number of circles
## radius: Radius (m)
## apply-radius: Apply radius
## num-angle-segments: Number of angle segments
## unit: Unit
## segment-unit: Distance [Unit]
## apply-segment: Apply distance

# draw-symbol: Set symbol
# draw-point: Set point
# draw-text: Set text
# draw-freehand: Draw freehand
# draw-line: Draw line
# draw-polygon: Draw polygon
# draw-distance-circle: Draw distance circle
# draw-compass-rose: Draw compass rose
# draw-dimline: Draw dimensioning
# draw-hectoline: Draw hectometer line

# apply-line: Apply line
# apply-polygon: Apply polygon
# apply-symbol: Apply symbol

# text:
## symbols-from-selection: Symbols from selection {0}
## points-from-selection: Points from selection {0}
## text-from-selection: Text from selection {0}
## line-from-selection: Lines from selection {0}
## polygon-from-selection: Polygons from selection {0}

# selection: Selection
## take-from: Take from selection
## symbology: Symbology
## label1:

The selected objects from {0} can be transferred to the Map-Markup tool.
|The representation (colors) will be taken from the current Map-Markup settings and 
can be changed individually for each object later.
|To better distinguish the Map-Markup elements, they can be identified later using the field specified here:

## exception-to-many-objects: A selection of geo-objects with more than {0} vertices cannot be transferred to Map-Markup!

# io:
## exception-invalid-char: Invalid character in the name. Avoid the following characters: {0}
## exception-no-projects-found: No Map-Markup projects have been saved under your user yet. Save a Map-Markup project before using this tool.
## confirm-delete-project: Should the Map-Markup project '{mapmarkup-io-load-name}' really be deleted?

## extend-current-session: Extend existing Map-Markup
## replace-current-session: Replace existing Map-Markup

## upload-label1:

Map-Markup objects can be uploaded here. Valid file extensions are *.gpx

## download-label1:

Map-Markup objects can be downloaded here.

## download-label-gpx:

For GPX, only the drawn lines are exported as 'Tracks' and the texts or symbols as 'Waypoints'.

## download-label-shape:

For ESRI Shape files, the target projection must also be specified.
A Shapefile is created for each geometry type and packed into a ZIP file.

## download-label-json:

In Map-Markup projects, all objects (plus representation) are downloaded 
as GeoJSON and can be uploaded again later.
