using ExcelDataReader;
using System.Data;
using System.IO;

namespace E.Standard.DocumentReader;

public class DocumentReader
{
    public DocumentReader()
    {

    }

    public DataSet ReadDocument(string filePath)
    {
        return ReadDocument(filePath, new FileStream(filePath, FileMode.Open));
    }

    public DataSet ReadDocument(string fileName, Stream stream)
    {
        switch (fileName.ToLower().Substring(fileName.LastIndexOf(".")))
        {
            case ".xls":
                return ReadExcelDocument(stream);
            case ".xlsx":
                return ReadOpenExcelDocument(stream);
            case ".csv":
                return new CSVReader().Read(stream);
            case ".txt":
                return new SimpleTextReader().ToDataset(stream);
        }

        return null;
    }

    private DataSet ReadExcelDocument(Stream stream)
    {
        IExcelDataReader excelReader = ExcelReaderFactory.CreateBinaryReader(stream);

        //excelReader.IsFirstRowAsColumnNames = true;
        DataSet result = excelReader.AsDataSet(new ExcelDataSetConfiguration()
        {

        });

        return result;

    }

    private DataSet ReadOpenExcelDocument(Stream stream)
    {
        IExcelDataReader excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);

        //excelReader.IsFirstRowAsColumnNames = true;
        DataSet result = excelReader.AsDataSet(new ExcelDataSetConfiguration()
        {

            // Gets or sets a value indicating whether to set the DataColumn.DataType 
            // property in a second pass.
            UseColumnDataType = true,

            // Gets or sets a callback to obtain configuration options for a DataTable. 
            ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
            {

                // Gets or sets a value indicating the prefix of generated column names.
                EmptyColumnNamePrefix = "Column",

                // Gets or sets a value indicating whether to use a row from the 
                // data as column names.
                UseHeaderRow = true,

                // Gets or sets a callback to determine which row is the header row. 
                // Only called when UseHeaderRow = true.
                //ReadHeaderRow = (rowReader) => {
                //    // F.ex skip the first row and use the 2nd row as column headers:
                //    rowReader.Read();
                //},

                // Gets or sets a callback to determine whether to include the 
                // current row in the DataTable.
                //FilterRow = (rowReader) => {
                //    return true;
                //},

                //// Gets or sets a callback to determine whether to include the specific
                //// column in the DataTable. Called once per column after reading the 
                //// headers.
                //FilterColumn = (rowReader, columnIndex) => {
                //    return true;
                //}
            }
        });

        return result;
    }
}
