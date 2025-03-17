using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Geometry;
using E.Standard.WebMapping.GeoServices.Tiling.Models;
using System.Collections.Generic;

namespace E.Standard.WebMapping.GeoServices.Tiling.Extensions;

static class TileGridExtensions
{
    static public IEnumerable<TileData> GetTileUrls(this TileGrid grid,
                                           string[] tileUrlTemplates,
                                           int row_s, int row_e, int col_s, int col_e, int level,
                                           double resolution,
                                           GeometricTransformerPro geoTransform,
                                           ServiceRestirctions serviceRestrictions)
    {
        var tileData = new List<TileData>();
        int templateIndex = 0, templateLength = tileUrlTemplates.Length;

        for (int row = row_s; row <= row_e; row++)
        {
            for (int col = col_s; col <= col_e; col++)
            {

                if (serviceRestrictions != null)
                {
                    var tilePoint2 = grid.TileUpperLeft(row, col, resolution);
                    Envelope tileEnvelope = new Envelope(tilePoint2.X, tilePoint2.Y,
                        tilePoint2.X + grid.TileWidth(resolution), tilePoint2.Y + grid.TileHeight(resolution) * (grid.Orientation == TileGridOrientation.UpperLeft ? -1.0 : 1.0));

                    if (geoTransform != null)
                    {
                        geoTransform.InvTransform(tileEnvelope);
                    }

                    if (!serviceRestrictions.EnvelopeInBounds(tileEnvelope))
                    {
                        continue;
                    }
                }


                string tileUrl = tileUrlTemplates[(templateIndex++) % templateLength]
                                    .Replace("[LEVEL]", level.ToString())
                                    .Replace("[ROW]", row.ToString())
                                    .Replace("[COL]", col.ToString());

                tileUrl = tileUrl.Replace("[LEVEL_PAD2]", level.ToString().PadLeft(2, '0'));
                tileUrl = tileUrl.Replace("[ROW_DIV3_PAD3]", (row / 1000000).ToString().PadLeft(3, '0') + "/" +
                                                      ((row / 1000) % 1000).ToString().PadLeft(3, '0') + "/" +
                                                      ((row % 1000).ToString().PadLeft(3, '0')));
                tileUrl = tileUrl.Replace("[COL_DIV3_PAD3]", (col / 1000000).ToString().PadLeft(3, '0') + "/" +
                                                      ((col / 1000) % 1000).ToString().PadLeft(3, '0') + "/" +
                                                      ((col % 1000).ToString().PadLeft(3, '0')));
                tileUrl = tileUrl.Replace("[ROW_HEX_PAD8]", row.ToString("x").PadLeft(8, '0'));
                tileUrl = tileUrl.Replace("[COL_HEX_PAD8]", col.ToString("x").PadLeft(8, '0'));

                tileData.Add(new TileData()
                {
                    Url = tileUrl,
                    Row = row,
                    Col = col,
                    //Level = level
                });
            }
        }

        return tileData;
    }
}
