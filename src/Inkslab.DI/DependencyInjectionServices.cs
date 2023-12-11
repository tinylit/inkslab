using Inkslab.DI.Annotations;
using Inkslab.DI.Options;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Inkslab.DI
{
    class DependencyInjectionServices : IDependencyInjectionServices
    {
        private readonly IServiceCollection services;
        private readonly IServiceProvider service;
        private readonly HashSet<Assembly> assemblies = new HashSet<Assembly>();

        public DependencyInjectionServices(IServiceCollection services, IServiceProvider service)
        {
            this.services = services;
            this.service = service;
        }

        public IReadOnlyCollection<Assembly> Assemblies
        {
            get => assemblies;
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
                this.assemblies.Add(assembly);
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
                this.assemblies.Add(assembly);
            }

            return this;
        }

        public IDependencyInjectionServices ConfigureByDefined()
        {
            var assemblyTypes = Assemblies
                .SelectMany(x => x.GetTypes())
                .Where(x => !x.IsValueType && !x.IsAbstract)
                .ToList();

            DiConfigureServices(assemblyTypes);

            return this;
        }


        public IServiceCollection ConfigureByAuto(DependencyInjectionOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var assemblyTypes = Assemblies
                .SelectMany(x => x.GetTypes())
                .SkipWhile(options.Ignore)
                .ToList();

            var effectiveTypes = assemblyTypes.FindAll(x => !x.IsValueType && !x.IsAbstract);

            DiByExport(services, options, assemblyTypes, effectiveTypes);

            DiController(services, options, effectiveTypes);

            return services;
        }

        private static void DiByExport(IServiceCollection services, DependencyInjectionOptions options, List<Type> assemblyTypes, List<Type> effectiveTypes)
        {
            var exportAttributeType = typeof(ServiceLifetimeAttribute);

            List<Type> dependencies = new List<Type>(options.MaxDepth * 2 + 3);

            foreach (var type in assemblyTypes)
            {
                var attribute = (ServiceLifetimeAttribute)type.GetCustomAttribute(exportAttributeType, true);

                if (attribute is null)
                {
                    continue;
                }

                if (type.IsInterface || type.IsAbstract)
                {
                    if (Di(services, options, type, effectiveTypes, 0, dependencies))
                    {
                        continue;
                    }

                    dependencies.Add(type);

                    throw DiError("Service", type, options.MaxDepth, dependencies);
                }

                if (DiConstructor(services, options, type, effectiveTypes, 0, dependencies))
                {
                    services.Add(new ServiceDescriptor(type, type, attribute.Lifetime));
                }
                else
                {
                    throw DiError("Service", type, options.MaxDepth, dependencies);
                }
            }
        }

        private void DiConfigureServices(List<Type> assemblyTypes)
        {
            var configureServices = new List<IConfigureServices>();

            var configureServicesType = typeof(IConfigureServices);

            foreach (var type in assemblyTypes.Where(x => x.IsClass && !x.IsAbstract && configureServicesType.IsAssignableFrom(x)))
            {
                configureServices.Add((IConfigureServices)ActivatorUtilities.CreateInstance(service, type));
            }

            foreach (var configure in configureServices)
            {
                configure.ConfigureServices(services);
            }
        }

        private static void DiController(IServiceCollection services, DependencyInjectionOptions options, List<Type> assemblyTypes)
        {
            List<Type> dependencies = new List<Type>(options.MaxDepth * 2 + 3);

            var effectiveTypes = assemblyTypes.FindAll(x => !options.Ignore(x));

            foreach (var controllerType in effectiveTypes.Where(options.IsControllerType))
            {
                if (!DiConstructor(services, options, controllerType, effectiveTypes, 0, dependencies))
                {
                    throw DiError("Controller", controllerType, options.MaxDepth, dependencies);
                }

                if (!options.DiControllerActionIsFromServicesParameters)
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

        private static bool DiConstructor(IServiceCollection services, DependencyInjectionOptions options, Type implementationType, List<Type> effectiveTypes, int depth, List<Type> dependencies)
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

                    if (Di(services, options, parameterInfo.ParameterType, effectiveTypes, depth + 1, dependencies))
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

        private static bool Di(IServiceCollection services, DependencyInjectionOptions options, Type serviceType, List<Type> effectiveTypes, int depth, List<Type> dependencies)
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

            var interfaceTypes = serviceType.IsInterface
                ? serviceType.GetInterfaces()
                : Type.EmptyTypes;

            if (serviceType.IsGenericTypeDefinition)
            {
                return DiTypeDefinition(services, options, serviceType, interfaceTypes, effectiveTypes, depth, dependencies, isMulti);
            }

            var implementationTypes = (serviceType.IsInterface || serviceType.IsAbstract)
                ? effectiveTypes
                    .Where(serviceType.IsAssignableFrom)
                    .ToList()
                : new List<Type> { serviceType };

            implementationTypes.Sort(new TypeComparer(serviceType, interfaceTypes));

            if (implementationTypes.Count > 0)
            {
                return DiServiceLifetime(services, options, serviceType, implementationTypes, effectiveTypes, depth, dependencies, isMulti);
            }

            return serviceType.IsGenericType && DiTypeDefinition(services, options, serviceType, interfaceTypes, effectiveTypes, depth, dependencies, isMulti);
        }

        private static bool DiTypeDefinition(IServiceCollection services, DependencyInjectionOptions options, Type serviceType, Type[] interfaceTypes, List<Type> effectiveTypes, int depth, List<Type> dependencies, bool isMulti)
        {
            var typeDefinition = serviceType.IsGenericTypeDefinition
                ? serviceType
                : serviceType.GetGenericTypeDefinition();

            var typeDefinitionTypes = effectiveTypes
                .Where(typeDefinition.IsLike)
                .ToList();

            typeDefinitionTypes.Sort(new TypeComparer(serviceType, interfaceTypes));

            if (typeDefinitionTypes.Count == 0)
            {
                return false;
            }

            if (!serviceType.IsGenericTypeDefinition)
            {
                return DiServiceLifetime(services, options, typeDefinition, typeDefinitionTypes, effectiveTypes, depth, dependencies, isMulti);
            }

            if (typeDefinitionTypes.TrueForAll(x => x.IsGenericTypeDefinition))
            {
                return DiServiceLifetime(services, options, typeDefinition, typeDefinitionTypes, effectiveTypes, depth, dependencies, isMulti);
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
                if (DiServiceLifetime(services, options, serviceGroup.Key, serviceGroup.ToList(), effectiveTypes, depth, dependencies, isMulti))
                {
                    continue;
                }

                flag = false;
            }

            return flag;
        }

        private sealed class TypeComparer : IComparer<Type>
        {
            private readonly Type serviceType;
            private readonly Type[] interfaceTypes;

            public TypeComparer(Type serviceType, Type[] interfaceTypes)
            {
                this.serviceType = serviceType;
                this.interfaceTypes = interfaceTypes;
            }

            private int CardinalityCode(Type implementationType)
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

                if (serviceType.IsGenericTypeDefinition)
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

        private static bool DiServiceLifetime(IServiceCollection services, DependencyInjectionOptions options, Type serviceType, List<Type> implementationTypes, List<Type> effectiveTypes, int depth, List<Type> dependencies, bool isMulti)
        {
            if (implementationTypes.Count == 0)
            {
                return false;
            }

            bool flag = false;

            foreach (var implementationType in DiImplementationAnalysis(options, serviceType, implementationTypes, isMulti))
            {
                if (DiConstructor(services, options, implementationType, effectiveTypes, depth, dependencies))
                {
                    flag = true;

                    var serviceAttribute = serviceType.GetCustomAttribute<ServiceLifetimeAttribute>(false);
                    var implementationAttribute = implementationType.GetCustomAttribute<ServiceLifetimeAttribute>(false);

                    ServiceLifetime lifetime = options.Lifetime;

                    if (implementationAttribute is null)
                    {
                        if (serviceAttribute is null)
                        {
                        }
                        else
                        {
                            lifetime = serviceAttribute.Lifetime;
                        }
                    }
                    else if (serviceAttribute is null)
                    {
                        lifetime = implementationAttribute.Lifetime;
                    }
                    else
                    {
                        if (implementationAttribute.Lifetime > serviceAttribute.Lifetime)
                        {
                            throw new NotSupportedException($"生命周期为【{serviceAttribute.Lifetime}】的【{serviceType.Name}】类型，不支持声明周期为【{implementationAttribute.Lifetime}】的【{implementationType.Name}】实现。");
                        }

                        lifetime = implementationAttribute.Lifetime;
                    }

                    services.Add(new ServiceDescriptor(serviceType, implementationType, lifetime));

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