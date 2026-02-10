#unit: Unit of length

#category_unit: Representation

#expression: Expression

Here you can specify an expression that calculates the stationing. 
The expression must contain a parameter that specifies the length in meters. 
Example: {0} m, {json-property1} {json-property2} ... If an API is queried, the JSON parameters of the response can be used here as placeholders. 
Line breaks can be forced with \n.

#category_expression: Representation

#point_line_relation: Point-line relationship (SQL)

#category_point_line_relation: Link to point-line theme

#point_stat_field: Stationing field of the point theme

#category_point_stat_field: Link to point-line theme

#service_url: Service URL

Here you can specify a URL to a service that calculates the stationing. 
If no value is specified here, the stationing is calculated from the point and line themes. 
The following placeholders are possible: {x}, {y} ... x,y in WGS84, {x:espgCode}, {y:epsgCode} ... x,y converted to EPSG code, {mapscale} ... current map scale

#category_service_url: Or query service API

#category_calc_sref_id: Calculation

#calc_sref_id: Coordinate system to be used for the calculation (EPSG code)

To ensure the accuracy of the results, calculations should be performed in a projection plane with minimal length distortion. 
If no value or 0 is specified here, the calculation is performed in the map projection. 
This can lead to distortions with WebMercator or geographic projections. 
In this case, a projected coordinate system such as Gauss-Krüger is ideal.