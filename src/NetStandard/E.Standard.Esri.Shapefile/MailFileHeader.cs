namespace E.Standard.Esri.Shapefile;

struct MainFileHeader
{
    public int FileCode;
    public int Unused1;
    public int Unused2;
    public int Unused3;
    public int Unused4;
    public int Unused5;
    public int FileLength;
    public int Version;
    public ShapeType ShapeType;
    public double Xmin;
    public double Ymin;
    public double Xmax;
    public double Ymax;
    public double Zmin;
    public double Zmax;
    public double Mmin;
    public double Mmax;
}
