using System.Data;
using System.Linq.Expressions;
using Reform.Interfaces;
using Reform.Objects;

namespace Reform.Logic;

public sealed class CommandBuilder<T>(
    IMetadataProvider<T> metadataProvider,
    ISqlBuilder<T> sqlBuilder,
    IDialect dialect)
    : ICommandBuilder<T>
    where T : class
{
    public IDbCommand GetCountCommand(IDbConnection connection, Expression<Func<T, bool>>? predicate)
    {
        var commandText = sqlBuilder.GetCountSql(predicate, out var parameters);
        return GetCommand(connection, commandText, parameters);
    }

    public IDbCommand GetExistsCommand(IDbConnection connection, Expression<Func<T, bool>> predicate)
    {
        var commandText = sqlBuilder.GetExistsSql(predicate, out var parameters);
        return GetCommand(connection, commandText, parameters);
    }

    public IDbCommand GetSelectCommand(IDbConnection connection, QueryCriteria<T> queryCriteria)
    {
        var doPaging = queryCriteria.PageCriteria != null && queryCriteria.PageCriteria.PageSize != 0 &&
                       queryCriteria.PageCriteria.Page != 0;

        if (doPaging)
        {
            if (queryCriteria.SortCriteria.Count == 0)
                queryCriteria.SortCriteria.Add(SortCriterion.Ascending(metadataProvider.PrimaryKeyPropertyName));
        }

        var parameters = new Dictionary<string, object>();
        var commandText = sqlBuilder.GetSelectSql(queryCriteria, ref parameters);

        return GetCommand(connection, commandText, parameters);
    }

    public IDbCommand GetInsertCommand(IDbConnection connection, T instance)
    {
        var parameters = new Dictionary<string, object>();
        var commandText = sqlBuilder.GetInsertSql(instance, ref parameters);

        return GetCommand(connection, $"{commandText}; {dialect.IdentitySql}", parameters);
    }

    public IDbCommand GetUpdateCommand(IDbConnection connection, T instance, T original,
                                       Expression<Func<T, bool>>? predicate)
    {
        var parameters = new Dictionary<string, object>();
        var commandText = sqlBuilder.GetUpdateSql(instance, original, ref parameters, predicate);

        return GetCommand(connection, commandText, parameters);
    }

    public IDbCommand GetDeleteCommand(IDbConnection connection, Expression<Func<T, bool>>? predicate)
    {
        var parameters = new Dictionary<string, object>();
        var commandText = sqlBuilder.GetDeleteSql(predicate, ref parameters);

        return GetCommand(connection, commandText, parameters);
    }

    private IDbCommand GetCommand(IDbConnection connection, string commandText, Dictionary<string, object> parameters)
    {
        var command = dialect.CreateCommand(commandText, connection);

        foreach (var param in parameters.Keys)
        {
            var p = command.CreateParameter();
            p.ParameterName = $"{dialect.ParameterPrefix}{param}";
            p.Value = parameters[param] ?? DBNull.Value;
            command.Parameters.Add(p);
        }

        return command;
    }
}
