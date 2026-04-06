using System.Data;
using System.Linq.Expressions;
using Reform.Interfaces;
using Reform.Objects;

namespace Reform.Logic
{
    public sealed class CommandBuilder<T> : ICommandBuilder<T> where T : class
    {
        private readonly IMetadataProvider<T> _metadataProvider;
        private readonly ISqlBuilder<T> _sqlBuilder;
        private readonly IDialect _dialect;

        public CommandBuilder(IMetadataProvider<T> metadataProvider, ISqlBuilder<T> sqlBuilder, IDialect dialect)
        {
            _metadataProvider = metadataProvider;
            _sqlBuilder = sqlBuilder;
            _dialect = dialect;
        }

        public IDbCommand GetCountCommand(IDbConnection connection, Expression<Func<T, bool>> predicate)
        {
            var commandText = _sqlBuilder.GetCountSql(predicate, out var parameters);
            return GetCommand(connection, commandText, parameters);
        }

        public IDbCommand GetExistsCommand(IDbConnection connection, Expression<Func<T, bool>> predicate)
        {
            var commandText = _sqlBuilder.GetExistsSql(predicate, out var parameters);
            return GetCommand(connection, commandText, parameters);
        }

        public IDbCommand GetSelectCommand(IDbConnection connection, QueryCriteria<T> queryCriteria)
        {
            var doPaging = queryCriteria.PageCriteria.PageSize != 0 &&
                           queryCriteria.PageCriteria.Page != 0;

            if (doPaging && queryCriteria.SortCriteria.Count == 0)
            {
                queryCriteria.SortCriteria.Add(SortCriterion.Ascending(_metadataProvider.PrimaryKeyPropertyName));
            }

            var parameters = new Dictionary<string, object>();
            var commandText = _sqlBuilder.GetSelectSql(queryCriteria, ref parameters);

            return GetCommand(connection, commandText, parameters);
        }

        public IDbCommand GetInsertCommand(IDbConnection connection, T instance)
        {
            var parameters = new Dictionary<string, object>();
            var commandText = _sqlBuilder.GetInsertSql(instance, ref parameters);

            return GetCommand(connection, $"{commandText}; {_dialect.IdentitySql}", parameters);
        }

        public IDbCommand GetUpdateCommand(IDbConnection connection, T instance, T original,
                                           Expression<Func<T, bool>> predicate)
        {
            var parameters = new Dictionary<string, object>();
            var commandText = _sqlBuilder.GetUpdateSql(instance, original, ref parameters, predicate);

            return GetCommand(connection, commandText, parameters);
        }

        public IDbCommand GetDeleteCommand(IDbConnection connection, Expression<Func<T, bool>> predicate)
        {
            var parameters = new Dictionary<string, object>();
            var commandText = _sqlBuilder.GetDeleteSql(predicate, ref parameters);

            return GetCommand(connection, commandText, parameters);
        }

        public IDbCommand GetTruncateCommand(IDbConnection connection)
        {
            var commandText = _sqlBuilder.GetTruncateSql();

            return GetCommand(connection, commandText, new Dictionary<string, object>());
        }

        private IDbCommand GetCommand(IDbConnection connection, string commandText, Dictionary<string, object> parameters)
        {
            var command = _dialect.CreateCommand(commandText, connection);

            foreach (var param in parameters.Keys)
            {
                var p = command.CreateParameter();
                p.ParameterName = $"{_dialect.ParameterPrefix}{param}";
                p.Value = parameters[param];
                command.Parameters.Add(p);
            }

            return command;
        }
    }
}
