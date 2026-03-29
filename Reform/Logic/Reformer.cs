using System;
using Reform.Dialects;
using Reform.Interfaces;

namespace Reform.Logic
{
    public static class Reformer
    {
        private static readonly ReformBuilder _builder = new ReformBuilder();
        private static ReformFactory _factory;
        private static readonly object _lock = new();

        public static void UseSqlite(string connectionString = null)
        {
            lock (_lock)
            {
                _builder.UseSqlite(connectionString);
                InvalidateFactory();
            }
        }

        public static void UseSqlServer(string connectionString = null)
        {
            lock (_lock)
            {
                _builder.UseSqlServer(connectionString);
                InvalidateFactory();
            }
        }

        public static void UseMySql(string connectionString = null)
        {
            lock (_lock)
            {
                _builder.UseMySql(connectionString);
                InvalidateFactory();
            }
        }

        public static void RegisterType(Type type, Type implementation)
        {
            lock (_lock)
            {
                _builder.Register(type, implementation);
                InvalidateFactory();
            }
        }

        public static IReform<T> Reform<T>() where T : class
        {
            lock (_lock)
            {
                EnsureFactory();
                return _factory.For<T>();
            }
        }

        public static T Resolve<T>()
        {
            lock (_lock)
            {
                EnsureFactory();
                return _factory.Resolve<T>();
            }
        }

        private static void EnsureFactory()
        {
            _factory ??= _builder.Build();
        }

        private static void InvalidateFactory()
        {
            _factory?.Dispose();
            _factory = null;
        }
    }
}
