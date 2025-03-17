using System.Data;
using System.IO;

namespace E.Standard.DocumentReader;

public class SimpleTextReader
{
    public DataSet ToDataset(Stream stream)
    {
        using (var textReader = new StreamReader(stream))
        {
            DataSet dataset = new DataSet();
            DataTable table = new DataTable("Textdokument");
            dataset.Tables.Add(table);
            table.Columns.Add(new DataColumn("Text"));

            string line;
            while ((line = textReader.ReadLine()) != null)
            {
                DataRow row = table.NewRow();
                row[0] = line;
                table.Rows.Add(row);
            }

            return dataset;
        }
    }
}
