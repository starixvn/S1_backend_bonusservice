using System.Data;
using System.Diagnostics;

using BonusService.Service.Model;
using BonusService.Service.Model.enums;

using Dapper;

using MySql.Data.MySqlClient;

using Serilog.Context;

namespace BonusService.Service.Library
{
    public sealed class MySqlClient
    {
        public readonly DataBase DataBase;

        private readonly string _connectionString;

        private readonly IEnumerable<string> _backupConnectionStringList;

        private readonly ILogger<MySqlClient> _logger;

        public MySqlClient(IServiceProvider provider, DataBase dataBase, string connectionString)
        {
            DataBase = dataBase;
            _logger = provider.GetRequiredService<ILogger<MySqlClient>>();
            _connectionString = connectionString;
            _backupConnectionStringList = null;
        }

        public MySqlClient(IServiceProvider provider, DataBase dataBase, string connectionString, IEnumerable<string> backupConnectionStringList)
        {
            DataBase = dataBase;
            _logger = provider.GetRequiredService<ILogger<MySqlClient>>();
            _connectionString = connectionString;
            _backupConnectionStringList = backupConnectionStringList;
        }

        private string HealthCheck()
        {
            if (_backupConnectionStringList == null)
            {
                return _connectionString;
            }

            foreach (var backupConn in _backupConnectionStringList)
            {
                try
                {
                    using var connection = new MySqlConnection(backupConn);
                    var dbSlaveStatus = connection.QueryFirstOrDefault<DBSlaveStatus>("Service_HealthCheck_V2", commandType: CommandType.StoredProcedure);

                    // 拿不到Status或延遲太久都換下一個連線
                    if (dbSlaveStatus == null || dbSlaveStatus.Seconds_Behind_Master > 180)
                    {
                        continue;
                    }

                    return backupConn;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"{ReplaceDBString(backupConn)} HealthCheck Exception");
                }
            }

            // 前面連線檢查都有問題，用最後一個連線
            return _backupConnectionStringList.Last();
        }

        public MySqlConnection GetMySqlConnection()
        {
            var conn = HealthCheck();
            return new MySqlConnection(conn);
        }

        public T QueryFirst<T>(string name, object parameters)
        {
            T result;
            var sw = Stopwatch.StartNew();
            try
            {
                var conn = HealthCheck();
                using var connection = new MySqlConnection(conn);
                var command = new CommandDefinition(name, parameters,
                    commandType: CommandType.StoredProcedure,
                    flags: CommandFlags.NoCache);
                result = connection.QueryFirstOrDefault<T>(command);
                connection.Close();
                connection.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result;
        }

        public IEnumerable<T> QueryUseCommandDefinition<T>(string name, object parameters, int? commandTimeout = null)
        {
            IEnumerable<T> result;
            var sw = Stopwatch.StartNew();
            try
            {
                var conn = HealthCheck();
                conn = $"{conn}ProcedureCacheSize=0;";
                using var connection = new MySqlConnection(conn);
                if (connection.State != ConnectionState.Open) connection.Open();// 自己 Control connection open close，不要讓 ORM 自己做

                var command = new CommandDefinition(name, parameters, // 使用 CommandDefinition 不讓
                    commandTimeout: commandTimeout,
                    commandType: CommandType.StoredProcedure,
                    flags: CommandFlags.NoCache);

                result = connection.Query<T>(command);
                connection.Close();
                connection.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result.ToList();
        }
        
        public IEnumerable<T> Query<T>(string spName, object parameters = null, int? commandTimeout = null)
        {
            IEnumerable<T> result;
            var sw = Stopwatch.StartNew();
            try
            {
                var conn = HealthCheck();
                using var connection = new MySqlConnection(conn);
                var command = new CommandDefinition(spName, parameters,
                    commandTimeout: commandTimeout,
                    commandType: CommandType.StoredProcedure);
                result = connection.Query<T>(command);
                connection.Close();
                connection.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogError(e, spName);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{spName} {sw.Elapsed:c}");
            return result.ToList();
        }

        public int Execute(string name, object parameters = null)
        {
            int result;
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                result = connection.Execute(name, parameters, commandType: CommandType.StoredProcedure);
                connection.Close();
                connection.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result;
        }

        public int Execute(string name, IDbTransaction transaction, object parameters = null)
        {
            var sw = Stopwatch.StartNew();
            int result;
            try
            {
                result = transaction.Connection.Execute(name, parameters, commandType: CommandType.StoredProcedure);
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result;
        }   

        public async Task<bool> ExecuteAsync(string name, SqlMapper.IDynamicParameters parameters)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                await using var connection = new MySqlConnection(_connectionString);
                await connection.ExecuteAsync(name, parameters, commandType: CommandType.StoredProcedure);
                await connection.CloseAsync();
                await connection.DisposeAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return true;
        }

        public T ExecuteScalar<T>(string name, object parameters)
        {
            T result;
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                result = connection.ExecuteScalar<T>(name, parameters, commandType: CommandType.StoredProcedure);
                connection.Close();
                connection.Dispose();
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result;
        }
        
        public void CommitAndDisposeConnection(MySqlConnection conn, IDbTransaction trans)
        {
            trans.Commit();
            trans.Dispose();
            conn.Close();
            conn.Dispose();
        }

        public void RollbackAndDisposeConnection(MySqlConnection conn, IDbTransaction trans)
        {
            trans.Rollback();
            trans.Dispose();
            conn.Close();
            conn.Dispose();
        }

        public async Task CommitAndDisposeConnectionAsync(MySqlConnection conn, MySqlTransaction trans)
        {
            await trans.CommitAsync();
            await trans.DisposeAsync();
            await conn.CloseAsync();
            await conn.DisposeAsync();
        }

        public async Task RollbackAndDisposeConnectionAsync(MySqlConnection conn, MySqlTransaction trans)
        {
            await trans.RollbackAsync();
            await trans.DisposeAsync();
            await conn.CloseAsync();
            await conn.DisposeAsync();
        }

        private string ReplaceDBString(string conn)
        {
            return conn.Substring(0, 28);
        }
    }
}