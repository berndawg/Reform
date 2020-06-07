namespace Reform.Interfaces
{
    // ReSharper disable once UnusedTypeParameter
    public interface IConnectionStringProvider
    {
        string GetConnectionString(string databaseName);
    }
}