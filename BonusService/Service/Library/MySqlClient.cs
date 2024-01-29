using System.Data;
using System.Diagnostics;
using System.Xml.Linq;

using BonusService.Service.Model.enums;

using Dapper;

using MySql.Data.MySqlClient;

using Newtonsoft.Json;

using Serilog.Context;

namespace BonusService.Service.Library
{
    public sealed class MySqlClient
    {
        public DataBase DataBase { get; set; }

        private readonly string _connectionString;

        private readonly string _backupConnectionString;

        private readonly ILogger<MySqlClient> _logger;

        public MySqlClient(IServiceProvider provider, DataBase dataBase, string connectionString, string backupConnectionString = null)
        {
            DataBase = dataBase;
            _logger = provider.GetRequiredService<ILogger<MySqlClient>>();
            _connectionString = connectionString;
            _backupConnectionString = backupConnectionString;
        }

        private string HealthCheck()
        {
            if (_backupConnectionString == null)
            {
                return _connectionString;
            }

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var check = connection.QueryFirst<bool>("Service_HealthCheck", commandType: CommandType.StoredProcedure);
                return check ? _connectionString : _backupConnectionString;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "HealthCheck Exception");
                return _backupConnectionString;
            }
        }

        public MySqlConnection GetMySqlConnection()
        {
            var conn = HealthCheck();
            return new MySqlConnection(conn);
        }

        public T QueryFirst<T>(string name, object parameters = null)
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

        public IEnumerable<T> Query<T>(string name, object parameters = null, int? commandTimeout = null)
        {
            IEnumerable<T> result;
            var sw = Stopwatch.StartNew();
            try
            {
                var conn = HealthCheck();
                using var connection = new MySqlConnection(conn);
                
                var command = new CommandDefinition(
                    commandText: name,
                    parameters: parameters,
                    commandTimeout: commandTimeout,
                    commandType: CommandType.StoredProcedure,
                    flags: CommandFlags.NoCache);

                result = connection.Query<T>(command);
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

        public IEnumerable<T> RetryQuery<T>(string name, DynamicParameters parameters = null, int? commandTimeout = null)
        {
            IEnumerable<T> result;
            var sw = Stopwatch.StartNew();
            try
            {
                var conn = HealthCheck();                                
                using var connection = new MySqlConnection(conn);
                var command = new CommandDefinition(
                   commandText: name,
                   parameters: parameters,
                   commandTimeout: commandTimeout,
                   commandType: CommandType.StoredProcedure,
                   flags: CommandFlags.NoCache);
                result = connection.Query<T>(command);
            }
            catch (MySqlException sqlEx)
            {
                var conn = HealthCheck();
                _logger.LogError(sqlEx, $"DBRetry {conn.Substring(0, 30)} | {name} {sw.Elapsed:c}| {SerializeDynamicParameters(parameters)}");

                using var connection = new MySqlConnection(conn);
                var command = new CommandDefinition(
                   commandText: name,
                   parameters: parameters,
                   commandTimeout: commandTimeout,
                   commandType: CommandType.StoredProcedure,
                   flags: CommandFlags.NoCache);
                result = connection.Query<T>(command);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"{name} {sw.Elapsed:c}| {SerializeDynamicParameters(parameters)}");
                throw;
            }

            LogContext.PushProperty("ElapsedMilliseconds", sw.Elapsed.TotalMilliseconds);
            _logger.LogDebug($"{name} {sw.Elapsed:c}");
            return result;
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

        public async Task<bool> ExecuteAsync(string name, object parameters)
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

        private string SerializeDynamicParameters(DynamicParameters parameters)
        {
            var dictionary = new Dictionary<string, object>();
            foreach (var paramName in parameters.ParameterNames)
            {
                var value = parameters.Get<dynamic>(paramName);
                dictionary.Add(paramName, value);
            }

            return JsonConvert.SerializeObject(dictionary);
        }
    }
}