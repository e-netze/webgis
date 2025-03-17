using E.Standard.DbConnector;
using E.Standard.DbConnector.Schema;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace E.Standard.WebMapping.Core.Geometry;

public class SpatialReferenceCollection : IDbSchemaProvider
{
    static object lockThis = new object();

    private List<SpatialReference> _sRefs = new List<SpatialReference>();

    //public void LoadFromCSV(string filename)
    //{
    //    _sRefs = new List<SpatialReference>();

    //    StreamReader sr = new StreamReader(filename);

    //    string line;
    //    while ((line = sr.ReadLine()) != null)
    //    {
    //        string[] parts = line.Split(';');
    //        if (parts.Length < 4) continue;

    //        SpatialReference sRef = new SpatialReference(int.Parse(parts[0]), parts[4], parts[2]);
    //        sRef.EPSG = parts[1];
    //        sRef.WKT = parts[3];
    //        _sRefs.Add(sRef);
    //    }
    //    sr.Close();
    //}

    static public string p4DatabaseConnection = String.Empty;

    private string _rootPath = String.Empty;
    public string RootPath
    {
        get { return _rootPath; }
        set
        {
            lock (lockThis)
            {
                _rootPath = value;
            }
        }
    }

    public SpatialReference ById(int id)
    {
        if (id == -1)
        {
            return null;
        }

        lock (lockThis)
        {
            foreach (SpatialReference sRef in _sRefs)
            {
                if (sRef.Id == id)
                {
                    return sRef.Clone();
                }
            }

            SpatialReference sr = LoadId(id);
            if (sr != null)
            {
                _sRefs.Add(sr);
            }

            return sr;
        }
    }

    private SpatialReference LoadId(int id)
    {
        if (String.IsNullOrEmpty(_rootPath) && String.IsNullOrEmpty(SpatialReferenceCollection.p4DatabaseConnection))
        {
            return null;
        }

        SpatialReference spatialReference = null;

        if (SpatialReferenceCollection.p4DatabaseConnection == "#")
        {
            var epsgId = id.ToString();
            var srRow = this
                .CsvRows
                .Where(r => r.Split(';')[1] == epsgId)
                .FirstOrDefault();

            if (srRow != null)
            {
                spatialReference = new SpatialReference(id,
                    name: srRow.Split(';')[3],
                    proj4: srRow.Split(';')[2]);
                spatialReference.EPSG = epsgId;
            }
        }
        else
        {
            DBConnection conn = new DBConnection();
            if (String.IsNullOrEmpty(SpatialReferenceCollection.p4DatabaseConnection))
            {
                conn.OleDbConnectionMDB = _rootPath + @"/p4.mdb";
            }
            else
            {
                conn.OleDbConnectionMDB = SpatialReferenceCollection.p4DatabaseConnection;
            }

            DataTable tab = conn.Select("select * from P4 where PROJ_EPSG=" + id);
            if (tab != null && tab.Rows.Count != 0)
            {
                spatialReference = new SpatialReference(id,
                    tab.Rows[0]["PROJ_DESCRIPTION"].ToString(),
                    tab.Rows[0]["PROJ_P4"].ToString());
                spatialReference.EPSG = id.ToString();
            }
        }

        if (spatialReference == null)
        {
            return null;
        }

        // WKT unused
        //string wkt = String.IsNullOrEmpty(_rootPath) ? String.Empty : LoadWKT(_rootPath + @"/esri/geographic.txt", id);
        //if (String.IsNullOrEmpty(wkt))
        //{
        //    wkt = String.IsNullOrEmpty(_rootPath) ? String.Empty : LoadWKT(_rootPath + @"/esri/projected.txt", id);
        //}
        //spatialReference.WKT = wkt;

        #region Determine Axis (eg for WMS 1.3.0)

        string pgWKT = String.IsNullOrEmpty(_rootPath) ? String.Empty : LoadPostGisWKT(_rootPath + @"/pg.txt", id).ToUpper();
        string axisX = null, axisY = null;

        if (!String.IsNullOrEmpty(pgWKT))
        {
            axisX = GetQoutedWKTParameter(pgWKT, "AXIS[\"X\"", ",", "]");
            axisY = GetQoutedWKTParameter(pgWKT, "AXIS[\"Y\"", ",", "]");

            if (String.IsNullOrEmpty(axisX) && String.IsNullOrEmpty(axisY))  // Northing,Easing Axis UTM
            {
                axisX = GetQoutedWKTParameter(pgWKT, "AXIS[\"x\"", ",", "]");
                axisY = GetQoutedWKTParameter(pgWKT, "AXIS[\"y\"", ",", "]");
            }
            if (String.IsNullOrEmpty(axisX) && String.IsNullOrEmpty(axisY))  // Northing,Easing Axis UTM
            {
                axisX = GetQoutedWKTParameter(pgWKT, "AXIS[\"EASTING\"", ",", "]");
                axisY = GetQoutedWKTParameter(pgWKT, "AXIS[\"NORTHING\"", ",", "]");
            }
            if (String.IsNullOrEmpty(axisX) && String.IsNullOrEmpty(axisY))  // Northing,Easing Axis UTM
            {
                axisX = GetQoutedWKTParameter(pgWKT, "AXIS[\"Easting\"", ",", "]");
                axisY = GetQoutedWKTParameter(pgWKT, "AXIS[\"Northing\"", ",", "]");
            }
            if (String.IsNullOrEmpty(axisX) && String.IsNullOrEmpty(axisY))  // Northing,Easing Axis UTM
            {
                axisX = GetQoutedWKTParameter(pgWKT, "AXIS[\"easting\"", ",", "]");
                axisY = GetQoutedWKTParameter(pgWKT, "AXIS[\"northing\"", ",", "]");
            }


        }

        // other known behavoir
        if (String.IsNullOrEmpty(axisX) && String.IsNullOrEmpty(axisY))
        {
            if (spatialReference.Proj4.Contains("+proj=utm"))  // UTM
            {
                axisX = "EAST";
                axisY = "NORTH";
            }
            else if (spatialReference.IsProjective == false)  // Laut tests mit QGIS => 4326 => X-Achse zeigt nach Norden!!!
            {
                axisX = "NORTH";
                axisY = "EAST";
            }
        }

        if (!String.IsNullOrEmpty(axisX) && !String.IsNullOrEmpty(axisY))
        {
            switch (axisX.ToUpper())
            {
                case "NORTH":
                    spatialReference.AxisX = AxisDirection.North;
                    break;
                case "EAST":
                    spatialReference.AxisX = AxisDirection.East;
                    break;
                case "SOUTH":
                    spatialReference.AxisX = AxisDirection.South;
                    break;
                case "WEST":
                    spatialReference.AxisX = AxisDirection.West;
                    break;
            }
            switch (axisY.ToUpper())
            {
                case "NORTH":
                    spatialReference.AxisY = AxisDirection.North;
                    break;
                case "EAST":
                    spatialReference.AxisY = AxisDirection.East;
                    break;
                case "SOUTH":
                    spatialReference.AxisY = AxisDirection.South;
                    break;
                case "WEST":
                    spatialReference.AxisY = AxisDirection.West;
                    break;
            }
        }

        #endregion

        return spatialReference;
    }

    private string LoadWKT(string filename, int id)
    {
        StreamReader sr = null;
        try
        {
            sr = new StreamReader(filename);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                line = line.Replace("\t", " ");
                if (line == id.ToString() ||
                    line.StartsWith(id + " "))
                {
                    line = sr.ReadLine();
                    return line;
                }
            }
            return String.Empty;
        }
        catch
        {
            return String.Empty;
        }
        finally
        {
            if (sr != null)
            {
                try { sr.Close(); }
                catch { }
            }
        }
    }

    private string LoadPostGisWKT(string filename, int id)
    {
        StreamReader sr = null;
        string id_ = id + ";";
        try
        {
            sr = new StreamReader(filename);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line.StartsWith(id_))
                {
                    string[] words = line.Split(';');
                    if (words[2] == id.ToString())
                    {
                        return words[3].Substring(1, words[3].Length - 2);
                    }
                }
            }
            return String.Empty;
        }
        catch
        {
            return String.Empty;
        }
        finally
        {
            if (sr != null)
            {
                try { sr.Close(); }
                catch { }
            }
        }
    }

    public int[] Ids
    {
        get
        {
            if (String.IsNullOrEmpty(_rootPath) && String.IsNullOrEmpty(SpatialReferenceCollection.p4DatabaseConnection))
            {
                return null;
            }

            if (SpatialReferenceCollection.p4DatabaseConnection == "#")
            {
                return CsvRows
                    .Select(r => r.Split(';')[1])
                    .Select(i => int.Parse(i))
                    .ToArray();
            }
            else
            {
                DBConnection conn = new DBConnection();
                if (String.IsNullOrEmpty(SpatialReferenceCollection.p4DatabaseConnection))
                {
                    conn.OleDbConnectionMDB = _rootPath + @"\p4.mdb";
                }
                else
                {
                    conn.OleDbConnectionMDB = SpatialReferenceCollection.p4DatabaseConnection;
                }

                DataTable tab = conn.Select("select PROJ_EPSG from P4");
                if (tab == null)
                {
                    throw new Exception(conn.errorMessage);
                }

                List<int> ret = new List<int>();
                foreach (DataRow row in tab.Rows)
                {
                    if (row["PROJ_EPSG"] == null || row["PROJ_EPSG"] == DBNull.Value)
                    {
                        continue;
                    }

                    ret.Add(Convert.ToInt32(row["PROJ_EPSG"]));
                }

                return ret.ToArray();
            }
        }
    }

    #region IDbSchemaProvider

    public DataSet DbSchema
    {
        get
        {
            DataSet dataset = new DataSet();

            DataTable table = new DataTable("p4");
            table.Columns.Add(new DataColumn("@ID", typeof(int)));
            table.Columns.Add(new DataColumn("PROJ_EPSG", typeof(int)));
            table.Columns.Add(new DataColumn("PROJ_P4:2000", typeof(string)));
            table.Columns.Add(new DataColumn("PROJ_DESCRIPTION:2000", typeof(string)));

            foreach (string csvRow in CsvRows)
            {
                string[] csvCols = csvRow.Split(';');
                DataRow row = table.NewRow();
                row["@ID"] = int.Parse(csvCols[0]);
                row["PROJ_EPSG"] = int.Parse(csvCols[1]);
                row["PROJ_P4:2000"] = csvCols[2];
                row["PROJ_DESCRIPTION:2000"] = csvCols[3];

                table.Rows.Add(row);
            }

            dataset.Tables.Add(table);

            return dataset;
        }
    }

    public string DbConnectionString => p4DatabaseConnection;

    public string[] CsvRows
    {
        get
        {
            string csv =
@"1;2000;+proj=tmerc +lat_0=0 +lon_0=-62 +k=0.999500 +x_0=400000 +y_0=0 +ellps=clrk80 +units=m +no_defs;Anguilla 1957 / British West Indies Grid
2;2001;+proj=tmerc +lat_0=0 +lon_0=-62 +k=0.999500 +x_0=400000 +y_0=0 +ellps=clrk80 +units=m +no_defs;Antigua 1943 / British West Indies Grid
3;2002;+proj=tmerc +lat_0=0 +lon_0=-62 +k=0.999500 +x_0=400000 +y_0=0 +ellps=clrk80 +towgs84=725,685,536,0,0,0,0 +units=m +no_defs;Dominica 1945 / British West Indies Grid
4;2003;+proj=tmerc +lat_0=0 +lon_0=-62 +k=0.999500 +x_0=400000 +y_0=0 +ellps=clrk80 +towgs84=72,213.7,93,0,0,0,0 +units=m +no_defs;Grenada 1953 / British West Indies Grid
5;2004;+proj=tmerc +lat_0=0 +lon_0=-62 +k=0.999500 +x_0=400000 +y_0=0 +ellps=clrk80 +towgs84=174,359,365,0,0,0,0 +units=m +no_defs;Montserrat 1958 / British West Indies Grid
6;2005;+proj=tmerc +lat_0=0 +lon_0=-62 +k=0.999500 +x_0=400000 +y_0=0 +ellps=clrk80 +units=m +no_defs;St. Kitts 1955 / British West Indies Grid
7;2006;+proj=tmerc +lat_0=0 +lon_0=-62 +k=0.999500 +x_0=400000 +y_0=0 +ellps=clrk80 +towgs84=-149,128,296,0,0,0,0 +units=m +no_defs;St. Lucia 1955 / British West Indies Grid
8;2007;+proj=tmerc +lat_0=0 +lon_0=-62 +k=0.999500 +x_0=400000 +y_0=0 +ellps=clrk80 +towgs84=195.671,332.517,274.607,0,0,0,0 +units=m +no_defs;St. Vincent 45 / British West Indies Grid
9;2008;+proj=tmerc +lat_0=0 +lon_0=-55.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=clrk66 +units=m +no_defs;NAD27(CGQ77) / SCoPQ zone 2
10;2009;+proj=tmerc +lat_0=0 +lon_0=-58.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=clrk66 +units=m +no_defs;NAD27(CGQ77) / SCoPQ zone 3
11;2010;+proj=tmerc +lat_0=0 +lon_0=-61.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=clrk66 +units=m +no_defs;NAD27(CGQ77) / SCoPQ zone 4
12;2011;+proj=tmerc +lat_0=0 +lon_0=-64.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=clrk66 +units=m +no_defs;NAD27(CGQ77) / SCoPQ zone 5
13;2012;+proj=tmerc +lat_0=0 +lon_0=-67.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=clrk66 +units=m +no_defs;NAD27(CGQ77) / SCoPQ zone 6
14;2013;+proj=tmerc +lat_0=0 +lon_0=-70.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=clrk66 +units=m +no_defs;NAD27(CGQ77) / SCoPQ zone 7
15;2014;+proj=tmerc +lat_0=0 +lon_0=-73.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=clrk66 +units=m +no_defs;NAD27(CGQ77) / SCoPQ zone 8
16;2015;+proj=tmerc +lat_0=0 +lon_0=-76.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=clrk66 +units=m +no_defs;NAD27(CGQ77) / SCoPQ zone 9
17;2016;+proj=tmerc +lat_0=0 +lon_0=-79.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=clrk66 +units=m +no_defs;NAD27(CGQ77) / SCoPQ zone 10
18;2017;+proj=tmerc +lat_0=0 +lon_0=-73.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=clrk66 +units=m +no_defs;NAD27(76) / MTM zone 8
19;2018;+proj=tmerc +lat_0=0 +lon_0=-76.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=clrk66 +units=m +no_defs;NAD27(76) / MTM zone 9
20;2019;+proj=tmerc +lat_0=0 +lon_0=-79.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=clrk66 +units=m +no_defs;NAD27(76) / MTM zone 10
21;2020;+proj=tmerc +lat_0=0 +lon_0=-82.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=clrk66 +units=m +no_defs;NAD27(76) / MTM zone 11
22;2021;+proj=tmerc +lat_0=0 +lon_0=-81 +k=0.999900 +x_0=304800 +y_0=0 +ellps=clrk66 +units=m +no_defs;NAD27(76) / MTM zone 12
23;2022;+proj=tmerc +lat_0=0 +lon_0=-84 +k=0.999900 +x_0=304800 +y_0=0 +ellps=clrk66 +units=m +no_defs;NAD27(76) / MTM zone 13
24;2023;+proj=tmerc +lat_0=0 +lon_0=-87 +k=0.999900 +x_0=304800 +y_0=0 +ellps=clrk66 +units=m +no_defs;NAD27(76) / MTM zone 14
25;2024;+proj=tmerc +lat_0=0 +lon_0=-90 +k=0.999900 +x_0=304800 +y_0=0 +ellps=clrk66 +units=m +no_defs;NAD27(76) / MTM zone 15
26;2025;+proj=tmerc +lat_0=0 +lon_0=-93 +k=0.999900 +x_0=304800 +y_0=0 +ellps=clrk66 +units=m +no_defs;NAD27(76) / MTM zone 16
27;2026;+proj=tmerc +lat_0=0 +lon_0=-96 +k=0.999900 +x_0=304800 +y_0=0 +ellps=clrk66 +units=m +no_defs;NAD27(76) / MTM zone 17
28;2027;+proj=utm +zone=15 +ellps=clrk66 +units=m +no_defs;NAD27(76) / UTM zone 15N
29;2028;+proj=utm +zone=16 +ellps=clrk66 +units=m +no_defs;NAD27(76) / UTM zone 16N
30;2029;+proj=utm +zone=17 +ellps=clrk66 +units=m +no_defs;NAD27(76) / UTM zone 17N
31;2030;+proj=utm +zone=18 +ellps=clrk66 +units=m +no_defs;NAD27(76) / UTM zone 18N
32;2031;+proj=utm +zone=17 +ellps=clrk66 +units=m +no_defs;NAD27(CGQ77) / UTM zone 17N
33;2032;+proj=utm +zone=18 +ellps=clrk66 +units=m +no_defs;NAD27(CGQ77) / UTM zone 18N
34;2033;+proj=utm +zone=19 +ellps=clrk66 +units=m +no_defs;NAD27(CGQ77) / UTM zone 19N
35;2034;+proj=utm +zone=20 +ellps=clrk66 +units=m +no_defs;NAD27(CGQ77) / UTM zone 20N
36;2035;+proj=utm +zone=21 +ellps=clrk66 +units=m +no_defs;NAD27(CGQ77) / UTM zone 21N
37;2036;+proj=sterea +lat_0=46.5 +lon_0=-66.5 +k=0.999912 +x_0=2500000 +y_0=7500000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NAD83(CSRS98) / New Brunswick Stereo (deprecated)
38;2037;+proj=utm +zone=19 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NAD83(CSRS98) / UTM zone 19N (deprecated)
39;2038;+proj=utm +zone=20 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NAD83(CSRS98) / UTM zone 20N (deprecated)
40;2039;+proj=tmerc +lat_0=31.73439361111111 +lon_0=35.20451694444445 +k=1.000007 +x_0=219529.584 +y_0=626907.39 +ellps=GRS80 +towgs84=-48,55,52,0,0,0,0 +units=m +no_defs;Israel / Israeli TM Grid
41;2040;+proj=utm +zone=30 +ellps=clrk80 +towgs84=-125,53,467,0,0,0,0 +units=m +no_defs;Locodjo 1965 / UTM zone 30N
42;2041;+proj=utm +zone=30 +ellps=clrk80 +towgs84=-124.76,53,466.79,0,0,0,0 +units=m +no_defs;Abidjan 1987 / UTM zone 30N
43;2042;+proj=utm +zone=29 +ellps=clrk80 +towgs84=-125,53,467,0,0,0,0 +units=m +no_defs;Locodjo 1965 / UTM zone 29N
44;2043;+proj=utm +zone=29 +ellps=clrk80 +towgs84=-124.76,53,466.79,0,0,0,0 +units=m +no_defs;Abidjan 1987 / UTM zone 29N
45;2044;+proj=tmerc +lat_0=0 +lon_0=105 +k=1.000000 +x_0=18500000 +y_0=0 +ellps=krass +towgs84=-17.51,-108.32,-62.39,0,0,0,0 +units=m +no_defs;Hanoi 1972 / Gauss-Kruger zone 18
46;2045;+proj=tmerc +lat_0=0 +lon_0=111 +k=1.000000 +x_0=19500000 +y_0=0 +ellps=krass +towgs84=-17.51,-108.32,-62.39,0,0,0,0 +units=m +no_defs;Hanoi 1972 / Gauss-Kruger zone 19
47;2046;+ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;Hartebeesthoek94 / Lo15
48;2047;+ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;Hartebeesthoek94 / Lo17
49;2048;+ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;Hartebeesthoek94 / Lo19
50;2049;+ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;Hartebeesthoek94 / Lo21
51;2050;+ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;Hartebeesthoek94 / Lo23
52;2051;+ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;Hartebeesthoek94 / Lo25
53;2052;+ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;Hartebeesthoek94 / Lo27
54;2053;+ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;Hartebeesthoek94 / Lo29
55;2054;+ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;Hartebeesthoek94 / Lo31
56;2055;+ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;Hartebeesthoek94 / Lo33
57;2056;+proj=somerc +lat_0=46.95240555555556 +lon_0=7.439583333333333 +x_0=2600000 +y_0=1200000 +ellps=bessel +towgs84=674.374,15.056,405.346,0,0,0,0 +units=m +no_defs;CH1903+ / LV95
58;2057;+proj=omerc +lat_0=27.51882880555555 +lonc=52.60353916666667 +alpha=0.5716611944444444 +k=0.999895934 +x_0=658377.437 +y_0=3044969.194 +ellps=intl +towgs84=-133.63,-157.5,-158.62,0,0,0,0 +units=m +no_defs;Rassadiran / Nakhl e Taqi
59;2058;+proj=utm +zone=38 +ellps=intl +units=m +no_defs;ED50(ED77) / UTM zone 38N
60;2059;+proj=utm +zone=39 +ellps=intl +units=m +no_defs;ED50(ED77) / UTM zone 39N
61;2060;+proj=utm +zone=40 +ellps=intl +units=m +no_defs;ED50(ED77) / UTM zone 40N
62;2061;+proj=utm +zone=41 +ellps=intl +units=m +no_defs;ED50(ED77) / UTM zone 41N
63;2062;+proj=lcc +lat_1=40 +lat_0=40 +lon_0=0 +k_0=0.9988085293 +x_0=600000 +y_0=600000 +a=6378298.3 +b=6356657.142669562 +pm=madrid +units=m +no_defs;Madrid 1870 (Madrid) / Spain
64;2063;+proj=utm +zone=28 +a=6378249.2 +b=6356515 +towgs84=-23,259,-9,0,0,0,0 +units=m +no_defs;Dabola 1981 / UTM zone 28N
65;2064;+proj=utm +zone=29 +a=6378249.2 +b=6356515 +towgs84=-23,259,-9,0,0,0,0 +units=m +no_defs;Dabola 1981 / UTM zone 29N
66;2065;+proj=krovak +lat_0=49.5 +lon_0=42.5 +alpha=30.28813972222222 +k=0.9999 +x_0=0 +y_0=0 +ellps=bessel +pm=ferro +units=m +no_defs;S-JTSK (Ferro) / Krovak
67;2066;+proj=cass +lat_0=11.25217861111111 +lon_0=-60.68600888888889 +x_0=37718.66159325 +y_0=36209.91512952 +a=6378293.645208759 +b=6356617.987679838 +to_meter=0.201166195164 +no_defs;Mount Dillon / Tobago Grid
68;2067;+proj=utm +zone=20 +ellps=intl +units=m +no_defs;Naparima 1955 / UTM zone 20N
69;2068;+proj=tmerc +lat_0=0 +lon_0=9 +k=0.999900 +x_0=200000 +y_0=0 +ellps=intl +units=m +no_defs;ELD79 / Libya zone 5
70;2069;+proj=tmerc +lat_0=0 +lon_0=11 +k=0.999900 +x_0=200000 +y_0=0 +ellps=intl +units=m +no_defs;ELD79 / Libya zone 6
71;2070;+proj=tmerc +lat_0=0 +lon_0=13 +k=0.999900 +x_0=200000 +y_0=0 +ellps=intl +units=m +no_defs;ELD79 / Libya zone 7
72;2071;+proj=tmerc +lat_0=0 +lon_0=15 +k=0.999900 +x_0=200000 +y_0=0 +ellps=intl +units=m +no_defs;ELD79 / Libya zone 8
73;2072;+proj=tmerc +lat_0=0 +lon_0=17 +k=0.999900 +x_0=200000 +y_0=0 +ellps=intl +units=m +no_defs;ELD79 / Libya zone 9
74;2073;+proj=tmerc +lat_0=0 +lon_0=19 +k=0.999900 +x_0=200000 +y_0=0 +ellps=intl +units=m +no_defs;ELD79 / Libya zone 10
75;2074;+proj=tmerc +lat_0=0 +lon_0=21 +k=0.999900 +x_0=200000 +y_0=0 +ellps=intl +units=m +no_defs;ELD79 / Libya zone 11
76;2075;+proj=tmerc +lat_0=0 +lon_0=23 +k=0.999900 +x_0=200000 +y_0=0 +ellps=intl +units=m +no_defs;ELD79 / Libya zone 12
77;2076;+proj=tmerc +lat_0=0 +lon_0=25 +k=0.999900 +x_0=200000 +y_0=0 +ellps=intl +units=m +no_defs;ELD79 / Libya zone 13
78;2077;+proj=utm +zone=32 +ellps=intl +units=m +no_defs;ELD79 / UTM zone 32N
79;2078;+proj=utm +zone=33 +ellps=intl +units=m +no_defs;ELD79 / UTM zone 33N
80;2079;+proj=utm +zone=34 +ellps=intl +units=m +no_defs;ELD79 / UTM zone 34N
81;2080;+proj=utm +zone=35 +ellps=intl +units=m +no_defs;ELD79 / UTM zone 35N
82;2081;+proj=tmerc +lat_0=-90 +lon_0=-69 +k=1.000000 +x_0=2500000 +y_0=0 +ellps=intl +units=m +no_defs;Chos Malal 1914 / Argentina zone 2
83;2082;+proj=tmerc +lat_0=-90 +lon_0=-69 +k=1.000000 +x_0=2500000 +y_0=0 +ellps=intl +towgs84=27.5,14,186.4,0,0,0,0 +units=m +no_defs;Pampa del Castillo / Argentina zone 2
84;2083;+proj=tmerc +lat_0=-90 +lon_0=-69 +k=1.000000 +x_0=2500000 +y_0=0 +ellps=intl +units=m +no_defs;Hito XVIII 1963 / Argentina zone 2
85;2084;+proj=utm +zone=19 +south +ellps=intl +units=m +no_defs;Hito XVIII 1963 / UTM zone 19S
86;2085;+proj=lcc +lat_1=22.35 +lat_0=22.35 +lon_0=-81 +k_0=0.99993602 +x_0=500000 +y_0=280296.016 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / Cuba Norte
87;2086;+proj=lcc +lat_1=20.71666666666667 +lat_0=20.71666666666667 +lon_0=-76.83333333333333 +k_0=0.99994848 +x_0=500000 +y_0=229126.939 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / Cuba Sur
88;2087;+proj=tmerc +lat_0=0 +lon_0=12 +k=0.999600 +x_0=500000 +y_0=0 +ellps=intl +units=m +no_defs;ELD79 / TM 12 NE
89;2088;+proj=tmerc +lat_0=0 +lon_0=11 +k=0.999600 +x_0=500000 +y_0=0 +a=6378249.2 +b=6356515 +units=m +no_defs;Carthage / TM 11 NE
90;2089;+proj=utm +zone=38 +ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;Yemen NGN96 / UTM zone 38N
91;2090;+proj=utm +zone=39 +ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;Yemen NGN96 / UTM zone 39N
92;2091;+proj=tmerc +lat_0=0 +lon_0=45 +k=1.000000 +x_0=8500000 +y_0=0 +ellps=krass +towgs84=-76,-138,67,0,0,0,0 +units=m +no_defs;South Yemen / Gauss Kruger zone 8 (deprecated)
93;2092;+proj=tmerc +lat_0=0 +lon_0=51 +k=1.000000 +x_0=9500000 +y_0=0 +ellps=krass +towgs84=-76,-138,67,0,0,0,0 +units=m +no_defs;South Yemen / Gauss Kruger zone 9 (deprecated)
94;2093;+proj=tmerc +lat_0=0 +lon_0=106 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +towgs84=-17.51,-108.32,-62.39,0,0,0,0 +units=m +no_defs;Hanoi 1972 / GK 106 NE
95;2094;+proj=tmerc +lat_0=0 +lon_0=106 +k=0.999600 +x_0=500000 +y_0=0 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / TM 106 NE
96;2095;+proj=utm +zone=28 +ellps=intl +towgs84=-173,253,27,0,0,0,0 +units=m +no_defs;Bissau / UTM zone 28N
97;2096;+proj=tmerc +lat_0=38 +lon_0=129 +k=1.000000 +x_0=200000 +y_0=500000 +ellps=bessel +units=m +no_defs;Korean 1985 / Korea East Belt
98;2097;+proj=tmerc +lat_0=38 +lon_0=127 +k=1.000000 +x_0=200000 +y_0=500000 +ellps=bessel +units=m +no_defs;Korean 1985 / Korea Central Belt
99;2098;+proj=tmerc +lat_0=38 +lon_0=125 +k=1.000000 +x_0=200000 +y_0=500000 +ellps=bessel +units=m +no_defs;Korean 1985 / Korea West Belt
100;2099;+proj=cass +lat_0=25.38236111111111 +lon_0=50.76138888888889 +x_0=100000 +y_0=100000 +ellps=helmert +units=m +no_defs;Qatar 1948 / Qatar Grid
101;2100;+proj=tmerc +lat_0=0 +lon_0=24 +k=0.999600 +x_0=500000 +y_0=0 +ellps=GRS80 +towgs84=-199.87,74.79,246.62,0,0,0,0 +units=m +no_defs;GGRS87 / Greek Grid
102;2101;+proj=lcc +lat_1=10.16666666666667 +lat_0=10.16666666666667 +lon_0=-71.60561777777777 +k_0=1 +x_0=0 +y_0=-52684.972 +ellps=intl +units=m +no_defs;Lake / Maracaibo Grid M1
103;2102;+proj=lcc +lat_1=10.16666666666667 +lat_0=10.16666666666667 +lon_0=-71.60561777777777 +k_0=1 +x_0=200000 +y_0=147315.028 +ellps=intl +units=m +no_defs;Lake / Maracaibo Grid
104;2103;+proj=lcc +lat_1=10.16666666666667 +lat_0=10.16666666666667 +lon_0=-71.60561777777777 +k_0=1 +x_0=500000 +y_0=447315.028 +ellps=intl +units=m +no_defs;Lake / Maracaibo Grid M3
105;2104;+proj=lcc +lat_1=10.16666666666667 +lat_0=10.16666666666667 +lon_0=-71.60561777777777 +k_0=1 +x_0=-17044 +y_0=-23139.97 +ellps=intl +units=m +no_defs;Lake / Maracaibo La Rosa Grid
106;2105;+proj=tmerc +lat_0=-36.87972222222222 +lon_0=174.7641666666667 +k=0.999900 +x_0=400000 +y_0=800000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / Mount Eden Circuit 2000
107;2106;+proj=tmerc +lat_0=-37.76111111111111 +lon_0=176.4661111111111 +k=1.000000 +x_0=400000 +y_0=800000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / Bay of Plenty Circuit 2000
108;2107;+proj=tmerc +lat_0=-38.62444444444444 +lon_0=177.8855555555556 +k=1.000000 +x_0=400000 +y_0=800000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / Poverty Bay Circuit 2000
109;2108;+proj=tmerc +lat_0=-39.65083333333333 +lon_0=176.6736111111111 +k=1.000000 +x_0=400000 +y_0=800000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / Hawkes Bay Circuit 2000
110;2109;+proj=tmerc +lat_0=-39.13555555555556 +lon_0=174.2277777777778 +k=1.000000 +x_0=400000 +y_0=800000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / Taranaki Circuit 2000
111;2110;+proj=tmerc +lat_0=-39.51222222222222 +lon_0=175.64 +k=1.000000 +x_0=400000 +y_0=800000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / Tuhirangi Circuit 2000
112;2111;+proj=tmerc +lat_0=-40.24194444444444 +lon_0=175.4880555555555 +k=1.000000 +x_0=400000 +y_0=800000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / Wanganui Circuit 2000
113;2112;+proj=tmerc +lat_0=-40.92527777777777 +lon_0=175.6472222222222 +k=1.000000 +x_0=400000 +y_0=800000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / Wairarapa Circuit 2000
114;2113;+proj=tmerc +lat_0=-41.3011111111111 +lon_0=174.7763888888889 +k=1.000000 +x_0=400000 +y_0=800000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / Wellington Circuit 2000
115;2114;+proj=tmerc +lat_0=-40.71472222222223 +lon_0=172.6719444444444 +k=1.000000 +x_0=400000 +y_0=800000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / Collingwood Circuit 2000
116;2115;+proj=tmerc +lat_0=-41.27444444444444 +lon_0=173.2991666666667 +k=1.000000 +x_0=400000 +y_0=800000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / Nelson Circuit 2000
117;2116;+proj=tmerc +lat_0=-41.28972222222222 +lon_0=172.1088888888889 +k=1.000000 +x_0=400000 +y_0=800000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / Karamea Circuit 2000
118;2117;+proj=tmerc +lat_0=-41.81055555555555 +lon_0=171.5811111111111 +k=1.000000 +x_0=400000 +y_0=800000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / Buller Circuit 2000
119;2118;+proj=tmerc +lat_0=-42.33361111111111 +lon_0=171.5497222222222 +k=1.000000 +x_0=400000 +y_0=800000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / Grey Circuit 2000
120;2119;+proj=tmerc +lat_0=-42.68888888888888 +lon_0=173.01 +k=1.000000 +x_0=400000 +y_0=800000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / Amuri Circuit 2000
121;2120;+proj=tmerc +lat_0=-41.54444444444444 +lon_0=173.8019444444444 +k=1.000000 +x_0=400000 +y_0=800000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / Marlborough Circuit 2000
122;2121;+proj=tmerc +lat_0=-42.88611111111111 +lon_0=170.9797222222222 +k=1.000000 +x_0=400000 +y_0=800000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / Hokitika Circuit 2000
123;2122;+proj=tmerc +lat_0=-43.11 +lon_0=170.2608333333333 +k=1.000000 +x_0=400000 +y_0=800000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / Okarito Circuit 2000
124;2123;+proj=tmerc +lat_0=-43.97777777777778 +lon_0=168.6061111111111 +k=1.000000 +x_0=400000 +y_0=800000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / Jacksons Bay Circuit 2000
125;2124;+proj=tmerc +lat_0=-43.59055555555556 +lon_0=172.7269444444445 +k=1.000000 +x_0=400000 +y_0=800000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / Mount Pleasant Circuit 2000
126;2125;+proj=tmerc +lat_0=-43.74861111111111 +lon_0=171.3605555555555 +k=1.000000 +x_0=400000 +y_0=800000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / Gawler Circuit 2000
127;2126;+proj=tmerc +lat_0=-44.40194444444445 +lon_0=171.0572222222222 +k=1.000000 +x_0=400000 +y_0=800000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / Timaru Circuit 2000
128;2127;+proj=tmerc +lat_0=-44.735 +lon_0=169.4675 +k=1.000000 +x_0=400000 +y_0=800000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / Lindis Peak Circuit 2000
129;2128;+proj=tmerc +lat_0=-45.13277777777778 +lon_0=168.3986111111111 +k=1.000000 +x_0=400000 +y_0=800000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / Mount Nicholas Circuit 2000
130;2129;+proj=tmerc +lat_0=-45.56361111111111 +lon_0=167.7386111111111 +k=1.000000 +x_0=400000 +y_0=800000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / Mount York Circuit 2000
131;2130;+proj=tmerc +lat_0=-45.81611111111111 +lon_0=170.6283333333333 +k=1.000000 +x_0=400000 +y_0=800000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / Observation Point Circuit 2000
132;2131;+proj=tmerc +lat_0=-45.86138888888889 +lon_0=170.2825 +k=0.999960 +x_0=400000 +y_0=800000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / North Taieri Circuit 2000
133;2132;+proj=tmerc +lat_0=-46.6 +lon_0=168.3427777777778 +k=1.000000 +x_0=400000 +y_0=800000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / Bluff Circuit 2000
134;2133;+proj=utm +zone=58 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / UTM zone 58S
135;2134;+proj=utm +zone=59 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / UTM zone 59S
136;2135;+proj=utm +zone=60 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / UTM zone 60S
137;2136;+proj=tmerc +lat_0=4.666666666666667 +lon_0=-1 +k=0.999750 +x_0=274319.7391633579 +y_0=0 +a=6378300 +b=6356751.689189189 +towgs84=-199,32,322,0,0,0,0 +to_meter=0.3047997101815088 +no_defs;Accra / Ghana National Grid
138;2137;+proj=tmerc +lat_0=0 +lon_0=-1 +k=0.999600 +x_0=500000 +y_0=0 +a=6378300 +b=6356751.689189189 +towgs84=-199,32,322,0,0,0,0 +units=m +no_defs;Accra / TM 1 NW
139;2138;+proj=lcc +lat_1=60 +lat_2=46 +lat_0=44 +lon_0=-68.5 +x_0=0 +y_0=0 +ellps=clrk66 +units=m +no_defs;NAD27(CGQ77) / Quebec Lambert
140;2139;+proj=tmerc +lat_0=0 +lon_0=-55.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NAD83(CSRS98) / SCoPQ zone 2 (deprecated)
141;2140;+proj=tmerc +lat_0=0 +lon_0=-58.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NAD83(CSRS98) / MTM zone 3 (deprecated)
142;2141;+proj=tmerc +lat_0=0 +lon_0=-61.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NAD83(CSRS98) / MTM zone 4 (deprecated)
143;2142;+proj=tmerc +lat_0=0 +lon_0=-64.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NAD83(CSRS98) / MTM zone 5 (deprecated)
144;2143;+proj=tmerc +lat_0=0 +lon_0=-67.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NAD83(CSRS98) / MTM zone 6 (deprecated)
145;2144;+proj=tmerc +lat_0=0 +lon_0=-70.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NAD83(CSRS98) / MTM zone 7 (deprecated)
146;2145;+proj=tmerc +lat_0=0 +lon_0=-73.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NAD83(CSRS98) / MTM zone 8 (deprecated)
147;2146;+proj=tmerc +lat_0=0 +lon_0=-76.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NAD83(CSRS98) / MTM zone 9 (deprecated)
148;2147;+proj=tmerc +lat_0=0 +lon_0=-79.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NAD83(CSRS98) / MTM zone 10 (deprecated)
149;2148;+proj=utm +zone=21 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NAD83(CSRS98) / UTM zone 21N (deprecated)
150;2149;+proj=utm +zone=18 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NAD83(CSRS98) / UTM zone 18N (deprecated)
151;2150;+proj=utm +zone=17 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NAD83(CSRS98) / UTM zone 17N (deprecated)
152;2151;+proj=utm +zone=13 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NAD83(CSRS98) / UTM zone 13N (deprecated)
153;2152;+proj=utm +zone=12 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NAD83(CSRS98) / UTM zone 12N (deprecated)
154;2153;+proj=utm +zone=11 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NAD83(CSRS98) / UTM zone 11N (deprecated)
155;2154;+proj=lcc +lat_1=49 +lat_2=44 +lat_0=46.5 +lon_0=3 +x_0=700000 +y_0=6600000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;RGF93 / Lambert-93
156;2155;+proj=lcc +lat_1=-14.26666666666667 +lat_0=-14.26666666666667 +lon_0=170 +k_0=1 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +towgs84=-115,118,426,0,0,0,0 +to_meter=0.3048006096012192 +no_defs;American Samoa 1962 / American Samoa Lambert (deprecated)
157;2156;+proj=utm +zone=59 +south +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / UTM zone 59S (deprecated)
158;2157;+proj=tmerc +lat_0=53.5 +lon_0=-8 +k=0.999820 +x_0=600000 +y_0=750000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;IRENET95 / Irish Transverse Mercator
159;2158;+proj=utm +zone=29 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;IRENET95 / UTM zone 29N
160;2159;+proj=tmerc +lat_0=6.666666666666667 +lon_0=-12 +k=1.000000 +x_0=152399.8550907544 +y_0=0 +a=6378300 +b=6356751.689189189 +to_meter=0.3047997101815088 +no_defs;Sierra Leone 1924 / New Colony Grid
161;2160;+proj=tmerc +lat_0=6.666666666666667 +lon_0=-12 +k=1.000000 +x_0=243839.7681452071 +y_0=182879.8261089053 +a=6378300 +b=6356751.689189189 +to_meter=0.3047997101815088 +no_defs;Sierra Leone 1924 / New War Office Grid
162;2161;+proj=utm +zone=28 +ellps=clrk80 +towgs84=-88,4,101,0,0,0,0 +units=m +no_defs;Sierra Leone 1968 / UTM zone 28N
163;2162;+proj=utm +zone=29 +ellps=clrk80 +towgs84=-88,4,101,0,0,0,0 +units=m +no_defs;Sierra Leone 1968 / UTM zone 29N
164;2163;+proj=laea +lat_0=45 +lon_0=-100 +x_0=0 +y_0=0 +a=6370997 +b=6370997 +units=m +no_defs;US National Atlas Equal Area
165;2164;+proj=tmerc +lat_0=0 +lon_0=-5 +k=0.999600 +x_0=500000 +y_0=0 +ellps=clrk80 +towgs84=-125,53,467,0,0,0,0 +units=m +no_defs;Locodjo 1965 / TM 5 NW
166;2165;+proj=tmerc +lat_0=0 +lon_0=-5 +k=0.999600 +x_0=500000 +y_0=0 +ellps=clrk80 +towgs84=-124.76,53,466.79,0,0,0,0 +units=m +no_defs;Abidjan 1987 / TM 5 NW
167;2166;+proj=tmerc +lat_0=0 +lon_0=9 +k=1.000000 +x_0=3500000 +y_0=0 +ellps=krass +towgs84=24,-123,-94,0.02,-0.25,-0.13,1.1 +units=m +no_defs;Pulkovo 1942(83) / Gauss Kruger zone 3 (deprecated)
168;2167;+proj=tmerc +lat_0=0 +lon_0=12 +k=1.000000 +x_0=4500000 +y_0=0 +ellps=krass +towgs84=24,-123,-94,0.02,-0.25,-0.13,1.1 +units=m +no_defs;Pulkovo 1942(83) / Gauss Kruger zone 4 (deprecated)
169;2168;+proj=tmerc +lat_0=0 +lon_0=15 +k=1.000000 +x_0=5500000 +y_0=0 +ellps=krass +towgs84=24,-123,-94,0.02,-0.25,-0.13,1.1 +units=m +no_defs;Pulkovo 1942(83) / Gauss Kruger zone 5 (deprecated)
170;2169;+proj=tmerc +lat_0=49.83333333333334 +lon_0=6.166666666666667 +k=1.000000 +x_0=80000 +y_0=100000 +ellps=intl +towgs84=-193,13.7,-39.3,-0.41,-2.933,2.688,0.43 +units=m +no_defs;Luxembourg 1930 / Gauss
171;2170;+proj=tmerc +lat_0=0 +lon_0=15 +k=0.999900 +x_0=500000 +y_0=0 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI / Slovenia Grid
172;2171;+proj=sterea +lat_0=50.625 +lon_0=21.08333333333333 +k=0.999800 +x_0=4637000 +y_0=5647000 +ellps=krass +towgs84=33.4,-146.6,-76.3,-0.359,-0.053,0.844,-0.84 +units=m +no_defs;Pulkovo 1942(58) / Poland zone I (deprecated)
173;2176;+proj=tmerc +lat_0=0 +lon_0=15 +k=0.999923 +x_0=5500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;ETRS89 / Poland CS2000 zone 5
174;2177;+proj=tmerc +lat_0=0 +lon_0=18 +k=0.999923 +x_0=6500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;ETRS89 / Poland CS2000 zone 6
175;2178;+proj=tmerc +lat_0=0 +lon_0=21 +k=0.999923 +x_0=7500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;ETRS89 / Poland CS2000 zone 7
176;2179;+proj=tmerc +lat_0=0 +lon_0=24 +k=0.999923 +x_0=8500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;ETRS89 / Poland CS2000 zone 8
177;2180;+proj=tmerc +lat_0=0 +lon_0=19 +k=0.999300 +x_0=500000 +y_0=-5300000 +ellps=GRS80 +units=m +no_defs;ETRS89 / Poland CS92
178;2188;+proj=utm +zone=25 +ellps=intl +units=m +no_defs;Azores Occidental 1939 / UTM zone 25N
179;2189;+proj=utm +zone=26 +ellps=intl +towgs84=-104,167,-38,0,0,0,0 +units=m +no_defs;Azores Central 1948 / UTM zone 26N
180;2190;+proj=utm +zone=26 +ellps=intl +towgs84=-203,141,53,0,0,0,0 +units=m +no_defs;Azores Oriental 1940 / UTM zone 26N
181;2191;+proj=utm +zone=28 +ellps=intl +units=m +no_defs;Madeira 1936 / UTM zone 28N (deprecated)
182;2192;+proj=lcc +lat_1=46.8 +lat_0=46.8 +lon_0=2.337229166666667 +k_0=0.99987742 +x_0=600000 +y_0=2200000 +ellps=intl +units=m +no_defs;ED50 / France EuroLambert
183;2193;+proj=tmerc +lat_0=0 +lon_0=173 +k=0.999600 +x_0=1600000 +y_0=10000000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NZGD2000 / New Zealand Transverse Mercator
184;2194;+proj=lcc +lat_1=-14.26666666666667 +lat_0=-14.26666666666667 +lon_0=-170 +k_0=1 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +towgs84=-115,118,426,0,0,0,0 +to_meter=0.3048006096012192 +no_defs;American Samoa 1962 / American Samoa Lambert (deprecated)
185;2195;+proj=utm +zone=2 +south +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / UTM zone 2S
186;2196;+proj=tmerc +lat_0=0 +lon_0=9.5 +k=0.999950 +x_0=200000 +y_0=0 +ellps=GRS80 +units=m +no_defs;ETRS89 / Kp2000 Jutland
187;2197;+proj=tmerc +lat_0=0 +lon_0=12 +k=0.999950 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;ETRS89 / Kp2000 Zealand
188;2198;+proj=tmerc +lat_0=0 +lon_0=15 +k=1.000000 +x_0=900000 +y_0=0 +ellps=GRS80 +units=m +no_defs;ETRS89 / Kp2000 Bornholm
189;2199;+proj=tmerc +lat_0=0 +lon_0=21 +k=1.000000 +x_0=4500000 +y_0=0 +ellps=krass +units=m +no_defs;Albanian 1987 / Gauss Kruger zone 4 (deprecated)
190;2200;+proj=sterea +lat_0=46.5 +lon_0=-66.5 +k=0.999912 +x_0=300000 +y_0=800000 +a=6378135 +b=6356750.304921594 +units=m +no_defs;ATS77 / New Brunswick Stereographic (ATS77)
191;2201;+proj=utm +zone=18 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;REGVEN / UTM zone 18N
192;2202;+proj=utm +zone=19 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;REGVEN / UTM zone 19N
193;2203;+proj=utm +zone=20 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;REGVEN / UTM zone 20N
194;2204;+proj=lcc +lat_1=35.25 +lat_2=36.41666666666666 +lat_0=34.66666666666666 +lon_0=-86 +x_0=609601.2192024384 +y_0=30480.06096012192 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Tennessee
195;2205;+proj=lcc +lat_1=37.96666666666667 +lat_2=38.96666666666667 +lat_0=37.5 +lon_0=-84.25 +x_0=500000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Kentucky North
196;2206;+proj=tmerc +lat_0=0 +lon_0=27 +k=1.000000 +x_0=9500000 +y_0=0 +ellps=intl +units=m +no_defs;ED50 / 3-degree Gauss-Kruger zone 9
197;2207;+proj=tmerc +lat_0=0 +lon_0=30 +k=1.000000 +x_0=10500000 +y_0=0 +ellps=intl +units=m +no_defs;ED50 / 3-degree Gauss-Kruger zone 10
198;2208;+proj=tmerc +lat_0=0 +lon_0=33 +k=1.000000 +x_0=11500000 +y_0=0 +ellps=intl +units=m +no_defs;ED50 / 3-degree Gauss-Kruger zone 11
199;2209;+proj=tmerc +lat_0=0 +lon_0=36 +k=1.000000 +x_0=12500000 +y_0=0 +ellps=intl +units=m +no_defs;ED50 / 3-degree Gauss-Kruger zone 12
200;2210;+proj=tmerc +lat_0=0 +lon_0=39 +k=1.000000 +x_0=13500000 +y_0=0 +ellps=intl +units=m +no_defs;ED50 / 3-degree Gauss-Kruger zone 13
201;2211;+proj=tmerc +lat_0=0 +lon_0=42 +k=1.000000 +x_0=14500000 +y_0=0 +ellps=intl +units=m +no_defs;ED50 / 3-degree Gauss-Kruger zone 14
202;2212;+proj=tmerc +lat_0=0 +lon_0=45 +k=1.000000 +x_0=15500000 +y_0=0 +ellps=intl +units=m +no_defs;ED50 / 3-degree Gauss-Kruger zone 15
203;2213;+proj=tmerc +lat_0=0 +lon_0=30 +k=0.999600 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;ETRS89 / TM 30 NE
204;2214;+proj=tmerc +lat_0=0 +lon_0=10.5 +k=0.999000 +x_0=1000000 +y_0=1000000 +ellps=intl +towgs84=-206.1,-174.7,-87.7,0,0,0,0 +units=m +no_defs;Douala 1948 / AOF west (deprecated)
205;2215;+proj=utm +zone=32 +a=6378249.2 +b=6356515 +towgs84=-70.9,-151.8,-41.4,0,0,0,0 +units=m +no_defs;Manoca 1962 / UTM zone 32N
206;2216;+proj=utm +zone=22 +ellps=intl +units=m +no_defs;Qornoq 1927 / UTM zone 22N
207;2217;+proj=utm +zone=23 +ellps=intl +units=m +no_defs;Qornoq 1927 / UTM zone 23N
208;2219;+proj=utm +zone=19 +a=6378135 +b=6356750.304921594 +units=m +no_defs;ATS77 / UTM zone 19N
209;2220;+proj=utm +zone=20 +a=6378135 +b=6356750.304921594 +units=m +no_defs;ATS77 / UTM zone 20N
210;2222;+proj=tmerc +lat_0=31 +lon_0=-110.1666666666667 +k=0.999900 +x_0=213360 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048 +no_defs;NAD83 / Arizona East (ft)
211;2223;+proj=tmerc +lat_0=31 +lon_0=-111.9166666666667 +k=0.999900 +x_0=213360 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048 +no_defs;NAD83 / Arizona Central (ft)
212;2224;+proj=tmerc +lat_0=31 +lon_0=-113.75 +k=0.999933 +x_0=213360 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048 +no_defs;NAD83 / Arizona West (ft)
213;2225;+proj=lcc +lat_1=41.66666666666666 +lat_2=40 +lat_0=39.33333333333334 +lon_0=-122 +x_0=2000000.0001016 +y_0=500000.0001016001 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / California zone 1 (ftUS)
214;2226;+proj=lcc +lat_1=39.83333333333334 +lat_2=38.33333333333334 +lat_0=37.66666666666666 +lon_0=-122 +x_0=2000000.0001016 +y_0=500000.0001016001 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / California zone 2 (ftUS)
215;2227;+proj=lcc +lat_1=38.43333333333333 +lat_2=37.06666666666667 +lat_0=36.5 +lon_0=-120.5 +x_0=2000000.0001016 +y_0=500000.0001016001 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / California zone 3 (ftUS)
216;2228;+proj=lcc +lat_1=37.25 +lat_2=36 +lat_0=35.33333333333334 +lon_0=-119 +x_0=2000000.0001016 +y_0=500000.0001016001 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / California zone 4 (ftUS)
217;2229;+proj=lcc +lat_1=35.46666666666667 +lat_2=34.03333333333333 +lat_0=33.5 +lon_0=-118 +x_0=2000000.0001016 +y_0=500000.0001016001 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / California zone 5 (ftUS)
218;2230;+proj=lcc +lat_1=33.88333333333333 +lat_2=32.78333333333333 +lat_0=32.16666666666666 +lon_0=-116.25 +x_0=2000000.0001016 +y_0=500000.0001016001 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / California zone 6 (ftUS)
219;2231;+proj=lcc +lat_1=40.78333333333333 +lat_2=39.71666666666667 +lat_0=39.33333333333334 +lon_0=-105.5 +x_0=914401.8288036576 +y_0=304800.6096012192 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Colorado North (ftUS)
220;2232;+proj=lcc +lat_1=39.75 +lat_2=38.45 +lat_0=37.83333333333334 +lon_0=-105.5 +x_0=914401.8288036576 +y_0=304800.6096012192 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Colorado Central (ftUS)
221;2233;+proj=lcc +lat_1=38.43333333333333 +lat_2=37.23333333333333 +lat_0=36.66666666666666 +lon_0=-105.5 +x_0=914401.8288036576 +y_0=304800.6096012192 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Colorado South (ftUS)
222;2234;+proj=lcc +lat_1=41.86666666666667 +lat_2=41.2 +lat_0=40.83333333333334 +lon_0=-72.75 +x_0=304800.6096012192 +y_0=152400.3048006096 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Connecticut (ftUS)
223;2235;+proj=tmerc +lat_0=38 +lon_0=-75.41666666666667 +k=0.999995 +x_0=200000.0001016002 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Delaware (ftUS)
224;2236;+proj=tmerc +lat_0=24.33333333333333 +lon_0=-81 +k=0.999941 +x_0=200000.0001016002 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Florida East (ftUS)
225;2237;+proj=tmerc +lat_0=24.33333333333333 +lon_0=-82 +k=0.999941 +x_0=200000.0001016002 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Florida West (ftUS)
226;2238;+proj=lcc +lat_1=30.75 +lat_2=29.58333333333333 +lat_0=29 +lon_0=-84.5 +x_0=600000 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Florida North (ftUS)
227;2239;+proj=tmerc +lat_0=30 +lon_0=-82.16666666666667 +k=0.999900 +x_0=200000.0001016002 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Georgia East (ftUS)
228;2240;+proj=tmerc +lat_0=30 +lon_0=-84.16666666666667 +k=0.999900 +x_0=699999.9998983998 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Georgia West (ftUS)
229;2241;+proj=tmerc +lat_0=41.66666666666666 +lon_0=-112.1666666666667 +k=0.999947 +x_0=200000.0001016002 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Idaho East (ftUS)
230;2242;+proj=tmerc +lat_0=41.66666666666666 +lon_0=-114 +k=0.999947 +x_0=500000.0001016001 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Idaho Central (ftUS)
231;2243;+proj=tmerc +lat_0=41.66666666666666 +lon_0=-115.75 +k=0.999933 +x_0=800000.0001016001 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Idaho West (ftUS)
232;2244;+proj=tmerc +lat_0=37.5 +lon_0=-85.66666666666667 +k=0.999967 +x_0=99999.99989839978 +y_0=249364.9987299975 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Indiana East (ftUS) (deprecated)
233;2245;+proj=tmerc +lat_0=37.5 +lon_0=-87.08333333333333 +k=0.999967 +x_0=900000 +y_0=249364.9987299975 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Indiana West (ftUS) (deprecated)
234;2246;+proj=lcc +lat_1=37.96666666666667 +lat_2=38.96666666666667 +lat_0=37.5 +lon_0=-84.25 +x_0=500000.0001016001 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Kentucky North (ftUS)
297;2322;+proj=tmerc +lat_0=0 +lon_0=36 +k=1.000000 +x_0=500000 +y_0=0 +ellps=intl +units=m +no_defs;ED50 / TM36
235;2247;+proj=lcc +lat_1=37.93333333333333 +lat_2=36.73333333333333 +lat_0=36.33333333333334 +lon_0=-85.75 +x_0=500000.0001016001 +y_0=500000.0001016001 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Kentucky South (ftUS)
236;2248;+proj=lcc +lat_1=39.45 +lat_2=38.3 +lat_0=37.66666666666666 +lon_0=-77 +x_0=399999.9998983998 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Maryland (ftUS)
237;2249;+proj=lcc +lat_1=42.68333333333333 +lat_2=41.71666666666667 +lat_0=41 +lon_0=-71.5 +x_0=200000.0001016002 +y_0=750000 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Massachusetts Mainland (ftUS)
238;2250;+proj=lcc +lat_1=41.48333333333333 +lat_2=41.28333333333333 +lat_0=41 +lon_0=-70.5 +x_0=500000.0001016001 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Massachusetts Island (ftUS)
239;2251;+proj=lcc +lat_1=47.08333333333334 +lat_2=45.48333333333333 +lat_0=44.78333333333333 +lon_0=-87 +x_0=7999999.999968001 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048 +no_defs;NAD83 / Michigan North (ft)
240;2252;+proj=lcc +lat_1=45.7 +lat_2=44.18333333333333 +lat_0=43.31666666666667 +lon_0=-84.36666666666666 +x_0=5999999.999976001 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048 +no_defs;NAD83 / Michigan Central (ft)
241;2253;+proj=lcc +lat_1=43.66666666666666 +lat_2=42.1 +lat_0=41.5 +lon_0=-84.36666666666666 +x_0=3999999.999984 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048 +no_defs;NAD83 / Michigan South (ft)
242;2254;+proj=tmerc +lat_0=29.5 +lon_0=-88.83333333333333 +k=0.999950 +x_0=300000.0000000001 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Mississippi East (ftUS)
243;2255;+proj=tmerc +lat_0=29.5 +lon_0=-90.33333333333333 +k=0.999950 +x_0=699999.9998983998 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Mississippi West (ftUS)
244;2256;+proj=lcc +lat_1=49 +lat_2=45 +lat_0=44.25 +lon_0=-109.5 +x_0=599999.9999976 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048 +no_defs;NAD83 / Montana (ft)
245;2257;+proj=tmerc +lat_0=31 +lon_0=-104.3333333333333 +k=0.999909 +x_0=165000 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / New Mexico East (ftUS)
246;2258;+proj=tmerc +lat_0=31 +lon_0=-106.25 +k=0.999900 +x_0=500000.0001016001 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / New Mexico Central (ftUS)
247;2259;+proj=tmerc +lat_0=31 +lon_0=-107.8333333333333 +k=0.999917 +x_0=830000.0001016001 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / New Mexico West (ftUS)
248;2260;+proj=tmerc +lat_0=38.83333333333334 +lon_0=-74.5 +k=0.999900 +x_0=150000 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / New York East (ftUS)
249;2261;+proj=tmerc +lat_0=40 +lon_0=-76.58333333333333 +k=0.999938 +x_0=249999.9998983998 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / New York Central (ftUS)
250;2262;+proj=tmerc +lat_0=40 +lon_0=-78.58333333333333 +k=0.999938 +x_0=350000.0001016001 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / New York West (ftUS)
251;2263;+proj=lcc +lat_1=41.03333333333333 +lat_2=40.66666666666666 +lat_0=40.16666666666666 +lon_0=-74 +x_0=300000.0000000001 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / New York Long Island (ftUS)
252;2264;+proj=lcc +lat_1=36.16666666666666 +lat_2=34.33333333333334 +lat_0=33.75 +lon_0=-79 +x_0=609601.2192024384 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / North Carolina (ftUS)
253;2265;+proj=lcc +lat_1=48.73333333333333 +lat_2=47.43333333333333 +lat_0=47 +lon_0=-100.5 +x_0=599999.9999976 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048 +no_defs;NAD83 / North Dakota North (ft)
254;2266;+proj=lcc +lat_1=47.48333333333333 +lat_2=46.18333333333333 +lat_0=45.66666666666666 +lon_0=-100.5 +x_0=599999.9999976 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048 +no_defs;NAD83 / North Dakota South (ft)
255;2267;+proj=lcc +lat_1=36.76666666666667 +lat_2=35.56666666666667 +lat_0=35 +lon_0=-98 +x_0=600000 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Oklahoma North (ftUS)
256;2268;+proj=lcc +lat_1=35.23333333333333 +lat_2=33.93333333333333 +lat_0=33.33333333333334 +lon_0=-98 +x_0=600000 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Oklahoma South (ftUS)
257;2269;+proj=lcc +lat_1=46 +lat_2=44.33333333333334 +lat_0=43.66666666666666 +lon_0=-120.5 +x_0=2500000.0001424 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048 +no_defs;NAD83 / Oregon North (ft)
258;2270;+proj=lcc +lat_1=44 +lat_2=42.33333333333334 +lat_0=41.66666666666666 +lon_0=-120.5 +x_0=1500000.0001464 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048 +no_defs;NAD83 / Oregon South (ft)
259;2271;+proj=lcc +lat_1=41.95 +lat_2=40.88333333333333 +lat_0=40.16666666666666 +lon_0=-77.75 +x_0=600000 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Pennsylvania North (ftUS)
260;2272;+proj=lcc +lat_1=40.96666666666667 +lat_2=39.93333333333333 +lat_0=39.33333333333334 +lon_0=-77.75 +x_0=600000 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Pennsylvania South (ftUS)
261;2273;+proj=lcc +lat_1=34.83333333333334 +lat_2=32.5 +lat_0=31.83333333333333 +lon_0=-81 +x_0=609600 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048 +no_defs;NAD83 / South Carolina (ft)
262;2274;+proj=lcc +lat_1=36.41666666666666 +lat_2=35.25 +lat_0=34.33333333333334 +lon_0=-86 +x_0=600000 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Tennessee (ftUS)
263;2275;+proj=lcc +lat_1=36.18333333333333 +lat_2=34.65 +lat_0=34 +lon_0=-101.5 +x_0=200000.0001016002 +y_0=999999.9998983998 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Texas North (ftUS)
264;2276;+proj=lcc +lat_1=33.96666666666667 +lat_2=32.13333333333333 +lat_0=31.66666666666667 +lon_0=-98.5 +x_0=600000 +y_0=2000000.0001016 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Texas North Central (ftUS)
265;2277;+proj=lcc +lat_1=31.88333333333333 +lat_2=30.11666666666667 +lat_0=29.66666666666667 +lon_0=-100.3333333333333 +x_0=699999.9998983998 +y_0=3000000 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Texas Central (ftUS)
266;2278;+proj=lcc +lat_1=30.28333333333333 +lat_2=28.38333333333333 +lat_0=27.83333333333333 +lon_0=-99 +x_0=600000 +y_0=3999999.9998984 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Texas South Central (ftUS)
267;2279;+proj=lcc +lat_1=27.83333333333333 +lat_2=26.16666666666667 +lat_0=25.66666666666667 +lon_0=-98.5 +x_0=300000.0000000001 +y_0=5000000.0001016 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Texas South (ftUS)
268;2280;+proj=lcc +lat_1=41.78333333333333 +lat_2=40.71666666666667 +lat_0=40.33333333333334 +lon_0=-111.5 +x_0=500000.0001504 +y_0=999999.9999960001 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048 +no_defs;NAD83 / Utah North (ft)
269;2281;+proj=lcc +lat_1=40.65 +lat_2=39.01666666666667 +lat_0=38.33333333333334 +lon_0=-111.5 +x_0=500000.0001504 +y_0=1999999.999992 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048 +no_defs;NAD83 / Utah Central (ft)
270;2282;+proj=lcc +lat_1=38.35 +lat_2=37.21666666666667 +lat_0=36.66666666666666 +lon_0=-111.5 +x_0=500000.0001504 +y_0=2999999.999988 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048 +no_defs;NAD83 / Utah South (ft)
271;2283;+proj=lcc +lat_1=39.2 +lat_2=38.03333333333333 +lat_0=37.66666666666666 +lon_0=-78.5 +x_0=3500000.0001016 +y_0=2000000.0001016 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Virginia North (ftUS)
272;2284;+proj=lcc +lat_1=37.96666666666667 +lat_2=36.76666666666667 +lat_0=36.33333333333334 +lon_0=-78.5 +x_0=3500000.0001016 +y_0=999999.9998983998 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Virginia South (ftUS)
273;2285;+proj=lcc +lat_1=48.73333333333333 +lat_2=47.5 +lat_0=47 +lon_0=-120.8333333333333 +x_0=500000.0001016001 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Washington North (ftUS)
274;2286;+proj=lcc +lat_1=47.33333333333334 +lat_2=45.83333333333334 +lat_0=45.33333333333334 +lon_0=-120.5 +x_0=500000.0001016001 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Washington South (ftUS)
275;2287;+proj=lcc +lat_1=46.76666666666667 +lat_2=45.56666666666667 +lat_0=45.16666666666666 +lon_0=-90 +x_0=600000 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Wisconsin North (ftUS)
276;2288;+proj=lcc +lat_1=45.5 +lat_2=44.25 +lat_0=43.83333333333334 +lon_0=-90 +x_0=600000 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Wisconsin Central (ftUS)
277;2289;+proj=lcc +lat_1=44.06666666666667 +lat_2=42.73333333333333 +lat_0=42 +lon_0=-90 +x_0=600000 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Wisconsin South (ftUS)
278;2290;+proj=sterea +lat_0=47.25 +lon_0=-63 +k=0.999912 +x_0=700000 +y_0=400000 +a=6378135 +b=6356750.304921594 +units=m +no_defs;ATS77 / Prince Edward Isl. Stereographic (ATS77)
279;2291;+proj=sterea +lat_0=47.25 +lon_0=-63 +k=0.999912 +x_0=400000 +y_0=800000 +a=6378135 +b=6356750.304921594 +units=m +no_defs;NAD83(CSRS98) / Prince Edward Isl. Stereographic (NAD83) (deprecated)
280;2292;+proj=sterea +lat_0=47.25 +lon_0=-63 +k=0.999912 +x_0=400000 +y_0=800000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;NAD83(CSRS98) / Prince Edward Isl. Stereographic (NAD83) (deprecated)
281;2294;+proj=tmerc +lat_0=0 +lon_0=-61.5 +k=0.999900 +x_0=4500000 +y_0=0 +a=6378135 +b=6356750.304921594 +units=m +no_defs;ATS77 / MTM Nova Scotia zone 4
282;2295;+proj=tmerc +lat_0=0 +lon_0=-64.5 +k=0.999900 +x_0=5500000 +y_0=0 +a=6378135 +b=6356750.304921594 +units=m +no_defs;ATS77 / MTM Nova Scotia zone 5
283;2308;+proj=tmerc +lat_0=0 +lon_0=109 +k=0.999600 +x_0=500000 +y_0=10000000 +ellps=bessel +units=m +no_defs;Batavia / TM 109 SE
284;2309;+proj=tmerc +lat_0=0 +lon_0=116 +k=0.999600 +x_0=500000 +y_0=10000000 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / TM 116 SE
285;2310;+proj=tmerc +lat_0=0 +lon_0=132 +k=0.999600 +x_0=500000 +y_0=10000000 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / TM 132 SE
286;2311;+proj=tmerc +lat_0=0 +lon_0=6 +k=0.999600 +x_0=500000 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / TM 6 NE
287;2312;+proj=utm +zone=33 +ellps=clrk80 +units=m +no_defs;Garoua / UTM zone 33N
288;2313;+proj=utm +zone=33 +ellps=clrk80 +units=m +no_defs;Kousseri / UTM zone 33N
289;2314;+proj=cass +lat_0=10.44166666666667 +lon_0=-61.33333333333334 +x_0=86501.46392052001 +y_0=65379.0134283 +a=6378293.645208759 +b=6356617.987679838 +to_meter=0.3047972654 +no_defs;Trinidad 1903 / Trinidad Grid (ftCla)
290;2315;+proj=utm +zone=19 +south +ellps=intl +units=m +no_defs;Campo Inchauspe / UTM zone 19S
291;2316;+proj=utm +zone=20 +south +ellps=intl +units=m +no_defs;Campo Inchauspe / UTM zone 20S
292;2317;+proj=lcc +lat_1=9 +lat_2=3 +lat_0=6 +lon_0=-66 +x_0=1000000 +y_0=1000000 +ellps=intl +units=m +no_defs;PSAD56 / ICN Regional
293;2318;+proj=lcc +lat_1=17 +lat_2=33 +lat_0=25.08951 +lon_0=48 +x_0=0 +y_0=0 +ellps=intl +units=m +no_defs;Ain el Abd / Aramco Lambert
294;2319;+proj=tmerc +lat_0=0 +lon_0=27 +k=1.000000 +x_0=500000 +y_0=0 +ellps=intl +units=m +no_defs;ED50 / TM27
295;2320;+proj=tmerc +lat_0=0 +lon_0=30 +k=1.000000 +x_0=500000 +y_0=0 +ellps=intl +units=m +no_defs;ED50 / TM30
296;2321;+proj=tmerc +lat_0=0 +lon_0=33 +k=1.000000 +x_0=500000 +y_0=0 +ellps=intl +units=m +no_defs;ED50 / TM33
298;2323;+proj=tmerc +lat_0=0 +lon_0=39 +k=1.000000 +x_0=500000 +y_0=0 +ellps=intl +units=m +no_defs;ED50 / TM39
299;2324;+proj=tmerc +lat_0=0 +lon_0=42 +k=1.000000 +x_0=500000 +y_0=0 +ellps=intl +units=m +no_defs;ED50 / TM42
300;2325;+proj=tmerc +lat_0=0 +lon_0=45 +k=1.000000 +x_0=500000 +y_0=0 +ellps=intl +units=m +no_defs;ED50 / TM45
301;2326;+proj=tmerc +lat_0=22.31213333333334 +lon_0=114.1785555555556 +k=1.000000 +x_0=836694.05 +y_0=819069.8 +ellps=intl +towgs84=-162.619,-276.959,-161.764,0.067753,-2.24365,-1.15883,-1.09425 +units=m +no_defs;Hong Kong 1980 Grid System
302;2327;+proj=tmerc +lat_0=0 +lon_0=75 +k=1.000000 +x_0=13500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / Gauss-Kruger zone 13
303;2328;+proj=tmerc +lat_0=0 +lon_0=81 +k=1.000000 +x_0=14500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / Gauss-Kruger zone 14
304;2329;+proj=tmerc +lat_0=0 +lon_0=87 +k=1.000000 +x_0=15500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / Gauss-Kruger zone 15
305;2330;+proj=tmerc +lat_0=0 +lon_0=93 +k=1.000000 +x_0=16500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / Gauss-Kruger zone 16
306;2331;+proj=tmerc +lat_0=0 +lon_0=99 +k=1.000000 +x_0=17500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / Gauss-Kruger zone 17
307;2332;+proj=tmerc +lat_0=0 +lon_0=105 +k=1.000000 +x_0=18500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / Gauss-Kruger zone 18
308;2333;+proj=tmerc +lat_0=0 +lon_0=111 +k=1.000000 +x_0=19500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / Gauss-Kruger zone 19
309;2334;+proj=tmerc +lat_0=0 +lon_0=117 +k=1.000000 +x_0=20500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / Gauss-Kruger zone 20
310;2335;+proj=tmerc +lat_0=0 +lon_0=123 +k=1.000000 +x_0=21500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / Gauss-Kruger zone 21
311;2336;+proj=tmerc +lat_0=0 +lon_0=129 +k=1.000000 +x_0=22500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / Gauss-Kruger zone 22
312;2337;+proj=tmerc +lat_0=0 +lon_0=135 +k=1.000000 +x_0=23500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / Gauss-Kruger zone 23
313;2338;+proj=tmerc +lat_0=0 +lon_0=75 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / Gauss-Kruger CM 75E
314;2339;+proj=tmerc +lat_0=0 +lon_0=81 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / Gauss-Kruger CM 81E
315;2340;+proj=tmerc +lat_0=0 +lon_0=87 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / Gauss-Kruger CM 87E
316;2341;+proj=tmerc +lat_0=0 +lon_0=93 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / Gauss-Kruger CM 93E
317;2342;+proj=tmerc +lat_0=0 +lon_0=99 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / Gauss-Kruger CM 99E
318;2343;+proj=tmerc +lat_0=0 +lon_0=105 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / Gauss-Kruger CM 105E
319;2344;+proj=tmerc +lat_0=0 +lon_0=111 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / Gauss-Kruger CM 111E
320;2345;+proj=tmerc +lat_0=0 +lon_0=117 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / Gauss-Kruger CM 117E
321;2346;+proj=tmerc +lat_0=0 +lon_0=123 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / Gauss-Kruger CM 123E
322;2347;+proj=tmerc +lat_0=0 +lon_0=129 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / Gauss-Kruger CM 129E
323;2348;+proj=tmerc +lat_0=0 +lon_0=135 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / Gauss-Kruger CM 135E
324;2349;+proj=tmerc +lat_0=0 +lon_0=75 +k=1.000000 +x_0=25500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger zone 25
325;2350;+proj=tmerc +lat_0=0 +lon_0=78 +k=1.000000 +x_0=26500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger zone 26
326;2351;+proj=tmerc +lat_0=0 +lon_0=81 +k=1.000000 +x_0=27500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger zone 27
327;2352;+proj=tmerc +lat_0=0 +lon_0=84 +k=1.000000 +x_0=28500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger zone 28
328;2353;+proj=tmerc +lat_0=0 +lon_0=87 +k=1.000000 +x_0=29500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger zone 29
329;2354;+proj=tmerc +lat_0=0 +lon_0=90 +k=1.000000 +x_0=30500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger zone 30
330;2355;+proj=tmerc +lat_0=0 +lon_0=93 +k=1.000000 +x_0=31500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger zone 31
331;2356;+proj=tmerc +lat_0=0 +lon_0=96 +k=1.000000 +x_0=32500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger zone 32
332;2357;+proj=tmerc +lat_0=0 +lon_0=99 +k=1.000000 +x_0=33500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger zone 33
333;2358;+proj=tmerc +lat_0=0 +lon_0=102 +k=1.000000 +x_0=34500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger zone 34
334;2359;+proj=tmerc +lat_0=0 +lon_0=105 +k=1.000000 +x_0=35500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger zone 35
335;2360;+proj=tmerc +lat_0=0 +lon_0=108 +k=1.000000 +x_0=36500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger zone 36
336;2361;+proj=tmerc +lat_0=0 +lon_0=111 +k=1.000000 +x_0=37500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger zone 37
337;2362;+proj=tmerc +lat_0=0 +lon_0=114 +k=1.000000 +x_0=38500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger zone 38
338;2363;+proj=tmerc +lat_0=0 +lon_0=117 +k=1.000000 +x_0=39500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger zone 39
339;2364;+proj=tmerc +lat_0=0 +lon_0=120 +k=1.000000 +x_0=40500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger zone 40
340;2365;+proj=tmerc +lat_0=0 +lon_0=123 +k=1.000000 +x_0=41500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger zone 41
341;2366;+proj=tmerc +lat_0=0 +lon_0=126 +k=1.000000 +x_0=42500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger zone 42
342;2367;+proj=tmerc +lat_0=0 +lon_0=129 +k=1.000000 +x_0=43500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger zone 43
343;2368;+proj=tmerc +lat_0=0 +lon_0=132 +k=1.000000 +x_0=44500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger zone 44
344;2369;+proj=tmerc +lat_0=0 +lon_0=135 +k=1.000000 +x_0=45500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger zone 45
345;2370;+proj=tmerc +lat_0=0 +lon_0=75 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger CM 75E
346;2371;+proj=tmerc +lat_0=0 +lon_0=78 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger CM 78E
347;2372;+proj=tmerc +lat_0=0 +lon_0=81 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger CM 81E
348;2373;+proj=tmerc +lat_0=0 +lon_0=84 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger CM 84E
349;2374;+proj=tmerc +lat_0=0 +lon_0=87 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger CM 87E
350;2375;+proj=tmerc +lat_0=0 +lon_0=90 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger CM 90E
351;2376;+proj=tmerc +lat_0=0 +lon_0=93 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger CM 93E
352;2377;+proj=tmerc +lat_0=0 +lon_0=96 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger CM 96E
353;2378;+proj=tmerc +lat_0=0 +lon_0=99 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger CM 99E
354;2379;+proj=tmerc +lat_0=0 +lon_0=102 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger CM 102E
355;2380;+proj=tmerc +lat_0=0 +lon_0=105 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger CM 105E
356;2381;+proj=tmerc +lat_0=0 +lon_0=108 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger CM 108E
357;2382;+proj=tmerc +lat_0=0 +lon_0=111 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger CM 111E
358;2383;+proj=tmerc +lat_0=0 +lon_0=114 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger CM 114E
359;2384;+proj=tmerc +lat_0=0 +lon_0=117 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger CM 117E
360;2385;+proj=tmerc +lat_0=0 +lon_0=120 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger CM 120E
361;2386;+proj=tmerc +lat_0=0 +lon_0=123 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger CM 123E
362;2387;+proj=tmerc +lat_0=0 +lon_0=126 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger CM 126E
363;2388;+proj=tmerc +lat_0=0 +lon_0=129 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger CM 129E
364;2389;+proj=tmerc +lat_0=0 +lon_0=132 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger CM 132E
365;2390;+proj=tmerc +lat_0=0 +lon_0=135 +k=1.000000 +x_0=500000 +y_0=0 +a=6378140 +b=6356755.288157528 +units=m +no_defs;Xian 1980 / 3-degree Gauss-Kruger CM 135E
366;2391;+proj=tmerc +lat_0=0 +lon_0=21 +k=1.000000 +x_0=1500000 +y_0=0 +ellps=intl +units=m +no_defs;KKJ / Finland zone 1
367;2392;+proj=tmerc +lat_0=0 +lon_0=24 +k=1.000000 +x_0=2500000 +y_0=0 +ellps=intl +units=m +no_defs;KKJ / Finland zone 2
368;2393;+proj=tmerc +lat_0=0 +lon_0=27 +k=1.000000 +x_0=3500000 +y_0=0 +ellps=intl +units=m +no_defs;KKJ / Finland Uniform Coordinate System
369;2394;+proj=tmerc +lat_0=0 +lon_0=30 +k=1.000000 +x_0=4500000 +y_0=0 +ellps=intl +units=m +no_defs;KKJ / Finland zone 4
370;2395;+proj=tmerc +lat_0=0 +lon_0=45 +k=1.000000 +x_0=8500000 +y_0=0 +ellps=krass +towgs84=-76,-138,67,0,0,0,0 +units=m +no_defs;South Yemen / Gauss-Kruger zone 8
371;2396;+proj=tmerc +lat_0=0 +lon_0=51 +k=1.000000 +x_0=9500000 +y_0=0 +ellps=krass +towgs84=-76,-138,67,0,0,0,0 +units=m +no_defs;South Yemen / Gauss-Kruger zone 9
372;2397;+proj=tmerc +lat_0=0 +lon_0=9 +k=1.000000 +x_0=3500000 +y_0=0 +ellps=krass +towgs84=24,-123,-94,0.02,-0.25,-0.13,1.1 +units=m +no_defs;Pulkovo 1942(83) / Gauss-Kruger zone 3
373;2398;+proj=tmerc +lat_0=0 +lon_0=12 +k=1.000000 +x_0=4500000 +y_0=0 +ellps=krass +towgs84=24,-123,-94,0.02,-0.25,-0.13,1.1 +units=m +no_defs;Pulkovo 1942(83) / Gauss-Kruger zone 4
374;2399;+proj=tmerc +lat_0=0 +lon_0=15 +k=1.000000 +x_0=5500000 +y_0=0 +ellps=krass +towgs84=24,-123,-94,0.02,-0.25,-0.13,1.1 +units=m +no_defs;Pulkovo 1942(83) / Gauss-Kruger zone 5
375;2400;+proj=tmerc +lat_0=0 +lon_0=15.80827777777778 +k=1.000000 +x_0=1500000 +y_0=0 +ellps=bessel +units=m +no_defs;RT90 2.5 gon W (deprecated)
376;2401;+proj=tmerc +lat_0=0 +lon_0=75 +k=1.000000 +x_0=25500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger zone 25
377;2402;+proj=tmerc +lat_0=0 +lon_0=78 +k=1.000000 +x_0=26500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger zone 26
378;2403;+proj=tmerc +lat_0=0 +lon_0=81 +k=1.000000 +x_0=27500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger zone 27
379;2404;+proj=tmerc +lat_0=0 +lon_0=84 +k=1.000000 +x_0=28500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger zone 28
380;2405;+proj=tmerc +lat_0=0 +lon_0=87 +k=1.000000 +x_0=29500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger zone 29
381;2406;+proj=tmerc +lat_0=0 +lon_0=90 +k=1.000000 +x_0=30500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger zone 30
382;2407;+proj=tmerc +lat_0=0 +lon_0=93 +k=1.000000 +x_0=31500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger zone 31
383;2408;+proj=tmerc +lat_0=0 +lon_0=96 +k=1.000000 +x_0=32500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger zone 32
384;2409;+proj=tmerc +lat_0=0 +lon_0=99 +k=1.000000 +x_0=33500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger zone 33
385;2410;+proj=tmerc +lat_0=0 +lon_0=102 +k=1.000000 +x_0=34500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger zone 34
386;2411;+proj=tmerc +lat_0=0 +lon_0=105 +k=1.000000 +x_0=35500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger zone 35
387;2412;+proj=tmerc +lat_0=0 +lon_0=108 +k=1.000000 +x_0=36500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger zone 36
388;2413;+proj=tmerc +lat_0=0 +lon_0=111 +k=1.000000 +x_0=37500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger zone 37
389;2414;+proj=tmerc +lat_0=0 +lon_0=114 +k=1.000000 +x_0=38500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger zone 38
390;2415;+proj=tmerc +lat_0=0 +lon_0=117 +k=1.000000 +x_0=39500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger zone 39
391;2416;+proj=tmerc +lat_0=0 +lon_0=120 +k=1.000000 +x_0=40500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger zone 40
392;2417;+proj=tmerc +lat_0=0 +lon_0=123 +k=1.000000 +x_0=41500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger zone 41
393;2418;+proj=tmerc +lat_0=0 +lon_0=126 +k=1.000000 +x_0=42500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger zone 42
394;2419;+proj=tmerc +lat_0=0 +lon_0=129 +k=1.000000 +x_0=43500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger zone 43
395;2420;+proj=tmerc +lat_0=0 +lon_0=132 +k=1.000000 +x_0=44500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger zone 44
396;2421;+proj=tmerc +lat_0=0 +lon_0=135 +k=1.000000 +x_0=45500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger zone 45
397;2422;+proj=tmerc +lat_0=0 +lon_0=75 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger CM 75E
398;2423;+proj=tmerc +lat_0=0 +lon_0=78 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger CM 78E
399;2424;+proj=tmerc +lat_0=0 +lon_0=81 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger CM 81E
400;2425;+proj=tmerc +lat_0=0 +lon_0=84 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger CM 84E
401;2426;+proj=tmerc +lat_0=0 +lon_0=87 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger CM 87E
402;2427;+proj=tmerc +lat_0=0 +lon_0=90 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger CM 90E
403;2428;+proj=tmerc +lat_0=0 +lon_0=93 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger CM 93E
404;2429;+proj=tmerc +lat_0=0 +lon_0=96 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger CM 96E
405;2430;+proj=tmerc +lat_0=0 +lon_0=99 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger CM 99E
406;2431;+proj=tmerc +lat_0=0 +lon_0=102 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger CM 102E
407;2432;+proj=tmerc +lat_0=0 +lon_0=105 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger CM 105E
408;2433;+proj=tmerc +lat_0=0 +lon_0=108 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger CM 108E
409;2434;+proj=tmerc +lat_0=0 +lon_0=111 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger CM 111E
410;2435;+proj=tmerc +lat_0=0 +lon_0=114 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger CM 114E
411;2436;+proj=tmerc +lat_0=0 +lon_0=117 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger CM 117E
412;2437;+proj=tmerc +lat_0=0 +lon_0=120 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger CM 120E
413;2438;+proj=tmerc +lat_0=0 +lon_0=123 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger CM 123E
414;2439;+proj=tmerc +lat_0=0 +lon_0=126 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger CM 126E
415;2440;+proj=tmerc +lat_0=0 +lon_0=129 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger CM 129E
416;2441;+proj=tmerc +lat_0=0 +lon_0=132 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger CM 132E
417;2442;+proj=tmerc +lat_0=0 +lon_0=135 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / 3-degree Gauss-Kruger CM 135E
418;2443;+proj=tmerc +lat_0=33 +lon_0=129.5 +k=0.999900 +x_0=0 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;JGD2000 / Japan Plane Rectangular CS I
419;2444;+proj=tmerc +lat_0=33 +lon_0=131 +k=0.999900 +x_0=0 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;JGD2000 / Japan Plane Rectangular CS II
420;2445;+proj=tmerc +lat_0=36 +lon_0=132.1666666666667 +k=0.999900 +x_0=0 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;JGD2000 / Japan Plane Rectangular CS III
421;2446;+proj=tmerc +lat_0=33 +lon_0=133.5 +k=0.999900 +x_0=0 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;JGD2000 / Japan Plane Rectangular CS IV
422;2447;+proj=tmerc +lat_0=36 +lon_0=134.3333333333333 +k=0.999900 +x_0=0 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;JGD2000 / Japan Plane Rectangular CS V
423;2448;+proj=tmerc +lat_0=36 +lon_0=136 +k=0.999900 +x_0=0 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;JGD2000 / Japan Plane Rectangular CS VI
424;2449;+proj=tmerc +lat_0=36 +lon_0=137.1666666666667 +k=0.999900 +x_0=0 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;JGD2000 / Japan Plane Rectangular CS VII
425;2450;+proj=tmerc +lat_0=36 +lon_0=138.5 +k=0.999900 +x_0=0 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;JGD2000 / Japan Plane Rectangular CS VIII
426;2451;+proj=tmerc +lat_0=36 +lon_0=139.8333333333333 +k=0.999900 +x_0=0 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;JGD2000 / Japan Plane Rectangular CS IX
427;2452;+proj=tmerc +lat_0=40 +lon_0=140.8333333333333 +k=0.999900 +x_0=0 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;JGD2000 / Japan Plane Rectangular CS X
428;2453;+proj=tmerc +lat_0=44 +lon_0=140.25 +k=0.999900 +x_0=0 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;JGD2000 / Japan Plane Rectangular CS XI
429;2454;+proj=tmerc +lat_0=44 +lon_0=142.25 +k=0.999900 +x_0=0 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;JGD2000 / Japan Plane Rectangular CS XII
430;2455;+proj=tmerc +lat_0=44 +lon_0=144.25 +k=0.999900 +x_0=0 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;JGD2000 / Japan Plane Rectangular CS XIII
431;2456;+proj=tmerc +lat_0=26 +lon_0=142 +k=0.999900 +x_0=0 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;JGD2000 / Japan Plane Rectangular CS XIV
432;2457;+proj=tmerc +lat_0=26 +lon_0=127.5 +k=0.999900 +x_0=0 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;JGD2000 / Japan Plane Rectangular CS XV
433;2458;+proj=tmerc +lat_0=26 +lon_0=124 +k=0.999900 +x_0=0 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;JGD2000 / Japan Plane Rectangular CS XVI
434;2459;+proj=tmerc +lat_0=26 +lon_0=131 +k=0.999900 +x_0=0 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;JGD2000 / Japan Plane Rectangular CS XVII
435;2460;+proj=tmerc +lat_0=20 +lon_0=136 +k=0.999900 +x_0=0 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;JGD2000 / Japan Plane Rectangular CS XVIII
436;2461;+proj=tmerc +lat_0=26 +lon_0=154 +k=0.999900 +x_0=0 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;JGD2000 / Japan Plane Rectangular CS XIX
437;2462;+proj=tmerc +lat_0=0 +lon_0=21 +k=1.000000 +x_0=4500000 +y_0=0 +ellps=krass +units=m +no_defs;Albanian 1987 / Gauss-Kruger zone 4
438;2463;+proj=tmerc +lat_0=0 +lon_0=21 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger CM 21E
439;2464;+proj=tmerc +lat_0=0 +lon_0=27 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger CM 27E
440;2465;+proj=tmerc +lat_0=0 +lon_0=33 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger CM 33E
441;2466;+proj=tmerc +lat_0=0 +lon_0=39 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger CM 39E
442;2467;+proj=tmerc +lat_0=0 +lon_0=45 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger CM 45E
443;2468;+proj=tmerc +lat_0=0 +lon_0=51 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger CM 51E
444;2469;+proj=tmerc +lat_0=0 +lon_0=57 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger CM 57E
445;2470;+proj=tmerc +lat_0=0 +lon_0=63 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger CM 63E
446;2471;+proj=tmerc +lat_0=0 +lon_0=69 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger CM 69E
447;2472;+proj=tmerc +lat_0=0 +lon_0=75 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger CM 75E
448;2473;+proj=tmerc +lat_0=0 +lon_0=81 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger CM 81E
449;2474;+proj=tmerc +lat_0=0 +lon_0=87 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger CM 87E
450;2475;+proj=tmerc +lat_0=0 +lon_0=93 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger CM 93E
451;2476;+proj=tmerc +lat_0=0 +lon_0=99 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger CM 99E
452;2477;+proj=tmerc +lat_0=0 +lon_0=105 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger CM 105E
453;2478;+proj=tmerc +lat_0=0 +lon_0=111 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger CM 111E
454;2479;+proj=tmerc +lat_0=0 +lon_0=117 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger CM 117E
455;2480;+proj=tmerc +lat_0=0 +lon_0=123 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger CM 123E
456;2481;+proj=tmerc +lat_0=0 +lon_0=129 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger CM 129E
457;2482;+proj=tmerc +lat_0=0 +lon_0=135 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger CM 135E
458;2483;+proj=tmerc +lat_0=0 +lon_0=141 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger CM 141E
459;2484;+proj=tmerc +lat_0=0 +lon_0=147 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger CM 147E
460;2485;+proj=tmerc +lat_0=0 +lon_0=153 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger CM 153E
461;2486;+proj=tmerc +lat_0=0 +lon_0=159 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger CM 159E
462;2487;+proj=tmerc +lat_0=0 +lon_0=165 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger CM 165E
463;2488;+proj=tmerc +lat_0=0 +lon_0=171 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger CM 171E
464;2489;+proj=tmerc +lat_0=0 +lon_0=177 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger CM 177E
465;2490;+proj=tmerc +lat_0=0 +lon_0=-177 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger CM 177W
466;2491;+proj=tmerc +lat_0=0 +lon_0=-171 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger CM 171W
467;2492;+proj=tmerc +lat_0=0 +lon_0=9 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 9E
468;2493;+proj=tmerc +lat_0=0 +lon_0=15 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 15E
469;2494;+proj=tmerc +lat_0=0 +lon_0=21 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 21E
470;2495;+proj=tmerc +lat_0=0 +lon_0=27 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 27E
471;2496;+proj=tmerc +lat_0=0 +lon_0=33 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 33E
472;2497;+proj=tmerc +lat_0=0 +lon_0=39 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 39E
473;2498;+proj=tmerc +lat_0=0 +lon_0=45 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 45E
474;2499;+proj=tmerc +lat_0=0 +lon_0=51 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 51E
475;2500;+proj=tmerc +lat_0=0 +lon_0=57 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 57E
476;2501;+proj=tmerc +lat_0=0 +lon_0=63 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 63E
477;2502;+proj=tmerc +lat_0=0 +lon_0=69 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 69E
478;2503;+proj=tmerc +lat_0=0 +lon_0=75 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 75E
479;2504;+proj=tmerc +lat_0=0 +lon_0=81 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 81E
480;2505;+proj=tmerc +lat_0=0 +lon_0=87 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 87E
481;2506;+proj=tmerc +lat_0=0 +lon_0=93 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 93E
482;2507;+proj=tmerc +lat_0=0 +lon_0=99 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 99E
483;2508;+proj=tmerc +lat_0=0 +lon_0=105 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 105E
484;2509;+proj=tmerc +lat_0=0 +lon_0=111 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 111E
485;2510;+proj=tmerc +lat_0=0 +lon_0=117 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 117E
486;2511;+proj=tmerc +lat_0=0 +lon_0=123 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 123E
487;2512;+proj=tmerc +lat_0=0 +lon_0=129 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 129E
488;2513;+proj=tmerc +lat_0=0 +lon_0=135 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 135E
489;2514;+proj=tmerc +lat_0=0 +lon_0=141 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 141E
490;2515;+proj=tmerc +lat_0=0 +lon_0=147 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 147E
491;2516;+proj=tmerc +lat_0=0 +lon_0=153 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 153E
492;2517;+proj=tmerc +lat_0=0 +lon_0=159 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 159E
493;2518;+proj=tmerc +lat_0=0 +lon_0=165 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 165E
494;2519;+proj=tmerc +lat_0=0 +lon_0=171 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 171E
495;2520;+proj=tmerc +lat_0=0 +lon_0=177 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 177E
496;2521;+proj=tmerc +lat_0=0 +lon_0=-177 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 177W
497;2522;+proj=tmerc +lat_0=0 +lon_0=-171 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger CM 171W
498;2523;+proj=tmerc +lat_0=0 +lon_0=21 +k=1.000000 +x_0=7500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 7
499;2524;+proj=tmerc +lat_0=0 +lon_0=24 +k=1.000000 +x_0=8500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 8
500;2525;+proj=tmerc +lat_0=0 +lon_0=27 +k=1.000000 +x_0=9500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 9
501;2526;+proj=tmerc +lat_0=0 +lon_0=30 +k=1.000000 +x_0=10500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 10
502;2527;+proj=tmerc +lat_0=0 +lon_0=33 +k=1.000000 +x_0=11500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 11
503;2528;+proj=tmerc +lat_0=0 +lon_0=36 +k=1.000000 +x_0=12500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 12
504;2529;+proj=tmerc +lat_0=0 +lon_0=39 +k=1.000000 +x_0=13500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 13
505;2530;+proj=tmerc +lat_0=0 +lon_0=42 +k=1.000000 +x_0=14500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 14
506;2531;+proj=tmerc +lat_0=0 +lon_0=45 +k=1.000000 +x_0=15500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 15
507;2532;+proj=tmerc +lat_0=0 +lon_0=48 +k=1.000000 +x_0=16500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 16
508;2533;+proj=tmerc +lat_0=0 +lon_0=51 +k=1.000000 +x_0=17500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 17
509;2534;+proj=tmerc +lat_0=0 +lon_0=54 +k=1.000000 +x_0=18500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 18
510;2535;+proj=tmerc +lat_0=0 +lon_0=57 +k=1.000000 +x_0=19500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 19
511;2536;+proj=tmerc +lat_0=0 +lon_0=60 +k=1.000000 +x_0=20500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 20
512;2537;+proj=tmerc +lat_0=0 +lon_0=63 +k=1.000000 +x_0=21500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 21
513;2538;+proj=tmerc +lat_0=0 +lon_0=66 +k=1.000000 +x_0=22500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 22
514;2539;+proj=tmerc +lat_0=0 +lon_0=69 +k=1.000000 +x_0=23500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 23
515;2540;+proj=tmerc +lat_0=0 +lon_0=72 +k=1.000000 +x_0=24500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 24
516;2541;+proj=tmerc +lat_0=0 +lon_0=75 +k=1.000000 +x_0=25500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 25
517;2542;+proj=tmerc +lat_0=0 +lon_0=78 +k=1.000000 +x_0=26500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 26
518;2543;+proj=tmerc +lat_0=0 +lon_0=81 +k=1.000000 +x_0=27500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 27
519;2544;+proj=tmerc +lat_0=0 +lon_0=84 +k=1.000000 +x_0=28500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 28
520;2545;+proj=tmerc +lat_0=0 +lon_0=87 +k=1.000000 +x_0=29500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 29
521;2546;+proj=tmerc +lat_0=0 +lon_0=90 +k=1.000000 +x_0=30500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 30
522;2547;+proj=tmerc +lat_0=0 +lon_0=93 +k=1.000000 +x_0=31500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 31
523;2548;+proj=tmerc +lat_0=0 +lon_0=96 +k=1.000000 +x_0=32500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 32
524;2549;+proj=tmerc +lat_0=0 +lon_0=99 +k=1.000000 +x_0=33500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 33
525;2550;+proj=utm +zone=50 +south +ellps=bessel +towgs84=-404.78,685.68,45.47,0,0,0,0 +units=m +no_defs;Samboja / UTM zone 50S (deprecated)
526;2551;+proj=tmerc +lat_0=0 +lon_0=102 +k=1.000000 +x_0=34500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 34
527;2552;+proj=tmerc +lat_0=0 +lon_0=105 +k=1.000000 +x_0=35500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 35
528;2553;+proj=tmerc +lat_0=0 +lon_0=108 +k=1.000000 +x_0=36500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 36
529;2554;+proj=tmerc +lat_0=0 +lon_0=111 +k=1.000000 +x_0=37500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 37
530;2555;+proj=tmerc +lat_0=0 +lon_0=114 +k=1.000000 +x_0=38500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 38
531;2556;+proj=tmerc +lat_0=0 +lon_0=117 +k=1.000000 +x_0=39500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 39
532;2557;+proj=tmerc +lat_0=0 +lon_0=120 +k=1.000000 +x_0=40500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 40
533;2558;+proj=tmerc +lat_0=0 +lon_0=123 +k=1.000000 +x_0=41500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 41
534;2559;+proj=tmerc +lat_0=0 +lon_0=126 +k=1.000000 +x_0=42500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 42
535;2560;+proj=tmerc +lat_0=0 +lon_0=129 +k=1.000000 +x_0=43500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 43
536;2561;+proj=tmerc +lat_0=0 +lon_0=132 +k=1.000000 +x_0=44500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 44
537;2562;+proj=tmerc +lat_0=0 +lon_0=135 +k=1.000000 +x_0=45500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 45
538;2563;+proj=tmerc +lat_0=0 +lon_0=138 +k=1.000000 +x_0=46500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 46
539;2564;+proj=tmerc +lat_0=0 +lon_0=141 +k=1.000000 +x_0=47500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 47
540;2565;+proj=tmerc +lat_0=0 +lon_0=144 +k=1.000000 +x_0=48500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 48
541;2566;+proj=tmerc +lat_0=0 +lon_0=147 +k=1.000000 +x_0=49500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 49
542;2567;+proj=tmerc +lat_0=0 +lon_0=150 +k=1.000000 +x_0=50500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 50
543;2568;+proj=tmerc +lat_0=0 +lon_0=153 +k=1.000000 +x_0=51500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 51
544;2569;+proj=tmerc +lat_0=0 +lon_0=156 +k=1.000000 +x_0=52500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 52
545;2570;+proj=tmerc +lat_0=0 +lon_0=159 +k=1.000000 +x_0=53500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 53
546;2571;+proj=tmerc +lat_0=0 +lon_0=162 +k=1.000000 +x_0=54500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 54
547;2572;+proj=tmerc +lat_0=0 +lon_0=165 +k=1.000000 +x_0=55500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 55
548;2573;+proj=tmerc +lat_0=0 +lon_0=168 +k=1.000000 +x_0=56500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 56
549;2574;+proj=tmerc +lat_0=0 +lon_0=171 +k=1.000000 +x_0=57500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 57
550;2575;+proj=tmerc +lat_0=0 +lon_0=174 +k=1.000000 +x_0=58500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 58
551;2576;+proj=tmerc +lat_0=0 +lon_0=177 +k=1.000000 +x_0=59500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 59
552;2577;+proj=tmerc +lat_0=0 +lon_0=180 +k=1.000000 +x_0=60000000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 60 (deprecated)
553;2578;+proj=tmerc +lat_0=0 +lon_0=-177 +k=1.000000 +x_0=61500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 61
554;2579;+proj=tmerc +lat_0=0 +lon_0=-174 +k=1.000000 +x_0=62500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 62
555;2580;+proj=tmerc +lat_0=0 +lon_0=-171 +k=1.000000 +x_0=63500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 63
556;2581;+proj=tmerc +lat_0=0 +lon_0=-168 +k=1.000000 +x_0=64500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 64
557;2582;+proj=tmerc +lat_0=0 +lon_0=21 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 21E
558;2583;+proj=tmerc +lat_0=0 +lon_0=24 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 24E
559;2584;+proj=tmerc +lat_0=0 +lon_0=27 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 27E
560;2585;+proj=tmerc +lat_0=0 +lon_0=30 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 30E
561;2586;+proj=tmerc +lat_0=0 +lon_0=33 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 33E
562;2587;+proj=tmerc +lat_0=0 +lon_0=36 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 36E
563;2588;+proj=tmerc +lat_0=0 +lon_0=39 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 39E
564;2589;+proj=tmerc +lat_0=0 +lon_0=42 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 42E
565;2590;+proj=tmerc +lat_0=0 +lon_0=45 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 45E
566;2591;+proj=tmerc +lat_0=0 +lon_0=48 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 48E
567;2592;+proj=tmerc +lat_0=0 +lon_0=51 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 51E
568;2593;+proj=tmerc +lat_0=0 +lon_0=54 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 54E
569;2594;+proj=tmerc +lat_0=0 +lon_0=57 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 57E
570;2595;+proj=tmerc +lat_0=0 +lon_0=60 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 60E
571;2596;+proj=tmerc +lat_0=0 +lon_0=63 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 63E
572;2597;+proj=tmerc +lat_0=0 +lon_0=66 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 66E
573;2598;+proj=tmerc +lat_0=0 +lon_0=69 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 69E
574;2599;+proj=tmerc +lat_0=0 +lon_0=72 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 72E
575;2600;+proj=tmerc +lat_0=0 +lon_0=24 +k=0.999800 +x_0=500000 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;Lietuvos Koordinoei Sistema 1994 (deprecated)
576;2601;+proj=tmerc +lat_0=0 +lon_0=75 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 75E
577;2602;+proj=tmerc +lat_0=0 +lon_0=78 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 78E
578;2603;+proj=tmerc +lat_0=0 +lon_0=81 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 81E
579;2604;+proj=tmerc +lat_0=0 +lon_0=84 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 84E
580;2605;+proj=tmerc +lat_0=0 +lon_0=87 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 87E
581;2606;+proj=tmerc +lat_0=0 +lon_0=90 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 90E
582;2607;+proj=tmerc +lat_0=0 +lon_0=93 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 93E
583;2608;+proj=tmerc +lat_0=0 +lon_0=96 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 96E
584;2609;+proj=tmerc +lat_0=0 +lon_0=99 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 99E
585;2610;+proj=tmerc +lat_0=0 +lon_0=102 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 102E
586;2611;+proj=tmerc +lat_0=0 +lon_0=105 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 105E
587;2612;+proj=tmerc +lat_0=0 +lon_0=108 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 108E
588;2613;+proj=tmerc +lat_0=0 +lon_0=111 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 111E
589;2614;+proj=tmerc +lat_0=0 +lon_0=114 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 114E
590;2615;+proj=tmerc +lat_0=0 +lon_0=117 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 117E
591;2616;+proj=tmerc +lat_0=0 +lon_0=120 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 120E
592;2617;+proj=tmerc +lat_0=0 +lon_0=123 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 123E
593;2618;+proj=tmerc +lat_0=0 +lon_0=126 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 126E
594;2619;+proj=tmerc +lat_0=0 +lon_0=129 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 129E
595;2620;+proj=tmerc +lat_0=0 +lon_0=132 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 132E
596;2621;+proj=tmerc +lat_0=0 +lon_0=135 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 135E
597;2622;+proj=tmerc +lat_0=0 +lon_0=138 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 138E
598;2623;+proj=tmerc +lat_0=0 +lon_0=141 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 141E
599;2624;+proj=tmerc +lat_0=0 +lon_0=144 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 144E
600;2625;+proj=tmerc +lat_0=0 +lon_0=147 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 147E
601;2626;+proj=tmerc +lat_0=0 +lon_0=150 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 150E
602;2627;+proj=tmerc +lat_0=0 +lon_0=153 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 153E
603;2628;+proj=tmerc +lat_0=0 +lon_0=156 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 156E
604;2629;+proj=tmerc +lat_0=0 +lon_0=159 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 159E
605;2630;+proj=tmerc +lat_0=0 +lon_0=162 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 162E
606;2631;+proj=tmerc +lat_0=0 +lon_0=165 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 165E
607;2632;+proj=tmerc +lat_0=0 +lon_0=168 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 168E
608;2633;+proj=tmerc +lat_0=0 +lon_0=171 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 171E
609;2634;+proj=tmerc +lat_0=0 +lon_0=174 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 174E
610;2635;+proj=tmerc +lat_0=0 +lon_0=177 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 177E
611;2636;+proj=tmerc +lat_0=0 +lon_0=180 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 180E
612;2637;+proj=tmerc +lat_0=0 +lon_0=-177 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 177W
613;2638;+proj=tmerc +lat_0=0 +lon_0=-174 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 174W
614;2639;+proj=tmerc +lat_0=0 +lon_0=-171 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 171W
615;2640;+proj=tmerc +lat_0=0 +lon_0=-168 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 168W
616;2641;+proj=tmerc +lat_0=0 +lon_0=21 +k=1.000000 +x_0=7500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 7
617;2642;+proj=tmerc +lat_0=0 +lon_0=24 +k=1.000000 +x_0=8500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 8
618;2643;+proj=tmerc +lat_0=0 +lon_0=27 +k=1.000000 +x_0=9500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 9
619;2644;+proj=tmerc +lat_0=0 +lon_0=30 +k=1.000000 +x_0=10500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 10
620;2645;+proj=tmerc +lat_0=0 +lon_0=33 +k=1.000000 +x_0=11500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 11
621;2646;+proj=tmerc +lat_0=0 +lon_0=36 +k=1.000000 +x_0=12500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 12
622;2647;+proj=tmerc +lat_0=0 +lon_0=39 +k=1.000000 +x_0=13500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 13
623;2648;+proj=tmerc +lat_0=0 +lon_0=42 +k=1.000000 +x_0=14500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 14
624;2649;+proj=tmerc +lat_0=0 +lon_0=45 +k=1.000000 +x_0=15500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 15
625;2650;+proj=tmerc +lat_0=0 +lon_0=48 +k=1.000000 +x_0=16500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 16
626;2651;+proj=tmerc +lat_0=0 +lon_0=51 +k=1.000000 +x_0=17500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 17
627;2652;+proj=tmerc +lat_0=0 +lon_0=54 +k=1.000000 +x_0=18500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 18
628;2653;+proj=tmerc +lat_0=0 +lon_0=57 +k=1.000000 +x_0=19500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 19
629;2654;+proj=tmerc +lat_0=0 +lon_0=60 +k=1.000000 +x_0=20500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 20
630;2655;+proj=tmerc +lat_0=0 +lon_0=63 +k=1.000000 +x_0=21500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 21
631;2656;+proj=tmerc +lat_0=0 +lon_0=66 +k=1.000000 +x_0=22500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 22
632;2657;+proj=tmerc +lat_0=0 +lon_0=69 +k=1.000000 +x_0=23500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 23
633;2658;+proj=tmerc +lat_0=0 +lon_0=72 +k=1.000000 +x_0=24500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 24
634;2659;+proj=tmerc +lat_0=0 +lon_0=75 +k=1.000000 +x_0=25500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 25
635;2660;+proj=tmerc +lat_0=0 +lon_0=78 +k=1.000000 +x_0=26500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 26
636;2661;+proj=tmerc +lat_0=0 +lon_0=81 +k=1.000000 +x_0=27500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 27
637;2662;+proj=tmerc +lat_0=0 +lon_0=84 +k=1.000000 +x_0=28500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 28
638;2663;+proj=tmerc +lat_0=0 +lon_0=87 +k=1.000000 +x_0=29500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 29
639;2664;+proj=tmerc +lat_0=0 +lon_0=90 +k=1.000000 +x_0=30500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 30
640;2665;+proj=tmerc +lat_0=0 +lon_0=93 +k=1.000000 +x_0=31500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 31
641;2666;+proj=tmerc +lat_0=0 +lon_0=96 +k=1.000000 +x_0=32500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 32
642;2667;+proj=tmerc +lat_0=0 +lon_0=99 +k=1.000000 +x_0=33500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 33
643;2668;+proj=tmerc +lat_0=0 +lon_0=102 +k=1.000000 +x_0=34500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 34
644;2669;+proj=tmerc +lat_0=0 +lon_0=105 +k=1.000000 +x_0=35500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 35
645;2670;+proj=tmerc +lat_0=0 +lon_0=108 +k=1.000000 +x_0=36500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 36
646;2671;+proj=tmerc +lat_0=0 +lon_0=111 +k=1.000000 +x_0=37500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 37
647;2672;+proj=tmerc +lat_0=0 +lon_0=114 +k=1.000000 +x_0=38500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 38
648;2673;+proj=tmerc +lat_0=0 +lon_0=117 +k=1.000000 +x_0=39500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 39
649;2674;+proj=tmerc +lat_0=0 +lon_0=120 +k=1.000000 +x_0=40500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 40
650;2675;+proj=tmerc +lat_0=0 +lon_0=123 +k=1.000000 +x_0=41500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 41
651;2676;+proj=tmerc +lat_0=0 +lon_0=126 +k=1.000000 +x_0=42500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 42
652;2677;+proj=tmerc +lat_0=0 +lon_0=129 +k=1.000000 +x_0=43500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 43
653;2678;+proj=tmerc +lat_0=0 +lon_0=132 +k=1.000000 +x_0=44500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 44
654;2679;+proj=tmerc +lat_0=0 +lon_0=135 +k=1.000000 +x_0=45500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 45
655;2680;+proj=tmerc +lat_0=0 +lon_0=138 +k=1.000000 +x_0=46500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 46
656;2681;+proj=tmerc +lat_0=0 +lon_0=141 +k=1.000000 +x_0=47500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 47
657;2682;+proj=tmerc +lat_0=0 +lon_0=144 +k=1.000000 +x_0=48500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 48
658;2683;+proj=tmerc +lat_0=0 +lon_0=147 +k=1.000000 +x_0=49500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 49
659;2684;+proj=tmerc +lat_0=0 +lon_0=150 +k=1.000000 +x_0=50500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 50
660;2685;+proj=tmerc +lat_0=0 +lon_0=153 +k=1.000000 +x_0=51500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 51
661;2686;+proj=tmerc +lat_0=0 +lon_0=156 +k=1.000000 +x_0=52500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 52
662;2687;+proj=tmerc +lat_0=0 +lon_0=159 +k=1.000000 +x_0=53500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 53
663;2688;+proj=tmerc +lat_0=0 +lon_0=162 +k=1.000000 +x_0=54500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 54
664;2689;+proj=tmerc +lat_0=0 +lon_0=165 +k=1.000000 +x_0=55500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 55
665;2690;+proj=tmerc +lat_0=0 +lon_0=168 +k=1.000000 +x_0=56500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 56
666;2691;+proj=tmerc +lat_0=0 +lon_0=171 +k=1.000000 +x_0=57500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 57
667;2692;+proj=tmerc +lat_0=0 +lon_0=174 +k=1.000000 +x_0=58500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 58
668;2693;+proj=tmerc +lat_0=0 +lon_0=177 +k=1.000000 +x_0=59500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 59
669;2694;+proj=tmerc +lat_0=0 +lon_0=180 +k=1.000000 +x_0=60000000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 60 (deprecated)
670;2695;+proj=tmerc +lat_0=0 +lon_0=-177 +k=1.000000 +x_0=61500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 61
671;2696;+proj=tmerc +lat_0=0 +lon_0=-174 +k=1.000000 +x_0=62500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 62
672;2697;+proj=tmerc +lat_0=0 +lon_0=-171 +k=1.000000 +x_0=63500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 63
673;2698;+proj=tmerc +lat_0=0 +lon_0=-168 +k=1.000000 +x_0=64500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 64
674;2699;+proj=tmerc +lat_0=0 +lon_0=21 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 21E
675;2700;+proj=tmerc +lat_0=0 +lon_0=24 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 24E
676;2701;+proj=tmerc +lat_0=0 +lon_0=27 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 27E
677;2702;+proj=tmerc +lat_0=0 +lon_0=30 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 30E
678;2703;+proj=tmerc +lat_0=0 +lon_0=33 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 33E
679;2704;+proj=tmerc +lat_0=0 +lon_0=36 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 36E
680;2705;+proj=tmerc +lat_0=0 +lon_0=39 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 39E
681;2706;+proj=tmerc +lat_0=0 +lon_0=42 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 42E
682;2707;+proj=tmerc +lat_0=0 +lon_0=45 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 45E
683;2708;+proj=tmerc +lat_0=0 +lon_0=48 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 48E
684;2709;+proj=tmerc +lat_0=0 +lon_0=51 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 51E
685;2710;+proj=tmerc +lat_0=0 +lon_0=54 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 54E
686;2711;+proj=tmerc +lat_0=0 +lon_0=57 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 57E
687;2712;+proj=tmerc +lat_0=0 +lon_0=60 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 60E
688;2713;+proj=tmerc +lat_0=0 +lon_0=63 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 63E
689;2714;+proj=tmerc +lat_0=0 +lon_0=66 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 66E
690;2715;+proj=tmerc +lat_0=0 +lon_0=69 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 69E
691;2716;+proj=tmerc +lat_0=0 +lon_0=72 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 72E
692;2717;+proj=tmerc +lat_0=0 +lon_0=75 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 75E
693;2718;+proj=tmerc +lat_0=0 +lon_0=78 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 78E
694;2719;+proj=tmerc +lat_0=0 +lon_0=81 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 81E
695;2720;+proj=tmerc +lat_0=0 +lon_0=84 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 84E
696;2721;+proj=tmerc +lat_0=0 +lon_0=87 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 87E
697;2722;+proj=tmerc +lat_0=0 +lon_0=90 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 90E
698;2723;+proj=tmerc +lat_0=0 +lon_0=93 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 93E
699;2724;+proj=tmerc +lat_0=0 +lon_0=96 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 96E
700;2725;+proj=tmerc +lat_0=0 +lon_0=99 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 99E
701;2726;+proj=tmerc +lat_0=0 +lon_0=102 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 102E
702;2727;+proj=tmerc +lat_0=0 +lon_0=105 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 105E
703;2728;+proj=tmerc +lat_0=0 +lon_0=108 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 108E
704;2729;+proj=tmerc +lat_0=0 +lon_0=111 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 111E
705;2730;+proj=tmerc +lat_0=0 +lon_0=114 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 114E
706;2731;+proj=tmerc +lat_0=0 +lon_0=117 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 117E
707;2732;+proj=tmerc +lat_0=0 +lon_0=120 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 120E
708;2733;+proj=tmerc +lat_0=0 +lon_0=123 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 123E
709;2734;+proj=tmerc +lat_0=0 +lon_0=126 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 126E
710;2735;+proj=tmerc +lat_0=0 +lon_0=129 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 129E
711;2736;+proj=utm +zone=36 +south +ellps=clrk66 +units=m +no_defs;Tete / UTM zone 36S
712;2737;+proj=utm +zone=37 +south +ellps=clrk66 +units=m +no_defs;Tete / UTM zone 37S
713;2738;+proj=tmerc +lat_0=0 +lon_0=132 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 132E
714;2739;+proj=tmerc +lat_0=0 +lon_0=135 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 135E
715;2740;+proj=tmerc +lat_0=0 +lon_0=138 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 138E
716;2741;+proj=tmerc +lat_0=0 +lon_0=141 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 141E
717;2742;+proj=tmerc +lat_0=0 +lon_0=144 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 144E
718;2743;+proj=tmerc +lat_0=0 +lon_0=147 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 147E
719;2744;+proj=tmerc +lat_0=0 +lon_0=150 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 150E
720;2745;+proj=tmerc +lat_0=0 +lon_0=153 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 153E
721;2746;+proj=tmerc +lat_0=0 +lon_0=156 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 156E
722;2747;+proj=tmerc +lat_0=0 +lon_0=159 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 159E
723;2748;+proj=tmerc +lat_0=0 +lon_0=162 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 162E
724;2749;+proj=tmerc +lat_0=0 +lon_0=165 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 165E
725;2750;+proj=tmerc +lat_0=0 +lon_0=168 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 168E
726;2751;+proj=tmerc +lat_0=0 +lon_0=171 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 171E
727;2752;+proj=tmerc +lat_0=0 +lon_0=174 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 174E
728;2753;+proj=tmerc +lat_0=0 +lon_0=177 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 177E
729;2754;+proj=tmerc +lat_0=0 +lon_0=180 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 180E
730;2755;+proj=tmerc +lat_0=0 +lon_0=-177 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 177W
731;2756;+proj=tmerc +lat_0=0 +lon_0=-174 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 174W
732;2757;+proj=tmerc +lat_0=0 +lon_0=-171 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 171W
733;2758;+proj=tmerc +lat_0=0 +lon_0=-168 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 168W
734;2759;+proj=tmerc +lat_0=30.5 +lon_0=-85.83333333333333 +k=0.999960 +x_0=200000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Alabama East
735;2760;+proj=tmerc +lat_0=30 +lon_0=-87.5 +k=0.999933 +x_0=600000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Alabama West
736;2761;+proj=tmerc +lat_0=31 +lon_0=-110.1666666666667 +k=0.999900 +x_0=213360 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Arizona East
737;2762;+proj=tmerc +lat_0=31 +lon_0=-111.9166666666667 +k=0.999900 +x_0=213360 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Arizona Central
738;2763;+proj=tmerc +lat_0=31 +lon_0=-113.75 +k=0.999933 +x_0=213360 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Arizona West
739;2764;+proj=lcc +lat_1=36.23333333333333 +lat_2=34.93333333333333 +lat_0=34.33333333333334 +lon_0=-92 +x_0=400000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Arkansas North
740;2765;+proj=lcc +lat_1=34.76666666666667 +lat_2=33.3 +lat_0=32.66666666666666 +lon_0=-92 +x_0=400000 +y_0=400000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Arkansas South
741;2766;+proj=lcc +lat_1=41.66666666666666 +lat_2=40 +lat_0=39.33333333333334 +lon_0=-122 +x_0=2000000 +y_0=500000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / California zone 1
742;2767;+proj=lcc +lat_1=39.83333333333334 +lat_2=38.33333333333334 +lat_0=37.66666666666666 +lon_0=-122 +x_0=2000000 +y_0=500000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / California zone 2
743;2768;+proj=lcc +lat_1=38.43333333333333 +lat_2=37.06666666666667 +lat_0=36.5 +lon_0=-120.5 +x_0=2000000 +y_0=500000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / California zone 3
744;2769;+proj=lcc +lat_1=37.25 +lat_2=36 +lat_0=35.33333333333334 +lon_0=-119 +x_0=2000000 +y_0=500000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / California zone 4
745;2770;+proj=lcc +lat_1=35.46666666666667 +lat_2=34.03333333333333 +lat_0=33.5 +lon_0=-118 +x_0=2000000 +y_0=500000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / California zone 5
746;2771;+proj=lcc +lat_1=33.88333333333333 +lat_2=32.78333333333333 +lat_0=32.16666666666666 +lon_0=-116.25 +x_0=2000000 +y_0=500000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / California zone 6
747;2772;+proj=lcc +lat_1=40.78333333333333 +lat_2=39.71666666666667 +lat_0=39.33333333333334 +lon_0=-105.5 +x_0=914401.8289 +y_0=304800.6096 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Colorado North
748;2773;+proj=lcc +lat_1=39.75 +lat_2=38.45 +lat_0=37.83333333333334 +lon_0=-105.5 +x_0=914401.8289 +y_0=304800.6096 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Colorado Central
749;2774;+proj=lcc +lat_1=38.43333333333333 +lat_2=37.23333333333333 +lat_0=36.66666666666666 +lon_0=-105.5 +x_0=914401.8289 +y_0=304800.6096 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Colorado South
750;2775;+proj=lcc +lat_1=41.86666666666667 +lat_2=41.2 +lat_0=40.83333333333334 +lon_0=-72.75 +x_0=304800.6096 +y_0=152400.3048 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Connecticut
751;2776;+proj=tmerc +lat_0=38 +lon_0=-75.41666666666667 +k=0.999995 +x_0=200000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Delaware
752;2777;+proj=tmerc +lat_0=24.33333333333333 +lon_0=-81 +k=0.999941 +x_0=200000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Florida East
753;2778;+proj=tmerc +lat_0=24.33333333333333 +lon_0=-82 +k=0.999941 +x_0=200000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Florida West
754;2779;+proj=lcc +lat_1=30.75 +lat_2=29.58333333333333 +lat_0=29 +lon_0=-84.5 +x_0=600000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Florida North
755;2780;+proj=tmerc +lat_0=30 +lon_0=-82.16666666666667 +k=0.999900 +x_0=200000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Georgia East
756;2781;+proj=tmerc +lat_0=30 +lon_0=-84.16666666666667 +k=0.999900 +x_0=700000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Georgia West
757;2782;+proj=tmerc +lat_0=18.83333333333333 +lon_0=-155.5 +k=0.999967 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Hawaii zone 1
758;2783;+proj=tmerc +lat_0=20.33333333333333 +lon_0=-156.6666666666667 +k=0.999967 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Hawaii zone 2
759;2784;+proj=tmerc +lat_0=21.16666666666667 +lon_0=-158 +k=0.999990 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Hawaii zone 3
760;2785;+proj=tmerc +lat_0=21.83333333333333 +lon_0=-159.5 +k=0.999990 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Hawaii zone 4
761;2786;+proj=tmerc +lat_0=21.66666666666667 +lon_0=-160.1666666666667 +k=1.000000 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Hawaii zone 5
762;2787;+proj=tmerc +lat_0=41.66666666666666 +lon_0=-112.1666666666667 +k=0.999947 +x_0=200000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Idaho East
763;2788;+proj=tmerc +lat_0=41.66666666666666 +lon_0=-114 +k=0.999947 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Idaho Central
764;2789;+proj=tmerc +lat_0=41.66666666666666 +lon_0=-115.75 +k=0.999933 +x_0=800000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Idaho West
765;2790;+proj=tmerc +lat_0=36.66666666666666 +lon_0=-88.33333333333333 +k=0.999975 +x_0=300000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Illinois East
766;2791;+proj=tmerc +lat_0=36.66666666666666 +lon_0=-90.16666666666667 +k=0.999941 +x_0=700000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Illinois West
767;2792;+proj=tmerc +lat_0=37.5 +lon_0=-85.66666666666667 +k=0.999967 +x_0=100000 +y_0=250000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Indiana East
768;2793;+proj=tmerc +lat_0=37.5 +lon_0=-87.08333333333333 +k=0.999967 +x_0=900000 +y_0=250000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Indiana West
769;2794;+proj=lcc +lat_1=43.26666666666667 +lat_2=42.06666666666667 +lat_0=41.5 +lon_0=-93.5 +x_0=1500000 +y_0=1000000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Iowa North
770;2795;+proj=lcc +lat_1=41.78333333333333 +lat_2=40.61666666666667 +lat_0=40 +lon_0=-93.5 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Iowa South
771;2796;+proj=lcc +lat_1=39.78333333333333 +lat_2=38.71666666666667 +lat_0=38.33333333333334 +lon_0=-98 +x_0=400000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Kansas North
772;2797;+proj=lcc +lat_1=38.56666666666667 +lat_2=37.26666666666667 +lat_0=36.66666666666666 +lon_0=-98.5 +x_0=400000 +y_0=400000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Kansas South
773;2798;+proj=lcc +lat_1=37.96666666666667 +lat_2=38.96666666666667 +lat_0=37.5 +lon_0=-84.25 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Kentucky North
774;2799;+proj=lcc +lat_1=37.93333333333333 +lat_2=36.73333333333333 +lat_0=36.33333333333334 +lon_0=-85.75 +x_0=500000 +y_0=500000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Kentucky South
775;2800;+proj=lcc +lat_1=32.66666666666666 +lat_2=31.16666666666667 +lat_0=30.5 +lon_0=-92.5 +x_0=1000000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Louisiana North
776;2801;+proj=lcc +lat_1=30.7 +lat_2=29.3 +lat_0=28.5 +lon_0=-91.33333333333333 +x_0=1000000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Louisiana South
777;2802;+proj=tmerc +lat_0=43.66666666666666 +lon_0=-68.5 +k=0.999900 +x_0=300000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Maine East
778;2803;+proj=tmerc +lat_0=42.83333333333334 +lon_0=-70.16666666666667 +k=0.999967 +x_0=900000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Maine West
779;2804;+proj=lcc +lat_1=39.45 +lat_2=38.3 +lat_0=37.66666666666666 +lon_0=-77 +x_0=400000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Maryland
780;2805;+proj=lcc +lat_1=42.68333333333333 +lat_2=41.71666666666667 +lat_0=41 +lon_0=-71.5 +x_0=200000 +y_0=750000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Massachusetts Mainland
781;2806;+proj=lcc +lat_1=41.48333333333333 +lat_2=41.28333333333333 +lat_0=41 +lon_0=-70.5 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Massachusetts Island
782;2807;+proj=lcc +lat_1=47.08333333333334 +lat_2=45.48333333333333 +lat_0=44.78333333333333 +lon_0=-87 +x_0=8000000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Michigan North
783;2808;+proj=lcc +lat_1=45.7 +lat_2=44.18333333333333 +lat_0=43.31666666666667 +lon_0=-84.36666666666666 +x_0=6000000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Michigan Central
784;2809;+proj=lcc +lat_1=43.66666666666666 +lat_2=42.1 +lat_0=41.5 +lon_0=-84.36666666666666 +x_0=4000000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Michigan South
785;2810;+proj=lcc +lat_1=48.63333333333333 +lat_2=47.03333333333333 +lat_0=46.5 +lon_0=-93.09999999999999 +x_0=800000 +y_0=100000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Minnesota North
786;2811;+proj=lcc +lat_1=47.05 +lat_2=45.61666666666667 +lat_0=45 +lon_0=-94.25 +x_0=800000 +y_0=100000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Minnesota Central
787;2812;+proj=lcc +lat_1=45.21666666666667 +lat_2=43.78333333333333 +lat_0=43 +lon_0=-94 +x_0=800000 +y_0=100000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Minnesota South
788;2813;+proj=tmerc +lat_0=29.5 +lon_0=-88.83333333333333 +k=0.999950 +x_0=300000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Mississippi East
789;2814;+proj=tmerc +lat_0=29.5 +lon_0=-90.33333333333333 +k=0.999950 +x_0=700000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Mississippi West
790;2815;+proj=tmerc +lat_0=35.83333333333334 +lon_0=-90.5 +k=0.999933 +x_0=250000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Missouri East
791;2816;+proj=tmerc +lat_0=35.83333333333334 +lon_0=-92.5 +k=0.999933 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Missouri Central
792;2817;+proj=tmerc +lat_0=36.16666666666666 +lon_0=-94.5 +k=0.999941 +x_0=850000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Missouri West
793;2818;+proj=lcc +lat_1=49 +lat_2=45 +lat_0=44.25 +lon_0=-109.5 +x_0=600000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Montana
794;2819;+proj=lcc +lat_1=43 +lat_2=40 +lat_0=39.83333333333334 +lon_0=-100 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Nebraska
795;2820;+proj=tmerc +lat_0=34.75 +lon_0=-115.5833333333333 +k=0.999900 +x_0=200000 +y_0=8000000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Nevada East
796;2821;+proj=tmerc +lat_0=34.75 +lon_0=-116.6666666666667 +k=0.999900 +x_0=500000 +y_0=6000000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Nevada Central
797;2822;+proj=tmerc +lat_0=34.75 +lon_0=-118.5833333333333 +k=0.999900 +x_0=800000 +y_0=4000000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Nevada West
798;2823;+proj=tmerc +lat_0=42.5 +lon_0=-71.66666666666667 +k=0.999967 +x_0=300000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / New Hampshire
799;2824;+proj=tmerc +lat_0=38.83333333333334 +lon_0=-74.5 +k=0.999900 +x_0=150000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / New Jersey
800;2825;+proj=tmerc +lat_0=31 +lon_0=-104.3333333333333 +k=0.999909 +x_0=165000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / New Mexico East
801;2826;+proj=tmerc +lat_0=31 +lon_0=-106.25 +k=0.999900 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / New Mexico Central
802;2827;+proj=tmerc +lat_0=31 +lon_0=-107.8333333333333 +k=0.999917 +x_0=830000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / New Mexico West
803;2828;+proj=tmerc +lat_0=38.83333333333334 +lon_0=-74.5 +k=0.999900 +x_0=150000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / New York East
804;2829;+proj=tmerc +lat_0=40 +lon_0=-76.58333333333333 +k=0.999938 +x_0=250000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / New York Central
805;2830;+proj=tmerc +lat_0=40 +lon_0=-78.58333333333333 +k=0.999938 +x_0=350000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / New York West
806;2831;+proj=lcc +lat_1=41.03333333333333 +lat_2=40.66666666666666 +lat_0=40.16666666666666 +lon_0=-74 +x_0=300000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / New York Long Island
807;2832;+proj=lcc +lat_1=48.73333333333333 +lat_2=47.43333333333333 +lat_0=47 +lon_0=-100.5 +x_0=600000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / North Dakota North
808;2833;+proj=lcc +lat_1=47.48333333333333 +lat_2=46.18333333333333 +lat_0=45.66666666666666 +lon_0=-100.5 +x_0=600000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / North Dakota South
809;2834;+proj=lcc +lat_1=41.7 +lat_2=40.43333333333333 +lat_0=39.66666666666666 +lon_0=-82.5 +x_0=600000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Ohio North
810;2835;+proj=lcc +lat_1=40.03333333333333 +lat_2=38.73333333333333 +lat_0=38 +lon_0=-82.5 +x_0=600000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Ohio South
811;2836;+proj=lcc +lat_1=36.76666666666667 +lat_2=35.56666666666667 +lat_0=35 +lon_0=-98 +x_0=600000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Oklahoma North
812;2837;+proj=lcc +lat_1=35.23333333333333 +lat_2=33.93333333333333 +lat_0=33.33333333333334 +lon_0=-98 +x_0=600000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Oklahoma South
813;2838;+proj=lcc +lat_1=46 +lat_2=44.33333333333334 +lat_0=43.66666666666666 +lon_0=-120.5 +x_0=2500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Oregon North
814;2839;+proj=lcc +lat_1=44 +lat_2=42.33333333333334 +lat_0=41.66666666666666 +lon_0=-120.5 +x_0=1500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Oregon South
815;2840;+proj=tmerc +lat_0=41.08333333333334 +lon_0=-71.5 +k=0.999994 +x_0=100000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Rhode Island
816;2841;+proj=lcc +lat_1=45.68333333333333 +lat_2=44.41666666666666 +lat_0=43.83333333333334 +lon_0=-100 +x_0=600000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / South Dakota North
817;2842;+proj=lcc +lat_1=44.4 +lat_2=42.83333333333334 +lat_0=42.33333333333334 +lon_0=-100.3333333333333 +x_0=600000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / South Dakota South
818;2843;+proj=lcc +lat_1=36.41666666666666 +lat_2=35.25 +lat_0=34.33333333333334 +lon_0=-86 +x_0=600000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Tennessee
819;2844;+proj=lcc +lat_1=36.18333333333333 +lat_2=34.65 +lat_0=34 +lon_0=-101.5 +x_0=200000 +y_0=1000000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Texas North
820;2845;+proj=lcc +lat_1=33.96666666666667 +lat_2=32.13333333333333 +lat_0=31.66666666666667 +lon_0=-98.5 +x_0=600000 +y_0=2000000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Texas North Central
821;2846;+proj=lcc +lat_1=31.88333333333333 +lat_2=30.11666666666667 +lat_0=29.66666666666667 +lon_0=-100.3333333333333 +x_0=700000 +y_0=3000000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Texas Central
822;2847;+proj=lcc +lat_1=30.28333333333333 +lat_2=28.38333333333333 +lat_0=27.83333333333333 +lon_0=-99 +x_0=600000 +y_0=4000000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Texas South Central
823;2848;+proj=lcc +lat_1=27.83333333333333 +lat_2=26.16666666666667 +lat_0=25.66666666666667 +lon_0=-98.5 +x_0=300000 +y_0=5000000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Texas South
824;2849;+proj=lcc +lat_1=41.78333333333333 +lat_2=40.71666666666667 +lat_0=40.33333333333334 +lon_0=-111.5 +x_0=500000 +y_0=1000000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Utah North
825;2850;+proj=lcc +lat_1=40.65 +lat_2=39.01666666666667 +lat_0=38.33333333333334 +lon_0=-111.5 +x_0=500000 +y_0=2000000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Utah Central
826;2851;+proj=lcc +lat_1=38.35 +lat_2=37.21666666666667 +lat_0=36.66666666666666 +lon_0=-111.5 +x_0=500000 +y_0=3000000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Utah South
827;2852;+proj=tmerc +lat_0=42.5 +lon_0=-72.5 +k=0.999964 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Vermont
828;2853;+proj=lcc +lat_1=39.2 +lat_2=38.03333333333333 +lat_0=37.66666666666666 +lon_0=-78.5 +x_0=3500000 +y_0=2000000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Virginia North
829;2854;+proj=lcc +lat_1=37.96666666666667 +lat_2=36.76666666666667 +lat_0=36.33333333333334 +lon_0=-78.5 +x_0=3500000 +y_0=1000000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Virginia South
830;2855;+proj=lcc +lat_1=48.73333333333333 +lat_2=47.5 +lat_0=47 +lon_0=-120.8333333333333 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Washington North
831;2856;+proj=lcc +lat_1=47.33333333333334 +lat_2=45.83333333333334 +lat_0=45.33333333333334 +lon_0=-120.5 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Washington South
832;2857;+proj=lcc +lat_1=40.25 +lat_2=39 +lat_0=38.5 +lon_0=-79.5 +x_0=600000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / West Virginia North
833;2858;+proj=lcc +lat_1=38.88333333333333 +lat_2=37.48333333333333 +lat_0=37 +lon_0=-81 +x_0=600000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / West Virginia South
834;2859;+proj=lcc +lat_1=46.76666666666667 +lat_2=45.56666666666667 +lat_0=45.16666666666666 +lon_0=-90 +x_0=600000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Wisconsin North
835;2860;+proj=lcc +lat_1=45.5 +lat_2=44.25 +lat_0=43.83333333333334 +lon_0=-90 +x_0=600000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Wisconsin Central
836;2861;+proj=lcc +lat_1=44.06666666666667 +lat_2=42.73333333333333 +lat_0=42 +lon_0=-90 +x_0=600000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Wisconsin South
837;2862;+proj=tmerc +lat_0=40.5 +lon_0=-105.1666666666667 +k=0.999938 +x_0=200000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Wyoming East
838;2863;+proj=tmerc +lat_0=40.5 +lon_0=-107.3333333333333 +k=0.999938 +x_0=400000 +y_0=100000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Wyoming East Central
839;2864;+proj=tmerc +lat_0=40.5 +lon_0=-108.75 +k=0.999938 +x_0=600000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Wyoming West Central
840;2865;+proj=tmerc +lat_0=40.5 +lon_0=-110.0833333333333 +k=0.999938 +x_0=800000 +y_0=100000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Wyoming West
841;2866;+proj=lcc +lat_1=18.43333333333333 +lat_2=18.03333333333333 +lat_0=17.83333333333333 +lon_0=-66.43333333333334 +x_0=200000 +y_0=200000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Puerto Rico & Virgin Is.
842;2867;+proj=tmerc +lat_0=31 +lon_0=-110.1666666666667 +k=0.999900 +x_0=213360 +y_0=0 +ellps=GRS80 +to_meter=0.3048 +no_defs;NAD83(HARN) / Arizona East (ft)
843;2868;+proj=tmerc +lat_0=31 +lon_0=-111.9166666666667 +k=0.999900 +x_0=213360 +y_0=0 +ellps=GRS80 +to_meter=0.3048 +no_defs;NAD83(HARN) / Arizona Central (ft)
844;2869;+proj=tmerc +lat_0=31 +lon_0=-113.75 +k=0.999933 +x_0=213360 +y_0=0 +ellps=GRS80 +to_meter=0.3048 +no_defs;NAD83(HARN) / Arizona West (ft)
845;2870;+proj=lcc +lat_1=41.66666666666666 +lat_2=40 +lat_0=39.33333333333334 +lon_0=-122 +x_0=2000000.0001016 +y_0=500000.0001016001 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / California zone 1 (ftUS)
846;2871;+proj=lcc +lat_1=39.83333333333334 +lat_2=38.33333333333334 +lat_0=37.66666666666666 +lon_0=-122 +x_0=2000000.0001016 +y_0=500000.0001016001 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / California zone 2 (ftUS)
847;2872;+proj=lcc +lat_1=38.43333333333333 +lat_2=37.06666666666667 +lat_0=36.5 +lon_0=-120.5 +x_0=2000000.0001016 +y_0=500000.0001016001 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / California zone 3 (ftUS)
848;2873;+proj=lcc +lat_1=37.25 +lat_2=36 +lat_0=35.33333333333334 +lon_0=-119 +x_0=2000000.0001016 +y_0=500000.0001016001 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / California zone 4 (ftUS)
849;2874;+proj=lcc +lat_1=35.46666666666667 +lat_2=34.03333333333333 +lat_0=33.5 +lon_0=-118 +x_0=2000000.0001016 +y_0=500000.0001016001 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / California zone 5 (ftUS)
850;2875;+proj=lcc +lat_1=33.88333333333333 +lat_2=32.78333333333333 +lat_0=32.16666666666666 +lon_0=-116.25 +x_0=2000000.0001016 +y_0=500000.0001016001 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / California zone 6 (ftUS)
851;2876;+proj=lcc +lat_1=40.78333333333333 +lat_2=39.71666666666667 +lat_0=39.33333333333334 +lon_0=-105.5 +x_0=914401.8288036576 +y_0=304800.6096012192 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Colorado North (ftUS)
852;2877;+proj=lcc +lat_1=39.75 +lat_2=38.45 +lat_0=37.83333333333334 +lon_0=-105.5 +x_0=914401.8288036576 +y_0=304800.6096012192 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Colorado Central (ftUS)
853;2878;+proj=lcc +lat_1=38.43333333333333 +lat_2=37.23333333333333 +lat_0=36.66666666666666 +lon_0=-105.5 +x_0=914401.8288036576 +y_0=304800.6096012192 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Colorado South (ftUS)
854;2879;+proj=lcc +lat_1=41.86666666666667 +lat_2=41.2 +lat_0=40.83333333333334 +lon_0=-72.75 +x_0=304800.6096012192 +y_0=152400.3048006096 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Connecticut (ftUS)
855;2880;+proj=tmerc +lat_0=38 +lon_0=-75.41666666666667 +k=0.999995 +x_0=200000.0001016002 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Delaware (ftUS)
856;2881;+proj=tmerc +lat_0=24.33333333333333 +lon_0=-81 +k=0.999941 +x_0=200000.0001016002 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Florida East (ftUS)
857;2882;+proj=tmerc +lat_0=24.33333333333333 +lon_0=-82 +k=0.999941 +x_0=200000.0001016002 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Florida West (ftUS)
858;2883;+proj=lcc +lat_1=30.75 +lat_2=29.58333333333333 +lat_0=29 +lon_0=-84.5 +x_0=600000 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Florida North (ftUS)
859;2884;+proj=tmerc +lat_0=30 +lon_0=-82.16666666666667 +k=0.999900 +x_0=200000.0001016002 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Georgia East (ftUS)
860;2885;+proj=tmerc +lat_0=30 +lon_0=-84.16666666666667 +k=0.999900 +x_0=699999.9998983998 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Georgia West (ftUS)
861;2886;+proj=tmerc +lat_0=41.66666666666666 +lon_0=-112.1666666666667 +k=0.999947 +x_0=200000.0001016002 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Idaho East (ftUS)
862;2887;+proj=tmerc +lat_0=41.66666666666666 +lon_0=-114 +k=0.999947 +x_0=500000.0001016001 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Idaho Central (ftUS)
863;2888;+proj=tmerc +lat_0=41.66666666666666 +lon_0=-115.75 +k=0.999933 +x_0=800000.0001016001 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Idaho West (ftUS)
864;2889;+proj=tmerc +lat_0=37.5 +lon_0=-85.66666666666667 +k=0.999967 +x_0=99999.99989839978 +y_0=249364.9987299975 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Indiana East (ftUS) (deprecated)
865;2890;+proj=tmerc +lat_0=37.5 +lon_0=-87.08333333333333 +k=0.999967 +x_0=900000 +y_0=249364.9987299975 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Indiana West (ftUS) (deprecated)
866;2891;+proj=lcc +lat_1=37.96666666666667 +lat_2=38.96666666666667 +lat_0=37.5 +lon_0=-84.25 +x_0=500000.0001016001 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Kentucky North (ftUS)
867;2892;+proj=lcc +lat_1=37.93333333333333 +lat_2=36.73333333333333 +lat_0=36.33333333333334 +lon_0=-85.75 +x_0=500000.0001016001 +y_0=500000.0001016001 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Kentucky South (ftUS)
868;2893;+proj=lcc +lat_1=39.45 +lat_2=38.3 +lat_0=37.66666666666666 +lon_0=-77 +x_0=399999.9998983998 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Maryland (ftUS)
869;2894;+proj=lcc +lat_1=42.68333333333333 +lat_2=41.71666666666667 +lat_0=41 +lon_0=-71.5 +x_0=200000.0001016002 +y_0=750000 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Massachusetts Mainland (ftUS)
870;2895;+proj=lcc +lat_1=41.48333333333333 +lat_2=41.28333333333333 +lat_0=41 +lon_0=-70.5 +x_0=500000.0001016001 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Massachusetts Island (ftUS)
871;2896;+proj=lcc +lat_1=47.08333333333334 +lat_2=45.48333333333333 +lat_0=44.78333333333333 +lon_0=-87 +x_0=7999999.999968001 +y_0=0 +ellps=GRS80 +to_meter=0.3048 +no_defs;NAD83(HARN) / Michigan North (ft)
872;2897;+proj=lcc +lat_1=45.7 +lat_2=44.18333333333333 +lat_0=43.31666666666667 +lon_0=-84.36666666666666 +x_0=5999999.999976001 +y_0=0 +ellps=GRS80 +to_meter=0.3048 +no_defs;NAD83(HARN) / Michigan Central (ft)
873;2898;+proj=lcc +lat_1=43.66666666666666 +lat_2=42.1 +lat_0=41.5 +lon_0=-84.36666666666666 +x_0=3999999.999984 +y_0=0 +ellps=GRS80 +to_meter=0.3048 +no_defs;NAD83(HARN) / Michigan South (ft)
874;2899;+proj=tmerc +lat_0=29.5 +lon_0=-88.83333333333333 +k=0.999950 +x_0=300000.0000000001 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Mississippi East (ftUS)
875;2900;+proj=tmerc +lat_0=29.5 +lon_0=-90.33333333333333 +k=0.999950 +x_0=699999.9998983998 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Mississippi West (ftUS)
876;2901;+proj=lcc +lat_1=49 +lat_2=45 +lat_0=44.25 +lon_0=-109.5 +x_0=599999.9999976 +y_0=0 +ellps=GRS80 +to_meter=0.3048 +no_defs;NAD83(HARN) / Montana (ft)
877;2902;+proj=tmerc +lat_0=31 +lon_0=-104.3333333333333 +k=0.999909 +x_0=165000 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / New Mexico East (ftUS)
878;2903;+proj=tmerc +lat_0=31 +lon_0=-106.25 +k=0.999900 +x_0=500000.0001016001 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / New Mexico Central (ftUS)
879;2904;+proj=tmerc +lat_0=31 +lon_0=-107.8333333333333 +k=0.999917 +x_0=830000.0001016001 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / New Mexico West (ftUS)
880;2905;+proj=tmerc +lat_0=38.83333333333334 +lon_0=-74.5 +k=0.999900 +x_0=150000 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / New York East (ftUS)
881;2906;+proj=tmerc +lat_0=40 +lon_0=-76.58333333333333 +k=0.999938 +x_0=249999.9998983998 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / New York Central (ftUS)
882;2907;+proj=tmerc +lat_0=40 +lon_0=-78.58333333333333 +k=0.999938 +x_0=350000.0001016001 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / New York West (ftUS)
883;2908;+proj=lcc +lat_1=41.03333333333333 +lat_2=40.66666666666666 +lat_0=40.16666666666666 +lon_0=-74 +x_0=300000.0000000001 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / New York Long Island (ftUS)
884;2909;+proj=lcc +lat_1=48.73333333333333 +lat_2=47.43333333333333 +lat_0=47 +lon_0=-100.5 +x_0=599999.9999976 +y_0=0 +ellps=GRS80 +to_meter=0.3048 +no_defs;NAD83(HARN) / North Dakota North (ft)
885;2910;+proj=lcc +lat_1=47.48333333333333 +lat_2=46.18333333333333 +lat_0=45.66666666666666 +lon_0=-100.5 +x_0=599999.9999976 +y_0=0 +ellps=GRS80 +to_meter=0.3048 +no_defs;NAD83(HARN) / North Dakota South (ft)
886;2911;+proj=lcc +lat_1=36.76666666666667 +lat_2=35.56666666666667 +lat_0=35 +lon_0=-98 +x_0=600000 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Oklahoma North (ftUS)
887;2912;+proj=lcc +lat_1=35.23333333333333 +lat_2=33.93333333333333 +lat_0=33.33333333333334 +lon_0=-98 +x_0=600000 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Oklahoma South (ftUS)
888;2913;+proj=lcc +lat_1=46 +lat_2=44.33333333333334 +lat_0=43.66666666666666 +lon_0=-120.5 +x_0=2500000.0001424 +y_0=0 +ellps=GRS80 +to_meter=0.3048 +no_defs;NAD83(HARN) / Oregon North (ft)
889;2914;+proj=lcc +lat_1=44 +lat_2=42.33333333333334 +lat_0=41.66666666666666 +lon_0=-120.5 +x_0=1500000.0001464 +y_0=0 +ellps=GRS80 +to_meter=0.3048 +no_defs;NAD83(HARN) / Oregon South (ft)
890;2915;+proj=lcc +lat_1=36.41666666666666 +lat_2=35.25 +lat_0=34.33333333333334 +lon_0=-86 +x_0=600000 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Tennessee (ftUS)
891;2916;+proj=lcc +lat_1=36.18333333333333 +lat_2=34.65 +lat_0=34 +lon_0=-101.5 +x_0=200000.0001016002 +y_0=999999.9998983998 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Texas North (ftUS)
892;2917;+proj=lcc +lat_1=33.96666666666667 +lat_2=32.13333333333333 +lat_0=31.66666666666667 +lon_0=-98.5 +x_0=600000 +y_0=2000000.0001016 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Texas North Central (ftUS)
893;2918;+proj=lcc +lat_1=31.88333333333333 +lat_2=30.11666666666667 +lat_0=29.66666666666667 +lon_0=-100.3333333333333 +x_0=699999.9998983998 +y_0=3000000 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Texas Central (ftUS)
894;2919;+proj=lcc +lat_1=30.28333333333333 +lat_2=28.38333333333333 +lat_0=27.83333333333333 +lon_0=-99 +x_0=600000 +y_0=3999999.9998984 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Texas South Central (ftUS)
895;2920;+proj=lcc +lat_1=27.83333333333333 +lat_2=26.16666666666667 +lat_0=25.66666666666667 +lon_0=-98.5 +x_0=300000.0000000001 +y_0=5000000.0001016 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Texas South (ftUS)
896;2921;+proj=lcc +lat_1=41.78333333333333 +lat_2=40.71666666666667 +lat_0=40.33333333333334 +lon_0=-111.5 +x_0=500000.0001504 +y_0=999999.9999960001 +ellps=GRS80 +to_meter=0.3048 +no_defs;NAD83(HARN) / Utah North (ft)
897;2922;+proj=lcc +lat_1=40.65 +lat_2=39.01666666666667 +lat_0=38.33333333333334 +lon_0=-111.5 +x_0=500000.0001504 +y_0=1999999.999992 +ellps=GRS80 +to_meter=0.3048 +no_defs;NAD83(HARN) / Utah Central (ft)
898;2923;+proj=lcc +lat_1=38.35 +lat_2=37.21666666666667 +lat_0=36.66666666666666 +lon_0=-111.5 +x_0=500000.0001504 +y_0=2999999.999988 +ellps=GRS80 +to_meter=0.3048 +no_defs;NAD83(HARN) / Utah South (ft)
899;2924;+proj=lcc +lat_1=39.2 +lat_2=38.03333333333333 +lat_0=37.66666666666666 +lon_0=-78.5 +x_0=3500000.0001016 +y_0=2000000.0001016 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Virginia North (ftUS)
900;2925;+proj=lcc +lat_1=37.96666666666667 +lat_2=36.76666666666667 +lat_0=36.33333333333334 +lon_0=-78.5 +x_0=3500000.0001016 +y_0=999999.9998983998 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Virginia South (ftUS)
901;2926;+proj=lcc +lat_1=48.73333333333333 +lat_2=47.5 +lat_0=47 +lon_0=-120.8333333333333 +x_0=500000.0001016001 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Washington North (ftUS)
902;2927;+proj=lcc +lat_1=47.33333333333334 +lat_2=45.83333333333334 +lat_0=45.33333333333334 +lon_0=-120.5 +x_0=500000.0001016001 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Washington South (ftUS)
903;2928;+proj=lcc +lat_1=46.76666666666667 +lat_2=45.56666666666667 +lat_0=45.16666666666666 +lon_0=-90 +x_0=600000 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Wisconsin North (ftUS)
904;2929;+proj=lcc +lat_1=45.5 +lat_2=44.25 +lat_0=43.83333333333334 +lon_0=-90 +x_0=600000 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Wisconsin Central (ftUS)
905;2930;+proj=lcc +lat_1=44.06666666666667 +lat_2=42.73333333333333 +lat_0=42 +lon_0=-90 +x_0=600000 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Wisconsin South (ftUS)
906;2931;+proj=tmerc +lat_0=0 +lon_0=13 +k=0.999600 +x_0=500000 +y_0=0 +a=6378249.2 +b=6356515 +towgs84=-106,-87,188,0,0,0,0 +units=m +no_defs;Beduaram / TM 13 NE
907;2932;+proj=tmerc +lat_0=24.45 +lon_0=51.21666666666667 +k=0.999990 +x_0=200000 +y_0=300000 +ellps=intl +towgs84=-119.425,-303.659,-11.0006,1.1643,0.174458,1.09626,3.65706 +units=m +no_defs;QND95 / Qatar National Grid
908;2933;+proj=utm +zone=50 +south +ellps=bessel +units=m +no_defs;Segara / UTM zone 50S
909;2934;+proj=merc +lon_0=110 +k=0.997000 +x_0=3900000 +y_0=900000 +ellps=bessel +pm=jakarta +units=m +no_defs;Segara (Jakarta) / NEIEZ (deprecated)
910;2935;+proj=tmerc +lat_0=0.1166666666666667 +lon_0=41.53333333333333 +k=1.000000 +x_0=1300000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / CS63 zone A1
911;2936;+proj=tmerc +lat_0=0.1166666666666667 +lon_0=44.53333333333333 +k=1.000000 +x_0=2300000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / CS63 zone A2
912;2937;+proj=tmerc +lat_0=0.1166666666666667 +lon_0=47.53333333333333 +k=1.000000 +x_0=3300000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / CS63 zone A3
913;2938;+proj=tmerc +lat_0=0.1166666666666667 +lon_0=50.53333333333333 +k=1.000000 +x_0=4300000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / CS63 zone A4
914;2939;+proj=tmerc +lat_0=0.1333333333333333 +lon_0=50.76666666666667 +k=1.000000 +x_0=2300000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / CS63 zone K2
915;2940;+proj=tmerc +lat_0=0.1333333333333333 +lon_0=53.76666666666667 +k=1.000000 +x_0=3300000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / CS63 zone K3
916;2941;+proj=tmerc +lat_0=0.1333333333333333 +lon_0=56.76666666666667 +k=1.000000 +x_0=4300000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / CS63 zone K4
917;2942;+proj=utm +zone=28 +ellps=intl +towgs84=-499,-249,314,0,0,0,0 +units=m +no_defs;Porto Santo / UTM zone 28N
918;2943;+proj=utm +zone=28 +ellps=intl +units=m +no_defs;Selvagem Grande / UTM zone 28N
919;2944;+proj=tmerc +lat_0=0 +lon_0=-55.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / SCoPQ zone 2
920;2945;+proj=tmerc +lat_0=0 +lon_0=-58.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / MTM zone 3
921;2946;+proj=tmerc +lat_0=0 +lon_0=-61.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / MTM zone 4
922;2947;+proj=tmerc +lat_0=0 +lon_0=-64.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / MTM zone 5
923;2948;+proj=tmerc +lat_0=0 +lon_0=-67.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / MTM zone 6
924;2949;+proj=tmerc +lat_0=0 +lon_0=-70.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / MTM zone 7
925;2950;+proj=tmerc +lat_0=0 +lon_0=-73.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / MTM zone 8
926;2951;+proj=tmerc +lat_0=0 +lon_0=-76.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / MTM zone 9
927;2952;+proj=tmerc +lat_0=0 +lon_0=-79.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / MTM zone 10
928;2953;+proj=sterea +lat_0=46.5 +lon_0=-66.5 +k=0.999912 +x_0=2500000 +y_0=7500000 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / New Brunswick Stereo
929;2954;+proj=sterea +lat_0=47.25 +lon_0=-63 +k=0.999912 +x_0=400000 +y_0=800000 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / Prince Edward Isl. Stereographic (NAD83)
930;2955;+proj=utm +zone=11 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / UTM zone 11N
931;2956;+proj=utm +zone=12 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / UTM zone 12N
932;2957;+proj=utm +zone=13 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / UTM zone 13N
933;2958;+proj=utm +zone=17 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / UTM zone 17N
934;2959;+proj=utm +zone=18 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / UTM zone 18N
935;2960;+proj=utm +zone=19 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / UTM zone 19N
936;2961;+proj=utm +zone=20 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / UTM zone 20N
937;2962;+proj=utm +zone=21 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / UTM zone 21N
938;2964;+proj=aea +lat_1=55 +lat_2=65 +lat_0=50 +lon_0=-154 +x_0=0 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Alaska Albers
939;2965;+proj=tmerc +lat_0=37.5 +lon_0=-85.66666666666667 +k=0.999967 +x_0=99999.99989839978 +y_0=249999.9998983998 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Indiana East (ftUS)
940;2966;+proj=tmerc +lat_0=37.5 +lon_0=-87.08333333333333 +k=0.999967 +x_0=900000 +y_0=249999.9998983998 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Indiana West (ftUS)
941;2967;+proj=tmerc +lat_0=37.5 +lon_0=-85.66666666666667 +k=0.999967 +x_0=99999.99989839978 +y_0=249999.9998983998 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Indiana East (ftUS)
942;2968;+proj=tmerc +lat_0=37.5 +lon_0=-87.08333333333333 +k=0.999967 +x_0=900000 +y_0=249999.9998983998 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Indiana West (ftUS)
943;2969;+proj=utm +zone=20 +ellps=intl +towgs84=137,248,-430,0,0,0,0 +units=m +no_defs;Fort Marigot / UTM zone 20N
944;2970;+proj=utm +zone=20 +ellps=intl +units=m +no_defs;Guadeloupe 1948 / UTM zone 20N
945;2971;+proj=utm +zone=22 +ellps=intl +towgs84=-186,230,110,0,0,0,0 +units=m +no_defs;CSG67 / UTM zone 22N
946;2972;+proj=utm +zone=22 +ellps=GRS80 +towgs84=2,2,-2,0,0,0,0 +units=m +no_defs;RGFG95 / UTM zone 22N
947;2973;+proj=utm +zone=20 +ellps=intl +units=m +no_defs;Martinique 1938 / UTM zone 20N
948;2975;+proj=utm +zone=40 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;RGR92 / UTM zone 40S
949;2976;+proj=utm +zone=6 +south +ellps=intl +towgs84=162,117,154,0,0,0,0 +units=m +no_defs;Tahiti 52 / UTM zone 6S
950;2977;+proj=utm +zone=5 +south +ellps=intl +units=m +no_defs;Tahaa 54 / UTM zone 5S
951;2978;+proj=utm +zone=7 +south +ellps=intl +units=m +no_defs;IGN72 Nuku Hiva / UTM zone 7S
952;2979;+proj=utm +zone=42 +south +ellps=intl +towgs84=145,-187,103,0,0,0,0 +units=m +no_defs;K0 1949 / UTM zone 42S (deprecated)
953;2980;+proj=utm +zone=38 +south +ellps=intl +towgs84=-382,-59,-262,0,0,0,0 +units=m +no_defs;Combani 1950 / UTM zone 38S
954;2981;+proj=utm +zone=58 +south +ellps=intl +units=m +no_defs;IGN56 Lifou / UTM zone 58S
955;2982;+proj=utm +zone=58 +south +ellps=intl +units=m +no_defs;IGN72 Grand Terre / UTM zone 58S (deprecated)
956;2983;+proj=utm +zone=58 +south +ellps=intl +towgs84=-122.383,-188.696,103.344,3.5107,-4.9668,-5.7047,4.4798 +units=m +no_defs;ST87 Ouvea / UTM zone 58S (deprecated)
957;2984;+proj=lcc +lat_1=-20.66666666666667 +lat_2=-22.33333333333333 +lat_0=-21.5 +lon_0=166 +x_0=400000 +y_0=300000 +ellps=intl +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;RGNC 1991 / Lambert New Caledonia (deprecated)
958;2987;+proj=utm +zone=21 +ellps=clrk66 +towgs84=30,430,368,0,0,0,0 +units=m +no_defs;Saint Pierre et Miquelon 1950 / UTM zone 21N
959;2988;+proj=utm +zone=1 +south +ellps=intl +units=m +no_defs;MOP78 / UTM zone 1S
960;2989;+proj=utm +zone=20 +ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;RRAF 1991 / UTM zone 20N
961;2990;+proj=tmerc +lat_0=-21.11666666666667 +lon_0=55.53333333333333 +k=1.000000 +x_0=50000 +y_0=160000 +ellps=intl +units=m +no_defs;Reunion 1947 / TM Reunion
962;2991;+proj=lcc +lat_1=43 +lat_2=45.5 +lat_0=41.75 +lon_0=-120.5 +x_0=400000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Oregon Lambert
963;2992;+proj=lcc +lat_1=43 +lat_2=45.5 +lat_0=41.75 +lon_0=-120.5 +x_0=399999.9999984 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048 +no_defs;NAD83 / Oregon Lambert (ft)
964;2993;+proj=lcc +lat_1=43 +lat_2=45.5 +lat_0=41.75 +lon_0=-120.5 +x_0=400000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Oregon Lambert
965;2994;+proj=lcc +lat_1=43 +lat_2=45.5 +lat_0=41.75 +lon_0=-120.5 +x_0=399999.9999984 +y_0=0 +ellps=GRS80 +to_meter=0.3048 +no_defs;NAD83(HARN) / Oregon Lambert (ft)
966;2995;+proj=utm +zone=58 +south +ellps=intl +units=m +no_defs;IGN53 Mare / UTM zone 58S
967;2996;+proj=utm +zone=58 +south +ellps=intl +units=m +no_defs;ST84 Ile des Pins / UTM zone 58S
968;2997;+proj=utm +zone=58 +south +ellps=intl +towgs84=-480.26,-438.32,-643.429,16.3119,20.1721,-4.0349,-111.7 +units=m +no_defs;ST71 Belep / UTM zone 58S
969;2998;+proj=utm +zone=58 +south +ellps=intl +units=m +no_defs;NEA74 Noumea / UTM zone 58S
970;2999;+proj=utm +zone=38 +south +ellps=intl +units=m +no_defs;Grand Comoros / UTM zone 38S
971;3000;+proj=merc +lon_0=110 +k=0.997000 +x_0=3900000 +y_0=900000 +ellps=bessel +units=m +no_defs;Segara / NEIEZ
972;3001;+proj=merc +lon_0=110 +k=0.997000 +x_0=3900000 +y_0=900000 +ellps=bessel +units=m +no_defs;Batavia / NEIEZ
973;3002;+proj=merc +lon_0=110 +k=0.997000 +x_0=3900000 +y_0=900000 +ellps=bessel +towgs84=-587.8,519.75,145.76,0,0,0,0 +units=m +no_defs;Makassar / NEIEZ
974;3003;+proj=tmerc +lat_0=0 +lon_0=9 +k=0.999600 +x_0=1500000 +y_0=0 +ellps=intl +units=m +no_defs;Monte Mario / Italy zone 1
975;3004;+proj=tmerc +lat_0=0 +lon_0=15 +k=0.999600 +x_0=2520000 +y_0=0 +ellps=intl +units=m +no_defs;Monte Mario / Italy zone 2
976;3005;+proj=aea +lat_1=50 +lat_2=58.5 +lat_0=45 +lon_0=-126 +x_0=1000000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / BC Albers
977;3006;+proj=utm +zone=33 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SWEREF99 TM
978;3007;+proj=tmerc +lat_0=0 +lon_0=12 +k=1.000000 +x_0=150000 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SWEREF99 12 00
979;3008;+proj=tmerc +lat_0=0 +lon_0=13.5 +k=1.000000 +x_0=150000 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SWEREF99 13 30
980;3009;+proj=tmerc +lat_0=0 +lon_0=15 +k=1.000000 +x_0=150000 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SWEREF99 15 00
981;3010;+proj=tmerc +lat_0=0 +lon_0=16.5 +k=1.000000 +x_0=150000 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SWEREF99 16 30
982;3011;+proj=tmerc +lat_0=0 +lon_0=18 +k=1.000000 +x_0=150000 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SWEREF99 18 00
983;3012;+proj=tmerc +lat_0=0 +lon_0=14.25 +k=1.000000 +x_0=150000 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SWEREF99 14 15
984;3013;+proj=tmerc +lat_0=0 +lon_0=15.75 +k=1.000000 +x_0=150000 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SWEREF99 15 45
985;3014;+proj=tmerc +lat_0=0 +lon_0=17.25 +k=1.000000 +x_0=150000 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SWEREF99 17 15
986;3015;+proj=tmerc +lat_0=0 +lon_0=18.75 +k=1.000000 +x_0=150000 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SWEREF99 18 45
987;3016;+proj=tmerc +lat_0=0 +lon_0=20.25 +k=1.000000 +x_0=150000 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SWEREF99 20 15
988;3017;+proj=tmerc +lat_0=0 +lon_0=21.75 +k=1.000000 +x_0=150000 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SWEREF99 21 45
989;3018;+proj=tmerc +lat_0=0 +lon_0=23.25 +k=1.000000 +x_0=150000 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SWEREF99 23 15
990;3019;+proj=tmerc +lat_0=0 +lon_0=11.30827777777778 +k=1.000000 +x_0=1500000 +y_0=0 +ellps=bessel +units=m +no_defs;RT90 7.5 gon V
991;3020;+proj=tmerc +lat_0=0 +lon_0=13.55827777777778 +k=1.000000 +x_0=1500000 +y_0=0 +ellps=bessel +units=m +no_defs;RT90 5 gon V
992;3021;+proj=tmerc +lat_0=0 +lon_0=15.80827777777778 +k=1.000000 +x_0=1500000 +y_0=0 +ellps=bessel +units=m +no_defs;RT90 2.5 gon V
993;3022;+proj=tmerc +lat_0=0 +lon_0=18.05827777777778 +k=1.000000 +x_0=1500000 +y_0=0 +ellps=bessel +units=m +no_defs;RT90 0 gon
994;3023;+proj=tmerc +lat_0=0 +lon_0=20.30827777777778 +k=1.000000 +x_0=1500000 +y_0=0 +ellps=bessel +units=m +no_defs;RT90 2.5 gon O
995;3024;+proj=tmerc +lat_0=0 +lon_0=22.55827777777778 +k=1.000000 +x_0=1500000 +y_0=0 +ellps=bessel +units=m +no_defs;RT90 5 gon O
996;3025;+proj=tmerc +lat_0=0 +lon_0=11.30827777777778 +k=1.000000 +x_0=1500000 +y_0=0 +ellps=bessel +units=m +no_defs;RT38 7.5 gon V
997;3026;+proj=tmerc +lat_0=0 +lon_0=13.55827777777778 +k=1.000000 +x_0=1500000 +y_0=0 +ellps=bessel +units=m +no_defs;RT38 5 gon V
998;3027;+proj=tmerc +lat_0=0 +lon_0=15.80827777777778 +k=1.000000 +x_0=1500000 +y_0=0 +ellps=bessel +units=m +no_defs;RT38 2.5 gon V
999;3028;+proj=tmerc +lat_0=0 +lon_0=18.05827777777778 +k=1.000000 +x_0=1500000 +y_0=0 +ellps=bessel +units=m +no_defs;RT38 0 gon
1000;3029;+proj=tmerc +lat_0=0 +lon_0=20.30827777777778 +k=1.000000 +x_0=1500000 +y_0=0 +ellps=bessel +units=m +no_defs;RT38 2.5 gon O
1001;3030;+proj=tmerc +lat_0=0 +lon_0=22.55827777777778 +k=1.000000 +x_0=1500000 +y_0=0 +ellps=bessel +units=m +no_defs;RT38 5 gon O
1002;3031;+proj=stere +lat_0=-90 +lat_ts=-71 +lon_0=0 +k=1 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / Antarctic Polar Stereographic
1003;3032;+proj=stere +lat_0=-90 +lat_ts=-71 +lon_0=70 +k=1 +x_0=6000000 +y_0=6000000 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / Australian Antarctic Polar Stereographic
1004;3033;+proj=lcc +lat_1=-68.5 +lat_2=-74.5 +lat_0=-50 +lon_0=70 +x_0=6000000 +y_0=6000000 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / Australian Antarctic Lambert
1005;3034;+proj=lcc +lat_1=35 +lat_2=65 +lat_0=52 +lon_0=10 +x_0=4000000 +y_0=2800000 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-LCC
1006;3035;+proj=laea +lat_0=52 +lon_0=10 +x_0=4321000 +y_0=3210000 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-LAEA
1007;3036;+proj=utm +zone=36 +south +ellps=WGS84 +towgs84=0,0,0,-0,-0,-0,0 +units=m +no_defs;Moznet / UTM zone 36S
1008;3037;+proj=utm +zone=37 +south +ellps=WGS84 +towgs84=0,0,0,-0,-0,-0,0 +units=m +no_defs;Moznet / UTM zone 37S
1009;3038;+proj=utm +zone=26 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-TM26
1010;3039;+proj=utm +zone=27 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-TM27
1011;3040;+proj=utm +zone=28 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-TM28
1012;3041;+proj=utm +zone=29 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-TM29
1013;3042;+proj=utm +zone=30 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-TM30
1014;3043;+proj=utm +zone=31 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-TM31
1015;3044;+proj=utm +zone=32 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-TM32
1016;3045;+proj=utm +zone=33 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-TM33
1017;3046;+proj=utm +zone=34 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-TM34
1018;3047;+proj=utm +zone=35 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-TM35
1019;3048;+proj=utm +zone=36 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-TM36
1020;3049;+proj=utm +zone=37 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-TM37
1021;3050;+proj=utm +zone=38 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-TM38
1022;3051;+proj=utm +zone=39 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-TM39
1023;3054;+proj=utm +zone=26 +ellps=intl +towgs84=-73,46,-86,0,0,0,0 +units=m +no_defs;Hjorsey 1955 / UTM zone 26N
1024;3055;+proj=utm +zone=27 +ellps=intl +towgs84=-73,46,-86,0,0,0,0 +units=m +no_defs;Hjorsey 1955 / UTM zone 27N
1025;3056;+proj=utm +zone=28 +ellps=intl +towgs84=-73,46,-86,0,0,0,0 +units=m +no_defs;Hjorsey 1955 / UTM zone 28N
1026;3057;+proj=lcc +lat_1=64.25 +lat_2=65.75 +lat_0=65 +lon_0=-19 +x_0=500000 +y_0=500000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;ISN93 / Lambert 1993
1027;3058;+proj=tmerc +lat_0=0 +lon_0=-8.5 +k=1.000000 +x_0=50000 +y_0=-7800000 +ellps=intl +towgs84=982.609,552.753,-540.873,32.3934,-153.257,-96.2266,16.805 +units=m +no_defs;Helle 1954 / Jan Mayen Grid
1028;3059;+proj=tmerc +lat_0=0 +lon_0=24 +k=0.999600 +x_0=500000 +y_0=-6000000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;LKS92 / Latvia TM
1029;3060;+proj=utm +zone=58 +south +ellps=intl +units=m +no_defs;IGN72 Grande Terre / UTM zone 58S
1030;3061;+proj=utm +zone=28 +ellps=intl +units=m +no_defs;Porto Santo 1995 / UTM zone 28N
1031;3062;+proj=utm +zone=26 +ellps=intl +units=m +no_defs;Azores Oriental 1995 / UTM zone 26N
1032;3063;+proj=utm +zone=26 +ellps=intl +units=m +no_defs;Azores Central 1995 / UTM zone 26N
1033;3064;+proj=utm +zone=32 +ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;IGM95 / UTM zone 32N
1034;3065;+proj=utm +zone=33 +ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;IGM95 / UTM zone 33N
1035;3066;+proj=tmerc +lat_0=0 +lon_0=37 +k=0.999800 +x_0=500000 +y_0=-3000000 +ellps=intl +units=m +no_defs;ED50 / Jordan TM
1036;3067;+proj=utm +zone=35 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-TM35FIN
1037;3068;+proj=cass +lat_0=52.41864827777778 +lon_0=13.62720366666667 +x_0=40000 +y_0=10000 +ellps=bessel +datum=potsdam +units=m +no_defs;DHDN / Soldner Berlin
1038;3069;+proj=tmerc +lat_0=0 +lon_0=-90 +k=0.999600 +x_0=500000 +y_0=-4500000 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / Wisconsin Transverse Mercator
1039;3070;+proj=tmerc +lat_0=0 +lon_0=-90 +k=0.999600 +x_0=520000 +y_0=-4480000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Wisconsin Transverse Mercator
1040;3071;+proj=tmerc +lat_0=0 +lon_0=-90 +k=0.999600 +x_0=520000 +y_0=-4480000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Wisconsin Transverse Mercator
1041;3072;+proj=tmerc +lat_0=43.83333333333334 +lon_0=-67.875 +k=0.999980 +x_0=700000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Maine CS2000 East
1042;3073;+proj=tmerc +lat_0=43 +lon_0=-69.125 +k=0.999980 +x_0=500000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Maine CS2000 Central
1043;3074;+proj=tmerc +lat_0=42.83333333333334 +lon_0=-70.375 +k=0.999980 +x_0=300000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Maine CS2000 West
1044;3075;+proj=tmerc +lat_0=43.83333333333334 +lon_0=-67.875 +k=0.999980 +x_0=700000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Maine CS2000 East
1045;3076;+proj=tmerc +lat_0=43 +lon_0=-69.125 +k=0.999980 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Maine CS2000 Central
1046;3077;+proj=tmerc +lat_0=42.83333333333334 +lon_0=-70.375 +k=0.999980 +x_0=300000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Maine CS2000 West
1047;3078;+proj=omerc +lat_0=45.30916666666666 +lonc=-86 +alpha=337.25556 +k=0.9996 +x_0=2546731.496 +y_0=-4354009.816 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Michigan Oblique Mercator
1048;3079;+proj=omerc +lat_0=45.30916666666666 +lonc=-86 +alpha=337.25556 +k=0.9996 +x_0=2546731.496 +y_0=-4354009.816 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Michigan Oblique Mercator
1049;3080;+proj=lcc +lat_1=27.41666666666667 +lat_2=34.91666666666666 +lat_0=31.16666666666667 +lon_0=-100 +x_0=914400 +y_0=914400 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048 +no_defs;NAD27 / Shackleford
1050;3081;+proj=lcc +lat_1=27.41666666666667 +lat_2=34.91666666666666 +lat_0=31.16666666666667 +lon_0=-100 +x_0=1000000 +y_0=1000000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Texas State Mapping System
1051;3082;+proj=lcc +lat_1=27.5 +lat_2=35 +lat_0=18 +lon_0=-100 +x_0=1500000 +y_0=5000000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Texas Centric Lambert Conformal
1052;3083;+proj=aea +lat_1=27.5 +lat_2=35 +lat_0=18 +lon_0=-100 +x_0=1500000 +y_0=6000000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Texas Centric Albers Equal Area
1053;3084;+proj=lcc +lat_1=27.5 +lat_2=35 +lat_0=18 +lon_0=-100 +x_0=1500000 +y_0=5000000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Texas Centric Lambert Conformal
1054;3085;+proj=aea +lat_1=27.5 +lat_2=35 +lat_0=18 +lon_0=-100 +x_0=1500000 +y_0=6000000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Texas Centric Albers Equal Area
1055;3086;+proj=aea +lat_1=24 +lat_2=31.5 +lat_0=24 +lon_0=-84 +x_0=400000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Florida GDL Albers
1056;3087;+proj=aea +lat_1=24 +lat_2=31.5 +lat_0=24 +lon_0=-84 +x_0=400000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Florida GDL Albers
1057;3088;+proj=lcc +lat_1=37.08333333333334 +lat_2=38.66666666666666 +lat_0=36.33333333333334 +lon_0=-85.75 +x_0=1500000 +y_0=1000000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Kentucky Single Zone
1058;3089;+proj=lcc +lat_1=37.08333333333334 +lat_2=38.66666666666666 +lat_0=36.33333333333334 +lon_0=-85.75 +x_0=1500000 +y_0=999999.9998983998 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / Kentucky Single Zone (ftUS)
1059;3090;+proj=lcc +lat_1=37.08333333333334 +lat_2=38.66666666666666 +lat_0=36.33333333333334 +lon_0=-85.75 +x_0=1500000 +y_0=1000000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Kentucky Single Zone
1060;3091;+proj=lcc +lat_1=37.08333333333334 +lat_2=38.66666666666666 +lat_0=36.33333333333334 +lon_0=-85.75 +x_0=1500000 +y_0=999999.9998983998 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Kentucky Single Zone (ftUS)
1061;3092;+proj=utm +zone=51 +ellps=bessel +units=m +no_defs;Tokyo / UTM zone 51N
1062;3093;+proj=utm +zone=52 +ellps=bessel +units=m +no_defs;Tokyo / UTM zone 52N
1063;3094;+proj=utm +zone=53 +ellps=bessel +units=m +no_defs;Tokyo / UTM zone 53N
1064;3095;+proj=utm +zone=54 +ellps=bessel +units=m +no_defs;Tokyo / UTM zone 54N
1065;3096;+proj=utm +zone=55 +ellps=bessel +units=m +no_defs;Tokyo / UTM zone 55N
1066;3097;+proj=utm +zone=51 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;JGD2000 / UTM zone 51N
1067;3098;+proj=utm +zone=52 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;JGD2000 / UTM zone 52N
1068;3099;+proj=utm +zone=53 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;JGD2000 / UTM zone 53N
1069;3100;+proj=utm +zone=54 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;JGD2000 / UTM zone 54N
1070;3101;+proj=utm +zone=55 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;JGD2000 / UTM zone 55N
1071;3102;+proj=lcc +lat_1=-14.26666666666667 +lat_0=-14.26666666666667 +lon_0=-170 +k_0=1 +x_0=152400.3048006096 +y_0=95169.31165862332 +ellps=clrk66 +towgs84=-115,118,426,0,0,0,0 +to_meter=0.3048006096012192 +no_defs;American Samoa 1962 / American Samoa Lambert
1072;3103;+proj=utm +zone=28 +ellps=clrk80 +units=m +no_defs;Mauritania 1999 / UTM zone 28N (deprecated)
1073;3104;+proj=utm +zone=29 +ellps=clrk80 +units=m +no_defs;Mauritania 1999 / UTM zone 29N (deprecated)
1074;3105;+proj=utm +zone=30 +ellps=clrk80 +units=m +no_defs;Mauritania 1999 / UTM zone 30N (deprecated)
1075;3106;+proj=tmerc +lat_0=0 +lon_0=90 +k=0.999600 +x_0=500000 +y_0=0 +a=6377276.345 +b=6356075.413140239 +units=m +no_defs;Gulshan 303 / Bangladesh Transverse Mercator
1076;3107;+proj=lcc +lat_1=-28 +lat_2=-36 +lat_0=-32 +lon_0=135 +x_0=1000000 +y_0=2000000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;GDA94 / SA Lambert
1077;3108;+proj=tmerc +lat_0=49.5 +lon_0=-2.416666666666667 +k=0.999997 +x_0=47000 +y_0=50000 +ellps=GRS80 +units=m +no_defs;ETRS89 / Guernsey Grid
1078;3109;+proj=tmerc +lat_0=49.225 +lon_0=-2.135 +k=1.000000 +x_0=40000 +y_0=70000 +ellps=GRS80 +units=m +no_defs;ETRS89 / Jersey Transverse Mercator
1079;3110;+proj=lcc +lat_1=-36 +lat_2=-38 +lat_0=-37 +lon_0=145 +x_0=2500000 +y_0=4500000 +ellps=aust_SA +units=m +no_defs;AGD66 / Vicgrid66
1080;3111;+proj=lcc +lat_1=-36 +lat_2=-38 +lat_0=-37 +lon_0=145 +x_0=2500000 +y_0=2500000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;GDA94 / Vicgrid94
1081;3112;+proj=lcc +lat_1=-18 +lat_2=-36 +lat_0=0 +lon_0=134 +x_0=0 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;GDA94 / Geoscience Australia Lambert
1082;3113;+proj=tmerc +lat_0=-28 +lon_0=153 +k=0.999990 +x_0=50000 +y_0=100000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;GDA94 / BCSG02
1083;3114;+proj=tmerc +lat_0=4.596200416666666 +lon_0=-80.07750791666666 +k=1.000000 +x_0=1000000 +y_0=1000000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;MAGNA-SIRGAS / Colombia Far West zone
1084;3115;+proj=tmerc +lat_0=4.596200416666666 +lon_0=-77.07750791666666 +k=1.000000 +x_0=1000000 +y_0=1000000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;MAGNA-SIRGAS / Colombia West zone
1085;3116;+proj=tmerc +lat_0=4.596200416666666 +lon_0=-74.07750791666666 +k=1.000000 +x_0=1000000 +y_0=1000000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;MAGNA-SIRGAS / Colombia Bogota zone
1086;3117;+proj=tmerc +lat_0=4.596200416666666 +lon_0=-71.07750791666666 +k=1.000000 +x_0=1000000 +y_0=1000000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;MAGNA-SIRGAS / Colombia East Central zone
1087;3118;+proj=tmerc +lat_0=4.596200416666666 +lon_0=-68.07750791666666 +k=1.000000 +x_0=1000000 +y_0=1000000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;MAGNA-SIRGAS / Colombia East zone
1088;3119;+proj=tmerc +lat_0=0 +lon_0=10.5 +k=0.999000 +x_0=1000000 +y_0=1000000 +ellps=intl +towgs84=-206.1,-174.7,-87.7,0,0,0,0 +units=m +no_defs;Douala 1948 / AEF west
1089;3120;+proj=sterea +lat_0=50.625 +lon_0=21.08333333333333 +k=0.999800 +x_0=4637000 +y_0=5467000 +ellps=krass +towgs84=33.4,-146.6,-76.3,-0.359,-0.053,0.844,-0.84 +units=m +no_defs;Pulkovo 1942(58) / Poland zone I
1090;3121;+proj=tmerc +lat_0=0 +lon_0=117 +k=0.999950 +x_0=500000 +y_0=0 +ellps=clrk66 +towgs84=-127.62,-67.24,-47.04,-3.068,4.903,1.578,-1.06 +units=m +no_defs;PRS92 / Philippines zone 1
1091;3122;+proj=tmerc +lat_0=0 +lon_0=119 +k=0.999950 +x_0=500000 +y_0=0 +ellps=clrk66 +towgs84=-127.62,-67.24,-47.04,-3.068,4.903,1.578,-1.06 +units=m +no_defs;PRS92 / Philippines zone 2
1092;3123;+proj=tmerc +lat_0=0 +lon_0=121 +k=0.999950 +x_0=500000 +y_0=0 +ellps=clrk66 +towgs84=-127.62,-67.24,-47.04,-3.068,4.903,1.578,-1.06 +units=m +no_defs;PRS92 / Philippines zone 3
1093;3124;+proj=tmerc +lat_0=0 +lon_0=123 +k=0.999950 +x_0=500000 +y_0=0 +ellps=clrk66 +towgs84=-127.62,-67.24,-47.04,-3.068,4.903,1.578,-1.06 +units=m +no_defs;PRS92 / Philippines zone 4
1094;3125;+proj=tmerc +lat_0=0 +lon_0=125 +k=0.999950 +x_0=500000 +y_0=0 +ellps=clrk66 +towgs84=-127.62,-67.24,-47.04,-3.068,4.903,1.578,-1.06 +units=m +no_defs;PRS92 / Philippines zone 5
1095;3126;+proj=tmerc +lat_0=0 +lon_0=19 +k=1.000000 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-GK19FIN
1096;3127;+proj=tmerc +lat_0=0 +lon_0=20 +k=1.000000 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-GK20FIN
1097;3128;+proj=tmerc +lat_0=0 +lon_0=21 +k=1.000000 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-GK21FIN
1098;3129;+proj=tmerc +lat_0=0 +lon_0=22 +k=1.000000 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-GK22FIN
1099;3130;+proj=tmerc +lat_0=0 +lon_0=23 +k=1.000000 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-GK23FIN
1100;3131;+proj=tmerc +lat_0=0 +lon_0=24 +k=1.000000 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-GK24FIN
1101;3132;+proj=tmerc +lat_0=0 +lon_0=25 +k=1.000000 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-GK25FIN
1102;3133;+proj=tmerc +lat_0=0 +lon_0=26 +k=1.000000 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-GK26FIN
1103;3134;+proj=tmerc +lat_0=0 +lon_0=27 +k=1.000000 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-GK27FIN
1104;3135;+proj=tmerc +lat_0=0 +lon_0=28 +k=1.000000 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-GK28FIN
1105;3136;+proj=tmerc +lat_0=0 +lon_0=29 +k=1.000000 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-GK29FIN
1106;3137;+proj=tmerc +lat_0=0 +lon_0=30 +k=1.000000 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-GK30FIN
1107;3138;+proj=tmerc +lat_0=0 +lon_0=31 +k=1.000000 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;ETRS89 / ETRS-GK31FIN
1108;3140;+proj=cass +lat_0=-18 +lon_0=178 +x_0=109435.392 +y_0=141622.272 +a=6378306.3696 +b=6356571.996 +towgs84=51,391,-36,0,0,0,0 +to_meter=0.201168 +no_defs;Viti Levu 1912 / Viti Levu Grid
1109;3141;+proj=utm +zone=60 +south +ellps=intl +towgs84=265.025,384.929,-194.046,0,0,0,0 +units=m +no_defs;Fiji 1956 / UTM zone 60S
1110;3142;+proj=utm +zone=1 +south +ellps=intl +towgs84=265.025,384.929,-194.046,0,0,0,0 +units=m +no_defs;Fiji 1956 / UTM zone 1S
1111;3143;+proj=tmerc +lat_0=-17 +lon_0=178.75 +k=0.999850 +x_0=2000000 +y_0=4000000 +ellps=WGS72 +units=m +no_defs;Fiji 1986 / Fiji Map Grid
1112;3146;+proj=tmerc +lat_0=0 +lon_0=18 +k=1.000000 +x_0=6500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 6
1113;3147;+proj=tmerc +lat_0=0 +lon_0=18 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger CM 18E
1114;3148;+proj=utm +zone=48 +a=6377276.345 +b=6356075.413140239 +units=m +no_defs;Indian 1960 / UTM zone 48N
1115;3149;+proj=utm +zone=49 +a=6377276.345 +b=6356075.413140239 +units=m +no_defs;Indian 1960 / UTM zone 49N
1116;3150;+proj=tmerc +lat_0=0 +lon_0=18 +k=1.000000 +x_0=6500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 6
1117;3151;+proj=tmerc +lat_0=0 +lon_0=18 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger CM 18E
1118;3152;+proj=tmerc +lat_0=0 +lon_0=18.05779 +k=0.999994 +x_0=100178.1808 +y_0=-6500614.7836 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;ST74
1119;3153;+proj=aea +lat_1=50 +lat_2=58.5 +lat_0=45 +lon_0=-126 +x_0=1000000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / BC Albers
1120;3154;+proj=utm +zone=7 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / UTM zone 7N
1121;3155;+proj=utm +zone=8 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / UTM zone 8N
1122;3156;+proj=utm +zone=9 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / UTM zone 9N
1123;3157;+proj=utm +zone=10 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / UTM zone 10N
1124;3158;+proj=utm +zone=14 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / UTM zone 14N
1125;3159;+proj=utm +zone=15 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / UTM zone 15N
1126;3160;+proj=utm +zone=16 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / UTM zone 16N
1127;3161;+proj=lcc +lat_1=44.5 +lat_2=53.5 +lat_0=0 +lon_0=-85 +x_0=930000 +y_0=6430000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Ontario MNR Lambert
1128;3162;+proj=lcc +lat_1=44.5 +lat_2=53.5 +lat_0=0 +lon_0=-85 +x_0=930000 +y_0=6430000 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / Ontario MNR Lambert
1129;3163;+proj=lcc +lat_1=-20.66666666666667 +lat_2=-22.33333333333333 +lat_0=-21.5 +lon_0=166 +x_0=400000 +y_0=300000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;RGNC91-93 / Lambert New Caledonia
1130;3164;+proj=utm +zone=58 +south +ellps=WGS84 +towgs84=-56.263,16.136,-22.856,0,0,0,0 +units=m +no_defs;ST87 Ouvea / UTM zone 58S
1131;3165;+proj=lcc +lat_1=-22.24469175 +lat_2=-22.29469175 +lat_0=-22.26969175 +lon_0=166.44242575 +x_0=0.66 +y_0=1.02 +ellps=intl +units=m +no_defs;NEA74 Noumea / Noumea Lambert
1132;3166;+proj=lcc +lat_1=-22.24472222222222 +lat_2=-22.29472222222222 +lat_0=-22.26972222222222 +lon_0=166.4425 +x_0=8.313000000000001 +y_0=-2.354 +ellps=intl +units=m +no_defs;NEA74 Noumea / Noumea Lambert 2
1133;3167;+proj=omerc +lat_0=4 +lonc=102.25 +alpha=323.0257905 +k=0.99984 +x_0=40000 +y_0=0 +a=6377295.664 +b=6356094.667915204 +to_meter=20.116756 +no_defs;Kertau (RSO) / RSO Malaya (ch)
1134;3168;+proj=omerc +lat_0=4 +lonc=102.25 +alpha=323.0257905 +k=0.99984 +x_0=804670.24 +y_0=0 +a=6377295.664 +b=6356094.667915204 +units=m +no_defs;Kertau (RSO) / RSO Malaya (m)
1135;3169;+proj=utm +zone=57 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;RGNC91-93 / UTM zone 57S
1136;3170;+proj=utm +zone=58 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;RGNC91-93 / UTM zone 58S
1137;3171;+proj=utm +zone=59 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;RGNC91-93 / UTM zone 59S
1138;3172;+proj=utm +zone=59 +south +ellps=intl +units=m +no_defs;IGN53 Mare / UTM zone 59S
1139;3176;+proj=tmerc +lat_0=0 +lon_0=106 +k=0.999600 +x_0=500000 +y_0=0 +a=6377276.345 +b=6356075.413140239 +units=m +no_defs;Indian 1960 / TM 106 NE
1140;3177;+proj=tmerc +lat_0=0 +lon_0=17 +k=0.996500 +x_0=1000000 +y_0=0 +ellps=intl +towgs84=-208.406,-109.878,-2.5764,0,0,0,0 +units=m +no_defs;LGD2006 / Libya TM
1141;3178;+proj=utm +zone=18 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;GR96 / UTM zone 18N
1142;3179;+proj=utm +zone=19 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;GR96 / UTM zone 19N
1143;3180;+proj=utm +zone=20 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;GR96 / UTM zone 20N
1144;3181;+proj=utm +zone=21 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;GR96 / UTM zone 21N
1145;3182;+proj=utm +zone=22 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;GR96 / UTM zone 22N
1146;3183;+proj=utm +zone=23 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;GR96 / UTM zone 23N
1147;3184;+proj=utm +zone=24 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;GR96 / UTM zone 24N
1148;3185;+proj=utm +zone=25 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;GR96 / UTM zone 25N
1149;3186;+proj=utm +zone=26 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;GR96 / UTM zone 26N
1150;3187;+proj=utm +zone=27 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;GR96 / UTM zone 27N
1151;3188;+proj=utm +zone=28 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;GR96 / UTM zone 28N
1152;3189;+proj=utm +zone=29 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;GR96 / UTM zone 29N
1153;3190;+proj=tmerc +lat_0=0 +lon_0=9 +k=0.999950 +x_0=200000 +y_0=0 +ellps=intl +towgs84=-208.406,-109.878,-2.5764,0,0,0,0 +units=m +no_defs;LGD2006 / Libya TM zone 5
1154;3191;+proj=tmerc +lat_0=0 +lon_0=11 +k=0.999950 +x_0=200000 +y_0=0 +ellps=intl +towgs84=-208.406,-109.878,-2.5764,0,0,0,0 +units=m +no_defs;LGD2006 / Libya TM zone 6
1155;3192;+proj=tmerc +lat_0=0 +lon_0=13 +k=0.999950 +x_0=200000 +y_0=0 +ellps=intl +towgs84=-208.406,-109.878,-2.5764,0,0,0,0 +units=m +no_defs;LGD2006 / Libya TM zone 7
1156;3193;+proj=tmerc +lat_0=0 +lon_0=15 +k=0.999950 +x_0=200000 +y_0=0 +ellps=intl +towgs84=-208.406,-109.878,-2.5764,0,0,0,0 +units=m +no_defs;LGD2006 / Libya TM zone 8
1157;3194;+proj=tmerc +lat_0=0 +lon_0=17 +k=0.999950 +x_0=200000 +y_0=0 +ellps=intl +towgs84=-208.406,-109.878,-2.5764,0,0,0,0 +units=m +no_defs;LGD2006 / Libya TM zone 9
1158;3195;+proj=tmerc +lat_0=0 +lon_0=19 +k=0.999950 +x_0=200000 +y_0=0 +ellps=intl +towgs84=-208.406,-109.878,-2.5764,0,0,0,0 +units=m +no_defs;LGD2006 / Libya TM zone 10
1159;3196;+proj=tmerc +lat_0=0 +lon_0=21 +k=0.999950 +x_0=200000 +y_0=0 +ellps=intl +towgs84=-208.406,-109.878,-2.5764,0,0,0,0 +units=m +no_defs;LGD2006 / Libya TM zone 11
1160;3197;+proj=tmerc +lat_0=0 +lon_0=23 +k=0.999950 +x_0=200000 +y_0=0 +ellps=intl +towgs84=-208.406,-109.878,-2.5764,0,0,0,0 +units=m +no_defs;LGD2006 / Libya TM zone 12
1161;3198;+proj=tmerc +lat_0=0 +lon_0=25 +k=0.999950 +x_0=200000 +y_0=0 +ellps=intl +towgs84=-208.406,-109.878,-2.5764,0,0,0,0 +units=m +no_defs;LGD2006 / Libya TM zone 13
1162;3199;+proj=utm +zone=32 +ellps=intl +towgs84=-208.406,-109.878,-2.5764,0,0,0,0 +units=m +no_defs;LGD2006 / UTM zone 32N
1163;3200;+proj=lcc +lat_1=32.5 +lat_0=32.5 +lon_0=45 +k_0=0.9987864078000001 +x_0=1500000 +y_0=1166200 +ellps=clrk80 +units=m +no_defs;FD58 / Iraq zone
1164;3201;+proj=utm +zone=33 +ellps=intl +towgs84=-208.406,-109.878,-2.5764,0,0,0,0 +units=m +no_defs;LGD2006 / UTM zone 33N
1165;3202;+proj=utm +zone=34 +ellps=intl +towgs84=-208.406,-109.878,-2.5764,0,0,0,0 +units=m +no_defs;LGD2006 / UTM zone 34N
1166;3203;+proj=utm +zone=35 +ellps=intl +towgs84=-208.406,-109.878,-2.5764,0,0,0,0 +units=m +no_defs;LGD2006 / UTM zone 35N
1167;3204;+proj=lcc +lat_1=-60.66666666666666 +lat_2=-63.33333333333334 +lat_0=-90 +lon_0=-66 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SP19-20
1168;3205;+proj=lcc +lat_1=-60.66666666666666 +lat_2=-63.33333333333334 +lat_0=-90 +lon_0=-54 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SP21-22
1169;3206;+proj=lcc +lat_1=-60.66666666666666 +lat_2=-63.33333333333334 +lat_0=-90 +lon_0=-42 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SP23-24
1170;3207;+proj=lcc +lat_1=-64.66666666666667 +lat_2=-67.33333333333333 +lat_0=-90 +lon_0=-174 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SQ01-02
1171;3208;+proj=lcc +lat_1=-64.66666666666667 +lat_2=-67.33333333333333 +lat_0=-90 +lon_0=-66 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SQ19-20
1172;3209;+proj=lcc +lat_1=-64.66666666666667 +lat_2=-67.33333333333333 +lat_0=-90 +lon_0=-54 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SQ21-22
1173;3210;+proj=lcc +lat_1=-64.66666666666667 +lat_2=-67.33333333333333 +lat_0=-90 +lon_0=42 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SQ37-38
1174;3211;+proj=lcc +lat_1=-64.66666666666667 +lat_2=-67.33333333333333 +lat_0=-90 +lon_0=54 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SQ39-40
1175;3212;+proj=lcc +lat_1=-64.66666666666667 +lat_2=-67.33333333333333 +lat_0=-90 +lon_0=66 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SQ41-42
1176;3213;+proj=lcc +lat_1=-64.66666666666667 +lat_2=-67.33333333333333 +lat_0=-90 +lon_0=78 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SQ43-44
1177;3214;+proj=lcc +lat_1=-64.66666666666667 +lat_2=-67.33333333333333 +lat_0=-90 +lon_0=90 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SQ45-46
1178;3215;+proj=lcc +lat_1=-64.66666666666667 +lat_2=-67.33333333333333 +lat_0=-90 +lon_0=102 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SQ47-48
1179;3216;+proj=lcc +lat_1=-64.66666666666667 +lat_2=-67.33333333333333 +lat_0=-90 +lon_0=114 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SQ49-50
1180;3217;+proj=lcc +lat_1=-64.66666666666667 +lat_2=-67.33333333333333 +lat_0=-90 +lon_0=126 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SQ51-52
1181;3218;+proj=lcc +lat_1=-64.66666666666667 +lat_2=-67.33333333333333 +lat_0=-90 +lon_0=138 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SQ53-54
1182;3219;+proj=lcc +lat_1=-64.66666666666667 +lat_2=-67.33333333333333 +lat_0=-90 +lon_0=150 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SQ55-56
1183;3220;+proj=lcc +lat_1=-64.66666666666667 +lat_2=-67.33333333333333 +lat_0=-90 +lon_0=162 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SQ57-58
1184;3221;+proj=lcc +lat_1=-68.66666666666667 +lat_2=-71.33333333333333 +lat_0=-90 +lon_0=-102 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SR13-14
1185;3222;+proj=lcc +lat_1=-68.66666666666667 +lat_2=-71.33333333333333 +lat_0=-90 +lon_0=-90 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SR15-16
1186;3223;+proj=lcc +lat_1=-68.66666666666667 +lat_2=-71.33333333333333 +lat_0=-90 +lon_0=-78 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SR17-18
1187;3224;+proj=lcc +lat_1=-68.66666666666667 +lat_2=-71.33333333333333 +lat_0=-90 +lon_0=-66 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SR19-20
1188;3225;+proj=lcc +lat_1=-68.66666666666667 +lat_2=-71.33333333333333 +lat_0=-90 +lon_0=-18 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SR27-28
1189;3226;+proj=lcc +lat_1=-68.66666666666667 +lat_2=-71.33333333333333 +lat_0=-90 +lon_0=-6 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SR29-30
1190;3227;+proj=lcc +lat_1=-68.66666666666667 +lat_2=-71.33333333333333 +lat_0=-90 +lon_0=6 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SR31-32
1191;3228;+proj=lcc +lat_1=-68.66666666666667 +lat_2=-71.33333333333333 +lat_0=-90 +lon_0=18 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SR33-34
1192;3229;+proj=lcc +lat_1=-68.66666666666667 +lat_2=-71.33333333333333 +lat_0=-90 +lon_0=30 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SR35-36
1193;3230;+proj=lcc +lat_1=-68.66666666666667 +lat_2=-71.33333333333333 +lat_0=-90 +lon_0=42 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SR37-38
1194;3231;+proj=lcc +lat_1=-68.66666666666667 +lat_2=-71.33333333333333 +lat_0=-90 +lon_0=54 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SR39-40
1195;3232;+proj=lcc +lat_1=-68.66666666666667 +lat_2=-71.33333333333333 +lat_0=-90 +lon_0=66 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SR41-42
1196;3233;+proj=lcc +lat_1=-68.66666666666667 +lat_2=-71.33333333333333 +lat_0=-90 +lon_0=78 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SR43-44
1197;3234;+proj=lcc +lat_1=-68.66666666666667 +lat_2=-71.33333333333333 +lat_0=-90 +lon_0=90 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SR45-46
1198;3235;+proj=lcc +lat_1=-68.66666666666667 +lat_2=-71.33333333333333 +lat_0=-90 +lon_0=102 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SR47-48
1199;3236;+proj=lcc +lat_1=-68.66666666666667 +lat_2=-71.33333333333333 +lat_0=-90 +lon_0=114 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SR49-50
1200;3237;+proj=lcc +lat_1=-68.66666666666667 +lat_2=-71.33333333333333 +lat_0=-90 +lon_0=126 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SR51-52
1201;3238;+proj=lcc +lat_1=-68.66666666666667 +lat_2=-71.33333333333333 +lat_0=-90 +lon_0=138 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SR53-54
1202;3239;+proj=lcc +lat_1=-68.66666666666667 +lat_2=-71.33333333333333 +lat_0=-90 +lon_0=150 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SR55-56
1203;3240;+proj=lcc +lat_1=-68.66666666666667 +lat_2=-71.33333333333333 +lat_0=-90 +lon_0=162 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SR57-58
1204;3241;+proj=lcc +lat_1=-68.66666666666667 +lat_2=-71.33333333333333 +lat_0=-90 +lon_0=174 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SR59-60
1205;3242;+proj=lcc +lat_1=-72.66666666666667 +lat_2=-75.33333333333333 +lat_0=-90 +lon_0=-153 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SS04-06
1206;3243;+proj=lcc +lat_1=-72.66666666666667 +lat_2=-75.33333333333333 +lat_0=-90 +lon_0=-135 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SS07-09
1207;3244;+proj=lcc +lat_1=-72.66666666666667 +lat_2=-75.33333333333333 +lat_0=-90 +lon_0=-117 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SS10-12
1208;3245;+proj=lcc +lat_1=-72.66666666666667 +lat_2=-75.33333333333333 +lat_0=-90 +lon_0=-99 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SS13-15
1209;3246;+proj=lcc +lat_1=-72.66666666666667 +lat_2=-75.33333333333333 +lat_0=-90 +lon_0=-81 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SS16-18
1210;3247;+proj=lcc +lat_1=-72.66666666666667 +lat_2=-75.33333333333333 +lat_0=-90 +lon_0=-63 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SS19-21
1211;3248;+proj=lcc +lat_1=-72.66666666666667 +lat_2=-75.33333333333333 +lat_0=-90 +lon_0=-27 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SS25-27
1212;3249;+proj=lcc +lat_1=-72.66666666666667 +lat_2=-75.33333333333333 +lat_0=-90 +lon_0=-9 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SS28-30
1213;3250;+proj=lcc +lat_1=-72.66666666666667 +lat_2=-75.33333333333333 +lat_0=-90 +lon_0=9 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SS31-33
1214;3251;+proj=lcc +lat_1=-72.66666666666667 +lat_2=-75.33333333333333 +lat_0=-90 +lon_0=27 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SS34-36
1215;3252;+proj=lcc +lat_1=-72.66666666666667 +lat_2=-75.33333333333333 +lat_0=-90 +lon_0=45 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SS37-39
1216;3253;+proj=lcc +lat_1=-72.66666666666667 +lat_2=-75.33333333333333 +lat_0=-90 +lon_0=63 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SS40-42
1217;3254;+proj=lcc +lat_1=-72.66666666666667 +lat_2=-75.33333333333333 +lat_0=-90 +lon_0=81 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SS43-45
1218;3255;+proj=lcc +lat_1=-72.66666666666667 +lat_2=-75.33333333333333 +lat_0=-90 +lon_0=99 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SS46-48
1219;3256;+proj=lcc +lat_1=-72.66666666666667 +lat_2=-75.33333333333333 +lat_0=-90 +lon_0=117 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SS49-51
1220;3257;+proj=lcc +lat_1=-72.66666666666667 +lat_2=-75.33333333333333 +lat_0=-90 +lon_0=135 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SS52-54
1221;3258;+proj=lcc +lat_1=-72.66666666666667 +lat_2=-75.33333333333333 +lat_0=-90 +lon_0=153 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SS55-57
1222;3259;+proj=lcc +lat_1=-72.66666666666667 +lat_2=-75.33333333333333 +lat_0=-90 +lon_0=171 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SS58-60
1223;3260;+proj=lcc +lat_1=-76.66666666666667 +lat_2=-79.33333333333333 +lat_0=-90 +lon_0=-168 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW ST01-04
1224;3261;+proj=lcc +lat_1=-76.66666666666667 +lat_2=-79.33333333333333 +lat_0=-90 +lon_0=-144 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW ST05-08
1225;3262;+proj=lcc +lat_1=-76.66666666666667 +lat_2=-79.33333333333333 +lat_0=-90 +lon_0=-120 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW ST09-12
1226;3263;+proj=lcc +lat_1=-76.66666666666667 +lat_2=-79.33333333333333 +lat_0=-90 +lon_0=-96 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW ST13-16
1227;3264;+proj=lcc +lat_1=-76.66666666666667 +lat_2=-79.33333333333333 +lat_0=-90 +lon_0=-72 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW ST17-20
1228;3265;+proj=lcc +lat_1=-76.66666666666667 +lat_2=-79.33333333333333 +lat_0=-90 +lon_0=-48 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW ST21-24
1229;3266;+proj=lcc +lat_1=-76.66666666666667 +lat_2=-79.33333333333333 +lat_0=-90 +lon_0=-24 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW ST25-28
1230;3267;+proj=lcc +lat_1=-76.66666666666667 +lat_2=-79.33333333333333 +lat_0=-90 +lon_0=0 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW ST29-32
1231;3268;+proj=lcc +lat_1=-76.66666666666667 +lat_2=-79.33333333333333 +lat_0=-90 +lon_0=24 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW ST33-36
1232;3269;+proj=lcc +lat_1=-76.66666666666667 +lat_2=-79.33333333333333 +lat_0=-90 +lon_0=48 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW ST37-40
1233;3270;+proj=lcc +lat_1=-76.66666666666667 +lat_2=-79.33333333333333 +lat_0=-90 +lon_0=72 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW ST41-44
1234;3271;+proj=lcc +lat_1=-76.66666666666667 +lat_2=-79.33333333333333 +lat_0=-90 +lon_0=96 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW ST45-48
1235;3272;+proj=lcc +lat_1=-76.66666666666667 +lat_2=-79.33333333333333 +lat_0=-90 +lon_0=120 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW ST49-52
1236;3273;+proj=lcc +lat_1=-76.66666666666667 +lat_2=-79.33333333333333 +lat_0=-90 +lon_0=144 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW ST53-56
1237;3274;+proj=lcc +lat_1=-76.66666666666667 +lat_2=-79.33333333333333 +lat_0=-90 +lon_0=168 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW ST57-60
1238;3275;+proj=stere +lat_0=-90 +lat_ts=-80.23861111111111 +lon_0=-165 +k=1 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SU01-05
1239;3276;+proj=stere +lat_0=-90 +lat_ts=-80.23861111111111 +lon_0=-135 +k=1 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SU06-10
1240;3277;+proj=stere +lat_0=-90 +lat_ts=-80.23861111111111 +lon_0=-105 +k=1 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SU11-15
1241;3278;+proj=stere +lat_0=-90 +lat_ts=-80.23861111111111 +lon_0=-75 +k=1 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SU16-20
1242;3279;+proj=stere +lat_0=-90 +lat_ts=-80.23861111111111 +lon_0=-45 +k=1 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SU21-25
1243;3280;+proj=stere +lat_0=-90 +lat_ts=-80.23861111111111 +lon_0=-15 +k=1 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SU26-30
1244;3281;+proj=stere +lat_0=-90 +lat_ts=-80.23861111111111 +lon_0=15 +k=1 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SU31-35
1245;3282;+proj=stere +lat_0=-90 +lat_ts=-80.23861111111111 +lon_0=45 +k=1 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SU36-40
1246;3283;+proj=stere +lat_0=-90 +lat_ts=-80.23861111111111 +lon_0=75 +k=1 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SU41-45
1247;3284;+proj=stere +lat_0=-90 +lat_ts=-80.23861111111111 +lon_0=105 +k=1 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SU46-50
1248;3285;+proj=stere +lat_0=-90 +lat_ts=-80.23861111111111 +lon_0=135 +k=1 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SU51-55
1249;3286;+proj=stere +lat_0=-90 +lat_ts=-80.23861111111111 +lon_0=165 +k=1 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SU56-60
1250;3287;+proj=stere +lat_0=-90 +lat_ts=-80.23861111111111 +lon_0=-150 +k=1 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SV01-10
1251;3288;+proj=stere +lat_0=-90 +lat_ts=-80.23861111111111 +lon_0=-90 +k=1 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SV11-20
1252;3289;+proj=stere +lat_0=-90 +lat_ts=-80.23861111111111 +lon_0=-30 +k=1 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SV21-30
1253;3290;+proj=stere +lat_0=-90 +lat_ts=-80.23861111111111 +lon_0=30 +k=1 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SV31-40
1254;3291;+proj=stere +lat_0=-90 +lat_ts=-80.23861111111111 +lon_0=90 +k=1 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SV41-50
1255;3292;+proj=stere +lat_0=-90 +lat_ts=-80.23861111111111 +lon_0=150 +k=1 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SV51-60
1256;3293;+proj=stere +lat_0=-90 +lat_ts=-80.23861111111111 +lon_0=0 +k=1 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / SCAR IMW SW01-60
1257;3294;+proj=lcc +lat_1=-76.66666666666667 +lat_2=-79.33333333333333 +lat_0=-78 +lon_0=162 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / USGS Transantarctic Mountains
1258;3296;+proj=utm +zone=5 +south +ellps=GRS80 +units=m +no_defs;RGPF / UTM zone 5S
1259;3297;+proj=utm +zone=6 +south +ellps=GRS80 +units=m +no_defs;RGPF / UTM zone 6S
1260;3298;+proj=utm +zone=7 +south +ellps=GRS80 +units=m +no_defs;RGPF / UTM zone 7S
1261;3299;+proj=utm +zone=8 +south +ellps=GRS80 +units=m +no_defs;RGPF / UTM zone 8S
1262;3300;+proj=lcc +lat_1=59.33333333333334 +lat_2=58 +lat_0=57.51755393055556 +lon_0=24 +x_0=500000 +y_0=6375000 +ellps=GRS80 +towgs84=0.055,-0.541,-0.185,0.0183,-0.0003,-0.007,-0.014 +units=m +no_defs;Estonian Coordinate System of 1992
1263;3301;+proj=lcc +lat_1=59.33333333333334 +lat_2=58 +lat_0=57.51755393055556 +lon_0=24 +x_0=500000 +y_0=6375000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;Estonian Coordinate System of 1997
1264;3302;+proj=utm +zone=7 +south +ellps=intl +units=m +no_defs;IGN63 Hiva Oa / UTM zone 7S
1265;3303;+proj=utm +zone=7 +south +ellps=intl +towgs84=347.103,1078.12,2623.92,-33.8875,70.6773,-9.3943,186.074 +units=m +no_defs;Fatu Iva 72 / UTM zone 7S
1266;3304;+proj=utm +zone=6 +south +ellps=intl +units=m +no_defs;Tahiti 79 / UTM zone 6S
1267;3305;+proj=utm +zone=6 +south +ellps=intl +towgs84=215.525,149.593,176.229,-3.2624,-1.692,-1.1571,10.4773 +units=m +no_defs;Moorea 87 / UTM zone 6S
1268;3306;+proj=utm +zone=5 +south +ellps=intl +towgs84=217.037,86.959,23.956,0,0,0,0 +units=m +no_defs;Maupiti 83 / UTM zone 5S
1269;3307;+proj=utm +zone=39 +ellps=WGS84 +towgs84=0,-0.15,0.68,0,0,0,0 +units=m +no_defs;Nakhl-e Ghanem / UTM zone 39N
1270;3308;+proj=lcc +lat_1=-30.75 +lat_2=-35.75 +lat_0=-33.25 +lon_0=147 +x_0=9300000 +y_0=4500000 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;GDA94 / NSW Lambert
1271;3309;+proj=aea +lat_1=34 +lat_2=40.5 +lat_0=0 +lon_0=-120 +x_0=0 +y_0=-4000000 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / California Albers
1272;3310;+proj=aea +lat_1=34 +lat_2=40.5 +lat_0=0 +lon_0=-120 +x_0=0 +y_0=-4000000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / California Albers
1273;3311;+proj=aea +lat_1=34 +lat_2=40.5 +lat_0=0 +lon_0=-120 +x_0=0 +y_0=-4000000 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / California Albers
1274;3312;+proj=utm +zone=21 +ellps=intl +towgs84=-186,230,110,0,0,0,0 +units=m +no_defs;CSG67 / UTM zone 21N
1275;3313;+proj=utm +zone=21 +ellps=GRS80 +towgs84=2,2,-2,0,0,0,0 +units=m +no_defs;RGFG95 / UTM zone 21N
1276;3314;+proj=lcc +lat_1=-6.5 +lat_2=-11.5 +lat_0=0 +lon_0=26 +x_0=0 +y_0=0 +ellps=clrk66 +units=m +no_defs;Katanga 1955 / Katanga Lambert
1277;3315;+proj=tmerc +lat_0=-9 +lon_0=26 +k=0.999800 +x_0=0 +y_0=0 +ellps=clrk66 +units=m +no_defs;Katanga 1955 / Katanga TM
1278;3316;+proj=tmerc +lat_0=0 +lon_0=22 +k=0.999900 +x_0=500000 +y_0=10000000 +ellps=clrk80 +units=m +no_defs;Kasai 1953 / Congo TM zone 22
1279;3317;+proj=tmerc +lat_0=0 +lon_0=24 +k=0.999900 +x_0=500000 +y_0=10000000 +ellps=clrk80 +units=m +no_defs;Kasai 1953 / Congo TM zone 24
1280;3318;+proj=tmerc +lat_0=0 +lon_0=12 +k=0.999900 +x_0=500000 +y_0=10000000 +ellps=clrk80 +units=m +no_defs;IGC 1962 / Congo TM zone 12
1281;3319;+proj=tmerc +lat_0=0 +lon_0=14 +k=0.999900 +x_0=500000 +y_0=10000000 +ellps=clrk80 +units=m +no_defs;IGC 1962 / Congo TM zone 14
1282;3320;+proj=tmerc +lat_0=0 +lon_0=16 +k=0.999900 +x_0=500000 +y_0=10000000 +ellps=clrk80 +units=m +no_defs;IGC 1962 / Congo TM zone 16
1283;3321;+proj=tmerc +lat_0=0 +lon_0=18 +k=0.999900 +x_0=500000 +y_0=10000000 +ellps=clrk80 +units=m +no_defs;IGC 1962 / Congo TM zone 18
1284;3322;+proj=tmerc +lat_0=0 +lon_0=20 +k=0.999900 +x_0=500000 +y_0=10000000 +ellps=clrk80 +units=m +no_defs;IGC 1962 / Congo TM zone 20
1285;3323;+proj=tmerc +lat_0=0 +lon_0=22 +k=0.999900 +x_0=500000 +y_0=10000000 +ellps=clrk80 +units=m +no_defs;IGC 1962 / Congo TM zone 22
1286;3324;+proj=tmerc +lat_0=0 +lon_0=24 +k=0.999900 +x_0=500000 +y_0=10000000 +ellps=clrk80 +units=m +no_defs;IGC 1962 / Congo TM zone 24
1287;3325;+proj=tmerc +lat_0=0 +lon_0=26 +k=0.999900 +x_0=500000 +y_0=10000000 +ellps=clrk80 +units=m +no_defs;IGC 1962 / Congo TM zone 26
1288;3326;+proj=tmerc +lat_0=0 +lon_0=28 +k=0.999900 +x_0=500000 +y_0=10000000 +ellps=clrk80 +units=m +no_defs;IGC 1962 / Congo TM zone 28
1289;3327;+proj=tmerc +lat_0=0 +lon_0=30 +k=0.999900 +x_0=500000 +y_0=10000000 +ellps=clrk80 +units=m +no_defs;IGC 1962 / Congo TM zone 30
1290;3328;+proj=sterea +lat_0=52.16666666666666 +lon_0=19.16666666666667 +k=0.999714 +x_0=500000 +y_0=500000 +ellps=krass +towgs84=33.4,-146.6,-76.3,-0.359,-0.053,0.844,-0.84 +units=m +no_defs;Pulkovo 1942(58) / GUGiK-80
1291;3329;+proj=tmerc +lat_0=0 +lon_0=15 +k=1.000000 +x_0=5500000 +y_0=0 +ellps=krass +towgs84=33.4,-146.6,-76.3,-0.359,-0.053,0.844,-0.84 +units=m +no_defs;Pulkovo 1942(58) / 3-degree Gauss-Kruger zone 5
1292;3330;+proj=tmerc +lat_0=0 +lon_0=18 +k=1.000000 +x_0=6500000 +y_0=0 +ellps=krass +towgs84=33.4,-146.6,-76.3,-0.359,-0.053,0.844,-0.84 +units=m +no_defs;Pulkovo 1942(58) / 3-degree Gauss-Kruger zone 6
1293;3331;+proj=tmerc +lat_0=0 +lon_0=21 +k=1.000000 +x_0=7500000 +y_0=0 +ellps=krass +towgs84=33.4,-146.6,-76.3,-0.359,-0.053,0.844,-0.84 +units=m +no_defs;Pulkovo 1942(58) / 3-degree Gauss-Kruger zone 7
1294;3332;+proj=tmerc +lat_0=0 +lon_0=24 +k=1.000000 +x_0=8500000 +y_0=0 +ellps=krass +towgs84=33.4,-146.6,-76.3,-0.359,-0.053,0.844,-0.84 +units=m +no_defs;Pulkovo 1942(58) / 3-degree Gauss-Kruger zone 8
1295;3333;+proj=tmerc +lat_0=0 +lon_0=15 +k=1.000000 +x_0=3500000 +y_0=0 +ellps=krass +towgs84=33.4,-146.6,-76.3,-0.359,-0.053,0.844,-0.84 +units=m +no_defs;Pulkovo 1942(58) / Gauss-Kruger zone 3
1296;3334;+proj=tmerc +lat_0=0 +lon_0=21 +k=1.000000 +x_0=4500000 +y_0=0 +ellps=krass +towgs84=33.4,-146.6,-76.3,-0.359,-0.053,0.844,-0.84 +units=m +no_defs;Pulkovo 1942(58) / Gauss-Kruger zone 4
1297;3335;+proj=tmerc +lat_0=0 +lon_0=27 +k=1.000000 +x_0=5500000 +y_0=0 +ellps=krass +towgs84=33.4,-146.6,-76.3,-0.359,-0.053,0.844,-0.84 +units=m +no_defs;Pulkovo 1942(58) / Gauss-Kruger zone 5
1298;3336;+proj=utm +zone=42 +south +ellps=intl +towgs84=145,-187,103,0,0,0,0 +units=m +no_defs;IGN 1962 Kerguelen / UTM zone 42S
1299;3337;+proj=lcc +lat_1=-20.19506944444445 +lat_0=-20.19506944444445 +lon_0=57.52182777777778 +k_0=1 +x_0=1000000 +y_0=1000000 +ellps=clrk80 +towgs84=-770.1,158.4,-498.2,0,0,0,0 +units=m +no_defs;Le Pouce 1934 / Mauritius Grid
1300;3339;+proj=tmerc +lat_0=0 +lon_0=12 +k=0.999900 +x_0=500000 +y_0=10000000 +ellps=clrk80 +towgs84=-79.9,-158,-168.9,0,0,0,0 +units=m +no_defs;IGCB 1955 / Congo TM zone 12
1301;3340;+proj=tmerc +lat_0=0 +lon_0=14 +k=0.999900 +x_0=500000 +y_0=10000000 +ellps=clrk80 +towgs84=-79.9,-158,-168.9,0,0,0,0 +units=m +no_defs;IGCB 1955 / Congo TM zone 14
1302;3341;+proj=tmerc +lat_0=0 +lon_0=16 +k=0.999900 +x_0=500000 +y_0=10000000 +ellps=clrk80 +towgs84=-79.9,-158,-168.9,0,0,0,0 +units=m +no_defs;IGCB 1955 / Congo TM zone 16
1303;3342;+proj=utm +zone=33 +south +ellps=clrk80 +towgs84=-79.9,-158,-168.9,0,0,0,0 +units=m +no_defs;IGCB 1955 / UTM zone 33S
1304;3343;+proj=utm +zone=28 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;Mauritania 1999 / UTM zone 28N
1305;3344;+proj=utm +zone=29 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;Mauritania 1999 / UTM zone 29N
1306;3345;+proj=utm +zone=30 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;Mauritania 1999 / UTM zone 30N
1307;3346;+proj=tmerc +lat_0=0 +lon_0=24 +k=0.999800 +x_0=500000 +y_0=0 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;LKS94 / Lithuania TM
1308;3347;+proj=lcc +lat_1=49 +lat_2=77 +lat_0=63.390675 +lon_0=-91.86666666666666 +x_0=6200000 +y_0=3000000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Statistics Canada Lambert
1309;3348;+proj=lcc +lat_1=49 +lat_2=77 +lat_0=63.390675 +lon_0=-91.86666666666666 +x_0=6200000 +y_0=3000000 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / Statistics Canada Lambert
1310;3349;+proj=merc +lon_0=-150 +k=1.000000 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / PDC Mercator
1311;3350;+proj=tmerc +lat_0=0.1 +lon_0=21.95 +k=1.000000 +x_0=250000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / CS63 zone C0
1312;3351;+proj=tmerc +lat_0=0.1 +lon_0=24.95 +k=1.000000 +x_0=1250000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / CS63 zone C1
1313;3352;+proj=tmerc +lat_0=0.1 +lon_0=27.95 +k=1.000000 +x_0=2250000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / CS63 zone C2
1314;3353;+proj=utm +zone=32 +south +ellps=intl +units=m +no_defs;Mhast (onshore) / UTM zone 32S
1315;3354;+proj=utm +zone=32 +south +ellps=intl +units=m +no_defs;Mhast (offshore) / UTM zone 32S
1316;3355;+proj=tmerc +lat_0=30 +lon_0=31 +k=1.000000 +x_0=615000 +y_0=810000 +ellps=helmert +towgs84=-146.21,112.63,4.05,0,0,0,0 +units=m +no_defs;Egypt Gulf of Suez S-650 TL / Red Belt
1317;3356;+proj=utm +zone=17 +ellps=clrk66 +towgs84=67.8,106.1,138.8,0,0,0,0 +units=m +no_defs;Grand Cayman 1959 / UTM zone 17N
1318;3357;+proj=utm +zone=17 +ellps=clrk66 +units=m +no_defs;Little Cayman 1961 / UTM zone 17N
1319;3358;+proj=lcc +lat_1=36.16666666666666 +lat_2=34.33333333333334 +lat_0=33.75 +lon_0=-79 +x_0=609601.22 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / North Carolina
1320;3359;+proj=lcc +lat_1=36.16666666666666 +lat_2=34.33333333333334 +lat_0=33.75 +lon_0=-79 +x_0=609601.2192024385 +y_0=0 +ellps=GRS80 +to_meter=0.3048 +no_defs;NAD83(HARN) / North Carolina (ftUS) (deprecated)
1321;3360;+proj=lcc +lat_1=34.83333333333334 +lat_2=32.5 +lat_0=31.83333333333333 +lon_0=-81 +x_0=609600 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / South Carolina
1322;3361;+proj=lcc +lat_1=34.83333333333334 +lat_2=32.5 +lat_0=31.83333333333333 +lon_0=-81 +x_0=609600 +y_0=0 +ellps=GRS80 +to_meter=0.3048 +no_defs;NAD83(HARN) / South Carolina (ft)
1323;3362;+proj=lcc +lat_1=41.95 +lat_2=40.88333333333333 +lat_0=40.16666666666666 +lon_0=-77.75 +x_0=600000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Pennsylvania North
1324;3363;+proj=lcc +lat_1=41.95 +lat_2=40.88333333333333 +lat_0=40.16666666666666 +lon_0=-77.75 +x_0=600000 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Pennsylvania North (ftUS)
1325;3364;+proj=lcc +lat_1=40.96666666666667 +lat_2=39.93333333333333 +lat_0=39.33333333333334 +lon_0=-77.75 +x_0=600000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(HARN) / Pennsylvania South
1326;3365;+proj=lcc +lat_1=40.96666666666667 +lat_2=39.93333333333333 +lat_0=39.33333333333334 +lon_0=-77.75 +x_0=600000 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / Pennsylvania South (ftUS)
1327;3366;+proj=cass +lat_0=22.31213333333334 +lon_0=114.1785555555556 +x_0=40243.57775604237 +y_0=19069.93351512578 +a=6378293.645208759 +b=6356617.987679838 +units=m +no_defs;Hong Kong 1963 Grid System (deprecated)
1328;3367;+proj=utm +zone=28 +ellps=clrk80 +units=m +no_defs;IGN Astro 1960 / UTM zone 28N
1329;3368;+proj=utm +zone=29 +ellps=clrk80 +units=m +no_defs;IGN Astro 1960 / UTM zone 29N
1330;3369;+proj=utm +zone=30 +ellps=clrk80 +units=m +no_defs;IGN Astro 1960 / UTM zone 30N
1331;3370;+proj=utm +zone=59 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / UTM zone 59N
1332;3371;+proj=utm +zone=60 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / UTM zone 60N
1333;3372;+proj=utm +zone=59 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / UTM zone 59N
1334;3373;+proj=utm +zone=60 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / UTM zone 60N
1335;3374;+proj=utm +zone=29 +ellps=intl +units=m +no_defs;FD54 / UTM zone 29N
1336;3375;+proj=omerc +lat_0=4 +lonc=102.25 +alpha=323.0257964666666 +k=0.99984 +x_0=804671 +y_0=0 +ellps=GRS80 +units=m +no_defs;GDM2000 / Peninsula RSO
1337;3376;+proj=omerc +lat_0=4 +lonc=115 +alpha=53.31580995 +k=0.99984 +x_0=0 +y_0=0 +ellps=GRS80 +units=m +no_defs;GDM2000 / East Malaysia BRSO
1338;3377;+proj=cass +lat_0=2.121679744444445 +lon_0=103.4279362361111 +x_0=-14810.562 +y_0=8758.32 +ellps=GRS80 +units=m +no_defs;GDM2000 / Johor Grid
1339;3378;+proj=cass +lat_0=2.682347636111111 +lon_0=101.9749050416667 +x_0=3673.785 +y_0=-4240.573 +ellps=GRS80 +units=m +no_defs;GDM2000 / Sembilan and Melaka Grid
1340;3379;+proj=cass +lat_0=3.769388088888889 +lon_0=102.3682989833333 +x_0=-7368.228 +y_0=6485.858 +ellps=GRS80 +units=m +no_defs;GDM2000 / PahangGrid
1341;3380;+proj=cass +lat_0=3.68464905 +lon_0=101.3891079138889 +x_0=-34836.161 +y_0=56464.049 +ellps=GRS80 +units=m +no_defs;GDM2000 / Selangor Grid
1342;3381;+proj=cass +lat_0=4.9762852 +lon_0=103.070275625 +x_0=19594.245 +y_0=3371.895 +ellps=GRS80 +units=m +no_defs;GDM2000 / Terengganu Grid
1343;3382;+proj=cass +lat_0=5.421517541666667 +lon_0=100.3443769638889 +x_0=-23.414 +y_0=62.283 +ellps=GRS80 +units=m +no_defs;GDM2000 / Pinang Grid
1344;3383;+proj=cass +lat_0=5.964672713888889 +lon_0=100.6363711111111 +x_0=0 +y_0=0 +ellps=GRS80 +units=m +no_defs;GDM2000 / Kedah and Perlis Grid
1345;3384;+proj=cass +lat_0=4.859063022222222 +lon_0=100.8154105861111 +x_0=-1.769 +y_0=133454.779 +ellps=GRS80 +units=m +no_defs;GDM2000 / Perak Grid
1346;3385;+proj=cass +lat_0=5.972543658333334 +lon_0=102.2952416694444 +x_0=13227.851 +y_0=8739.894 +ellps=GRS80 +units=m +no_defs;GDM2000 / Kelantan Grid
1347;3386;+proj=tmerc +lat_0=0 +lon_0=18 +k=1.000000 +x_0=500000 +y_0=0 +ellps=intl +units=m +no_defs;KKJ / Finland zone 0
1348;3387;+proj=tmerc +lat_0=0 +lon_0=33 +k=1.000000 +x_0=5500000 +y_0=0 +ellps=intl +units=m +no_defs;KKJ / Finland zone 5
1349;3388;+proj=merc +lon_0=51 +k=1.000000 +x_0=0 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Caspian Sea Mercator
1350;3389;+proj=tmerc +lat_0=0 +lon_0=180 +k=1.000000 +x_0=60500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / 3-degree Gauss-Kruger zone 60
1351;3390;+proj=tmerc +lat_0=0 +lon_0=180 +k=1.000000 +x_0=60500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / 3-degree Gauss-Kruger zone 60
1352;3391;+proj=utm +zone=37 +ellps=clrk80 +towgs84=84.1,-320.1,218.7,0,0,0,0 +units=m +no_defs;Karbala 1979 (Polservice) / UTM zone 37N
1353;3392;+proj=utm +zone=38 +ellps=clrk80 +towgs84=84.1,-320.1,218.7,0,0,0,0 +units=m +no_defs;Karbala 1979 (Polservice) / UTM zone 38N
1354;3393;+proj=utm +zone=39 +ellps=clrk80 +towgs84=84.1,-320.1,218.7,0,0,0,0 +units=m +no_defs;Karbala 1979 (Polservice) / UTM zone 39N
1355;3394;+proj=lcc +lat_1=32.5 +lat_0=32.5 +lon_0=45 +k_0=0.9987864078000001 +x_0=1500000 +y_0=1166200 +ellps=clrk80 +units=m +no_defs;Nahrwan 1934 / Iraq zone
1356;3395;+proj=merc +lon_0=0 +k=1.000000 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / World Mercator
1357;3396;+proj=tmerc +lat_0=0 +lon_0=9 +k=1.000000 +x_0=3500000 +y_0=0 +ellps=bessel +units=m +no_defs;PD/83 / 3-degree Gauss zone 3
1358;3397;+proj=tmerc +lat_0=0 +lon_0=12 +k=1.000000 +x_0=4500000 +y_0=0 +ellps=bessel +units=m +no_defs;PD/83 / 3-degree Gauss zone 4
1359;3398;+proj=tmerc +lat_0=0 +lon_0=12 +k=1.000000 +x_0=4500000 +y_0=0 +ellps=bessel +units=m +no_defs;RD/83 / 3-degree Gauss zone 4
1360;3399;+proj=tmerc +lat_0=0 +lon_0=15 +k=1.000000 +x_0=5500000 +y_0=0 +ellps=bessel +units=m +no_defs;RD/83 / 3-degree Gauss zone 5
1361;3400;+proj=tmerc +lat_0=0 +lon_0=-115 +k=0.999200 +x_0=500000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Alberta 10-TM (Forest)
1362;3401;+proj=tmerc +lat_0=0 +lon_0=-115 +k=0.999200 +x_0=0 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Alberta 10-TM (Resource)
1363;3402;+proj=tmerc +lat_0=0 +lon_0=-115 +k=0.999200 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / Alberta 10-TM (Forest)
1364;3403;+proj=tmerc +lat_0=0 +lon_0=-115 +k=0.999200 +x_0=0 +y_0=0 +ellps=GRS80 +units=m +no_defs;NAD83(CSRS) / Alberta 10-TM (Resource)
1365;3404;+proj=lcc +lat_1=36.16666666666666 +lat_2=34.33333333333334 +lat_0=33.75 +lon_0=-79 +x_0=609601.2192024384 +y_0=0 +ellps=GRS80 +to_meter=0.3048006096012192 +no_defs;NAD83(HARN) / North Carolina (ftUS)
1366;3405;+proj=utm +zone=48 +ellps=WGS84 +units=m +no_defs;VN-2000 / UTM zone 48N
1367;3406;+proj=utm +zone=49 +ellps=WGS84 +units=m +no_defs;VN-2000 / UTM zone 49N
1368;3407;+proj=cass +lat_0=22.31213333333334 +lon_0=114.1785555555556 +x_0=40243.57775604237 +y_0=19069.93351512578 +a=6378293.645208759 +b=6356617.987679838 +to_meter=0.3047972654 +no_defs;Hong Kong 1963 Grid System
1369;3439;+proj=utm +zone=39 +ellps=clrk80 +units=m +no_defs;PSD93 / UTM zone 39N
1370;3440;+proj=utm +zone=40 +ellps=clrk80 +units=m +no_defs;PSD93 / UTM zone 40N
1371;3561;+proj=tmerc +lat_0=18.83333333333333 +lon_0=-155.5 +k=0.999967 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +to_meter=0.3048006096012192 +no_defs;Old Hawaiian / Hawaii zone 1
1372;3562;+proj=tmerc +lat_0=20.33333333333333 +lon_0=-156.6666666666667 +k=0.999967 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +to_meter=0.3048006096012192 +no_defs;Old Hawaiian / Hawaii zone 2
1373;3563;+proj=tmerc +lat_0=21.16666666666667 +lon_0=-158 +k=0.999990 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +to_meter=0.3048006096012192 +no_defs;Old Hawaiian / Hawaii zone 3
1374;3564;+proj=tmerc +lat_0=21.83333333333333 +lon_0=-159.5 +k=0.999990 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +to_meter=0.3048006096012192 +no_defs;Old Hawaiian / Hawaii zone 4
1375;3565;+proj=tmerc +lat_0=21.66666666666667 +lon_0=-160.1666666666667 +k=1.000000 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +to_meter=0.3048006096012192 +no_defs;Old Hawaiian / Hawaii zone 5
1376;3920;+proj=utm +zone=20 +ellps=clrk66 +towgs84=11,72,-101,0,0,0,0 +units=m +no_defs;Puerto Rico / UTM zone 20N
1377;3991;+proj=lcc +lat_1=18.43333333333333 +lat_2=18.03333333333333 +lat_0=17.83333333333333 +lon_0=-66.43333333333334 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +towgs84=11,72,-101,0,0,0,0 +to_meter=0.3048006096012192 +no_defs;Puerto Rico State Plane CS of 1927
1378;3992;+proj=lcc +lat_1=18.43333333333333 +lat_2=18.03333333333333 +lat_0=17.83333333333333 +lon_0=-66.43333333333334 +x_0=152400.3048006096 +y_0=30480.06096012192 +ellps=clrk66 +towgs84=11,72,-101,0,0,0,0 +to_meter=0.3048006096012192 +no_defs;Puerto Rico / St. Croix
1379;4001;+proj=longlat +ellps=airy +no_defs;Unknown datum based upon the Airy 1830 ellipsoid
1380;4002;+proj=longlat +a=6377340.189 +b=6356034.447938534 +no_defs;Unknown datum based upon the Airy Modified 1849 ellipsoid
1381;4003;+proj=longlat +ellps=aust_SA +no_defs;Unknown datum based upon the Australian National Spheroid
1382;4004;+proj=longlat +ellps=bessel +no_defs;Unknown datum based upon the Bessel 1841 ellipsoid
1383;4005;+proj=longlat +a=6377492.018 +b=6356173.508712696 +no_defs;Unknown datum based upon the Bessel Modified ellipsoid
1384;4006;+proj=longlat +ellps=bess_nam +no_defs;Unknown datum based upon the Bessel Namibia ellipsoid
1385;4007;+proj=longlat +a=6378293.645208759 +b=6356617.987679838 +no_defs;Unknown datum based upon the Clarke 1858 ellipsoid
1386;4008;+proj=longlat +ellps=clrk66 +no_defs;Unknown datum based upon the Clarke 1866 ellipsoid
1387;4009;+proj=longlat +a=6378450.047548896 +b=6356826.621488444 +no_defs;Unknown datum based upon the Clarke 1866 Michigan ellipsoid
1388;4010;+proj=longlat +a=6378300.789 +b=6356566.435 +no_defs;Unknown datum based upon the Clarke 1880 (Benoit) ellipsoid
1389;4011;+proj=longlat +a=6378249.2 +b=6356515 +no_defs;Unknown datum based upon the Clarke 1880 (IGN) ellipsoid
1390;4012;+proj=longlat +ellps=clrk80 +no_defs;Unknown datum based upon the Clarke 1880 (RGS) ellipsoid
1391;4013;+proj=longlat +a=6378249.145 +b=6356514.966398753 +no_defs;Unknown datum based upon the Clarke 1880 (Arc) ellipsoid
1392;4014;+proj=longlat +a=6378249.2 +b=6356514.996941779 +no_defs;Unknown datum based upon the Clarke 1880 (SGA 1922) ellipsoid
1393;4015;+proj=longlat +a=6377276.345 +b=6356075.413140239 +no_defs;Unknown datum based upon the Everest 1830 (1937 Adjustment) ellipsoid
1394;4016;+proj=longlat +ellps=evrstSS +no_defs;Unknown datum based upon the Everest 1830 (1967 Definition) ellipsoid
1395;4018;+proj=longlat +a=6377304.063 +b=6356103.038993155 +no_defs;Unknown datum based upon the Everest 1830 Modified ellipsoid
1396;4019;+proj=longlat +ellps=GRS80 +no_defs;Unknown datum based upon the GRS 1980 ellipsoid
1397;4020;+proj=longlat +ellps=helmert +no_defs;Unknown datum based upon the Helmert 1906 ellipsoid
1398;4021;+proj=longlat +a=6378160 +b=6356774.50408554 +no_defs;Unknown datum based upon the Indonesian National Spheroid
1399;4022;+proj=longlat +ellps=intl +no_defs;Unknown datum based upon the International 1924 ellipsoid
1400;4024;+proj=longlat +ellps=krass +no_defs;Unknown datum based upon the Krassowsky 1940 ellipsoid
1401;4025;+proj=longlat +ellps=WGS66 +no_defs;Unknown datum based upon the NWL 9D ellipsoid
1402;4027;+proj=longlat +a=6376523 +b=6355862.933255573 +no_defs;Unknown datum based upon the Plessis 1817 ellipsoid
1403;4028;+proj=longlat +a=6378298.3 +b=6356657.142669562 +no_defs;Unknown datum based upon the Struve 1860 ellipsoid
1404;4029;+proj=longlat +a=6378300 +b=6356751.689189189 +no_defs;Unknown datum based upon the War Office ellipsoid
1405;4030;+proj=longlat +ellps=WGS84 +no_defs;Unknown datum based upon the WGS 84 ellipsoid
1406;4031;+proj=longlat +ellps=WGS84 +no_defs;Unknown datum based upon the GEM 10C ellipsoid
1407;4032;+proj=longlat +a=6378136.2 +b=6356751.516927429 +no_defs;Unknown datum based upon the OSU86F ellipsoid
1408;4033;+proj=longlat +a=6378136.3 +b=6356751.616592146 +no_defs;Unknown datum based upon the OSU91A ellipsoid
1409;4034;+proj=longlat +ellps=clrk80 +no_defs;Unknown datum based upon the Clarke 1880 ellipsoid
1410;4035;+proj=longlat +a=6371000 +b=6371000 +no_defs;Unknown datum based upon the Authalic Sphere
1411;4036;+proj=longlat +ellps=GRS67 +no_defs;Unknown datum based upon the GRS 1967 ellipsoid
1412;4041;+proj=longlat +a=6378135 +b=6356750.304921594 +no_defs;Unknown datum based upon the Average Terrestrial System 1977 ellipsoid
1413;4042;+proj=longlat +a=6377299.36559538 +b=6356098.357204817 +no_defs;Unknown datum based upon the Everest (1830 Definition) ellipsoid
1414;4043;+proj=longlat +ellps=WGS72 +no_defs;Unknown datum based upon the WGS 72 ellipsoid
1415;4044;+proj=longlat +a=6377301.243 +b=6356100.230165385 +no_defs;Unknown datum based upon the Everest 1830 (1962 Definition) ellipsoid
1416;4045;+proj=longlat +a=6377299.151 +b=6356098.145120132 +no_defs;Unknown datum based upon the Everest 1830 (1975 Definition) ellipsoid
1417;4047;+proj=longlat +a=6371007 +b=6371007 +no_defs;Unspecified datum based upon the GRS 1980 Authalic Sphere
1418;4052;+proj=longlat +a=6370997 +b=6370997 +no_defs;Unspecified datum based upon the Clarke 1866 Authalic Sphere
1419;4120;+proj=longlat +ellps=bessel +no_defs;Greek
1420;4121;+proj=longlat +ellps=GRS80 +towgs84=-199.87,74.79,246.62,0,0,0,0 +no_defs;GGRS87
1421;4122;+proj=longlat +a=6378135 +b=6356750.304921594 +no_defs;ATS77
1422;4123;+proj=longlat +ellps=intl +no_defs;KKJ
1423;4124;+proj=longlat +ellps=bessel +no_defs;RT90
1424;4125;+proj=longlat +ellps=bessel +towgs84=-404.78,685.68,45.47,0,0,0,0 +no_defs;Samboja
1425;4126;+proj=longlat +ellps=GRS80 +no_defs;LKS94 (ETRS89)
1426;4127;+proj=longlat +ellps=clrk66 +no_defs;Tete
1427;4128;+proj=longlat +ellps=clrk66 +no_defs;Madzansua
1428;4129;+proj=longlat +ellps=clrk66 +no_defs;Observatario
1429;4130;+proj=longlat +ellps=WGS84 +towgs84=0,0,0,-0,-0,-0,0 +no_defs;Moznet
1430;4131;+proj=longlat +a=6377276.345 +b=6356075.413140239 +no_defs;Indian 1960
1431;4132;+proj=longlat +ellps=clrk80 +no_defs;FD58
1432;4133;+proj=longlat +ellps=GRS80 +towgs84=0.055,-0.541,-0.185,0.0183,-0.0003,-0.007,-0.014 +no_defs;EST92
1433;4134;+proj=longlat +ellps=clrk80 +no_defs;PDO Survey Datum 1993
1434;4135;+proj=longlat +ellps=clrk66 +no_defs;Old Hawaiian
1435;4136;+proj=longlat +ellps=clrk66 +no_defs;St. Lawrence Island
1436;4137;+proj=longlat +ellps=clrk66 +no_defs;St. Paul Island
1437;4138;+proj=longlat +ellps=clrk66 +no_defs;St. George Island
1438;4139;+proj=longlat +ellps=clrk66 +towgs84=11,72,-101,0,0,0,0 +no_defs;Puerto Rico
1439;4140;+proj=longlat +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +no_defs;NAD83(CSRS98)
1440;4141;+proj=longlat +ellps=GRS80 +towgs84=-48,55,52,0,0,0,0 +no_defs;Israel
1441;4142;+proj=longlat +ellps=clrk80 +towgs84=-125,53,467,0,0,0,0 +no_defs;Locodjo 1965
1442;4143;+proj=longlat +ellps=clrk80 +towgs84=-124.76,53,466.79,0,0,0,0 +no_defs;Abidjan 1987
1443;4144;+proj=longlat +a=6377276.345 +b=6356075.413140239 +no_defs;Kalianpur 1937
1444;4145;+proj=longlat +a=6377301.243 +b=6356100.230165385 +no_defs;Kalianpur 1962
1445;4146;+proj=longlat +a=6377299.151 +b=6356098.145120132 +towgs84=295,736,257,0,0,0,0 +no_defs;Kalianpur 1975
1446;4147;+proj=longlat +ellps=krass +towgs84=-17.51,-108.32,-62.39,0,0,0,0 +no_defs;Hanoi 1972
1447;4148;+proj=longlat +ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +no_defs;Hartebeesthoek94
1448;4149;+proj=longlat +ellps=bessel +towgs84=674.374,15.056,405.346,0,0,0,0 +no_defs;CH1903
1449;4150;+proj=longlat +ellps=bessel +towgs84=674.374,15.056,405.346,0,0,0,0 +no_defs;CH1903+
1450;4151;+proj=longlat +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +no_defs;CHTRF95
1451;4152;+proj=longlat +ellps=GRS80 +no_defs;NAD83(HARN)
1452;4153;+proj=longlat +ellps=intl +towgs84=-133.63,-157.5,-158.62,0,0,0,0 +no_defs;Rassadiran
1453;4154;+proj=longlat +ellps=intl +no_defs;ED50(ED77)
1454;4155;+proj=longlat +a=6378249.2 +b=6356515 +towgs84=-83,37,124,0,0,0,0 +no_defs;Dabola 1981
1455;4156;+proj=longlat +ellps=bessel +no_defs;S-JTSK
1456;4157;+proj=longlat +a=6378293.645208759 +b=6356617.987679838 +no_defs;Mount Dillon
1457;4158;+proj=longlat +ellps=intl +no_defs;Naparima 1955
1458;4159;+proj=longlat +ellps=intl +no_defs;ELD79
1459;4160;+proj=longlat +ellps=intl +no_defs;Chos Malal 1914
1460;4161;+proj=longlat +ellps=intl +towgs84=27.5,14,186.4,0,0,0,0 +no_defs;Pampa del Castillo
1461;4162;+proj=longlat +ellps=bessel +no_defs;Korean 1985
1462;4163;+proj=longlat +ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +no_defs;Yemen NGN96
1463;4164;+proj=longlat +ellps=krass +towgs84=-76,-138,67,0,0,0,0 +no_defs;South Yemen
1464;4165;+proj=longlat +ellps=intl +towgs84=-173,253,27,0,0,0,0 +no_defs;Bissau
1465;4166;+proj=longlat +ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +no_defs;Korean 1995
1466;4167;+proj=longlat +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +no_defs;NZGD2000
1467;4168;+proj=longlat +a=6378300 +b=6356751.689189189 +towgs84=-199,32,322,0,0,0,0 +no_defs;Accra
1468;4169;+proj=longlat +ellps=clrk66 +towgs84=-115,118,426,0,0,0,0 +no_defs;American Samoa 1962
1469;4170;+proj=longlat +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +no_defs;SIRGAS
1470;4171;+proj=longlat +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +no_defs;RGF93
1471;4172;+proj=longlat +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +no_defs;POSGAR
1472;4173;+proj=longlat +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +no_defs;IRENET95
1473;4174;+proj=longlat +a=6378300 +b=6356751.689189189 +no_defs;Sierra Leone 1924
1474;4175;+proj=longlat +ellps=clrk80 +towgs84=-88,4,101,0,0,0,0 +no_defs;Sierra Leone 1968
1475;4176;+proj=longlat +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +no_defs;Australian Antarctic
1476;4178;+proj=longlat +ellps=krass +towgs84=24,-123,-94,0.02,-0.25,-0.13,1.1 +no_defs;Pulkovo 1942(83)
1477;4179;+proj=longlat +ellps=krass +towgs84=33.4,-146.6,-76.3,-0.359,-0.053,0.844,-0.84 +no_defs;Pulkovo 1942(58)
1478;4180;+proj=longlat +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +no_defs;EST97
1479;4181;+proj=longlat +ellps=intl +towgs84=-193,13.7,-39.3,-0.41,-2.933,2.688,0.43 +no_defs;Luxembourg 1930
1480;4182;+proj=longlat +ellps=intl +no_defs;Azores Occidental 1939
1481;4183;+proj=longlat +ellps=intl +towgs84=-104,167,-38,0,0,0,0 +no_defs;Azores Central 1948
1482;4184;+proj=longlat +ellps=intl +towgs84=-203,141,53,0,0,0,0 +no_defs;Azores Oriental 1940
1483;4185;+proj=longlat +ellps=intl +no_defs;Madeira 1936
1484;4188;+proj=longlat +ellps=airy +towgs84=482.5,-130.6,564.6,-1.042,-0.214,-0.631,8.15 +no_defs;OSNI 1952
1485;4189;+proj=longlat +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +no_defs;REGVEN
1486;4190;+proj=longlat +ellps=GRS80 +no_defs;POSGAR 98
1487;4191;+proj=longlat +ellps=krass +no_defs;Albanian 1987
1488;4192;+proj=longlat +ellps=intl +towgs84=-206.1,-174.7,-87.7,0,0,0,0 +no_defs;Douala 1948
1489;4193;+proj=longlat +a=6378249.2 +b=6356515 +towgs84=-70.9,-151.8,-41.4,0,0,0,0 +no_defs;Manoca 1962
1490;4194;+proj=longlat +ellps=intl +no_defs;Qornoq 1927
1491;4195;+proj=longlat +ellps=intl +towgs84=105,326,-102.5,0,0,0.814,-0.6 +no_defs;Scoresbysund 1952
1492;4196;+proj=longlat +ellps=intl +towgs84=-45,417,-3.5,0,0,0.814,-0.6 +no_defs;Ammassalik 1958
1493;4197;+proj=longlat +ellps=clrk80 +no_defs;Garoua
1494;4198;+proj=longlat +ellps=clrk80 +no_defs;Kousseri
1495;4199;+proj=longlat +ellps=intl +no_defs;Egypt 1930
1496;4200;+proj=longlat +ellps=krass +no_defs;Pulkovo 1995
1497;4201;+proj=longlat +ellps=clrk80 +no_defs;Adindan
1498;4202;+proj=longlat +ellps=aust_SA +no_defs;AGD66
1499;4203;+proj=longlat +ellps=aust_SA +no_defs;AGD84
1500;4204;+proj=longlat +ellps=intl +no_defs;Ain el Abd
1501;4205;+proj=longlat +ellps=krass +towgs84=-43,-163,45,0,0,0,0 +no_defs;Afgooye
1502;4206;+proj=longlat +a=6378249.2 +b=6356515 +no_defs;Agadez
1503;4207;+proj=longlat +ellps=intl +no_defs;Lisbon
1504;4208;+proj=longlat +ellps=intl +no_defs;Aratu
1505;4209;+proj=longlat +a=6378249.145 +b=6356514.966398753 +no_defs;Arc 1950
1506;4210;+proj=longlat +ellps=clrk80 +no_defs;Arc 1960
1507;4211;+proj=longlat +ellps=bessel +no_defs;Batavia
1508;4212;+proj=longlat +ellps=clrk80 +towgs84=31.95,300.99,419.19,0,0,0,0 +no_defs;Barbados 1938
1509;4213;+proj=longlat +a=6378249.2 +b=6356515 +towgs84=-106,-87,188,0,0,0,0 +no_defs;Beduaram
1510;4214;+proj=longlat +ellps=krass +no_defs;Beijing 1954
1511;4215;+proj=longlat +ellps=intl +no_defs;Belge 1950
1512;4216;+proj=longlat +ellps=clrk66 +towgs84=-73,213,296,0,0,0,0 +no_defs;Bermuda 1957
1513;4218;+proj=longlat +ellps=intl +towgs84=307,304,-318,0,0,0,0 +no_defs;Bogota 1975
1514;4219;+proj=longlat +ellps=bessel +towgs84=-384,664,-48,0,0,0,0 +no_defs;Bukit Rimpah
1515;4220;+proj=longlat +ellps=clrk80 +no_defs;Camacupa
1516;4221;+proj=longlat +ellps=intl +no_defs;Campo Inchauspe
1517;4222;+proj=longlat +a=6378249.145 +b=6356514.966398753 +no_defs;Cape
1518;4223;+proj=longlat +a=6378249.2 +b=6356515 +no_defs;Carthage
1519;4224;+proj=longlat +ellps=intl +towgs84=-134,229,-29,0,0,0,0 +no_defs;Chua
1520;4225;+proj=longlat +ellps=intl +towgs84=-206,172,-6,0,0,0,0 +no_defs;Corrego Alegre
1521;4226;+proj=longlat +a=6378249.2 +b=6356515 +no_defs;Cote d'Ivoire
1522;4227;+proj=longlat +a=6378249.2 +b=6356515 +no_defs;Deir ez Zor
1523;4228;+proj=longlat +a=6378249.2 +b=6356515 +no_defs;Douala
1524;4229;+proj=longlat +ellps=helmert +no_defs;Egypt 1907
1525;4230;+proj=longlat +ellps=intl +no_defs;ED50
1526;4231;+proj=longlat +ellps=intl +no_defs;ED87
1527;4232;+proj=longlat +ellps=clrk80 +no_defs;Fahud
1528;4233;+proj=longlat +ellps=intl +towgs84=-133,-321,50,0,0,0,0 +no_defs;Gandajika 1970
1529;4234;+proj=longlat +a=6378249.2 +b=6356515 +no_defs;Garoua
1530;4235;+proj=longlat +ellps=intl +no_defs;Guyane Francaise
1531;4236;+proj=longlat +ellps=intl +towgs84=-637,-549,-203,0,0,0,0 +no_defs;Hu Tzu Shan
1532;4237;+proj=longlat +ellps=GRS67 +no_defs;HD72
1533;4238;+proj=longlat +a=6378160 +b=6356774.50408554 +no_defs;ID74
1534;4239;+proj=longlat +a=6377276.345 +b=6356075.413140239 +towgs84=217,823,299,0,0,0,0 +no_defs;Indian 1954
1535;4240;+proj=longlat +a=6377276.345 +b=6356075.413140239 +no_defs;Indian 1975
1536;4241;+proj=longlat +ellps=clrk80 +no_defs;Jamaica 1875
1537;4242;+proj=longlat +ellps=clrk66 +no_defs;JAD69
1538;4243;+proj=longlat +a=6377299.36559538 +b=6356098.357204817 +no_defs;Kalianpur 1880
1539;4244;+proj=longlat +a=6377276.345 +b=6356075.413140239 +towgs84=-97,787,86,0,0,0,0 +no_defs;Kandawala
1540;4245;+proj=longlat +a=6377304.063 +b=6356103.038993155 +towgs84=-11,851,5,0,0,0,0 +no_defs;Kertau 1968
1541;4246;+proj=longlat +ellps=clrk80 +towgs84=-294.7,-200.1,525.5,0,0,0,0 +no_defs;KOC
1542;4247;+proj=longlat +ellps=intl +towgs84=-273.5,110.6,-357.9,0,0,0,0 +no_defs;La Canoa
1543;4248;+proj=longlat +ellps=intl +no_defs;PSAD56
1544;4249;+proj=longlat +ellps=intl +no_defs;Lake
1545;4250;+proj=longlat +ellps=clrk80 +towgs84=-130,29,364,0,0,0,0 +no_defs;Leigon
1546;4251;+proj=longlat +ellps=clrk80 +towgs84=-90,40,88,0,0,0,0 +no_defs;Liberia 1964
1547;4252;+proj=longlat +a=6378249.2 +b=6356515 +no_defs;Lome
1548;4253;+proj=longlat +ellps=clrk66 +no_defs;Luzon 1911
1549;4254;+proj=longlat +ellps=intl +no_defs;Hito XVIII 1963
1550;4255;+proj=longlat +ellps=intl +towgs84=-333,-222,114,0,0,0,0 +no_defs;Herat North
1551;4256;+proj=longlat +ellps=clrk80 +towgs84=41,-220,-134,0,0,0,0 +no_defs;Mahe 1971
1552;4257;+proj=longlat +ellps=bessel +towgs84=-587.8,519.75,145.76,0,0,0,0 +no_defs;Makassar
1553;4258;+proj=longlat +ellps=GRS80 +no_defs;ETRS89
1554;4259;+proj=longlat +ellps=intl +no_defs;Malongo 1987
1555;4260;+proj=longlat +ellps=clrk80 +towgs84=-70.9,-151.8,-41.4,0,0,0,0 +no_defs;Manoca
1556;4261;+proj=longlat +a=6378249.2 +b=6356515 +towgs84=31,146,47,0,0,0,0 +no_defs;Merchich
1557;4262;+proj=longlat +ellps=bessel +towgs84=639,405,60,0,0,0,0 +no_defs;Massawa
1558;4263;+proj=longlat +ellps=clrk80 +no_defs;Minna
1559;4264;+proj=longlat +ellps=intl +towgs84=-252.95,-4.11,-96.38,0,0,0,0 +no_defs;Mhast
1560;4265;+proj=longlat +ellps=intl +no_defs;Monte Mario
1561;4266;+proj=longlat +a=6378249.2 +b=6356515 +no_defs;M'poraloko
1562;4267;+proj=longlat +ellps=clrk66 +datum=NAD27 +no_defs;NAD27
1563;4268;+proj=longlat +a=6378450.047548896 +b=6356826.621488444 +no_defs;NAD27 Michigan
1564;4269;+proj=longlat +ellps=GRS80 +datum=NAD83 +no_defs;NAD83
1565;4270;+proj=longlat +ellps=clrk80 +no_defs;Nahrwan 1967
1566;4271;+proj=longlat +ellps=intl +no_defs;Naparima 1972
1567;4272;+proj=longlat +ellps=intl +datum=nzgd49 +no_defs;NZGD49
1568;4273;+proj=longlat +a=6377492.018 +b=6356173.508712696 +towgs84=278.3,93,474.5,7.889,0.05,-6.61,6.21 +no_defs;NGO 1948
1569;4274;+proj=longlat +ellps=intl +no_defs;Datum 73
1570;4275;+proj=longlat +a=6378249.2 +b=6356515 +towgs84=-168,-60,320,0,0,0,0 +no_defs;NTF
1571;4276;+proj=longlat +ellps=WGS66 +no_defs;NSWC 9Z-2
1572;4277;+proj=longlat +ellps=airy +no_defs;OSGB 1936
1573;4278;+proj=longlat +ellps=airy +no_defs;OSGB70
1574;4279;+proj=longlat +ellps=airy +no_defs;OS(SN)80
1575;4280;+proj=longlat +ellps=bessel +no_defs;Padang
1576;4281;+proj=longlat +a=6378300.789 +b=6356566.435 +towgs84=-275.722,94.7824,340.894,-8.001,-4.42,-11.821,1 +no_defs;Palestine 1923
1577;4282;+proj=longlat +a=6378249.2 +b=6356515 +no_defs;Pointe Noire
1578;4283;+proj=longlat +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +no_defs;GDA94
1579;4284;+proj=longlat +ellps=krass +no_defs;Pulkovo 1942
1580;4285;+proj=longlat +ellps=intl +no_defs;Qatar 1974
1581;4286;+proj=longlat +ellps=helmert +no_defs;Qatar 1948
1582;4287;+proj=longlat +ellps=intl +towgs84=164,138,-189,0,0,0,0 +no_defs;Qornoq
1583;4288;+proj=longlat +ellps=intl +no_defs;Loma Quintana
1584;4289;+proj=longlat +ellps=bessel +towgs84=565.237,50.0087,465.658,-0.406857,0.350733,-1.87035,4.0812 +no_defs;Amersfoort
1585;4291;+proj=longlat +ellps=GRS67 +no_defs;SAD69
1586;4292;+proj=longlat +ellps=intl +towgs84=-355,21,72,0,0,0,0 +no_defs;Sapper Hill 1943
1587;4293;+proj=longlat +ellps=bess_nam +no_defs;Schwarzeck
1588;4294;+proj=longlat +ellps=bessel +no_defs;Segora
1589;4295;+proj=longlat +ellps=bessel +no_defs;Serindung
1590;4296;+proj=longlat +a=6378249.2 +b=6356515 +no_defs;Sudan
1591;4297;+proj=longlat +ellps=intl +towgs84=-189,-242,-91,0,0,0,0 +no_defs;Tananarive
1592;4298;+proj=longlat +ellps=evrstSS +no_defs;Timbalai 1948
1593;4299;+proj=longlat +a=6377340.189 +b=6356034.447938534 +no_defs;TM65
1594;4300;+proj=longlat +a=6377340.189 +b=6356034.447938534 +no_defs;TM75
1595;4301;+proj=longlat +ellps=bessel +no_defs;Tokyo
1596;4302;+proj=longlat +a=6378293.645208759 +b=6356617.987679838 +no_defs;Trinidad 1903
1597;4303;+proj=longlat +ellps=helmert +no_defs;TC(1948)
1598;4304;+proj=longlat +a=6378249.2 +b=6356515 +towgs84=-73,-247,227,0,0,0,0 +no_defs;Voirol 1875
1599;4306;+proj=longlat +ellps=bessel +no_defs;Bern 1938
1600;4307;+proj=longlat +ellps=clrk80 +no_defs;Nord Sahara 1959
1601;4308;+proj=longlat +ellps=bessel +no_defs;RT38
1602;4309;+proj=longlat +ellps=intl +towgs84=-155,171,37,0,0,0,0 +no_defs;Yacare
1603;4310;+proj=longlat +a=6378249.2 +b=6356515 +no_defs;Yoff
1604;4311;+proj=longlat +ellps=intl +towgs84=-265,120,-358,0,0,0,0 +no_defs;Zanderij
1605;4312;+proj=longlat +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +no_defs;MGI
1606;4313;+proj=longlat +ellps=intl +no_defs;Belge 1972
1607;4314;+proj=longlat +ellps=bessel +datum=potsdam +no_defs;DHDN
1608;4315;+proj=longlat +a=6378249.2 +b=6356515 +towgs84=-23,259,-9,0,0,0,0 +no_defs;Conakry 1905
1609;4316;+proj=longlat +ellps=intl +no_defs;Dealul Piscului 1933
1610;4317;+proj=longlat +ellps=krass +no_defs;Dealul Piscului 1970
1611;4318;+proj=longlat +ellps=WGS84 +towgs84=-3.2,-5.7,2.8,0,0,0,0 +no_defs;NGN
1612;4319;+proj=longlat +ellps=GRS80 +no_defs;KUDAMS
1613;4322;+proj=longlat +ellps=WGS72 +no_defs;WGS 72
1614;4324;+proj=longlat +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +no_defs;WGS 72BE
1615;4326;+proj=longlat +ellps=WGS84 +datum=WGS84 +towgs84=0,0,0,0,0,0,0 +no_defs;WGS 84
1616;4600;+proj=longlat +ellps=clrk80 +no_defs;Anguilla 1957
1617;4601;+proj=longlat +ellps=clrk80 +no_defs;Antigua 1943
1618;4602;+proj=longlat +ellps=clrk80 +towgs84=725,685,536,0,0,0,0 +no_defs;Dominica 1945
1619;4603;+proj=longlat +ellps=clrk80 +towgs84=72,213.7,93,0,0,0,0 +no_defs;Grenada 1953
1620;4604;+proj=longlat +ellps=clrk80 +towgs84=174,359,365,0,0,0,0 +no_defs;Montserrat 1958
1621;4605;+proj=longlat +ellps=clrk80 +no_defs;St. Kitts 1955
1622;4606;+proj=longlat +ellps=clrk80 +towgs84=-149,128,296,0,0,0,0 +no_defs;St. Lucia 1955
1623;4607;+proj=longlat +ellps=clrk80 +towgs84=195.671,332.517,274.607,0,0,0,0 +no_defs;St. Vincent 1945
1624;4608;+proj=longlat +ellps=clrk66 +no_defs;NAD27(76)
1625;4609;+proj=longlat +ellps=clrk66 +no_defs;NAD27(CGQ77)
1626;4610;+proj=longlat +a=6378140 +b=6356755.288157528 +no_defs;Xian 1980
1627;4611;+proj=longlat +ellps=intl +towgs84=-162.619,-276.959,-161.764,0.067753,-2.24365,-1.15883,-1.09425 +no_defs;Hong Kong 1980
1628;4612;+proj=longlat +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +no_defs;JGD2000
1629;4613;+proj=longlat +ellps=bessel +no_defs;Segara
1630;4614;+proj=longlat +ellps=intl +towgs84=-119.425,-303.659,-11.0006,1.1643,0.174458,1.09626,3.65706 +no_defs;QND95
1631;4615;+proj=longlat +ellps=intl +towgs84=-499,-249,314,0,0,0,0 +no_defs;Porto Santo
1632;4616;+proj=longlat +ellps=intl +no_defs;Selvagem Grande
1633;4617;+proj=longlat +ellps=GRS80 +no_defs;NAD83(CSRS)
1634;4618;+proj=longlat +ellps=aust_SA +no_defs;SAD69
1635;4619;+proj=longlat +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +no_defs;SWEREF99
1636;4620;+proj=longlat +ellps=clrk80 +towgs84=-106,-129,165,0,0,0,0 +no_defs;Point 58
1637;4621;+proj=longlat +ellps=intl +towgs84=137,248,-430,0,0,0,0 +no_defs;Fort Marigot
1638;4622;+proj=longlat +ellps=intl +no_defs;Guadeloupe 1948
1639;4623;+proj=longlat +ellps=intl +towgs84=-186,230,110,0,0,0,0 +no_defs;CSG67
1640;4624;+proj=longlat +ellps=GRS80 +towgs84=2,2,-2,0,0,0,0 +no_defs;RGFG95
1641;4625;+proj=longlat +ellps=intl +no_defs;Martinique 1938
1642;4626;+proj=longlat +ellps=intl +no_defs;Reunion 1947
1643;4627;+proj=longlat +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +no_defs;RGR92
1644;4628;+proj=longlat +ellps=intl +towgs84=162,117,154,0,0,0,0 +no_defs;Tahiti 52
1645;4629;+proj=longlat +ellps=intl +no_defs;Tahaa 54
1646;4630;+proj=longlat +ellps=intl +no_defs;IGN72 Nuku Hiva
1647;4631;+proj=longlat +ellps=intl +towgs84=145,-187,103,0,0,0,0 +no_defs;K0 1949
1648;4632;+proj=longlat +ellps=intl +towgs84=-382,-59,-262,0,0,0,0 +no_defs;Combani 1950
1649;4633;+proj=longlat +ellps=intl +no_defs;IGN56 Lifou
1650;4634;+proj=longlat +ellps=intl +no_defs;IGN72 Grand Terre
1651;4635;+proj=longlat +ellps=intl +towgs84=-122.383,-188.696,103.344,3.5107,-4.9668,-5.7047,4.4798 +no_defs;ST87 Ouvea
1652;4636;+proj=longlat +ellps=intl +towgs84=365,194,166,0,0,0,0 +no_defs;Petrels 1972
1653;4637;+proj=longlat +ellps=intl +towgs84=325,154,172,0,0,0,0 +no_defs;Perroud 1950
1654;4638;+proj=longlat +ellps=clrk66 +towgs84=30,430,368,0,0,0,0 +no_defs;Saint Pierre et Miquelon 1950
1655;4639;+proj=longlat +ellps=intl +no_defs;MOP78
1656;4640;+proj=longlat +ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +no_defs;RRAF 1991
1657;4641;+proj=longlat +ellps=intl +no_defs;IGN53 Mare
1658;4642;+proj=longlat +ellps=intl +no_defs;ST84 Ile des Pins
1659;4643;+proj=longlat +ellps=intl +towgs84=-480.26,-438.32,-643.429,16.3119,20.1721,-4.0349,-111.7 +no_defs;ST71 Belep
1660;4644;+proj=longlat +ellps=intl +no_defs;NEA74 Noumea
1661;4645;+proj=longlat +ellps=intl +towgs84=0,0,0,0,0,0,0 +no_defs;RGNC 1991
1662;4646;+proj=longlat +ellps=intl +no_defs;Grand Comoros
1663;4657;+proj=longlat +a=6377019.27 +b=6355762.5391 +towgs84=-28,199,5,0,0,0,0 +no_defs;Reykjavik 1900
1664;4658;+proj=longlat +ellps=intl +towgs84=-73,46,-86,0,0,0,0 +no_defs;Hjorsey 1955
1665;4659;+proj=longlat +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +no_defs;ISN93
1666;4660;+proj=longlat +ellps=intl +towgs84=982.609,552.753,-540.873,32.3934,-153.257,-96.2266,16.805 +no_defs;Helle 1954
1667;4661;+proj=longlat +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +no_defs;LKS92
1668;4662;+proj=longlat +ellps=intl +no_defs;IGN72 Grande Terre
1669;4663;+proj=longlat +ellps=intl +no_defs;Porto Santo 1995
1670;4664;+proj=longlat +ellps=intl +no_defs;Azores Oriental 1995
1671;4665;+proj=longlat +ellps=intl +no_defs;Azores Central 1995
1672;4666;+proj=longlat +ellps=bessel +no_defs;Lisbon 1890
1673;4667;+proj=longlat +ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +no_defs;IKBD-92
1674;4668;+proj=longlat +ellps=intl +towgs84=-86,-98,-119,0,0,0,0 +no_defs;ED79
1675;4669;+proj=longlat +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +no_defs;LKS94
1676;4670;+proj=longlat +ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +no_defs;IGM95
1677;4671;+proj=longlat +a=6378249.2 +b=6356515 +no_defs;Voirol 1879
1678;4672;+proj=longlat +ellps=intl +towgs84=175,-38,113,0,0,0,0 +no_defs;CI1971
1679;4673;+proj=longlat +ellps=intl +towgs84=174.05,-25.49,112.57,-0,-0,0.554,0.2263 +no_defs;CI1979
1680;4674;+proj=longlat +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +no_defs;SIRGAS 2000
1681;4675;+proj=longlat +ellps=clrk66 +towgs84=-100,-248,259,0,0,0,0 +no_defs;Guam 1963
1682;4676;+proj=longlat +ellps=krass +no_defs;Vientiane 1982
1683;4677;+proj=longlat +ellps=krass +no_defs;Lao 1993
1684;4678;+proj=longlat +ellps=krass +towgs84=44.585,-131.212,-39.544,0,0,0,0 +no_defs;Lao 1997
1685;4679;+proj=longlat +ellps=clrk80 +towgs84=-80.01,253.26,291.19,0,0,0,0 +no_defs;Jouik 1961
1686;4680;+proj=longlat +ellps=clrk80 +towgs84=124.5,-63.5,-281,0,0,0,0 +no_defs;Nouakchott 1965
1687;4681;+proj=longlat +ellps=clrk80 +no_defs;Mauritania 1999
1688;4682;+proj=longlat +a=6377276.345 +b=6356075.413140239 +no_defs;Gulshan 303
1689;4683;+proj=longlat +ellps=clrk66 +towgs84=-127.62,-67.24,-47.04,-3.068,4.903,1.578,-1.06 +no_defs;PRS92
1690;4684;+proj=longlat +ellps=intl +towgs84=-133,-321,50,0,0,0,0 +no_defs;Gan 1970
1691;4685;+proj=longlat +ellps=intl +no_defs;Gandajika
1692;4686;+proj=longlat +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +no_defs;MAGNA-SIRGAS
1693;4687;+proj=longlat +ellps=GRS80 +no_defs;RGPF
1694;4688;+proj=longlat +ellps=intl +towgs84=347.103,1078.12,2623.92,-33.8875,70.6773,-9.3943,186.074 +no_defs;Fatu Iva 72
1695;4689;+proj=longlat +ellps=intl +no_defs;IGN63 Hiva Oa
1696;4690;+proj=longlat +ellps=intl +no_defs;Tahiti 79
1697;4691;+proj=longlat +ellps=intl +towgs84=215.525,149.593,176.229,-3.2624,-1.692,-1.1571,10.4773 +no_defs;Moorea 87
1698;4692;+proj=longlat +ellps=intl +towgs84=217.037,86.959,23.956,0,0,0,0 +no_defs;Maupiti 83
1699;4693;+proj=longlat +ellps=WGS84 +towgs84=0,-0.15,0.68,0,0,0,0 +no_defs;Nakhl-e Ghanem
1700;4694;+proj=longlat +ellps=GRS80 +no_defs;POSGAR 94
1701;4695;+proj=longlat +ellps=clrk66 +no_defs;Katanga 1955
1702;4696;+proj=longlat +ellps=clrk80 +no_defs;Kasai 1953
1703;4697;+proj=longlat +ellps=clrk80 +no_defs;IGC 1962 6th Parallel South
1704;4698;+proj=longlat +ellps=intl +towgs84=145,-187,103,0,0,0,0 +no_defs;IGN 1962 Kerguelen
1705;4699;+proj=longlat +ellps=clrk80 +towgs84=-770.1,158.4,-498.2,0,0,0,0 +no_defs;Le Pouce 1934
1706;4700;+proj=longlat +ellps=clrk80 +no_defs;IGN Astro 1960
1707;4701;+proj=longlat +ellps=clrk80 +towgs84=-79.9,-158,-168.9,0,0,0,0 +no_defs;IGCB 1955
1708;4702;+proj=longlat +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +no_defs;Mauritania 1999
1709;4703;+proj=longlat +ellps=clrk80 +no_defs;Mhast 1951
1710;4704;+proj=longlat +ellps=intl +no_defs;Mhast (onshore)
1711;4705;+proj=longlat +ellps=intl +no_defs;Mhast (offshore)
1712;4706;+proj=longlat +ellps=helmert +towgs84=-146.21,112.63,4.05,0,0,0,0 +no_defs;Egypt Gulf of Suez S-650 TL
1713;4707;+proj=longlat +ellps=intl +towgs84=114,-116,-333,0,0,0,0 +no_defs;Tern Island 1961
1714;4708;+proj=longlat +ellps=aust_SA +towgs84=-491,-22,435,0,0,0,0 +no_defs;Cocos Islands 1965
1715;4709;+proj=longlat +ellps=intl +towgs84=145,75,-272,0,0,0,0 +no_defs;Iwo Jima 1945
1716;4710;+proj=longlat +ellps=intl +towgs84=-320,550,-494,0,0,0,0 +no_defs;St. Helena 1971
1717;4711;+proj=longlat +ellps=intl +towgs84=124,-234,-25,0,0,0,0 +no_defs;Marcus Island 1952
1718;4712;+proj=longlat +ellps=intl +towgs84=-205,107,53,0,0,0,0 +no_defs;Ascension Island 1958
1719;4713;+proj=longlat +ellps=clrk80 +towgs84=-79,-129,145,0,0,0,0 +no_defs;Ayabelle Lighthouse
1720;4714;+proj=longlat +ellps=intl +towgs84=-127,-769,472,0,0,0,0 +no_defs;Bellevue
1721;4715;+proj=longlat +ellps=intl +towgs84=-104,-129,239,0,0,0,0 +no_defs;Camp Area Astro
1722;4716;+proj=longlat +ellps=intl +towgs84=298,-304,-375,0,0,0,0 +no_defs;Phoenix Islands 1966
1723;4717;+proj=longlat +ellps=clrk66 +towgs84=-2,151,181,0,0,0,0 +no_defs;Cape Canaveral
1724;4718;+proj=longlat +ellps=intl +no_defs;Solomon 1968
1725;4719;+proj=longlat +ellps=intl +towgs84=211,147,111,0,0,0,0 +no_defs;Easter Island 1967
1726;4720;+proj=longlat +ellps=WGS72 +no_defs;Fiji 1986
1727;4721;+proj=longlat +ellps=intl +towgs84=265.025,384.929,-194.046,0,0,0,0 +no_defs;Fiji 1956
1728;4722;+proj=longlat +ellps=intl +towgs84=-794,119,-298,0,0,0,0 +no_defs;South Georgia 1968
1729;4723;+proj=longlat +ellps=clrk66 +towgs84=67.8,106.1,138.8,0,0,0,0 +no_defs;Grand Cayman 1959
1730;4724;+proj=longlat +ellps=intl +towgs84=208,-435,-229,0,0,0,0 +no_defs;Diego Garcia 1969
1731;4725;+proj=longlat +ellps=intl +towgs84=189,-79,-202,0,0,0,0 +no_defs;Johnston Island 1961
1732;4726;+proj=longlat +ellps=clrk66 +no_defs;Little Cayman 1961
1733;4727;+proj=longlat +ellps=intl +no_defs;Midway 1961
1734;4728;+proj=longlat +ellps=intl +towgs84=-307,-92,127,0,0,0,0 +no_defs;Pico de la Nieves
1735;4729;+proj=longlat +ellps=intl +towgs84=185,165,42,0,0,0,0 +no_defs;Pitcairn 1967
1736;4730;+proj=longlat +ellps=intl +towgs84=170,42,84,0,0,0,0 +no_defs;Santo 1965
1737;4731;+proj=longlat +ellps=clrk80 +towgs84=51,391,-36,0,0,0,0 +no_defs;Viti Levu 1916
1738;4732;+proj=longlat +a=6378270 +b=6356794.343434343 +towgs84=102,52,-38,0,0,0,0 +no_defs;Marshall Islands 1960
1739;4733;+proj=longlat +ellps=intl +towgs84=276,-57,149,0,0,0,0 +no_defs;Wake Island 1952
1740;4734;+proj=longlat +ellps=intl +towgs84=-632,438,-609,0,0,0,0 +no_defs;Tristan 1968
1741;4735;+proj=longlat +ellps=intl +towgs84=647,1777,-1124,0,0,0,0 +no_defs;Kusaie 1951
1742;4736;+proj=longlat +ellps=clrk80 +towgs84=260,12,-147,0,0,0,0 +no_defs;Deception Island
1743;4737;+proj=longlat +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +no_defs;Korea 2000
1744;4738;+proj=longlat +a=6378293.645208759 +b=6356617.987679838 +no_defs;Hong Kong 1963
1745;4739;+proj=longlat +ellps=intl +towgs84=-156,-271,-189,0,0,0,0 +no_defs;Hong Kong 1963(67)
1746;4740;+proj=longlat +a=6378136 +b=6356751.361745712 +towgs84=0,0,1.5,-0,-0,0.076,0 +no_defs;PZ-90
1747;4741;+proj=longlat +ellps=intl +no_defs;FD54
1748;4742;+proj=longlat +ellps=GRS80 +no_defs;GDM2000
1749;4743;+proj=longlat +ellps=clrk80 +towgs84=84.1,-320.1,218.7,0,0,0,0 +no_defs;Karbala 1979 (Polservice)
1750;4744;+proj=longlat +ellps=clrk80 +no_defs;Nahrwan 1934
1751;4745;+proj=longlat +ellps=bessel +no_defs;RD/83
1752;4746;+proj=longlat +ellps=bessel +no_defs;PD/83
1753;4747;+proj=longlat +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +no_defs;GR96
1754;4748;+proj=longlat +a=6378306.3696 +b=6356571.996 +towgs84=51,391,-36,0,0,0,0 +no_defs;Vanua Levu 1915
1755;4749;+proj=longlat +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +no_defs;RGNC91-93
1756;4750;+proj=longlat +ellps=WGS84 +towgs84=-56.263,16.136,-22.856,0,0,0,0 +no_defs;ST87 Ouvea
1757;4751;+proj=longlat +a=6377295.664 +b=6356094.667915204 +no_defs;Kertau (RSO)
1758;4752;+proj=longlat +a=6378306.3696 +b=6356571.996 +towgs84=51,391,-36,0,0,0,0 +no_defs;Viti Levu 1912
1759;4753;+proj=longlat +ellps=intl +no_defs;fk89
1760;4754;+proj=longlat +ellps=intl +towgs84=-208.406,-109.878,-2.5764,0,0,0,0 +no_defs;LGD2006
1761;4755;+proj=longlat +ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +no_defs;DGN95
1762;4756;+proj=longlat +ellps=WGS84 +no_defs;VN-2000
1763;4801;+proj=longlat +ellps=bessel +pm=bern +no_defs;Bern 1898 (Bern)
1764;4802;+proj=longlat +ellps=intl +pm=bogota +no_defs;Bogota 1975 (Bogota)
1765;4803;+proj=longlat +ellps=intl +pm=lisbon +no_defs;Lisbon (Lisbon)
1766;4804;+proj=longlat +ellps=bessel +towgs84=-587.8,519.75,145.76,0,0,0,0 +pm=jakarta +no_defs;Makassar (Jakarta)
1767;4805;+proj=longlat +ellps=bessel +pm=ferro +no_defs;MGI (Ferro)
1768;4806;+proj=longlat +ellps=intl +pm=rome +no_defs;Monte Mario (Rome)
1769;4807;+proj=longlat +a=6378249.2 +b=6356515 +towgs84=-168,-60,320,0,0,0,0 +pm=paris +no_defs;NTF (Paris)
1770;4808;+proj=longlat +ellps=bessel +pm=jakarta +no_defs;Padang (Jakarta)
1771;4809;+proj=longlat +ellps=intl +pm=brussels +no_defs;Belge 1950 (Brussels)
1772;4810;+proj=longlat +ellps=intl +towgs84=-189,-242,-91,0,0,0,0 +pm=paris +no_defs;Tananarive (Paris)
1773;4811;+proj=longlat +a=6378249.2 +b=6356515 +towgs84=-73,-247,227,0,0,0,0 +pm=paris +no_defs;Voirol 1875 (Paris)
1774;4813;+proj=longlat +ellps=bessel +pm=jakarta +no_defs;Batavia (Jakarta)
1775;4814;+proj=longlat +ellps=bessel +pm=stockholm +no_defs;RT38 (Stockholm)
1776;4815;+proj=longlat +ellps=bessel +pm=athens +no_defs;Greek (Athens)
1777;4816;+proj=longlat +a=6378249.2 +b=6356515 +pm=paris +no_defs;Carthage (Paris)
1778;4817;+proj=longlat +a=6377492.018 +b=6356173.508712696 +towgs84=278.3,93,474.5,7.889,0.05,-6.61,6.21 +pm=oslo +no_defs;NGO 1948 (Oslo)
1779;4818;+proj=longlat +ellps=bessel +pm=ferro +no_defs;S-JTSK (Ferro)
1780;4819;+proj=longlat +ellps=clrk80 +pm=paris +no_defs;Nord Sahara 1959 (Paris)
1781;4820;+proj=longlat +ellps=bessel +pm=jakarta +no_defs;Segara (Jakarta)
1782;4821;+proj=longlat +a=6378249.2 +b=6356515 +pm=paris +no_defs;Voirol 1879 (Paris)
1783;4901;+proj=longlat +a=6376523 +b=6355862.933255573 +pm=paris +no_defs;ATF (Paris)
1784;4902;+proj=longlat +a=6376523 +b=6355862.933255573 +pm=paris +no_defs;NDG (Paris)
1785;4903;+proj=longlat +a=6378298.3 +b=6356657.142669562 +pm=madrid +no_defs;Madrid 1870 (Madrid)
1786;4904;+proj=longlat +ellps=bessel +pm=lisbon +no_defs;Lisbon 1890 (Lisbon)
1787;20004;+proj=tmerc +lat_0=0 +lon_0=21 +k=1.000000 +x_0=4500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger zone 4
1788;20005;+proj=tmerc +lat_0=0 +lon_0=27 +k=1.000000 +x_0=5500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger zone 5
1789;20006;+proj=tmerc +lat_0=0 +lon_0=33 +k=1.000000 +x_0=6500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger zone 6
1790;20007;+proj=tmerc +lat_0=0 +lon_0=39 +k=1.000000 +x_0=7500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger zone 7
1791;20008;+proj=tmerc +lat_0=0 +lon_0=45 +k=1.000000 +x_0=8500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger zone 8
1792;20009;+proj=tmerc +lat_0=0 +lon_0=51 +k=1.000000 +x_0=9500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger zone 9
1793;20010;+proj=tmerc +lat_0=0 +lon_0=57 +k=1.000000 +x_0=10500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger zone 10
1794;20011;+proj=tmerc +lat_0=0 +lon_0=63 +k=1.000000 +x_0=11500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger zone 11
1795;20012;+proj=tmerc +lat_0=0 +lon_0=69 +k=1.000000 +x_0=12500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger zone 12
1796;20013;+proj=tmerc +lat_0=0 +lon_0=75 +k=1.000000 +x_0=13500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger zone 13
1797;20014;+proj=tmerc +lat_0=0 +lon_0=81 +k=1.000000 +x_0=14500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger zone 14
1798;20015;+proj=tmerc +lat_0=0 +lon_0=87 +k=1.000000 +x_0=15500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger zone 15
1799;20016;+proj=tmerc +lat_0=0 +lon_0=93 +k=1.000000 +x_0=16500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger zone 16
1800;20017;+proj=tmerc +lat_0=0 +lon_0=99 +k=1.000000 +x_0=17500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger zone 17
1801;20018;+proj=tmerc +lat_0=0 +lon_0=105 +k=1.000000 +x_0=18500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger zone 18
1802;20019;+proj=tmerc +lat_0=0 +lon_0=111 +k=1.000000 +x_0=19500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger zone 19
1803;20020;+proj=tmerc +lat_0=0 +lon_0=117 +k=1.000000 +x_0=20500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger zone 20
1804;20021;+proj=tmerc +lat_0=0 +lon_0=123 +k=1.000000 +x_0=21500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger zone 21
1805;20022;+proj=tmerc +lat_0=0 +lon_0=129 +k=1.000000 +x_0=22500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger zone 22
1806;20023;+proj=tmerc +lat_0=0 +lon_0=135 +k=1.000000 +x_0=23500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger zone 23
1807;20024;+proj=tmerc +lat_0=0 +lon_0=141 +k=1.000000 +x_0=24500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger zone 24
1808;20025;+proj=tmerc +lat_0=0 +lon_0=147 +k=1.000000 +x_0=25500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger zone 25
1809;20026;+proj=tmerc +lat_0=0 +lon_0=153 +k=1.000000 +x_0=26500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger zone 26
1810;20027;+proj=tmerc +lat_0=0 +lon_0=159 +k=1.000000 +x_0=27500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger zone 27
1811;20028;+proj=tmerc +lat_0=0 +lon_0=165 +k=1.000000 +x_0=28500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger zone 28
1812;20029;+proj=tmerc +lat_0=0 +lon_0=171 +k=1.000000 +x_0=29500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger zone 29
1813;20030;+proj=tmerc +lat_0=0 +lon_0=177 +k=1.000000 +x_0=30500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger zone 30
1814;20031;+proj=tmerc +lat_0=0 +lon_0=-177 +k=1.000000 +x_0=31500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger zone 31
1815;20032;+proj=tmerc +lat_0=0 +lon_0=-171 +k=1.000000 +x_0=32500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger zone 32
1816;20064;+proj=tmerc +lat_0=0 +lon_0=21 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger 4N (deprecated)
1817;20065;+proj=tmerc +lat_0=0 +lon_0=27 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger 5N (deprecated)
1818;20066;+proj=tmerc +lat_0=0 +lon_0=33 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger 6N (deprecated)
1819;20067;+proj=tmerc +lat_0=0 +lon_0=39 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger 7N (deprecated)
1820;20068;+proj=tmerc +lat_0=0 +lon_0=45 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger 8N (deprecated)
1821;20069;+proj=tmerc +lat_0=0 +lon_0=51 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger 9N (deprecated)
1822;20070;+proj=tmerc +lat_0=0 +lon_0=57 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger 10N (deprecated)
1823;20071;+proj=tmerc +lat_0=0 +lon_0=63 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger 11N (deprecated)
1824;20072;+proj=tmerc +lat_0=0 +lon_0=69 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger 12N (deprecated)
1825;20073;+proj=tmerc +lat_0=0 +lon_0=75 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger 13N (deprecated)
1826;20074;+proj=tmerc +lat_0=0 +lon_0=81 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger 14N (deprecated)
1827;20075;+proj=tmerc +lat_0=0 +lon_0=87 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger 15N (deprecated)
1828;20076;+proj=tmerc +lat_0=0 +lon_0=93 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger 16N (deprecated)
1829;20077;+proj=tmerc +lat_0=0 +lon_0=99 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger 17N (deprecated)
1830;20078;+proj=tmerc +lat_0=0 +lon_0=105 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger 18N (deprecated)
1831;20079;+proj=tmerc +lat_0=0 +lon_0=111 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger 19N (deprecated)
1832;20080;+proj=tmerc +lat_0=0 +lon_0=117 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger 20N (deprecated)
1833;20081;+proj=tmerc +lat_0=0 +lon_0=123 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger 21N (deprecated)
1834;20082;+proj=tmerc +lat_0=0 +lon_0=129 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger 22N (deprecated)
1835;20083;+proj=tmerc +lat_0=0 +lon_0=135 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger 23N (deprecated)
1836;20084;+proj=tmerc +lat_0=0 +lon_0=141 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger 24N (deprecated)
1837;20085;+proj=tmerc +lat_0=0 +lon_0=147 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger 25N (deprecated)
1838;20086;+proj=tmerc +lat_0=0 +lon_0=153 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger 26N (deprecated)
1839;20087;+proj=tmerc +lat_0=0 +lon_0=159 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger 27N (deprecated)
1840;20088;+proj=tmerc +lat_0=0 +lon_0=165 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger 28N (deprecated)
1841;20089;+proj=tmerc +lat_0=0 +lon_0=171 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger 29N (deprecated)
1842;20090;+proj=tmerc +lat_0=0 +lon_0=177 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger 30N (deprecated)
1843;20091;+proj=tmerc +lat_0=0 +lon_0=-177 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger 31N (deprecated)
1844;20092;+proj=tmerc +lat_0=0 +lon_0=-171 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1995 / Gauss-Kruger 32N (deprecated)
1845;20135;+proj=utm +zone=35 +ellps=clrk80 +units=m +no_defs;Adindan / UTM zone 35N
1846;20136;+proj=utm +zone=36 +ellps=clrk80 +units=m +no_defs;Adindan / UTM zone 36N
1847;20137;+proj=utm +zone=37 +ellps=clrk80 +units=m +no_defs;Adindan / UTM zone 37N
1848;20138;+proj=utm +zone=38 +ellps=clrk80 +units=m +no_defs;Adindan / UTM zone 38N
1849;20248;+proj=utm +zone=48 +south +ellps=aust_SA +units=m +no_defs;AGD66 / AMG zone 48
1850;20249;+proj=utm +zone=49 +south +ellps=aust_SA +units=m +no_defs;AGD66 / AMG zone 49
1851;20250;+proj=utm +zone=50 +south +ellps=aust_SA +units=m +no_defs;AGD66 / AMG zone 50
1852;20251;+proj=utm +zone=51 +south +ellps=aust_SA +units=m +no_defs;AGD66 / AMG zone 51
1853;20252;+proj=utm +zone=52 +south +ellps=aust_SA +units=m +no_defs;AGD66 / AMG zone 52
1854;20253;+proj=utm +zone=53 +south +ellps=aust_SA +units=m +no_defs;AGD66 / AMG zone 53
1855;20254;+proj=utm +zone=54 +south +ellps=aust_SA +units=m +no_defs;AGD66 / AMG zone 54
1856;20255;+proj=utm +zone=55 +south +ellps=aust_SA +units=m +no_defs;AGD66 / AMG zone 55
1857;20256;+proj=utm +zone=56 +south +ellps=aust_SA +units=m +no_defs;AGD66 / AMG zone 56
1858;20257;+proj=utm +zone=57 +south +ellps=aust_SA +units=m +no_defs;AGD66 / AMG zone 57
1859;20258;+proj=utm +zone=58 +south +ellps=aust_SA +units=m +no_defs;AGD66 / AMG zone 58
1860;20348;+proj=utm +zone=48 +south +ellps=aust_SA +units=m +no_defs;AGD84 / AMG zone 48
1861;20349;+proj=utm +zone=49 +south +ellps=aust_SA +units=m +no_defs;AGD84 / AMG zone 49
1862;20350;+proj=utm +zone=50 +south +ellps=aust_SA +units=m +no_defs;AGD84 / AMG zone 50
1863;20351;+proj=utm +zone=51 +south +ellps=aust_SA +units=m +no_defs;AGD84 / AMG zone 51
1864;20352;+proj=utm +zone=52 +south +ellps=aust_SA +units=m +no_defs;AGD84 / AMG zone 52
1865;20353;+proj=utm +zone=53 +south +ellps=aust_SA +units=m +no_defs;AGD84 / AMG zone 53
1866;20354;+proj=utm +zone=54 +south +ellps=aust_SA +units=m +no_defs;AGD84 / AMG zone 54
1867;20355;+proj=utm +zone=55 +south +ellps=aust_SA +units=m +no_defs;AGD84 / AMG zone 55
1868;20356;+proj=utm +zone=56 +south +ellps=aust_SA +units=m +no_defs;AGD84 / AMG zone 56
1869;20357;+proj=utm +zone=57 +south +ellps=aust_SA +units=m +no_defs;AGD84 / AMG zone 57
1870;20358;+proj=utm +zone=58 +south +ellps=aust_SA +units=m +no_defs;AGD84 / AMG zone 58
1871;20436;+proj=utm +zone=36 +ellps=intl +units=m +no_defs;Ain el Abd / UTM zone 36N
1872;20437;+proj=utm +zone=37 +ellps=intl +units=m +no_defs;Ain el Abd / UTM zone 37N
1873;20438;+proj=utm +zone=38 +ellps=intl +units=m +no_defs;Ain el Abd / UTM zone 38N
1874;20439;+proj=utm +zone=39 +ellps=intl +units=m +no_defs;Ain el Abd / UTM zone 39N
1875;20440;+proj=utm +zone=40 +ellps=intl +units=m +no_defs;Ain el Abd / UTM zone 40N
1876;20499;+proj=utm +zone=39 +ellps=intl +units=m +no_defs;Ain el Abd / Bahrain Grid
1877;20538;+proj=utm +zone=38 +ellps=krass +towgs84=-43,-163,45,0,0,0,0 +units=m +no_defs;Afgooye / UTM zone 38N
1878;20539;+proj=utm +zone=39 +ellps=krass +towgs84=-43,-163,45,0,0,0,0 +units=m +no_defs;Afgooye / UTM zone 39N
1879;20790;+proj=tmerc +lat_0=39.66666666666666 +lon_0=1 +k=1.000000 +x_0=200000 +y_0=300000 +ellps=intl +pm=lisbon +units=m +no_defs;Lisbon (Lisbon)/Portuguese National Grid
1880;20791;+proj=tmerc +lat_0=39.66666666666666 +lon_0=1 +k=1.000000 +x_0=0 +y_0=0 +ellps=intl +pm=lisbon +units=m +no_defs;Lisbon (Lisbon)/Portuguese Grid
1881;20822;+proj=utm +zone=22 +south +ellps=intl +units=m +no_defs;Aratu / UTM zone 22S
1882;20823;+proj=utm +zone=23 +south +ellps=intl +units=m +no_defs;Aratu / UTM zone 23S
1883;20824;+proj=utm +zone=24 +south +ellps=intl +units=m +no_defs;Aratu / UTM zone 24S
1884;20934;+proj=utm +zone=34 +south +a=6378249.145 +b=6356514.966398753 +units=m +no_defs;Arc 1950 / UTM zone 34S
1885;20935;+proj=utm +zone=35 +south +a=6378249.145 +b=6356514.966398753 +units=m +no_defs;Arc 1950 / UTM zone 35S
1886;20936;+proj=utm +zone=36 +south +a=6378249.145 +b=6356514.966398753 +units=m +no_defs;Arc 1950 / UTM zone 36S
1887;21035;+proj=utm +zone=35 +south +ellps=clrk80 +units=m +no_defs;Arc 1960 / UTM zone 35S
1888;21036;+proj=utm +zone=36 +south +ellps=clrk80 +units=m +no_defs;Arc 1960 / UTM zone 36S
1889;21037;+proj=utm +zone=37 +south +ellps=clrk80 +units=m +no_defs;Arc 1960 / UTM zone 37S
1890;21095;+proj=utm +zone=35 +ellps=clrk80 +units=m +no_defs;Arc 1960 / UTM zone 35N
1891;21096;+proj=utm +zone=36 +ellps=clrk80 +units=m +no_defs;Arc 1960 / UTM zone 36N
1892;21097;+proj=utm +zone=37 +ellps=clrk80 +units=m +no_defs;Arc 1960 / UTM zone 37N
1893;21100;+proj=merc +lon_0=110 +k=0.997000 +x_0=3900000 +y_0=900000 +ellps=bessel +pm=jakarta +units=m +no_defs;Batavia (Jakarta) / NEIEZ (deprecated)
1894;21148;+proj=utm +zone=48 +south +ellps=bessel +units=m +no_defs;Batavia / UTM zone 48S
1895;21149;+proj=utm +zone=49 +south +ellps=bessel +units=m +no_defs;Batavia / UTM zone 49S
1896;21150;+proj=utm +zone=50 +south +ellps=bessel +units=m +no_defs;Batavia / UTM zone 50S
1897;21291;+proj=tmerc +lat_0=0 +lon_0=-62 +k=0.999500 +x_0=400000 +y_0=0 +ellps=clrk80 +towgs84=31.95,300.99,419.19,0,0,0,0 +units=m +no_defs;Barbados 1938 / British West Indies Grid
1898;21292;+proj=tmerc +lat_0=13.17638888888889 +lon_0=-59.55972222222222 +k=0.999999 +x_0=30000 +y_0=75000 +ellps=clrk80 +towgs84=31.95,300.99,419.19,0,0,0,0 +units=m +no_defs;Barbados 1938 / Barbados National Grid
1899;21413;+proj=tmerc +lat_0=0 +lon_0=75 +k=1.000000 +x_0=13500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger zone 13
1900;21414;+proj=tmerc +lat_0=0 +lon_0=81 +k=1.000000 +x_0=14500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger zone 14
1901;21415;+proj=tmerc +lat_0=0 +lon_0=87 +k=1.000000 +x_0=15500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger zone 15
1902;21416;+proj=tmerc +lat_0=0 +lon_0=93 +k=1.000000 +x_0=16500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger zone 16
1903;21417;+proj=tmerc +lat_0=0 +lon_0=99 +k=1.000000 +x_0=17500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger zone 17
1904;21418;+proj=tmerc +lat_0=0 +lon_0=105 +k=1.000000 +x_0=18500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger zone 18
1905;21419;+proj=tmerc +lat_0=0 +lon_0=111 +k=1.000000 +x_0=19500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger zone 19
1906;21420;+proj=tmerc +lat_0=0 +lon_0=117 +k=1.000000 +x_0=20500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger zone 20
1907;21421;+proj=tmerc +lat_0=0 +lon_0=123 +k=1.000000 +x_0=21500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger zone 21
1908;21422;+proj=tmerc +lat_0=0 +lon_0=129 +k=1.000000 +x_0=22500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger zone 22
1909;21423;+proj=tmerc +lat_0=0 +lon_0=135 +k=1.000000 +x_0=23500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger zone 23
1910;21453;+proj=tmerc +lat_0=0 +lon_0=75 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger CM 75E
1911;21454;+proj=tmerc +lat_0=0 +lon_0=81 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger CM 81E
1912;21455;+proj=tmerc +lat_0=0 +lon_0=87 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger CM 87E
1913;21456;+proj=tmerc +lat_0=0 +lon_0=93 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger CM 93E
1914;21457;+proj=tmerc +lat_0=0 +lon_0=99 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger CM 99E
1915;21458;+proj=tmerc +lat_0=0 +lon_0=105 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger CM 105E
1916;21459;+proj=tmerc +lat_0=0 +lon_0=111 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger CM 111E
1917;21460;+proj=tmerc +lat_0=0 +lon_0=117 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger CM 117E
1918;21461;+proj=tmerc +lat_0=0 +lon_0=123 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger CM 123E
1919;21462;+proj=tmerc +lat_0=0 +lon_0=129 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger CM 129E
1920;21463;+proj=tmerc +lat_0=0 +lon_0=135 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger CM 135E
1921;21473;+proj=tmerc +lat_0=0 +lon_0=75 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger 13N (deprecated)
1922;21474;+proj=tmerc +lat_0=0 +lon_0=81 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger 14N (deprecated)
1923;21475;+proj=tmerc +lat_0=0 +lon_0=87 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger 15N (deprecated)
1924;21476;+proj=tmerc +lat_0=0 +lon_0=93 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger 16N (deprecated)
1925;21477;+proj=tmerc +lat_0=0 +lon_0=99 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger 17N (deprecated)
1926;21478;+proj=tmerc +lat_0=0 +lon_0=105 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger 18N (deprecated)
1927;21479;+proj=tmerc +lat_0=0 +lon_0=111 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger 19N (deprecated)
1928;21480;+proj=tmerc +lat_0=0 +lon_0=117 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger 20N (deprecated)
1929;21481;+proj=tmerc +lat_0=0 +lon_0=123 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger 21N (deprecated)
1930;21482;+proj=tmerc +lat_0=0 +lon_0=129 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger 22N (deprecated)
1931;21483;+proj=tmerc +lat_0=0 +lon_0=135 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Beijing 1954 / Gauss-Kruger 23N (deprecated)
1932;21500;+proj=lcc +lat_1=49.83333333333334 +lat_2=51.16666666666666 +lat_0=90 +lon_0=0 +x_0=150000 +y_0=5400000 +ellps=intl +pm=brussels +units=m +no_defs;Belge 1950 (Brussels) / Belge Lambert 50
1933;21780;+proj=somerc +lat_0=46.95240555555556 +lon_0=0 +x_0=0 +y_0=0 +ellps=bessel +pm=bern +units=m +no_defs;Bern 1898 (Bern) / LV03C
1934;21781;+proj=somerc +lat_0=46.95240555555556 +lon_0=7.439583333333333 +x_0=600000 +y_0=200000 +ellps=bessel +towgs84=674.374,15.056,405.346,0,0,0,0 +units=m +no_defs;CH1903 / LV03
1935;21817;+proj=utm +zone=17 +ellps=intl +towgs84=307,304,-318,0,0,0,0 +units=m +no_defs;Bogota 1975 / UTM zone 17N (deprecated)
1936;21818;+proj=utm +zone=18 +ellps=intl +towgs84=307,304,-318,0,0,0,0 +units=m +no_defs;Bogota 1975 / UTM zone 18N
1937;21891;+proj=tmerc +lat_0=4.599047222222222 +lon_0=-77.08091666666667 +k=1.000000 +x_0=1000000 +y_0=1000000 +ellps=intl +towgs84=307,304,-318,0,0,0,0 +units=m +no_defs;Bogota 1975 / Colombia West zone (deprecated)
1938;21892;+proj=tmerc +lat_0=4.599047222222222 +lon_0=-74.08091666666667 +k=1.000000 +x_0=1000000 +y_0=1000000 +ellps=intl +towgs84=307,304,-318,0,0,0,0 +units=m +no_defs;Bogota 1975 / Colombia Bogota zone (deprecated)
1939;21893;+proj=tmerc +lat_0=4.599047222222222 +lon_0=-71.08091666666667 +k=1.000000 +x_0=1000000 +y_0=1000000 +ellps=intl +towgs84=307,304,-318,0,0,0,0 +units=m +no_defs;Bogota 1975 / Colombia East Central zone (deprecated)
1940;21894;+proj=tmerc +lat_0=4.599047222222222 +lon_0=-68.08091666666667 +k=1.000000 +x_0=1000000 +y_0=1000000 +ellps=intl +towgs84=307,304,-318,0,0,0,0 +units=m +no_defs;Bogota 1975 / Colombia East (deprecated)
1941;21896;+proj=tmerc +lat_0=4.599047222222222 +lon_0=-77.08091666666667 +k=1.000000 +x_0=1000000 +y_0=1000000 +ellps=intl +towgs84=307,304,-318,0,0,0,0 +units=m +no_defs;Bogota 1975 / Colombia West zone
1942;21897;+proj=tmerc +lat_0=4.599047222222222 +lon_0=-74.08091666666667 +k=1.000000 +x_0=1000000 +y_0=1000000 +ellps=intl +towgs84=307,304,-318,0,0,0,0 +units=m +no_defs;Bogota 1975 / Colombia Bogota zone
1943;21898;+proj=tmerc +lat_0=4.599047222222222 +lon_0=-71.08091666666667 +k=1.000000 +x_0=1000000 +y_0=1000000 +ellps=intl +towgs84=307,304,-318,0,0,0,0 +units=m +no_defs;Bogota 1975 / Colombia East Central zone
1944;21899;+proj=tmerc +lat_0=4.599047222222222 +lon_0=-68.08091666666667 +k=1.000000 +x_0=1000000 +y_0=1000000 +ellps=intl +towgs84=307,304,-318,0,0,0,0 +units=m +no_defs;Bogota 1975 / Colombia East
1945;22032;+proj=utm +zone=32 +south +ellps=clrk80 +units=m +no_defs;Camacupa / UTM zone 32S
1946;22033;+proj=utm +zone=33 +south +ellps=clrk80 +units=m +no_defs;Camacupa / UTM zone 33S
1947;22091;+proj=tmerc +lat_0=0 +lon_0=11.5 +k=0.999600 +x_0=500000 +y_0=10000000 +ellps=clrk80 +units=m +no_defs;Camacupa / TM 11.30 SE
1948;22092;+proj=tmerc +lat_0=0 +lon_0=12 +k=0.999600 +x_0=500000 +y_0=10000000 +ellps=clrk80 +units=m +no_defs;Camacupa / TM 12 SE
1949;22171;+proj=tmerc +lat_0=-90 +lon_0=-72 +k=1.000000 +x_0=1500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;POSGAR 98 / Argentina 1
1950;22172;+proj=tmerc +lat_0=-90 +lon_0=-69 +k=1.000000 +x_0=2500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;POSGAR 98 / Argentina 2
1951;22173;+proj=tmerc +lat_0=-90 +lon_0=-66 +k=1.000000 +x_0=3500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;POSGAR 98 / Argentina 3
1952;22174;+proj=tmerc +lat_0=-90 +lon_0=-63 +k=1.000000 +x_0=4500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;POSGAR 98 / Argentina 4
1953;22175;+proj=tmerc +lat_0=-90 +lon_0=-60 +k=1.000000 +x_0=5500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;POSGAR 98 / Argentina 5
1954;22176;+proj=tmerc +lat_0=-90 +lon_0=-57 +k=1.000000 +x_0=6500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;POSGAR 98 / Argentina 6
1955;22177;+proj=tmerc +lat_0=-90 +lon_0=-54 +k=1.000000 +x_0=7500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;POSGAR 98 / Argentina 7
1956;22181;+proj=tmerc +lat_0=-90 +lon_0=-72 +k=1.000000 +x_0=1500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;POSGAR 94 / Argentina 1
1957;22182;+proj=tmerc +lat_0=-90 +lon_0=-69 +k=1.000000 +x_0=2500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;POSGAR 94 / Argentina 2
1958;22183;+proj=tmerc +lat_0=-90 +lon_0=-66 +k=1.000000 +x_0=3500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;POSGAR 94 / Argentina 3
1959;22184;+proj=tmerc +lat_0=-90 +lon_0=-63 +k=1.000000 +x_0=4500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;POSGAR 94 / Argentina 4
1960;22185;+proj=tmerc +lat_0=-90 +lon_0=-60 +k=1.000000 +x_0=5500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;POSGAR 94 / Argentina 5
1961;22186;+proj=tmerc +lat_0=-90 +lon_0=-57 +k=1.000000 +x_0=6500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;POSGAR 94 / Argentina 6
1962;22187;+proj=tmerc +lat_0=-90 +lon_0=-54 +k=1.000000 +x_0=7500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;POSGAR 94 / Argentina 7
1963;22191;+proj=tmerc +lat_0=-90 +lon_0=-72 +k=1.000000 +x_0=1500000 +y_0=0 +ellps=intl +units=m +no_defs;Campo Inchauspe / Argentina 1
1964;22192;+proj=tmerc +lat_0=-90 +lon_0=-69 +k=1.000000 +x_0=2500000 +y_0=0 +ellps=intl +units=m +no_defs;Campo Inchauspe / Argentina 2
1965;22193;+proj=tmerc +lat_0=-90 +lon_0=-66 +k=1.000000 +x_0=3500000 +y_0=0 +ellps=intl +units=m +no_defs;Campo Inchauspe / Argentina 3
1966;22194;+proj=tmerc +lat_0=-90 +lon_0=-63 +k=1.000000 +x_0=4500000 +y_0=0 +ellps=intl +units=m +no_defs;Campo Inchauspe / Argentina 4
1967;22195;+proj=tmerc +lat_0=-90 +lon_0=-60 +k=1.000000 +x_0=5500000 +y_0=0 +ellps=intl +units=m +no_defs;Campo Inchauspe / Argentina 5
1968;22196;+proj=tmerc +lat_0=-90 +lon_0=-57 +k=1.000000 +x_0=6500000 +y_0=0 +ellps=intl +units=m +no_defs;Campo Inchauspe / Argentina 6
1969;22197;+proj=tmerc +lat_0=-90 +lon_0=-54 +k=1.000000 +x_0=7500000 +y_0=0 +ellps=intl +units=m +no_defs;Campo Inchauspe / Argentina 7
1970;22234;+proj=utm +zone=34 +south +a=6378249.145 +b=6356514.966398753 +units=m +no_defs;Cape / UTM zone 34S
1971;22235;+proj=utm +zone=35 +south +a=6378249.145 +b=6356514.966398753 +units=m +no_defs;Cape / UTM zone 35S
1972;22236;+proj=utm +zone=36 +south +a=6378249.145 +b=6356514.966398753 +units=m +no_defs;Cape / UTM zone 36S
1973;22275;+a=6378249.145 +b=6356514.966398753 +units=m +no_defs;South African Coordinate System zone 15
1974;22277;+a=6378249.145 +b=6356514.966398753 +units=m +no_defs;South African Coordinate System zone 17
1975;22279;+a=6378249.145 +b=6356514.966398753 +units=m +no_defs;South African Coordinate System zone 19
1976;22281;+a=6378249.145 +b=6356514.966398753 +units=m +no_defs;South African Coordinate System zone 21
1977;22283;+a=6378249.145 +b=6356514.966398753 +units=m +no_defs;South African Coordinate System zone 23
1978;22285;+a=6378249.145 +b=6356514.966398753 +units=m +no_defs;South African Coordinate System zone 25
1979;22287;+a=6378249.145 +b=6356514.966398753 +units=m +no_defs;South African Coordinate System zone 27
1980;22289;+a=6378249.145 +b=6356514.966398753 +units=m +no_defs;South African Coordinate System zone 29
1981;22291;+a=6378249.145 +b=6356514.966398753 +units=m +no_defs;South African Coordinate System zone 31
1982;22293;+a=6378249.145 +b=6356514.966398753 +units=m +no_defs;South African Coordinate System zone 33
1983;22300;+a=6378249.2 +b=6356515 +pm=paris +units=km +no_defs;Carthage (Paris) / Tunisia Mining Grid
1984;22332;+proj=utm +zone=32 +a=6378249.2 +b=6356515 +units=m +no_defs;Carthage / UTM zone 32N
1985;22391;+proj=lcc +lat_1=36 +lat_0=36 +lon_0=9.9 +k_0=0.999625544 +x_0=500000 +y_0=300000 +a=6378249.2 +b=6356515 +units=m +no_defs;Carthage / Nord Tunisie
1986;22392;+proj=lcc +lat_1=33.3 +lat_0=33.3 +lon_0=9.9 +k_0=0.999625769 +x_0=500000 +y_0=300000 +a=6378249.2 +b=6356515 +units=m +no_defs;Carthage / Sud Tunisie
1987;22521;+proj=utm +zone=21 +south +ellps=intl +towgs84=-206,172,-6,0,0,0,0 +units=m +no_defs;Corrego Alegre / UTM zone 21S
1988;22522;+proj=utm +zone=22 +south +ellps=intl +towgs84=-206,172,-6,0,0,0,0 +units=m +no_defs;Corrego Alegre / UTM zone 22S
1989;22523;+proj=utm +zone=23 +south +ellps=intl +towgs84=-206,172,-6,0,0,0,0 +units=m +no_defs;Corrego Alegre / UTM zone 23S
1990;22524;+proj=utm +zone=24 +south +ellps=intl +towgs84=-206,172,-6,0,0,0,0 +units=m +no_defs;Corrego Alegre / UTM zone 24S
1991;22525;+proj=utm +zone=25 +south +ellps=intl +towgs84=-206,172,-6,0,0,0,0 +units=m +no_defs;Corrego Alegre / UTM zone 25S
1992;22700;+proj=lcc +lat_1=34.65 +lat_0=34.65 +lon_0=37.35 +k_0=0.9996256 +x_0=300000 +y_0=300000 +a=6378249.2 +b=6356515 +units=m +no_defs;Deir ez Zor / Levant Zone
1993;22770;+proj=lcc +lat_1=34.65 +lat_0=34.65 +lon_0=37.35 +k_0=0.9996256 +x_0=300000 +y_0=300000 +a=6378249.2 +b=6356515 +units=m +no_defs;Deir ez Zor / Syria Lambert
1994;22780;+proj=sterea +lat_0=34.2 +lon_0=39.15 +k=0.999534 +x_0=0 +y_0=0 +a=6378249.2 +b=6356515 +units=m +no_defs;Deir ez Zor / Levant Stereographic
1995;22832;+proj=utm +zone=32 +a=6378249.2 +b=6356515 +units=m +no_defs;Douala / UTM zone 32N (deprecated)
1996;22991;+proj=tmerc +lat_0=30 +lon_0=35 +k=1.000000 +x_0=300000 +y_0=1100000 +ellps=helmert +units=m +no_defs;Egypt 1907 / Blue Belt
1997;22992;+proj=tmerc +lat_0=30 +lon_0=31 +k=1.000000 +x_0=615000 +y_0=810000 +ellps=helmert +units=m +no_defs;Egypt 1907 / Red Belt
1998;22993;+proj=tmerc +lat_0=30 +lon_0=27 +k=1.000000 +x_0=700000 +y_0=200000 +ellps=helmert +units=m +no_defs;Egypt 1907 / Purple Belt
1999;22994;+proj=tmerc +lat_0=30 +lon_0=27 +k=1.000000 +x_0=700000 +y_0=1200000 +ellps=helmert +units=m +no_defs;Egypt 1907 / Extended Purple Belt
2000;23028;+proj=utm +zone=28 +ellps=intl +units=m +no_defs;ED50 / UTM zone 28N
2001;23029;+proj=utm +zone=29 +ellps=intl +units=m +no_defs;ED50 / UTM zone 29N
2002;23030;+proj=utm +zone=30 +ellps=intl +units=m +no_defs;ED50 / UTM zone 30N
2003;23031;+proj=utm +zone=31 +ellps=intl +units=m +no_defs;ED50 / UTM zone 31N
2004;23032;+proj=utm +zone=32 +ellps=intl +units=m +no_defs;ED50 / UTM zone 32N
2005;23033;+proj=utm +zone=33 +ellps=intl +units=m +no_defs;ED50 / UTM zone 33N
2006;23034;+proj=utm +zone=34 +ellps=intl +units=m +no_defs;ED50 / UTM zone 34N
2007;23035;+proj=utm +zone=35 +ellps=intl +units=m +no_defs;ED50 / UTM zone 35N
2008;23036;+proj=utm +zone=36 +ellps=intl +units=m +no_defs;ED50 / UTM zone 36N
2009;23037;+proj=utm +zone=37 +ellps=intl +units=m +no_defs;ED50 / UTM zone 37N
2010;23038;+proj=utm +zone=38 +ellps=intl +units=m +no_defs;ED50 / UTM zone 38N
2011;23090;+proj=tmerc +lat_0=0 +lon_0=0 +k=0.999600 +x_0=500000 +y_0=0 +ellps=intl +units=m +no_defs;ED50 / TM 0 N
2012;23095;+proj=tmerc +lat_0=0 +lon_0=5 +k=0.999600 +x_0=500000 +y_0=0 +ellps=intl +units=m +no_defs;ED50 / TM 5 NE
2013;23239;+proj=utm +zone=39 +ellps=clrk80 +units=m +no_defs;Fahud / UTM zone 39N
2014;23240;+proj=utm +zone=40 +ellps=clrk80 +units=m +no_defs;Fahud / UTM zone 40N
2015;23433;+proj=utm +zone=33 +a=6378249.2 +b=6356515 +units=m +no_defs;Garoua / UTM zone 33N (deprecated)
2016;23700;+proj=somerc +lat_0=47.14439372222222 +lon_0=19.04857177777778 +x_0=650000 +y_0=200000 +ellps=GRS67 +units=m +no_defs;HD72 / EOV
2017;23846;+proj=utm +zone=46 +a=6378160 +b=6356774.50408554 +units=m +no_defs;ID74 / UTM zone 46N
2018;23847;+proj=utm +zone=47 +a=6378160 +b=6356774.50408554 +units=m +no_defs;ID74 / UTM zone 47N
2019;23848;+proj=utm +zone=48 +a=6378160 +b=6356774.50408554 +units=m +no_defs;ID74 / UTM zone 48N
2020;23849;+proj=utm +zone=49 +a=6378160 +b=6356774.50408554 +units=m +no_defs;ID74 / UTM zone 49N
2021;23850;+proj=utm +zone=50 +a=6378160 +b=6356774.50408554 +units=m +no_defs;ID74 / UTM zone 50N
2022;23851;+proj=utm +zone=51 +a=6378160 +b=6356774.50408554 +units=m +no_defs;ID74 / UTM zone 51N
2023;23852;+proj=utm +zone=52 +a=6378160 +b=6356774.50408554 +units=m +no_defs;ID74 / UTM zone 52N
2024;23853;+proj=utm +zone=53 +a=6378160 +b=6356774.50408554 +units=m +no_defs;ID74 / UTM zone 53N (deprecated)
2025;23866;+proj=utm +zone=46 +ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;DGN95 / UTM zone 46N
2026;23867;+proj=utm +zone=47 +ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;DGN95 / UTM zone 47N
2027;23868;+proj=utm +zone=48 +ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;DGN95 / UTM zone 48N
2028;23869;+proj=utm +zone=49 +ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;DGN95 / UTM zone 49N
2029;23870;+proj=utm +zone=50 +ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;DGN95 / UTM zone 50N
2030;23871;+proj=utm +zone=51 +ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;DGN95 / UTM zone 51N
2031;23872;+proj=utm +zone=52 +ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;DGN95 / UTM zone 52N
2032;23877;+proj=utm +zone=47 +south +ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;DGN95 / UTM zone 47S
2033;23878;+proj=utm +zone=48 +south +ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;DGN95 / UTM zone 48S
2034;23879;+proj=utm +zone=49 +south +ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;DGN95 / UTM zone 49S
2035;23880;+proj=utm +zone=50 +south +ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;DGN95 / UTM zone 50S
2036;23881;+proj=utm +zone=51 +south +ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;DGN95 / UTM zone 51S
2037;23882;+proj=utm +zone=52 +south +ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;DGN95 / UTM zone 52S
2038;23883;+proj=utm +zone=53 +south +ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;DGN95 / UTM zone 53S
2039;23884;+proj=utm +zone=54 +south +ellps=WGS84 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;DGN95 / UTM zone 54S
2040;23886;+proj=utm +zone=46 +south +a=6378160 +b=6356774.50408554 +units=m +no_defs;ID74 / UTM zone 46S (deprecated)
2041;23887;+proj=utm +zone=47 +south +a=6378160 +b=6356774.50408554 +units=m +no_defs;ID74 / UTM zone 47S
2042;23888;+proj=utm +zone=48 +south +a=6378160 +b=6356774.50408554 +units=m +no_defs;ID74 / UTM zone 48S
2043;23889;+proj=utm +zone=49 +south +a=6378160 +b=6356774.50408554 +units=m +no_defs;ID74 / UTM zone 49S
2044;23890;+proj=utm +zone=50 +south +a=6378160 +b=6356774.50408554 +units=m +no_defs;ID74 / UTM zone 50S
2045;23891;+proj=utm +zone=51 +south +a=6378160 +b=6356774.50408554 +units=m +no_defs;ID74 / UTM zone 51S
2046;23892;+proj=utm +zone=52 +south +a=6378160 +b=6356774.50408554 +units=m +no_defs;ID74 / UTM zone 52S
2047;23893;+proj=utm +zone=53 +south +a=6378160 +b=6356774.50408554 +units=m +no_defs;ID74 / UTM zone 53S
2048;23894;+proj=utm +zone=54 +south +a=6378160 +b=6356774.50408554 +units=m +no_defs;ID74 / UTM zone 54S
2049;23946;+proj=utm +zone=46 +a=6377276.345 +b=6356075.413140239 +towgs84=217,823,299,0,0,0,0 +units=m +no_defs;Indian 1954 / UTM zone 46N
2050;23947;+proj=utm +zone=47 +a=6377276.345 +b=6356075.413140239 +towgs84=217,823,299,0,0,0,0 +units=m +no_defs;Indian 1954 / UTM zone 47N
2051;23948;+proj=utm +zone=48 +a=6377276.345 +b=6356075.413140239 +towgs84=217,823,299,0,0,0,0 +units=m +no_defs;Indian 1954 / UTM zone 48N
2052;24047;+proj=utm +zone=47 +a=6377276.345 +b=6356075.413140239 +units=m +no_defs;Indian 1975 / UTM zone 47N
2053;24048;+proj=utm +zone=48 +a=6377276.345 +b=6356075.413140239 +units=m +no_defs;Indian 1975 / UTM zone 48N
2054;24100;+proj=lcc +lat_1=18 +lat_0=18 +lon_0=-77 +k_0=1 +x_0=167638.49597 +y_0=121918.90616 +ellps=clrk80 +to_meter=0.3047972654 +no_defs;Jamaica 1875 / Jamaica (Old Grid)
2055;24200;+proj=lcc +lat_1=18 +lat_0=18 +lon_0=-77 +k_0=1 +x_0=250000 +y_0=150000 +ellps=clrk66 +units=m +no_defs;JAD69 / Jamaica National Grid
2056;24305;+proj=utm +zone=45 +a=6377276.345 +b=6356075.413140239 +units=m +no_defs;Kalianpur 1937 / UTM zone 45N
2057;24306;+proj=utm +zone=46 +a=6377276.345 +b=6356075.413140239 +units=m +no_defs;Kalianpur 1937 / UTM zone 46N
2058;24311;+proj=utm +zone=41 +a=6377301.243 +b=6356100.230165385 +units=m +no_defs;Kalianpur 1962 / UTM zone 41N
2059;24312;+proj=utm +zone=42 +a=6377301.243 +b=6356100.230165385 +units=m +no_defs;Kalianpur 1962 / UTM zone 42N
2060;24313;+proj=utm +zone=43 +a=6377301.243 +b=6356100.230165385 +units=m +no_defs;Kalianpur 1962 / UTM zone 43N
2061;24342;+proj=utm +zone=42 +a=6377299.151 +b=6356098.145120132 +towgs84=295,736,257,0,0,0,0 +units=m +no_defs;Kalianpur 1975 / UTM zone 42N
2062;24343;+proj=utm +zone=43 +a=6377299.151 +b=6356098.145120132 +towgs84=295,736,257,0,0,0,0 +units=m +no_defs;Kalianpur 1975 / UTM zone 43N
2063;24344;+proj=utm +zone=44 +a=6377299.151 +b=6356098.145120132 +towgs84=295,736,257,0,0,0,0 +units=m +no_defs;Kalianpur 1975 / UTM zone 44N
2064;24345;+proj=utm +zone=45 +a=6377299.151 +b=6356098.145120132 +towgs84=295,736,257,0,0,0,0 +units=m +no_defs;Kalianpur 1975 / UTM zone 45N
2065;24346;+proj=utm +zone=46 +a=6377299.151 +b=6356098.145120132 +towgs84=295,736,257,0,0,0,0 +units=m +no_defs;Kalianpur 1975 / UTM zone 46N
2066;24347;+proj=utm +zone=47 +a=6377299.151 +b=6356098.145120132 +towgs84=295,736,257,0,0,0,0 +units=m +no_defs;Kalianpur 1975 / UTM zone 47N
2067;24370;+proj=lcc +lat_1=39.5 +lat_0=39.5 +lon_0=68 +k_0=0.99846154 +x_0=2153865.73916853 +y_0=2368292.194628102 +a=6377299.36559538 +b=6356098.357204817 +to_meter=0.9143985307444408 +no_defs;Kalianpur 1880 / India zone 0
2068;24371;+proj=lcc +lat_1=32.5 +lat_0=32.5 +lon_0=68 +k_0=0.99878641 +x_0=2743195.592233322 +y_0=914398.5307444407 +a=6377299.36559538 +b=6356098.357204817 +to_meter=0.9143985307444408 +no_defs;Kalianpur 1880 / India zone I
2069;24372;+proj=lcc +lat_1=26 +lat_0=26 +lon_0=74 +k_0=0.99878641 +x_0=2743195.592233322 +y_0=914398.5307444407 +a=6377299.36559538 +b=6356098.357204817 +to_meter=0.9143985307444408 +no_defs;Kalianpur 1880 / India zone IIa
2070;24373;+proj=lcc +lat_1=19 +lat_0=19 +lon_0=80 +k_0=0.99878641 +x_0=2743195.592233322 +y_0=914398.5307444407 +a=6377299.36559538 +b=6356098.357204817 +to_meter=0.9143985307444408 +no_defs;Kalianpur 1880 / India zone III
2071;24374;+proj=lcc +lat_1=12 +lat_0=12 +lon_0=80 +k_0=0.99878641 +x_0=2743195.592233322 +y_0=914398.5307444407 +a=6377299.36559538 +b=6356098.357204817 +to_meter=0.9143985307444408 +no_defs;Kalianpur 1880 / India zone IV
2072;24375;+proj=lcc +lat_1=26 +lat_0=26 +lon_0=90 +k_0=0.99878641 +x_0=2743185.69 +y_0=914395.23 +a=6377276.345 +b=6356075.413140239 +units=m +no_defs;Kalianpur 1937 / India zone IIb
2073;24376;+proj=lcc +lat_1=32.5 +lat_0=32.5 +lon_0=68 +k_0=0.99878641 +x_0=2743196.4 +y_0=914398.8 +a=6377301.243 +b=6356100.230165385 +units=m +no_defs;Kalianpur 1962 / India zone I
2074;24377;+proj=lcc +lat_1=26 +lat_0=26 +lon_0=74 +k_0=0.99878641 +x_0=2743196.4 +y_0=914398.8 +a=6377301.243 +b=6356100.230165385 +units=m +no_defs;Kalianpur 1962 / India zone IIa
2075;24378;+proj=lcc +lat_1=32.5 +lat_0=32.5 +lon_0=68 +k_0=0.99878641 +x_0=2743195.5 +y_0=914398.5 +a=6377299.151 +b=6356098.145120132 +towgs84=295,736,257,0,0,0,0 +units=m +no_defs;Kalianpur 1975 / India zone I
2076;24379;+proj=lcc +lat_1=26 +lat_0=26 +lon_0=74 +k_0=0.99878641 +x_0=2743195.5 +y_0=914398.5 +a=6377299.151 +b=6356098.145120132 +towgs84=295,736,257,0,0,0,0 +units=m +no_defs;Kalianpur 1975 / India zone IIa
2077;24380;+proj=lcc +lat_1=26 +lat_0=26 +lon_0=90 +k_0=0.99878641 +x_0=2743195.5 +y_0=914398.5 +a=6377299.151 +b=6356098.145120132 +towgs84=295,736,257,0,0,0,0 +units=m +no_defs;Kalianpur 1975 / India zone IIb
2078;24381;+proj=lcc +lat_1=19 +lat_0=19 +lon_0=80 +k_0=0.99878641 +x_0=2743195.5 +y_0=914398.5 +a=6377299.151 +b=6356098.145120132 +towgs84=295,736,257,0,0,0,0 +units=m +no_defs;Kalianpur 1975 / India zone III
2079;24382;+proj=lcc +lat_1=26 +lat_0=26 +lon_0=90 +k_0=0.99878641 +x_0=2743195.592233322 +y_0=914398.5307444407 +a=6377299.36559538 +b=6356098.357204817 +to_meter=0.9143985307444408 +no_defs;Kalianpur 1880 / India zone IIb
2080;24383;+proj=lcc +lat_1=12 +lat_0=12 +lon_0=80 +k_0=0.99878641 +x_0=2743195.5 +y_0=914398.5 +a=6377299.151 +b=6356098.145120132 +towgs84=295,736,257,0,0,0,0 +units=m +no_defs;Kalianpur 1975 / India zone IV
2081;24500;+proj=cass +lat_0=1.287646666666667 +lon_0=103.8530022222222 +x_0=30000 +y_0=30000 +a=6377304.063 +b=6356103.038993155 +towgs84=-11,851,5,0,0,0,0 +units=m +no_defs;Kertau 1968 / Singapore Grid
2082;24547;+proj=utm +zone=47 +a=6377304.063 +b=6356103.038993155 +towgs84=-11,851,5,0,0,0,0 +units=m +no_defs;Kertau 1968 / UTM zone 47N
2083;24548;+proj=utm +zone=48 +a=6377304.063 +b=6356103.038993155 +towgs84=-11,851,5,0,0,0,0 +units=m +no_defs;Kertau 1968 / UTM zone 48N
2084;24571;+proj=omerc +lat_0=4 +lonc=102.25 +alpha=323.0257905 +k=0.99984 +x_0=804671.2997750348 +y_0=0 +a=6377304.063 +b=6356103.038993155 +towgs84=-11,851,5,0,0,0,0 +to_meter=20.11678249437587 +no_defs;Kertau / R.S.O. Malaya (ch) (deprecated)
2085;24600;+proj=lcc +lat_1=32.5 +lat_0=32.5 +lon_0=45 +k_0=0.9987864078000001 +x_0=1500000 +y_0=1166200 +ellps=clrk80 +towgs84=-294.7,-200.1,525.5,0,0,0,0 +units=m +no_defs;KOC Lambert
2086;24718;+proj=utm +zone=18 +ellps=intl +towgs84=-273.5,110.6,-357.9,0,0,0,0 +units=m +no_defs;La Canoa / UTM zone 18N
2087;24719;+proj=utm +zone=19 +ellps=intl +towgs84=-273.5,110.6,-357.9,0,0,0,0 +units=m +no_defs;La Canoa / UTM zone 19N
2088;24720;+proj=utm +zone=20 +ellps=intl +towgs84=-273.5,110.6,-357.9,0,0,0,0 +units=m +no_defs;La Canoa / UTM zone 20N
2089;24817;+proj=utm +zone=17 +ellps=intl +units=m +no_defs;PSAD56 / UTM zone 17N
2090;24818;+proj=utm +zone=18 +ellps=intl +units=m +no_defs;PSAD56 / UTM zone 18N
2091;24819;+proj=utm +zone=19 +ellps=intl +units=m +no_defs;PSAD56 / UTM zone 19N
2092;24820;+proj=utm +zone=20 +ellps=intl +units=m +no_defs;PSAD56 / UTM zone 20N
2093;24821;+proj=utm +zone=21 +ellps=intl +units=m +no_defs;PSAD56 / UTM zone 21N
2094;24877;+proj=utm +zone=17 +south +ellps=intl +units=m +no_defs;PSAD56 / UTM zone 17S
2095;24878;+proj=utm +zone=18 +south +ellps=intl +units=m +no_defs;PSAD56 / UTM zone 18S
2096;24879;+proj=utm +zone=19 +south +ellps=intl +units=m +no_defs;PSAD56 / UTM zone 19S
2097;24880;+proj=utm +zone=20 +south +ellps=intl +units=m +no_defs;PSAD56 / UTM zone 20S
2098;24881;+proj=utm +zone=21 +south +ellps=intl +units=m +no_defs;PSAD56 / UTM zone 21S
2099;24882;+proj=utm +zone=22 +south +ellps=intl +units=m +no_defs;PSAD56 / UTM zone 22S
2100;24891;+proj=tmerc +lat_0=-6 +lon_0=-80.5 +k=0.999830 +x_0=222000 +y_0=1426834.743 +ellps=intl +units=m +no_defs;PSAD56 / Peru west zone
2101;24892;+proj=tmerc +lat_0=-9.5 +lon_0=-76 +k=0.999330 +x_0=720000 +y_0=1039979.159 +ellps=intl +units=m +no_defs;PSAD56 / Peru central zone
2102;24893;+proj=tmerc +lat_0=-9.5 +lon_0=-70.5 +k=0.999530 +x_0=1324000 +y_0=1040084.558 +ellps=intl +units=m +no_defs;PSAD56 / Peru east zone
2103;25000;+proj=tmerc +lat_0=4.666666666666667 +lon_0=-1 +k=0.999750 +x_0=274319.51 +y_0=0 +ellps=clrk80 +towgs84=-130,29,364,0,0,0,0 +units=m +no_defs;Leigon / Ghana Metre Grid
2104;25231;+proj=utm +zone=31 +a=6378249.2 +b=6356515 +units=m +no_defs;Lome / UTM zone 31N
2105;25391;+proj=tmerc +lat_0=0 +lon_0=117 +k=0.999950 +x_0=500000 +y_0=0 +ellps=clrk66 +units=m +no_defs;Luzon 1911 / Philippines zone I
2106;25392;+proj=tmerc +lat_0=0 +lon_0=119 +k=0.999950 +x_0=500000 +y_0=0 +ellps=clrk66 +units=m +no_defs;Luzon 1911 / Philippines zone II
2107;25393;+proj=tmerc +lat_0=0 +lon_0=121 +k=0.999950 +x_0=500000 +y_0=0 +ellps=clrk66 +units=m +no_defs;Luzon 1911 / Philippines zone III
2108;25394;+proj=tmerc +lat_0=0 +lon_0=123 +k=0.999950 +x_0=500000 +y_0=0 +ellps=clrk66 +units=m +no_defs;Luzon 1911 / Philippines zone IV
2109;25395;+proj=tmerc +lat_0=0 +lon_0=125 +k=0.999950 +x_0=500000 +y_0=0 +ellps=clrk66 +units=m +no_defs;Luzon 1911 / Philippines zone V
2110;25700;+proj=merc +lon_0=110 +k=0.997000 +x_0=3900000 +y_0=900000 +ellps=bessel +towgs84=-587.8,519.75,145.76,0,0,0,0 +pm=jakarta +units=m +no_defs;Makassar (Jakarta) / NEIEZ (deprecated)
2111;25828;+proj=utm +zone=28 +ellps=GRS80 +units=m +no_defs;ETRS89 / UTM zone 28N
2112;25829;+proj=utm +zone=29 +ellps=GRS80 +units=m +no_defs;ETRS89 / UTM zone 29N
2113;25830;+proj=utm +zone=30 +ellps=GRS80 +units=m +no_defs;ETRS89 / UTM zone 30N
2114;25831;+proj=utm +zone=31 +ellps=GRS80 +units=m +no_defs;ETRS89 / UTM zone 31N
2115;25832;+proj=utm +zone=32 +ellps=GRS80 +units=m +no_defs;ETRS89 / UTM zone 32N
2116;25833;+proj=utm +zone=33 +ellps=GRS80 +units=m +no_defs;ETRS89 / UTM zone 33N
2117;25834;+proj=utm +zone=34 +ellps=GRS80 +units=m +no_defs;ETRS89 / UTM zone 34N
2118;25835;+proj=utm +zone=35 +ellps=GRS80 +units=m +no_defs;ETRS89 / UTM zone 35N
2119;25836;+proj=utm +zone=36 +ellps=GRS80 +units=m +no_defs;ETRS89 / UTM zone 36N
2120;25837;+proj=utm +zone=37 +ellps=GRS80 +units=m +no_defs;ETRS89 / UTM zone 37N
2121;25838;+proj=utm +zone=38 +ellps=GRS80 +units=m +no_defs;ETRS89 / UTM zone 38N
2122;25884;+proj=tmerc +lat_0=0 +lon_0=24 +k=0.999600 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;ETRS89 / TM Baltic93
2123;25932;+proj=utm +zone=32 +south +ellps=intl +units=m +no_defs;Malongo 1987 / UTM zone 32S
2124;26191;+proj=lcc +lat_1=33.3 +lat_0=33.3 +lon_0=-5.4 +k_0=0.999625769 +x_0=500000 +y_0=300000 +a=6378249.2 +b=6356515 +towgs84=31,146,47,0,0,0,0 +units=m +no_defs;Merchich / Nord Maroc
2125;26192;+proj=lcc +lat_1=29.7 +lat_0=29.7 +lon_0=-5.4 +k_0=0.9996155960000001 +x_0=500000 +y_0=300000 +a=6378249.2 +b=6356515 +towgs84=31,146,47,0,0,0,0 +units=m +no_defs;Merchich / Sud Maroc
2126;26193;+proj=lcc +lat_1=26.1 +lat_0=26.1 +lon_0=-5.4 +k_0=0.9996 +x_0=1200000 +y_0=400000 +a=6378249.2 +b=6356515 +towgs84=31,146,47,0,0,0,0 +units=m +no_defs;Merchich / Sahara (deprecated)
2127;26194;+proj=lcc +lat_1=26.1 +lat_0=26.1 +lon_0=-5.4 +k_0=0.999616304 +x_0=1200000 +y_0=400000 +a=6378249.2 +b=6356515 +towgs84=31,146,47,0,0,0,0 +units=m +no_defs;Merchich / Sahara Nord
2128;26195;+proj=lcc +lat_1=22.5 +lat_0=22.5 +lon_0=-5.4 +k_0=0.999616437 +x_0=1500000 +y_0=400000 +a=6378249.2 +b=6356515 +towgs84=31,146,47,0,0,0,0 +units=m +no_defs;Merchich / Sahara Sud
2129;26237;+proj=utm +zone=37 +ellps=bessel +towgs84=639,405,60,0,0,0,0 +units=m +no_defs;Massawa / UTM zone 37N
2130;26331;+proj=utm +zone=31 +ellps=clrk80 +units=m +no_defs;Minna / UTM zone 31N
2131;26332;+proj=utm +zone=32 +ellps=clrk80 +units=m +no_defs;Minna / UTM zone 32N
2132;26391;+proj=tmerc +lat_0=4 +lon_0=4.5 +k=0.999750 +x_0=230738.26 +y_0=0 +ellps=clrk80 +units=m +no_defs;Minna / Nigeria West Belt
2133;26392;+proj=tmerc +lat_0=4 +lon_0=8.5 +k=0.999750 +x_0=670553.98 +y_0=0 +ellps=clrk80 +units=m +no_defs;Minna / Nigeria Mid Belt
2134;26393;+proj=tmerc +lat_0=4 +lon_0=12.5 +k=0.999750 +x_0=1110369.7 +y_0=0 +ellps=clrk80 +units=m +no_defs;Minna / Nigeria East Belt
2135;26432;+proj=utm +zone=32 +south +ellps=intl +towgs84=-252.95,-4.11,-96.38,0,0,0,0 +units=m +no_defs;Mhast / UTM zone 32S (deprecated)
2136;26591;+proj=tmerc +lat_0=0 +lon_0=-3.45233333333333 +k=0.999600 +x_0=1500000 +y_0=0 +ellps=intl +pm=rome +units=m +no_defs;Monte Mario (Rome) / Italy zone 1
2137;26592;+proj=tmerc +lat_0=0 +lon_0=2.54766666666666 +k=0.999600 +x_0=2520000 +y_0=0 +ellps=intl +pm=rome +units=m +no_defs;Monte Mario (Rome) / Italy zone 2
2138;26632;+proj=utm +zone=32 +a=6378249.2 +b=6356515 +units=m +no_defs;M'poraloko / UTM zone 32N
2139;26692;+proj=utm +zone=32 +south +a=6378249.2 +b=6356515 +units=m +no_defs;M'poraloko / UTM zone 32S
2140;26701;+proj=utm +zone=1 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / UTM zone 1N
2141;26702;+proj=utm +zone=2 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / UTM zone 2N
2142;26703;+proj=utm +zone=3 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / UTM zone 3N
2143;26704;+proj=utm +zone=4 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / UTM zone 4N
2144;26705;+proj=utm +zone=5 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / UTM zone 5N
2145;26706;+proj=utm +zone=6 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / UTM zone 6N
2146;26707;+proj=utm +zone=7 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / UTM zone 7N
2147;26708;+proj=utm +zone=8 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / UTM zone 8N
2148;26709;+proj=utm +zone=9 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / UTM zone 9N
2149;26710;+proj=utm +zone=10 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / UTM zone 10N
2150;26711;+proj=utm +zone=11 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / UTM zone 11N
2151;26712;+proj=utm +zone=12 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / UTM zone 12N
2152;26713;+proj=utm +zone=13 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / UTM zone 13N
2153;26714;+proj=utm +zone=14 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / UTM zone 14N
2154;26715;+proj=utm +zone=15 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / UTM zone 15N
2155;26716;+proj=utm +zone=16 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / UTM zone 16N
2156;26717;+proj=utm +zone=17 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / UTM zone 17N
2157;26718;+proj=utm +zone=18 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / UTM zone 18N
2158;26719;+proj=utm +zone=19 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / UTM zone 19N
2159;26720;+proj=utm +zone=20 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / UTM zone 20N
2160;26721;+proj=utm +zone=21 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / UTM zone 21N
2161;26722;+proj=utm +zone=22 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / UTM zone 22N
2162;26729;+proj=tmerc +lat_0=30.5 +lon_0=-85.83333333333333 +k=0.999960 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Alabama East
2163;26730;+proj=tmerc +lat_0=30 +lon_0=-87.5 +k=0.999933 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Alabama West
2164;26731;+proj=omerc +lat_0=57 +lonc=-133.6666666666667 +alpha=323.1301023611111 +k=0.9999 +x_0=5000000.001016002 +y_0=-5000000.001016002 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Alaska zone 1
2165;26732;+proj=tmerc +lat_0=54 +lon_0=-142 +k=0.999900 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Alaska zone 2
2166;26733;+proj=tmerc +lat_0=54 +lon_0=-146 +k=0.999900 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Alaska zone 3
2167;26734;+proj=tmerc +lat_0=54 +lon_0=-150 +k=0.999900 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Alaska zone 4
2168;26735;+proj=tmerc +lat_0=54 +lon_0=-154 +k=0.999900 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Alaska zone 5
2169;26736;+proj=tmerc +lat_0=54 +lon_0=-158 +k=0.999900 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Alaska zone 6
2170;26737;+proj=tmerc +lat_0=54 +lon_0=-162 +k=0.999900 +x_0=213360.4267208534 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Alaska zone 7
2171;26738;+proj=tmerc +lat_0=54 +lon_0=-166 +k=0.999900 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Alaska zone 8
2172;26739;+proj=tmerc +lat_0=54 +lon_0=-170 +k=0.999900 +x_0=182880.3657607315 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Alaska zone 9
2173;26740;+proj=lcc +lat_1=53.83333333333334 +lat_2=51.83333333333334 +lat_0=51 +lon_0=-176 +x_0=914401.8288036576 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Alaska zone 10
2174;26741;+proj=lcc +lat_1=41.66666666666666 +lat_2=40 +lat_0=39.33333333333334 +lon_0=-122 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / California zone I
2175;26742;+proj=lcc +lat_1=39.83333333333334 +lat_2=38.33333333333334 +lat_0=37.66666666666666 +lon_0=-122 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / California zone II
2176;26743;+proj=lcc +lat_1=38.43333333333333 +lat_2=37.06666666666667 +lat_0=36.5 +lon_0=-120.5 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / California zone III
2177;26744;+proj=lcc +lat_1=37.25 +lat_2=36 +lat_0=35.33333333333334 +lon_0=-119 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / California zone IV
2178;26745;+proj=lcc +lat_1=35.46666666666667 +lat_2=34.03333333333333 +lat_0=33.5 +lon_0=-118 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / California zone V
2179;26746;+proj=lcc +lat_1=33.88333333333333 +lat_2=32.78333333333333 +lat_0=32.16666666666666 +lon_0=-116.25 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / California zone VI
2180;26747;+proj=lcc +lat_1=34.41666666666666 +lat_2=33.86666666666667 +lat_0=34.13333333333333 +lon_0=-118.3333333333333 +x_0=1276106.450596901 +y_0=127079.524511049 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / California zone VII (deprecated)
2181;26748;+proj=tmerc +lat_0=31 +lon_0=-110.1666666666667 +k=0.999900 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Arizona East
2182;26749;+proj=tmerc +lat_0=31 +lon_0=-111.9166666666667 +k=0.999900 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Arizona Central
2183;26750;+proj=tmerc +lat_0=31 +lon_0=-113.75 +k=0.999933 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Arizona West
2184;26751;+proj=lcc +lat_1=36.23333333333333 +lat_2=34.93333333333333 +lat_0=34.33333333333334 +lon_0=-92 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Arkansas North
2185;26752;+proj=lcc +lat_1=34.76666666666667 +lat_2=33.3 +lat_0=32.66666666666666 +lon_0=-92 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Arkansas South
2186;26753;+proj=lcc +lat_1=39.71666666666667 +lat_2=40.78333333333333 +lat_0=39.33333333333334 +lon_0=-105.5 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Colorado North
2187;26754;+proj=lcc +lat_1=39.75 +lat_2=38.45 +lat_0=37.83333333333334 +lon_0=-105.5 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Colorado Central
2188;26755;+proj=lcc +lat_1=38.43333333333333 +lat_2=37.23333333333333 +lat_0=36.66666666666666 +lon_0=-105.5 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Colorado South
2189;26756;+proj=lcc +lat_1=41.86666666666667 +lat_2=41.2 +lat_0=40.83333333333334 +lon_0=-72.75 +x_0=182880.3657607315 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Connecticut
2190;26757;+proj=tmerc +lat_0=38 +lon_0=-75.41666666666667 +k=0.999995 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Delaware
2191;26758;+proj=tmerc +lat_0=24.33333333333333 +lon_0=-81 +k=0.999941 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Florida East
2192;26759;+proj=tmerc +lat_0=24.33333333333333 +lon_0=-82 +k=0.999941 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Florida West
2193;26760;+proj=lcc +lat_1=30.75 +lat_2=29.58333333333333 +lat_0=29 +lon_0=-84.5 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Florida North
2194;26766;+proj=tmerc +lat_0=30 +lon_0=-82.16666666666667 +k=0.999900 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Georgia East
2195;26767;+proj=tmerc +lat_0=30 +lon_0=-84.16666666666667 +k=0.999900 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Georgia West
2196;26768;+proj=tmerc +lat_0=41.66666666666666 +lon_0=-112.1666666666667 +k=0.999947 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Idaho East
2197;26769;+proj=tmerc +lat_0=41.66666666666666 +lon_0=-114 +k=0.999947 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Idaho Central
2198;26770;+proj=tmerc +lat_0=41.66666666666666 +lon_0=-115.75 +k=0.999933 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Idaho West
2199;26771;+proj=tmerc +lat_0=36.66666666666666 +lon_0=-88.33333333333333 +k=0.999975 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Illinois East
2200;26772;+proj=tmerc +lat_0=36.66666666666666 +lon_0=-90.16666666666667 +k=0.999941 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Illinois West
2201;26773;+proj=tmerc +lat_0=37.5 +lon_0=-85.66666666666667 +k=0.999967 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Indiana East
2202;26774;+proj=tmerc +lat_0=37.5 +lon_0=-87.08333333333333 +k=0.999967 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Indiana West
2203;26775;+proj=lcc +lat_1=43.26666666666667 +lat_2=42.06666666666667 +lat_0=41.5 +lon_0=-93.5 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Iowa North
2204;26776;+proj=lcc +lat_1=41.78333333333333 +lat_2=40.61666666666667 +lat_0=40 +lon_0=-93.5 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Iowa South
2205;26777;+proj=lcc +lat_1=39.78333333333333 +lat_2=38.71666666666667 +lat_0=38.33333333333334 +lon_0=-98 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Kansas North
2206;26778;+proj=lcc +lat_1=38.56666666666667 +lat_2=37.26666666666667 +lat_0=36.66666666666666 +lon_0=-98.5 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Kansas South
2207;26779;+proj=lcc +lat_1=37.96666666666667 +lat_2=38.96666666666667 +lat_0=37.5 +lon_0=-84.25 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Kentucky North
2208;26780;+proj=lcc +lat_1=36.73333333333333 +lat_2=37.93333333333333 +lat_0=36.33333333333334 +lon_0=-85.75 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Kentucky South
2209;26781;+proj=lcc +lat_1=31.16666666666667 +lat_2=32.66666666666666 +lat_0=30.66666666666667 +lon_0=-92.5 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Louisiana North
2210;26782;+proj=lcc +lat_1=29.3 +lat_2=30.7 +lat_0=28.66666666666667 +lon_0=-91.33333333333333 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Louisiana South
2211;26783;+proj=tmerc +lat_0=43.83333333333334 +lon_0=-68.5 +k=0.999900 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Maine East
2212;26784;+proj=tmerc +lat_0=42.83333333333334 +lon_0=-70.16666666666667 +k=0.999967 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Maine West
2213;26785;+proj=lcc +lat_1=38.3 +lat_2=39.45 +lat_0=37.83333333333334 +lon_0=-77 +x_0=243840.4876809754 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Maryland
2214;26786;+proj=lcc +lat_1=41.71666666666667 +lat_2=42.68333333333333 +lat_0=41 +lon_0=-71.5 +x_0=182880.3657607315 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Massachusetts Mainland
2215;26787;+proj=lcc +lat_1=41.28333333333333 +lat_2=41.48333333333333 +lat_0=41 +lon_0=-70.5 +x_0=60960.12192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Massachusetts Island
2216;26791;+proj=lcc +lat_1=47.03333333333333 +lat_2=48.63333333333333 +lat_0=46.5 +lon_0=-93.09999999999999 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Minnesota North
2217;26792;+proj=lcc +lat_1=45.61666666666667 +lat_2=47.05 +lat_0=45 +lon_0=-94.25 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Minnesota Central
2218;26793;+proj=lcc +lat_1=43.78333333333333 +lat_2=45.21666666666667 +lat_0=43 +lon_0=-94 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Minnesota South
2219;26794;+proj=tmerc +lat_0=29.66666666666667 +lon_0=-88.83333333333333 +k=0.999960 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Mississippi East
2220;26795;+proj=tmerc +lat_0=30.5 +lon_0=-90.33333333333333 +k=0.999941 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Mississippi West
2221;26796;+proj=tmerc +lat_0=35.83333333333334 +lon_0=-90.5 +k=0.999933 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Missouri East
2222;26797;+proj=tmerc +lat_0=35.83333333333334 +lon_0=-92.5 +k=0.999933 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Missouri Central
2223;26798;+proj=tmerc +lat_0=36.16666666666666 +lon_0=-94.5 +k=0.999941 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Missouri West
2224;26799;+proj=lcc +lat_1=34.41666666666666 +lat_2=33.86666666666667 +lat_0=34.13333333333333 +lon_0=-118.3333333333333 +x_0=1276106.450596901 +y_0=1268253.006858014 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / California zone VII
2225;26801;+proj=tmerc +lat_0=41.5 +lon_0=-83.66666666666667 +k=0.999943 +x_0=152400.3048006096 +y_0=0 +a=6378450.047548896 +b=6356826.621488444 +to_meter=0.3048006096012192 +no_defs;NAD Michigan / Michigan East
2226;26802;+proj=tmerc +lat_0=41.5 +lon_0=-85.75 +k=0.999909 +x_0=152400.3048006096 +y_0=0 +a=6378450.047548896 +b=6356826.621488444 +to_meter=0.3048006096012192 +no_defs;NAD Michigan / Michigan Old Central
2227;26803;+proj=tmerc +lat_0=41.5 +lon_0=-88.75 +k=0.999909 +x_0=152400.3048006096 +y_0=0 +a=6378450.047548896 +b=6356826.621488444 +to_meter=0.3048006096012192 +no_defs;NAD Michigan / Michigan West
2228;26811;+proj=lcc +lat_1=45.48333333333333 +lat_2=47.08333333333334 +lat_0=44.78333333333333 +lon_0=-87 +x_0=609601.2192024384 +y_0=0 +a=6378450.047548896 +b=6356826.621488444 +to_meter=0.3048006096012192 +no_defs;NAD Michigan / Michigan North
2229;26812;+proj=lcc +lat_1=44.18333333333333 +lat_2=45.7 +lat_0=43.31666666666667 +lon_0=-84.33333333333333 +x_0=609601.2192024384 +y_0=0 +a=6378450.047548896 +b=6356826.621488444 +to_meter=0.3048006096012192 +no_defs;NAD Michigan / Michigan Central
2230;26813;+proj=lcc +lat_1=42.1 +lat_2=43.66666666666666 +lat_0=41.5 +lon_0=-84.33333333333333 +x_0=609601.2192024384 +y_0=0 +a=6378450.047548896 +b=6356826.621488444 +to_meter=0.3048006096012192 +no_defs;NAD Michigan / Michigan South
2231;26901;+proj=utm +zone=1 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / UTM zone 1N
2232;26902;+proj=utm +zone=2 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / UTM zone 2N
2233;26903;+proj=utm +zone=3 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / UTM zone 3N
2234;26904;+proj=utm +zone=4 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / UTM zone 4N
2235;26905;+proj=utm +zone=5 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / UTM zone 5N
2236;26906;+proj=utm +zone=6 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / UTM zone 6N
2237;26907;+proj=utm +zone=7 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / UTM zone 7N
2238;26908;+proj=utm +zone=8 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / UTM zone 8N
2239;26909;+proj=utm +zone=9 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / UTM zone 9N
2240;26910;+proj=utm +zone=10 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / UTM zone 10N
2241;26911;+proj=utm +zone=11 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / UTM zone 11N
2242;26912;+proj=utm +zone=12 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / UTM zone 12N
2243;26913;+proj=utm +zone=13 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / UTM zone 13N
2244;26914;+proj=utm +zone=14 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / UTM zone 14N
2245;26915;+proj=utm +zone=15 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / UTM zone 15N
2246;26916;+proj=utm +zone=16 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / UTM zone 16N
2247;26917;+proj=utm +zone=17 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / UTM zone 17N
2248;26918;+proj=utm +zone=18 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / UTM zone 18N
2249;26919;+proj=utm +zone=19 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / UTM zone 19N
2250;26920;+proj=utm +zone=20 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / UTM zone 20N
2251;26921;+proj=utm +zone=21 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / UTM zone 21N
2252;26922;+proj=utm +zone=22 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / UTM zone 22N
2253;26923;+proj=utm +zone=23 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / UTM zone 23N
2254;26929;+proj=tmerc +lat_0=30.5 +lon_0=-85.83333333333333 +k=0.999960 +x_0=200000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Alabama East
2255;26930;+proj=tmerc +lat_0=30 +lon_0=-87.5 +k=0.999933 +x_0=600000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Alabama West
2256;26931;+proj=omerc +lat_0=57 +lonc=-133.6666666666667 +alpha=323.1301023611111 +k=0.9999 +x_0=5000000 +y_0=-5000000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Alaska zone 1
2257;26932;+proj=tmerc +lat_0=54 +lon_0=-142 +k=0.999900 +x_0=500000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Alaska zone 2
2258;26933;+proj=tmerc +lat_0=54 +lon_0=-146 +k=0.999900 +x_0=500000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Alaska zone 3
2259;26934;+proj=tmerc +lat_0=54 +lon_0=-150 +k=0.999900 +x_0=500000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Alaska zone 4
2260;26935;+proj=tmerc +lat_0=54 +lon_0=-154 +k=0.999900 +x_0=500000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Alaska zone 5
2261;26936;+proj=tmerc +lat_0=54 +lon_0=-158 +k=0.999900 +x_0=500000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Alaska zone 6
2262;26937;+proj=tmerc +lat_0=54 +lon_0=-162 +k=0.999900 +x_0=500000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Alaska zone 7
2263;26938;+proj=tmerc +lat_0=54 +lon_0=-166 +k=0.999900 +x_0=500000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Alaska zone 8
2264;26939;+proj=tmerc +lat_0=54 +lon_0=-170 +k=0.999900 +x_0=500000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Alaska zone 9
2265;26940;+proj=lcc +lat_1=53.83333333333334 +lat_2=51.83333333333334 +lat_0=51 +lon_0=-176 +x_0=1000000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Alaska zone 10
2266;26941;+proj=lcc +lat_1=41.66666666666666 +lat_2=40 +lat_0=39.33333333333334 +lon_0=-122 +x_0=2000000 +y_0=500000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / California zone 1
2267;26942;+proj=lcc +lat_1=39.83333333333334 +lat_2=38.33333333333334 +lat_0=37.66666666666666 +lon_0=-122 +x_0=2000000 +y_0=500000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / California zone 2
2268;26943;+proj=lcc +lat_1=38.43333333333333 +lat_2=37.06666666666667 +lat_0=36.5 +lon_0=-120.5 +x_0=2000000 +y_0=500000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / California zone 3
2269;26944;+proj=lcc +lat_1=37.25 +lat_2=36 +lat_0=35.33333333333334 +lon_0=-119 +x_0=2000000 +y_0=500000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / California zone 4
2270;26945;+proj=lcc +lat_1=35.46666666666667 +lat_2=34.03333333333333 +lat_0=33.5 +lon_0=-118 +x_0=2000000 +y_0=500000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / California zone 5
2271;26946;+proj=lcc +lat_1=33.88333333333333 +lat_2=32.78333333333333 +lat_0=32.16666666666666 +lon_0=-116.25 +x_0=2000000 +y_0=500000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / California zone 6
2272;26948;+proj=tmerc +lat_0=31 +lon_0=-110.1666666666667 +k=0.999900 +x_0=213360 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Arizona East
2273;26949;+proj=tmerc +lat_0=31 +lon_0=-111.9166666666667 +k=0.999900 +x_0=213360 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Arizona Central
2274;26950;+proj=tmerc +lat_0=31 +lon_0=-113.75 +k=0.999933 +x_0=213360 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Arizona West
2275;26951;+proj=lcc +lat_1=36.23333333333333 +lat_2=34.93333333333333 +lat_0=34.33333333333334 +lon_0=-92 +x_0=400000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Arkansas North
2276;26952;+proj=lcc +lat_1=34.76666666666667 +lat_2=33.3 +lat_0=32.66666666666666 +lon_0=-92 +x_0=400000 +y_0=400000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Arkansas South
2277;26953;+proj=lcc +lat_1=40.78333333333333 +lat_2=39.71666666666667 +lat_0=39.33333333333334 +lon_0=-105.5 +x_0=914401.8289 +y_0=304800.6096 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Colorado North
2278;26954;+proj=lcc +lat_1=39.75 +lat_2=38.45 +lat_0=37.83333333333334 +lon_0=-105.5 +x_0=914401.8289 +y_0=304800.6096 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Colorado Central
2279;26955;+proj=lcc +lat_1=38.43333333333333 +lat_2=37.23333333333333 +lat_0=36.66666666666666 +lon_0=-105.5 +x_0=914401.8289 +y_0=304800.6096 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Colorado South
2280;26956;+proj=lcc +lat_1=41.86666666666667 +lat_2=41.2 +lat_0=40.83333333333334 +lon_0=-72.75 +x_0=304800.6096 +y_0=152400.3048 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Connecticut
2281;26957;+proj=tmerc +lat_0=38 +lon_0=-75.41666666666667 +k=0.999995 +x_0=200000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Delaware
2282;26958;+proj=tmerc +lat_0=24.33333333333333 +lon_0=-81 +k=0.999941 +x_0=200000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Florida East
2283;26959;+proj=tmerc +lat_0=24.33333333333333 +lon_0=-82 +k=0.999941 +x_0=200000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Florida West
2284;26960;+proj=lcc +lat_1=30.75 +lat_2=29.58333333333333 +lat_0=29 +lon_0=-84.5 +x_0=600000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Florida North
2285;26961;+proj=tmerc +lat_0=18.83333333333333 +lon_0=-155.5 +k=0.999967 +x_0=500000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Hawaii zone 1
2286;26962;+proj=tmerc +lat_0=20.33333333333333 +lon_0=-156.6666666666667 +k=0.999967 +x_0=500000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Hawaii zone 2
2287;26963;+proj=tmerc +lat_0=21.16666666666667 +lon_0=-158 +k=0.999990 +x_0=500000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Hawaii zone 3
2288;26964;+proj=tmerc +lat_0=21.83333333333333 +lon_0=-159.5 +k=0.999990 +x_0=500000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Hawaii zone 4
2289;26965;+proj=tmerc +lat_0=21.66666666666667 +lon_0=-160.1666666666667 +k=1.000000 +x_0=500000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Hawaii zone 5
2290;26966;+proj=tmerc +lat_0=30 +lon_0=-82.16666666666667 +k=0.999900 +x_0=200000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Georgia East
2291;26967;+proj=tmerc +lat_0=30 +lon_0=-84.16666666666667 +k=0.999900 +x_0=700000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Georgia West
2292;26968;+proj=tmerc +lat_0=41.66666666666666 +lon_0=-112.1666666666667 +k=0.999947 +x_0=200000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Idaho East
2293;26969;+proj=tmerc +lat_0=41.66666666666666 +lon_0=-114 +k=0.999947 +x_0=500000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Idaho Central
2294;26970;+proj=tmerc +lat_0=41.66666666666666 +lon_0=-115.75 +k=0.999933 +x_0=800000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Idaho West
2295;26971;+proj=tmerc +lat_0=36.66666666666666 +lon_0=-88.33333333333333 +k=0.999975 +x_0=300000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Illinois East
2296;26972;+proj=tmerc +lat_0=36.66666666666666 +lon_0=-90.16666666666667 +k=0.999941 +x_0=700000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Illinois West
2297;26973;+proj=tmerc +lat_0=37.5 +lon_0=-85.66666666666667 +k=0.999967 +x_0=100000 +y_0=250000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Indiana East
2298;26974;+proj=tmerc +lat_0=37.5 +lon_0=-87.08333333333333 +k=0.999967 +x_0=900000 +y_0=250000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Indiana West
2299;26975;+proj=lcc +lat_1=43.26666666666667 +lat_2=42.06666666666667 +lat_0=41.5 +lon_0=-93.5 +x_0=1500000 +y_0=1000000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Iowa North
2300;26976;+proj=lcc +lat_1=41.78333333333333 +lat_2=40.61666666666667 +lat_0=40 +lon_0=-93.5 +x_0=500000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Iowa South
2301;26977;+proj=lcc +lat_1=39.78333333333333 +lat_2=38.71666666666667 +lat_0=38.33333333333334 +lon_0=-98 +x_0=400000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Kansas North
2302;26978;+proj=lcc +lat_1=38.56666666666667 +lat_2=37.26666666666667 +lat_0=36.66666666666666 +lon_0=-98.5 +x_0=400000 +y_0=400000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Kansas South
2303;26979;+proj=lcc +lat_1=37.96666666666667 +lat_2=37.96666666666667 +lat_0=37.5 +lon_0=-84.25 +x_0=500000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Kentucky North (deprecated)
2304;26980;+proj=lcc +lat_1=37.93333333333333 +lat_2=36.73333333333333 +lat_0=36.33333333333334 +lon_0=-85.75 +x_0=500000 +y_0=500000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Kentucky South
2305;26981;+proj=lcc +lat_1=32.66666666666666 +lat_2=31.16666666666667 +lat_0=30.5 +lon_0=-92.5 +x_0=1000000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Louisiana North
2306;26982;+proj=lcc +lat_1=30.7 +lat_2=29.3 +lat_0=28.5 +lon_0=-91.33333333333333 +x_0=1000000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Louisiana South
2307;26983;+proj=tmerc +lat_0=43.66666666666666 +lon_0=-68.5 +k=0.999900 +x_0=300000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Maine East
2308;26984;+proj=tmerc +lat_0=42.83333333333334 +lon_0=-70.16666666666667 +k=0.999967 +x_0=900000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Maine West
2309;26985;+proj=lcc +lat_1=39.45 +lat_2=38.3 +lat_0=37.66666666666666 +lon_0=-77 +x_0=400000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Maryland
2310;26986;+proj=lcc +lat_1=42.68333333333333 +lat_2=41.71666666666667 +lat_0=41 +lon_0=-71.5 +x_0=200000 +y_0=750000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Massachusetts Mainland
2311;26987;+proj=lcc +lat_1=41.48333333333333 +lat_2=41.28333333333333 +lat_0=41 +lon_0=-70.5 +x_0=500000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Massachusetts Island
2312;26988;+proj=lcc +lat_1=47.08333333333334 +lat_2=45.48333333333333 +lat_0=44.78333333333333 +lon_0=-87 +x_0=8000000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Michigan North
2313;26989;+proj=lcc +lat_1=45.7 +lat_2=44.18333333333333 +lat_0=43.31666666666667 +lon_0=-84.36666666666666 +x_0=6000000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Michigan Central
2314;26990;+proj=lcc +lat_1=43.66666666666666 +lat_2=42.1 +lat_0=41.5 +lon_0=-84.36666666666666 +x_0=4000000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Michigan South
2315;26991;+proj=lcc +lat_1=48.63333333333333 +lat_2=47.03333333333333 +lat_0=46.5 +lon_0=-93.09999999999999 +x_0=800000 +y_0=100000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Minnesota North
2316;26992;+proj=lcc +lat_1=47.05 +lat_2=45.61666666666667 +lat_0=45 +lon_0=-94.25 +x_0=800000 +y_0=100000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Minnesota Central
2317;26993;+proj=lcc +lat_1=45.21666666666667 +lat_2=43.78333333333333 +lat_0=43 +lon_0=-94 +x_0=800000 +y_0=100000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Minnesota South
2318;26994;+proj=tmerc +lat_0=29.5 +lon_0=-88.83333333333333 +k=0.999950 +x_0=300000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Mississippi East
2319;26995;+proj=tmerc +lat_0=29.5 +lon_0=-90.33333333333333 +k=0.999950 +x_0=700000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Mississippi West
2320;26996;+proj=tmerc +lat_0=35.83333333333334 +lon_0=-90.5 +k=0.999933 +x_0=250000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Missouri East
2321;26997;+proj=tmerc +lat_0=35.83333333333334 +lon_0=-92.5 +k=0.999933 +x_0=500000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Missouri Central
2322;26998;+proj=tmerc +lat_0=36.16666666666666 +lon_0=-94.5 +k=0.999941 +x_0=850000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Missouri West
2323;27037;+proj=utm +zone=37 +ellps=clrk80 +units=m +no_defs;Nahrwan 1967 / UTM zone 37N
2324;27038;+proj=utm +zone=38 +ellps=clrk80 +units=m +no_defs;Nahrwan 1967 / UTM zone 38N
2325;27039;+proj=utm +zone=39 +ellps=clrk80 +units=m +no_defs;Nahrwan 1967 / UTM zone 39N
2326;27040;+proj=utm +zone=40 +ellps=clrk80 +units=m +no_defs;Nahrwan 1967 / UTM zone 40N
2327;27120;+proj=utm +zone=20 +ellps=intl +units=m +no_defs;Naparima 1972 / UTM zone 20N
2328;27200;+proj=nzmg +lat_0=-41 +lon_0=173 +x_0=2510000 +y_0=6023150 +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / New Zealand Map Grid
2329;27205;+proj=tmerc +lat_0=-36.87986527777778 +lon_0=174.7643393611111 +k=0.999900 +x_0=300000 +y_0=700000 +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / Mount Eden Circuit
2330;27206;+proj=tmerc +lat_0=-37.76124980555556 +lon_0=176.46619725 +k=1.000000 +x_0=300000 +y_0=700000 +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / Bay of Plenty Circuit
2331;27207;+proj=tmerc +lat_0=-38.62470277777778 +lon_0=177.8856362777778 +k=1.000000 +x_0=300000 +y_0=700000 +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / Poverty Bay Circuit
2332;27208;+proj=tmerc +lat_0=-39.65092930555556 +lon_0=176.6736805277778 +k=1.000000 +x_0=300000 +y_0=700000 +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / Hawkes Bay Circuit
2333;27209;+proj=tmerc +lat_0=-39.13575830555556 +lon_0=174.22801175 +k=1.000000 +x_0=300000 +y_0=700000 +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / Taranaki Circuit
2334;27210;+proj=tmerc +lat_0=-39.51247038888889 +lon_0=175.6400368055556 +k=1.000000 +x_0=300000 +y_0=700000 +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / Tuhirangi Circuit
2335;27211;+proj=tmerc +lat_0=-40.24194713888889 +lon_0=175.4880996111111 +k=1.000000 +x_0=300000 +y_0=700000 +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / Wanganui Circuit
2336;27212;+proj=tmerc +lat_0=-40.92553263888889 +lon_0=175.6473496666667 +k=1.000000 +x_0=300000 +y_0=700000 +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / Wairarapa Circuit
2337;27213;+proj=tmerc +lat_0=-41.30131963888888 +lon_0=174.7766231111111 +k=1.000000 +x_0=300000 +y_0=700000 +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / Wellington Circuit
2338;27214;+proj=tmerc +lat_0=-40.71475905555556 +lon_0=172.6720465 +k=1.000000 +x_0=300000 +y_0=700000 +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / Collingwood Circuit
2339;27215;+proj=tmerc +lat_0=-41.27454472222222 +lon_0=173.2993168055555 +k=1.000000 +x_0=300000 +y_0=700000 +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / Nelson Circuit
2340;27216;+proj=tmerc +lat_0=-41.28991152777778 +lon_0=172.1090281944444 +k=1.000000 +x_0=300000 +y_0=700000 +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / Karamea Circuit
2341;27217;+proj=tmerc +lat_0=-41.81080286111111 +lon_0=171.5812600555556 +k=1.000000 +x_0=300000 +y_0=700000 +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / Buller Circuit
2342;27218;+proj=tmerc +lat_0=-42.33369427777778 +lon_0=171.5497713055556 +k=1.000000 +x_0=300000 +y_0=700000 +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / Grey Circuit
2343;27219;+proj=tmerc +lat_0=-42.68911658333333 +lon_0=173.0101333888889 +k=1.000000 +x_0=300000 +y_0=700000 +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / Amuri Circuit
2344;27220;+proj=tmerc +lat_0=-41.54448666666666 +lon_0=173.8020741111111 +k=1.000000 +x_0=300000 +y_0=700000 +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / Marlborough Circuit
2345;27221;+proj=tmerc +lat_0=-42.88632236111111 +lon_0=170.9799935 +k=1.000000 +x_0=300000 +y_0=700000 +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / Hokitika Circuit
2346;27222;+proj=tmerc +lat_0=-43.11012813888889 +lon_0=170.2609258333333 +k=1.000000 +x_0=300000 +y_0=700000 +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / Okarito Circuit
2347;27223;+proj=tmerc +lat_0=-43.97780288888889 +lon_0=168.606267 +k=1.000000 +x_0=300000 +y_0=700000 +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / Jacksons Bay Circuit
2348;27224;+proj=tmerc +lat_0=-43.59063758333333 +lon_0=172.7271935833333 +k=1.000000 +x_0=300000 +y_0=700000 +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / Mount Pleasant Circuit
2349;27225;+proj=tmerc +lat_0=-43.74871155555556 +lon_0=171.3607484722222 +k=1.000000 +x_0=300000 +y_0=700000 +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / Gawler Circuit
2350;27226;+proj=tmerc +lat_0=-44.40222036111111 +lon_0=171.0572508333333 +k=1.000000 +x_0=300000 +y_0=700000 +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / Timaru Circuit
2351;27227;+proj=tmerc +lat_0=-44.73526797222222 +lon_0=169.4677550833333 +k=1.000000 +x_0=300000 +y_0=700000 +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / Lindis Peak Circuit
2352;27228;+proj=tmerc +lat_0=-45.13290258333333 +lon_0=168.3986411944444 +k=1.000000 +x_0=300000 +y_0=700000 +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / Mount Nicholas Circuit
2353;27229;+proj=tmerc +lat_0=-45.56372616666666 +lon_0=167.7388617777778 +k=1.000000 +x_0=300000 +y_0=700000 +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / Mount York Circuit
2354;27230;+proj=tmerc +lat_0=-45.81619661111111 +lon_0=170.6285951666667 +k=1.000000 +x_0=300000 +y_0=700000 +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / Observation Point Circuit
2355;27231;+proj=tmerc +lat_0=-45.86151336111111 +lon_0=170.2825891111111 +k=0.999960 +x_0=300000 +y_0=700000 +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / North Taieri Circuit
2356;27232;+proj=tmerc +lat_0=-46.60000961111111 +lon_0=168.342872 +k=1.000000 +x_0=300002.66 +y_0=699999.58 +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / Bluff Circuit
2357;27258;+proj=utm +zone=58 +south +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / UTM zone 58S
2358;27259;+proj=utm +zone=59 +south +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / UTM zone 59S
2359;27260;+proj=utm +zone=60 +south +ellps=intl +datum=nzgd49 +units=m +no_defs;NZGD49 / UTM zone 60S
2360;27291;+proj=tmerc +lat_0=-39 +lon_0=175.5 +k=1.000000 +x_0=274319.5243848086 +y_0=365759.3658464114 +ellps=intl +datum=nzgd49 +to_meter=0.9143984146160287 +no_defs;NZGD49 / North Island Grid
2361;27292;+proj=tmerc +lat_0=-44 +lon_0=171.5 +k=1.000000 +x_0=457199.2073080143 +y_0=457199.2073080143 +ellps=intl +datum=nzgd49 +to_meter=0.9143984146160287 +no_defs;NZGD49 / South Island Grid
2362;27391;+proj=tmerc +lat_0=58 +lon_0=-4.666666666666667 +k=1.000000 +x_0=0 +y_0=0 +a=6377492.018 +b=6356173.508712696 +towgs84=278.3,93,474.5,7.889,0.05,-6.61,6.21 +pm=oslo +units=m +no_defs;NGO 1948 (Oslo) / NGO zone I
2363;27392;+proj=tmerc +lat_0=58 +lon_0=-2.333333333333333 +k=1.000000 +x_0=0 +y_0=0 +a=6377492.018 +b=6356173.508712696 +towgs84=278.3,93,474.5,7.889,0.05,-6.61,6.21 +pm=oslo +units=m +no_defs;NGO 1948 (Oslo) / NGO zone II
2364;27393;+proj=tmerc +lat_0=58 +lon_0=0 +k=1.000000 +x_0=0 +y_0=0 +a=6377492.018 +b=6356173.508712696 +towgs84=278.3,93,474.5,7.889,0.05,-6.61,6.21 +pm=oslo +units=m +no_defs;NGO 1948 (Oslo) / NGO zone III
2365;27394;+proj=tmerc +lat_0=58 +lon_0=2.5 +k=1.000000 +x_0=0 +y_0=0 +a=6377492.018 +b=6356173.508712696 +towgs84=278.3,93,474.5,7.889,0.05,-6.61,6.21 +pm=oslo +units=m +no_defs;NGO 1948 (Oslo) / NGO zone IV
2366;27395;+proj=tmerc +lat_0=58 +lon_0=6.166666666666667 +k=1.000000 +x_0=0 +y_0=0 +a=6377492.018 +b=6356173.508712696 +towgs84=278.3,93,474.5,7.889,0.05,-6.61,6.21 +pm=oslo +units=m +no_defs;NGO 1948 (Oslo) / NGO zone V
2367;27396;+proj=tmerc +lat_0=58 +lon_0=10.16666666666667 +k=1.000000 +x_0=0 +y_0=0 +a=6377492.018 +b=6356173.508712696 +towgs84=278.3,93,474.5,7.889,0.05,-6.61,6.21 +pm=oslo +units=m +no_defs;NGO 1948 (Oslo) / NGO zone VI
2368;27397;+proj=tmerc +lat_0=58 +lon_0=14.16666666666667 +k=1.000000 +x_0=0 +y_0=0 +a=6377492.018 +b=6356173.508712696 +towgs84=278.3,93,474.5,7.889,0.05,-6.61,6.21 +pm=oslo +units=m +no_defs;NGO 1948 (Oslo) / NGO zone VII
2369;27398;+proj=tmerc +lat_0=58 +lon_0=18.33333333333333 +k=1.000000 +x_0=0 +y_0=0 +a=6377492.018 +b=6356173.508712696 +towgs84=278.3,93,474.5,7.889,0.05,-6.61,6.21 +pm=oslo +units=m +no_defs;NGO 1948 (Oslo) / NGO zone VIII
2370;27429;+proj=utm +zone=29 +ellps=intl +units=m +no_defs;Datum 73 / UTM zone 29N
2371;27492;+proj=tmerc +lat_0=39.66666666666666 +lon_0=-8.131906111111112 +k=1.000000 +x_0=180.598 +y_0=-86.98999999999999 +ellps=intl +units=m +no_defs;Datum 73 / Modified Portuguese Grid
2372;27500;+proj=lcc +lat_1=49.50000000000001 +lat_0=49.50000000000001 +lon_0=5.4 +k_0=0.99950908 +x_0=500000 +y_0=300000 +a=6376523 +b=6355862.933255573 +pm=paris +units=m +no_defs;ATF (Paris) / Nord de Guerre
2373;27561;+proj=lcc +lat_1=49.50000000000001 +lat_0=49.50000000000001 +lon_0=0 +k_0=0.999877341 +x_0=600000 +y_0=200000 +a=6378249.2 +b=6356515 +towgs84=-168,-60,320,0,0,0,0 +pm=paris +units=m +no_defs;NTF (Paris) / Lambert Nord France
2374;27562;+proj=lcc +lat_1=46.8 +lat_0=46.8 +lon_0=0 +k_0=0.99987742 +x_0=600000 +y_0=200000 +a=6378249.2 +b=6356515 +towgs84=-168,-60,320,0,0,0,0 +pm=paris +units=m +no_defs;NTF (Paris) / Lambert Centre France
2375;27563;+proj=lcc +lat_1=44.10000000000001 +lat_0=44.10000000000001 +lon_0=0 +k_0=0.999877499 +x_0=600000 +y_0=200000 +a=6378249.2 +b=6356515 +towgs84=-168,-60,320,0,0,0,0 +pm=paris +units=m +no_defs;NTF (Paris) / Lambert Sud France
2376;27564;+proj=lcc +lat_1=42.16500000000001 +lat_0=42.16500000000001 +lon_0=0 +k_0=0.99994471 +x_0=234.358 +y_0=185861.369 +a=6378249.2 +b=6356515 +towgs84=-168,-60,320,0,0,0,0 +pm=paris +units=m +no_defs;NTF (Paris) / Lambert Corse
2377;27571;+proj=lcc +lat_1=49.50000000000001 +lat_0=49.50000000000001 +lon_0=0 +k_0=0.999877341 +x_0=600000 +y_0=1200000 +a=6378249.2 +b=6356515 +towgs84=-168,-60,320,0,0,0,0 +pm=paris +units=m +no_defs;NTF (Paris) / Lambert zone I
2378;27572;+proj=lcc +lat_1=46.8 +lat_0=46.8 +lon_0=0 +k_0=0.99987742 +x_0=600000 +y_0=2200000 +a=6378249.2 +b=6356515 +towgs84=-168,-60,320,0,0,0,0 +pm=paris +units=m +no_defs;NTF (Paris) / Lambert zone II
2379;27573;+proj=lcc +lat_1=44.10000000000001 +lat_0=44.10000000000001 +lon_0=0 +k_0=0.999877499 +x_0=600000 +y_0=3200000 +a=6378249.2 +b=6356515 +towgs84=-168,-60,320,0,0,0,0 +pm=paris +units=m +no_defs;NTF (Paris) / Lambert zone III
2380;27574;+proj=lcc +lat_1=42.16500000000001 +lat_0=42.16500000000001 +lon_0=0 +k_0=0.99994471 +x_0=234.358 +y_0=4185861.369 +a=6378249.2 +b=6356515 +towgs84=-168,-60,320,0,0,0,0 +pm=paris +units=m +no_defs;NTF (Paris) / Lambert zone IV
2381;27581;+proj=lcc +lat_1=49.50000000000001 +lat_0=49.50000000000001 +lon_0=0 +k_0=0.999877341 +x_0=600000 +y_0=1200000 +a=6378249.2 +b=6356515 +towgs84=-168,-60,320,0,0,0,0 +pm=paris +units=m +no_defs;NTF (Paris) / France I (deprecated)
2382;27582;+proj=lcc +lat_1=46.8 +lat_0=46.8 +lon_0=0 +k_0=0.99987742 +x_0=600000 +y_0=2200000 +a=6378249.2 +b=6356515 +towgs84=-168,-60,320,0,0,0,0 +pm=paris +units=m +no_defs;NTF (Paris) / France II (deprecated)
2383;27583;+proj=lcc +lat_1=44.10000000000001 +lat_0=44.10000000000001 +lon_0=0 +k_0=0.999877499 +x_0=600000 +y_0=3200000 +a=6378249.2 +b=6356515 +towgs84=-168,-60,320,0,0,0,0 +pm=paris +units=m +no_defs;NTF (Paris) / France III (deprecated)
2384;27584;+proj=lcc +lat_1=42.16500000000001 +lat_0=42.16500000000001 +lon_0=0 +k_0=0.99994471 +x_0=234.358 +y_0=4185861.369 +a=6378249.2 +b=6356515 +towgs84=-168,-60,320,0,0,0,0 +pm=paris +units=m +no_defs;NTF (Paris) / France IV (deprecated)
2385;27591;+proj=lcc +lat_1=49.50000000000001 +lat_0=49.50000000000001 +lon_0=0 +k_0=0.999877341 +x_0=600000 +y_0=200000 +a=6378249.2 +b=6356515 +towgs84=-168,-60,320,0,0,0,0 +pm=paris +units=m +no_defs;NTF (Paris) / Nord France (deprecated)
2386;27592;+proj=lcc +lat_1=46.8 +lat_0=46.8 +lon_0=0 +k_0=0.99987742 +x_0=600000 +y_0=200000 +a=6378249.2 +b=6356515 +towgs84=-168,-60,320,0,0,0,0 +pm=paris +units=m +no_defs;NTF (Paris) / Centre France (deprecated)
2387;27593;+proj=lcc +lat_1=44.10000000000001 +lat_0=44.10000000000001 +lon_0=0 +k_0=0.999877499 +x_0=600000 +y_0=200000 +a=6378249.2 +b=6356515 +towgs84=-168,-60,320,0,0,0,0 +pm=paris +units=m +no_defs;NTF (Paris) / Sud France (deprecated)
2388;27594;+proj=lcc +lat_1=42.16500000000001 +lat_0=42.16500000000001 +lon_0=0 +k_0=0.99994471 +x_0=234.358 +y_0=185861.369 +a=6378249.2 +b=6356515 +towgs84=-168,-60,320,0,0,0,0 +pm=paris +units=m +no_defs;NTF (Paris) / Corse (deprecated)
2389;27700;+proj=tmerc +lat_0=49 +lon_0=-2 +k=0.999601 +x_0=400000 +y_0=-100000 +ellps=airy +units=m +no_defs;OSGB 1936 / British National Grid
2390;28191;+proj=cass +lat_0=31.73409694444445 +lon_0=35.21208055555556 +x_0=170251.555 +y_0=126867.909 +a=6378300.789 +b=6356566.435 +towgs84=-275.722,94.7824,340.894,-8.001,-4.42,-11.821,1 +units=m +no_defs;Palestine 1923 / Palestine Grid
2391;28192;+proj=tmerc +lat_0=31.73409694444445 +lon_0=35.21208055555556 +k=1.000000 +x_0=170251.555 +y_0=1126867.909 +a=6378300.789 +b=6356566.435 +towgs84=-275.722,94.7824,340.894,-8.001,-4.42,-11.821,1 +units=m +no_defs;Palestine 1923 / Palestine Belt
2392;28193;+proj=cass +lat_0=31.73409694444445 +lon_0=35.21208055555556 +x_0=170251.555 +y_0=1126867.909 +a=6378300.789 +b=6356566.435 +towgs84=-275.722,94.7824,340.894,-8.001,-4.42,-11.821,1 +units=m +no_defs;Palestine 1923 / Israeli CS Grid
2393;28232;+proj=utm +zone=32 +south +a=6378249.2 +b=6356515 +units=m +no_defs;Pointe Noire / UTM zone 32S
2394;28348;+proj=utm +zone=48 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;GDA94 / MGA zone 48
2395;28349;+proj=utm +zone=49 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;GDA94 / MGA zone 49
2396;28350;+proj=utm +zone=50 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;GDA94 / MGA zone 50
2397;28351;+proj=utm +zone=51 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;GDA94 / MGA zone 51
2398;28352;+proj=utm +zone=52 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;GDA94 / MGA zone 52
2399;28353;+proj=utm +zone=53 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;GDA94 / MGA zone 53
2400;28354;+proj=utm +zone=54 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;GDA94 / MGA zone 54
2401;28355;+proj=utm +zone=55 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;GDA94 / MGA zone 55
2402;28356;+proj=utm +zone=56 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;GDA94 / MGA zone 56
2403;28357;+proj=utm +zone=57 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;GDA94 / MGA zone 57
2404;28358;+proj=utm +zone=58 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;GDA94 / MGA zone 58
2405;28402;+proj=tmerc +lat_0=0 +lon_0=9 +k=1.000000 +x_0=2500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 2
2406;28403;+proj=tmerc +lat_0=0 +lon_0=15 +k=1.000000 +x_0=3500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 3
2407;28404;+proj=tmerc +lat_0=0 +lon_0=21 +k=1.000000 +x_0=4500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 4
2408;28405;+proj=tmerc +lat_0=0 +lon_0=27 +k=1.000000 +x_0=5500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 5
2409;28406;+proj=tmerc +lat_0=0 +lon_0=33 +k=1.000000 +x_0=6500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 6
2410;28407;+proj=tmerc +lat_0=0 +lon_0=39 +k=1.000000 +x_0=7500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 7
2411;28408;+proj=tmerc +lat_0=0 +lon_0=45 +k=1.000000 +x_0=8500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 8
2412;28409;+proj=tmerc +lat_0=0 +lon_0=51 +k=1.000000 +x_0=9500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 9
2413;28410;+proj=tmerc +lat_0=0 +lon_0=57 +k=1.000000 +x_0=10500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 10
2414;28411;+proj=tmerc +lat_0=0 +lon_0=63 +k=1.000000 +x_0=11500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 11
2415;28412;+proj=tmerc +lat_0=0 +lon_0=69 +k=1.000000 +x_0=12500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 12
2416;28413;+proj=tmerc +lat_0=0 +lon_0=75 +k=1.000000 +x_0=13500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 13
2417;28414;+proj=tmerc +lat_0=0 +lon_0=81 +k=1.000000 +x_0=14500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 14
2418;28415;+proj=tmerc +lat_0=0 +lon_0=87 +k=1.000000 +x_0=15500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 15
2419;28416;+proj=tmerc +lat_0=0 +lon_0=93 +k=1.000000 +x_0=16500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 16
2420;28417;+proj=tmerc +lat_0=0 +lon_0=99 +k=1.000000 +x_0=17500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 17
2421;28418;+proj=tmerc +lat_0=0 +lon_0=105 +k=1.000000 +x_0=18500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 18
2422;28419;+proj=tmerc +lat_0=0 +lon_0=111 +k=1.000000 +x_0=19500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 19
2423;28420;+proj=tmerc +lat_0=0 +lon_0=117 +k=1.000000 +x_0=20500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 20
2424;28421;+proj=tmerc +lat_0=0 +lon_0=123 +k=1.000000 +x_0=21500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 21
2425;28422;+proj=tmerc +lat_0=0 +lon_0=129 +k=1.000000 +x_0=22500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 22
2426;28423;+proj=tmerc +lat_0=0 +lon_0=135 +k=1.000000 +x_0=23500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 23
2427;28424;+proj=tmerc +lat_0=0 +lon_0=141 +k=1.000000 +x_0=24500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 24
2428;28425;+proj=tmerc +lat_0=0 +lon_0=147 +k=1.000000 +x_0=25500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 25
2429;28426;+proj=tmerc +lat_0=0 +lon_0=153 +k=1.000000 +x_0=26500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 26
2430;28427;+proj=tmerc +lat_0=0 +lon_0=159 +k=1.000000 +x_0=27500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 27
2431;28428;+proj=tmerc +lat_0=0 +lon_0=165 +k=1.000000 +x_0=28500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 28
2432;28429;+proj=tmerc +lat_0=0 +lon_0=171 +k=1.000000 +x_0=29500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 29
2433;28430;+proj=tmerc +lat_0=0 +lon_0=177 +k=1.000000 +x_0=30500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 30
2434;28431;+proj=tmerc +lat_0=0 +lon_0=-177 +k=1.000000 +x_0=31500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 31
2435;28432;+proj=tmerc +lat_0=0 +lon_0=-171 +k=1.000000 +x_0=32500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger zone 32
2436;28462;+proj=tmerc +lat_0=0 +lon_0=9 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 2N (deprecated)
2437;28463;+proj=tmerc +lat_0=0 +lon_0=15 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 3N (deprecated)
2438;28464;+proj=tmerc +lat_0=0 +lon_0=21 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 4N (deprecated)
2439;28465;+proj=tmerc +lat_0=0 +lon_0=27 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 5N (deprecated)
2440;28466;+proj=tmerc +lat_0=0 +lon_0=33 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 6N (deprecated)
2441;28467;+proj=tmerc +lat_0=0 +lon_0=39 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 7N (deprecated)
2442;28468;+proj=tmerc +lat_0=0 +lon_0=45 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 8N (deprecated)
2443;28469;+proj=tmerc +lat_0=0 +lon_0=51 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 9N (deprecated)
2444;28470;+proj=tmerc +lat_0=0 +lon_0=57 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 10N (deprecated)
2445;28471;+proj=tmerc +lat_0=0 +lon_0=63 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 11N (deprecated)
2446;28472;+proj=tmerc +lat_0=0 +lon_0=69 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 12N (deprecated)
2447;28473;+proj=tmerc +lat_0=0 +lon_0=75 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 13N (deprecated)
2448;28474;+proj=tmerc +lat_0=0 +lon_0=81 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 14N (deprecated)
2449;28475;+proj=tmerc +lat_0=0 +lon_0=87 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 15N (deprecated)
2450;28476;+proj=tmerc +lat_0=0 +lon_0=93 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 16N (deprecated)
2451;28477;+proj=tmerc +lat_0=0 +lon_0=99 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 17N (deprecated)
2452;28478;+proj=tmerc +lat_0=0 +lon_0=105 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 18N (deprecated)
2453;28479;+proj=tmerc +lat_0=0 +lon_0=111 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 19N (deprecated)
2454;28480;+proj=tmerc +lat_0=0 +lon_0=117 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 20N (deprecated)
2455;28481;+proj=tmerc +lat_0=0 +lon_0=123 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 21N (deprecated)
2456;28482;+proj=tmerc +lat_0=0 +lon_0=129 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 22N (deprecated)
2457;28483;+proj=tmerc +lat_0=0 +lon_0=135 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 23N (deprecated)
2458;28484;+proj=tmerc +lat_0=0 +lon_0=141 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 24N (deprecated)
2459;28485;+proj=tmerc +lat_0=0 +lon_0=147 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 25N (deprecated)
2460;28486;+proj=tmerc +lat_0=0 +lon_0=153 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 26N (deprecated)
2461;28487;+proj=tmerc +lat_0=0 +lon_0=159 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 27N (deprecated)
2462;28488;+proj=tmerc +lat_0=0 +lon_0=165 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 28N (deprecated)
2463;28489;+proj=tmerc +lat_0=0 +lon_0=171 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 29N (deprecated)
2464;28490;+proj=tmerc +lat_0=0 +lon_0=177 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 30N (deprecated)
2465;28491;+proj=tmerc +lat_0=0 +lon_0=-177 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 31N (deprecated)
2466;28492;+proj=tmerc +lat_0=0 +lon_0=-171 +k=1.000000 +x_0=500000 +y_0=0 +ellps=krass +units=m +no_defs;Pulkovo 1942 / Gauss-Kruger 32N (deprecated)
2467;28600;+proj=tmerc +lat_0=24.45 +lon_0=51.21666666666667 +k=0.999990 +x_0=200000 +y_0=300000 +ellps=intl +units=m +no_defs;Qatar 1974 / Qatar National Grid
2468;28991;+proj=sterea +lat_0=52.15616055555555 +lon_0=5.38763888888889 +k=0.999908 +x_0=0 +y_0=0 +ellps=bessel +towgs84=565.237,50.0087,465.658,-0.406857,0.350733,-1.87035,4.0812 +units=m +no_defs;Amersfoort / RD Old
2469;28992;+proj=sterea +lat_0=52.15616055555555 +lon_0=5.38763888888889 +k=0.999908 +x_0=155000 +y_0=463000 +ellps=bessel +towgs84=565.237,50.0087,465.658,-0.406857,0.350733,-1.87035,4.0812 +units=m +no_defs;Amersfoort / RD New
2470;29100;+proj=poly +lat_0=0 +lon_0=-54 +x_0=5000000 +y_0=10000000 +ellps=GRS67 +units=m +no_defs;SAD69 / Brazil Polyconic (deprecated)
2471;29101;+proj=poly +lat_0=0 +lon_0=-54 +x_0=5000000 +y_0=10000000 +ellps=aust_SA +units=m +no_defs;SAD69 / Brazil Polyconic
2472;29118;+proj=utm +zone=18 +ellps=GRS67 +units=m +no_defs;SAD69 / UTM zone 18N (deprecated)
2473;29119;+proj=utm +zone=19 +ellps=GRS67 +units=m +no_defs;SAD69 / UTM zone 19N (deprecated)
2474;29120;+proj=utm +zone=20 +ellps=GRS67 +units=m +no_defs;SAD69 / UTM zone 20N (deprecated)
2475;29121;+proj=utm +zone=21 +ellps=GRS67 +units=m +no_defs;SAD69 / UTM zone 21N (deprecated)
2476;29122;+proj=utm +zone=22 +ellps=GRS67 +units=m +no_defs;SAD69 / UTM zone 22N (deprecated)
2477;29168;+proj=utm +zone=18 +ellps=aust_SA +units=m +no_defs;SAD69 / UTM zone 18N
2478;29169;+proj=utm +zone=19 +ellps=aust_SA +units=m +no_defs;SAD69 / UTM zone 19N
2479;29170;+proj=utm +zone=20 +ellps=aust_SA +units=m +no_defs;SAD69 / UTM zone 20N
2480;29171;+proj=utm +zone=21 +ellps=aust_SA +units=m +no_defs;SAD69 / UTM zone 21N
2481;29172;+proj=utm +zone=22 +ellps=aust_SA +units=m +no_defs;SAD69 / UTM zone 22N
2482;29177;+proj=utm +zone=17 +south +ellps=GRS67 +units=m +no_defs;SAD69 / UTM zone 17S (deprecated)
2483;29178;+proj=utm +zone=18 +south +ellps=GRS67 +units=m +no_defs;SAD69 / UTM zone 18S (deprecated)
2484;29179;+proj=utm +zone=19 +south +ellps=GRS67 +units=m +no_defs;SAD69 / UTM zone 19S (deprecated)
2485;29180;+proj=utm +zone=20 +south +ellps=GRS67 +units=m +no_defs;SAD69 / UTM zone 20S (deprecated)
2486;29181;+proj=utm +zone=21 +south +ellps=GRS67 +units=m +no_defs;SAD69 / UTM zone 21S (deprecated)
2487;29182;+proj=utm +zone=22 +south +ellps=GRS67 +units=m +no_defs;SAD69 / UTM zone 22S (deprecated)
2488;29183;+proj=utm +zone=23 +south +ellps=GRS67 +units=m +no_defs;SAD69 / UTM zone 23S (deprecated)
2489;29184;+proj=utm +zone=24 +south +ellps=GRS67 +units=m +no_defs;SAD69 / UTM zone 24S (deprecated)
2490;29185;+proj=utm +zone=25 +south +ellps=GRS67 +units=m +no_defs;SAD69 / UTM zone 25S (deprecated)
2491;29187;+proj=utm +zone=17 +south +ellps=aust_SA +units=m +no_defs;SAD69 / UTM zone 17S
2492;29188;+proj=utm +zone=18 +south +ellps=aust_SA +units=m +no_defs;SAD69 / UTM zone 18S
2493;29189;+proj=utm +zone=19 +south +ellps=aust_SA +units=m +no_defs;SAD69 / UTM zone 19S
2494;29190;+proj=utm +zone=20 +south +ellps=aust_SA +units=m +no_defs;SAD69 / UTM zone 20S
2495;29191;+proj=utm +zone=21 +south +ellps=aust_SA +units=m +no_defs;SAD69 / UTM zone 21S
2496;29192;+proj=utm +zone=22 +south +ellps=aust_SA +units=m +no_defs;SAD69 / UTM zone 22S
2497;29193;+proj=utm +zone=23 +south +ellps=aust_SA +units=m +no_defs;SAD69 / UTM zone 23S
2498;29194;+proj=utm +zone=24 +south +ellps=aust_SA +units=m +no_defs;SAD69 / UTM zone 24S
2499;29195;+proj=utm +zone=25 +south +ellps=aust_SA +units=m +no_defs;SAD69 / UTM zone 25S
2500;29220;+proj=utm +zone=20 +south +ellps=intl +towgs84=-355,21,72,0,0,0,0 +units=m +no_defs;Sapper Hill 1943 / UTM zone 20S
2501;29221;+proj=utm +zone=21 +south +ellps=intl +towgs84=-355,21,72,0,0,0,0 +units=m +no_defs;Sapper Hill 1943 / UTM zone 21S
2502;29333;+proj=utm +zone=33 +south +ellps=bess_nam +units=m +no_defs;Schwarzeck / UTM zone 33S
2503;29371;+ellps=bess_nam +to_meter=1.0000135965 +no_defs;South West African Coord. System zone 11
2504;29373;+ellps=bess_nam +to_meter=1.0000135965 +no_defs;South West African Coord. System zone 13
2505;29375;+ellps=bess_nam +to_meter=1.0000135965 +no_defs;South West African Coord. System zone 15
2506;29377;+ellps=bess_nam +to_meter=1.0000135965 +no_defs;South West African Coord. System zone 17
2507;29379;+ellps=bess_nam +to_meter=1.0000135965 +no_defs;South West African Coord. System zone 19
2508;29381;+ellps=bess_nam +to_meter=1.0000135965 +no_defs;South West African Coord. System zone 21
2509;29383;+ellps=bess_nam +to_meter=1.0000135965 +no_defs;South West African Coord. System zone 23
2510;29385;+ellps=bess_nam +to_meter=1.0000135965 +no_defs;South West African Coord. System zone 25
2511;29635;+proj=utm +zone=35 +a=6378249.2 +b=6356515 +units=m +no_defs;Sudan / UTM zone 35N (deprecated)
2512;29636;+proj=utm +zone=36 +a=6378249.2 +b=6356515 +units=m +no_defs;Sudan / UTM zone 36N (deprecated)
2513;29700;+proj=omerc +lat_0=-18.9 +lonc=44.10000000000001 +alpha=18.9 +k=0.9995000000000001 +x_0=400000 +y_0=800000 +ellps=intl +towgs84=-189,-242,-91,0,0,0,0 +pm=paris +units=m +no_defs;Tananarive (Paris) / Laborde Grid
2514;29738;+proj=utm +zone=38 +south +ellps=intl +towgs84=-189,-242,-91,0,0,0,0 +units=m +no_defs;Tananarive / UTM zone 38S
2515;29739;+proj=utm +zone=39 +south +ellps=intl +towgs84=-189,-242,-91,0,0,0,0 +units=m +no_defs;Tananarive / UTM zone 39S
2516;29849;+proj=utm +zone=49 +ellps=evrstSS +units=m +no_defs;Timbalai 1948 / UTM zone 49N
2517;29850;+proj=utm +zone=50 +ellps=evrstSS +units=m +no_defs;Timbalai 1948 / UTM zone 50N
2518;29871;+proj=omerc +lat_0=4 +lonc=115 +alpha=53.31582047222222 +k=0.99984 +x_0=590476.8714630401 +y_0=442857.653094361 +ellps=evrstSS +to_meter=20.11676512155263 +no_defs;Timbalai 1948 / RSO Borneo (ch)
2519;29872;+proj=omerc +lat_0=4 +lonc=115 +alpha=53.31582047222222 +k=0.99984 +x_0=590476.8727431979 +y_0=442857.6545573985 +ellps=evrstSS +to_meter=0.3047994715386762 +no_defs;Timbalai 1948 / RSO Borneo (ft)
2520;29873;+proj=omerc +lat_0=4 +lonc=115 +alpha=53.31582047222222 +k=0.99984 +x_0=590476.87 +y_0=442857.65 +ellps=evrstSS +units=m +no_defs;Timbalai 1948 / RSO Borneo (m)
2521;29900;+proj=tmerc +lat_0=53.5 +lon_0=-8 +k=1.000035 +x_0=200000 +y_0=250000 +a=6377340.189 +b=6356034.447938534 +units=m +no_defs;TM65 / Irish National Grid (deprecated)
2522;29901;+proj=tmerc +lat_0=53.5 +lon_0=-8 +k=1.000000 +x_0=200000 +y_0=250000 +ellps=airy +towgs84=482.5,-130.6,564.6,-1.042,-0.214,-0.631,8.15 +units=m +no_defs;OSNI 1952 / Irish National Grid
2523;29902;+proj=tmerc +lat_0=53.5 +lon_0=-8 +k=1.000035 +x_0=200000 +y_0=250000 +a=6377340.189 +b=6356034.447938534 +units=m +no_defs;TM65 / Irish Grid
2524;29903;+proj=tmerc +lat_0=53.5 +lon_0=-8 +k=1.000035 +x_0=200000 +y_0=250000 +a=6377340.189 +b=6356034.447938534 +units=m +no_defs;TM75 / Irish Grid
2525;30161;+proj=tmerc +lat_0=33 +lon_0=129.5 +k=0.999900 +x_0=0 +y_0=0 +ellps=bessel +units=m +no_defs;Tokyo / Japan Plane Rectangular CS I
2526;30162;+proj=tmerc +lat_0=33 +lon_0=131 +k=0.999900 +x_0=0 +y_0=0 +ellps=bessel +units=m +no_defs;Tokyo / Japan Plane Rectangular CS II
2527;30163;+proj=tmerc +lat_0=36 +lon_0=132.1666666666667 +k=0.999900 +x_0=0 +y_0=0 +ellps=bessel +units=m +no_defs;Tokyo / Japan Plane Rectangular CS III
2528;30164;+proj=tmerc +lat_0=33 +lon_0=133.5 +k=0.999900 +x_0=0 +y_0=0 +ellps=bessel +units=m +no_defs;Tokyo / Japan Plane Rectangular CS IV
2529;30165;+proj=tmerc +lat_0=36 +lon_0=134.3333333333333 +k=0.999900 +x_0=0 +y_0=0 +ellps=bessel +units=m +no_defs;Tokyo / Japan Plane Rectangular CS V
2530;30166;+proj=tmerc +lat_0=36 +lon_0=136 +k=0.999900 +x_0=0 +y_0=0 +ellps=bessel +units=m +no_defs;Tokyo / Japan Plane Rectangular CS VI
2531;30167;+proj=tmerc +lat_0=36 +lon_0=137.1666666666667 +k=0.999900 +x_0=0 +y_0=0 +ellps=bessel +units=m +no_defs;Tokyo / Japan Plane Rectangular CS VII
2532;30168;+proj=tmerc +lat_0=36 +lon_0=138.5 +k=0.999900 +x_0=0 +y_0=0 +ellps=bessel +units=m +no_defs;Tokyo / Japan Plane Rectangular CS VIII
2533;30169;+proj=tmerc +lat_0=36 +lon_0=139.8333333333333 +k=0.999900 +x_0=0 +y_0=0 +ellps=bessel +units=m +no_defs;Tokyo / Japan Plane Rectangular CS IX
2534;30170;+proj=tmerc +lat_0=40 +lon_0=140.8333333333333 +k=0.999900 +x_0=0 +y_0=0 +ellps=bessel +units=m +no_defs;Tokyo / Japan Plane Rectangular CS X
2535;30171;+proj=tmerc +lat_0=44 +lon_0=140.25 +k=0.999900 +x_0=0 +y_0=0 +ellps=bessel +units=m +no_defs;Tokyo / Japan Plane Rectangular CS XI
2536;30172;+proj=tmerc +lat_0=44 +lon_0=142.25 +k=0.999900 +x_0=0 +y_0=0 +ellps=bessel +units=m +no_defs;Tokyo / Japan Plane Rectangular CS XII
2537;30173;+proj=tmerc +lat_0=44 +lon_0=144.25 +k=0.999900 +x_0=0 +y_0=0 +ellps=bessel +units=m +no_defs;Tokyo / Japan Plane Rectangular CS XIII
2538;30174;+proj=tmerc +lat_0=26 +lon_0=142 +k=0.999900 +x_0=0 +y_0=0 +ellps=bessel +units=m +no_defs;Tokyo / Japan Plane Rectangular CS XIV
2539;30175;+proj=tmerc +lat_0=26 +lon_0=127.5 +k=0.999900 +x_0=0 +y_0=0 +ellps=bessel +units=m +no_defs;Tokyo / Japan Plane Rectangular CS XV
2540;30176;+proj=tmerc +lat_0=26 +lon_0=124 +k=0.999900 +x_0=0 +y_0=0 +ellps=bessel +units=m +no_defs;Tokyo / Japan Plane Rectangular CS XVI
2541;30177;+proj=tmerc +lat_0=26 +lon_0=131 +k=0.999900 +x_0=0 +y_0=0 +ellps=bessel +units=m +no_defs;Tokyo / Japan Plane Rectangular CS XVII
2542;30178;+proj=tmerc +lat_0=20 +lon_0=136 +k=0.999900 +x_0=0 +y_0=0 +ellps=bessel +units=m +no_defs;Tokyo / Japan Plane Rectangular CS XVIII
2543;30179;+proj=tmerc +lat_0=26 +lon_0=154 +k=0.999900 +x_0=0 +y_0=0 +ellps=bessel +units=m +no_defs;Tokyo / Japan Plane Rectangular CS XIX
2544;30200;+proj=cass +lat_0=10.44166666666667 +lon_0=-61.33333333333334 +x_0=86501.46392051999 +y_0=65379.0134283 +a=6378293.645208759 +b=6356617.987679838 +to_meter=0.201166195164 +no_defs;Trinidad 1903 / Trinidad Grid
2545;30339;+proj=utm +zone=39 +ellps=helmert +units=m +no_defs;TC(1948) / UTM zone 39N
2546;30340;+proj=utm +zone=40 +ellps=helmert +units=m +no_defs;TC(1948) / UTM zone 40N
2547;30491;+proj=lcc +lat_1=36 +lat_0=36 +lon_0=2.7 +k_0=0.999625544 +x_0=500000 +y_0=300000 +a=6378249.2 +b=6356515 +towgs84=-73,-247,227,0,0,0,0 +units=m +no_defs;Voirol 1875 / Nord Algerie (ancienne)
2548;30492;+proj=lcc +lat_1=33.3 +lat_0=33.3 +lon_0=2.7 +k_0=0.999625769 +x_0=500000 +y_0=300000 +a=6378249.2 +b=6356515 +towgs84=-73,-247,227,0,0,0,0 +units=m +no_defs;Voirol 1875 / Sud Algerie (ancienne)
2549;30493;+proj=lcc +lat_1=36 +lat_0=36 +lon_0=2.7 +k_0=0.999625544 +x_0=500000 +y_0=300000 +a=6378249.2 +b=6356515 +units=m +no_defs;Voirol 1879 / Nord Algerie (ancienne)
2550;30494;+proj=lcc +lat_1=33.3 +lat_0=33.3 +lon_0=2.7 +k_0=0.999625769 +x_0=500000 +y_0=300000 +a=6378249.2 +b=6356515 +units=m +no_defs;Voirol 1879 / Sud Algerie (ancienne)
2551;30729;+proj=utm +zone=29 +ellps=clrk80 +units=m +no_defs;Nord Sahara 1959 / UTM zone 29N
2552;30730;+proj=utm +zone=30 +ellps=clrk80 +units=m +no_defs;Nord Sahara 1959 / UTM zone 30N
2553;30731;+proj=utm +zone=31 +ellps=clrk80 +units=m +no_defs;Nord Sahara 1959 / UTM zone 31N
2554;30732;+proj=utm +zone=32 +ellps=clrk80 +units=m +no_defs;Nord Sahara 1959 / UTM zone 32N
2555;30791;+proj=lcc +lat_1=36 +lat_0=36 +lon_0=2.7 +k_0=0.999625544 +x_0=500135 +y_0=300090 +ellps=clrk80 +units=m +no_defs;Nord Sahara 1959 / Voirol Unifie Nord
2556;30792;+proj=lcc +lat_1=33.3 +lat_0=33.3 +lon_0=2.7 +k_0=0.999625769 +x_0=500135 +y_0=300090 +ellps=clrk80 +units=m +no_defs;Nord Sahara 1959 / Voirol Unifie Sud
2557;30800;+proj=tmerc +lat_0=0 +lon_0=15.80827777777778 +k=1.000000 +x_0=1500000 +y_0=0 +ellps=bessel +units=m +no_defs;RT38 2.5 gon W (deprecated)
2558;31028;+proj=utm +zone=28 +a=6378249.2 +b=6356515 +units=m +no_defs;Yoff / UTM zone 28N
2559;31121;+proj=utm +zone=21 +ellps=intl +towgs84=-265,120,-358,0,0,0,0 +units=m +no_defs;Zanderij / UTM zone 21N
2560;31154;+proj=tmerc +lat_0=0 +lon_0=-54 +k=0.999600 +x_0=500000 +y_0=0 +ellps=intl +towgs84=-265,120,-358,0,0,0,0 +units=m +no_defs;Zanderij / TM 54 NW
2561;31170;+proj=tmerc +lat_0=0 +lon_0=-55.68333333333333 +k=0.999600 +x_0=500000 +y_0=0 +ellps=intl +towgs84=-265,120,-358,0,0,0,0 +units=m +no_defs;Zanderij / Suriname Old TM
2562;31171;+proj=tmerc +lat_0=0 +lon_0=-55.68333333333333 +k=0.999900 +x_0=500000 +y_0=0 +ellps=intl +towgs84=-265,120,-358,0,0,0,0 +units=m +no_defs;Zanderij / Suriname TM
2563;31265;+proj=tmerc +lat_0=0 +lon_0=15 +k=1.000000 +x_0=5500000 +y_0=0 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI / 3-degree Gauss zone 5 (deprecated)
2564;31266;+proj=tmerc +lat_0=0 +lon_0=18 +k=1.000000 +x_0=6500000 +y_0=0 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI / 3-degree Gauss zone 6 (deprecated)
2565;31267;+proj=tmerc +lat_0=0 +lon_0=21 +k=1.000000 +x_0=7500000 +y_0=0 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI / 3-degree Gauss zone 7 (deprecated)
2566;31268;+proj=tmerc +lat_0=0 +lon_0=24 +k=1.000000 +x_0=8500000 +y_0=0 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI / 3-degree Gauss zone 8 (deprecated)
2567;31275;+proj=tmerc +lat_0=0 +lon_0=15 +k=0.999900 +x_0=5500000 +y_0=0 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI / Balkans zone 5
2568;31276;+proj=tmerc +lat_0=0 +lon_0=18 +k=0.999900 +x_0=6500000 +y_0=0 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI / Balkans zone 6
2569;31277;+proj=tmerc +lat_0=0 +lon_0=21 +k=0.999900 +x_0=7500000 +y_0=0 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI / Balkans zone 7
2570;31278;+proj=tmerc +lat_0=0 +lon_0=21 +k=0.999900 +x_0=7500000 +y_0=0 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI / Balkans zone 8 (deprecated)
2571;31279;+proj=tmerc +lat_0=0 +lon_0=24 +k=0.999900 +x_0=8500000 +y_0=0 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI / Balkans zone 8
2572;31281;+proj=tmerc +lat_0=0 +lon_0=10.33333333333333 +k=1.000000 +x_0=0 +y_0=0 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI (Ferro) / Austria West Zone
2573;31282;+proj=tmerc +lat_0=0 +lon_0=13.33333333333333 +k=1.000000 +x_0=0 +y_0=0 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI (Ferro) / Austria Central Zone
2574;31283;+proj=tmerc +lat_0=0 +lon_0=16.33333333333333 +k=1.000000 +x_0=0 +y_0=0 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI (Ferro) / Austria East Zone
2575;31284;+proj=tmerc +lat_0=0 +lon_0=10.33333333333333 +k=1.000000 +x_0=150000 +y_0=0 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;Austria MGI / BMN-M28
2576;31285;+proj=tmerc +lat_0=0 +lon_0=13.33333333333333 +k=1.000000 +x_0=450000 +y_0=0 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;Austria MGI / BMN-M31
2577;31286;+proj=tmerc +lat_0=0 +lon_0=16.33333333333333 +k=1.000000 +x_0=750000 +y_0=0 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;Austria MGI / BMN-M34
2578;31287;+proj=lcc +lat_1=49 +lat_2=46 +lat_0=47.5 +lon_0=13.33333333333333 +x_0=400000 +y_0=400000 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI / Austria Lambert
2579;31288;+proj=tmerc +lat_0=0 +lon_0=10.33333333333333 +k=1.000000 +x_0=150000 +y_0=0 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI (Ferro) / M28
2580;31289;+proj=tmerc +lat_0=0 +lon_0=13.33333333333333 +k=1.000000 +x_0=450000 +y_0=0 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI (Ferro) / M31
2581;31290;+proj=tmerc +lat_0=0 +lon_0=16.33333333333333 +k=1.000000 +x_0=750000 +y_0=0 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI (Ferro) / M34
2582;31291;+proj=tmerc +lat_0=0 +lon_0=10.33333333333333 +k=1.000000 +x_0=0 +y_0=0 +ellps=bessel +units=m +no_defs;MGI (Ferro) / Austria West Zone (deprecated)
2583;31292;+proj=tmerc +lat_0=0 +lon_0=13.33333333333333 +k=1.000000 +x_0=0 +y_0=0 +ellps=bessel +units=m +no_defs;MGI (Ferro) / Austria Central Zone (deprecated)
2584;31293;+proj=tmerc +lat_0=0 +lon_0=16.33333333333333 +k=1.000000 +x_0=0 +y_0=0 +ellps=bessel +units=m +no_defs;MGI (Ferro) / Austria East Zone (deprecated)
2585;31294;+proj=tmerc +lat_0=0 +lon_0=10.33333333333333 +k=1.000000 +x_0=150000 +y_0=0 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI / M28 (deprecated)
2586;31295;+proj=tmerc +lat_0=0 +lon_0=13.33333333333333 +k=1.000000 +x_0=450000 +y_0=0 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI / M31 (deprecated)
2587;31296;+proj=tmerc +lat_0=0 +lon_0=16.33333333333333 +k=1.000000 +x_0=750000 +y_0=0 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI / M34 (deprecated)
2588;31297;+proj=lcc +lat_1=49 +lat_2=46 +lat_0=47.5 +lon_0=13.33333333333333 +x_0=400000 +y_0=400000 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI / Austria Lambert (deprecated)
2589;31300;+proj=lcc +lat_1=49.83333333333334 +lat_2=51.16666666666666 +lat_0=90 +lon_0=4.356939722222222 +x_0=150000.01256 +y_0=5400088.4378 +ellps=intl +units=m +no_defs;Belge 1972 / Belge Lambert 72
2590;31370;+proj=lcc +lat_1=51.16666723333333 +lat_2=49.8333339 +lat_0=90 +lon_0=4.367486666666666 +x_0=150000.013 +y_0=5400088.438 +ellps=intl +units=m +no_defs;Belge 1972 / Belgian Lambert 72
2591;31461;+proj=tmerc +lat_0=0 +lon_0=3 +k=1.000000 +x_0=1500000 +y_0=0 +ellps=bessel +datum=potsdam +units=m +no_defs;DHDN / 3-degree Gauss zone 1 (deprecated)
2592;31462;+proj=tmerc +lat_0=0 +lon_0=6 +k=1.000000 +x_0=2500000 +y_0=0 +ellps=bessel +datum=potsdam +units=m +no_defs;DHDN / 3-degree Gauss zone 2 (deprecated)
2593;31463;+proj=tmerc +lat_0=0 +lon_0=9 +k=1.000000 +x_0=3500000 +y_0=0 +ellps=bessel +datum=potsdam +units=m +no_defs;DHDN / 3-degree Gauss zone 3 (deprecated)
2594;31464;+proj=tmerc +lat_0=0 +lon_0=12 +k=1.000000 +x_0=4500000 +y_0=0 +ellps=bessel +datum=potsdam +units=m +no_defs;DHDN / 3-degree Gauss zone 4 (deprecated)
2595;31465;+proj=tmerc +lat_0=0 +lon_0=15 +k=1.000000 +x_0=5500000 +y_0=0 +ellps=bessel +datum=potsdam +units=m +no_defs;DHDN / 3-degree Gauss zone 5 (deprecated)
2596;31466;+proj=tmerc +lat_0=0 +lon_0=6 +k=1.000000 +x_0=2500000 +y_0=0 +ellps=bessel +datum=potsdam +units=m +no_defs;DHDN / Gauss-Kruger zone 2
2597;31467;+proj=tmerc +lat_0=0 +lon_0=9 +k=1.000000 +x_0=3500000 +y_0=0 +ellps=bessel +datum=potsdam +units=m +no_defs;DHDN / Gauss-Kruger zone 3
2598;31468;+proj=tmerc +lat_0=0 +lon_0=12 +k=1.000000 +x_0=4500000 +y_0=0 +ellps=bessel +datum=potsdam +units=m +no_defs;DHDN / Gauss-Kruger zone 4
2599;31469;+proj=tmerc +lat_0=0 +lon_0=15 +k=1.000000 +x_0=5500000 +y_0=0 +ellps=bessel +datum=potsdam +units=m +no_defs;DHDN / Gauss-Kruger zone 5
2600;31528;+proj=utm +zone=28 +a=6378249.2 +b=6356515 +towgs84=-23,259,-9,0,0,0,0 +units=m +no_defs;Conakry 1905 / UTM zone 28N
2601;31529;+proj=utm +zone=29 +a=6378249.2 +b=6356515 +towgs84=-23,259,-9,0,0,0,0 +units=m +no_defs;Conakry 1905 / UTM zone 29N
2602;31600;+proj=sterea +lat_0=45.9 +lon_0=25.39246588888889 +k=0.999667 +x_0=500000 +y_0=500000 +ellps=intl +units=m +no_defs;Dealul Piscului 1933/ Stereo 33
2603;31700;+proj=sterea +lat_0=46 +lon_0=25 +k=0.999750 +x_0=500000 +y_0=500000 +ellps=krass +units=m +no_defs;Dealul Piscului 1970/ Stereo 70
2604;31838;+proj=utm +zone=38 +ellps=WGS84 +towgs84=-3.2,-5.7,2.8,0,0,0,0 +units=m +no_defs;NGN / UTM zone 38N
2605;31839;+proj=utm +zone=39 +ellps=WGS84 +towgs84=-3.2,-5.7,2.8,0,0,0,0 +units=m +no_defs;NGN / UTM zone 39N
2606;31900;+proj=tmerc +lat_0=0 +lon_0=48 +k=0.999600 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;KUDAMS / KTM (deprecated)
2607;31901;+proj=tmerc +lat_0=0 +lon_0=48 +k=1.000000 +x_0=500000 +y_0=0 +ellps=GRS80 +units=m +no_defs;KUDAMS / KTM
2608;31965;+proj=utm +zone=11 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS 2000 / UTM zone 11N
2609;31966;+proj=utm +zone=12 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS 2000 / UTM zone 12N
2610;31967;+proj=utm +zone=13 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS 2000 / UTM zone 13N
2611;31968;+proj=utm +zone=14 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS 2000 / UTM zone 14N
2612;31969;+proj=utm +zone=15 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS 2000 / UTM zone 15N
2613;31970;+proj=utm +zone=16 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS 2000 / UTM zone 16N
2614;31971;+proj=utm +zone=17 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS 2000 / UTM zone 17N
2615;31972;+proj=utm +zone=18 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS 2000 / UTM zone 18N
2616;31973;+proj=utm +zone=19 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS 2000 / UTM zone 19N
2617;31974;+proj=utm +zone=20 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS 2000 / UTM zone 20N
2618;31975;+proj=utm +zone=21 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS 2000 / UTM zone 21N
2619;31976;+proj=utm +zone=22 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS 2000 / UTM zone 22N
2620;31977;+proj=utm +zone=17 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS 2000 / UTM zone 17S
2621;31978;+proj=utm +zone=18 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS 2000 / UTM zone 18S
2622;31979;+proj=utm +zone=19 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS 2000 / UTM zone 19S
2623;31980;+proj=utm +zone=20 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS 2000 / UTM zone 20S
2624;31981;+proj=utm +zone=21 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS 2000 / UTM zone 21S
2625;31982;+proj=utm +zone=22 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS 2000 / UTM zone 22S
2626;31983;+proj=utm +zone=23 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS 2000 / UTM zone 23S
2627;31984;+proj=utm +zone=24 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS 2000 / UTM zone 24S
2628;31985;+proj=utm +zone=25 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS 2000 / UTM zone 25S
2629;31986;+proj=utm +zone=17 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS / UTM zone 17N
2630;31987;+proj=utm +zone=18 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS / UTM zone 18N
2631;31988;+proj=utm +zone=19 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS / UTM zone 19N
2632;31989;+proj=utm +zone=20 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS / UTM zone 20N
2633;31990;+proj=utm +zone=21 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS / UTM zone 21N
2634;31991;+proj=utm +zone=22 +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS / UTM zone 22N
2635;31992;+proj=utm +zone=17 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS / UTM zone 17S
2636;31993;+proj=utm +zone=18 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS / UTM zone 18S
2637;31994;+proj=utm +zone=19 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS / UTM zone 19S
2638;31995;+proj=utm +zone=20 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS / UTM zone 20S
2639;31996;+proj=utm +zone=21 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS / UTM zone 21S
2640;31997;+proj=utm +zone=22 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS / UTM zone 22S
2641;31998;+proj=utm +zone=23 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS / UTM zone 23S
2642;31999;+proj=utm +zone=24 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS / UTM zone 24S
2643;32000;+proj=utm +zone=25 +south +ellps=GRS80 +towgs84=0,0,0,0,0,0,0 +units=m +no_defs;SIRGAS / UTM zone 25S
2644;32001;+proj=lcc +lat_1=48.71666666666667 +lat_2=47.85 +lat_0=47 +lon_0=-109.5 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Montana North
2645;32002;+proj=lcc +lat_1=47.88333333333333 +lat_2=46.45 +lat_0=45.83333333333334 +lon_0=-109.5 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Montana Central
2646;32003;+proj=lcc +lat_1=46.4 +lat_2=44.86666666666667 +lat_0=44 +lon_0=-109.5 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Montana South
2647;32005;+proj=lcc +lat_1=41.85 +lat_2=42.81666666666667 +lat_0=41.33333333333334 +lon_0=-100 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Nebraska North
2648;32006;+proj=lcc +lat_1=40.28333333333333 +lat_2=41.71666666666667 +lat_0=39.66666666666666 +lon_0=-99.5 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Nebraska South
2649;32007;+proj=tmerc +lat_0=34.75 +lon_0=-115.5833333333333 +k=0.999900 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Nevada East
2650;32008;+proj=tmerc +lat_0=34.75 +lon_0=-116.6666666666667 +k=0.999900 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Nevada Central
2651;32009;+proj=tmerc +lat_0=34.75 +lon_0=-118.5833333333333 +k=0.999900 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Nevada West
2652;32010;+proj=tmerc +lat_0=42.5 +lon_0=-71.66666666666667 +k=0.999967 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / New Hampshire
2653;32011;+proj=tmerc +lat_0=38.83333333333334 +lon_0=-74.66666666666667 +k=0.999975 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / New Jersey
2654;32012;+proj=tmerc +lat_0=31 +lon_0=-104.3333333333333 +k=0.999909 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / New Mexico East
2655;32013;+proj=tmerc +lat_0=31 +lon_0=-106.25 +k=0.999900 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / New Mexico Central
2656;32014;+proj=tmerc +lat_0=31 +lon_0=-107.8333333333333 +k=0.999917 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / New Mexico West
2657;32015;+proj=tmerc +lat_0=40 +lon_0=-74.33333333333333 +k=0.999967 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / New York East
2658;32016;+proj=tmerc +lat_0=40 +lon_0=-76.58333333333333 +k=0.999938 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / New York Central
2659;32017;+proj=tmerc +lat_0=40 +lon_0=-78.58333333333333 +k=0.999938 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / New York West
2660;32018;+proj=lcc +lat_1=41.03333333333333 +lat_2=40.66666666666666 +lat_0=40.5 +lon_0=-74 +x_0=304800.6096012192 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / New York Long Island
2661;32019;+proj=lcc +lat_1=34.33333333333334 +lat_2=36.16666666666666 +lat_0=33.75 +lon_0=-79 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / North Carolina
2662;32020;+proj=lcc +lat_1=47.43333333333333 +lat_2=48.73333333333333 +lat_0=47 +lon_0=-100.5 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / North Dakota North
2663;32021;+proj=lcc +lat_1=46.18333333333333 +lat_2=47.48333333333333 +lat_0=45.66666666666666 +lon_0=-100.5 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / North Dakota South
2664;32022;+proj=lcc +lat_1=40.43333333333333 +lat_2=41.7 +lat_0=39.66666666666666 +lon_0=-82.5 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Ohio North
2665;32023;+proj=lcc +lat_1=38.73333333333333 +lat_2=40.03333333333333 +lat_0=38 +lon_0=-82.5 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Ohio South
2666;32024;+proj=lcc +lat_1=35.56666666666667 +lat_2=36.76666666666667 +lat_0=35 +lon_0=-98 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Oklahoma North
2667;32025;+proj=lcc +lat_1=33.93333333333333 +lat_2=35.23333333333333 +lat_0=33.33333333333334 +lon_0=-98 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Oklahoma South
2668;32026;+proj=lcc +lat_1=44.33333333333334 +lat_2=46 +lat_0=43.66666666666666 +lon_0=-120.5 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Oregon North
2669;32027;+proj=lcc +lat_1=42.33333333333334 +lat_2=44 +lat_0=41.66666666666666 +lon_0=-120.5 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Oregon South
2670;32028;+proj=lcc +lat_1=40.88333333333333 +lat_2=41.95 +lat_0=40.16666666666666 +lon_0=-77.75 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Pennsylvania North
2671;32029;+proj=lcc +lat_1=39.93333333333333 +lat_2=40.8 +lat_0=39.33333333333334 +lon_0=-77.75 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Pennsylvania South
2672;32030;+proj=tmerc +lat_0=41.08333333333334 +lon_0=-71.5 +k=0.999994 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Rhode Island
2673;32031;+proj=lcc +lat_1=33.76666666666667 +lat_2=34.96666666666667 +lat_0=33 +lon_0=-81 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / South Carolina North
2674;32033;+proj=lcc +lat_1=32.33333333333334 +lat_2=33.66666666666666 +lat_0=31.83333333333333 +lon_0=-81 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / South Carolina South
2675;32034;+proj=lcc +lat_1=44.41666666666666 +lat_2=45.68333333333333 +lat_0=43.83333333333334 +lon_0=-100 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / South Dakota North
2676;32035;+proj=lcc +lat_1=42.83333333333334 +lat_2=44.4 +lat_0=42.33333333333334 +lon_0=-100.3333333333333 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / South Dakota South
2677;32036;+proj=lcc +lat_1=35.25 +lat_2=36.41666666666666 +lat_0=34.66666666666666 +lon_0=-86 +x_0=30480.06096012192 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Tennessee (deprecated)
2678;32037;+proj=lcc +lat_1=34.65 +lat_2=36.18333333333333 +lat_0=34 +lon_0=-101.5 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Texas North
2679;32038;+proj=lcc +lat_1=32.13333333333333 +lat_2=33.96666666666667 +lat_0=31.66666666666667 +lon_0=-97.5 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Texas North Central
2680;32039;+proj=lcc +lat_1=30.11666666666667 +lat_2=31.88333333333333 +lat_0=29.66666666666667 +lon_0=-100.3333333333333 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Texas Central
2681;32040;+proj=lcc +lat_1=28.38333333333333 +lat_2=30.28333333333333 +lat_0=27.83333333333333 +lon_0=-99 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Texas South Central
2682;32041;+proj=lcc +lat_1=26.16666666666667 +lat_2=27.83333333333333 +lat_0=25.66666666666667 +lon_0=-98.5 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Texas South
2683;32042;+proj=lcc +lat_1=40.71666666666667 +lat_2=41.78333333333333 +lat_0=40.33333333333334 +lon_0=-111.5 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Utah North
2684;32043;+proj=lcc +lat_1=39.01666666666667 +lat_2=40.65 +lat_0=38.33333333333334 +lon_0=-111.5 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Utah Central
2685;32044;+proj=lcc +lat_1=37.21666666666667 +lat_2=38.35 +lat_0=36.66666666666666 +lon_0=-111.5 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Utah South
2686;32045;+proj=tmerc +lat_0=42.5 +lon_0=-72.5 +k=0.999964 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Vermont
2687;32046;+proj=lcc +lat_1=38.03333333333333 +lat_2=39.2 +lat_0=37.66666666666666 +lon_0=-78.5 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Virginia North
2688;32047;+proj=lcc +lat_1=36.76666666666667 +lat_2=37.96666666666667 +lat_0=36.33333333333334 +lon_0=-78.5 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Virginia South
2689;32048;+proj=lcc +lat_1=47.5 +lat_2=48.73333333333333 +lat_0=47 +lon_0=-120.8333333333333 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Washington North
2690;32049;+proj=lcc +lat_1=45.83333333333334 +lat_2=47.33333333333334 +lat_0=45.33333333333334 +lon_0=-120.5 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Washington South
2691;32050;+proj=lcc +lat_1=39 +lat_2=40.25 +lat_0=38.5 +lon_0=-79.5 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / West Virginia North
2692;32051;+proj=lcc +lat_1=37.48333333333333 +lat_2=38.88333333333333 +lat_0=37 +lon_0=-81 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / West Virginia South
2693;32052;+proj=lcc +lat_1=45.56666666666667 +lat_2=46.76666666666667 +lat_0=45.16666666666666 +lon_0=-90 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Wisconsin North
2694;32053;+proj=lcc +lat_1=44.25 +lat_2=45.5 +lat_0=43.83333333333334 +lon_0=-90 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Wisconsin Central
2695;32054;+proj=lcc +lat_1=42.73333333333333 +lat_2=44.06666666666667 +lat_0=42 +lon_0=-90 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Wisconsin South
2696;32055;+proj=tmerc +lat_0=40.66666666666666 +lon_0=-105.1666666666667 +k=0.999941 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Wyoming East
2697;32056;+proj=tmerc +lat_0=40.66666666666666 +lon_0=-107.3333333333333 +k=0.999941 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Wyoming East Central
2698;32057;+proj=tmerc +lat_0=40.66666666666666 +lon_0=-108.75 +k=0.999941 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Wyoming West Central
2699;32058;+proj=tmerc +lat_0=40.66666666666666 +lon_0=-110.0833333333333 +k=0.999941 +x_0=152400.3048006096 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Wyoming West
2700;32061;+proj=lcc +lat_1=16.81666666666667 +lat_0=16.81666666666667 +lon_0=-90.33333333333333 +k_0=0.99992226 +x_0=500000 +y_0=292209.579 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / Guatemala Norte
2701;32062;+proj=lcc +lat_1=14.9 +lat_0=14.9 +lon_0=-90.33333333333333 +k_0=0.99989906 +x_0=500000 +y_0=325992.681 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / Guatemala Sur
2702;32064;+proj=tmerc +lat_0=0 +lon_0=-99 +k=0.999600 +x_0=500000.001016002 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / BLM 14N (ftUS)
2703;32065;+proj=tmerc +lat_0=0 +lon_0=-93 +k=0.999600 +x_0=500000.001016002 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / BLM 15N (ftUS)
2704;32066;+proj=tmerc +lat_0=0 +lon_0=-87 +k=0.999600 +x_0=500000.001016002 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / BLM 16N (ftUS)
2705;32067;+proj=tmerc +lat_0=0 +lon_0=-81 +k=0.999600 +x_0=500000.001016002 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / BLM 17N (ftUS)
2706;32074;+proj=tmerc +lat_0=0 +lon_0=-99 +k=0.999600 +x_0=500000.001016002 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / BLM 14N (feet) (deprecated)
2707;32075;+proj=tmerc +lat_0=0 +lon_0=-93 +k=0.999600 +x_0=500000.001016002 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / BLM 15N (feet) (deprecated)
2708;32076;+proj=tmerc +lat_0=0 +lon_0=-87 +k=0.999600 +x_0=500000.001016002 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / BLM 16N (feet) (deprecated)
2709;32077;+proj=tmerc +lat_0=0 +lon_0=-81 +k=0.999600 +x_0=500000.001016002 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / BLM 17N (feet) (deprecated)
2710;32081;+proj=tmerc +lat_0=0 +lon_0=-53 +k=0.999900 +x_0=304800 +y_0=0 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / MTM zone 1
2711;32082;+proj=tmerc +lat_0=0 +lon_0=-56 +k=0.999900 +x_0=304800 +y_0=0 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / MTM zone 2
2712;32083;+proj=tmerc +lat_0=0 +lon_0=-58.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / MTM zone 3
2713;32084;+proj=tmerc +lat_0=0 +lon_0=-61.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / MTM zone 4
2714;32085;+proj=tmerc +lat_0=0 +lon_0=-64.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / MTM zone 5
2715;32086;+proj=tmerc +lat_0=0 +lon_0=-67.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / MTM zone 6
2716;32098;+proj=lcc +lat_1=60 +lat_2=46 +lat_0=44 +lon_0=-68.5 +x_0=0 +y_0=0 +ellps=clrk66 +datum=NAD27 +units=m +no_defs;NAD27 / Quebec Lambert
2717;32099;+proj=lcc +lat_1=27.83333333333333 +lat_2=26.16666666666667 +lat_0=25.66666666666667 +lon_0=-91.33333333333333 +x_0=609601.2192024384 +y_0=0 +ellps=clrk66 +datum=NAD27 +to_meter=0.3048006096012192 +no_defs;NAD27 / Louisiana Offshore
2718;32100;+proj=lcc +lat_1=49 +lat_2=45 +lat_0=44.25 +lon_0=-109.5 +x_0=600000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Montana
2719;32104;+proj=lcc +lat_1=43 +lat_2=40 +lat_0=39.83333333333334 +lon_0=-100 +x_0=500000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Nebraska
2720;32107;+proj=tmerc +lat_0=34.75 +lon_0=-115.5833333333333 +k=0.999900 +x_0=200000 +y_0=8000000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Nevada East
2721;32108;+proj=tmerc +lat_0=34.75 +lon_0=-116.6666666666667 +k=0.999900 +x_0=500000 +y_0=6000000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Nevada Central
2722;32109;+proj=tmerc +lat_0=34.75 +lon_0=-118.5833333333333 +k=0.999900 +x_0=800000 +y_0=4000000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Nevada West
2723;32110;+proj=tmerc +lat_0=42.5 +lon_0=-71.66666666666667 +k=0.999967 +x_0=300000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / New Hampshire
2724;32111;+proj=tmerc +lat_0=38.83333333333334 +lon_0=-74.5 +k=0.999900 +x_0=150000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / New Jersey
2725;32112;+proj=tmerc +lat_0=31 +lon_0=-104.3333333333333 +k=0.999909 +x_0=165000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / New Mexico East
2726;32113;+proj=tmerc +lat_0=31 +lon_0=-106.25 +k=0.999900 +x_0=500000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / New Mexico Central
2727;32114;+proj=tmerc +lat_0=31 +lon_0=-107.8333333333333 +k=0.999917 +x_0=830000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / New Mexico West
2728;32115;+proj=tmerc +lat_0=38.83333333333334 +lon_0=-74.5 +k=0.999900 +x_0=150000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / New York East
2729;32116;+proj=tmerc +lat_0=40 +lon_0=-76.58333333333333 +k=0.999938 +x_0=250000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / New York Central
2730;32117;+proj=tmerc +lat_0=40 +lon_0=-78.58333333333333 +k=0.999938 +x_0=350000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / New York West
2731;32118;+proj=lcc +lat_1=41.03333333333333 +lat_2=40.66666666666666 +lat_0=40.16666666666666 +lon_0=-74 +x_0=300000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / New York Long Island
2732;32119;+proj=lcc +lat_1=36.16666666666666 +lat_2=34.33333333333334 +lat_0=33.75 +lon_0=-79 +x_0=609601.22 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / North Carolina
2733;32120;+proj=lcc +lat_1=48.73333333333333 +lat_2=47.43333333333333 +lat_0=47 +lon_0=-100.5 +x_0=600000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / North Dakota North
2734;32121;+proj=lcc +lat_1=47.48333333333333 +lat_2=46.18333333333333 +lat_0=45.66666666666666 +lon_0=-100.5 +x_0=600000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / North Dakota South
2735;32122;+proj=lcc +lat_1=41.7 +lat_2=40.43333333333333 +lat_0=39.66666666666666 +lon_0=-82.5 +x_0=600000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Ohio North
2736;32123;+proj=lcc +lat_1=40.03333333333333 +lat_2=38.73333333333333 +lat_0=38 +lon_0=-82.5 +x_0=600000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Ohio South
2737;32124;+proj=lcc +lat_1=36.76666666666667 +lat_2=35.56666666666667 +lat_0=35 +lon_0=-98 +x_0=600000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Oklahoma North
2738;32125;+proj=lcc +lat_1=35.23333333333333 +lat_2=33.93333333333333 +lat_0=33.33333333333334 +lon_0=-98 +x_0=600000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Oklahoma South
2739;32126;+proj=lcc +lat_1=46 +lat_2=44.33333333333334 +lat_0=43.66666666666666 +lon_0=-120.5 +x_0=2500000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Oregon North
2740;32127;+proj=lcc +lat_1=44 +lat_2=42.33333333333334 +lat_0=41.66666666666666 +lon_0=-120.5 +x_0=1500000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Oregon South
2741;32128;+proj=lcc +lat_1=41.95 +lat_2=40.88333333333333 +lat_0=40.16666666666666 +lon_0=-77.75 +x_0=600000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Pennsylvania North
2742;32129;+proj=lcc +lat_1=40.96666666666667 +lat_2=39.93333333333333 +lat_0=39.33333333333334 +lon_0=-77.75 +x_0=600000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Pennsylvania South
2743;32130;+proj=tmerc +lat_0=41.08333333333334 +lon_0=-71.5 +k=0.999994 +x_0=100000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Rhode Island
2744;32133;+proj=lcc +lat_1=34.83333333333334 +lat_2=32.5 +lat_0=31.83333333333333 +lon_0=-81 +x_0=609600 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / South Carolina
2745;32134;+proj=lcc +lat_1=45.68333333333333 +lat_2=44.41666666666666 +lat_0=43.83333333333334 +lon_0=-100 +x_0=600000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / South Dakota North
2746;32135;+proj=lcc +lat_1=44.4 +lat_2=42.83333333333334 +lat_0=42.33333333333334 +lon_0=-100.3333333333333 +x_0=600000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / South Dakota South
2747;32136;+proj=lcc +lat_1=36.41666666666666 +lat_2=35.25 +lat_0=34.33333333333334 +lon_0=-86 +x_0=600000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Tennessee
2748;32137;+proj=lcc +lat_1=36.18333333333333 +lat_2=34.65 +lat_0=34 +lon_0=-101.5 +x_0=200000 +y_0=1000000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Texas North
2749;32138;+proj=lcc +lat_1=33.96666666666667 +lat_2=32.13333333333333 +lat_0=31.66666666666667 +lon_0=-98.5 +x_0=600000 +y_0=2000000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Texas North Central
2750;32139;+proj=lcc +lat_1=31.88333333333333 +lat_2=30.11666666666667 +lat_0=29.66666666666667 +lon_0=-100.3333333333333 +x_0=700000 +y_0=3000000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Texas Central
2751;32140;+proj=lcc +lat_1=30.28333333333333 +lat_2=28.38333333333333 +lat_0=27.83333333333333 +lon_0=-99 +x_0=600000 +y_0=4000000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Texas South Central
2752;32141;+proj=lcc +lat_1=27.83333333333333 +lat_2=26.16666666666667 +lat_0=25.66666666666667 +lon_0=-98.5 +x_0=300000 +y_0=5000000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Texas South
2753;32142;+proj=lcc +lat_1=41.78333333333333 +lat_2=40.71666666666667 +lat_0=40.33333333333334 +lon_0=-111.5 +x_0=500000 +y_0=1000000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Utah North
2754;32143;+proj=lcc +lat_1=40.65 +lat_2=39.01666666666667 +lat_0=38.33333333333334 +lon_0=-111.5 +x_0=500000 +y_0=2000000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Utah Central
2755;32144;+proj=lcc +lat_1=38.35 +lat_2=37.21666666666667 +lat_0=36.66666666666666 +lon_0=-111.5 +x_0=500000 +y_0=3000000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Utah South
2756;32145;+proj=tmerc +lat_0=42.5 +lon_0=-72.5 +k=0.999964 +x_0=500000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Vermont
2757;32146;+proj=lcc +lat_1=39.2 +lat_2=38.03333333333333 +lat_0=37.66666666666666 +lon_0=-78.5 +x_0=3500000 +y_0=2000000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Virginia North
2758;32147;+proj=lcc +lat_1=37.96666666666667 +lat_2=36.76666666666667 +lat_0=36.33333333333334 +lon_0=-78.5 +x_0=3500000 +y_0=1000000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Virginia South
2759;32148;+proj=lcc +lat_1=48.73333333333333 +lat_2=47.5 +lat_0=47 +lon_0=-120.8333333333333 +x_0=500000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Washington North
2760;32149;+proj=lcc +lat_1=47.33333333333334 +lat_2=45.83333333333334 +lat_0=45.33333333333334 +lon_0=-120.5 +x_0=500000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Washington South
2761;32150;+proj=lcc +lat_1=40.25 +lat_2=39 +lat_0=38.5 +lon_0=-79.5 +x_0=600000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / West Virginia North
2762;32151;+proj=lcc +lat_1=38.88333333333333 +lat_2=37.48333333333333 +lat_0=37 +lon_0=-81 +x_0=600000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / West Virginia South
2763;32152;+proj=lcc +lat_1=46.76666666666667 +lat_2=45.56666666666667 +lat_0=45.16666666666666 +lon_0=-90 +x_0=600000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Wisconsin North
2764;32153;+proj=lcc +lat_1=45.5 +lat_2=44.25 +lat_0=43.83333333333334 +lon_0=-90 +x_0=600000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Wisconsin Central
2765;32154;+proj=lcc +lat_1=44.06666666666667 +lat_2=42.73333333333333 +lat_0=42 +lon_0=-90 +x_0=600000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Wisconsin South
2766;32155;+proj=tmerc +lat_0=40.5 +lon_0=-105.1666666666667 +k=0.999938 +x_0=200000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Wyoming East
2767;32156;+proj=tmerc +lat_0=40.5 +lon_0=-107.3333333333333 +k=0.999938 +x_0=400000 +y_0=100000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Wyoming East Central
2768;32157;+proj=tmerc +lat_0=40.5 +lon_0=-108.75 +k=0.999938 +x_0=600000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Wyoming West Central
2769;32158;+proj=tmerc +lat_0=40.5 +lon_0=-110.0833333333333 +k=0.999938 +x_0=800000 +y_0=100000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Wyoming West
2770;32161;+proj=lcc +lat_1=18.43333333333333 +lat_2=18.03333333333333 +lat_0=17.83333333333333 +lon_0=-66.43333333333334 +x_0=200000 +y_0=200000 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Puerto Rico & Virgin Is.
2771;32164;+proj=tmerc +lat_0=0 +lon_0=-99 +k=0.999600 +x_0=500000.001016002 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / BLM 14N (ftUS)
2772;32165;+proj=tmerc +lat_0=0 +lon_0=-93 +k=0.999600 +x_0=500000.001016002 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / BLM 15N (ftUS)
2773;32166;+proj=tmerc +lat_0=0 +lon_0=-87 +k=0.999600 +x_0=500000.001016002 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / BLM 16N (ftUS)
2774;32167;+proj=tmerc +lat_0=0 +lon_0=-81 +k=0.999600 +x_0=500000.001016002 +y_0=0 +ellps=GRS80 +datum=NAD83 +to_meter=0.3048006096012192 +no_defs;NAD83 / BLM 17N (ftUS)
2775;32180;+proj=tmerc +lat_0=0 +lon_0=-55.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / SCoPQ zone 2
2776;32181;+proj=tmerc +lat_0=0 +lon_0=-53 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / MTM zone 1
2777;32182;+proj=tmerc +lat_0=0 +lon_0=-56 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / MTM zone 2
2778;32183;+proj=tmerc +lat_0=0 +lon_0=-58.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / MTM zone 3
2779;32184;+proj=tmerc +lat_0=0 +lon_0=-61.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / MTM zone 4
2780;32185;+proj=tmerc +lat_0=0 +lon_0=-64.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / MTM zone 5
2781;32186;+proj=tmerc +lat_0=0 +lon_0=-67.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / MTM zone 6
2782;32187;+proj=tmerc +lat_0=0 +lon_0=-70.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / MTM zone 7
2783;32188;+proj=tmerc +lat_0=0 +lon_0=-73.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / MTM zone 8
2784;32189;+proj=tmerc +lat_0=0 +lon_0=-76.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / MTM zone 9
2785;32190;+proj=tmerc +lat_0=0 +lon_0=-79.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / MTM zone 10
2786;32191;+proj=tmerc +lat_0=0 +lon_0=-82.5 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / MTM zone 11
2787;32192;+proj=tmerc +lat_0=0 +lon_0=-81 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / MTM zone 12
2788;32193;+proj=tmerc +lat_0=0 +lon_0=-84 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / MTM zone 13
2789;32194;+proj=tmerc +lat_0=0 +lon_0=-87 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / MTM zone 14
2790;32195;+proj=tmerc +lat_0=0 +lon_0=-90 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / MTM zone 15
2791;32196;+proj=tmerc +lat_0=0 +lon_0=-93 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / MTM zone 16
2792;32197;+proj=tmerc +lat_0=0 +lon_0=-96 +k=0.999900 +x_0=304800 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / MTM zone 17
2793;32198;+proj=lcc +lat_1=60 +lat_2=46 +lat_0=44 +lon_0=-68.5 +x_0=0 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Quebec Lambert
2794;32199;+proj=lcc +lat_1=27.83333333333333 +lat_2=26.16666666666667 +lat_0=25.5 +lon_0=-91.33333333333333 +x_0=1000000 +y_0=0 +ellps=GRS80 +datum=NAD83 +units=m +no_defs;NAD83 / Louisiana Offshore
2795;32201;+proj=utm +zone=1 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 1N
2796;32202;+proj=utm +zone=2 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 2N
2797;32203;+proj=utm +zone=3 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 3N
2798;32204;+proj=utm +zone=4 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 4N
2799;32205;+proj=utm +zone=5 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 5N
2800;32206;+proj=utm +zone=6 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 6N
2801;32207;+proj=utm +zone=7 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 7N
2802;32208;+proj=utm +zone=8 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 8N
2803;32209;+proj=utm +zone=9 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 9N
2804;32210;+proj=utm +zone=10 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 10N
2805;32211;+proj=utm +zone=11 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 11N
2806;32212;+proj=utm +zone=12 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 12N
2807;32213;+proj=utm +zone=13 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 13N
2808;32214;+proj=utm +zone=14 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 14N
2809;32215;+proj=utm +zone=15 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 15N
2810;32216;+proj=utm +zone=16 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 16N
2811;32217;+proj=utm +zone=17 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 17N
2812;32218;+proj=utm +zone=18 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 18N
2813;32219;+proj=utm +zone=19 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 19N
2814;32220;+proj=utm +zone=20 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 20N
2815;32221;+proj=utm +zone=21 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 21N
2816;32222;+proj=utm +zone=22 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 22N
2817;32223;+proj=utm +zone=23 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 23N
2818;32224;+proj=utm +zone=24 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 24N
2819;32225;+proj=utm +zone=25 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 25N
2820;32226;+proj=utm +zone=26 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 26N
2821;32227;+proj=utm +zone=27 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 27N
2822;32228;+proj=utm +zone=28 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 28N
2823;32229;+proj=utm +zone=29 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 29N
2824;32230;+proj=utm +zone=30 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 30N
2825;32231;+proj=utm +zone=31 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 31N
2826;32232;+proj=utm +zone=32 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 32N
2827;32233;+proj=utm +zone=33 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 33N
2828;32234;+proj=utm +zone=34 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 34N
2829;32235;+proj=utm +zone=35 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 35N
2830;32236;+proj=utm +zone=36 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 36N
2831;32237;+proj=utm +zone=37 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 37N
2832;32238;+proj=utm +zone=38 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 38N
2833;32239;+proj=utm +zone=39 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 39N
2834;32240;+proj=utm +zone=40 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 40N
2835;32241;+proj=utm +zone=41 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 41N
2836;32242;+proj=utm +zone=42 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 42N
2837;32243;+proj=utm +zone=43 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 43N
2838;32244;+proj=utm +zone=44 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 44N
2839;32245;+proj=utm +zone=45 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 45N
2840;32246;+proj=utm +zone=46 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 46N
2841;32247;+proj=utm +zone=47 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 47N
2842;32248;+proj=utm +zone=48 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 48N
2843;32249;+proj=utm +zone=49 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 49N
2844;32250;+proj=utm +zone=50 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 50N
2845;32251;+proj=utm +zone=51 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 51N
2846;32252;+proj=utm +zone=52 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 52N
2847;32253;+proj=utm +zone=53 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 53N
2848;32254;+proj=utm +zone=54 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 54N
2849;32255;+proj=utm +zone=55 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 55N
2850;32256;+proj=utm +zone=56 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 56N
2851;32257;+proj=utm +zone=57 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 57N
2852;32258;+proj=utm +zone=58 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 58N
2853;32259;+proj=utm +zone=59 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 59N
2854;32260;+proj=utm +zone=60 +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 60N
2855;32301;+proj=utm +zone=1 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 1S
2856;32302;+proj=utm +zone=2 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 2S
2857;32303;+proj=utm +zone=3 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 3S
2858;32304;+proj=utm +zone=4 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 4S
2859;32305;+proj=utm +zone=5 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 5S
2860;32306;+proj=utm +zone=6 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 6S
2861;32307;+proj=utm +zone=7 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 7S
2862;32308;+proj=utm +zone=8 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 8S
2863;32309;+proj=utm +zone=9 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 9S
2864;32310;+proj=utm +zone=10 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 10S
2865;32311;+proj=utm +zone=11 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 11S
2866;32312;+proj=utm +zone=12 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 12S
2867;32313;+proj=utm +zone=13 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 13S
2868;32314;+proj=utm +zone=14 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 14S
2869;32315;+proj=utm +zone=15 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 15S
2870;32316;+proj=utm +zone=16 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 16S
2871;32317;+proj=utm +zone=17 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 17S
2872;32318;+proj=utm +zone=18 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 18S
2873;32319;+proj=utm +zone=19 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 19S
2874;32320;+proj=utm +zone=20 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 20S
2875;32321;+proj=utm +zone=21 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 21S
2876;32322;+proj=utm +zone=22 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 22S
2877;32323;+proj=utm +zone=23 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 23S
2878;32324;+proj=utm +zone=24 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 24S
2879;32325;+proj=utm +zone=25 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 25S
2880;32326;+proj=utm +zone=26 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 26S
2881;32327;+proj=utm +zone=27 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 27S
2882;32328;+proj=utm +zone=28 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 28S
2883;32329;+proj=utm +zone=29 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 29S
2884;32330;+proj=utm +zone=30 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 30S
2885;32331;+proj=utm +zone=31 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 31S
2886;32332;+proj=utm +zone=32 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 32S
2887;32333;+proj=utm +zone=33 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 33S
2888;32334;+proj=utm +zone=34 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 34S
2889;32335;+proj=utm +zone=35 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 35S
2890;32336;+proj=utm +zone=36 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 36S
2891;32337;+proj=utm +zone=37 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 37S
2892;32338;+proj=utm +zone=38 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 38S
2893;32339;+proj=utm +zone=39 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 39S
2894;32340;+proj=utm +zone=40 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 40S
2895;32341;+proj=utm +zone=41 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 41S
2896;32342;+proj=utm +zone=42 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 42S
2897;32343;+proj=utm +zone=43 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 43S
2898;32344;+proj=utm +zone=44 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 44S
2899;32345;+proj=utm +zone=45 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 45S
2900;32346;+proj=utm +zone=46 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 46S
2901;32347;+proj=utm +zone=47 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 47S
2902;32348;+proj=utm +zone=48 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 48S
2903;32349;+proj=utm +zone=49 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 49S
2904;32350;+proj=utm +zone=50 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 50S
2905;32351;+proj=utm +zone=51 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 51S
2906;32352;+proj=utm +zone=52 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 52S
2907;32353;+proj=utm +zone=53 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 53S
2908;32354;+proj=utm +zone=54 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 54S
2909;32355;+proj=utm +zone=55 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 55S
2910;32356;+proj=utm +zone=56 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 56S
2911;32357;+proj=utm +zone=57 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 57S
2912;32358;+proj=utm +zone=58 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 58S
2913;32359;+proj=utm +zone=59 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 59S
2914;32360;+proj=utm +zone=60 +south +ellps=WGS72 +units=m +no_defs;WGS 72 / UTM zone 60S
2915;32401;+proj=utm +zone=1 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 1N
2916;32402;+proj=utm +zone=2 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 2N
2917;32403;+proj=utm +zone=3 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 3N
2918;32404;+proj=utm +zone=4 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 4N
2919;32405;+proj=utm +zone=5 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 5N
2920;32406;+proj=utm +zone=6 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 6N
2921;32407;+proj=utm +zone=7 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 7N
2922;32408;+proj=utm +zone=8 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 8N
2923;32409;+proj=utm +zone=9 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 9N
2924;32410;+proj=utm +zone=10 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 10N
2925;32411;+proj=utm +zone=11 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 11N
2926;32412;+proj=utm +zone=12 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 12N
2927;32413;+proj=utm +zone=13 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 13N
2928;32414;+proj=utm +zone=14 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 14N
2929;32415;+proj=utm +zone=15 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 15N
2930;32416;+proj=utm +zone=16 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 16N
2931;32417;+proj=utm +zone=17 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 17N
2932;32418;+proj=utm +zone=18 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 18N
2933;32419;+proj=utm +zone=19 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 19N
2934;32420;+proj=utm +zone=20 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 20N
2935;32421;+proj=utm +zone=21 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 21N
2936;32422;+proj=utm +zone=22 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 22N
2937;32423;+proj=utm +zone=23 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 23N
2938;32424;+proj=utm +zone=24 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 24N
2939;32425;+proj=utm +zone=25 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 25N
2940;32426;+proj=utm +zone=26 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 26N
2941;32427;+proj=utm +zone=27 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 27N
2942;32428;+proj=utm +zone=28 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 28N
2943;32429;+proj=utm +zone=29 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 29N
2944;32430;+proj=utm +zone=30 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 30N
2945;32431;+proj=utm +zone=31 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 31N
2946;32432;+proj=utm +zone=32 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 32N
2947;32433;+proj=utm +zone=33 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 33N
2948;32434;+proj=utm +zone=34 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 34N
2949;32435;+proj=utm +zone=35 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 35N
2950;32436;+proj=utm +zone=36 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 36N
2951;32437;+proj=utm +zone=37 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 37N
2952;32438;+proj=utm +zone=38 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 38N
2953;32439;+proj=utm +zone=39 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 39N
2954;32440;+proj=utm +zone=40 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 40N
2955;32441;+proj=utm +zone=41 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 41N
2956;32442;+proj=utm +zone=42 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 42N
2957;32443;+proj=utm +zone=43 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 43N
2958;32444;+proj=utm +zone=44 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 44N
2959;32445;+proj=utm +zone=45 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 45N
2960;32446;+proj=utm +zone=46 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 46N
2961;32447;+proj=utm +zone=47 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 47N
2962;32448;+proj=utm +zone=48 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 48N
2963;32449;+proj=utm +zone=49 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 49N
2964;32450;+proj=utm +zone=50 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 50N
2965;32451;+proj=utm +zone=51 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 51N
2966;32452;+proj=utm +zone=52 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 52N
2967;32453;+proj=utm +zone=53 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 53N
2968;32454;+proj=utm +zone=54 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 54N
2969;32455;+proj=utm +zone=55 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 55N
2970;32456;+proj=utm +zone=56 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 56N
2971;32457;+proj=utm +zone=57 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 57N
2972;32458;+proj=utm +zone=58 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 58N
2973;32459;+proj=utm +zone=59 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 59N
2974;32460;+proj=utm +zone=60 +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 60N
2975;32501;+proj=utm +zone=1 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 1S
2976;32502;+proj=utm +zone=2 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 2S
2977;32503;+proj=utm +zone=3 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 3S
2978;32504;+proj=utm +zone=4 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 4S
2979;32505;+proj=utm +zone=5 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 5S
2980;32506;+proj=utm +zone=6 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 6S
2981;32507;+proj=utm +zone=7 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 7S
2982;32508;+proj=utm +zone=8 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 8S
2983;32509;+proj=utm +zone=9 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 9S
2984;32510;+proj=utm +zone=10 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 10S
2985;32511;+proj=utm +zone=11 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 11S
2986;32512;+proj=utm +zone=12 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 12S
2987;32513;+proj=utm +zone=13 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 13S
2988;32514;+proj=utm +zone=14 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 14S
2989;32515;+proj=utm +zone=15 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 15S
2990;32516;+proj=utm +zone=16 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 16S
2991;32517;+proj=utm +zone=17 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 17S
2992;32518;+proj=utm +zone=18 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 18S
2993;32519;+proj=utm +zone=19 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 19S
2994;32520;+proj=utm +zone=20 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 20S
2995;32521;+proj=utm +zone=21 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 21S
2996;32522;+proj=utm +zone=22 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 22S
2997;32523;+proj=utm +zone=23 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 23S
2998;32524;+proj=utm +zone=24 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 24S
2999;32525;+proj=utm +zone=25 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 25S
3000;32526;+proj=utm +zone=26 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 26S
3001;32527;+proj=utm +zone=27 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 27S
3002;32528;+proj=utm +zone=28 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 28S
3003;32529;+proj=utm +zone=29 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 29S
3004;32530;+proj=utm +zone=30 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 30S
3005;32531;+proj=utm +zone=31 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 31S
3006;32532;+proj=utm +zone=32 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 32S
3007;32533;+proj=utm +zone=33 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 33S
3008;32534;+proj=utm +zone=34 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 34S
3009;32535;+proj=utm +zone=35 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 35S
3010;32536;+proj=utm +zone=36 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 36S
3011;32537;+proj=utm +zone=37 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 37S
3012;32538;+proj=utm +zone=38 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 38S
3013;32539;+proj=utm +zone=39 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 39S
3014;32540;+proj=utm +zone=40 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 40S
3015;32541;+proj=utm +zone=41 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 41S
3016;32542;+proj=utm +zone=42 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 42S
3017;32543;+proj=utm +zone=43 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 43S
3018;32544;+proj=utm +zone=44 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 44S
3019;32545;+proj=utm +zone=45 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 45S
3020;32546;+proj=utm +zone=46 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 46S
3021;32547;+proj=utm +zone=47 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 47S
3022;32548;+proj=utm +zone=48 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 48S
3023;32549;+proj=utm +zone=49 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 49S
3024;32550;+proj=utm +zone=50 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 50S
3025;32551;+proj=utm +zone=51 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 51S
3026;32552;+proj=utm +zone=52 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 52S
3027;32553;+proj=utm +zone=53 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 53S
3028;32554;+proj=utm +zone=54 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 54S
3029;32555;+proj=utm +zone=55 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 55S
3030;32556;+proj=utm +zone=56 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 56S
3031;32557;+proj=utm +zone=57 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 57S
3032;32558;+proj=utm +zone=58 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 58S
3033;32559;+proj=utm +zone=59 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 59S
3034;32560;+proj=utm +zone=60 +south +ellps=WGS72 +towgs84=0,0,1.9,0,0,0.814,-0.38 +units=m +no_defs;WGS 72BE / UTM zone 60S
3035;32601;+proj=utm +zone=1 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 1N
3036;32602;+proj=utm +zone=2 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 2N
3037;32603;+proj=utm +zone=3 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 3N
3038;32604;+proj=utm +zone=4 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 4N
3039;32605;+proj=utm +zone=5 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 5N
3040;32606;+proj=utm +zone=6 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 6N
3041;32607;+proj=utm +zone=7 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 7N
3042;32608;+proj=utm +zone=8 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 8N
3043;32609;+proj=utm +zone=9 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 9N
3044;32610;+proj=utm +zone=10 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 10N
3045;32611;+proj=utm +zone=11 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 11N
3046;32612;+proj=utm +zone=12 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 12N
3047;32613;+proj=utm +zone=13 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 13N
3048;32614;+proj=utm +zone=14 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 14N
3049;32615;+proj=utm +zone=15 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 15N
3050;32616;+proj=utm +zone=16 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 16N
3051;32617;+proj=utm +zone=17 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 17N
3052;32618;+proj=utm +zone=18 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 18N
3053;32619;+proj=utm +zone=19 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 19N
3054;32620;+proj=utm +zone=20 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 20N
3055;32621;+proj=utm +zone=21 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 21N
3056;32622;+proj=utm +zone=22 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 22N
3057;32623;+proj=utm +zone=23 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 23N
3058;32624;+proj=utm +zone=24 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 24N
3059;32625;+proj=utm +zone=25 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 25N
3060;32626;+proj=utm +zone=26 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 26N
3061;32627;+proj=utm +zone=27 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 27N
3062;32628;+proj=utm +zone=28 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 28N
3063;32629;+proj=utm +zone=29 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 29N
3064;32630;+proj=utm +zone=30 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 30N
3065;32631;+proj=utm +zone=31 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 31N
3066;32632;+proj=utm +zone=32 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 32N
3067;32633;+proj=utm +zone=33 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 33N
3068;32634;+proj=utm +zone=34 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 34N
3069;32635;+proj=utm +zone=35 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 35N
3070;32636;+proj=utm +zone=36 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 36N
3071;32637;+proj=utm +zone=37 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 37N
3072;32638;+proj=utm +zone=38 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 38N
3073;32639;+proj=utm +zone=39 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 39N
3074;32640;+proj=utm +zone=40 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 40N
3075;32641;+proj=utm +zone=41 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 41N
3076;32642;+proj=utm +zone=42 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 42N
3077;32643;+proj=utm +zone=43 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 43N
3078;32644;+proj=utm +zone=44 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 44N
3079;32645;+proj=utm +zone=45 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 45N
3080;32646;+proj=utm +zone=46 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 46N
3081;32647;+proj=utm +zone=47 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 47N
3082;32648;+proj=utm +zone=48 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 48N
3083;32649;+proj=utm +zone=49 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 49N
3084;32650;+proj=utm +zone=50 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 50N
3085;32651;+proj=utm +zone=51 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 51N
3086;32652;+proj=utm +zone=52 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 52N
3087;32653;+proj=utm +zone=53 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 53N
3088;32654;+proj=utm +zone=54 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 54N
3089;32655;+proj=utm +zone=55 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 55N
3090;32656;+proj=utm +zone=56 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 56N
3091;32657;+proj=utm +zone=57 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 57N
3092;32658;+proj=utm +zone=58 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 58N
3093;32659;+proj=utm +zone=59 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 59N
3094;32660;+proj=utm +zone=60 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 60N
3095;32661;+proj=stere +lat_0=90 +lat_ts=90 +lon_0=0 +k=0.994 +x_0=2000000 +y_0=2000000 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UPS North
3096;32662;+proj=eqc +lat_ts=0 +lon_0=0 +x_0=0 +y_0=0 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / Plate Carree
3097;32664;+proj=tmerc +lat_0=0 +lon_0=-99 +k=0.999600 +x_0=500000.001016002 +y_0=0 +ellps=WGS84 +datum=WGS84 +to_meter=0.3048006096012192 +no_defs;WGS 84 / BLM 14N (ftUS)
3098;32665;+proj=tmerc +lat_0=0 +lon_0=-93 +k=0.999600 +x_0=500000.001016002 +y_0=0 +ellps=WGS84 +datum=WGS84 +to_meter=0.3048006096012192 +no_defs;WGS 84 / BLM 15N (ftUS)
3099;32666;+proj=tmerc +lat_0=0 +lon_0=-87 +k=0.999600 +x_0=500000.001016002 +y_0=0 +ellps=WGS84 +datum=WGS84 +to_meter=0.3048006096012192 +no_defs;WGS 84 / BLM 16N (ftUS)
3100;32667;+proj=tmerc +lat_0=0 +lon_0=-81 +k=0.999600 +x_0=500000.001016002 +y_0=0 +ellps=WGS84 +datum=WGS84 +to_meter=0.3048006096012192 +no_defs;WGS 84 / BLM 17N (ftUS)
3101;32701;+proj=utm +zone=1 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 1S
3102;32702;+proj=utm +zone=2 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 2S
3103;32703;+proj=utm +zone=3 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 3S
3104;32704;+proj=utm +zone=4 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 4S
3105;32705;+proj=utm +zone=5 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 5S
3106;32706;+proj=utm +zone=6 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 6S
3107;32707;+proj=utm +zone=7 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 7S
3108;32708;+proj=utm +zone=8 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 8S
3109;32709;+proj=utm +zone=9 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 9S
3110;32710;+proj=utm +zone=10 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 10S
3111;32711;+proj=utm +zone=11 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 11S
3112;32712;+proj=utm +zone=12 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 12S
3113;32713;+proj=utm +zone=13 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 13S
3114;32714;+proj=utm +zone=14 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 14S
3115;32715;+proj=utm +zone=15 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 15S
3116;32716;+proj=utm +zone=16 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 16S
3117;32717;+proj=utm +zone=17 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 17S
3118;32718;+proj=utm +zone=18 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 18S
3119;32719;+proj=utm +zone=19 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 19S
3120;32720;+proj=utm +zone=20 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 20S
3121;32721;+proj=utm +zone=21 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 21S
3122;32722;+proj=utm +zone=22 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 22S
3123;32723;+proj=utm +zone=23 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 23S
3124;32724;+proj=utm +zone=24 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 24S
3125;32725;+proj=utm +zone=25 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 25S
3126;32726;+proj=utm +zone=26 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 26S
3127;32727;+proj=utm +zone=27 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 27S
3128;32728;+proj=utm +zone=28 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 28S
3129;32729;+proj=utm +zone=29 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 29S
3130;32730;+proj=utm +zone=30 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 30S
3131;32731;+proj=utm +zone=31 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 31S
3132;32732;+proj=utm +zone=32 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 32S
3133;32733;+proj=utm +zone=33 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 33S
3134;32734;+proj=utm +zone=34 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 34S
3135;32735;+proj=utm +zone=35 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 35S
3136;32736;+proj=utm +zone=36 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 36S
3137;32737;+proj=utm +zone=37 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 37S
3138;32738;+proj=utm +zone=38 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 38S
3139;32739;+proj=utm +zone=39 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 39S
3140;32740;+proj=utm +zone=40 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 40S
3141;32741;+proj=utm +zone=41 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 41S
3142;32742;+proj=utm +zone=42 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 42S
3143;32743;+proj=utm +zone=43 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 43S
3144;32744;+proj=utm +zone=44 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 44S
3145;32745;+proj=utm +zone=45 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 45S
3146;32746;+proj=utm +zone=46 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 46S
3147;32747;+proj=utm +zone=47 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 47S
3148;32748;+proj=utm +zone=48 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 48S
3149;32749;+proj=utm +zone=49 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 49S
3150;32750;+proj=utm +zone=50 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 50S
3151;32751;+proj=utm +zone=51 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 51S
3152;32752;+proj=utm +zone=52 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 52S
3153;32753;+proj=utm +zone=53 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 53S
3154;32754;+proj=utm +zone=54 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 54S
3155;32755;+proj=utm +zone=55 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 55S
3156;32756;+proj=utm +zone=56 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 56S
3157;32757;+proj=utm +zone=57 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 57S
3158;32758;+proj=utm +zone=58 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 58S
3159;32759;+proj=utm +zone=59 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 59S
3160;32760;+proj=utm +zone=60 +south +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UTM zone 60S
3161;32761;+proj=stere +lat_0=-90 +lat_ts=-90 +lon_0=0 +k=0.994 +x_0=2000000 +y_0=2000000 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / UPS South
3162;32766;+proj=tmerc +lat_0=0 +lon_0=36 +k=0.999600 +x_0=500000 +y_0=10000000 +ellps=WGS84 +datum=WGS84 +units=m +no_defs;WGS 84 / TM 36 SE
3164;31251;+proj=tmerc +lat_0=0 +lon_0=10.33333333333333 +k=1.000000 +x_0=0 +y_0=-5000000 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI (Ferro) / Austria GK  West Zone
3165;31252;+proj=tmerc +lat_0=0 +lon_0=13.33333333333333 +k=1.000000 +x_0=0 +y_0=-5000000 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI (Ferro) / Austria GK Central Zone
3166;31253;+proj=tmerc +lat_0=0 +lon_0=16.33333333333333 +k=1.000000 +x_0=0 +y_0=-5000000 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI (Ferro) / Austria GK East Zone
3167;31254;+proj=tmerc +lat_0=0 +lon_0=10.33333333333333 +k=1.000000 +x_0=0 +y_0=-5000000 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI / Austria GK  West Zone
3168;31255;+proj=tmerc +lat_0=0 +lon_0=13.33333333333333 +k=1.000000 +x_0=0 +y_0=-5000000 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI / Austria GK Central Zone
3169;31256;+proj=tmerc +lat_0=0 +lon_0=16.33333333333333 +k=1.000000 +x_0=0 +y_0=-5000000 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI / Austria GK East Zone
3170;31257;+proj=tmerc +lat_0=0 +lon_0=10.33333333333333 +k=1.000000 +x_0=150000 +y_0=-5000000 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI / Austria GK BMN-M28
3171;31258;+proj=tmerc +lat_0=0 +lon_0=13.33333333333333 +k=1.000000 +x_0=450000 +y_0=-5000000 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI / Austria GK BMN-M31
3172;31259;+proj=tmerc +lat_0=0 +lon_0=16.33333333333333 +k=1.000000 +x_0=750000 +y_0=-5000000 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;MGI / Austria GK BMN-M34
3173;3416;+proj=lcc +lat_1=49 +lat_2=46 +lat_0=47.5 +lon_0=13.33333333333333 +x_0=400000 +y_0=400000 +ellps=bessel +towgs84=577.326,90.129,463.919,5.137,1.474,5.297,2.4232 +units=m +no_defs;ETRS89 / Austria Lambert
3174;3857;+proj=merc +a=6378137 +b=6378137 +lat_ts=0.0 +lon_0=0.0 +x_0=0.0 +y_0=0 +k=1.0 +units=m +nadgrids=@null +wktext +no_defs;Web Mercator";

            return csv.Replace("\r", "").Split('\n');
        }
    }

    #endregion

    #region Helper

    private string GetQoutedWKTParameter(string wkt, string pre, string quote1, string quote2)
    {
        try
        {
            int pos = wkt.IndexOf(pre);
            if (pos != -1)
            {
                int posS = wkt.IndexOf(quote1, pos + pre.Length);
                int posE = wkt.IndexOf(quote2, posS + 1);

                return wkt.Substring(posS + 1, posE - posS - 1);
            }
        }
        catch { }
        return String.Empty;
    }

    #endregion
}
