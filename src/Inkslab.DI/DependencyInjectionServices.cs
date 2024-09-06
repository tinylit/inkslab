using Inkslab.DI.Annotations;
using Inkslab.DI.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Inkslab.Annotations;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Inkslab.DI
{
    sealed class DependencyInjectionServices : IDependencyInjectionServices
    {
        private readonly IServiceCollection _services;
        private readonly DependencyInjectionOptions _options;
        private readonly IServiceProvider _service;
        private readonly List<Type> _dependencies;
        private readonly HashSet<Assembly> _assemblies = new HashSet<Assembly>();
        private readonly HashSet<Type> _assemblyTypes = new HashSet<Type>();
        private readonly HashSet<Type> _effectiveTypes = new HashSet<Type>();
        private readonly List<Type> _implementTypes = new List<Type>();

        public DependencyInjectionServices(IServiceCollection services, DependencyInjectionOptions options, IServiceProvider service)
        {
            _services = services;
            _options = options;
            _service = service;

            _dependencies = new List<Type>(options.MaxDepth * 2 + 3);
        }

        public IReadOnlyCollection<Assembly> Assemblies
        {
            get => _assemblies;
        }

        public IDependencyInjectionServices AddAssembly(Assembly assembly)
        {
            if (assembly is null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            if (!_assemblies.Add(assembly))
            {
                return this;
            }

            foreach (var type in assembly.GetTypes())
            {
                if (type.IsValueType)
                {
                    continue;
                }

                if (!type.IsAbstract)
                {
                    _assemblyTypes.Add(type);
                }

                if (_options.Ignore(type))
                {
                    continue;
                }

                if (_effectiveTypes.Add(type))
                {
                    if (type.IsInterface || type.IsAbstract)
                    {
                        continue;
                    }

                    _implementTypes.Add(type);
                }
            }

            return this;
        }

        public IDependencyInjectionServices SeekAssemblies(string pattern = "*")
        {
            if (pattern is null)
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            var assemblies = AssemblyFinder.Find(pattern);

            foreach (var assembly in assemblies)
            {
                AddAssembly(assembly);
            }

            return this;
        }

        public IDependencyInjectionServices SeekAssemblies(params string[] patterns)
        {
            if (patterns is null)
            {
                throw new ArgumentNullException(nameof(patterns));
            }

            var assemblies = AssemblyFinder.Find(patterns);

            foreach (var assembly in assemblies)
            {
                AddAssembly(assembly);
            }

            return this;
        }

        public IDependencyInjectionServices ConfigureByDefined()
        {
            DiConfigureServices(_services, _service, _assemblyTypes);

            return this;
        }

        public IDependencyInjectionServices ConfigureServices(DependencyInjectionServicesOptions servicesOptions)
        {
            if (servicesOptions is null)
            {
                throw new ArgumentNullException(nameof(servicesOptions));
            }

            DiController(_services, _options, servicesOptions, _implementTypes, _dependencies);

            return this;
        }

        public IDependencyInjectionServices ConfigureByAuto()
        {
            DiByExport(_services, _options, _effectiveTypes, _implementTypes, _dependencies);

            return this;
        }

        public IDependencyInjectionServices Add<TService>() where TService : class => Add(typeof(TService));

        public IDependencyInjectionServices Add(Type serviceType) => Add(serviceType, serviceType);

        public IDependencyInjectionServices Add<TService, TImplementation>() where TService : class where TImplementation : TService => Add(typeof(TService), typeof(TImplementation));

        public IDependencyInjectionServices Add(Type serviceType, Type implementationType) => Add(serviceType, _options.Lifetime, implementationType);

        public IDependencyInjectionServices AddTransient<TService>() where TService : class => AddTransient(typeof(TService));

        public IDependencyInjectionServices AddTransient(Type serviceType) => AddTransient(serviceType, serviceType);

        public IDependencyInjectionServices AddTransient<TService, TImplementation>() where TService : class where TImplementation : TService => AddTransient(typeof(TService), typeof(TImplementation));

        public IDependencyInjectionServices AddTransient(Type serviceType, Type implementationType) => Add(serviceType, ServiceLifetime.Transient, implementationType);

        public IDependencyInjectionServices AddScoped<TService>() where TService : class => AddScoped(typeof(TService));

        public IDependencyInjectionServices AddScoped(Type serviceType) => AddScoped(serviceType, serviceType);

        public IDependencyInjectionServices AddScoped<TService, TImplementation>() where TService : class where TImplementation : TService => AddScoped(typeof(TService), typeof(TImplementation));

        public IDependencyInjectionServices AddScoped(Type serviceType, Type implementationType) => Add(serviceType, ServiceLifetime.Scoped, implementationType);

        public IDependencyInjectionServices AddSingleton<TService>() where TService : class => AddSingleton(typeof(TService));

        public IDependencyInjectionServices AddSingleton(Type serviceType) => AddSingleton(serviceType, serviceType);

        public IDependencyInjectionServices AddSingleton<TService, TImplementation>() where TService : class where TImplementation : TService => AddSingleton(typeof(TService), typeof(TImplementation));

        public IDependencyInjectionServices AddSingleton(Type serviceType, Type implementationType) => Add(serviceType, ServiceLifetime.Singleton, implementationType);

        public IDependencyInjectionServices Add(Type serviceType, ServiceLifetime lifetime, Type implementationType)
        {
            if (serviceType is null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            if (implementationType is null)
            {
                throw new ArgumentNullException(nameof(implementationType));
            }

            if (serviceType == implementationType && (implementationType.IsInterface || implementationType.IsAbstract)
                ? Di(_services, _options, serviceType, _implementTypes, lifetime, 1/* 确保服务没有标记生命周期方式时，使用指定声明周期注入。*/, _dependencies)
                : DiServiceLifetime(_services, _options, serviceType, new List<Type>(1) { implementationType }, _implementTypes, lifetime, 1/* 确保服务没有标记生命周期方式时，使用指定声明周期注入。*/, _dependencies, false))
            {
                return this;
            }

            _dependencies.Add(serviceType);

            throw DiError("Service", serviceType, _options.MaxDepth, _dependencies);
        }

        private static void DiByExport(IServiceCollection services, DependencyInjectionOptions options, IReadOnlyCollection<Type> assemblyTypes, IReadOnlyCollection<Type> effectiveTypes, List<Type> dependencies)
        {
            var exportAttributeType = typeof(ExportAttribute);

            foreach (var type in assemblyTypes)
            {
                var attribute = (ExportAttribute)type.GetCustomAttribute(exportAttributeType, false);

                if (attribute is null)
                {
                    continue;
                }

                if (type.IsInterface || type.IsAbstract)
                {
                    if (Di(services, options, attribute.Many ? typeof(IEnumerable<>).MakeGenericType(type) : type, effectiveTypes, dependencies))
                    {
                        continue;
                    }

                    dependencies.Add(type);

                    throw DiError("Service", type, options.MaxDepth, dependencies);
                }

                if (attribute.Many && !type.IsSealed)
                {
                    if (Di(services, options, attribute.Many ? typeof(IEnumerable<>).MakeGenericType(type) : type, effectiveTypes, dependencies))
                    {
                        continue;
                    }
                }
                else
                {
                    //? 已注入。
                    if (services.Any(x => x.ServiceType == type && x.ImplementationType == type))
                    {
                        continue;
                    }

                    if (DiServiceLifetime(services, options, type, new List<Type> { type }, effectiveTypes, dependencies))
                    {
                        continue;
                    }
                }

                throw DiError("Service", type, options.MaxDepth, dependencies);
            }
        }

        private static void DiConfigureServices(IServiceCollection services, IServiceProvider service, IReadOnlyCollection<Type> assemblyTypes)
        {
            var configureServices = new List<IConfigureServices>();

            var configureServicesType = typeof(IConfigureServices);

            foreach (var type in assemblyTypes.Where(configureServicesType.IsAssignableFrom))
            {
                configureServices.Add((IConfigureServices)ActivatorUtilities.CreateInstance(service, type));
            }

            foreach (var configure in configureServices)
            {
                configure.ConfigureServices(services);
            }
        }

        private static void DiController(IServiceCollection services, DependencyInjectionOptions options, DependencyInjectionServicesOptions servicesOptions, IReadOnlyCollection<Type> effectiveTypes, List<Type> dependencies)
        {
            foreach (var controllerType in effectiveTypes.Where(servicesOptions.IsServicesType))
            {
                if (!DiConstructor(services, options, controllerType, effectiveTypes, dependencies))
                {
                    throw DiError("Controller", controllerType, options.MaxDepth, dependencies);
                }

                if (!servicesOptions.DiServicesActionIsFromServicesParameters)
                {
                    continue;
                }

                foreach (var methodInfo in controllerType.GetMethods())
                {
                    if (methodInfo.DeclaringType == typeof(object))
                    {
                        continue;
                    }

                    foreach (var parameterInfo in methodInfo.GetParameters())
                    {
                        if (!servicesOptions.ActionParameterIsFromServices(parameterInfo))
                        {
                            continue;
                        }

                        if (Di(services, options, parameterInfo.ParameterType, effectiveTypes, ServiceLifetime.Transient, 0, dependencies))
                        {
                            continue;
                        }

                        dependencies.Add(parameterInfo.ParameterType);

                        throw DiError($"Controller Method({methodInfo.Name})", controllerType, options.MaxDepth, dependencies);
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

                dependencies.Clear();
            }

            return new TypeLoadException(sb.Append('.').ToString());
        }

        private static bool Di(IServiceCollection services, DependencyInjectionOptions options, Type serviceType, IReadOnlyCollection<Type> effectiveTypes, List<Type> dependencies)
            => Di(services, options, serviceType, effectiveTypes, ServiceLifetime.Transient, 0, dependencies);

        private static bool DiConstructor(IServiceCollection services, DependencyInjectionOptions options, Type implementationType, IReadOnlyCollection<Type> effectiveTypes, List<Type> dependencies)
            => DiConstructor(services, options, implementationType, effectiveTypes, ServiceLifetime.Transient, 0, dependencies);

        private static bool DiServiceLifetime(IServiceCollection services, DependencyInjectionOptions options, Type serviceType, IReadOnlyList<Type> implementationTypes, IReadOnlyCollection<Type> effectiveTypes, List<Type> dependencies, bool isMulti = false)
            => DiServiceLifetime(services, options, serviceType, implementationTypes, effectiveTypes, ServiceLifetime.Transient, 0, dependencies, isMulti);

        private static bool DiConstructor(IServiceCollection services, DependencyInjectionOptions options, Type implementationType, IReadOnlyCollection<Type> effectiveTypes, ServiceLifetime lifetime, int depth, List<Type> dependencies)
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

                    if (Di(services, options, parameterInfo.ParameterType, effectiveTypes, lifetime, depth + 1, dependencies))
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

        private static readonly HashSet<Type> _injectionFree = new HashSet<Type>
        {
            typeof(IServiceProvider),
            typeof(IServiceScope),
            typeof(IServiceScopeFactory)
        };

        private static bool Di(IServiceCollection services, DependencyInjectionOptions options, Type serviceType, IReadOnlyCollection<Type> effectiveTypes, ServiceLifetime lifetime, int depth, List<Type> dependencies)
        {
            if (_injectionFree.Contains(serviceType))
            {
                return true;
            }

            bool isMulti = false;

            //? 集合获取。
            if (serviceType.IsGenericType && serviceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                isMulti = true;

                serviceType = serviceType.GetGenericArguments()[0];

                goto label_core;
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

label_core:

            var interfaceTypes = serviceType.IsInterface
                ? serviceType.GetInterfaces()
                : Type.EmptyTypes;

            if (serviceType.IsGenericTypeDefinition)
            {
                return DiTypeDefinition(services, options, serviceType, interfaceTypes, effectiveTypes, lifetime, depth, dependencies, isMulti);
            }

            var implementationTypes = (serviceType.IsInterface || serviceType.IsAbstract)
                ? effectiveTypes
                    .Where(serviceType.IsAssignableFrom)
                    .ToList()
                : new List<Type> { serviceType };

            implementationTypes.Sort(new TypeComparer(serviceType, interfaceTypes));

            if (implementationTypes.Count > 0)
            {
                return DiServiceLifetime(services, options, serviceType, implementationTypes, effectiveTypes, lifetime, depth, dependencies, isMulti);
            }

            if (serviceType.IsGenericType)
            {
                return DiTypeDefinition(services, options, serviceType, interfaceTypes, effectiveTypes, lifetime, depth, dependencies, isMulti);
            }

            return isMulti;
        }

        private static bool DiTypeDefinition(IServiceCollection services, DependencyInjectionOptions options, Type serviceType, Type[] interfaceTypes, IReadOnlyCollection<Type> effectiveTypes, ServiceLifetime lifetime, int depth, List<Type> dependencies, bool isMulti)
        {
            var typeDefinition = serviceType.IsGenericTypeDefinition
                ? serviceType
                : serviceType.GetGenericTypeDefinition();

            var typeDefinitionTypes = effectiveTypes
                .Where(x => x.IsLike(typeDefinition, TypeLikeKind.IsGenericTypeDefinition))
                .ToList();

            typeDefinitionTypes.Sort(new TypeComparer(serviceType, interfaceTypes));

            if (typeDefinitionTypes.Count == 0)
            {
                return isMulti;
            }

            if (!serviceType.IsGenericTypeDefinition)
            {
                return DiServiceLifetime(services, options, typeDefinition, typeDefinitionTypes, effectiveTypes, lifetime, depth, dependencies, isMulti);
            }

            if (typeDefinitionTypes.TrueForAll(x => x.IsGenericTypeDefinition))
            {
                return DiServiceLifetime(services, options, typeDefinition, typeDefinitionTypes, effectiveTypes, lifetime, depth, dependencies, isMulti);
            }

            bool flag = true;

            foreach (var serviceGroup in typeDefinitionTypes.GroupBy(x =>
                     {
                         if (x.IsGenericTypeDefinition)
                         {
                             return typeDefinition;
                         }

                         if (typeDefinition.IsInterface)
                         {
                             foreach (var interfaceType in x.GetInterfaces())
                             {
                                 if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeDefinition)
                                 {
                                     return typeDefinition.MakeGenericType(interfaceType.GetGenericArguments());
                                 }
                             }
                         }
                         else
                         {
                             var implementationType = x.BaseType;

                             while (implementationType != null)
                             {
                                 if (implementationType.IsGenericType && implementationType.GetGenericTypeDefinition() == typeDefinition)
                                 {
                                     return typeDefinition.MakeGenericType(implementationType.GetGenericArguments());
                                 }

                                 implementationType = implementationType.BaseType;
                             }
                         }

                         throw new NotSupportedException();
                     }))
            {
                if (DiServiceLifetime(services, options, serviceGroup.Key, serviceGroup.ToList(), effectiveTypes, lifetime, depth, dependencies, isMulti))
                {
                    continue;
                }

                flag = false;
            }

            return flag;
        }

        private static bool DiServiceLifetime(IServiceCollection services, DependencyInjectionOptions options, Type serviceType, IReadOnlyList<Type> implementationTypes, IReadOnlyCollection<Type> effectiveTypes, ServiceLifetime lifetime, int depth, List<Type> dependencies, bool isMulti)
        {
            if (implementationTypes.Count == 0)
            {
                return isMulti;
            }

            bool flag = false;

            foreach (var implementationType in DiImplementationAnalysis(options, serviceType, implementationTypes, isMulti))
            {
                var serviceAttribute = serviceType.GetCustomAttribute<ServiceLifetimeAttribute>(false);
                var implementationAttribute = implementationType.GetCustomAttribute<ServiceLifetimeAttribute>(false);

                if (implementationAttribute is null && serviceAttribute is null)
                {
                    if (depth == 0)
                    {
                        lifetime = options.Lifetime;
                    }
                }
                else if (implementationAttribute is null)
                {
                    if (lifetime < serviceAttribute.Lifetime)
                    {
                        throw new NotSupportedException($"生命周期为【{serviceAttribute.Lifetime}】的【{serviceType.Name}】类型，不支持对声明周期为【{lifetime}】的服务注入。");
                    }

                    lifetime = serviceAttribute.Lifetime;
                }
                else
                {
                    if (serviceAttribute is null)
                    {
                    }
                    else if (implementationAttribute.Lifetime > serviceAttribute.Lifetime)
                    {
                        throw new NotSupportedException($"生命周期为【{serviceAttribute.Lifetime}】的【{serviceType.Name}】类型，不支持声明周期为【{implementationAttribute.Lifetime}】的【{implementationType.Name}】实现。");
                    }

                    if (lifetime < implementationAttribute.Lifetime)
                    {
                        throw new NotSupportedException($"服务【{serviceType.Name}】的【{implementationType.Name}】实现类声明周期为【{implementationAttribute.Lifetime}】，不支持对声明周期为【{lifetime}】的服务注入。");
                    }

                    lifetime = implementationAttribute.Lifetime;
                }

                if (DiConstructor(services, options, implementationType, effectiveTypes, lifetime, depth, dependencies))
                {
                    flag = true;

                    if (isMulti)
                    {
                        services.TryAddEnumerable(new ServiceDescriptor(serviceType, implementationType, lifetime));
                    }
                    else
                    {
                        services.Add(new ServiceDescriptor(serviceType, implementationType, lifetime));
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

        private static IEnumerable<Type> DiImplementationAnalysis(DependencyInjectionOptions options, Type serviceType, IReadOnlyList<Type> implementationTypes, bool isMulti)
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

                while (len > i && target.IsAmongOf(implementationTypes[i + 1]))
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

        private sealed class TypeComparer : IComparer<Type>
        {
            private readonly Type _serviceType;
            private readonly Type[] _interfaceTypes;

            private static readonly HashSet<Type> _systemTypes = new HashSet<Type>
            {
                typeof(IDisposable),
                typeof(IAsyncDisposable),
                typeof(ICloneable),
                typeof(ISerializable),
                typeof(IConvertible),
                typeof(IContainer),
                typeof(IComparable),
                typeof(IStructuralEquatable),
                typeof(IEquatable<>),
                typeof(IComparable<>),
                typeof(IEqualityComparer<>),
                typeof(IEqualityComparer),
            };

            public TypeComparer(Type serviceType, Type[] interfaceTypes)
            {
                _serviceType = serviceType;
                _interfaceTypes = interfaceTypes;
            }

            private int CardinalityCode(Type implementationType)
            {
                int compare = 0;

                if (_serviceType.IsInterface)
                {
                    var interfaces = implementationType.GetInterfaces();

                    for (int i = 0; i < interfaces.Length; i++)
                    {
                        Type interfaceType = interfaces[i];

                        if (interfaceType == _serviceType || _interfaceTypes.Contains(interfaceType)) //? 本身，或者是接口本身的继承接口。
                        {
                            continue;
                        }

                        if (_systemTypes.Contains(interfaceType.IsGenericType ? interfaceType.GetGenericTypeDefinition() : interfaceType))
                        {
                            continue;
                        }

                        compare++;
                    }

                    return compare;
                }

                do
                {
                    if (implementationType == _serviceType)
                    {
                        return compare;
                    }

                    compare++;

                    implementationType = implementationType.BaseType;
                } while (!(implementationType is null || implementationType == typeof(object)));

                return int.MaxValue;
            }

            public int Compare(Type x, Type y)
            {
                if (x is null)
                {
                    return y is null ? 0 : 1;
                }

                if (y is null)
                {
                    return -1;
                }

                if (_serviceType.IsGenericTypeDefinition)
                {
                    if (x.IsGenericTypeDefinition && y.IsGenericTypeDefinition)
                    {
                    }
                    else if (x.IsGenericTypeDefinition)
                    {
                        return -1;
                    }
                    else
                    {
                        return 1;
                    }
                }
                else if (x.IsGenericTypeDefinition && y.IsGenericTypeDefinition)
                {
                }
                else if (x.IsGenericTypeDefinition)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }

                return CardinalityCode(x) - CardinalityCode(y);
            }
        }

        private void Dispose(bool disposing)
        {
            _assemblies.Clear();

            if (disposing)
            {
                _assemblyTypes.Clear();
                _effectiveTypes.Clear();
                _implementTypes.Clear();
                _dependencies.Clear();
            }
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        public IDependencyInjectionServices ConfigureByExamine(Predicate<Type> match)
        {
            if (match is null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            var exportAttributeType = typeof(ExportAttribute);

            var serviceDescriptors = _services.Where(x => match(x.ServiceType)).ToList();

            foreach (var descriptor in serviceDescriptors)
            {
                if (descriptor.ImplementationInstance != null
                    || descriptor.ImplementationFactory != null)
                {
                    continue;
                }

                Type descriptorType = descriptor.ImplementationType;

                if (descriptorType.FullName.StartsWith("Microsoft.")) //? 微软自带的注入实现，不做自动检查。
                {
                    continue;
                }

                if (DiConstructor(_services, _options, descriptorType, _implementTypes, descriptor.Lifetime, 0, _dependencies))
                {
                    continue;
                }

                _dependencies.Add(descriptorType);
                _dependencies.Add(descriptor.ServiceType);

                throw DiError("Service", descriptorType, _options.MaxDepth, _dependencies);
            }

            return this;
        }
    }
}