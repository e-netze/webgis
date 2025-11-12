# name: Map Series Print

Print a map series in PDF format.

# container: Map

# layout-quality: Layout/Qualität

# layout: Layout

# format: Paper format

# print-scale: Print scale

# print-quality: Print quality

# print-jobs: Print jobs

# start-print-job: Start print job

# tools:
## save: Save Series
## open: Load Series
## upload: Upload Series
## download: Download Series
## remove-series: Remove Series

# create:
## method: Method
### bbox-grid: Bounding Box Grid
### intersection-grid: Intersection Grid
### along-polyline: Along a Polyline
## overlapping-percent: Overlapping (Percent)
## start: Create Series

# io:
## extend-current-session: Extend Current Series
## replace-current-session: Replace Current Series
## exception-no-sketch-defined: 

There is no series defined in the map yet. Please create
first a series. You can define the individual pages of the series
by clicking on the map.

## exception-shape-not-contains-vertices:

Oops, something went wrong.
The geometry of the series does not contain any vertices.

## exception-too-many-pages:

The loading of the series cannot be carried out correctly because the file
contains too many pages ({0} pages).
A maximum of {1} pages can be defined in a series. Therefore, not
all pages will be loaded.

## upload-label1: 

Here you can upload series. Valid file extensions are *.json.
Only series created with this tool can be uploaded.
 
# create-series-from-features: Series from geo-objects
## exception-too-many-pages:

The creation of the series cannot be carried out correctly because too many pages
would have to be created ({0} pages).
A maximum of {1} pages can be defined in a series. Change the scale
or the paper format to create fewer pages.
