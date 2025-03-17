using E.Standard.Json;
using E.Standard.WebGIS.Tools.Georeferencing.Image.Models;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse.Models;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using static E.Standard.WebMapping.Core.Api.ApiToolEventArguments;

namespace E.Standard.WebGIS.Tools.Georeferencing.Image.Extensions;

static public class GeorefImageMetadataExtensions
{
    static public void UpdatePassPoints(this GeorefImageMetadata georefMetadata, OverlayGeoRefDefintionClass overlayDef)
    {
        if (georefMetadata == null)
        {
            return;
        }

        georefMetadata.PassPoints = new List<PassPoint>();
        if (overlayDef.PassPoints != null)
        {
            foreach (var passPoint in overlayDef.PassPoints.Where(p => p.Vector != null && p.Pos != null))
            {
                ((ICollection<PassPoint>)georefMetadata.PassPoints)
                    .Add(new PassPoint()
                    {
                        VectorX = passPoint.Vector.x,
                        VectorY = passPoint.Vector.y,
                        ImageX = (int)(georefMetadata.ImageWidth * passPoint.Vector.x),
                        ImageY = (int)(georefMetadata.ImageHeight * passPoint.Vector.y),
                        WorldPoint = new GeoPosition()
                        {
                            Longitude = passPoint.Pos.X,
                            Latitude = passPoint.Pos.Y
                        }
                    });
            }
        }
    }

    static public void UpdatePosition(this GeorefImageMetadata georefMetadata, OverlayGeoRefDefintionClass overlayDef)
    {
        if (georefMetadata == null)
        {
            return;
        }

        georefMetadata.TopLeft = georefMetadata.TopRight = georefMetadata.BottomLeft = null;

        if (overlayDef.TopLeft != null)
        {
            georefMetadata.TopLeft = new GeoPosition()
            {
                Longitude = overlayDef.TopLeft.X,
                Latitude = overlayDef.TopLeft.Y
            };
        }

        if (overlayDef.TopRight != null)
        {
            georefMetadata.TopRight = new GeoPosition()
            {
                Longitude = overlayDef.TopRight.X,
                Latitude = overlayDef.TopRight.Y
            };
        }

        if (overlayDef.BottomLeft != null)
        {
            georefMetadata.BottomLeft = new GeoPosition()
            {
                Longitude = overlayDef.BottomLeft.X,
                Latitude = overlayDef.BottomLeft.Y
            };
        }
    }

    public static void ProjectWorld(this GeorefImageMetadata georefMetadata,
                                    IGeometricTransformer2 transformer)
    {
        if (georefMetadata?.PassPoints != null)
        {
            foreach (var passPoint in georefMetadata.PassPoints)
            {
                if (passPoint.WorldPoint != null)
                {
                    var point = new Point(passPoint.WorldPoint.Longitude, passPoint.WorldPoint.Latitude);
                    transformer.Transform(point);
                    passPoint.WorldPoint.Epsg = transformer.ToSrsId;
                    passPoint.WorldPoint.X = point.X;
                    passPoint.WorldPoint.Y = point.Y;
                }
            }
        }

        if (georefMetadata.TopLeft != null)
        {
            var point = new Point(georefMetadata.TopLeft.Longitude, georefMetadata.TopLeft.Latitude);
            transformer.Transform(point);
            georefMetadata.TopLeft.Epsg = transformer.ToSrsId;
            georefMetadata.TopLeft.X = point.X;
            georefMetadata.TopLeft.Y = point.Y;
        }

        if (georefMetadata.TopRight != null)
        {
            var point = new Point(georefMetadata.TopRight.Longitude, georefMetadata.TopRight.Latitude);
            transformer.Transform(point);
            georefMetadata.TopRight.Epsg = transformer.ToSrsId;
            georefMetadata.TopRight.X = point.X;
            georefMetadata.TopRight.Y = point.Y;
        }

        if (georefMetadata.BottomLeft != null)
        {
            var point = new Point(georefMetadata.BottomLeft.Longitude, georefMetadata.BottomLeft.Latitude);
            transformer.Transform(point);
            georefMetadata.BottomLeft.Epsg = transformer.ToSrsId;
            georefMetadata.BottomLeft.X = point.X;
            georefMetadata.BottomLeft.Y = point.Y;
        }
    }

    public static void ProjectGeographic(this GeorefImageMetadata georefMetadata,
                                    IGeometricTransformer transformer)
    {
        if (georefMetadata?.PassPoints != null)
        {
            foreach (var passPoint in georefMetadata.PassPoints)
            {
                if (passPoint.WorldPoint != null)
                {
                    var point = new Point(passPoint.WorldPoint.X, passPoint.WorldPoint.Y);
                    transformer.InvTransform(point);
                    passPoint.WorldPoint.Longitude = point.X;
                    passPoint.WorldPoint.Latitude = point.Y;
                }
            }
        }

        if (georefMetadata.TopLeft != null)
        {
            var point = new Point(georefMetadata.TopLeft.X, georefMetadata.TopLeft.Y);
            transformer.InvTransform(point);
            georefMetadata.TopLeft.Longitude = point.X;
            georefMetadata.TopLeft.Latitude = point.Y;
        }

        if (georefMetadata.TopRight != null)
        {
            var point = new Point(georefMetadata.TopRight.X, georefMetadata.TopRight.Y);
            transformer.InvTransform(point);
            georefMetadata.TopRight.Longitude = point.X;
            georefMetadata.TopRight.Latitude = point.Y;
        }

        if (georefMetadata.BottomLeft != null)
        {
            var point = new Point(georefMetadata.BottomLeft.X, georefMetadata.BottomLeft.Y);
            transformer.InvTransform(point);
            georefMetadata.BottomLeft.Longitude = point.X;
            georefMetadata.BottomLeft.Latitude = point.Y;
        }
    }

    public static double WorldWidth(this GeorefImageMetadata georefMetadata)
    {
        return new Point(georefMetadata.TopRight.X, georefMetadata.TopRight.Y).Distance2D(new Point(georefMetadata.TopLeft.X, georefMetadata.TopLeft.Y));
    }
    public static double WorldHeight(this GeorefImageMetadata georefMetadata)
    {
        return new Point(georefMetadata.BottomLeft.X, georefMetadata.BottomLeft.Y).Distance2D(new Point(georefMetadata.TopLeft.X, georefMetadata.TopLeft.Y));
    }

    static public void SaveGeorefImageMetadata(this GeorefImageMetadata georefMetadata, IBridge bridge)
    {
        if (!String.IsNullOrEmpty(georefMetadata?.Id))
        {
            bridge.Storage.Save($"{georefMetadata.Id.GeorefImageIdToStorageName()}.meta", JSerializer.Serialize(georefMetadata));
        }
    }

    static public IEnumerable<StaticOverlayServiceDefinitionDTO.PassPoint> StaticOverlayServiceDefinitionPassPoints(this GeorefImageMetadata georefMetadata)
    {
        if (georefMetadata.PassPoints == null || georefMetadata.PassPoints.Count() == 0)
        {
            return null;
        }

        return georefMetadata.PassPoints
                             .Where(p => p.WorldPoint != null)
                             .Select(p => new StaticOverlayServiceDefinitionDTO.PassPoint()
                             {
                                 Vector = new double[] { p.VectorX, p.VectorY },
                                 World = new double[] { p.WorldPoint.Longitude, p.WorldPoint.Latitude }
                             });
    }

    static public float CalcWidthHeightRatio(this GeorefImageMetadata georefMetadata)
    {
        if (georefMetadata == null ||
            georefMetadata.ImageWidth == 0 || georefMetadata.ImageHeight == 0)
        {
            return 1f;
        }

        return georefMetadata.ImageWidth / (float)georefMetadata.ImageHeight;
    }

    static public string WorldFileExtension(this GeorefImageMetadata georefMetadata)
    {
        return georefMetadata.ImageExtension.WorldFileExtension();
    }

    static public string WorldFileExtension(this string fileExtension)
    {
        if (String.IsNullOrEmpty(fileExtension))
        {
            return String.Empty;
        }

        if (fileExtension.StartsWith("."))
        {
            fileExtension = fileExtension.Substring(1);
        }

        switch (fileExtension.ToLower())
        {
            case "png":
                return "pgw";
            case "jpg":
            case "jpeg":
                return "jgw";
            default:
                return "worldfile";
        }
    }

    static public Envelope Envelope4326(this GeorefImageMetadata georefImageMetadata, double? raisePercent = null)
    {
        if (georefImageMetadata.TopLeft == null ||
            georefImageMetadata.TopRight == null ||
            georefImageMetadata.BottomLeft == null)
        {
            return null;
        }

        var points = new[] {
            georefImageMetadata.TopLeft,
            georefImageMetadata.TopRight,
            georefImageMetadata.BottomLeft,
            new GeoPosition()
            {
                Longitude = georefImageMetadata.TopLeft.Longitude +
                                (georefImageMetadata.TopRight.Longitude - georefImageMetadata.TopLeft.Longitude) +
                                (georefImageMetadata.BottomLeft.Longitude - georefImageMetadata.TopLeft.Longitude),
                Latitude = georefImageMetadata.TopLeft.Latitude +
                                (georefImageMetadata.TopRight.Latitude - georefImageMetadata.TopLeft.Latitude) +
                                (georefImageMetadata.BottomLeft.Latitude - georefImageMetadata.TopLeft.Latitude)
            }
        };

        var envelope = new Envelope(
                points.Select(p => p.Longitude).Min(),
                points.Select(p => p.Latitude).Min(),
                points.Select(p => p.Longitude).Max(),
                points.Select(p => p.Latitude).Max());

        if (raisePercent.HasValue)
        {
            envelope.Raise(envelope.CenterPoint.X, envelope.CenterPoint.Y, raisePercent.Value);
        }

        return envelope;
    }

    static public bool IsGeoreferenced(this GeorefImageMetadata georefImageMetadata)
    {
        return georefImageMetadata != null && georefImageMetadata.BottomLeft != null && georefImageMetadata.TopLeft != null && georefImageMetadata.BottomLeft != null;
    }
}
