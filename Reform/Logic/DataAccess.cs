using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Reform.Interfaces;
using Reform.Objects;

namespace Reform.Logic
{
    internal sealed class DataAccess<T> : IDataAccess<T> where T : class
    {
        private readonly IMetadataProvider<T> _metadataProvider;
        private readonly ICommandBuilder<T> _commandBuilder;
        private readonly IDebugLogger _debugLogger;

        internal DataAccess(IMetadataProvider<T> metadataProvider, ICommandBuilder<T> commandBuilder, IDebugLogger debugLogger)
        {
            _metadataProvider = metadataProvider;
            _commandBuilder = commandBuilder;
            _debugLogger = debugLogger;
        }

        public int Count(IDbConnection connection, Expression<Func<T, bool>> predicate)
        {
            using (IDbCommand command = _commandBuilder.GetCountCommand(connection, predicate))
            {
                return Convert.ToInt32(ExecuteScalar(command));
            }
        }

        public bool Exists(IDbConnection connection, Expression<Func<T, bool>> predicate)
        {
            using (IDbCommand command = _commandBuilder.GetExistsCommand(connection, predicate))
            {
                return Convert.ToInt64(ExecuteScalar(command)) == 1;
            }
        }

        public IEnumerable<T> Select(IDbConnection connection, QueryCriteria<T> queryCriteria)
        {
            using (IDbCommand command = _commandBuilder.GetSelectCommand(connection, queryCriteria))
            {
                return ExecuteReader(command);
            }
        }

        public void Insert(IDbConnection connection, T instance)
        {
            using (IDbCommand command = _commandBuilder.GetInsertCommand(connection, instance))
            {
                object id = ExecuteScalar(command);

                if (id != null && id != DBNull.Value)
                    _metadataProvider.SetPrimaryKeyValue(instance, Convert.ToInt32(id));
            }
        }

        public void Update(IDbConnection connection, T instance)
        {
            // Build a predicate for PK = value
            var pkValue = _metadataProvider.GetPrimaryKeyValue(instance);
            var param = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(param, _metadataProvider.PrimaryKeyPropertyName);
            var constant = Expression.Constant(pkValue, property.Type);
            var equality = Expression.Equal(property, constant);
            var predicate = Expression.Lambda<Func<T, bool>>(equality, param);

            Update(connection, instance, predicate);
        }

        public void Update(IDbConnection connection, T instance, Expression<Func<T, bool>> predicate)
        {
            var queryCriteria = new QueryCriteria<T> { Predicate = predicate };
            var list = new List<T>(Select(connection, queryCriteria));

            if (list.Count != 1)
                throw new ApplicationException($"Expected to find 1 {typeof(T).Name} but found {list.Count}");

            using (IDbCommand command = _commandBuilder.GetUpdateCommand(connection, instance, list[0], predicate))
            {
                ExecuteNonQuery(command);
            }
        }

        public void Delete(IDbConnection connection, T instance)
        {
            var pkValue = _metadataProvider.GetPrimaryKeyValue(instance);
            var param = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(param, _metadataProvider.PrimaryKeyPropertyName);
            var constant = Expression.Constant(pkValue, property.Type);
            var equality = Expression.Equal(property, constant);
            var predicate = Expression.Lambda<Func<T, bool>>(equality, param);

            Delete(connection, predicate);
        }

        public void Delete(IDbConnection connection, Expression<Func<T, bool>> predicate)
        {
            using (IDbCommand command = _commandBuilder.GetDeleteCommand(connection, predicate))
            {
                command.ExecuteNonQuery();
            }
        }

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

        private PropertyInfo[] GetPropertyInfos(IDataRecord reader)
        {
            PropertyInfo[] propertyInfos = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);

            var array = new PropertyInfo[reader.FieldCount];

            for (var i = 0; i < reader.FieldCount; i++)
            {
                string columnName = reader.GetName(i);
                array[i] = propertyInfos.FirstOrDefault(x => string.Compare(x.Name, columnName, StringComparison.OrdinalIgnoreCase) == 0);
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

                    // SQLite returns Int64 for integers, need to convert
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
    }
}
