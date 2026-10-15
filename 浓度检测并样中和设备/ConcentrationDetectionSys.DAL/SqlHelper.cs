using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ConcentrationDetectionSys.DAL
{
    /// <summary>
    /// 实现所有对SQL Server数据库的所有访问操作
    /// </summary>
    public static class SqlHelper
    {
        //public static readonly string connStr =
            //@"Data Source=CXL;Initial Catalog=ConcentrationDetectionDB;Integrated Security=True;Pooling=True;Max Pool Size=50;TrustServerCertificate=True";
        public static readonly string connStr =
            @"Data Source=localhost;Initial Catalog=ConcentrationDetectionDB;Integrated Security=True;Pooling=True;Max Pool Size=50;TrustServerCertificate=True";

        /// <summary>
        /// 增删改
        /// </summary>
        public static int ExecuteNonQuery(string sql, params SqlParameter[] pms)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    if (pms != null) cmd.Parameters.AddRange(pms);
                    conn.Open();
                    return cmd.ExecuteNonQuery();
                }
            }
        }
        /// <summary>
        /// 返回第一行第一列
        /// </summary>
        public static object ExecuteScalar(string sql, params SqlParameter[] pms)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    if (pms != null) cmd.Parameters.AddRange(pms);
                    conn.Open();
                    return cmd.ExecuteScalar();
                }
            }
        }
        /// <summary>
        /// 返回单行数组
        /// </summary>
        public static object[] EntireRow(string sql, params SqlParameter[] pms)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                using (SqlCommand command = new SqlCommand(sql, conn))
                {
                    if (pms != null) command.Parameters.AddRange(pms);
                    using (SqlDataReader reade = command.ExecuteReader())
                    {
                        if (reade.Read())
                        {
                            object[] rowData = new object[reade.FieldCount];
                            reade.GetSqlValues(rowData);
                            return rowData;
                        }
                    }
                }
            }
            return null;
        }
        /// <summary>
        /// 返回DataTable（上位机优先使用）
        /// </summary>
        public static DataTable ExecuteDataTable(string sql, params SqlParameter[] pms)
        {
            DataTable dt = new DataTable();
            using (SqlDataAdapter adapter = new SqlDataAdapter(sql, connStr))
            {
                if (pms != null)
                {
                    adapter.SelectCommand.Parameters.AddRange(pms);
                }
                adapter.Fill(dt);
            }
            return dt;
        }
        /// <summary>
        /// 读取字符串类型配置
        /// </summary>
        /// <param name="varName">变量英文名称</param>
        /// <param name="defaultVal">读取失败返回的默认值</param>
        /// <returns></returns>
        public static string GetString(string varName, string defaultVal = "")
        {
            string sql = "SELECT VarValue FROM SysVariableT WHERE VarName=@VarName";
            SqlParameter[] paras = { new SqlParameter("@VarName", varName) };
            DataTable dt = SqlHelper.ExecuteDataTable(sql, paras);

            if (dt.Rows.Count > 0 && dt.Rows[0]["VarValue"] != DBNull.Value)
            {
                return dt.Rows[0]["VarValue"].ToString();
            }
            return defaultVal;
        }

        /// <summary>
        /// 读取int整数
        /// </summary>
        public static int GetInt(string varName, int defaultVal = 0)
        {
            string str = GetString(varName);
            if (int.TryParse(str, out int val))
                return val;
            return defaultVal;
        }

        /// <summary>
        /// 读取double小数
        /// </summary>
        public static double GetDouble(string varName, double defaultVal = 0)
        {
            string str = GetString(varName);
            if (double.TryParse(str, out double val))
                return val;
            return defaultVal;
        }

        /// <summary>
        /// 读取bool布尔 1=true  0=false
        /// </summary>
        public static bool GetBool(string varName, bool defaultVal = false)
        {
            string str = GetString(varName);
            if (str == "1" || str.ToLower() == "true")
                return true;
            return false;
        }

        /// <summary>
        /// 修改变量值（写入数据库）
        /// </summary>
        /// <param name="varName">变量名</param>
        /// <param name="value">值</param>
        /// <returns>是否成功</returns>
        //public static bool SetStringValue(string varName, string value)
        //{
        //    string sql = @"UPDATE SysVariableT SET VarValue=@Val WHERE VarName=@VarName";
        //    SqlParameter[] paras =
        //    {
        //        new SqlParameter("@Val", value.ToString()),
        //        new SqlParameter("@VarName", varName)
        //    };
        //    int rows = SqlHelper.ExecuteNonQuery(sql, paras);
        //    if (rows > 0)
        //    {
        //        return true;
        //    }
            
        //    // 更新0行，代表记录不存在 → 执行新增
        //    string sqlInsert = @"INSERT INTO SysVariableT(VarName,VarValue) VALUES(@VarName,@Val)";
        //    int insertRows = SqlHelper.ExecuteNonQuery(sqlInsert, paras);
        //    return insertRows > 0;
        //}
        public static bool SetStringValue(string varName, string value)
        {
            string sql = @"
                        MERGE INTO SysVariableT AS t
                        USING (SELECT @VarName AS VarName, @Val AS VarValue) AS s
                        ON t.VarName = s.VarName
                        WHEN MATCHED THEN
                            UPDATE SET VarValue = s.VarValue
                        WHEN NOT MATCHED THEN
                            INSERT(VarName,VarValue) VALUES(s.VarName,s.VarValue);
                        ";
            SqlParameter[] paras =
            {
                new SqlParameter("@Val", value),
                new SqlParameter("@VarName", varName)
            };
            int rows = SqlHelper.ExecuteNonQuery(sql, paras);
            return rows > 0;
        }
        public static bool SetIntValue(string varName, int value)
        {
            string sql = @"UPDATE SysVariableT SET VarValue=@Val WHERE VarName=@VarName";
            SqlParameter[] paras =
            {
        new SqlParameter("@Val", SqlDbType.Int){Value = value},
        new SqlParameter("@VarName", varName)
    };
            int rows = SqlHelper.ExecuteNonQuery(sql, paras);
            return rows > 0;
        }
        /// <summary>
        /// 删除数据库【超过3个月】的数据（当前时间往前推3个月，早于这个时间的全部删掉）
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <param name="condition"></param>
        public static void DeleteBeforeSQLData(string tableName, string condition)
        {
            string sql = $"DELETE FROM {tableName} WHERE CONVERT(datetime, {condition}) < DATEADD(MONTH, -3, GETDATE())";
            SqlHelper.ExecuteNonQuery(sql);
        }
        /// <summary>
        /// 支持事务的增删改操作 (用于批量处理)
        /// </summary>
        public static int ExecuteTransaction(Action<SqlConnection, SqlTransaction> dbAction)
        {
            int totalAffected = 0;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                using (SqlTransaction transaction = conn.BeginTransaction())
                {
                    try
                    {
                        dbAction(conn, transaction);
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        throw; // 向上抛出异常，让业务层捕获
                    }
                }
            }
            return totalAffected;
        }
    }
}
