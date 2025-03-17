using System.Data;
using System.IO;

namespace E.Standard.Esri.Shapefile;

class DBFDataReader
{
    private DBFFile _file;
    private StreamReader _sr = null;
    private BinaryReader _br = null;
    private DataTable _tab;

    public DBFDataReader(DBFFile file, string fieldnames)
    {
        if (file == null)
        {
            return;
        }

        _file = file;
        _sr = new StreamReader(_file.Filename);
        _br = new BinaryReader(_sr.BaseStream);

        string[] names = null;
        fieldnames = fieldnames.Replace(" ", "");
        if (fieldnames != "*")
        {
            names = fieldnames.Split(',');
        }

        _tab = _file.DataTable(names);
    }

    public DataTable AllRecords
    {
        get
        {
            if (_file == null)
            {
                return null;
            }

            _file.Records(_tab, _br);
            return _tab;
        }
    }

    public void AddRecord(uint index)
    {
        _file.Record(index, _tab, _br);
    }
    public void Clear()
    {
        _tab.Rows.Clear();
    }
    public DataTable Table
    {
        get { return _tab; }
    }
    public void Dispose()
    {
        if (_tab != null)
        {
            _tab.Rows.Clear();
            _tab.Dispose();
            _tab = null;
        }
        if (_sr != null)
        {
            _sr.Close();
            _sr = null;
        }
    }
}
