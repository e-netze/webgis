using E.Standard.WebMapping.Core.Editing;
using E.Standard.WebMapping.Core.Geometry;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace E.Standard.OracleWorkspace;

public class Workspace : IFeatureWorkspaceSpatialReference, IFeatureWorkspaceGeometryOperations
{
    private string _connectionString = String.Empty;
    private string _tableName = String.Empty;
    private string _tableId = String.Empty, _tableShape = String.Empty;
    private int _id = -1;
    private Dictionary<string, string> _fieldValues = null;

    #region IFeatureWorksapce

    // Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=host_server_name)(PORT=a_port_number)))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=service_name)));User Id=user_id;Password=password;layer=[LAYER];layer_id=[LAYER_ID];layer_shape=[LAYER_SHAPE]
    public string ConnectionString
    {
        set
        {
            _connectionString = value;

            _tableName = RemoveConnectionStringParameter("layer");
            _tableId = RemoveConnectionStringParameter("layer_id");
            _tableShape = RemoveConnectionStringParameter("layer_shape");

            if (String.IsNullOrWhiteSpace(_tableName))
            {
                throw new ArgumentException("Oracle.FeateatureWorkspace.Workspace.ConnectionString: Paramter table is missing");
            }

            if (String.IsNullOrWhiteSpace(_tableId))
            {
                throw new ArgumentException("Oracle.FeateatureWorkspace.Workspace.ConnectionString: Paramter table_id is missing");
            }

            if (String.IsNullOrWhiteSpace(_tableShape))
            {
                throw new ArgumentException("Oracle.FeateatureWorkspace.Workspace.ConnectionString: Paramter table_shape is missing");
            }
        }
    }

    public string LastErrorMessage
    {
        get; private set;
    }

    public List<string> FieldNames
    {
        get
        {
            throw new NotImplementedException();
        }
    }

    public string CurrentFeatureGeometry
    {
        get; set;
    }

    public bool RebuildSpatialIndex
    {
        get
        {
            return false;
        }

        set
        {
        }
    }

    public string VersionName
    {
        get
        {
            return String.Empty;
        }

        set
        {

        }
    }

    public bool Connect(SqlCommand mode)
    {
        return true;
    }

    public Task<bool> DeleteCurrentFeature()
    {
        if (_id < 0)
        {
            return Task.FromResult(false);
        }

        using (var connection = new OracleConnection(_connectionString))
        {
            connection.Open();

            var command = new OracleCommand("delete from " + _tableName + " where " + _tableId + "=" + _id, connection);
            command.ExecuteNonQuery();
        }

        return Task.FromResult(true);
    }

    //private Task<bool> Delete(string where)
    //{
    //    using (var connection = new OracleConnection(_connectionString))
    //    {
    //        connection.Open();

    //        var command = new OracleCommand("delete from " + _tableName + " where " + where);
    //        command.ExecuteNonQuery();
    //    }

    //    return Task.FromResult(true);
    //}

    public Task<bool> Commit() => Task.FromResult(true);

    public void DisConnect()
    {
    }

    public string GetCurrentFeatureAttributValue(string name)
    {
        if (_fieldValues == null || !_fieldValues.ContainsKey(name))
        {
            return null;
        }

        return _fieldValues[name];
    }

    public bool CurrentFeatureHasAttribute(string name) => _fieldValues != null && _fieldValues.ContainsKey(name);

    public bool MoveTo(int ID)
    {
        _id = ID;

        return true;
    }

    public bool SetCurrentFeatureAttribute(string name, string value)
    {
        if (_fieldValues == null)
        {
            _fieldValues = new Dictionary<string, string>();
        }

        if (_fieldValues.ContainsKey(name))
        {
            return false;
        }

        _fieldValues.Add(name, value);

        return true;
    }

    async public Task<bool> StoreCurrentFeature()
    {
        // Kunde: Es soll auch möglich sein, dass man keine Attribute angibt...
        //if (_fieldValues == null || _fieldValues.Count() == 0)
        //    return false;

        if (_id > 0)
        {
            return await Update(_tableId + "=" + _id);
        }

        using (var connection = new OracleConnection(_connectionString))
        {
            connection.Open();

            string fields = _fieldValues != null ? String.Join(",", _fieldValues.Keys) : String.Empty;
            StringBuilder values = new StringBuilder();

            var command = new OracleCommand();
            command.Connection = connection;

            int valuesCounter = 0;

            if (_fieldValues != null)
            {
                foreach (var field in _fieldValues.Keys)
                {
                    if (valuesCounter > 0)
                    {
                        values.Append(",");
                    }

                    var value = _fieldValues[field];
                    if (IsOracleCommand(value))
                    {
                        values.Append(value);
                    }
                    else
                    {
                        values.Append(":p" + valuesCounter);
                        command.Parameters.Add(new OracleParameter(":p" + valuesCounter, value));
                    }
                    valuesCounter++;
                }
            }

            string sdoGeometry = this.SdoGeometryString;
            if (!String.IsNullOrWhiteSpace(sdoGeometry))
            {
                fields += (fields.Length > 0 ? "," : "") + _tableShape;
                values.Append((values.Length > 0 ? "," : "") + sdoGeometry);
            }

            if (fields.Length == 0)
            {
                throw new Exception("No fields found to update");
            }

            //command.CommandText = "SELECT * FROM V$NLS_PARAMETERS";
            //using (var datapter=new OracleDataAdapter(command))
            //{
            //    System.Data.DataTable tab = new System.Data.DataTable("dings");
            //    datapter.Fill(tab);
            //}

            //command.CommandText = "alter session set NLS_TIMESTAMP_FORMAT='DD.MM.YYYY HH24:MI:SS.FF'";
            //command.ExecuteNonQuery();
            //TO_DATE('1.1.2018 ....'DD.MM.YYYY HH24:MI:SS.FF'), 

            command.CommandText = "insert into " + _tableName + " (" + fields + ") values  (" + values.ToString() + ")";
            command.ExecuteNonQuery();
        }

        return true;
    }

    private Task<bool> Update(string where)
    {
        //if (_fieldValues == null || _fieldValues.Count() == 0)
        //    return false;

        using (var connection = new OracleConnection(_connectionString))
        {
            connection.Open();

            var command = new OracleCommand();
            command.Connection = connection;

            StringBuilder set = new StringBuilder();
            int valuesCounter = 0;

            if (_fieldValues != null)
            {
                foreach (var field in _fieldValues.Keys)
                {
                    if (valuesCounter > 0)
                    {
                        set.Append(",");
                    }

                    set.Append(field + "=:p" + valuesCounter);

                    command.Parameters.Add(new OracleParameter(":p" + valuesCounter, _fieldValues[field]));

                    valuesCounter++;
                }
            }

            string sdoGeometry = this.SdoGeometryString;
            if (!String.IsNullOrWhiteSpace(sdoGeometry))
            {
                set.Append((set.Length > 0 ? "," : "") + _tableShape + "=" + sdoGeometry);

                //set.Append((set.Length > 0 ? "," : "") + _tableShape + "=:p_shape");
                //var parameter = new OracleParameter(":p_shape", OracleDbType.Clob, sdoGeometry.Length);
                //parameter.Value = sdoGeometry;
                //command.Parameters.Add(parameter);
            }
            ;

            if (String.IsNullOrWhiteSpace(where))
            {
                where = _tableId + "=" + _id;
            }

            if (set.Length == 0)
            {
                throw new Exception("No fields found to update");
            }

            command.CommandText = "update " + _tableName + " set " + set.ToString() + " where " + where;
            command.ExecuteNonQuery();
        }

        return Task.FromResult(true);
    }

    #endregion

    #region IFeatureWorkspace2

    public int SrsId
    {
        get; set;
    }

    #endregion

    #region IFeatureWorkspaceGeometryOperations

    public bool ClosePolygonRings => true;
    public bool CleanRings => true;

    #endregion

    #region Helper

    private string SdoGeometryString
    {
        get
        {
            if (String.IsNullOrWhiteSpace(this.CurrentFeatureGeometry))
            {
                return String.Empty;
            }

            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<xml>" + this.CurrentFeatureGeometry + "</xml>");

            var shape = Shape.FromArcXML(doc.SelectSingleNode("xml").FirstChild, null);

            return Types.SdoGeometry.FromGeometry(shape, this.SrsId > 0 ? SrsId : null);
        }
    }

    private string RemoveConnectionStringParameter(string parameter)
    {
        if (_connectionString.Contains(";" + parameter + "="))
        {
            int pos0 = _connectionString.IndexOf(";" + parameter + "=");
            int pos1 = _connectionString.IndexOf(";", _connectionString.IndexOf(";" + parameter + "=") + 1);
            string parameterAndValue = pos1 > 0 ? _connectionString.Substring(pos0 + 1, pos1 - pos0 - 1) : _connectionString.Substring(pos0 + 1);

            _connectionString = _connectionString.Substring(0, pos0) + (pos1 > 0 ? _connectionString.Substring(pos1) : String.Empty);

            return parameterAndValue.Substring(parameter.Length + 1).Trim();
        }

        return String.Empty;
    }

    private bool IsOracleCommand(string val)
    {
        if (val == null || !val.Trim().EndsWith(")"))
        {
            return false;
        }

        if (val.ToLower().Trim().StartsWith("to_date("))
        {
            return true;
        }

        if (val.ToLower().Trim().StartsWith("to_timestamp("))
        {
            return true;
        }

        if (val.ToLower().Trim().StartsWith("to_timestamp_tz("))
        {
            return true;
        }

        if (val.ToLower().Trim().StartsWith("to_char("))
        {
            return true;
        }

        return false;
    }

    #endregion
}
