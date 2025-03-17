using E.Standard.WebGIS.Tools.Extensions;
using E.Standard.WebMapping.Core.Api;

namespace E.Standard.WebGIS.Tools.Tests.Redlining;

public class RedliningUploadTests
{
    [Theory]
    [InlineData("""
            <?xml version="1.0" encoding="utf-8"?>
                <gpx xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" version="1.1" xmlns="http://www.topografix.com/GPX/1/1">
                  <metadata />
                  <wpt lat="47.0513870747821" lon="15.4473427728917">
                    <name>Waypoint 1</name>
                  </wpt>
                  <wpt lat="47.0506043156458" lon="15.4477944815727">
                    <name>Waypoint 2</name>
                  </wpt>
                  <wpt lat="47.0499699410316" lon="15.4484776862764">
                    <name>In the middle</name>
                  </wpt>
                  <wpt lat="47.0498050252377" lon="15.4495527922003">
                    <name>Waypoint 3</name>
                  </wpt>
                  <wpt lat="47.0497841088774" lon="15.4502940483422">
                    <name>Waypoint 4</name>
                  </wpt>
                  <wpt lat="47.0498016211433" lon="15.4516975872971">
                    <name>Waypoint 5</name>
                  </wpt>
                  <trk>
                    <name>Track 1</name>
                    <trkseg>
                      <trkpt lat="47.051543" lon="15.44774" />
                      <trkpt lat="47.051119" lon="15.447991" />
                      <trkpt lat="47.05071" lon="15.448524" />
                      <trkpt lat="47.050338" lon="15.449534" />
                      <trkpt lat="47.050202" lon="15.450882" />
                      <trkpt lat="47.05008" lon="15.452375" />
                      <trkpt lat="47.050017" lon="15.454521" />
                    </trkseg>
                  </trk>
                </gpx>
    """)]
    public void ParseUploaded_GPX(string gpxXml)
    {
        // Arrange
        var e = new ApiToolEventArguments("");

        // Act
        var features = ApiToolEventFileExtensions.ParseGpx(e, gpxXml, true);

        // Assert
        Assert.NotNull(features?.Features);
        Assert.True(features.Features.Count() == 7);

        Assert.NotNull(features.Features.FirstOrDefault(f =>
                f["_meta.tool"]?.ToString() == "symbol"
                && f["_meta.text"]?.ToString() == "Waypoint 1"
                && "point".Equals(f.Geometry?.type, StringComparison.OrdinalIgnoreCase))
            );

        Assert.NotNull(features.Features.FirstOrDefault(f =>
                f["_meta.tool"]?.ToString() == "symbol"
                && f["_meta.text"]?.ToString() == "Waypoint 2"
                && "point".Equals(f.Geometry?.type, StringComparison.OrdinalIgnoreCase))
            );

        Assert.NotNull(features.Features.FirstOrDefault(f =>
                f["_meta.tool"]?.ToString() == "symbol"
                && f["_meta.text"]?.ToString() == "Waypoint 3"
                && "point".Equals(f.Geometry?.type, StringComparison.OrdinalIgnoreCase))
            );

        Assert.NotNull(features.Features.FirstOrDefault(f =>
                f["_meta.tool"]?.ToString() == "symbol"
                && f["_meta.text"]?.ToString() == "Waypoint 4"
                && "point".Equals(f.Geometry?.type, StringComparison.OrdinalIgnoreCase))
            );

        Assert.NotNull(features.Features.FirstOrDefault(f =>
                f["_meta.tool"]?.ToString() == "symbol"
                && f["_meta.text"]?.ToString() == "Waypoint 5"
                && "point".Equals(f.Geometry?.type, StringComparison.OrdinalIgnoreCase))
            );

        Assert.NotNull(features.Features.FirstOrDefault(f =>
                f["_meta.tool"]?.ToString() == "symbol"
                && f["_meta.text"]?.ToString() == "In the middle"
                && "point".Equals(f.Geometry?.type, StringComparison.OrdinalIgnoreCase))
            );

        Assert.NotNull(features.Features.FirstOrDefault(f =>
                f["_meta.tool"]?.ToString() == "line"
                && f["_meta.text"]?.ToString() == "Track 1"
                && "linestring".Equals(f.Geometry?.type, StringComparison.OrdinalIgnoreCase))
            );
    }

    [Theory]
    [InlineData("""
          {
          "type": "FeatureCollection",
          "features": [
            {
              "type": "Feature",
              "geometry": {
                "type": "LineString",
                "coordinates": [
                  [
                    13.654437,
                    47.501761
                  ],
                  [
                    13.638243,
                    47.620908
                  ],
                  [
                    13.657372,
                    47.674859
                  ],
                  [
                    13.697967,
                    47.705397
                  ],
                  [
                    13.819683,
                    47.734314
                  ],
                  [
                    13.945484,
                    47.750665
                  ]
                ]
              },
              "properties": {
                "stroke": "#ff0000",
                "stroke-opacity": 0.8,
                "stroke-width": 2,
                "stroke-style": "1",
                "_meta": {
                  "tool": "line",
                  "text": null,
                  "source": null
                }
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "Polygon",
                "coordinates": [
                  [
                    [
                      14.08663,
                      47.726201
                    ],
                    [
                      14.098176,
                      47.653575
                    ],
                    [
                      14.288408,
                      47.65713
                    ],
                    [
                      14.333896,
                      47.701188
                    ],
                    [
                      14.290203,
                      47.783511
                    ],
                    [
                      14.08663,
                      47.726201
                    ]
                  ]
                ]
              },
              "properties": {
                "stroke": "#ff0000",
                "stroke-opacity": 0.8,
                "stroke-width": 2,
                "stroke-style": "1",
                "fill": "#ffff00",
                "fill-opacity": 0.2,
                "_meta": {
                  "tool": "polygon",
                  "text": null,
                  "source": null
                }
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "Point",
                "coordinates": [
                  13.654033208406055,
                  47.70415750430203
                ]
              },
              "properties": {
                "point-color": "#ff0000",
                "point-size": 10,
                "_meta": {
                  "tool": "point"
                }
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "Point",
                "coordinates": [
                  13.687374690504344,
                  47.720781892709816
                ]
              },
              "properties": {
                "point-color": "#ff0000",
                "point-size": 10,
                "_meta": {
                  "tool": "point"
                }
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "Point",
                "coordinates": [
                  13.722192203172067,
                  47.694543676243335
                ]
              },
              "properties": {
                "point-color": "#ff0000",
                "point-size": 10,
                "_meta": {
                  "tool": "point"
                }
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "Point",
                "coordinates": [
                  13.709963,
                  47.73179
                ]
              },
              "properties": {
                "symbol": "graphics/markers/hotspot0.gif",
                "_meta": {
                  "tool": "symbol",
                  "symbol": {
                    "id": "graphics/markers/hotspot0.gif",
                    "icon": "graphics/markers/hotspot0.gif",
                    "iconSize": [
                      29,
                      30
                    ],
                    "iconAnchor": [
                      14,
                      15
                    ],
                    "popupAnchor": [
                      0,
                      -15
                    ]
                  }
                }
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "Point",
                "coordinates": [
                  13.745507,
                  47.732598
                ]
              },
              "properties": {
                "symbol": "graphics/markers/hotspot0.gif",
                "_meta": {
                  "tool": "symbol",
                  "symbol": {
                    "id": "graphics/markers/hotspot0.gif",
                    "icon": "graphics/markers/hotspot0.gif",
                    "iconSize": [
                      29,
                      30
                    ],
                    "iconAnchor": [
                      14,
                      15
                    ],
                    "popupAnchor": [
                      0,
                      -15
                    ]
                  }
                }
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "Point",
                "coordinates": [
                  13.775129,
                  47.734429
                ]
              },
              "properties": {
                "symbol": "graphics/markers/hotspot0.gif",
                "_meta": {
                  "tool": "symbol",
                  "symbol": {
                    "id": "graphics/markers/hotspot0.gif",
                    "icon": "graphics/markers/hotspot0.gif",
                    "iconSize": [
                      29,
                      30
                    ],
                    "iconAnchor": [
                      14,
                      15
                    ],
                    "popupAnchor": [
                      0,
                      -15
                    ]
                  }
                }
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "Point",
                "coordinates": [
                  13.83753,
                  47.744429
                ]
              },
              "properties": {
                "symbol": "graphics/markers/pin1.png",
                "_meta": {
                  "tool": "symbol",
                  "symbol": {
                    "id": "graphics/markers/pin1.png",
                    "icon": "graphics/markers/pin1.png",
                    "iconSize": [
                      23,
                      32
                    ],
                    "iconAnchor": [
                      0,
                      31
                    ],
                    "popupAnchor": [
                      11,
                      -31
                    ]
                  }
                }
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "Point",
                "coordinates": [
                  13.872723626958411,
                  47.7528820134406
                ]
              },
              "properties": {
                "font-color": "#00cccc",
                "font-style": "bolditalic",
                "font-size": "20",
                "_meta": {
                  "tool": "text",
                  "text": "This is a text!",
                  "source": null
                }
              }
            },
            {
              "type": "Feature",
              "geometry": {
                "type": "Point",
                "coordinates": [
                  13.82729,
                  47.727424
                ]
              },
              "properties": {
                "symbol": "https://xxx.com/symbol1.png",
                "_meta": {
                  "tool": "symbol",
                  "symbol": {
                    "id": "https://xxx.com/symbol1.png",
                    "icon": "https://xxx.com/symbol1.png",
                    "iconSize": [
                      23,
                      32
                    ],
                    "iconAnchor": [
                      0,
                      31
                    ],
                    "popupAnchor": [
                      11,
                      -31
                    ]
                  }
                }
              }
            }
          ]
        }

        """)]
    public void ParseUploaded_Redling_Project_GeoJson(string geoJson)
    {
        // Arrange
        var e = new ApiToolEventArguments("");

        // Act
        var features = ApiToolEventFileExtensions.ParseGeoJson(e, geoJson, true, true);

        // Assert
        Assert.NotNull(features?.Features);
        Assert.True(features.Features.Count() == 11);

        Assert.NotNull(features.Features.FirstOrDefault(f =>
                f["_meta.tool"]?.ToString() == "line"
                && f["_meta.text"] == null
                && "LineString".Equals(f.Geometry?.type, StringComparison.OrdinalIgnoreCase))
            );

        Assert.NotNull(features.Features.FirstOrDefault(f =>
                f["_meta.tool"]?.ToString() == "text"
                && f["_meta.text"]?.ToString() == "This is a text!"
                && "point".Equals(f.Geometry?.type, StringComparison.OrdinalIgnoreCase))
            );

        Assert.Null(features.Features.FirstOrDefault(f =>
                f["symbol"]?.ToString()?.Contains("xxx.com") == true
                || f["_meta.symbol.id"]?.ToString()?.Contains("xxx.com") == true
                || f["_meta.symbol.icon"]?.ToString()?.Contains("xxx.com") == true)
            );
    }
}
