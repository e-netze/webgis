using E.Standard.Esri.Shapefile.IO;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using E.Standard.WebMapping.Core.Geometry;
using System;
using System.Collections.Generic;
using System.IO;

namespace E.Standard.Esri.Shapefile;

public class ShapeFile
{
    public enum geometryType
    {
        Point = 0,
        Multipoint = 1,
        Polyline = 2,
        Polygon = 3
    }

    private string _file_SHP = "";
    private string _file_SHX = "";
    private string _file_SBX = "";
    private string _file_IDX = "";
    private string _file_DBF = "";
    private string _file_PRJ = "";
    private BinaryReader _shx = null;
    private BinaryReader _shp = null;
    private MainFileHeader _header = new MainFileHeader();
    private long _entities = 0;
    private string _title;
    private DBFFile _dbfFile;
    private readonly IStreamProvider _streamProvider;

    public ShapeFile(IStreamProvider streamProvider, string name)
    {
        try
        {
            _streamProvider = streamProvider;

            _file_SHP = $"{name}.shp";
            _file_SHX = $"{name}.shx";
            _file_SBX = $"{name}.sbx";
            _file_IDX = $"{name}.idx";
            _file_DBF = $"{name}.dbf";
            _file_PRJ = $"{name}.prj";

            if (!streamProvider.StreamExists(_file_SHP) ||
                !streamProvider.StreamExists(_file_SHX) ||
                !streamProvider.StreamExists(_file_DBF))
            {
                throw new ArgumentException("Invalid shape file");
            }

            _entities = (streamProvider[_file_SHX].Length - 100) / 8;
            _title = name;

            this.Open();
            ReadHeader();

            _dbfFile = new DBFFile(streamProvider, name);
        }
        catch (Exception)
        {
            Close();
            throw;
        }
    }

    public string IDX_Filename
    {
        get { return _file_IDX; }
    }
    public bool IDX_Exists
    {
        get
        {
            return _streamProvider.StreamExists(IDX_Filename);
        }
    }

    public string PRJ_Filename
    {
        get { return _file_PRJ; }
    }
    public bool PRJ_Exists
    {
        get
        {
            return _streamProvider.StreamExists(PRJ_Filename);
        }
    }

    public string Title
    {
        get { return _title; }
    }

    public long Entities
    {
        get { return _entities; }
    }

    public void Open()
    {
        _shx = new BinaryReader(_streamProvider[_file_SHX]);

        _shp = new BinaryReader(_streamProvider[_file_SHP]);
    }

    public void Close()
    {
        if (_shp != null)
        {
            _shp.Close();
        }

        _shp = null;
        if (_shx != null)
        {
            _shx.Close();
        }

        _shx = null;
    }

    static public uint SwapWord(uint word)
    {
        uint b1 = word & 0x000000ff;
        uint b2 = (word & 0x0000ff00) >> 8;
        uint b3 = (word & 0x00ff0000) >> 16;
        uint b4 = (word & 0xff000000) >> 24;

        return (b1 << 24) + (b2 << 16) + (b3 << 8) + b4;
    }

    private void ReadHeader()
    {
        if (_shp == null)
        {
            return;
        }

        _shp.BaseStream.Position = 0;
        _header.FileCode = (int)SwapWord((uint)_shp.ReadInt32());
        _header.Unused1 = (int)SwapWord((uint)_shp.ReadInt32());
        _header.Unused2 = (int)SwapWord((uint)_shp.ReadInt32());
        _header.Unused3 = (int)SwapWord((uint)_shp.ReadInt32());
        _header.Unused4 = (int)SwapWord((uint)_shp.ReadInt32());
        _header.Unused5 = (int)SwapWord((uint)_shp.ReadInt32());
        _header.FileLength = (int)SwapWord((uint)_shp.ReadInt32());
        _header.Version = _shp.ReadInt32();
        _header.ShapeType = (ShapeType)_shp.ReadInt32();
        _header.Xmin = _shp.ReadDouble();
        _header.Ymin = _shp.ReadDouble();
        _header.Xmax = _shp.ReadDouble();
        _header.Ymax = _shp.ReadDouble();
        _header.Zmin = _shp.ReadDouble();
        _header.Zmax = _shp.ReadDouble();
        _header.Mmin = _shp.ReadDouble();
        _header.Mmax = _shp.ReadDouble();
    }

    private MainFileHeader Header
    {
        get { return _header; }
    }

    private Envelope ReadEnvelopeFromPos(int pos)
    {
        try
        {
            _shp.BaseStream.Position = pos * 2;  // 16-bit Word
            uint recNumber = SwapWord((uint)_shp.ReadInt32());
            _shp.BaseStream.Position += 4;

            ShapeType sType = (ShapeType)_shp.ReadInt32();
            switch (sType)
            {
                case ShapeType.NullShape:
                    return null;
                case ShapeType.Point:
                    Point p = new Point(_shp.ReadDouble(), _shp.ReadDouble());
                    return new Envelope(p.X, p.Y, p.X, p.Y);
                default:
                    return new Envelope(
                        _shp.ReadDouble(),
                        _shp.ReadDouble(),
                        _shp.ReadDouble(),
                        _shp.ReadDouble());
            }
        }
        catch
        {
            return null;
        }
    }

    private Envelope ReadEnvelopeFromCurrentPos()
    {
        unsafe
        {
            fixed (void* v = _shp.ReadBytes(32))
            {
                double* p = (double*)v;
                return new Envelope(*p++, *p++, *p++, *p++);
            }
        }
    }

    public Envelope ReadEnvelope(uint index)
    {
        if (_shx == null)
        {
            return null;
        }

        try
        {
            _shx.BaseStream.Position = 100 + index * 8;
            uint offset = SwapWord((uint)_shx.ReadInt32());
            uint contentLength = SwapWord((uint)_shx.ReadInt32());

            _shp.BaseStream.Position = offset * 2;  // 16-bit Word
            uint recNumber = SwapWord((uint)_shp.ReadInt32());
            _shp.BaseStream.Position += 4;

            ShapeType sType = (ShapeType)_shp.ReadInt32();
            unsafe
            {
                switch (sType)
                {
                    case ShapeType.NullShape:
                        return null;
                    case ShapeType.PointM:
                    case ShapeType.PointZ:
                    case ShapeType.Point:
                        fixed (void* v = _shp.ReadBytes(sizeof(double) * 4))
                        {
                            double* pp = (double*)v;
                            Point p = new Point(*pp++, *pp++);
                            return new Envelope(p.X, p.Y, p.X, p.Y);
                        }
                    default:
                        fixed (void* v = _shp.ReadBytes(sizeof(double) * 4))
                        {
                            double* p = (double*)v;
                            return new Envelope(*p++, *p++, *p++, *p++);
                        }
                }
            }
        }
        catch
        {
            return null;
        }
    }

    public Feature ReadShape(uint index)
    {
        if (_shx == null)
        {
            return null;
        }

        try
        {
            _shx.BaseStream.Position = 100 + index * 8;
            uint offset = SwapWord((uint)_shx.ReadInt32());
            uint contentLength = SwapWord((uint)_shx.ReadInt32());

            _shp.BaseStream.Position = offset * 2;  // 16-bit Word
            uint recNumber = SwapWord((uint)_shp.ReadInt32());
            _shp.BaseStream.Position += 4;

            Feature feat = new Feature();
            feat.Attributes.Add(new WebMapping.Core.Attribute("ID", recNumber.ToString()));
            ShapeType sType = (ShapeType)_shp.ReadInt32();
            feat.Shape = ReadGeometry(sType);
            return feat;
        }
        catch
        {
            return null;
        }
    }

    public Feature ReadShape(uint index, Envelope envelope)
    {
        if (_shx == null)
        {
            return null;
        }

        try
        {
            _shx.BaseStream.Position = 100 + index * 8;
            uint offset = SwapWord((uint)_shx.ReadInt32());
            uint contentLength = SwapWord((uint)_shx.ReadInt32());

            _shp.BaseStream.Position = offset * 2;  // 16-bit Word
            uint recNumber = SwapWord((uint)_shp.ReadInt32());
            _shp.BaseStream.Position += 4;

            Feature feat = new Feature();
            feat.Attributes.Add(new WebMapping.Core.Attribute("ID", recNumber.ToString()));
            ShapeType sType = (ShapeType)_shp.ReadInt32();
            feat.Shape = ReadGeometry(sType);
            return feat;
        }
        catch
        {
            return null;
        }
    }

    private Shape ReadGeometry(ShapeType sType)
    {
        int numPoints = 0, numParts = 0;
        int[] parts;

        Shape geometry = null;
        switch (sType)
        {
            case ShapeType.NullShape:
                break;
            case ShapeType.Point:
                geometry = new Point(_shp.ReadDouble(), _shp.ReadDouble());
                break;
            case ShapeType.PointM:
                geometry = new Point(_shp.ReadDouble(), _shp.ReadDouble());
                //double m = _shp.ReadDouble();
                break;
            case ShapeType.PointZ:
                geometry = new Point(_shp.ReadDouble(), _shp.ReadDouble(), _shp.ReadDouble());
                //double m=_shp.ReadDouble();
                break;
            case ShapeType.MultiPointZ:
            case ShapeType.MultiPointM:
            case ShapeType.MultiPoint:
                MultiPoint mPoint = new MultiPoint();
                _shp.BaseStream.Position += 32; // BoundingBox
                numPoints = _shp.ReadInt32();
                ReadPoints(numPoints, mPoint);

                if (sType == ShapeType.MultiPointZ)
                {
                    ReadZRange();
                    ReadZ(mPoint);
                }
                if (sType == ShapeType.MultiPointM || sType == ShapeType.MultiPointZ)
                {
                    ReadMRange();
                    ReadM(mPoint);
                }
                geometry = mPoint;
                break;
            case ShapeType.PolyLineM:
            case ShapeType.PolyLineZ:
            case ShapeType.PolyLine:
                _shp.BaseStream.Position += 32; // BoundingBox
                numParts = _shp.ReadInt32();
                numPoints = _shp.ReadInt32();
                parts = ReadParts(numParts);
                Polyline polyline = new Polyline();
                for (int i = 0; i < numParts; i++)
                {
                    WebMapping.Core.Geometry.Path path = new WebMapping.Core.Geometry.Path();
                    ReadPart(i, parts, numPoints, path);
                    polyline.AddPath(path);
                }
                if (sType == ShapeType.PolyLineZ)
                {
                    ReadZRange();
                    for (int i = 0; i < polyline.PathCount; i++)
                    {
                        ReadZ(polyline[i]);
                    }
                }
                if (sType == ShapeType.PolyLineM || sType == ShapeType.PolyLineZ)
                {
                    ReadMRange();
                    for (int i = 0; i < polyline.PathCount; i++)
                    {
                        ReadM(polyline[i]);
                    }
                }
                geometry = polyline;
                break;
            case ShapeType.PolygonM:
            case ShapeType.PolygonZ:
            case ShapeType.Polygon:
                _shp.BaseStream.Position += 32; // BoundingBox

                numParts = _shp.ReadInt32();
                numPoints = _shp.ReadInt32();
                parts = ReadParts(numParts);
                Polygon polygon = new Polygon();
                for (int i = 0; i < numParts; i++)
                {
                    Ring ring = new Ring();
                    ReadPart(i, parts, numPoints, ring);
                    polygon.AddRing(ring);
                }
                if (sType == ShapeType.PolygonZ)
                {
                    ReadZRange();
                    for (int i = 0; i < polygon.RingCount; i++)
                    {
                        ReadZ(polygon[i]);
                    }
                }
                if (sType == ShapeType.PolygonM || sType == ShapeType.PolygonZ)
                {
                    ReadMRange();
                    for (int i = 0; i < polygon.RingCount; i++)
                    {
                        ReadM(polygon[i]);
                    }
                }
                geometry = polygon;
                break;
        }
        return geometry;
    }
    public uint GetIndexFromRecNumber(uint recNumber)
    {
        try
        {
            _shx.BaseStream.Position = 100 + (recNumber - 1) * 8;
            uint offset = SwapWord((uint)_shx.ReadInt32());
            uint contentLength = SwapWord((uint)_shx.ReadInt32());

            _shp.BaseStream.Position = offset * 2;  // 16-bit Word
            uint rec = SwapWord((uint)_shp.ReadInt32());

            if (rec == recNumber)
            {
                return recNumber - 1;
            }

            for (uint index = 0; index < this.Entities; index++)
            {
                _shx.BaseStream.Position = 100 + index * 8;
                offset = SwapWord((uint)_shx.ReadInt32());
                contentLength = SwapWord((uint)_shx.ReadInt32());

                _shp.BaseStream.Position = offset * 2;  // 16-bit Word
                rec = SwapWord((uint)_shp.ReadInt32());

                if (rec == recNumber)
                {
                    return index;
                }
            }
            return (uint)this.Entities + 1;
        }
        catch
        {
            return (uint)this.Entities + 1;
        }
    }

    #region Write
    public static ShapeFile Create(IStreamProvider streamProvider, string name, geometryType geomDef, List<IField> fields)
    {
        if (fields == null)
        {
            return null;
        }

        try
        {
            string file_SHP = $"{name}.shp";
            string file_SHX = $"{name}.shx";

            #region DBF

            if (!DBFFile.Create(streamProvider, name, fields))
            {
                return null;
            }

            #endregion

            #region SHP

            ShapeType type = ShapeType.NullShape;
            switch (geomDef)
            {
                case geometryType.Point:
                    type = ShapeType.Point;
                    break;
                case geometryType.Multipoint:
                    type = ShapeType.MultiPoint;
                    break;
                case geometryType.Polyline:
                    type = ShapeType.PolyLine;
                    break;
                case geometryType.Polygon:
                    type = ShapeType.Polygon;
                    break;
            }

            BinaryWriter bw = new BinaryWriter(streamProvider.CreateStream(file_SHP));

            bw.Write((int)SwapWord(9994));     // FileCode
            bw.Write(0);                        // Unused1;
            bw.Write(0);                        // Unused2;
            bw.Write(0);                        // Unused3;
            bw.Write(0);                        // Unused4;
            bw.Write(0);                        // Unused5;
            bw.Write((int)SwapWord(50));      // FileLength;
            bw.Write(1000);                     // Version
            bw.Write((int)type);                     // ShapeType
            bw.Write((double)0.0);                   // Xmin
            bw.Write((double)0.0);                   // Ymin
            bw.Write((double)0.0);                   // Xmax
            bw.Write((double)0.0);                   // Ymax
            bw.Write((double)0.0);                   // Zmin
            bw.Write((double)0.0);                   // Zmax
            bw.Write((double)0.0);                   // Mmin
            bw.Write((double)0.0);                   // Mmax

            //bw.Flush();

            #endregion

            #region SHX

            bw = new BinaryWriter(streamProvider.CreateStream(file_SHX));

            bw.Write((int)SwapWord(9994));     // FileCode
            bw.Write(0);                        // Unused1;
            bw.Write(0);                        // Unused2;
            bw.Write(0);                        // Unused3;
            bw.Write(0);                        // Unused4;
            bw.Write(0);                        // Unused5;
            bw.Write((int)SwapWord(50));      // FileLength;
            bw.Write(1000);                     // Version
            bw.Write((int)type);                     // ShapeType
            bw.Write((double)0.0);                   // Xmin
            bw.Write((double)0.0);                   // Ymin
            bw.Write((double)0.0);                   // Xmax
            bw.Write((double)0.0);                   // Ymax
            bw.Write((double)0.0);                   // Zmin
            bw.Write((double)0.0);                   // Zmax
            bw.Write((double)0.0);                   // Mmin
            bw.Write((double)0.0);                   // Mmax

            //bw.Flush();

            #endregion

            return new ShapeFile(streamProvider, name);
        }
        catch
        {
            return null;
        }
    }

    public bool WriteShape(Feature feature)
    {
        if (feature == null)
        {
            return false;
        }

        BinaryWriter bw_shp = null;
        BinaryWriter bw_shx = null;
        //FileStream fs_shx = null, fs_shp = null;

        try
        {
            bw_shx = new BinaryWriter(_streamProvider[_file_SHX]);
            bw_shp = new BinaryWriter(_streamProvider[_file_SHP]);

            long pos1 = bw_shp.BaseStream.Position;
            uint recNumber = (uint)(bw_shx.BaseStream.Length - 100) / 8 + 1;

            HeaderEnvelope he = new HeaderEnvelope();

            long contentsLenthPos = 0;
            switch (_header.ShapeType)
            {
                case ShapeType.NullShape:
                    break;
                case ShapeType.PointM:
                case ShapeType.PointZ:
                case ShapeType.Point:
                    if (!(feature.Shape is Point))
                    {
                        return false;
                    }

                    Point p = (Point)feature.Shape;
                    he.minx = he.maxx = p.X;
                    he.miny = he.maxy = p.Y;
                    if (_header.ShapeType == ShapeType.PointZ)
                    {
                        he.minz = he.maxz = p.Z;
                    }

                    contentsLenthPos = WriteFeatureHeader(bw_shp, recNumber);
                    WritePoint(bw_shp, (Point)feature.Shape);
                    if (_header.ShapeType == ShapeType.PointZ)
                    {
                        bw_shp.Write(((Point)feature.Shape).Z);
                    }
                    if (_header.ShapeType == ShapeType.PointM || _header.ShapeType == ShapeType.PointZ)
                    {
                        //bw_shp.Write(((IPoint)feature.Shape).M);
                        bw_shp.Write((double)0.0);
                    }
                    break;
                case ShapeType.MultiPointM:
                case ShapeType.MultiPointZ:
                case ShapeType.MultiPoint:
                    if (feature.Shape is Point)
                    {
                        contentsLenthPos = WriteFeatureHeader(bw_shp, recNumber);
                        WriteEnvelope(bw_shp, feature.Shape.ShapeEnvelope, he);
                        bw_shp.Write(1);
                        WritePoint(bw_shp, (Point)feature.Shape);
                        if (_header.ShapeType == ShapeType.MultiPointZ)
                        {
                            bw_shp.Write(((Point)feature.Shape).Z);
                        }
                        if (_header.ShapeType == ShapeType.MultiPointM || _header.ShapeType == ShapeType.MultiPointZ)
                        {
                            //bw_shp.Write(((IPoint)feature.Shape).M);
                            bw_shp.Write((double)0.0);
                        }
                    }
                    else if (feature.Shape is PointCollection)
                    {
                        contentsLenthPos = WriteFeatureHeader(bw_shp, recNumber);
                        WriteEnvelope(bw_shp, feature.Shape.ShapeEnvelope, he);
                        bw_shp.Write(((PointCollection)feature.Shape).PointCount);
                        WritePoints(bw_shp, (PointCollection)feature.Shape);
                        if (_header.ShapeType == ShapeType.MultiPointZ)
                        {
                            WritePointsZRange(bw_shp, (PointCollection)feature.Shape);
                            WritePointsZ(bw_shp, (PointCollection)feature.Shape);
                        }
                        if (_header.ShapeType == ShapeType.MultiPointM || _header.ShapeType == ShapeType.MultiPointZ)
                        {
                            //bw_shp.Write(((IPoint)feature.Shape).M);
                            WritePointsMRange(bw_shp, (PointCollection)feature.Shape);
                            WritePointsM(bw_shp, (PointCollection)feature.Shape);
                        }
                    }
                    else
                    {
                        return false;
                    }
                    break;
                case ShapeType.PolyLineM:
                case ShapeType.PolyLineZ:
                case ShapeType.PolyLine:
                    if (!(feature.Shape is Polyline))
                    {
                        return false;
                    }

                    Polyline pline = (Polyline)feature.Shape;

                    contentsLenthPos = WriteFeatureHeader(bw_shp, recNumber);
                    WriteEnvelope(bw_shp, feature.Shape.ShapeEnvelope, he);

                    bw_shp.Write(pline.PathCount);
                    WritePointCount(bw_shp, pline);
                    WriteParts(bw_shp, pline);

                    for (int i = 0; i < pline.PathCount; i++)
                    {
                        WritePoints(bw_shp, pline[i]);
                    }

                    if (_header.ShapeType == ShapeType.PolyLineM || _header.ShapeType == ShapeType.PolyLineZ)
                    {
                        PointCollection pColl = new PointCollection(WebMapping.Core.Geometry.SpatialAlgorithms.ShapePoints(pline, false)); // SpatialAlgorithms.Algorithm.GeometryPoints(pline, false);
                        if (_header.ShapeType == ShapeType.PolyLineZ)
                        {
                            WritePointsZRange(bw_shp, pColl);
                            WritePointsZ(bw_shp, pColl);
                        }
                        //bw_shp.Write(((IPoint)feature.Shape).M);
                        WritePointsMRange(bw_shp, pColl);
                        WritePointsM(bw_shp, pColl);
                    }
                    break;
                case ShapeType.PolygonM:
                case ShapeType.PolygonZ:
                case ShapeType.Polygon:
                    if (!(feature.Shape is Polygon))
                    {
                        return false;
                    }

                    Polygon poly = (Polygon)feature.Shape;

                    contentsLenthPos = WriteFeatureHeader(bw_shp, recNumber);
                    WriteEnvelope(bw_shp, feature.Shape.ShapeEnvelope, he);

                    bw_shp.Write(poly.RingCount);
                    WritePointCount(bw_shp, poly);
                    WriteParts(bw_shp, poly);

                    for (int i = 0; i < poly.RingCount; i++)
                    {
                        WritePoints(bw_shp, poly[i]);
                    }

                    if (_header.ShapeType == ShapeType.PolygonM || _header.ShapeType == ShapeType.PolygonZ)
                    {
                        PointCollection pColl = new PointCollection(WebMapping.Core.Geometry.SpatialAlgorithms.ShapePoints(poly, false)); // SpatialAlgorithms.Algorithm.GeometryPoints(poly, false);
                        if (_header.ShapeType == ShapeType.PolygonZ)
                        {
                            WritePointsZRange(bw_shp, pColl);
                            WritePointsZ(bw_shp, pColl);
                        }
                        //bw_shp.Write(((IPoint)feature.Shape).M);
                        WritePointsMRange(bw_shp, pColl);
                        WritePointsM(bw_shp, pColl);
                    }
                    break;
                default:
                    return false;
            }

            //bw_shp.Flush();

            uint contentsSize = (uint)(bw_shp.BaseStream.Position - pos1 - 8) / 2; // -8 weil recnumber und nullword nicht mitzählen. Erst Shapetype und coordinaten,...
            bw_shx.Write((int)SwapWord((uint)(pos1 / 2))); // 16 bit Words
            bw_shx.Write((int)SwapWord(contentsSize));

            //bw_shx.Flush();

            //fs_shp = new FileStream(_file_SHP, FileMode.Open);
            //fs_shx = new FileStream(_file_SHX, FileMode.Open);

            //bw_shp = new BinaryWriter(fs_shp);
            //bw_shx = new BinaryWriter(fs_shx);

            if (contentsLenthPos != 0)
            {
                bw_shp.BaseStream.Position = contentsLenthPos;
                bw_shp.Write((int)SwapWord(contentsSize));
            }

            UpdateHeaderEnvelope(bw_shp, he);
            UpdateHeaderEnvelope(bw_shx, he);

            //bw_shp.Flush();
            //bw_shx.Flush();

            //this.Open();

            _dbfFile.WriteRecord(recNumber, feature);
            return true;
        }
        catch (Exception ex)
        {
            string err = ex.Message;
            return false;
        }
        finally
        {

        }
    }

    private void UpdateHeaderEnvelope(BinaryWriter bw, HeaderEnvelope he)
    {
        try
        {
            double MinX = (_header.Xmin != 0.0) ? _header.Xmin : he.minx;
            double MinY = (_header.Ymin != 0.0) ? _header.Ymin : he.miny;
            double MaxX = (_header.Xmax != 0.0) ? _header.Xmax : he.maxx;
            double MaxY = (_header.Ymax != 0.0) ? _header.Ymax : he.maxy;
            double MinZ = (_header.Zmin != 0.0) ? _header.Zmin : he.minz;
            double MaxZ = (_header.Zmax != 0.0) ? _header.Zmax : he.maxz;
            double MinM = (_header.Mmin != 0.0) ? _header.Mmin : he.minm;
            double MaxM = (_header.Mmax != 0.0) ? _header.Mmax : he.maxm;

            bw.BaseStream.Position = 36;
            bw.Write((double)(_header.Xmin = Math.Min(MinX, he.minx)));
            bw.Write((double)(_header.Ymin = Math.Min(MinY, he.miny)));
            bw.Write((double)(_header.Xmax = Math.Max(MaxX, he.maxx)));
            bw.Write((double)(_header.Ymax = Math.Max(MaxY, he.maxy)));
            bw.Write((double)(_header.Zmin = Math.Min(MinZ, he.minz)));
            bw.Write((double)(_header.Zmax = Math.Max(MaxZ, he.maxz)));
            bw.Write((double)(_header.Mmin = Math.Min(MinM, he.minm)));
            bw.Write((double)(_header.Mmax = Math.Max(MaxM, he.maxm)));

            // FileLength in 16 bit Words
            bw.BaseStream.Position = 24;
            bw.Write((int)SwapWord((uint)(bw.BaseStream.Length / 2)));
            //bw.BaseStream.Flush();

            bw.BaseStream.Position = bw.BaseStream.Length;
        }
        catch (Exception ex)
        {
            string err = ex.Message;
        }
    }

    private long WriteFeatureHeader(BinaryWriter bw, uint recNumber)
    {
        bw.Write((int)SwapWord(recNumber));
        long contentsLengthPos = bw.BaseStream.Position;
        bw.Write(0);
        bw.Write((int)_header.ShapeType);

        return contentsLengthPos;
    }
    private void WriteEnvelope(BinaryWriter bw, Envelope envelope, HeaderEnvelope he)
    {
        if (envelope == null)
        {
            bw.Write((double)0.0);
            bw.Write((double)0.0);
            bw.Write((double)0.0);
            bw.Write((double)0.0);
        }
        else
        {
            bw.Write(he.minx = envelope.MinX);
            bw.Write(he.miny = envelope.MinY);
            bw.Write(he.maxx = envelope.MaxX);
            bw.Write(he.maxy = envelope.MaxY);
        }
    }
    private void WritePoint(BinaryWriter bw, Point point)
    {
        bw.Write((double)(point.X));
        bw.Write((double)(point.Y));

        if (_header.ShapeType == ShapeType.PointZ ||
            _header.ShapeType == ShapeType.MultiPointZ ||
            _header.ShapeType == ShapeType.PolyLineZ ||
            _header.ShapeType == ShapeType.PolygonZ)
        {
            bw.Write((double)point.Z);
        }
    }
    private void WritePoints(BinaryWriter bw, PointCollection pColl)
    {
        for (int i = 0; i < pColl.PointCount; i++)
        {
            WritePoint(bw, pColl[i]);
        }
    }
    private void WritePointsMRange(BinaryWriter bw, PointCollection pColl)
    {
        double min = 0;
        double max = 0;
        //for (int i = 0; i < pColl.PointCount; i++)
        //{
        //    if (i == 0)
        //    {
        //        min = pColl[i].M;
        //        max = pColl[i].M;
        //    }
        //    else
        //    {
        //        min = Math.Min(min, pColl[i].M);
        //        max = Math.Max(max, pColl[i].M);
        //    }
        //}
        bw.Write(min);
        bw.Write(max);
    }
    private void WritePointsM(BinaryWriter bw, PointCollection pColl)
    {
        for (int i = 0; i < pColl.PointCount; i++)
        {
            //bw.Write(pColl[i].M);
            bw.Write((double)0);
        }
    }
    private void WritePointsZRange(BinaryWriter bw, PointCollection pColl)
    {
        double min = 0;
        double max = 0;
        for (int i = 0; i < pColl.PointCount; i++)
        {
            if (i == 0)
            {
                min = pColl[i].Z;
                max = pColl[i].Z;
            }
            else
            {
                min = Math.Min(min, pColl[i].Z);
                max = Math.Max(max, pColl[i].Z);
            }
        }
        bw.Write(min);
        bw.Write(max);
    }
    private void WritePointsZ(BinaryWriter bw, PointCollection pColl)
    {
        for (int i = 0; i < pColl.PointCount; i++)
        {
            bw.Write(pColl[i].Z);
        }
    }
    private void WriteParts(BinaryWriter bw, Shape geometry)
    {
        if (geometry is Polyline)
        {
            Polyline pLine = (Polyline)geometry;
            int c = 0;
            for (int i = 0; i < pLine.PathCount; i++)
            {
                bw.Write(c);
                c += pLine[i].PointCount;
            }
        }
        else if (geometry is Polygon)
        {
            Polygon poly = (Polygon)geometry;
            int c = 0;
            for (int i = 0; i < poly.RingCount; i++)
            {
                bw.Write(c);
                c += poly[i].PointCount;
            }
        }
    }
    private void WritePointCount(BinaryWriter bw, Shape geometry)
    {
        if (geometry is Polyline)
        {
            Polyline pLine = (Polyline)geometry;
            int c = 0;
            for (int i = 0; i < pLine.PathCount; i++)
            {
                c += pLine[i].PointCount;
            }
            bw.Write(c);
        }
        else if (geometry is Polygon)
        {
            Polygon poly = (Polygon)geometry;
            int c = 0;
            for (int i = 0; i < poly.RingCount; i++)
            {
                c += poly[i].PointCount;
            }
            bw.Write(c);
        }
        else
        {
            bw.Write(0);
        }
    }
    #endregion

    private void ReadPoints(int numPoints, PointCollection pointCol)
    {
        unsafe
        {
            fixed (void* b = _shp.ReadBytes(sizeof(double) * 2 * numPoints))
            {
                double* p = (double*)b;
                for (int i = 0; i < numPoints; i++)
                {
                    pointCol.AddPoint(new Point(*p++, *p++));
                }
            }
        }
        /*

			for(int i=0;i<numPoints;i++) 
			{
				pointCol.AddPoint(new Point(_shp.ReadDouble(),_shp.ReadDouble()));
			}*/
    }
    private void ReadMRange()
    {
        _shp.ReadDouble();  // minM
        _shp.ReadDouble();  // maxM
    }
    private void ReadM(PointCollection pointCol)
    {
        // M gibts noch nicht im gView
        //for (int i = 0; i < pointCol.PointCount; i++)
        //{
        //    pointCol[i].M = _shp.ReadDouble();
        //}
    }
    private void ReadZRange()
    {
        _shp.ReadDouble();  // minZ
        _shp.ReadDouble();  // maxZ
    }
    private void ReadZ(PointCollection pointCol)
    {
        for (int i = 0; i < pointCol.PointCount; i++)
        {
            pointCol[i].Z = _shp.ReadDouble();
        }
    }
    private int[] ReadParts(int numParts)
    {
        /*
			int [] parts=new int[numParts];
			for(int i=0;i<numParts;i++)
				parts[i]=_shp.ReadInt32();
			return parts;
         * */
        unsafe
        {
            fixed (void* v = _shp.ReadBytes(4 * numParts))
            {
                int* p = (int*)v;
                int[] parts = new int[numParts];
                for (int i = 0; i < numParts; i++)
                {
                    parts[i] = *p++;
                }

                return parts;
            }
        }
    }
    private void ReadPart(int partNr, int[] parts, int numPoints, PointCollection pointCol)
    {
        partNr++;
        int num = 0;
        if (partNr >= parts.Length)
        {
            num = numPoints - parts[partNr - 1];
        }
        else
        {
            num = parts[partNr] - parts[partNr - 1];
        }

        ReadPoints(num, pointCol);
    }

    private DBFDataReader DBFDataReader(string fieldnames)
    {
        return new DBFDataReader(_dbfFile, fieldnames);
    }

    public List<IField> Fields
    {
        get
        {
            if (_dbfFile != null)
            {
                return _dbfFile.Fields;
            }
            return new List<IField>();
        }
    }

    private class HeaderEnvelope
    {
        public double minx = 0.0, maxx = 0.0;
        public double miny = 0.0, maxy = 0.0;
        public double minz = 0.0, maxz = 0.0;
        public double minm = 0.0, maxm = 0.0;
    }
}
