using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace System
{
    /// <summary>
    /// 类型扩展。
    /// </summary>
    public static class TypeExtensions
    {
        static readonly Type CharType = typeof(char);
        static readonly Type StringType = typeof(string);
        static readonly Type DecimalType = typeof(decimal);
        static readonly Type Nullable_T_Type = typeof(Nullable<>);
        static readonly Type KeyValuePair_TKey_TValue_Type = typeof(KeyValuePair<,>);

        private static readonly HashSet<Type> _simpleTypes = new HashSet<Type>()
        {            
        /*+ 基元类型。
         * bool
         * sbyte
         * byte
         * char
         * float
         * double
         * short
         * ushort
         * int
         * uint
         * long
         * ulong
         * IntPtr
         * UIntPtr
         */

            DecimalType,
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
        public static bool IsMini(this Type type) => type.IsEnum || type.IsValueType && (type.IsPrimitive ? type != CharType : type == DecimalType);

        /// <summary>
        /// 当前类型是否是简单类型（基础类型）。
        /// </summary>
        /// <param name="type">类型。</param>
        /// <returns>是返回True，不是返回False。</returns>
        public static bool IsSimple(this Type type) => type.IsValueType ? (type.IsEnum || type.IsPrimitive || _simpleTypes.Contains(type)) : type == StringType;

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

        /// <summary>
        /// 类型 <paramref name="implementationType"/> 是 <paramref name="type"/> 的子类或实现类，或本身或任意父类或任意实现接口为泛型且与 <paramref name="type"/> 相同（泛型数相等、顺序相同且约束相似）。
        /// </summary>
        /// <param name="type">指定类型。</param>
        /// <param name="implementationType">比较类型。</param>
        /// <returns></returns>
        public static bool IsLike(this Type type, Type implementationType)
        {
            if (type is null)
            {
                return implementationType is null;
            }

            if (implementationType is null)
            {
                return false;
            }

            if (implementationType.IsInterface && type.IsClass)
            {
                return false;
            }

            if (type.IsGenericParameter ^ implementationType.IsGenericParameter)
            {
                return false;
            }

            if (type.IsGenericType ^ implementationType.IsGenericType)
            {
                return false;
            }

            if (type.IsGenericTypeDefinition ^ implementationType.IsGenericTypeDefinition)
            {
                return false;
            }

            if (!type.IsGenericTypeDefinition && !type.IsGenericParameter)
            {
                return type.IsAssignableFrom(implementationType);
            }

            bool isTypeDefinition = type.IsGenericTypeDefinition;

            if (type.IsInterface)
            {
                if (IsLikeInterfaces(type, implementationType.GetInterfaces()))
                {
                    if (isTypeDefinition)
                    {
                        goto label_typeDefinition;
                    }

                    return true;
                }

                return false;
            }

            if (IsLikeClass(type, implementationType))
            {
                if (isTypeDefinition)
                {
                    goto label_typeDefinition;
                }

                return true;

            }

            return false;

label_typeDefinition:

            return IsLikeGenericArguments(type.GetGenericArguments(), implementationType.GetGenericArguments());
        }

        private static bool IsLikeClass(Type type, Type implementationType)
        {
            if (type == typeof(object))
            {
                return true;
            }

            if (type.IsGenericType)
            {
                var typeDefinition = type.GetGenericTypeDefinition();

                do
                {
                    if (implementationType.IsGenericType && implementationType.GetGenericTypeDefinition() == typeDefinition)
                    {
                        return true;
                    }

                    implementationType = implementationType.BaseType;

                } while (implementationType != null);
            }
            else
            {
                do
                {
                    if (type == implementationType)
                    {
                        return true;
                    }

                    implementationType = implementationType.BaseType;

                } while (implementationType != null);
            }

            return false;
        }

        private static bool IsLikeInterfaces(Type interfaceType, Type[] implementationTypes)
        {
            if (!interfaceType.IsGenericType)
            {
                return implementationTypes.Contains(interfaceType);
            }

            var typeDefinition = interfaceType.GetGenericTypeDefinition();

            foreach (var type in implementationTypes)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeDefinition)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsLikeGenericArguments(Type[] typeArguments, Type[] implementationTypeArguments)
        {
            if (typeArguments.Length != implementationTypeArguments.Length)
            {
                return false;
            }

            for (int i = 0; i < typeArguments.Length; i++)
            {
                if (!IsLikeGenericArgument(typeArguments[i], implementationTypeArguments[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsLikeGenericArgument(Type typeArgument, Type implementationTypeArgument)
        {
            if ((typeArgument.GenericParameterAttributes & GenericParameterAttributes.DefaultConstructorConstraint) == GenericParameterAttributes.DefaultConstructorConstraint)
            {
                if ((implementationTypeArgument.GenericParameterAttributes & GenericParameterAttributes.DefaultConstructorConstraint) != GenericParameterAttributes.DefaultConstructorConstraint
                    && (implementationTypeArgument.GenericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != GenericParameterAttributes.NotNullableValueTypeConstraint)
                {
                    return false;
                }
            }

            if ((typeArgument.GenericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != (implementationTypeArgument.GenericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint))
            {
                return false;
            }

            if ((typeArgument.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) == GenericParameterAttributes.ReferenceTypeConstraint)
            {
                if ((implementationTypeArgument.GenericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) == GenericParameterAttributes.NotNullableValueTypeConstraint)
                {
                    return false;
                }
            }

            return IsLike(typeArgument, implementationTypeArgument);
        }
    }
}

