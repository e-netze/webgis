using E.Standard.GeoRSS.Abstraction;
using System.Data;

namespace E.Standard.GeoRSS20;

public class DataTableItemCursor : IItemCursor
{
    private DataTable _tab = null;
    private int _pos = 0;

    public DataTableItemCursor(DataTable tab)
    {
        _tab = tab;
    }

    #region IItemCursor Member

    public IItem NextItem
    {
        get
        {
            if (_tab == null || _tab.Rows.Count <= _pos)
            {
                return null;
            }

            DataRow row = _tab.Rows[_pos++];
            GeoRSSItem item = new GeoRSSItem();

            foreach (DataColumn column in _tab.Columns)
            {
                object obj = row[column.ColumnName];
                if (obj != null)
                {
                    item[column.ColumnName] = obj.ToString();
                }
                else
                {
                    item[column.ColumnName] = null;
                }
            }

            return item;
        }
    }

    #endregion

    #region IDisposable Member

    public void Dispose()
    {

    }

    #endregion
}
