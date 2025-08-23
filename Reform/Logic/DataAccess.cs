// Copyright (c) 2020 Bernie Seabrook. All Rights Reserved.
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
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

        #endregion

        #region Constructors

        internal DataAccess(IMetadataProvider<T> metadataProvider, ICommandBuilder<T> commandBuilder, IDebugLogger debugLogger)
        {
            _metadataProvider = metadataProvider;
            _commandBuilder = commandBuilder;
            _debugLogger = debugLogger;
        }

        #endregion

        #region Implementation of IDataAccess

        #region Exists

        public int Count(SqlConnection connection, List<Filter> filters)
        {
            using (SqlCommand command = _commandBuilder.GetCountCommand(connection, filters))
            {
                return Convert.ToInt32(ExecuteScalar(command));
            }
        }

        public bool Exists(SqlConnection connection, List<Filter> filters)
        {
            using (SqlCommand command = _commandBuilder.GetExistsCommand(connection, filters))
            {
                return Convert.ToBoolean(ExecuteScalar(command));
            }
        }

        #endregion

        #region Select

        public IEnumerable<T> Select(SqlConnection connection, List<Filter> filters)
        {
            return Select(connection, new QueryCriteria
            {
                Filters = filters,
                PageCriteria = PageCriteria.All()
            });
        }

        public IEnumerable<T> Select(SqlConnection connection, QueryCriteria queryCriteria)
        {
            using (SqlCommand command = _commandBuilder.GetSelectCommand(connection, queryCriteria))
            {
                return ExecuteReader(command);
            }
        }

        #endregion

        #region Insert

        public void Insert(SqlConnection connection, T instance)
        {
            using (SqlCommand command = _commandBuilder.GetInsertCommand(connection, instance))
            {
                object id = ExecuteScalar(command);

                if (id is decimal decimalValue)
                    _metadataProvider.SetPrimaryKeyValue(instance, (int)decimalValue);
            }
        }

        #endregion

        #region Update

        public void Update(SqlConnection connection, T instance)
        {
            var filters = new List<Filter>
            {
                Filter.EqualTo(_metadataProvider.PrimaryKeyPropertyName, _metadataProvider.GetPrimaryKeyValue(instance))
            };

            Update(connection, instance, filters);
        }

        public void Update(SqlConnection connection, T instance, List<Filter> filters)
        {
            var list = new List<T>(Select(connection, filters));

            if (list.Count != 1)
                throw new ApplicationException($"Expected to find 1 {typeof(T).Name} but found {list.Count} using the criteria: {filters.ToText()}");

            using (SqlCommand command = _commandBuilder.GetUpdateCommand(connection, instance, list[0], filters))
            {
                ExecuteNonQuery(command);
            }
        }

        #endregion

        #region Delete

        public void Delete(SqlConnection connection, T instance)
        {
            string propertyName = _metadataProvider.PrimaryKeyPropertyName;
            object propertyValue = _metadataProvider.GetPrimaryKeyValue(instance);

            Delete(connection, new List<Filter>
            {
                Filter.EqualTo(propertyName, propertyValue)
            });
        }

        public void Delete(SqlConnection connection, List<Filter> filters)
        {
            using (SqlCommand command = _commandBuilder.GetDeleteCommand(connection, filters))
            {
                command.ExecuteNonQuery();
            }
        }

        #endregion

        public void Truncate(SqlConnection connection)
        {
            using (var command = new SqlCommand($"TRUNCATE TABLE [{_metadataProvider.SchemaName}].[{_metadataProvider.TableName}];", connection))
            {
                ExecuteNonQuery(command);
            }
        }

        public void BulkInsert(SqlConnection connection, List<T> list)
        {
            using (var bulk = new SqlBulkCopy(connection))
            {
                bulk.DestinationTableName = $"[{_metadataProvider.SchemaName}].[{_metadataProvider.TableName}]";
                bulk.WriteToServer(GetDataTable(list));
            }
        }

        public void Merge(SqlConnection connection, List<T> list, List<Filter> filters)
        {
            string tempTableName = $"#{_metadataProvider.TableName}Temp";

            string commandText = $"SELECT * INTO {tempTableName} FROM [{_metadataProvider.SchemaName}].[{_metadataProvider.TableName}] WHERE 1=2";

            using (var command = new SqlCommand(commandText, connection))
            {
                ExecuteNonQuery(command);
            }

            using (SqlCommand command = _commandBuilder.GetMergeCommand(connection, tempTableName, filters))
            {
                using (var bulk = new SqlBulkCopy(connection))
                {
                    bulk.DestinationTableName = tempTableName;
                    bulk.WriteToServer(GetDataTable(list));

                    ExecuteNonQuery(command);
                }
            }
        }

        #endregion

        #region Helpers

        private void ExecuteNonQuery(SqlCommand command)
        {
            WriteCommand(command);

            using (new OperationTimer(_debugLogger))
            {
                command.ExecuteNonQuery();
            }
        }

        private object ExecuteScalar(SqlCommand command)
        {
            WriteCommand(command);

            using (new OperationTimer(_debugLogger))
            {
                return command.ExecuteScalar();
            }
        }

        private IEnumerable<T> ExecuteReader(SqlCommand command)
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

        private void WriteCommand(SqlCommand command)
        {
            WriteLine(command.CommandText);

            foreach (SqlParameter param in command.Parameters)
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