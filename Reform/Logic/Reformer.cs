using System;
using System.Collections.Generic;
using System.Data;
using Unity;
using Unity.Injection;
using Unity.Lifetime;
using Reform.Interfaces;
using Reform.Objects;

namespace Reform.Logic
{
    public static class Reformer
    {
        private static readonly UnityContainer _unityContainer;
        private static readonly Dictionary<Type, Type> _typeRegistrations;

        #region Constructor

        static Reformer()
        {
            _unityContainer = new UnityContainer();
            _typeRegistrations = new Dictionary<Type, Type>
            {
                { typeof (IDebugLogger), typeof (DebugLogger) },
                { typeof (IMetadataProvider<>), typeof (MetadataProvider<>) },
                { typeof (IReform<>), typeof (Reform<>) },
                { typeof (IDataAccess<>), typeof (DataAccess<>) },
                { typeof (IConnectionProvider<>), typeof (ConnectionProvider<>) },
                { typeof (ICommandBuilder<>), typeof (CommandBuilder<>) },
                { typeof (ISqlBuilder<>), typeof (MySqlBuilder<>) },
                { typeof (IValidator<>), typeof (Validator<>) },
                { typeof (IScopeProvider), typeof (ScopeProvider) },
                { typeof (IParameterBuilder), typeof (ParameterBuilder) },
                { typeof (IColumnNameFormatter), typeof (MySqlColumnNameFormatter) },
                { typeof (IMapper), typeof (Mapper) }
            };

            foreach (var registration in _typeRegistrations)
            {
                _unityContainer.RegisterType(registration.Key, registration.Value, new SingletonLifetimeManager());
            }
        }

        #endregion

        public static void RegisterType(Type type, Type implementation)
        {
            _unityContainer.RegisterType(type, implementation, new SingletonLifetimeManager());
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
