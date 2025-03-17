using E.Standard.Esri.Shapefile.IO;
using E.Standard.WebMapping.Core;
using E.Standard.WebMapping.Core.Abstraction;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace E.Standard.Esri.Shapefile;

class DBFFile
{
    private string _name;
    private DBFFileHader _header;
    private List<FieldDescriptor> _fields;
    private Encoding _encoder = null;
    private char[] _trims = { '\0', ' ' };
    private static IFormatProvider _nhi = System.Globalization.CultureInfo.InvariantCulture.NumberFormat;
    private string _idField = "FID";
    private readonly IStreamProvider _streamProvider;

    public DBFFile(IStreamProvider streamProvider, string name)
    {
        try
        {
            _streamProvider = streamProvider;
            _name = $"{name}.dbf";

            BinaryReader br = new BinaryReader(streamProvider[_name]);

            _header = new DBFFileHader(br);
            _fields = new List<FieldDescriptor>();
            for (int i = 0; i < _header.FieldsCount; i++)
            {
                FieldDescriptor field = new FieldDescriptor(br);
                _fields.Add(field);
            }

            int c = 1;
            string idFieldName = _idField;
            while (HasField(idFieldName))
            {
                idFieldName = _idField + "_" + c++;
            }

            _idField = idFieldName;

            _encoder = null;
            try
            {
                switch ((CodePage)_header.LanguageDriver)
                {
                    case CodePage.DOS_USA:
                        _encoder = EncodingFromCodePage(437);
                        break;
                    case CodePage.DOS_Multilingual:
                        _encoder = EncodingFromCodePage(850);
                        break;
                    case CodePage.Windows_ANSI:
                        _encoder = EncodingFromCodePage(1252);
                        break;
                    case CodePage.EE_MS_DOS:
                        _encoder = EncodingFromCodePage(852);
                        break;
                    case CodePage.Nordic_MS_DOS:
                        _encoder = EncodingFromCodePage(865);
                        break;
                    case CodePage.Russian_MS_DOS:
                        _encoder = EncodingFromCodePage(866);
                        break;
                    case CodePage.Windows_EE:
                        _encoder = EncodingFromCodePage(1250);
                        break;
                    case CodePage.UTF_7:
#pragma warning disable SYSLIB0001
                        _encoder = new UTF7Encoding();
#pragma warning restore SYSLIB0001
                        break;
                }

                //
                // ESRI Shape ignorien anscheined immer den DBF Standard
                // und verwenden IMMER ISO-8859-1
                // https://gis.stackexchange.com/questions/3529/which-character-encoding-is-used-by-the-dbf-file-in-shapefiles
                //
                _encoder = Encoding.GetEncoding("ISO-8859-1");
            }
            catch { }
            if (_encoder == null)
            {
                _encoder = new UTF8Encoding();
            }


        }
        catch
        {

        }
    }

    private Encoding EncodingFromCodePage(int codePage)
    {
        foreach (EncodingInfo ei in Encoding.GetEncodings())
        {
            if (ei.CodePage == codePage)
            {
                return ei.GetEncoding();
            }
        }
        return null;
    }


    private bool HasField(string name)
    {
        foreach (FieldDescriptor fd in _fields)
        {
            if (fd.FieldName == name)
            {
                return true;
            }
        }
        return false;
    }
    public string Filename
    {
        get { return _name; }
    }

    internal DataTable DataTable()
    {
        return DataTable(null);
    }

    internal DataTable DataTable(string[] fieldnames)
    {
        DataTable tab = new DataTable();

        if (fieldnames != null)
        {
            foreach (string fieldname in fieldnames)
            {
                if (fieldname == _idField)
                {
                    tab.Columns.Add(_idField, typeof(uint));
                }
                foreach (FieldDescriptor field in _fields)
                {
                    if (field.FieldName == fieldname)
                    {
                        if (tab.Columns[fieldname] == null)
                        {
                            tab.Columns.Add(fieldname, DataType(field));
                        }
                    }
                }
            }
        }
        else
        {
            tab.Columns.Add(_idField, typeof(uint));
            foreach (FieldDescriptor field in _fields)
            {
                if (tab.Columns[field.FieldName] == null)
                {
                    tab.Columns.Add(field.FieldName, DataType(field));
                }
            }
        }

        return tab;
    }
    private Type DataType(FieldDescriptor fd)
    {
        switch (fd.FieldType)
        {
            case 'C': return typeof(string);
            case 'F':
            case 'N':
                if (fd.DecimalCount == 0)
                {
                    if (fd.FieldLength <= 6)
                    {
                        return typeof(short);
                    }

                    if (fd.FieldLength <= 9)
                    {
                        return typeof(int);
                    }

                    return typeof(long);
                }
                else // if( fd.DecimalCount==9 && fd.FieldLength==31 )
                {
                    if (fd.DecimalCount <= 9)
                    {
                        return typeof(float);
                    }

                    return typeof(double);
                }
            case 'L': return typeof(bool);
            case 'D': return typeof(DateTime);
            case 'I': return typeof(int);
            case 'O': return typeof(double);
            case '+': return typeof(int); // Autoincrement
            default: return typeof(string);
        }
    }
    private WebMapping.Core.FieldType FieldType(FieldDescriptor fd)
    {
        switch (fd.FieldType)
        {
            case 'C': return WebMapping.Core.FieldType.String;
            case 'F':
            case 'N':
                if (fd.DecimalCount == 0)
                {
                    if (fd.FieldLength <= 6)
                    {
                        return WebMapping.Core.FieldType.SmallInteger;
                    }

                    if (fd.FieldLength <= 9)
                    {
                        return WebMapping.Core.FieldType.Interger;
                    }

                    return WebMapping.Core.FieldType.BigInteger;
                }
                else  // if( fd.DecimalCount==9 && fd.FieldLength==31 ) 
                {
                    if (fd.DecimalCount <= 9)
                    {
                        return WebMapping.Core.FieldType.Float;
                    }

                    return WebMapping.Core.FieldType.Double;
                }
            case 'L': return WebMapping.Core.FieldType.Boolean;
            case 'D': return WebMapping.Core.FieldType.Date;
            case 'I': return WebMapping.Core.FieldType.Interger;
            case 'O': return WebMapping.Core.FieldType.Double;
            case '+': return WebMapping.Core.FieldType.Interger; // Autoincrement
            default: return WebMapping.Core.FieldType.String;
        }
    }

    public DataTable Record(uint index)
    {
        return Record(index, "*");
    }

    public DataTable Record(uint index, string fieldnames)
    {
        StreamReader sr = new StreamReader(_name);
        BinaryReader br = new BinaryReader(sr.BaseStream);

        string[] names = null;
        fieldnames = fieldnames.Replace(" ", "");
        if (fieldnames != "*")
        {
            names = fieldnames.Split(',');
        }

        DataTable tab = DataTable(names);
        Record(index, tab, br);

        sr.Close();
        return tab;
    }

    internal void Record(uint index, DataTable tab, BinaryReader br)
    {
        if (index > _header.recordCount || index < 1)
        {
            return;
        }

        br.BaseStream.Position = _header.headerLength + _header.LegthOfEachRecord * (index - 1);

        char deleted = br.ReadChar();
        if (deleted != ' ')
        {
            return;
        }

        DataRow row = tab.NewRow();
        foreach (FieldDescriptor field in _fields)
        {
            if (tab.Columns[field.FieldName] == null)
            {
                br.BaseStream.Position += field.FieldLength;
                continue;
            }

            switch (field.FieldType)
            {
                case 'C':
                    row[field.FieldName] = _encoder.GetString(br.ReadBytes(field.FieldLength)).TrimEnd(_trims);
                    break;
                case 'F':
                case 'N':
                    string str2 = _encoder.GetString(br.ReadBytes(field.FieldLength)).TrimEnd(_trims);
                    if (str2 != "")
                    {
                        try
                        {
                            if (field.DecimalCount == 0)
                            {
                                row[field.FieldName] = Convert.ToInt64(str2);
                            }
                            else
                            {
                                row[field.FieldName] = double.Parse(str2, _nhi);
                            }
                        }
                        catch { }
                    }
                    break;
                case '+':
                case 'I':
                    row[field.FieldName] = br.ReadInt32();
                    break;
                case 'O':
                    row[field.FieldName] = br.ReadDouble();
                    break;
                case 'L':
                    char c = br.ReadChar();
                    if (c == 'Y' || c == 'y' ||
                        c == 'T' || c == 't')
                    {
                        row[field.FieldName] = true;
                    }
                    else if (c == 'N' || c == 'n' ||
                        c == 'F' || c == 'f')
                    {
                        row[field.FieldName] = false;
                    }
                    else
                    {
                        row[field.FieldName] = null;
                    }

                    break;
                case 'D':
                    string date = _encoder.GetString(br.ReadBytes(field.FieldLength)).TrimEnd(_trims);
                    if (date.Length == 8)
                    {
                        int y = int.Parse(date.Substring(0, 4));
                        int m = int.Parse(date.Substring(4, 2));
                        int d = int.Parse(date.Substring(6, 2));
                        DateTime td = new DateTime(y, m, d);
                        row[field.FieldName] = td;
                    }
                    break;
            }
        }
        if (tab.Columns[_idField] != null)
        {
            row[_idField] = index;
        }

        tab.Rows.Add(row);
    }

    internal void Records(DataTable tab, BinaryReader br)
    {
        uint rowCount = _header.recordCount;

        for (uint i = 0; i < rowCount; i++)
        {
            br.BaseStream.Position = _header.headerLength + _header.LegthOfEachRecord * (i);

            char deleted = br.ReadChar();
            if (deleted != ' ')
            {
                continue;
            }

            DataRow row = tab.NewRow();

            foreach (FieldDescriptor field in _fields)
            {
                if (tab.Columns[field.FieldName] == null)
                {
                    br.BaseStream.Position += field.FieldLength;
                    continue;
                }

                switch (field.FieldType)
                {
                    case 'C':
                        row[field.FieldName] = _encoder.GetString(br.ReadBytes(field.FieldLength)).TrimEnd(_trims);
                        break;
                    case 'F':
                    case 'N':
                        string str2 = _encoder.GetString(br.ReadBytes(field.FieldLength)).TrimEnd(_trims);
                        if (str2 != "")
                        {
                            try
                            {
                                if (field.DecimalCount == 0)
                                {
                                    row[field.FieldName] = long.Parse(str2);
                                }
                                else
                                {
                                    row[field.FieldName] = double.Parse(str2, _nhi);
                                }
                            }
                            catch { }
                        }
                        break;
                    case '+':
                    case 'I':
                        row[field.FieldName] = br.ReadInt32();
                        break;
                    case 'O':
                        row[field.FieldName] = br.ReadDouble();
                        break;
                    case 'L':
                        char c = br.ReadChar();
                        if (c == 'Y' || c == 'y' ||
                            c == 'T' || c == 't')
                        {
                            row[field.FieldName] = true;
                        }
                        else if (c == 'N' || c == 'n' ||
                            c == 'F' || c == 'f')
                        {
                            row[field.FieldName] = false;
                        }
                        else
                        {
                            row[field.FieldName] = null;
                        }

                        break;
                    case 'D':
                        string date = _encoder.GetString(br.ReadBytes(field.FieldLength)).TrimEnd(_trims);
                        if (date.Length == 8)
                        {
                            int y = int.Parse(date.Substring(0, 4));
                            int m = int.Parse(date.Substring(4, 2));
                            int d = int.Parse(date.Substring(6, 2));
                            DateTime td = new DateTime(y, m, d);
                            row[field.FieldName] = td;
                        }
                        break;
                }
            }
            if (tab.Columns[_idField] != null)
            {
                row[_idField] = i + 1;
            }

            tab.Rows.Add(row);
        }
    }

    #region Writer

    internal static bool Create(IStreamProvider streamProvider, string name, List<IField> fields)
    {
        try
        {
            BinaryWriter bw = new BinaryWriter(streamProvider.CreateStream($"{name}.dbf"));

            bool ret = DBFFileHader.Write(bw, fields);

            //bw.Flush();

            return ret;
        }
        catch
        {
            return false;
        }
    }

    internal bool WriteRecord(uint index, Feature feature)
    {
        if (feature == null)
        {
            return false;
        }

        BinaryWriter bw = null;
        BinaryReader br = null;
        try
        {
            bw = new BinaryWriter(_streamProvider[_name]);

            long pos0 = bw.BaseStream.Position = _header.headerLength + _header.LegthOfEachRecord * (index - 1);
            long posX = 1;

            bw.Write((byte)' ');  // deleted Flag

            string str;
            foreach (FieldDescriptor fd in _fields)
            {
                object obj = feature[fd.FieldName];
                if (obj == null || obj == DBNull.Value)
                {
                    WriteNull(fd, bw);
                }
                else
                {
                    try
                    {
                        switch (fd.FieldType)
                        {
                            case 'C':
                                str = obj.ToString().PadRight(fd.FieldLength, ' ');
                                WriteString(fd, bw, str);
                                break;
                            case 'N':
                            case 'F':
                                if (fd.DecimalCount == 0)
                                {
                                    str = Convert.ToInt32(obj).ToString();
                                    str = str.PadLeft(fd.FieldLength, ' ');
                                    WriteString(fd, bw, str);
                                }
                                else
                                {
                                    str = Convert.ToDouble(obj).ToString(_nhi);
                                    str = str.PadLeft(fd.FieldLength, ' ');
                                    WriteString(fd, bw, str);
                                }
                                break;
                            case '+':
                            case 'I':
                                bw.Write(Convert.ToInt32(obj));
                                break;
                            case 'O':
                                bw.Write(Convert.ToDouble(obj));
                                break;
                            case 'L':
                                bool v = Convert.ToBoolean(obj);
                                str = (v) ? "T" : "F";
                                WriteString(fd, bw, str);
                                break;
                            case 'D':
                                DateTime td = Convert.ToDateTime(obj);
                                str = td.Year.ToString().PadLeft(4, '0') +
                                      td.Month.ToString().PadLeft(2, '0') +
                                      td.Day.ToString().PadLeft(2, '0');
                                WriteString(fd, bw, str);
                                break;
                            default:
                                WriteNull(fd, bw);
                                break;
                        }
                    }
                    catch
                    {
                        WriteNull(fd, bw);
                    }
                }
                posX += fd.FieldLength;
                bw.BaseStream.Position = pos0 + posX;
            }

            br = new BinaryReader(_streamProvider[_name]);
            br.BaseStream.Position = 4;
            uint recCount = (uint)br.ReadInt32();

            DateTime now = DateTime.Now;
            bw.BaseStream.Position = 1;
            bw.Write((byte)(now.Year - 1900));
            bw.Write((byte)now.Month);
            bw.Write((byte)now.Day);

            bw.Write((int)recCount + 1);

            return true;
        }
        catch (Exception ex)
        {
            string err = ex.Message;
            return false;
        }
        finally
        {
            bw.Flush();
        }
    }

    private void WriteNull(FieldDescriptor fd, BinaryWriter bw)
    {
        for (int i = 0; i < fd.FieldLength; i++)
        {
            bw.Write((byte)' ');
        }
    }
    private void WriteString(FieldDescriptor fd, BinaryWriter bw, string str)
    {
        byte[] bytes = _encoder.GetBytes(str);
        for (int i = 0; i < fd.FieldLength; i++)
        {
            if (i < bytes.Length)
            {
                bw.Write(bytes[i]);
            }
            else
            {
                bw.Write((byte)0);
            }
        }
    }

    #endregion

    public List<IField> Fields
    {
        get
        {
            List<IField> fields = new List<IField>();

            // ID
            Field field = new Field(_idField, WebMapping.Core.FieldType.ID);
            fields.Add(field);

            foreach (FieldDescriptor fd in _fields)
            {
                field = new Field(fd.FieldName, FieldType(fd));
                //field.name = fd.FieldName;
                //field.size = fd.FieldLength;
                //field.precision = fd.DecimalCount;
                //field.type = FieldType(fd);

                fields.Add(field);
            }

            return fields;
        }
    }


}
