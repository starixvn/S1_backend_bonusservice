using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Serilog.Context;

namespace BonusService.Service.Library
{
    public sealed class MySqlClient
    {
        private readonly string _connectionString;

        private readonly ILogger<MySqlClient> _logger;

        public MySqlClient(string connectionString, ILogger<MySqlClient> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public async Task<T> QueryFirstAsync<T>(string name, SqlMapper.IDynamicParameters parameters)
        {
            T result;
            var sw = Stopwatch.StartNew();
            try
            {
                await using var connection = new MySqlConnection(_connectionString);
                result = await connection.QueryFirstOrDefaultAsync<T>(name, parameters,
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogInformation($"{name} {sw.Elapsed:c}");
            return result;
        }

        public T QueryFirst<T>(string name, SqlMapper.IDynamicParameters parameters)
        {
            T result;
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                result = connection.QueryFirstOrDefault<T>(name, parameters,
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogInformation($"{name} {sw.Elapsed:c}");
            return result;
        }

        public bool Execute(string name, SqlMapper.IDynamicParameters parameters)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute(name, parameters, commandType: CommandType.StoredProcedure);
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogInformation($"{name} {sw.Elapsed:c}");
            return true;
        }

        public bool Execute(string command)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                connection.Execute(command, CommandType.Text);
            }
            catch (Exception e)
            {
                _logger.LogError(e, command);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogInformation(command);
            return true;
        }

        public async Task<bool> ExecuteAsync(string name, SqlMapper.IDynamicParameters parameters)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                await using var connection = new MySqlConnection(_connectionString);
                await connection.ExecuteAsync(name, parameters, commandType: CommandType.StoredProcedure);
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogInformation($"{name} {sw.Elapsed:c}");
            return true;
        }

        public IEnumerable<T> Query<T>(string name)
        {
            IEnumerable<T> result;
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                result = connection.Query<T>(name, commandType: CommandType.Text);
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogInformation(name);
            return result;
        }

        public IEnumerable<T> Query<T>(string name, SqlMapper.IDynamicParameters parameters, int? commandTimeout = null)
        {
            IEnumerable<T> result;
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                result = connection.Query<T>(name, parameters, commandTimeout: commandTimeout, commandType: CommandType.StoredProcedure);
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogInformation($"{name} {sw.Elapsed:c}");
            return result;
        }

        public T Query<T>(string name, SqlMapper.IDynamicParameters parameters,
            Func<SqlMapper.GridReader, T> func)
        {
            T result;
            var sw = Stopwatch.StartNew();
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                var multiple = connection.QueryMultiple(name, parameters, commandType: CommandType.StoredProcedure);
                result = func.Invoke(multiple);
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogInformation($"{name} {sw.Elapsed:c}");
            return result;
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string name, SqlMapper.IDynamicParameters parameters)
        {
            IEnumerable<T> result;
            var sw = Stopwatch.StartNew();
            try
            {
                await using var connection = new MySqlConnection(_connectionString);
                result = await connection.QueryAsync<T>(name, parameters, commandType: CommandType.StoredProcedure);
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogInformation($"{name} {sw.Elapsed:c}");
            return result;
        }

        public async Task<T> QueryAsync<T>(string name, SqlMapper.IDynamicParameters parameters,
            Func<SqlMapper.GridReader, T> func)
        {
            T result;
            var sw = Stopwatch.StartNew();
            try
            {
                await using var connection = new MySqlConnection(_connectionString);
                var multiple =
                    connection.QueryMultipleAsync(name, parameters, commandType: CommandType.StoredProcedure);
                result = func.Invoke(await multiple);
            }
            catch (Exception e)
            {
                _logger.LogError(e, name);
                throw;
            }

            LogContext.PushProperty("elapsed", sw.Elapsed.TotalMilliseconds);
            _logger.LogInformation($"{name} {sw.Elapsed:c}");
            return result;
        }
    }
}