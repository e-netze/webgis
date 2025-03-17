using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace E.Standard.DocumentReader;

public class CSVReader
{
    public DataSet Read(Stream stream)
    {
        var data = new byte[stream.Length];
        using (var byteReader = new BinaryReader(stream))
        {
            byteReader.Read(data, 0, data.Length);
        }

        data = ToUtf8(data);

        using (var textReader = new StreamReader(new MemoryStream(data), Encoding.UTF8, true))
        {
            string line;
            bool first = true;
            char separator = ';';

            DataSet dataset = new DataSet();
            DataTable table = new DataTable("CSV Tabelle");
            dataset.Tables.Add(table);

            while ((line = textReader.ReadLine()) != null)
            {
                if (String.IsNullOrWhiteSpace(line.Trim()))
                {
                    continue;
                }

                if (first)
                {
                    first = false;

                    if (line.Contains("\t"))
                    {
                        separator = '\t';
                    }

                    string[] columns = line.Split(separator);

                    foreach (string column in columns)
                    {
                        table.Columns.Add(new DataColumn(column.Trim()));
                    }
                }
                else
                {
                    string[] cells = line.Split(separator);
                    DataRow row = table.NewRow();

                    for (int i = 0, to = Math.Min(cells.Length, table.Columns.Count); i < to; i++)
                    {
                        row[i] = ParseCsvValue(cells[i]);
                    }

                    table.Rows.Add(row);
                }
            }

            return dataset;
        }
    }

    private string ParseCsvValue(string value)
    {
        value = value.Trim();
        if (value.StartsWith("="))
        {
            value = value.Substring(1);
            if (value.StartsWith("\"") && value.EndsWith("\""))
            {
                value = value.Substring(1, value.Length - 2);
            }
        }

        return value;
    }

    private byte[] ToUtf8(byte[] data)
    {
        var candidates = new Encoding[]{
            Encoding.UTF8,
#pragma warning disable SYSLIB0001
            Encoding.UTF7,
#pragma warning restore SYSLIB0001
            Encoding.GetEncoding("ISO-8859-1"),
            Encoding.UTF32
        };

        foreach (var candidate in candidates)
        {
            try
            {
                string text = candidate.GetString(data);
                if (text.Where(c => Convert.ToInt32(c) > 255).Count() == 0)
                {
                    return Encoding.UTF8.GetBytes(text);
                }
            }
            catch { }
        }

        return data;
    }
}
