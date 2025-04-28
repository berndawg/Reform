// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using MySql.Data.MySqlClient;
using Reform.Extensions;
using Reform.Interfaces;
using Reform.Objects;

namespace Reform.Logic
{
    internal sealed class DataAccess<T> : IDataAccess<T> where T : class
    {
        #region Fields

        private readonly IMetadataProvider<T> _metadataProvider;
        private readonly ICommandBuilder<T> _commandBuilder;
        private readonly IDebugLogger _debugLogger;
        private readonly DbProviderFactory _dbProviderFactory;

        #endregion

        #region Constructors

        internal DataAccess(IMetadataProvider<T> metadataProvider, ICommandBuilder<T> commandBuilder, IDebugLogger debugLogger)
        {
            _metadataProvider = metadataProvider;
            _commandBuilder = commandBuilder;
            _debugLogger = debugLogger;
            _dbProviderFactory = MySqlClientFactory.Instance;
        }

        #endregion

        #region Implementation of IDataAccess

        #region Exists

        public int Count(IDbConnection connection, Query<T> query)
        {
            using (IDbCommand command = _commandBuilder.GetCountCommand(connection, query))
            {
                return Convert.ToInt32(ExecuteScalar(command));
            }
        }

        public bool Exists(IDbConnection connection, Query<T> query)
        {
            using (IDbCommand command = _commandBuilder.GetExistsCommand(connection, query))
            {
                return Convert.ToBoolean(ExecuteScalar(command));
            }
        }

        #endregion

        #region Select

        public IEnumerable<T> Select(IDbConnection connection, Query<T> query)
        {
            using (IDbCommand command = _commandBuilder.GetSelectCommand(connection, query))
            {
                return ExecuteReader(command);
            }
        }

        #endregion

        #region Insert

        public void Insert(IDbConnection connection, T instance)
        {
            using (IDbCommand command = _commandBuilder.GetInsertCommand(connection, instance))
            {
                object id = ExecuteScalar(command);

                if (id is decimal decimalValue)
                    _metadataProvider.SetPrimaryKeyValue(instance, (int)decimalValue);
            }
        }

        #endregion

        #region Update

        public void Update(IDbConnection connection, T instance)
        {
            using (IDbCommand command = _commandBuilder.GetUpdateCommand(connection, instance))
            {
                ExecuteNonQuery(command);
            }
        }

        public void Update(IDbConnection connection, T instance, Query<T> query)
        {
            using (IDbCommand command = _commandBuilder.GetUpdateCommand(connection, instance, query))
            {
                ExecuteNonQuery(command);
            }
        }

        #endregion

        #region Delete

        public void Delete(IDbConnection connection, T instance)
        {
            using (IDbCommand command = _commandBuilder.GetDeleteCommand(connection, instance))
            {
                ExecuteNonQuery(command);
            }
        }

        public void Delete(IDbConnection connection, Query<T> query)
        {
            using (IDbCommand command = _commandBuilder.GetDeleteCommand(connection, query))
            {
                ExecuteNonQuery(command);
            }
        }

        #endregion

        public void Truncate(IDbConnection connection)
        {
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = $"TRUNCATE TABLE {_metadataProvider.SchemaName}.{_metadataProvider.TableName};";
                        ExecuteNonQuery(command);
                    }
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public void BulkInsert(IDbConnection connection, List<T> list)
        {
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    foreach (T item in list)
                    {
                        using (IDbCommand command = _commandBuilder.GetInsertCommand(connection, item))
                        {
                            command.Transaction = transaction;
                            ExecuteNonQuery(command);
                        }
                    }
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public void Merge(IDbConnection connection, List<T> list, Query<T> query)
        {
            using (IDbCommand command = _commandBuilder.GetMergeCommand(connection, list, query))
            {
                ExecuteNonQuery(command);
            }
        }

        #endregion

        #region Helpers

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
                using (IDataReader dataReader = command.ExecuteReader())
                {
                    PropertyInfo[] propertyInfos = null;

                    while (dataReader.Read())
                    {
                        if (propertyInfos == null)
                            propertyInfos = GetPropertyInfos(dataReader);

                        yield return GetInstance(dataReader, propertyInfos);
                    }
                }
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
                    propertyInfos[i].SetValue(instance, reader.GetValue(i));
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to set property '{propertyInfos[i].Name}'", e);
                }
            }

            return instance;
        }

        public DataTable GetDataTable(IEnumerable<T> list)
        {
            var dataTable = new DataTable(_metadataProvider.TableName);

            foreach (PropertyMap propertyMap in _metadataProvider.AllProperties)
            {
                dataTable.Columns.Add(new DataColumn(propertyMap.ColumnName, Nullable.GetUnderlyingType(propertyMap.PropertyType) ?? propertyMap.PropertyType));
            }

            foreach (T item in list)
            {
                dataTable.Rows.Add(GetDataRow(dataTable, item));
            }

            return dataTable;
        }

        private DataRow GetDataRow(DataTable dataTable, T item)
        {
            DataRow dataRow = dataTable.NewRow();
            dataRow.ItemArray = _metadataProvider.AllProperties.Select(propertyMap => propertyMap.GetPropertyValue(item)).ToArray();
            return dataRow;
        }

        private void WriteCommand(IDbCommand command)
        {
            WriteLine(command.CommandText);

            foreach (IDataParameter param in command.Parameters)
            {
                WriteLine($"@{param.ParameterName} = {param.Value}");
            }
        }

        private void WriteLine(string text)
        {
            _debugLogger.WriteLine(text);
        }

        #endregion
    }
}