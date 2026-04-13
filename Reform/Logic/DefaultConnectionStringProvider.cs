using Reform.Interfaces;

namespace Reform.Logic;

internal sealed class DefaultConnectionStringProvider(string? connectionString) : IConnectionStringProvider
{
    public string GetConnectionString(string databaseName)
    {
        return connectionString!;
    }
}
