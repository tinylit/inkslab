using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Inkslab
{
    /// <summary>
    /// 单列服务池。
    /// </summary>
    public static class SingletonPools
    {
        /// <summary>
        /// 服务。
        /// </summary>
        private static readonly Dictionary<Type, Type> ServiceCachings = new Dictionary<Type, Type>();

        /// <summary>
        /// 服务。
        /// </summary>
        private static readonly Dictionary<Type, Type> DefaultCachings = new Dictionary<Type, Type>();

        /// <summary>
        /// 添加服务。
        /// </summary>
        /// <param name="instance">实现。</param>
        /// <typeparam name="TService">服务类型。</typeparam>
        /// <returns>是否添加成功。</returns>
        public static bool TryAdd<TService>(TService instance)
            where TService : class
        {
            if (instance is null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            return Nested<TService>.TryAdd(instance, true);
        }

        /// <summary>
        /// 添加服务。
        /// </summary>
        /// <typeparam name="TService">服务类型。</typeparam>
        /// <param name="factory">实现工厂。</param>
        /// <returns>是否添加成功。</returns>
        public static bool TryAdd<TService>(Func<TService> factory)
            where TService : class
        {
            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return Nested<TService>.TryAdd(factory, true);
        }

        /// <summary>
        /// 添加服务。
        /// </summary>
        /// <typeparam name="TService">服务类型。</typeparam>
        /// <typeparam name="TImplementation">服务实现。</typeparam>
        /// <returns>是否添加成功。</returns>
        public static bool TryAdd<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
            => Nested<TService>.TryAdd(() => Nested<TService, TImplementation>.Instance);

        /// <summary>
        /// 获取服务。
        /// </summary>
        /// <typeparam name="TService">服务类型。</typeparam>
        /// <exception cref="NotImplementedException"> <typeparamref name="TService"/> 是接口或抽象类。</exception>
        /// <exception cref="NotSupportedException">未能找全 <typeparamref name="TService"/> 任意构造函数的参数。</exception>
        /// <returns>返回唯一实例。</returns>
        public static TService Singleton<TService>()
            where TService : class
        => AutoNested<TService>.Instance;

        /// <summary>
        /// 获取服务，没有注入 <typeparamref name="TService"/> 单列实现时，使用 <typeparamref name="TImplementation"/> 的实现。
        /// </summary>
        /// <typeparam name="TService">服务类型。</typeparam>
        /// <typeparam name="TImplementation">服务实现。</typeparam>
        /// <exception cref="NotSupportedException">未能找全 <typeparamref name="TImplementation"/> 任意构造函数的参数。</exception>
        /// <returns>返回唯一实例。</returns>
        public static TService Singleton<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
            => Nested<TService>.Instance ?? Nested<TService, TImplementation>.Instance;

        private class Nested<TService> where TService : class
        {
            private static Lazy<TService> _lazy = new Lazy<TService>(() => null);

            private static volatile bool _useBaseTryAdd = true;
            private static volatile bool _uninitialized = true;

            static Nested() => ServiceCachings[typeof(TService)] = typeof(Nested<TService>);

            protected static void AddDefaultImpl(Func<TService> factory)
            {
                if (_uninitialized)
                {
                    TryAdd(factory);
                }
            }

            public static bool TryAdd(TService instance, bool defineService = false)
            {
                if (defineService || _useBaseTryAdd)
                {
                    if (defineService)
                    {
                        _useBaseTryAdd = false;
                    }

                    _uninitialized &= false;

                    if (!_lazy.IsValueCreated)
                    {
#if NETSTANDARD2_1_OR_GREATER
                        _lazy = new Lazy<TService>(instance);
#else
                        _lazy = new Lazy<TService>(() => instance);
#endif

                        return true;
                    }
                }

                return false;
            }

            public static bool TryAdd(Func<TService> factory, bool defineService = false)
            {
                if (defineService || _useBaseTryAdd)
                {
                    if (defineService)
                    {
                        _useBaseTryAdd = false;
                    }

                    _uninitialized &= false;

                    if (!_lazy.IsValueCreated)
                    {
                        _lazy = new Lazy<TService>(() => factory.Invoke() ?? throw new NullReferenceException("注入服务工厂，返回值为“null”!"), true);

                        return true;
                    }
                }

                return false;
            }

            public static TService Instance => _lazy.Value;
        }

        private class AutoNested<TService> : Nested<TService> where TService : class
        {
            static AutoNested()
            {
                var type = typeof(TService);

                if (type.IsInterface || type.IsAbstract)
                {
                    AddDefaultImpl(() => throw new NotImplementedException($"未注入{type.FullName}服务的实现，可以使用【RuntimeServPools.TryAddSingleton<{type.Name}, {type.Name}Impl>()】注入服务实现，或使用【RuntimeServPools.Singleton<{type.Name}, Default{type.Name.TrimStart('i', 'I')}Impl>】安全获取实例，若未注入实现会生成【Default{type.Name.TrimStart('i', 'I')}Impl】的实例。"));
                }
                else
                {
                    AddDefaultImpl(() => Nested<TService, TService>.Instance);
                }
            }

            public new static TService Instance => Nested<TService>.Instance; //? 触发静态构造函数。
        }

        private static class Nested<TService, TImplementation>
        where TService : class
        where TImplementation : class, TService
        {

            private static class Lazy
            {
                private const BindingFlags DefaultLookup = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public;

                static Lazy()
                {
                    //~ 包含值类型且为非可选参数时，出现异常。

                    var serviceType = typeof(TService);
                    var conversionType = typeof(TImplementation);

                    var constructorInfos = conversionType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .OrderBy(x => x.IsPublic ? 0 : 1)
                        .ThenByDescending(x =>
                        {
                            //? 参数。
                            var parameterInfos = x.GetParameters();

                            //! 客户最优注册的类型。
                            var specifiedCount = parameterInfos.Count(y => ServiceCachings.ContainsKey(y.ParameterType));

                            return parameterInfos.Length * parameterInfos.Length + specifiedCount;
                        })
                        .ToList();

                    var constructorInfo = Resolved(typeof(Nested<TImplementation>), constructorInfos);

                    if (constructorInfo is null)
                    {
                        foreach (var item in constructorInfos.Skip(constructorInfos.Count - 1))
                        {
                            var parameterInfos = item.GetParameters();

                            foreach (var parameterInfo in parameterInfos)
                            {
                                if (parameterInfo.IsOptional || IsSurport(conversionType, parameterInfo.ParameterType))
                                {
                                    continue;
                                }

                                throw new NotSupportedException($"单例服务（{conversionType.FullName}=>{serviceType.FullName}）的构造函数参数（{parameterInfo.ParameterType.FullName}）未注入单例支持，可以使用【RuntimeServPools.TryAddSingleton<{parameterInfo.ParameterType.Name}, {parameterInfo.ParameterType.Name}Impl>()】注入服务实现。");
                            }
                        }
                    }

                    Instance = CreateInstance(constructorInfo);
                }

                private static ConstructorInfo Resolved(Type conversionType, List<ConstructorInfo> constructorInfos)
                {
                    foreach (var constructorInfo in constructorInfos)
                    {
                        bool flag = true;

                        var parameterInfos = constructorInfo.GetParameters();

                        foreach (var parameterInfo in parameterInfos)
                        {
                            if (parameterInfo.IsOptional || IsSurport(conversionType, parameterInfo.ParameterType))
                            {
                                continue;
                            }

                            flag = false;

                            break;
                        }

                        if (flag)
                        {
                            return constructorInfo;
                        }
                    }

                    return null;
                }

                private static bool IsSurport(Type conversionType, Type parameterType)
                {
                    if (ServiceCachings.ContainsKey(parameterType))
                    {
                        return true;
                    }

                    if (!parameterType.IsAbstract)
                    {
                        foreach (var kv in ServiceCachings)
                        {
                            if (kv.Value == parameterType)
                            {
                                ServiceCachings[parameterType] = kv.Value;

                                return true;
                            }
                        }
                    }

                    foreach (var kv in ServiceCachings)
                    {
                        if (parameterType.IsInterface)
                        {
                            if (kv.Value == conversionType)
                            {
                                continue;
                            }

                            if (parameterType.IsAssignableFrom(kv.Key))
                            {
                                ServiceCachings[parameterType] = kv.Value;

                                return true;
                            }
                        }
                        else if (parameterType.IsAssignableFrom(conversionType))
                        {
                            continue;
                        }
                        else if (parameterType.IsAssignableFrom(kv.Key))
                        {
                            ServiceCachings[parameterType] = kv.Value;

                            return true;
                        }
                    }

                    return false;
                }

                private static TImplementation CreateInstance(ConstructorInfo constructorInfo)
                {
                    var parameterInfos = constructorInfo.GetParameters();

                    object[] arguments = new object[parameterInfos.Length];

                    for (int i = 0; i < parameterInfos.Length; i++)
                    {
                        var parameterInfo = parameterInfos[i];

                        if (ServiceCachings.TryGetValue(parameterInfo.ParameterType, out Type implementType))
                        {
                            PropertyInfo instancePropertyInfo = implementType.GetProperty("Instance", DefaultLookup);

                            var instance = instancePropertyInfo.GetValue(null, null);

                            if (instance is null)
                            {
                                if (DefaultCachings.TryGetValue(parameterInfo.ParameterType, out Type defaultType))
                                {
                                    //? 同时存在时以“Nested<TService, TImplementation>”类型存储。
                                    instancePropertyInfo = defaultType.GetProperty("Instance", DefaultLookup);

                                    instance = instancePropertyInfo.GetValue(null, null);
                                }
                            }

                            arguments[i] = instance;

                            continue;
                        }
                        else
                        {
                            arguments[i] = parameterInfo.DefaultValue;
                        }
                    }

                    return (TImplementation)constructorInfo.Invoke(arguments);
                }

                public static TImplementation Instance { get; }
            }

            static Nested()
            {
                //~ 包含值类型且为非可选参数时，出现异常。

                var serviceType = typeof(TService);
                var conversionType = typeof(TImplementation);

                if (conversionType.IsInterface)
                {
                    throw new NotSupportedException($"单例服务({conversionType.FullName}=>{serviceType.FullName})的实现（{conversionType.FullName}）是接口，不能被实例化!");
                }

                if (conversionType.IsAbstract)
                {
                    throw new NotSupportedException($"单例服务({conversionType.FullName}=>{serviceType.FullName})的实现（{conversionType.FullName}）是抽象类，不能被实例化!");
                }

                DefaultCachings[typeof(TService)] = typeof(Nested<TService, TImplementation>);
            }

            public static TImplementation Instance => Lazy.Instance;
        }
    }
}
