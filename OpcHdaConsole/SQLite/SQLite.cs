using System;
using System.IO;
using System.Text;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Collections.Generic;

namespace OpcHdaConsole
{
    public class Table
    {
        public List<string> columns_name = new List<string>();
        public List<string> columns_type = new List<string>();
        public List<List<object>> rc = new List<List<object>>();
    }

    public class SQLite
    {
        private string path_to_database;
        private ConnectionState state = ConnectionState.Closed;
        private SQLiteConnection connection;

        // public
        public SQLite(string path_to_database)
        {
            this.path_to_database = path_to_database;
        }
        ~SQLite()
        {
            connection.Close();
        }

        public bool Execute(string query)
        {
            if (!Connect())
                return (false);

            try
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    command.ExecuteNonQuery();
                }
            }
            catch (SQLiteException e)
            {
                Program.Log(e.Message);
                return (false);
            }
            finally
            {
                state = connection.State;
            }
            return (true);
        }
    
        public Table Select(string query)
        {
            Table table;

            try
            {
                table = new Table();

                if (!Connect())
                    return (null);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    //this.command.Parameters.AddWithValue
                    //this.command.Prepare();
                    using (var reader = command.ExecuteReader())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            table.columns_name.Add(reader.GetOriginalName(i));
                            table.columns_type.Add(reader.GetDataTypeName(i));
                        }

                        while (reader.Read())
                        {
                            table.rc.Add(new List<object>());
                            for (int i = 0; i < reader.FieldCount; i++)
                                table.rc[table.rc.Count - 1].Add(reader.GetValue(i));
                        }
                    }
                }
            }
            catch (SQLiteException e)
            {
                state = connection.State;
                Program.Log(e.Message);
                return (null);
            }
            return (table);
        }
        public long SelectCount(string table)
        {
            try
            {
                if (!Connect())
                    return (0);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT COUNT(*) AS count FROM " + table;
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.FieldCount <= 0)
                            return 0;

                        while (reader.Read())
                            return reader.GetInt64(0);
                    }
                }
            }
            catch (SQLiteException e)
            {
                state = connection.State;
                Program.Log(e.Message);
            }
            return (0);
        }
        public List<T> SelectColumn<T>(string query)
        {
            List<T> column = new List<T>();

            try
            {
                if (!Connect())
                    return (column);

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = query;
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.FieldCount <= 0)
                            return column;

                        while (reader.Read())
                            column.Add((T)reader.GetValue(0));
                    }
                }
            }
            catch (SQLiteException e)
            {
                state = connection.State;
                Program.Log(e.Message);
            }
            return (column);
        }

        public bool IsConnected()
        {
            if (state == ConnectionState.Open)
                return (true);
            else
                return (false);
        }
        public string GetState()
        {
            return (state.ToString());
        }
        
        // private
        private bool CreateFile()
        {
            try
            {
                if (!File.Exists(path_to_database))
                    SQLiteConnection.CreateFile(path_to_database);
            }
            catch (SQLiteException e)
            {
                Program.Log(e.Message);
                return (false);
            }
            return (true);
        }
        private bool Connect()
        {
            if (IsConnected())
                return (true);

            if (!CreateFile())
                return (false);

            try
            {
                connection = new SQLiteConnection("Data Source=" + path_to_database + ";Version=3;");
                connection.Open();
            }
            catch (SQLiteException e)
            {
                Program.Log(e.Message);
                return (false);
            }
            finally
            {
                state = connection.State;
            }

            Execute("PRAGMA journal_mode = wal;");
            //Execute("PRAGMA busy = ?;");
            return (true);
        }
    }
}
