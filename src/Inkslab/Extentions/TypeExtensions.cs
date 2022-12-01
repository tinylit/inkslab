using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace System
{
    /// <summary>
    /// 类型相似。
    /// </summary>
    [Flags]
    public enum TypeLikeKind
    {
        /// <summary>
        /// 类型模板。
        /// 1、源类型为普通类型：目标类型可赋值给源类型。
        /// 2、源类型为泛型：
        ///     2.1 目标类型是泛型时，实现的泛型接口或继承的泛型类的泛型声明是源类型的泛型声明，且泛型参数和源类型的泛型参数相同或相似。
        ///     2.2 目标类型不是泛型时，实现的泛型接口或继承的泛型类的泛型是源类型。
        /// 3、源类型为泛型声明:
        ///     3.1 目标类型为泛型声明类型时，泛型参数和源类型泛型参数相同，且泛型参数一一对应且相同或相似。
        ///     3.2 目标类型部位泛型声明类型时，实现的泛型接口或继承的泛型类的泛型声明是源类型相同。
        /// 4、 源类型为泛型参数：
        ///     4.1 目标类型为泛型参数，先判断两者泛型约束（无参构造函数、值类型、引用类型、基础类、基础接口）是否相似。
        ///     4.2 目标类型不为泛型参数，依次校验以下内容：
        ///         4.2.1 源类型有值类型约束，目标类型必须是值类型且不为可空类型。
        ///         4.2.2 源类型有引用类型约束，目标类型必须是引用类型。
        ///         4.2.3 源类型有无参构造函数约束，目标类型必须有公共无参构造函数。
        ///         4.2.4 源类型有接口约束，目标类型需与所有接口类型相似。
        ///         4.2.5 源类型有基类约束，目标类型需与基类相似。
        /// </summary>
        Template = 0,
        /// <summary>
        /// 类型均为 <see cref="Type.IsGenericType"/>。
        /// </summary>
        IsGenericType = 1,
        /// <summary>
        /// 类型均为 <see cref="Type.IsGenericTypeDefinition"/>.
        /// </summary>
        IsGenericTypeDefinition = 2,
        /// <summary>
        /// 类型均为 <see cref="Type.IsGenericParameter"/>.
        /// </summary>
        IsGenericTypeParameter = 4
    }

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
            /*? 基元类型。
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
        /// <returns>是否相似。</returns>
        public static bool IsLike(this Type type, Type implementationType)
            => IsLike(type, implementationType, TypeLikeKind.IsGenericType | TypeLikeKind.IsGenericTypeDefinition | TypeLikeKind.IsGenericTypeParameter);

        /// <summary>
        /// 类型 <paramref name="implementationType"/> 是 <paramref name="type"/> 的子类或实现类，或本身或任意父类或任意实现接口为泛型且与 <paramref name="type"/> 相同（泛型数相等、顺序相同且约束相似）。
        /// </summary>
        /// <param name="type">指定类型。</param>
        /// <param name="implementationType">比较类型。</param>
        /// <returns>是否相似。</returns>
        public static bool IsLikeTemplate(this Type type, Type implementationType)
            => IsLike(type, implementationType, TypeLikeKind.Template);

        /// <summary>
        /// 类型 <paramref name="implementationType"/> 是 <paramref name="type"/> 的子类或实现类，或本身或任意父类或任意实现接口为泛型且与 <paramref name="type"/> 相同（泛型数相等、顺序相同且约束相似）。
        /// </summary>
        /// <param name="type">指定类型。</param>
        /// <param name="implementationType">比较类型。</param>
        /// <param name="likeKind">相似比较。</param>
        /// <returns>是否相似。</returns>
        public static bool IsLike(this Type type, Type implementationType, TypeLikeKind likeKind)
        {
            if (type == implementationType)
            {
                return true;
            }

            if (implementationType is null)
            {
                return false;
            }

            if (implementationType.IsInterface && (type.IsClass || type.IsValueType))
            {
                return false;
            }

            if (type.IsGenericParameter)
            {
                if (implementationType.IsGenericParameter)
                {
                    return IsLikeGenericParameter(type, implementationType);
                }

                if ((likeKind & TypeLikeKind.IsGenericTypeParameter) == TypeLikeKind.IsGenericTypeParameter)
                {
                    return false;
                }

                return IsLikeGenericParameterAssign(type, implementationType);
            }

            if (type.IsGenericTypeDefinition)
            {
                if (implementationType.IsGenericTypeDefinition)
                {
                    if (IsLikeTypeDefinition(type, implementationType))
                    {
                        goto label_generic_type;
                    }

                    return false;
                }

                if ((likeKind & TypeLikeKind.IsGenericTypeDefinition) == TypeLikeKind.IsGenericTypeDefinition)
                {
                    return false;
                }
            }
            else if (type.IsGenericType)
            {
                if (implementationType.IsGenericType)
                {
                    if (IsLikeTypeDefinition(type.GetGenericTypeDefinition(), implementationType))
                    {
                        goto label_generic_type;
                    }

                    return false;
                }

                if ((likeKind & TypeLikeKind.IsGenericType) == TypeLikeKind.IsGenericType)
                {
                    return false;
                }
            }

            if (type == typeof(object))
            {
                return true;
            }

            bool isGenericType = type.IsGenericType;

            if (type.IsInterface)
            {
                if (IsLikeInterfaces(type, implementationType.GetInterfaces()))
                {
                    if (isGenericType)
                    {
                        goto label_generic_type;
                    }

                    return true;
                }

                return false;
            }

            if (IsLikeClass(type, implementationType))
            {
                if (isGenericType)
                {
                    goto label_generic_type;
                }

                return true;

            }

            return false;

label_generic_type:

            return IsLikeGenericArguments(type.GetGenericArguments(), implementationType.GetGenericArguments());
        }

        private static bool IsLikeClass(Type type, Type implementationType)
        {
            if (type.IsGenericType)
            {
                var typeDefinition = type.IsGenericTypeDefinition
                    ? type
                    : type.GetGenericTypeDefinition();

                do
                {
                    if (implementationType.IsGenericType && implementationType.GetGenericTypeDefinition() == typeDefinition)
                    {
                        return true;
                    }

                    implementationType = implementationType.BaseType;

                } while (implementationType is not null);
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

                } while (implementationType is not null);
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
                var typeArgument = typeArguments[i];
                var implementationTypeArgument = implementationTypeArguments[i];

                if (typeArgument.IsGenericParameter)
                {
                    if (implementationTypeArgument.IsGenericParameter)
                    {
                        if (IsLikeGenericParameter(typeArgument, implementationTypeArgument))
                        {
                            continue;
                        }
                    }
                    else if (IsLikeGenericParameterAssign(typeArgument, implementationTypeArgument))
                    {
                        continue;
                    }

                    return false;
                }

                if (typeArgument.IsGenericType || typeArgument.IsGenericTypeDefinition)
                {
                    if (implementationTypeArgument.IsGenericType || implementationTypeArgument.IsGenericTypeDefinition)
                    {
                        if (!IsLikeTypeDefinition(typeArgument.IsGenericTypeDefinition
                            ? typeArgument
                            : typeArgument.GetGenericTypeDefinition(), implementationTypeArgument))
                        {
                            return false;
                        }
                    }

                    return IsLikeGenericArguments(typeArgument.GetGenericArguments(), implementationTypeArgument.GetGenericArguments());
                }

                if (typeArgument != implementationTypeArgument)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsLikeGenericParameter(Type typeArgument, Type implementationTypeArgument)
        {
            //? 值类型约束。
            if ((typeArgument.GenericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) > (implementationTypeArgument.GenericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint))
            {
                return false;
            }

            //? 引用类约束。
            if ((typeArgument.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) > (implementationTypeArgument.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint))
            {
                return false;
            }

            //? 协变。
            if ((typeArgument.GenericParameterAttributes & GenericParameterAttributes.Covariant) < (implementationTypeArgument.GenericParameterAttributes & GenericParameterAttributes.Covariant))
            {
                return false;
            }

            //? 逆变。
            if ((typeArgument.GenericParameterAttributes & GenericParameterAttributes.Contravariant) < (implementationTypeArgument.GenericParameterAttributes & GenericParameterAttributes.Contravariant))
            {
                return false;
            }

            //? 无参构造函数。
            if ((typeArgument.GenericParameterAttributes & GenericParameterAttributes.DefaultConstructorConstraint) == GenericParameterAttributes.DefaultConstructorConstraint)
            {
                if ((implementationTypeArgument.GenericParameterAttributes & GenericParameterAttributes.DefaultConstructorConstraint) != GenericParameterAttributes.DefaultConstructorConstraint
                    && (implementationTypeArgument.GenericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) != GenericParameterAttributes.NotNullableValueTypeConstraint)
                {
                    return false;
                }
            }

            foreach (var interfaceType in typeArgument.GetInterfaces())
            {
                if (!IsLike(interfaceType, implementationTypeArgument))
                {
                    return false;
                }
            }

            return IsLike(typeArgument.BaseType, implementationTypeArgument.BaseType);
        }

        private static bool IsLikeTypeDefinition(Type typeDefinition, Type implementationType)
        {
            if (typeDefinition.IsInterface)
            {
                foreach (var interfaceType in implementationType.GetInterfaces())
                {
                    if (interfaceType.IsGenericType && (interfaceType == typeDefinition || interfaceType.GetGenericTypeDefinition() == typeDefinition))
                    {
                        return true;
                    }
                }
            }
            else
            {
                do
                {
                    if (implementationType.IsGenericType && (typeDefinition == implementationType || typeDefinition == implementationType.GetGenericTypeDefinition()))
                    {
                        return true;
                    }

                    implementationType = implementationType.BaseType;

                } while (implementationType is not null);
            }

            return false;
        }

        private static bool IsLikeGenericParameterAssign(Type typeDefinition, Type implementationType)
        {
            //? 值类型约束。
            if ((typeDefinition.GenericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) == GenericParameterAttributes.NotNullableValueTypeConstraint)
            {
                if (implementationType.IsClass || implementationType.IsNullable())
                {
                    return false;
                }
            }

            //? 引用类约束。
            if ((typeDefinition.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) == GenericParameterAttributes.ReferenceTypeConstraint)
            {
                if (implementationType.IsValueType)
                {
                    return false;
                }
            }

            //? 无参构造函数。
            if ((typeDefinition.GenericParameterAttributes & GenericParameterAttributes.DefaultConstructorConstraint) == GenericParameterAttributes.DefaultConstructorConstraint)
            {
                if (!implementationType.IsValueType)
                {
                    var constructorInfo = implementationType.GetConstructor(Type.EmptyTypes);

                    if (constructorInfo is null)
                    {
                        return false;
                    }
                }
            }

            foreach (var interfaceType in typeDefinition.GetInterfaces())
            {
                if (!IsLikeTemplate(interfaceType, implementationType))
                {
                    return false;
                }
            }

            return typeDefinition.BaseType is null || IsLikeTemplate(typeDefinition.BaseType, implementationType);
        }
    }
}

