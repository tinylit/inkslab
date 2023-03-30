using Inkslab.DI.Annotations;
using Inkslab.DI.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using XServiceCollection = Inkslab.DI.Collections.IServiceCollection;

namespace Inkslab.DI
{
    class DependencyInjectionServices : IDependencyInjectionServices
    {
        private readonly IServiceCollection services;
        private readonly HashSet<Assembly> assemblies = new HashSet<Assembly>();

        public DependencyInjectionServices(IServiceCollection services)
        {
            this.services = services;
        }

        public ICollection<Assembly> Assemblies { get => assemblies; }
        public IDependencyInjectionServices SeekAssemblies(string pattern = "*")
        {
            if (pattern is null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            var assemblies = AssemblyFinder.Find(pattern);

            foreach (var assembly in assemblies)
            {
                this.assemblies.Add(assembly);
            }

            return this;
        }

        public IDependencyInjectionServices RemoveAll()
        {
            assemblies.Clear();

            return this;
        }

        public IDependencyInjectionServices RemoveAssemblies(Predicate<Assembly> match)
        {
            assemblies.RemoveWhere(match);

            return this;
        }
        
        public IDependencyInjectionServices AddAssembly(Assembly assembly)
        {
            if (assembly is null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            assemblies.Add(assembly);

            return this;
        }

        public IDependencyInjectionServices AddAssemblies(Assembly[] assemblies)
        {
            if (assemblies is null)
            {
                throw new ArgumentNullException(nameof(assemblies));
            }

            foreach (var assembly in assemblies)
            {
                if (assembly is null)
                {
                    continue;
                }

                this.assemblies.Add(assembly);
            }

            return this;
        }

        public IDependencyInjectionServices ConfigureByDefined()
        {
            var assembliyTypes = Assemblies
                .SelectMany(x => x.GetTypes())
                .Where(x => !x.IsValueType && !x.IsAbstract)
                .ToList();

            DiConfigureServices(new Collections.ServiceCollection(services), assembliyTypes);

            return this;
        }


        public IServiceCollection ConfigureByAuto(DependencyInjectionOptions options)
        {

            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var assembliyTypes = Assemblies
                .SelectMany(x => x.GetTypes())
                .Where(x => !x.IsValueType && !x.IsAbstract)
                .ToList();

            DiController(services, options, assembliyTypes);

            return services;
        }

        private static void DiConfigureServices(XServiceCollection services, List<Type> assembliyTypes)
        {
            var configureServices = new List<IConfigureServices>();

            var configureServicesType = typeof(IConfigureServices);

            foreach (var type in assembliyTypes.Where(x => x.IsClass && !x.IsAbstract && configureServicesType.IsAssignableFrom(x)))
            {
                configureServices.Add((IConfigureServices)Activator.CreateInstance(type));
            }

            foreach (var configure in configureServices)
            {
                configure.ConfigureServices(services);
            }
        }

        private static void DiController(IServiceCollection services, DependencyInjectionOptions options, List<Type> assembliyTypes)
        {
            List<Type> dependencies = new List<Type>(options.MaxDepth * 2 + 3);

            var effectiveTypes = assembliyTypes.FindAll(x => !options.Ignore(x));

            foreach (var controllerType in effectiveTypes.Where(options.IsControllerType))
            {
                if (!DiConstructor(services, options, controllerType, effectiveTypes, 0, dependencies))
                {
                    throw DiError("Controller", controllerType, options.MaxDepth, dependencies);
                }

                if (options.DiControllerActionIsFromServicesParameters)
                {
                    foreach (var methodInfo in controllerType.GetMethods())
                    {
                        if (methodInfo.DeclaringType == typeof(object))
                        {
                            continue;
                        }

                        foreach (var parameterInfo in methodInfo.GetParameters())
                        {
                            if (!options.ActionParameterIsFromServices(parameterInfo))
                            {
                                continue;
                            }

                            if (Di(services, options, parameterInfo.ParameterType, effectiveTypes, 0, dependencies))
                            {
                                continue;
                            }

                            dependencies.Add(parameterInfo.ParameterType);

                            throw DiError($"Controller Method({methodInfo.Name})", controllerType, options.MaxDepth, dependencies);
                        }
                    }
                }
            }
        }

        private static Exception DiError(string name, Type type, int maxDepth, List<Type> dependencies)
        {
            var sb = new StringBuilder();

            sb.Append(name)
                .Append(" '")
                .Append(type.Name)
                .Append("' cannot be created and the current maximum dependency injection depth is ")
                .Append(maxDepth);

            if (dependencies.Count > 0)
            {
                sb.AppendLine(".")
                    .AppendLine("Dependency details are as follows:");

                dependencies.Reverse();

                for (int i = 0, len = dependencies.Count - 1; i <= len; i++)
                {
                    Type serviceType = dependencies[i];

                    if (i > 0)
                    {
                        sb.Append(" => ")
                            .Append(Environment.NewLine);

                        for (int j = 0; j < i; j += 2)
                        {
                            sb.Append('\t');
                        }
                    }

                    if (i < len)
                    {
                        ++i;
                    }

                    Type implementationType = dependencies[i];

                    if (serviceType == implementationType)
                    {
                        sb.Append(serviceType.Name);
                    }
                    else
                    {
                        sb.Append('{')
                            .Append(serviceType.Name)
                            .Append('=')
                            .Append(implementationType.Name)
                            .Append('}');
                    }
                }
            }

            return new TypeLoadException(sb.Append('.').ToString());
        }

        private static bool DiConstructor(IServiceCollection services, DependencyInjectionOptions options, Type implementationType, List<Type> assemblyTypes, int depth, List<Type> dependencies)
        {
            bool flag = false;

            int startIndex = dependencies.Count;

            foreach (var constructorInfo in implementationType.GetConstructors())
            {
                if (!constructorInfo.IsPublic)
                {
                    continue;
                }

                flag = true;

                if (dependencies.Count > startIndex)
                {
                    dependencies.RemoveRange(startIndex + 1, dependencies.Count - startIndex - 1);
                }

                foreach (var parameterInfo in constructorInfo.GetParameters())
                {
                    if (parameterInfo.IsOptional)
                    {
                        continue;
                    }

                    if (implementationType.IsAssignableFrom(parameterInfo.ParameterType)) //? 避免循环依赖。
                    {
                        flag = false;

                        break;
                    }

                    if (Di(services, options, parameterInfo.ParameterType, assemblyTypes, depth + 1, dependencies))
                    {
                        continue;
                    }

                    dependencies.Add(parameterInfo.ParameterType);

                    flag = false;

                    break;
                }

                if (flag)
                {
                    break;
                }
            }

            return flag;
        }

        private static bool Di(IServiceCollection services, DependencyInjectionOptions options, Type serviceType, List<Type> assemblyTypes, int depth, List<Type> dependencies)
        {
            if (serviceType == typeof(IServiceProvider) || serviceType == typeof(IServiceScopeFactory))
            {
                return true;
            }

            bool isMulti = false;

            //? 集合获取。
            if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                isMulti = true;

                serviceType = serviceType.GetGenericArguments()[0];
            }

            if (services.Any(x => x.ServiceType == serviceType)) //? 已有注入时，不再自动注入。
            {
                return true;
            }

            if (serviceType.IsGenericType)
            {
                var typeDefinition = serviceType.GetGenericTypeDefinition();

                if (services.Any(x => x.ServiceType == typeDefinition)) //? 已有注入时，不再自动注入。
                {
                    return true;
                }
            }

            if (serviceType.IsIgnore())
            {
                return false;
            }

            var interfaceTypes = serviceType.IsInterface
                ? serviceType.GetInterfaces()
                : Type.EmptyTypes;

            var implementationTypes = (serviceType.IsInterface || serviceType.IsAbstract)
                ? assemblyTypes
                    .Where(serviceType.IsAssignableFrom)
                    .OrderBy(y => OrderByDepth(serviceType, y, interfaceTypes))
                    .ToList()
                : new List<Type> { serviceType };

            if (implementationTypes.Count > 0)
            {
                return DiServiceLifetime(services, options, serviceType, implementationTypes, assemblyTypes, depth, dependencies, isMulti);
            }

            return serviceType.IsGenericType && DiGeneric(services, options, serviceType, interfaceTypes, assemblyTypes, depth, dependencies, isMulti);
        }

        private static bool DiGeneric(IServiceCollection services, DependencyInjectionOptions options, Type serviceType, Type[] interfaceTypes, List<Type> assemblyTypes, int depth, List<Type> dependencies, bool isMulti)
        {
            var typeDefinition = serviceType.GetGenericTypeDefinition();

            var typeDefinitionTypes = assemblyTypes
                              .Where(typeDefinition.IsLike)
                              .OrderBy(y => OrderByDepth(typeDefinition, y, interfaceTypes))
                              .ToList();

            return DiServiceLifetime(services, options, typeDefinition, typeDefinitionTypes, assemblyTypes, depth, dependencies, isMulti);
        }

        private static int OrderByDepth(Type serviceType, Type implementationType, Type[] interfaceTypes)
        {
            int compare = 0;

            if (serviceType.IsInterface)
            {
                Type cloneType = typeof(ICloneable);
                Type disposableType = typeof(IDisposable);
                Type asyncDisposable = typeof(IAsyncDisposable);

                foreach (var interfaceType in implementationType.GetInterfaces())
                {
                    if (interfaceType == serviceType || interfaceTypes.Contains(interfaceType)) //? 本身，或者是接口本身的继承接口。
                    {
                        continue;
                    }

                    if (interfaceType == disposableType || interfaceType == asyncDisposable)
                    {
                        continue;
                    }

                    if (interfaceType == cloneType)
                    {
                        continue;
                    }

                    compare++;
                }

                return compare;
            }

            do
            {
                if (implementationType == serviceType)
                {
                    return compare;
                }

                compare++;

                implementationType = implementationType.BaseType;

            } while (!(implementationType is null || implementationType == typeof(object)));

            return int.MaxValue;
        }

        private static bool DiServiceLifetime(IServiceCollection services, DependencyInjectionOptions options, Type serviceType, List<Type> implementationTypes, List<Type> assemblyTypes, int depth, List<Type> dependencies, bool isMulti)
        {
            bool flag = false;

            foreach (var implementationType in DiImplementationAnalysis(options, serviceType, implementationTypes, isMulti))
            {
                if (DiConstructor(services, options, implementationType, assemblyTypes, depth, dependencies))
                {
                    flag = true;

                    var attrbute = (ServiceLifetimeAttribute)(implementationType.GetCustomAttribute(typeof(ServiceLifetimeAttribute), false) ?? serviceType.GetCustomAttribute(typeof(ServiceLifetimeAttribute), false));

                    switch (attrbute?.Lifetime ?? options.Lifetime)
                    {
                        case ServiceLifetime.Singleton:
                            services.AddSingleton(serviceType, implementationType);
                            break;
                        case ServiceLifetime.Transient:
                            services.AddTransient(serviceType, implementationType);
                            break;
                        case ServiceLifetime.Scoped:
                        default:
                            services.AddScoped(serviceType, implementationType);
                            break;
                    }

                    if (isMulti) //? 注入一个支持。
                    {
                        continue;
                    }

                    break;
                }

                dependencies.Add(implementationType);

                break;
            }

            return flag;
        }

        private static IEnumerable<Type> DiImplementationAnalysis(DependencyInjectionOptions options, Type serviceType, List<Type> implementationTypes, bool isMulti)
        {
            if (implementationTypes.Count == 1)
            {
                yield return implementationTypes[0];
            }

            if (implementationTypes.Count <= 1)
            {
                yield break;
            }

            List<Type> effectiveTypes = new List<Type>();

            for (int i = 0, len = implementationTypes.Count - 1; i <= len; i++)
            {
                Type target = implementationTypes[i];

                while (len > i && target.IsLike(implementationTypes[i + 1]))
                {
                    i++;
                }

                if (isMulti)
                {
                    yield return target;
                }
                else
                {
                    effectiveTypes.Add(target);
                }
            }

            if (isMulti)
            {
                yield break;
            }

            if (effectiveTypes.Count == 1)
            {
                yield return effectiveTypes[0];
            }

            if (effectiveTypes.Count <= 1)
            {
                yield break;
            }

            yield return options.ResolveConflictingTypes(serviceType, effectiveTypes);
        }
    }
}
