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
        private static readonly Dictionary<Type, Type> serviceCachings = new Dictionary<Type, Type>();

        /// <summary>
        /// 添加服务。
        /// </summary>
        /// <returns>是否添加成功。</returns>
        public static bool TryAdd<TService>()
            where TService : class
            => Nested<TService>.TryAdd(() => Nested<TService, TService>.Instance);

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

            return Nested<TService>.TryAdd(instance);
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

            return Nested<TService>.TryAdd(factory);
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
            => AutoNested<TService>.AutoInstance;

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
            private static Lazy<TService> lazy = new Lazy<TService>(() => null);

            private static volatile bool useBaseTryAdd = true;
            private static volatile bool uninitialized = true;

            protected static void AddDefaultImpl(Func<TService> factory)
            {
                if (uninitialized)
                {
                    TryAddCore(factory);
                }
            }

            private static bool TryAddCore(Func<TService> factory, bool defineService = false)
            {
                if (defineService || useBaseTryAdd)
                {
                    if (defineService)
                    {
                        useBaseTryAdd = false;

                        serviceCachings[typeof(TService)] = typeof(Nested<TService>);
                    }

                    uninitialized = false;

                    if (lazy.IsValueCreated)
                    {
                        return false;
                    }

                    lazy = new Lazy<TService>(() => factory.Invoke() ?? throw new NullReferenceException("注入服务工厂，返回值为“null”!"), true);

                    return true;
                }

                return false;
            }

            public static bool TryAdd(TService instance) => TryAdd(() => instance);

            public static bool TryAdd(Func<TService> factory) => TryAddCore(factory, true);

            public static TService Instance => lazy.Value;
        }

        private class AutoNested<TService> : Nested<TService> where TService : class
        {
            static AutoNested()
            {
                var type = typeof(TService);

                if (type.IsInterface || type.IsAbstract)
                {
                    AddDefaultImpl(() => throw new NotImplementedException(
                        $"未注入{type.FullName}服务的实现，可以使用【SingletonPools.TryAdd<{type.Name}, {type.Name}Impl>()】注入服务实现。"));
                }
                else
                {
                    AddDefaultImpl(() => Nested<TService, TService>.Instance);
                }
            }

            public static TService AutoInstance => Instance; //? 触发静态构造函数。
        }

        private static class Nested<TService, TImplementation>
            where TService : class
            where TImplementation : class, TService
        {
            private static class Lazy
            {
                private const BindingFlags DefaultLookup = BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly;

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
                            var specifiedCount = parameterInfos.Count(y => serviceCachings.ContainsKey(y.ParameterType));

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
                                if (parameterInfo.IsOptional || IsSupport(conversionType, parameterInfo.ParameterType))
                                {
                                    continue;
                                }

                                throw new NotSupportedException($"单例服务（{conversionType.FullName}=>{serviceType.FullName}）的构造函数参数（{parameterInfo.ParameterType.FullName}）未注入单例支持，可以使用【SingletonPools.TryAdd<{parameterInfo.ParameterType.Name}, {parameterInfo.ParameterType.Name}Impl>()】注入服务实现。");
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
                            if (parameterInfo.IsOptional || IsSupport(conversionType, parameterInfo.ParameterType))
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

                private static bool IsSupport(Type conversionType, Type parameterType)
                {
                    if (serviceCachings.ContainsKey(parameterType))
                    {
                        return true;
                    }

                    if (!parameterType.IsAbstract)
                    {
                        foreach (var kv in serviceCachings)
                        {
                            if (kv.Value == parameterType)
                            {
                                serviceCachings[parameterType] = kv.Value;

                                return true;
                            }
                        }
                    }

                    foreach (var kv in serviceCachings)
                    {
                        if (kv.Value == conversionType) //? 防递归注入。
                        {
                            continue;
                        }

                        if (!parameterType.IsAssignableFrom(kv.Key))
                        {
                            continue;
                        }

                        serviceCachings[parameterType] = kv.Value;

                        return true;
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

                        if (serviceCachings.TryGetValue(parameterInfo.ParameterType, out Type implementType))
                        {
                            PropertyInfo propertyInfo = implementType.GetProperty("Instance", DefaultLookup);

                            arguments[i] = propertyInfo!.GetValue(null, null);
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
            }

            public static TImplementation Instance => Lazy.Instance;
        }
    }
}