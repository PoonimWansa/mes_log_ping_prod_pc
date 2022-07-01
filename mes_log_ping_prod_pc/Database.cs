using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.OleDb;
using Microsoft.Win32.SafeHandles;
using System.Reflection;
using System.Globalization;
using System.IO;
using Oracle.ManagedDataAccess.Client;
using System.Runtime.InteropServices;

namespace mes_log_ping_prod_pc
{
    public class MyOleDb : IDisposable
    {
        #region **** Properties
        /// <summary>
        /// Returns last sql executed.
        /// </summary>
        public string LastSQL
        {
            get { return (_lastSQL == null) ? string.Empty : _lastSQL; }
        }

        /// <summary>
        /// Returns number of rows affected by last sql command,
        /// Will return -1 if sql error or use SELECT statement.
        /// </summary>
        public int RowsAffected
        {
            get { return _rowAffected; }
        }

        /// <summary>
        /// Returns last error detected.
        /// </summary>
        public string LastError
        {
            get { return (_lastError == null) ? string.Empty : _lastError; }
        }
        #endregion

        #region **** Constructor
        /// <summary>
        /// Initializes a new instance of OracleDB class with the specified server.
        /// </summary>
        /// <param name="sv"></param>
        public MyOleDb(string connectionString)
        {
            DBCon = new OleDbConnection(connectionString);
            DBCom = new OleDbCommand(string.Empty, DBCon);
        }

        #endregion

        #region **** Methods

        public bool ExecQuery(string sql, out DataTable dt, out string message)
        {
            _lastSQL = sql;
            message = "";
            OleDbDataAdapter adapter = null;
            dt = new DataTable();

            try
            {
                adapter = new OleDbDataAdapter(sql, DBCon);
                adapter.Fill(dt);
            }
            catch (Exception ex)
            {
                message = ex.Message;
                dt = new DataTable();
                return false;
            }
            finally
            {
                if (adapter != null)
                {
                    adapter.Dispose();
                    adapter = null;
                }
            }

            return true;
        }

        public bool ExecNonQuery(string sql, out string message)
        {
            message = "";
            _lastSQL = sql;

            try
            {
                DBCon.Open();
                DBCom.CommandText = sql;
                _rowAffected = DBCom.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                _rowAffected = -1;
                _lastError = ex.Message;
                message = ex.Message;
                return false;
            }
            finally
            {
                DBCon.Close();
            }

            return true;
        }

        #endregion

        #region **** Variables

        private int _rowAffected = 0;
        private string _lastSQL = null;
        private OleDbConnection DBCon = null;
        private OleDbCommand DBCom = null;

        private string _lastError = null;
        #endregion

        #region **** IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    if (DBCom != null)
                    {
                        DBCom.Dispose();
                        DBCom = null;
                    }

                    if (DBCon != null)
                    {
                        if (DBCon.State != ConnectionState.Closed) DBCon.Close();
                        DBCon.Dispose();
                        DBCon = null;
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                _lastSQL = null;
                _lastError = null;

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~OracleDB() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }

    public class MyOracleManagedDataAccess : IDisposable
    {
        private OracleConnection oracleConnection;
        private string lastSQL = "";
        private int rowAffected = -1;
        public enum ServiceType
        {
            SID,
            SERVICE_NAME
        }

        #region *** Property
        public int RowsAffected
        {
            get { return rowAffected; }
        }

        #endregion

        #region *** Constructor
        public MyOracleManagedDataAccess(string ConnectionString, out string ErrorMessage)
        {
            try
            {
                oracleConnection = new OracleConnection(ConnectionString);
                ErrorMessage = "";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error, {ex.Message}, ContectionString = {ConnectionString}";
            }
        }

        public MyOracleManagedDataAccess(string HostIpAddress, string Port, ServiceType serviceType, string ServiceName, string User, string Password, out string ErrorMessage)
        {
            string ConString = "";
            try
            {
                string CmdService = serviceType == ServiceType.SID ? "SERVICE_NAME" : "SID";
                ConString = $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)" +
                                   $"(HOST={HostIpAddress})(PORT={Port}))" +
                                   $"(CONNECT_DATA=({CmdService}={ServiceName})));" +
                                   $"User ID={User};Password={Password};";
                oracleConnection = new OracleConnection(ConString);
                ErrorMessage = "";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error, {ex.Message}, ContectionString = {ConString}";
            }
        }

        public MyOracleManagedDataAccess(string HostIpAddress, string Port, string SID, string User, string Password, out string ErrorMessage)
        {
            string ConString = "";
            try
            {
                ConString = $"Data Source=" +
                     $"(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(COMMUNITY=tcp.world)" +
                     $"(PROTOCOL=TCP)(Host={HostIpAddress})(Port={Port})))" +
                     $"(CONNECT_DATA=(SERVER=dedicated) (SID={SID})));" +
                     $"Persist Security Info=false;" +
                     $"User ID={User};Password={Password};";
                oracleConnection = new OracleConnection(ConString);
                ErrorMessage = "";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error, {ex.Message}, ContectionString = {ConString}";
            }
        }
        #endregion

        #region *** Method
        public bool ExecQuery(string sql, out DataTable obj, out string message)
        {
            lastSQL = sql;
            message = "";
            DataTable dt = new DataTable();
            OracleDataAdapter oracleDataAdapter;

            try
            {
                oracleConnection.Open();
                oracleDataAdapter = new OracleDataAdapter(sql, oracleConnection);
                oracleDataAdapter.Fill(dt);
                obj = dt;

            }
            catch (Exception ex)
            {
                message = ex.Message;
                obj = null;
                return false;
            }
            finally
            {
                oracleConnection.Close();
            }
            return true;
        }


        public bool ExecNonQuery(string sql, out string message)
        {
            message = "";
            lastSQL = sql;

            try
            {
                oracleConnection.Open();
                OracleCommand oracleCommand = new OracleCommand(sql, oracleConnection);
                oracleCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                rowAffected = -1;
                message = ex.Message;
                return false;
            }
            finally
            {
                oracleConnection.Close();
            }

            return true;
        }

        #endregion

        #region *** Dispose

        bool disposed = false;
        SafeHandle handle = new SafeFileHandle(IntPtr.Zero, true);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                handle.Dispose();
                oracleConnection.Close();
                oracleConnection.Dispose();
            }

            disposed = true;
        }

        #endregion
    }

    /// <summary>
    /// Manage DataTable
    /// Add By : Anusorn
    /// Date : 2019/01/20
    /// </summary>
    public static class MyDataTable
    {
        #region ** Method

        #region * General

        /// <summary>
        /// Set Coloum
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="Column">Name Of Column</param>
        /// <returns>OK : New Table, NG : Old Table</returns>
        public static DataTable AddColumn(DataTable Table, string Column)
        {
            try
            {
                if (!Table.Columns.Contains(Column))
                {
                    Table.Columns.Add(Column);
                }
                return Table;
            }
            catch
            {
                return Table;
            }
        }

        /// <summary>
        /// Set Coloum
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="Column"></param>
        /// <param name="Index">Index Of Column</param>
        /// <returns>OK : New Table, NG : Old Table</returns>
        public static DataTable AddColumn(DataTable Table, string Column, int Index)
        {
            try
            {
                if (!Table.Columns.Contains(Column))
                {
                    Table.Columns.Add(Column).SetOrdinal(Index);
                }
                return Table;
            }
            catch
            {
                return Table;
            }
        }

        /// <summary>
        /// Find avg,sum from coloum from datatable
        /// </summary>
        /// <param name="DT">Datatable</param>
        /// <param name="Format">Ex. sum("coloum")</param>
        /// <returns></returns>
        public static object GetTableByCompute(DataTable DT, string Format)
        {
            try
            {
                return DT.Compute(Format, "");
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Create datable by range select
        /// </summary>
        /// <param name="DT">Datatable</param>
        /// <param name="FormatSelect">Ex. Coloum > 10</param>
        /// <returns></returns>
        public static DataTable GetTableBySelect(DataTable DT, string FormatSelect)
        {
            try
            {
                return DT = DT.Select(FormatSelect).CopyToDataTable();
            }
            catch
            {
                return DT = new DataTable();
            }
        }

        /// <summary>
        /// Distinct 2 Coloum
        /// </summary>
        /// <param name="DT"></param>
        /// <param name="Coloum1"></param>
        /// <param name="Coloum2"></param>
        /// <returns></returns>
        public static DataTable GetTableByDistinct(DataTable DT, string Coloum1, string Coloum2)
        {
            try
            {
                DataView DV = new DataView(DT);
                return DV.ToTable(true, Coloum1, Coloum2);
            }
            catch
            {
                return DT = new DataTable();
            }
        }

        /// <summary>
        /// Distinct 1 Coloum
        /// </summary>
        /// <param name="DT"></param>
        /// <param name="Coloum1"></param>
        /// <returns></returns>
        public static DataTable GetTableByDistinct(DataTable DT, string Coloum1)
        {
            try
            {
                DataView DV = new DataView(DT);
                return DV.ToTable(true, Coloum1);
            }
            catch
            {
                return DT = new DataTable();
            }
        }

        /// <summary>
        /// Get Value Form Cell Of Datatable
        /// </summary>
        /// <param name="DT"></param>
        /// <param name="Column"></param>
        /// <param name="ReplaceNull"></param>
        /// <param name="Row"></param>
        /// <returns></returns>
        public static string GetCell(DataTable DT, string Column, string ReplaceNull, int Row = 0)
        {
            string Return = "";
            try
            {
                Return = DT.Rows[Row][Column].ToString();
                if (Return == "") Return = ReplaceNull;
            }
            catch
            {
                return Return = ReplaceNull;
            }
            return Return;
        }

        /// <summary>
        /// Change Value In DataTable
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="FormatFindRow"></param>
        /// <param name="Coloum"></param>
        /// <param name="ColValue"></param>
        /// <returns>OK : New Edit Table,NG : Old Table</returns>
        public static DataTable SetCell(DataTable Table, string FormatFindRow, string ColName, string ColValue)
        {
            if (GetIndexOfRow(Table, FormatFindRow, out int Index))
            {
                Table.Rows[Index][ColName] = ColValue;
            }
            return Table;
        }


        public static DataTable SetCell(DataTable Table, int RowIndex, string ColName, string ColValue)
        {
            try
            {
                Table.Rows[RowIndex][ColName] = ColValue;
                return Table;
            }
            catch
            {
                return new DataTable();
            }
        }

        /// <summary>
        /// Get datatble that sort by condition
        /// </summary>
        /// <param name="DT">Datatable</param>
        /// <param name="FormatView">Ex. coloum DESC</param>
        /// <returns></returns>
        public static DataTable GetTableBySort(DataTable DT, string FormatView)
        {
            try
            {
                DataView DV = DT.DefaultView;
                DV.Sort = FormatView;
                return DV.ToTable();
            }
            catch
            {
                return DT = new DataTable();
            }
        }

        /// <summary>
        /// Find Row Index
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="Format"></param>
        /// <param name="Index"></param>
        /// <returns></returns>
        public static bool GetIndexOfRow(DataTable Table, string Format, out int Index)
        {
            try
            {
                DataRow[] arrDR = Table.Select(Format);
                Index = Table.Rows.IndexOf(arrDR[0]);
                return true;
            }
            catch
            {
                Index = 0;
                return false;
            }
        }

        /// <summary>
        /// Find Coloum Index
        /// </summary>
        /// <param name="Table"></param>
        /// <param name="ColoumName"></param>
        /// <param name="Index"></param>
        /// <returns></returns>
        public static bool GetIndexOfColoum(DataTable Table, string ColoumName, out int Index)
        {
            try
            {
                Index = 0;
                foreach (DataColumn column in Table.Columns)
                {
                    if (column.ColumnName.Contains("VAL"))
                    {
                        Index = column.Ordinal;
                    }
                }
                return true;
            }
            catch
            {
                Index = 0;
                return false;
            }
        }

        /// <summary>
        /// Get Datatable By Some Coloum
        /// </summary>
        /// <param name="Coloums">Coloum Name</param>
        /// <param name="Table"></param>
        /// <returns></returns>
        public static DataTable GetTableSomeColoum(string[] Coloums, DataTable Table)
        {
            try
            {
                return new DataView(Table).ToTable(false, Coloums);
            }
            catch
            {
                return new DataTable();
            }
        }

        public static DataTable SetColoumName(DataTable Table, string OldColoum, string NewColoum)
        {
            try
            {
                Table.Columns[OldColoum].ColumnName = NewColoum;
                return Table;
            }
            catch
            {
                return Table;
            }
        }

        public static DataTable RemoveColoumName(DataTable Table, string Coloum)
        {
            try
            {
                if (Table.Columns.Contains(Coloum))
                {
                    Table.Columns.Remove(Coloum);
                }
                return Table;
            }
            catch
            {
                return Table;
            }
        }

        #endregion

        #region * MVC
        public static List<T> ConvertDataTable<T>(DataTable dt)
        {
            List<T> data = new List<T>();
            foreach (DataRow row in dt.Rows)
            {
                T item = GetItem<T>(row);
                data.Add(item);
            }
            return data;
        }
        public static T GetItem<T>(DataRow dr)
        {
            Type temp = typeof(T);
            T obj = Activator.CreateInstance<T>();

            foreach (DataColumn column in dr.Table.Columns)
            {
                foreach (PropertyInfo pro in temp.GetProperties())
                {
                    if (pro.Name == column.ColumnName)
                        pro.SetValue(obj, dr[column.ColumnName], null);
                    else
                        continue;
                }
            }
            return obj;
        }
        #endregion

        #region * IO
        /// <summary>
        /// Get DataTable From Csv File. 2018/12/06 By Anusorn
        /// </summary>
        /// <param name="path"></param>
        /// <param name="isFirstRowHeader"></param>
        /// <returns></returns>
        public static DataTable GetDataTableFromCsv(string path, bool isFirstRowHeader)
        {
            string header = isFirstRowHeader ? "Yes" : "No";

            string pathOnly = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);

            string sql = @"SELECT * FROM [" + fileName + "]";

            using (OleDbConnection connection = new OleDbConnection(
                      @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + pathOnly +
                      ";Extended Properties=\"Text;HDR=" + header + "\""))
            using (OleDbCommand command = new OleDbCommand(sql, connection))
            using (OleDbDataAdapter adapter = new OleDbDataAdapter(command))
            {
                DataTable dataTable = new DataTable();
                dataTable.Locale = CultureInfo.CurrentCulture;
                adapter.Fill(dataTable);
                return dataTable;
            }
        }

        /// <summary>
        /// Get DataTable Form Text File. 2018/12/06 By Anusorn
        /// </summary>
        /// <param name="FilePath"></param>
        /// <param name="NumberOfColumns"></param>
        /// <returns></returns>
        public static DataTable ConvertTextFileToDataTable(string FilePath, int NumberOfColumns)
        {
            using (DataTable DT = new DataTable())
            {
                for (int idxColoum = 0; idxColoum < NumberOfColumns; idxColoum++)
                {
                    DT.Columns.Add(new DataColumn("Column" + (idxColoum + 1).ToString()));
                }

                if (Directory.Exists(FilePath))
                {
                    string[] arrLine = System.IO.File.ReadAllLines(FilePath);

                    for (int idxRow = 0; idxRow < arrLine.Length; idxRow++)
                    {
                        string Line = arrLine[idxRow];
                        var Cols = Line.Split(':');
                        DataRow DR = DT.NewRow();

                        for (int idxColoum = 0; idxColoum < NumberOfColumns; idxColoum++)
                        {
                            DR[idxColoum] = Cols[idxColoum];
                        }
                        DT.Rows.Add(DR);
                    }
                }
                return DT;
            }
        }

        public static bool ExportCSVWinFormType(DataTable Table, string FileName)
        {
            try
            {
                var lines = new List<string>();

                string[] columnNames = Table.Columns
                    .Cast<DataColumn>()
                    .Select(column => column.ColumnName)
                    .ToArray();

                var header = string.Join(",", columnNames.Select(name => $"\"{name}\""));
                lines.Add(header);

                var valueLines = Table.AsEnumerable()
                    .Select(row => string.Join(",", row.ItemArray.Select(val => $"\"{val}\"")));

                lines.AddRange(valueLines);
                File.WriteAllLines($"{FileName}.csv", lines);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #endregion
    }
}
