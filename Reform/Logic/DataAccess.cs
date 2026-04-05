using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
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
        private readonly Func<T> _factory;
        private readonly ParameterExpression _pkParam;
        private readonly MemberExpression _pkProperty;

        public DataAccess(IMetadataProvider<T> metadataProvider, ICommandBuilder<T> commandBuilder, IDebugLogger debugLogger)
        {
            _metadataProvider = metadataProvider;
            _commandBuilder = commandBuilder;
            _debugLogger = debugLogger;
            _factory = Expression.Lambda<Func<T>>(Expression.New(typeof(T))).Compile();
            _pkParam = Expression.Parameter(typeof(T), "x");
            _pkProperty = Expression.Property(_pkParam, _metadataProvider.PrimaryKeyPropertyName);
        }

        private Expression<Func<T, bool>> BuildPkPredicate(object pkValue)
        {
            var constant = Expression.Constant(pkValue, _pkProperty.Type);
            var equality = Expression.Equal(_pkProperty, constant);
            return Expression.Lambda<Func<T, bool>>(equality, _pkParam);
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
            var predicate = BuildPkPredicate(_metadataProvider.GetPrimaryKeyValue(instance));
            Update(connection, transaction, instance, predicate);
        }

        public void Update(IDbConnection connection, IDbTransaction transaction, T instance, Expression<Func<T, bool>> predicate)
        {
            var queryCriteria = new QueryCriteria<T> { Predicate = predicate };
            var list = new List<T>(Select(connection, transaction, queryCriteria));

            if (list.Count != 1)
                throw new InvalidOperationException($"Expected to find 1 {typeof(T).Name} but found {list.Count}.");

            using (IDbCommand command = _commandBuilder.GetUpdateCommand(connection, instance, list[0], predicate))
            {
                if (string.IsNullOrEmpty(command.CommandText))
                    return;

                command.Transaction = transaction;
                ExecuteNonQuery(command);
            }
        }

        public void Delete(IDbConnection connection, IDbTransaction transaction, T instance)
        {
            var predicate = BuildPkPredicate(_metadataProvider.GetPrimaryKeyValue(instance));
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

        public void Truncate(IDbConnection connection, IDbTransaction transaction)
        {
            using (IDbCommand command = _commandBuilder.GetTruncateCommand(connection))
            {
                command.Transaction = transaction;
                ExecuteNonQuery(command);
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
            var predicate = BuildPkPredicate(_metadataProvider.GetPrimaryKeyValue(instance));
            await UpdateAsync(connection, transaction, instance, predicate);
        }

        public async Task UpdateAsync(IDbConnection connection, IDbTransaction transaction, T instance, Expression<Func<T, bool>> predicate)
        {
            var queryCriteria = new QueryCriteria<T> { Predicate = predicate };
            var list = new List<T>(await SelectAsync(connection, transaction, queryCriteria));

            if (list.Count != 1)
                throw new InvalidOperationException($"Expected to find 1 {typeof(T).Name} but found {list.Count}.");

            using (IDbCommand command = _commandBuilder.GetUpdateCommand(connection, instance, list[0], predicate))
            {
                if (string.IsNullOrEmpty(command.CommandText))
                    return;

                command.Transaction = transaction;
                await ExecuteNonQueryAsync((DbCommand)command);
            }
        }

        public async Task DeleteAsync(IDbConnection connection, IDbTransaction transaction, T instance)
        {
            var predicate = BuildPkPredicate(_metadataProvider.GetPrimaryKeyValue(instance));
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

        public async Task TruncateAsync(IDbConnection connection, IDbTransaction transaction)
        {
            using (IDbCommand command = _commandBuilder.GetTruncateCommand(connection))
            {
                command.Transaction = transaction;
                await ExecuteNonQueryAsync((DbCommand)command);
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
                    PropertyMap[] propertyMaps = null;

                    while (dataReader.Read())
                    {
                        if (propertyMaps == null)
                            propertyMaps = GetPropertyMaps(dataReader);

                        list.Add(GetInstance(dataReader, propertyMaps));
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
                    PropertyMap[] propertyMaps = null;

                    while (await dataReader.ReadAsync())
                    {
                        if (propertyMaps == null)
                            propertyMaps = GetPropertyMaps(dataReader);

                        list.Add(GetInstance(dataReader, propertyMaps));
                    }
                }

                return list;
            }
        }

        #endregion

        #region Mapping

        private PropertyMap[] GetPropertyMaps(IDataRecord reader)
        {
            var array = new PropertyMap[reader.FieldCount];

            for (var i = 0; i < reader.FieldCount; i++)
            {
                string columnName = reader.GetName(i);
                array[i] = _metadataProvider.GetPropertyMapByColumnName(columnName);
            }

            return array;
        }

        private T GetInstance(IDataRecord reader, PropertyMap[] propertyMaps)
        {
            T instance = _factory();

            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (propertyMaps[i] == null)
                    continue;

                if (reader.IsDBNull(i))
                    continue;

                try
                {
                    object value = reader.GetValue(i);
                    Type targetType = propertyMaps[i].PropertyType;
                    Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

                    if (value.GetType() != underlyingType)
                        value = Convert.ChangeType(value, underlyingType);

                    propertyMaps[i].SetPropertyValue(instance, value);
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to set property '{propertyMaps[i].PropertyName}'", e);
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
