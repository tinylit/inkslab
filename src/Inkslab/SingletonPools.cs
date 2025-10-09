using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

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
        private static readonly ConcurrentDictionary<Type, Type> _serviceCachings = new ConcurrentDictionary<Type, Type>();
        
        /// <summary>
        /// 构造函数缓存。
        /// </summary>
        private static readonly ConcurrentDictionary<Type, ConstructorInfo[]> _constructorCache = new ConcurrentDictionary<Type, ConstructorInfo[]>();
        
        /// <summary>
        /// 参数信息缓存。
        /// </summary>
        private static readonly ConcurrentDictionary<ConstructorInfo, ParameterInfo[]> _parameterCache = new ConcurrentDictionary<ConstructorInfo, ParameterInfo[]>();
        
        /// <summary>
        /// 属性信息缓存。
        /// </summary>
        private static readonly ConcurrentDictionary<Type, PropertyInfo> _propertyCache = new ConcurrentDictionary<Type, PropertyInfo>();

        /// <summary>
        /// 添加服务。
        /// </summary>
        /// <returns>是否添加成功。</returns>
        public static bool TryAdd<TService>()
            where TService : class
            => Nested<TService>.TryAdd(() => Nested<TService, TService>.Instance, SingletonWeights.Normal);

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

            return Nested<TService>.TryAdd(factory, SingletonWeights.Delegation);
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
            => Nested<TService>.TryAdd(() => Nested<TService, TImplementation>.Instance, SingletonWeights.Normal);

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

        private enum SingletonWeights
        {
            /// <summary>
            /// 最低。
            /// </summary>
            Lowest,
            /// <summary>
            /// 正常。
            /// </summary>
            Normal,
            /// <summary>
            /// 委托。
            /// </summary>
            Delegation,
            /// <summary>
            /// 指定。
            /// </summary>
            Designation
        }

        private class Nested<TService> where TService : class
        {
            private static Lazy<TService> lazy = new Lazy<TService>(() => null);
            private static bool uninitialized = true;
            private static SingletonWeights singletonWeights = SingletonWeights.Lowest;

            public static bool TryAdd(Func<TService> factory, SingletonWeights weights)
            {
                if (uninitialized || weights >= singletonWeights)
                {
                    uninitialized = false;

                    singletonWeights = weights;

                    if (lazy.IsValueCreated)
                    {
                        return false;
                    }

                    _serviceCachings.TryAdd(typeof(TService), typeof(Nested<TService>));

                    lazy = new Lazy<TService>(() => factory.Invoke() ?? throw new NullReferenceException("注入服务工厂，返回值为“null”!"), true);

                    return true;
                }

                return false;
            }

            public static bool TryAdd(TService instance) => TryAdd(() => instance, SingletonWeights.Designation);

            public static TService Instance => lazy.Value;
        }

        private class AutoNested<TService> : Nested<TService> where TService : class
        {
            static AutoNested()
            {
                var type = typeof(TService);

                if (type.IsInterface || type.IsAbstract)
                {
                    TryAdd(() => throw new NotSupportedException($"未注入{type.FullName}服务的实现，可以使用【SingletonPools.TryAdd<{type.Name}, {type.Name}Impl>()】注入服务实现。"), SingletonWeights.Lowest);
                }
                else
                {
                    TryAdd(() => Nested<TService, TService>.Instance, SingletonWeights.Lowest);
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

                    // 使用缓存的构造函数获取
                    var constructorInfos = GetCachedConstructors(conversionType);

                    var constructorInfo = Resolved(typeof(Nested<TImplementation>), constructorInfos);

                    if (constructorInfo is null)
                    {
                        // 优化：只检查最后一个构造函数，因为已经按优先级排序
                        var lastConstructor = constructorInfos[constructorInfos.Length - 1];
                        var parameterInfos = GetCachedParameters(lastConstructor);

                        foreach (var parameterInfo in parameterInfos)
                        {
                            if (parameterInfo.IsOptional || IsSupport(conversionType, parameterInfo.ParameterType))
                            {
                                continue;
                            }

                            throw new NotSupportedException($"单例服务（{conversionType.FullName}=>{serviceType.FullName}）的构造函数参数（{parameterInfo.ParameterType.FullName}）未注入单例支持，可以使用【SingletonPools.TryAdd<{parameterInfo.ParameterType.Name}, {parameterInfo.ParameterType.Name}Impl>()】注入服务实现。");
                        }
                    }

                    Instance = CreateInstance(constructorInfo);
                }

                /// <summary>
                /// 获取缓存的构造函数列表
                /// </summary>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static ConstructorInfo[] GetCachedConstructors(Type conversionType)
                {
                    return _constructorCache.GetOrAdd(conversionType, type =>
                        type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                            .OrderBy(x => x.IsPublic ? 0 : 1)
                            .ThenByDescending(x =>
                            {
                                var parameterInfos = GetCachedParameters(x);
                                var specifiedCount = 0;
                                
                                // 优化：避免 LINQ Count() 的开销
                                foreach (var param in parameterInfos)
                                {
                                    if (_serviceCachings.ContainsKey(param.ParameterType))
                                    {
                                        specifiedCount++;
                                    }
                                }

                                return parameterInfos.Length * parameterInfos.Length + specifiedCount;
                            })
                            .ToArray());
                }

                /// <summary>
                /// 获取缓存的参数信息
                /// </summary>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static ParameterInfo[] GetCachedParameters(ConstructorInfo constructorInfo)
                {
                    return _parameterCache.GetOrAdd(constructorInfo, ctor => ctor.GetParameters());
                }

                private static ConstructorInfo Resolved(Type conversionType, ConstructorInfo[] constructorInfos)
                {
                    foreach (var constructorInfo in constructorInfos)
                    {
                        var parameterInfos = GetCachedParameters(constructorInfo);
                        bool isValid = true;

                        foreach (var parameterInfo in parameterInfos)
                        {
                            if (!parameterInfo.IsOptional && !IsSupport(conversionType, parameterInfo.ParameterType))
                            {
                                isValid = false;
                                break;
                            }
                        }

                        if (isValid)
                        {
                            return constructorInfo;
                        }
                    }

                    return null;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static bool IsSupport(Type conversionType, Type parameterType)
                {
                    // 快速路径：直接检查缓存
                    if (_serviceCachings.ContainsKey(parameterType))
                    {
                        return true;
                    }

                    // 对于非抽象类型，尝试找到匹配的实现
                    if (!parameterType.IsAbstract)
                    {
                        foreach (var kv in _serviceCachings)
                        {
                            if (ReferenceEquals(kv.Value, parameterType))
                            {
                                _serviceCachings.TryAdd(parameterType, kv.Value);
                                return true;
                            }
                        }
                    }

                    // 检查可分配性
                    foreach (var kv in _serviceCachings)
                    {
                        if (ReferenceEquals(kv.Value, conversionType)) // 防递归注入
                        {
                            continue;
                        }

                        if (parameterType.IsAssignableFrom(kv.Key))
                        {
                            _serviceCachings.TryAdd(parameterType, kv.Value);
                            return true;
                        }
                    }

                    return false;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static TImplementation CreateInstance(ConstructorInfo constructorInfo)
                {
                    var parameterInfos = GetCachedParameters(constructorInfo);

                    if (parameterInfos.Length == 0)
                    {
                        // 快速路径：无参构造函数
                        return (TImplementation)constructorInfo.Invoke(new object[0]);
                    }

                    var arguments = new object[parameterInfos.Length];

                    for (int i = 0; i < parameterInfos.Length; i++)
                    {
                        var parameterInfo = parameterInfos[i];

                        if (_serviceCachings.TryGetValue(parameterInfo.ParameterType, out Type implementType))
                        {
                            // 使用缓存的属性信息
                            var propertyInfo = GetCachedInstanceProperty(implementType);
                            arguments[i] = propertyInfo.GetValue(null, null);
                        }
                        else
                        {
                            arguments[i] = parameterInfo.DefaultValue;
                        }
                    }

                    return (TImplementation)constructorInfo.Invoke(arguments);
                }

                /// <summary>
                /// 获取缓存的实例属性
                /// </summary>
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                private static PropertyInfo GetCachedInstanceProperty(Type implementType)
                {
                    return _propertyCache.GetOrAdd(implementType, type => 
                        type.GetProperty("Instance", DefaultLookup) ?? 
                        throw new InvalidOperationException($"Type {type.FullName} does not have an Instance property"));
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