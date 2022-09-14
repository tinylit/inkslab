using System.Collections.Generic;

namespace System
{
    /// <summary>
    /// 类型扩展。
    /// </summary>
    public static class TypeExtensions
    {
        static readonly Type Nullable_T_Type = typeof(Nullable<>);
        static readonly Type KeyValuePair_TKey_TValue_Type = typeof(KeyValuePair<,>);

        private static readonly HashSet<Type> _miniTypes = new HashSet<Type>()
        {
            typeof(bool),
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal)
        };

        private static readonly HashSet<Type> _simpleTypes = new HashSet<Type>()
        {
            //+ _miniTypes
            typeof(char),
            typeof(string),
            typeof(Guid),
            typeof(TimeSpan),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(byte[])
        };

        /// <summary>
        /// 当前类型是否是迷你类型（枚举或不需要引号包裹的类型）。
        /// </summary>
        /// <param name="type">类型。</param>
        /// <returns>是返回True，不是返回False。</returns>
        public static bool IsMini(this Type type) => type.IsEnum || type.IsValueType && _miniTypes.Contains(type);

        /// <summary>
        /// 当前类型是否是简单类型（基础类型）。
        /// </summary>
        /// <param name="type">类型。</param>
        /// <returns>是返回True，不是返回False。</returns>
        public static bool IsSimple(this Type type) => type.IsEnum || type.IsValueType && (_miniTypes.Contains(type) || _simpleTypes.Contains(type));

        /// <summary>
        /// 判断类型是否为Nullable类型。
        /// </summary>
        /// <param name="type"> 要处理的类型。</param>
        /// <returns> 是返回True，不是返回False。</returns>
        public static bool IsNullable(this Type type) => type.IsValueType && type.IsGenericType && type.GetGenericTypeDefinition() == Nullable_T_Type;

        /// <summary>
        /// 判断类型是否为KeyValuePair类型。
        /// </summary>
        /// <param name="type"> 要处理的类型。 </param>
        /// <returns> 是返回True，不是返回False。 </returns>
        public static bool IsKeyValuePair(this Type type) => type.IsValueType && type.IsGenericType && type.GetGenericTypeDefinition() == KeyValuePair_TKey_TValue_Type;
    }
}

