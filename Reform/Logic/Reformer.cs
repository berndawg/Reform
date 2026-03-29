using System;
using System.Collections.Generic;
using Reform.Dialects;
using Reform.Interfaces;
using Unity;
using Unity.Lifetime;

namespace Reform.Logic
{
    public static class Reformer
    {
        private static readonly UnityContainer _unityContainer;

        #region Constructor

        static Reformer()
        {
            _unityContainer = new UnityContainer();

            var dictionary = new Dictionary<Type, Type>
            {
                { typeof (IDebugLogger), typeof (DebugLogger) },
                { typeof (IMetadataProvider<>), typeof (MetadataProvider<>) },
                { typeof (IReform<>), typeof (Reform<>) },
                { typeof (IDataAccess<>), typeof (DataAccess<>) },
                { typeof (IConnectionProvider<>), typeof (ConnectionProvider<>) },
                { typeof (ICommandBuilder<>), typeof (CommandBuilder<>) },
                { typeof (ISqlBuilder<>), typeof (SqlBuilder<>) },
                { typeof (IValidator<>), typeof (Validator<>) },
                { typeof (IScopeProvider), typeof (ScopeProvider) },
                { typeof (IDialect), typeof (SqliteDialect) }
            };

            foreach (Type key in dictionary.Keys)
                RegisterType(key, dictionary[key]);
        }

        #endregion

        public static void RegisterType(Type type, Type implementation)
        {
            _unityContainer.RegisterType(type, implementation, new SingletonLifetimeManager());
        }

        public static void UseSqlite()
        {
            RegisterType(typeof(IDialect), typeof(SqliteDialect));
        }

        public static void UseSqlServer()
        {
            RegisterType(typeof(IDialect), typeof(SqlServerDialect));
        }

        public static void UseMySql()
        {
            RegisterType(typeof(IDialect), typeof(MySqlDialect));
        }

        public static IReform<T> Reform<T>() where T : class
        {
            return _unityContainer.Resolve<IReform<T>>();
        }

        public static T Resolve<T>()
        {
            return _unityContainer.Resolve<T>();
        }
    }
}
