using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Reform.Objects;

namespace Reform.Interfaces
{
    internal interface ICommandBuilder<in T> where T : class
    {
        SqlCommand GetCountCommand(SqlConnection connection, List<Filter> filters);
        SqlCommand GetExistsCommand(SqlConnection connection, List<Filter> filters);
        SqlCommand GetSelectCommand(SqlConnection connection, QueryCriteria queryCriteria);
        SqlCommand GetInsertCommand(SqlConnection connection, T instance);
        SqlCommand GetUpdateCommand(SqlConnection connection, T instance, T original, List<Filter> filters);
        SqlCommand GetDeleteCommand(SqlConnection connection, List<Filter> filters);
        SqlCommand GetMergeCommand(SqlConnection connection, string tempTableName, List<Filter> filters);
    }
}