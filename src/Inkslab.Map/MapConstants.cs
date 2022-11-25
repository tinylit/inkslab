using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Inkslab.Map
{
    /// <summary>
    /// 映射常量。
    /// </summary>
    public static class MapConstants
    {
        /// <summary>
        /// 静态内容。
        /// </summary>
        public const BindingFlags StaticBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        /// <summary>
        /// 本类的静态内容。
        /// </summary>
        public const BindingFlags StaticDeclaredOnlyBindingFlags = StaticBindingFlags | BindingFlags.DeclaredOnly;

        /// <summary>
        /// 实例。
        /// </summary>
        public const BindingFlags InstanceBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// 本类的实例。
        /// </summary>
        public const BindingFlags InstanceDeclaredOnlyBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// <see cref="IEnumerable"/> 接口。
        /// </summary>
        public readonly static Type EnumerableType = typeof(IEnumerable);

        /// <summary>
        /// <see cref="IEnumerator"/> 接口。
        /// </summary>
        public readonly static Type EnumeratorType = typeof(IEnumerator);

        /// <summary>
        /// <see cref="IEnumerable{T}"/> 接口。
        /// </summary>
        public readonly static Type Enumerable_T_Type = typeof(IEnumerable<>);

        /// <summary>
        /// <see cref="IEnumerator{T}"/> 接口。
        /// </summary>
        public readonly static Type Enumerator_T_Type = typeof(IEnumerator<>);

        /// <summary>
        /// <see cref="IEnumerator.MoveNext"/> 方法。
        /// </summary>
        public readonly static MethodInfo MoveNextMtd = EnumeratorType.GetMethod("MoveNext", Type.EmptyTypes);

        /// <summary>
        /// <see cref="StringExtentions.IsEmpty(string)"/> 方法。
        /// </summary>
        public readonly static MethodInfo IsEmptyMtd = typeof(StringExtentions).GetMethod(nameof(StringExtentions.IsEmpty), StaticDeclaredOnlyBindingFlags);

        /// <summary>
        /// <see cref="StringExtentions.ToNamingCase(string, NamingType)"/> 方法。
        /// </summary>
        public readonly static MethodInfo ToNamingCaseMtd = typeof(StringExtentions).GetMethod(nameof(StringExtentions.ToNamingCase), StaticDeclaredOnlyBindingFlags);

        /// <summary>
        /// <see cref="Enumerable.Cast{TResult}(IEnumerable)"/> 方法。
        /// </summary>
        public readonly static MethodInfo CastMtd = typeof(Enumerable).GetMethod(nameof(Enumerable.Cast), StaticDeclaredOnlyBindingFlags);

        /// <summary>
        /// <see cref="Enumerable.ToArray{TSource}(IEnumerable{TSource})"/> 方法。
        /// </summary>
        public readonly static MethodInfo ToArrayMtd = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray), StaticDeclaredOnlyBindingFlags);

        /// <summary>
        /// <see cref="string.ToLower()"/> 方法。
        /// </summary>
        public readonly static MethodInfo ToLowerMtd = typeof(string).GetMethod(nameof(string.ToLower), InstanceDeclaredOnlyBindingFlags, null, Type.EmptyTypes, null);

        /// <summary>
        /// <see cref="ICloneable.Clone()"/> 方法。
        /// </summary>
        public readonly static MethodInfo CloneMtd = typeof(ICloneable).GetMethod(nameof(ICloneable.Clone));

        /// <summary>
        /// <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)"/> 方法。
        /// </summary>
        public static readonly MethodInfo TryParseMtd = typeof(Enum).GetMember(nameof(Enum.TryParse), MemberTypes.Method, BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                                            .Cast<MethodInfo>()
                                            .First(x => x.IsGenericMethod && x.GetParameters().Length == 3);
        /// <summary>
        /// <see cref="InvalidCastException(string)"/> 构造函数。
        /// </summary>
        public readonly static ConstructorInfo InvalidCastExceptionCtorOfString = typeof(InvalidCastException).GetConstructor(new Type[1] { typeof(string) });
    }
}
