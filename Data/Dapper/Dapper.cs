using ChatApplication.Common;
using Dapper;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ChatApplication.Data.Dapper
{
    public class Dapper
    {
        public Dapper()
        {

        }

        #region Public methods
        public static async Task<IEnumerable<T>> GetAllAsync<T>(string sql, CommandType commandType, DynamicParameters parameters = null)
        {

            var con = new SqlConnection(GlobalValues.ConnectionString);
            await con.OpenAsync();

            var result = await con.QueryAsync<T>(sql, parameters, commandType: commandType);

            await con.CloseAsync();

            return result;
        }
        public static async Task<Tuple<T1, IEnumerable<T2>>> GetAllAndReturnValueAsync<T1, T2>(string sql, CommandType commandType,
                                                                                               DynamicParameters parameters, string outPerameter)
        {
            var con = new SqlConnection(GlobalValues.ConnectionString);

            await con.OpenAsync();

            var result = await con.QueryAsync<T2>(sql, parameters, commandType: commandType);

            var totalRecords = parameters.Get<T1>(outPerameter);

            await con.CloseAsync();

            return new Tuple<T1, IEnumerable<T2>>(totalRecords, result);
        }
        public static async Task<T> GetFirstAsync<T>(string sql, CommandType commandType, DynamicParameters parameters = null)
        {

            var con = new SqlConnection(GlobalValues.ConnectionString);
            await con.OpenAsync();

            var result = await con.QueryAsync<T>(sql, parameters, commandType: commandType);

            await con.CloseAsync();

            return result.ToList().FirstOrDefault();
        }
        public static async Task<dynamic> ExecuteAsync(string sql, CommandType commandType,
                                                            DynamicParameters parameters = null)
        {
            dynamic ReturnValue;

            var con = new SqlConnection(GlobalValues.ConnectionString);

            await con.OpenAsync();

            await con.ExecuteAsync(sql, parameters, commandType: commandType);

            await con.CloseAsync();

            return ReturnValue = new { Success = true };
        }
        public static async Task<T> ExecuteAndReturnValueAsync<T>(string sql, CommandType commandType,
                                                                   DynamicParameters parameters, string outPerameter)
        {
            var con = new SqlConnection(GlobalValues.ConnectionString);

            await con.OpenAsync();

            await con.ExecuteAsync(sql, parameters, commandType: commandType);
            var returnValue = parameters.Get<T>(outPerameter);

            await con.CloseAsync();

            return returnValue;
        }
        #endregion
    }
}
