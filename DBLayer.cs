using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace ASP_NET_CORE_Samples.DAL
{
    public class DataLayer
    {
      private readonly IConfiguration _config;
      private readonly string _cnnstr;
      private readonly string _mysqlcnnstr;
      private readonly int _maxAttempts;
      private readonly ILogger<DataLayer> _logger;
      public DataLayer(IConfiguration config, ILogger<DataLayer> logger)
      {
          _config = config;
          _cnnstr = _config.GetConnectionString("DefaultConnStr");
          _mysqlcnnstr = _config.GetConnectionString("MySQLConnStr");
          _maxAttempts = _config.GetValue<int>("connMaxAttempts");
          _logger = logger;
      }
      public async Task<Tresult> SQLSendQueryAsync<Tresult>(string command, SqlParameters parameters, string database = null)
        {
            int attempts = _maxAttempts;
            if (database != null)
                command = command.Replace("dbo", database + ".dbo");
            using (var conn = new SqlConnection(_cnnstr))
            using (var cmd = new SqlCommand(command, conn))
            {
                while (attempts-- > 0)
                {
                    try
                    {
                        await conn.OpenAsync().ConfigureAwait(false);
                        if (parameters != null)
                        {
                            foreach (var parameter in parameters)
                            {
                                cmd.Parameters.Add(new SqlParameter(parameter.Key, parameter.Value));
                            }                            
                        }
                        if (typeof(Tresult) == typeof(bool))
                        {
                            return (Tresult)(object)(await cmd.ExecuteNonQueryAsync().ConfigureAwait(false) > 0);
                        }
                        if (typeof(Tresult) == typeof(int))
                        {
                            return (Tresult)(await cmd.ExecuteScalarAsync().ConfigureAwait(false) ?? 0);
                        }
                        if (typeof(Tresult) == typeof(QueryData))
                        {
                            var data = new QueryData();
                            using (var adapter = new SqlDataAdapter(cmd))
                            using (var dataset = new DataSet())
                            {
                                adapter.Fill(dataset);
                                foreach (DataTable table in dataset.Tables)
                                {
                                    var newtable = new List<Dictionary<string, object>>();
                                    foreach (DataRow row in table.Rows)
                                    {
                                        var newrow = new Dictionary<string, object>();
                                        foreach (DataColumn column in table.Columns)
                                        {
                                            newrow.Add(column.ColumnName, row[column]);
                                        }
                                        newtable.Add(newrow);
                                    }
                                    data.Add(newtable);
                                }
                            }
                            return (Tresult)(object)data;
                        }
                    }
                    catch (SqlException sql_ex)
                    {
                        if (sql_ex.Number == -2)
                        {
                            await Task.Delay(1000).ConfigureAwait(false);
                            continue;
                        }
                        _logger.LogWarning($"sql exception: {sql_ex.Message}");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"data exception: {ex.Message}");
                        throw;
                    }
                }
            }
            _logger.LogWarning("default returned");
            return default;
        }
        
        public async Task<QueryData> SQLSendQueryFromCommandAsync(SqlCommand command)
        {
            int attempts = _maxAttempts;
            using (var conn = new SqlConnection(_cnnstr))
            {
                while (attempts-- > 0)
                {
                    try
                    {
                        await conn.OpenAsync().ConfigureAwait(false);
                        var data = new QueryData();
                        command.Connection = conn;
                        using var adapter = new SqlDataAdapter
                        {
                            SelectCommand = command
                        };
                        using var dataset = new DataSet();
                        adapter.Fill(dataset);
                        foreach (DataTable table in dataset.Tables)
                        {
                            var newtable = new List<Dictionary<string, object>>();
                            foreach (DataRow row in table.Rows)
                            {
                                var newrow = new Dictionary<string, object>();
                                foreach (DataColumn column in table.Columns)
                                {
                                    newrow.Add(column.ColumnName, row[column]);
                                }
                                newtable.Add(newrow);
                            }
                            data.Add(newtable);
                        }
                        return data;
                    }
                    catch (SqlException sql_ex)
                    {
                        if (sql_ex.Number == -2)
                        {
                            await Task.Delay(1000).ConfigureAwait(false);
                            continue;
                        }
                        _logger.LogWarning($"sql exception: {sql_ex.Message}");
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"data exception: {ex.Message}");
                        throw;
                    }
                }
            }
            _logger.LogWarning("default returned");
            return default;
        }
}
