using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class DB
    {
        public static Database _current = new Database();
    }

public class Database
{
    public int RowCount { get; set; }
    public static MySqlConnection Connection;
    public static MySql.Data.MySqlClient.MySqlDataReader SqlData;
    internal static void Init(string host, string user, string password, string database, int port)
    {

        Connection = new MySqlConnection("Server=" + host + ";User Id=" + user + ";Port=" + port + ";" +
                                         "Password=" + password + ";Database=" + database + ";charset=utf8;Allow Zero Datetime=True");

        try
        {
            Connection.Open();
            Console.WriteLine("Successfully connected to " + host + " " + port + " " + database);
        }
        catch (MySqlException ex)
        {
            Console.WriteLine(ex.ToString());
            return;
        }
    }

    public bool Execute(string sql, params object[] args)
    {
        StringBuilder sqlString = new StringBuilder();
        // Fix for floating point problems on some languages
        sqlString.AppendFormat(CultureInfo.GetCultureInfo("en-US").NumberFormat, sql, args);

        MySqlCommand sqlCommand = new MySqlCommand(sqlString.ToString(), Connection);

        try
        {
            sqlCommand.ExecuteNonQuery();
            return true;
        }
        catch (MySqlException ex)
        {
            Console.WriteLine(ex.Message);
            return false;
        }
    }

    public SQLResult Select(string sql, params object[] args)
    {
        SQLResult retData = new SQLResult();

        StringBuilder sqlString = new StringBuilder();
        // Fix for floating point problems on some languages
        sqlString.AppendFormat(CultureInfo.GetCultureInfo("en-US").NumberFormat, sql, args);

        MySqlCommand sqlCommand = new MySqlCommand(sqlString.ToString(), Connection);

        try
        {
            SqlData = sqlCommand.ExecuteReader(CommandBehavior.Default);
            retData.Load(SqlData);
            retData.Count = retData.Rows.Count;
            SqlData.Close();
        }
        catch (MySqlException ex)
        {
            Console.WriteLine(ex.Message);
        }

        return retData;
    }

    public class SQLResult : DataTable
    {
        public int Count { get; set; }

        public T Read<T>(int row, string columnName)
        {
            return (T)Convert.ChangeType(Rows[row][columnName], typeof(T));
        }
    }
}
