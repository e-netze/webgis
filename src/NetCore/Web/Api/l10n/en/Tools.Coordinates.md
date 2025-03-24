# name: Coordinates / Elevation

Query coordinates and elevation values

# container: Query

# enter-coordinates: Enter coordinates
# upload-coordinates: Upload coordinates (CSV)
# download-coordinates: Download coordinates (CSV)

# coordinate-system: Coordinate system
# easting: Easting
# northing: Northing
# apply-coordinates: Apply coordinates
# show-coordinates: Show coordinates

# exception-no-points-found: No points found for download

# upload:
## label1:

Coordinates can be uploaded here. The coordinates must be in CSV files with a semicolon
as a delimiter. The columns of the CSV file should correspond to point name/number, 
easting, and northing. The first line will be interpreted as the table header.

## label2:

The coordinate system in which the coordinates of the CSV file are present must be specified here:

## exception-too-many-points: A maximum of {0} coordinate rows can be uploaded
## exception-invalid-row: Invalid row: {0}

## sketch:

### label1: Here you can upload a GPX or GeoJson file that will be taken over as a sketch.
### exception-no-geometry-candidates: No suitable sketch candidates for geometry {0} were found in the file.

# download:

## sketch:

### label1: The current sketch can be downloaded here.


# tip-label: Input Tip
# tip:

md: There are projected coordinates (GK-M34, Web Mercator, ...) and geographic coordinates (WGS 84, GPS).
When entering, the coordinate system should always be selected first.

For projected coordinates, the easting and northing values are usually given in meters.

**GK-M34**
Easting: -67772.43 
Northing: 215837.13

For geographic coordinates, the easting corresponds to the geographic longitude (values west of the prime meridian must have a negative sign).
The northing corresponds to the geographic latitude (values south of the equator must have a negative sign).

The following notations are possible:

Easting: 15.439833 
Northing: 47.078167

**Degrees/Minutes:**
Easting: 15°26.39' 
Northing: 47°04.69'

*simplified with spaces:*
Easting: 15 26.39 
Northing: 47 04.69

**Degrees/Minutes/Seconds:**
Easting: 15°26'23.4'' 
Northing: 47°04'41.4''

*simplified with spaces:*
Easting: 15 26 23.4 
Northing: 47 04 41.4

For all coordinates: Comma and period are always interpreted as a comma.
