using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Jamiras.Components
{
    /// <summary>
    /// Manages services for an application.
    /// </summary>
    public class ServiceRepository
    {
        /// <summary>
        /// Constructs a new ServiceRepository
        /// </summary>
        public ServiceRepository()
        {
            _services = new Dictionary<Type, object>();
            _registeredTypes = new Dictionary<Type, Type>();
        }

        private readonly Dictionary<Type, object> _services;
        private readonly Dictionary<Type, Type> _registeredTypes;

        /// <summary>
        /// Creates a new global <see cref="Instance"/> of the ServiceRepository.
        /// </summary>
        public static void Reset()
        {
            Instance = new ServiceRepository();
        }

        /// <summary>
        /// Gets the global ServiceRepository instance
        /// </summary>
        public static ServiceRepository Instance { get; private set; }

        /// <summary>
        /// Registers a service instance type.
        /// </summary>
        /// <param name="serviceType">Type of service to register.</param>
        public void RegisterService(Type serviceType)
        {
            lock (_registeredTypes)
            {
                foreach (ExportAttribute export in serviceType.GetCustomAttributes(typeof(ExportAttribute), false))
                    _registeredTypes[export.ExportedInterface] = serviceType;
            }
        }

        /// <summary>
        /// Registers a service instance.
        /// </summary>
        /// <param name="service">Service instance to register.</param>
        /// <typeparam name="TService">Type to register service against.</typeparam>
        public void RegisterInstance<TService>(TService service)
            where TService : class
        {
            lock (_services)
            {
                _services[typeof(TService)] = service;
            }
        }

        /// <summary>
        /// Finds the registered implementation of a service
        /// </summary>
        /// <typeparam name="T">requested service</typeparam>
        /// <returns>service implementation</returns>
        public T FindService<T>()
            where T : class
        {
            lock (_services)
            {
                return (T)FindService(typeof(T), new List<Type>());
            }
        }

        private object FindService(Type serviceType, List<Type> beingInstantiated)
        {
            object service;
            if (_services.TryGetValue(serviceType, out service))
                return service;

            Type instanceType;
            if (!_registeredTypes.TryGetValue(serviceType, out instanceType))
                throw new InvalidOperationException("No service registered for " + serviceType.Name);

            if (beingInstantiated.Contains(serviceType))
            {
                StringBuilder builder = new StringBuilder();
                builder.Append("Circular reference detected - ");
                foreach (Type type in beingInstantiated)
                    builder.AppendFormat("{0} > ", type.Name);
                builder.Append(serviceType.Name);
                throw new InvalidOperationException(builder.ToString());
            }

            beingInstantiated.Add(serviceType);

            ConstructorInfo constructor = null;
            object[] imports = null;
            foreach (ConstructorInfo info in instanceType.GetConstructors())
            {
                var parameters = info.GetParameters();
                if (parameters.Length == 0)
                {
                    constructor = info;
                }
                else if (info.GetCustomAttributes(typeof(ImportingConstructorAttribute), false).Length > 0)
                {
                    constructor = info;

                    imports = new object[parameters.Length];
                    for (int i = 0; i < parameters.Length ; i++)
                        imports[i] = FindService(parameters[i].ParameterType, beingInstantiated);

                    break;
                }
            }

            if (constructor == null)
                throw new InvalidOperationException("Could not find default constructor or [ImportingConstructor] on " + instanceType.Name);

            beingInstantiated.Remove(serviceType);

            service = constructor.Invoke(imports);
            _services[serviceType] = service;
            return service;
        }
    }
}
