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
## dimpolygon: Dimensioning Polygon
## hectoline: Hectometer Line
## view_angle: Field of View (Angle)
## intersection_angle: Intersection Angle
## circle_arc: Circular Arc
## perpendicular: Perpendicular distance
## buffer_zone: Buffer zone
## object_distance: Object distance

## save: Save Drawing
## open: Load Drawing

## share: Share
## upload: Upload (GPX, ...)
## download: Download (GPX, Shape)

# toolgroups:
## draw: Draw
## measure: Measure
## io: Import/Export

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
## area-unit: Area unit
## label-edges: Label edges
## label-total-length: Label total length
## angle: Opening angle
## apply-angle: Apply angle
## angle-unit-deg: Degrees
## angle-unit-gon: Gon
## angle-unit-both: Degrees and gon
## angle-unit-none: None
## angle-type: Angle type
## angle-type-inner: Interior angle
## angle-type-deflection: Deflection angle (L/R)
## angle-type-azimuth: Azimuth
## label-axis-length: Label axis length
## show-angles: Show angles
## show-angles-intersection: Intersection angle
## show-angles-adjacent: Adjacent angle
## show-angles-both: Both
## show-angles-all: All
## label-line-length: Label line length
## length-unit: Length unit
## station-format: Station
## station-format-m: Meters (156.24m)
## station-format-km: Chainage (0+156.24)
## station-format-none: None
## buffer-source-type: Source type
## buffer-source-point: Point
## buffer-source-multipoint: Multiple points
## buffer-source-line: Line
## buffer-source-polygon: Polygon
## buffer-width: Buffer width (polygons: also negative)
## apply-buffer-width: Apply buffer width
## show-labels: Labels
## show-labels-all: All
## show-labels-radius: Radius
## show-labels-arc-length: Arc length
## show-labels-central-angle: Central angle
## show-number: Show number

# draw-symbol: Set symbol
# draw-point: Set point
# draw-text: Set text
# draw-freehand: Draw freehand
# draw-line: Draw line
# draw-polygon: Draw polygon
# draw-distance-circle: Draw distance circle
# draw-compass-rose: Draw compass rose
# draw-dimline: Draw dimensioning
# draw-dimpolygon: Draw dimensioning polygon
# draw-hectoline: Draw hectometer line
# draw-view-angle: Draw field of view (angle)
# draw-intersection-angle: Draw intersection angle
# draw-circle-arc: Draw circular arc
# draw-perpendicular: Draw perpendicular distance
# draw-buffer-zone: Draw buffer zone
# draw-object-distance: Measure object distance (click objects in the map)

# apply-line: Apply line
# finish-reference-line: Finish reference line
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

## exception-to-many-objects: A maximum of {0} objects can be transferred to Map-Markup
## exception-to-many-vertices: A selection of geo-objects with more than {0} vertices cannot be transferred to Map-Markup!

# io:
## exception-invalid-char: Invalid character in the name. Avoid the following characters: {0}
## exception-no-projects-found: No Map-Markup projects have been saved under your user yet. Save a Map-Markup project before using this tool.
## confirm-delete-project: Should the Map-Markup project '{mapmarkup-io-load-name}' really be deleted?
## exception-name-is-empty: Empty name is not allowed
## exception-name-to-long: Name to long. Maximum {0} letters allowed.

## extend-current-session: Extend existing Map-Markup
## replace-current-session: Replace existing Map-Markup

## upload-label1:

Map-Markup objects can be uploaded here. Valid file extensions are *.gpx

## mapmarkup-project: Map-Markup Project (Geo-Json)
## gpx-file: GPX File
## shape-file: ESRI Shape File

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
