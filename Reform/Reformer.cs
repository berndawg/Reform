using Microsoft.Extensions.DependencyInjection;
using Reform.Dialects;
using Reform.Interfaces;
using Reform.Logic;

namespace Reform
{
    public class Reformer
    {
        private Type _dialectType = typeof(SqliteDialect);
        private string? _connectionString;
        private readonly Dictionary<Type, Type> _registrations = new();
        private readonly Dictionary<Type, object> _instances = new();
        private readonly Dictionary<Type, Func<IServiceProvider, object>> _factories = new();

        public Reformer UseSqlite(string? connectionString = null)
        {
            _dialectType = typeof(SqliteDialect);
            _connectionString = connectionString;
            return this;
        }

        public Reformer UseSqlServer(string? connectionString = null)
        {
            _dialectType = typeof(SqlServerDialect);
            _connectionString = connectionString;
            return this;
        }

        public Reformer UseMySql(string? connectionString = null)
        {
            _dialectType = typeof(MySqlDialect);
            _connectionString = connectionString;
            return this;
        }

        public Reformer UsePostgreSql(string? connectionString = null)
        {
            _dialectType = typeof(PostgreSqlDialect);
            _connectionString = connectionString;
            return this;
        }

        public Reformer Register(Type serviceType, Type implementationType)
        {
            _registrations[serviceType] = implementationType;
            return this;
        }

        public Reformer Register<TService>(TService instance) where TService : notnull
        {
            _instances[typeof(TService)] = instance;
            return this;
        }

        public Reformer Register<TService>(Func<IServiceProvider, TService> factory) where TService : notnull
        {
            _factories[typeof(TService)] = sp => factory(sp);
            return this;
        }

        internal void SetDialect(Type dialectType)
        {
            _dialectType = dialectType;
        }

        public ReformFactory Build()
        {
            var services = new ServiceCollection();

            // Core infrastructure
            services.AddSingleton(typeof(IDialect), _dialectType);
            services.AddSingleton<IDebugLogger, DebugLogger>();

            // Connection string provider
            services.AddSingleton<IConnectionStringProvider>(new DefaultConnectionStringProvider(_connectionString));

            // Generic services
            services.AddSingleton(typeof(IMetadataProvider<>), typeof(MetadataProvider<>));
            services.AddSingleton(typeof(IReform<>), typeof(Reform<>));
            services.AddSingleton(typeof(IDataAccess<>), typeof(DataAccess<>));
            services.AddSingleton(typeof(IConnectionProvider<>), typeof(ConnectionProvider<>));
            services.AddSingleton(typeof(ICommandBuilder<>), typeof(CommandBuilder<>));
            services.AddSingleton(typeof(ISqlBuilder<>), typeof(SqlBuilder<>));
            services.AddSingleton(typeof(IValidator<>), typeof(Validator<>));
            services.AddSingleton<ICodeGenerator, CodeGenerator>();

            // Custom type registrations (override defaults)
            foreach (var (serviceType, implType) in _registrations)
                services.AddSingleton(serviceType, implType);

            // Custom instance registrations (override defaults)
            foreach (var (serviceType, instance) in _instances)
                services.AddSingleton(serviceType, instance);

            // Custom factory registrations (override defaults, including instance/type registrations above)
            foreach (var (serviceType, factory) in _factories)
                services.AddSingleton(serviceType, factory);

            return new ReformFactory(services.BuildServiceProvider());
        }
    }
}
