using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Reform.Interfaces;
using Reform.Objects;

namespace Reform.Logic
{
    internal sealed class CommandBuilder<T> : ICommandBuilder<T> where T : class
    {
        #region Fields

        private readonly IMetadataProvider<T> _metadataProvider;
        private readonly ISqlBuilder<T> _sqlBuilder;

        #endregion

        #region Constructors

        internal CommandBuilder(IMetadataProvider<T> metadataProvider, ISqlBuilder<T> sqlBuilder)
        {
            _metadataProvider = metadataProvider;
            _sqlBuilder = sqlBuilder;
        }

        #endregion

        public SqlCommand GetCountCommand(SqlConnection connection, List<Filter> filters)
        {
            string commandText = _sqlBuilder.GetCountSql(filters, out Dictionary<string, object> parameterDictionary);
            return GetCommand(connection, commandText, parameterDictionary);
        }

        public SqlCommand GetExistsCommand(SqlConnection connection, List<Filter> filters)
        {
            string commandText = _sqlBuilder.GetExistsSql(filters, out Dictionary<string, object> parameterDictionary);
            return GetCommand(connection, commandText, parameterDictionary);
        }

        public SqlCommand GetSelectCommand(SqlConnection connection, QueryCriteria queryCriteria)
        {
            bool doPaging = queryCriteria.PageCriteria != null && queryCriteria.PageCriteria.PageSize != 0 &&
                            queryCriteria.PageCriteria.Page != 0;

            if (doPaging)
            {
                if (queryCriteria.SortCriteria.Count == 0)
                    queryCriteria.SortCriteria.Add(SortCriterion.Ascending(_metadataProvider.PrimaryKeyPropertyName));
            }

            var parameterDictionary = new Dictionary<string, object>();

            string commandText = _sqlBuilder.GetSelectSql(queryCriteria, ref parameterDictionary);

            return GetCommand(connection, commandText, parameterDictionary);
        }

        public SqlCommand GetInsertCommand(SqlConnection connection, T instance)
        {
            var parameterDictionary = new Dictionary<string, object>();

            string commandText = _sqlBuilder.GetInsertSql(instance, ref parameterDictionary);

            return GetCommand(connection, $"{commandText}; SELECT SCOPE_IDENTITY()", parameterDictionary);
        }

        public SqlCommand GetUpdateCommand(SqlConnection connection, T instance, T original, List<Filter> filters)
        {
            var parameterDictionary = new Dictionary<string, object>();
            string commandText = _sqlBuilder.GetUpdateSql(instance, original, ref parameterDictionary, filters);

            return GetCommand(connection, commandText, parameterDictionary);
        }

        public SqlCommand GetDeleteCommand(SqlConnection connection, List<Filter> filters)
        {
            var parameterDictionary = new Dictionary<string, object>();

            string commandText = _sqlBuilder.GetDeleteSql(filters, ref parameterDictionary);

            return GetCommand(connection, commandText, parameterDictionary);
        }

        public SqlCommand GetMergeCommand(SqlConnection connection, string tempTableName, List<Filter> filters)
        {
            var parameterDictionary = new Dictionary<string, object>();

            string commandText = _sqlBuilder.GetMergeSql(tempTableName, filters, ref parameterDictionary);

            return GetCommand(connection, commandText, parameterDictionary);
        }

        private SqlCommand GetCommand(SqlConnection connection, string commandText, Dictionary<string, object> parameterDictionary)
        {
            var command = new SqlCommand(commandText, connection);

            if (parameterDictionary.Keys.Count > 2100)
                throw new ApplicationException("The maximum of 2100 parameters has been exceeded");

            foreach (string param in parameterDictionary.Keys)
                command.Parameters.AddWithValue(param, parameterDictionary[param] ?? DBNull.Value);

            return command;
        }

    }
}
