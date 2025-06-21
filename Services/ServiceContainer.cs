namespace FlatBufferEx.Services
{
    /// <summary>
    /// Simple service container for dependency injection
    /// </summary>
    public class ServiceContainer
    {
        private readonly Dictionary<Type, object> _singletons = new();
        private readonly Dictionary<Type, Func<object>> _factories = new();

        /// <summary>
        /// Registers a singleton service
        /// </summary>
        /// <typeparam name="TInterface">Interface type</typeparam>
        /// <typeparam name="TImplementation">Implementation type</typeparam>
        public void RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface
        {
            _factories[typeof(TInterface)] = () =>
            {
                if (_singletons.TryGetValue(typeof(TInterface), out var existing))
                {
                    return existing;
                }

                var instance = CreateInstance<TImplementation>();
                _singletons[typeof(TInterface)] = instance;
                return instance;
            };
        }

        /// <summary>
        /// Gets a service instance
        /// </summary>
        /// <typeparam name="T">Service type</typeparam>
        /// <returns>Service instance</returns>
        public T GetService<T>()
        {
            if (_factories.TryGetValue(typeof(T), out var factory))
            {
                return (T)factory();
            }

            throw new InvalidOperationException($"Service {typeof(T).Name} is not registered");
        }

        /// <summary>
        /// Creates an instance with dependency injection
        /// </summary>
        /// <typeparam name="T">Type to create</typeparam>
        /// <returns>Created instance</returns>
        private T CreateInstance<T>() where T : class
        {
            var constructors = typeof(T).GetConstructors();
            var constructor = constructors.OrderByDescending(c => c.GetParameters().Length).First();
            
            var parameters = constructor.GetParameters()
                .Select(p => GetService(p.ParameterType))
                .ToArray();

            return (T)Activator.CreateInstance(typeof(T), parameters);
        }

        /// <summary>
        /// Gets a service by type
        /// </summary>
        /// <param name="serviceType">Service type</param>
        /// <returns>Service instance</returns>
        private object GetService(Type serviceType)
        {
            if (_factories.TryGetValue(serviceType, out var factory))
            {
                return factory();
            }

            throw new InvalidOperationException($"Service {serviceType.Name} is not registered");
        }
    }
} 