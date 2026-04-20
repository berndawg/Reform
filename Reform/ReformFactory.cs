using Microsoft.Extensions.DependencyInjection;
using Reform.Interfaces;

namespace Reform;

public class ReformFactory : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    internal ReformFactory(ServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IReform<T> For<T>() where T : class
    {
        return _serviceProvider.GetRequiredService<IReform<T>>();
    }

    public T Resolve<T>() where T : notnull
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    public string CodeGen(string tableName)
    {
        return Resolve<ICodeGenerator>().CodeGen(tableName);
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}
