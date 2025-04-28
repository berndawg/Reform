using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using MySql.Data.MySqlClient;
using Reform.Interfaces;
using Reform.Objects;

namespace Reform.Logic
{
    public class CommandBuilder<T> : ICommandBuilder<T> where T : class
    {
        #region Fields

        private readonly IMetadataProvider<T> _metadataProvider;
        private readonly ISqlBuilder<T> _sqlBuilder;
        private readonly DbProviderFactory _dbProviderFactory;

        #endregion

        #region Constructors

        public CommandBuilder(ISqlBuilder<T> sqlBuilder, IMetadataProvider<T> metadataProvider)
        {
            _sqlBuilder = sqlBuilder;
            _metadataProvider = metadataProvider;
            _dbProviderFactory = MySqlClientFactory.Instance;
        }

        #endregion

        #region Expression-based methods

        public IDbCommand GetCountCommand(IDbConnection connection, Query<T> query)
        {
            var command = connection.CreateCommand();
            command.CommandText = _sqlBuilder.GetCountSql(query, out var parameters);
            AddParameters(command, parameters);
            return command;
        }

        public IDbCommand GetExistsCommand(IDbConnection connection, Query<T> query)
        {
            var command = connection.CreateCommand();
            command.CommandText = _sqlBuilder.GetExistsSql(query, out var parameters);
            AddParameters(command, parameters);
            return command;
        }

        public IDbCommand GetSelectCommand(IDbConnection connection, Query<T> query)
        {
            var command = connection.CreateCommand();
            command.CommandText = _sqlBuilder.GetSelectSql(query, out var parameters);
            AddParameters(command, parameters);
            return command;
        }

        public IDbCommand GetInsertCommand(IDbConnection connection, T instance)
        {
            var command = connection.CreateCommand();
            var parameters = new Dictionary<string, object>();
            command.CommandText = _sqlBuilder.GetInsertSql(instance, ref parameters);
            AddParameters(command, parameters);
            return command;
        }

        public IDbCommand GetUpdateCommand(IDbConnection connection, T instance)
        {
            var command = connection.CreateCommand();
            var parameters = new Dictionary<string, object>();
            var predicate = GetPrimaryKeyPredicate(instance);
            command.CommandText = _sqlBuilder.GetUpdateSql(instance, null, predicate, ref parameters);
            AddParameters(command, parameters);
            return command;
        }

        public IDbCommand GetUpdateCommand(IDbConnection connection, T instance, Query<T> query)
        {
            var command = connection.CreateCommand();
            command.CommandText = _sqlBuilder.GetUpdateSql(instance, query, out var parameters);
            AddParameters(command, parameters);
            return command;
        }

        public IDbCommand GetDeleteCommand(IDbConnection connection, T instance)
        {
            var command = connection.CreateCommand();
            var query = new Query<T>().Where(GetPrimaryKeyPredicate(instance));
            command.CommandText = _sqlBuilder.GetDeleteSql(query, out var parameters);
            AddParameters(command, parameters);
            return command;
        }

        public IDbCommand GetDeleteCommand(IDbConnection connection, Query<T> query)
        {
            var command = connection.CreateCommand();
            command.CommandText = _sqlBuilder.GetDeleteSql(query, out var parameters);
            AddParameters(command, parameters);
            return command;
        }

        public IDbCommand GetMergeCommand(IDbConnection connection, List<T> list, Query<T> query)
        {
            var command = connection.CreateCommand();
            command.CommandText = _sqlBuilder.GetMergeSql(list, query, out var parameters);
            AddParameters(command, parameters);
            return command;
        }

        #endregion

        private void AddParameters(IDbCommand command, Dictionary<string, object> parameters)
        {
            foreach (var parameter in parameters)
            {
                var dbParameter = command.CreateParameter();
                dbParameter.ParameterName = parameter.Key;
                dbParameter.Value = parameter.Value ?? DBNull.Value;
                command.Parameters.Add(dbParameter);
            }
        }

        private Expression<Func<T, bool>> GetPrimaryKeyPredicate(T instance)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            
            // Get the primary key property name and value
            var propertyName = _metadataProvider.PrimaryKeyPropertyName;
            var propertyMap = _metadataProvider.GetPropertyMapByPropertyName(propertyName);
            var value = _metadataProvider.GetPrimaryKeyValue(instance);
            
            // Build the expression
            var propertyAccess = Expression.Property(parameter, propertyMap.PropertyInfo);
            var constant = Expression.Constant(value, propertyMap.PropertyType);
            var equals = Expression.Equal(propertyAccess, constant);

            return Expression.Lambda<Func<T, bool>>(equals, parameter);
        }
    }
}
