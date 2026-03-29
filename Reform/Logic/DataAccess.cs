using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Reform.Interfaces;
using Reform.Objects;

namespace Reform.Logic
{
    public sealed class DataAccess<T> : IDataAccess<T> where T : class
    {
        private readonly IMetadataProvider<T> _metadataProvider;
        private readonly ICommandBuilder<T> _commandBuilder;
        private readonly IDebugLogger _debugLogger;

        public DataAccess(IMetadataProvider<T> metadataProvider, ICommandBuilder<T> commandBuilder, IDebugLogger debugLogger)
        {
            _metadataProvider = metadataProvider;
            _commandBuilder = commandBuilder;
            _debugLogger = debugLogger;
        }

        #region Sync

        public int Count(IDbConnection connection, IDbTransaction transaction, Expression<Func<T, bool>> predicate)
        {
            using (IDbCommand command = _commandBuilder.GetCountCommand(connection, predicate))
            {
                command.Transaction = transaction;
                return Convert.ToInt32(ExecuteScalar(command));
            }
        }

        public bool Exists(IDbConnection connection, IDbTransaction transaction, Expression<Func<T, bool>> predicate)
        {
            using (IDbCommand command = _commandBuilder.GetExistsCommand(connection, predicate))
            {
                command.Transaction = transaction;
                return Convert.ToInt64(ExecuteScalar(command)) == 1;
            }
        }

        public IEnumerable<T> Select(IDbConnection connection, IDbTransaction transaction, QueryCriteria<T> queryCriteria)
        {
            using (IDbCommand command = _commandBuilder.GetSelectCommand(connection, queryCriteria))
            {
                command.Transaction = transaction;
                return ExecuteReader(command);
            }
        }

        public void Insert(IDbConnection connection, IDbTransaction transaction, T instance)
        {
            using (IDbCommand command = _commandBuilder.GetInsertCommand(connection, instance))
            {
                command.Transaction = transaction;
                object id = ExecuteScalar(command);

                if (id != null && id != DBNull.Value)
                    _metadataProvider.SetPrimaryKeyValue(instance, Convert.ChangeType(id, _metadataProvider.PrimaryKeyPropertyType));
            }
        }

        public void Update(IDbConnection connection, IDbTransaction transaction, T instance)
        {
            var pkValue = _metadataProvider.GetPrimaryKeyValue(instance);
            var param = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(param, _metadataProvider.PrimaryKeyPropertyName);
            var constant = Expression.Constant(pkValue, property.Type);
            var equality = Expression.Equal(property, constant);
            var predicate = Expression.Lambda<Func<T, bool>>(equality, param);

            Update(connection, transaction, instance, predicate);
        }

        public void Update(IDbConnection connection, IDbTransaction transaction, T instance, Expression<Func<T, bool>> predicate)
        {
            var queryCriteria = new QueryCriteria<T> { Predicate = predicate };
            var list = new List<T>(Select(connection, transaction, queryCriteria));

            if (list.Count != 1)
                throw new ApplicationException($"Expected to find 1 {typeof(T).Name} but found {list.Count}");

            using (IDbCommand command = _commandBuilder.GetUpdateCommand(connection, instance, list[0], predicate))
            {
                command.Transaction = transaction;
                ExecuteNonQuery(command);
            }
        }

        public void Delete(IDbConnection connection, IDbTransaction transaction, T instance)
        {
            var pkValue = _metadataProvider.GetPrimaryKeyValue(instance);
            var param = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(param, _metadataProvider.PrimaryKeyPropertyName);
            var constant = Expression.Constant(pkValue, property.Type);
            var equality = Expression.Equal(property, constant);
            var predicate = Expression.Lambda<Func<T, bool>>(equality, param);

            Delete(connection, transaction, predicate);
        }

        public void Delete(IDbConnection connection, IDbTransaction transaction, Expression<Func<T, bool>> predicate)
        {
            using (IDbCommand command = _commandBuilder.GetDeleteCommand(connection, predicate))
            {
                command.Transaction = transaction;
                command.ExecuteNonQuery();
            }
        }

        #endregion

        #region Async

        public async Task<int> CountAsync(IDbConnection connection, IDbTransaction transaction, Expression<Func<T, bool>> predicate)
        {
            using (IDbCommand command = _commandBuilder.GetCountCommand(connection, predicate))
            {
                command.Transaction = transaction;
                return Convert.ToInt32(await ExecuteScalarAsync((DbCommand)command));
            }
        }

        public async Task<bool> ExistsAsync(IDbConnection connection, IDbTransaction transaction, Expression<Func<T, bool>> predicate)
        {
            using (IDbCommand command = _commandBuilder.GetExistsCommand(connection, predicate))
            {
                command.Transaction = transaction;
                return Convert.ToInt64(await ExecuteScalarAsync((DbCommand)command)) == 1;
            }
        }

        public async Task<IEnumerable<T>> SelectAsync(IDbConnection connection, IDbTransaction transaction, QueryCriteria<T> queryCriteria)
        {
            using (IDbCommand command = _commandBuilder.GetSelectCommand(connection, queryCriteria))
            {
                command.Transaction = transaction;
                return await ExecuteReaderAsync((DbCommand)command);
            }
        }

        public async Task InsertAsync(IDbConnection connection, IDbTransaction transaction, T instance)
        {
            using (IDbCommand command = _commandBuilder.GetInsertCommand(connection, instance))
            {
                command.Transaction = transaction;
                object id = await ExecuteScalarAsync((DbCommand)command);

                if (id != null && id != DBNull.Value)
                    _metadataProvider.SetPrimaryKeyValue(instance, Convert.ChangeType(id, _metadataProvider.PrimaryKeyPropertyType));
            }
        }

        public async Task UpdateAsync(IDbConnection connection, IDbTransaction transaction, T instance)
        {
            var pkValue = _metadataProvider.GetPrimaryKeyValue(instance);
            var param = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(param, _metadataProvider.PrimaryKeyPropertyName);
            var constant = Expression.Constant(pkValue, property.Type);
            var equality = Expression.Equal(property, constant);
            var predicate = Expression.Lambda<Func<T, bool>>(equality, param);

            await UpdateAsync(connection, transaction, instance, predicate);
        }

        public async Task UpdateAsync(IDbConnection connection, IDbTransaction transaction, T instance, Expression<Func<T, bool>> predicate)
        {
            var queryCriteria = new QueryCriteria<T> { Predicate = predicate };
            var list = new List<T>(await SelectAsync(connection, transaction, queryCriteria));

            if (list.Count != 1)
                throw new ApplicationException($"Expected to find 1 {typeof(T).Name} but found {list.Count}");

            using (IDbCommand command = _commandBuilder.GetUpdateCommand(connection, instance, list[0], predicate))
            {
                command.Transaction = transaction;
                await ExecuteNonQueryAsync((DbCommand)command);
            }
        }

        public async Task DeleteAsync(IDbConnection connection, IDbTransaction transaction, T instance)
        {
            var pkValue = _metadataProvider.GetPrimaryKeyValue(instance);
            var param = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(param, _metadataProvider.PrimaryKeyPropertyName);
            var constant = Expression.Constant(pkValue, property.Type);
            var equality = Expression.Equal(property, constant);
            var predicate = Expression.Lambda<Func<T, bool>>(equality, param);

            await DeleteAsync(connection, transaction, predicate);
        }

        public async Task DeleteAsync(IDbConnection connection, IDbTransaction transaction, Expression<Func<T, bool>> predicate)
        {
            using (IDbCommand command = _commandBuilder.GetDeleteCommand(connection, predicate))
            {
                command.Transaction = transaction;
                await ((DbCommand)command).ExecuteNonQueryAsync();
            }
        }

        #endregion

        #region Execution helpers

        private void ExecuteNonQuery(IDbCommand command)
        {
            WriteCommand(command);

            using (new OperationTimer(_debugLogger))
            {
                command.ExecuteNonQuery();
            }
        }

        private object ExecuteScalar(IDbCommand command)
        {
            WriteCommand(command);

            using (new OperationTimer(_debugLogger))
            {
                return command.ExecuteScalar();
            }
        }

        private IEnumerable<T> ExecuteReader(IDbCommand command)
        {
            WriteCommand(command);

            using (new OperationTimer(_debugLogger))
            {
                var list = new List<T>();

                using (IDataReader dataReader = command.ExecuteReader())
                {
                    PropertyInfo[] propertyInfos = null;

                    while (dataReader.Read())
                    {
                        if (propertyInfos == null)
                            propertyInfos = GetPropertyInfos(dataReader);

                        list.Add(GetInstance(dataReader, propertyInfos));
                    }
                }

                return list;
            }
        }

        private async Task ExecuteNonQueryAsync(DbCommand command)
        {
            WriteCommand(command);

            using (new OperationTimer(_debugLogger))
            {
                await command.ExecuteNonQueryAsync();
            }
        }

        private async Task<object> ExecuteScalarAsync(DbCommand command)
        {
            WriteCommand(command);

            using (new OperationTimer(_debugLogger))
            {
                return await command.ExecuteScalarAsync();
            }
        }

        private async Task<IEnumerable<T>> ExecuteReaderAsync(DbCommand command)
        {
            WriteCommand(command);

            using (new OperationTimer(_debugLogger))
            {
                var list = new List<T>();

                using (var dataReader = await command.ExecuteReaderAsync())
                {
                    PropertyInfo[] propertyInfos = null;

                    while (await dataReader.ReadAsync())
                    {
                        if (propertyInfos == null)
                            propertyInfos = GetPropertyInfos(dataReader);

                        list.Add(GetInstance(dataReader, propertyInfos));
                    }
                }

                return list;
            }
        }

        #endregion

        #region Mapping

        private PropertyInfo[] GetPropertyInfos(IDataRecord reader)
        {
            var array = new PropertyInfo[reader.FieldCount];

            for (var i = 0; i < reader.FieldCount; i++)
            {
                string columnName = reader.GetName(i);
                var propertyMap = _metadataProvider.GetPropertyMapByColumnName(columnName);
                array[i] = propertyMap?.PropertyInfo;
            }

            return array;
        }

        private T GetInstance(IDataRecord reader, IReadOnlyList<PropertyInfo> propertyInfos)
        {
            T instance = (T)Activator.CreateInstance(_metadataProvider.Type);

            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (propertyInfos[i] == null)
                    continue;

                if (reader.IsDBNull(i))
                    continue;

                try
                {
                    object value = reader.GetValue(i);
                    Type targetType = propertyInfos[i].PropertyType;
                    Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

                    if (value.GetType() != underlyingType)
                        value = Convert.ChangeType(value, underlyingType);

                    propertyInfos[i].SetValue(instance, value);
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to set property '{propertyInfos[i].Name}'", e);
                }
            }

            return instance;
        }

        #endregion

        #region Logging

        private void WriteCommand(IDbCommand command)
        {
            WriteLine(command.CommandText);

            foreach (IDbDataParameter param in command.Parameters)
            {
                WriteLine($"@{param.ParameterName} = {param.Value}");
            }
        }

        private void WriteLine(string stringValue)
        {
            _debugLogger.WriteLine(stringValue);
        }

        #endregion
    }
}
