using System.Collections.Generic;
using System.Reflection;
using System.Text;
using static System.Linq.Expressions.Expression;

namespace System
{
    /// <summary>
    /// 枚举扩展。
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// 获取枚举描述。
        /// </summary>
        /// <typeparam name="TEnum">枚举类型。</typeparam>
        /// <param name="enum">枚举值。</param>
        /// <returns>枚举的描述。</returns>
        public static string GetText<TEnum>(this TEnum @enum) where TEnum : struct, Enum
        {
            var type = typeof(TEnum);

            if (Convert<TEnum>.IsFlags)
            {
                bool flag = false;

                var sb = new StringBuilder();

                foreach (var info in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    var constantValue = info.GetRawConstantValue();

                    if (!Nested<TEnum>.Contains(@enum, (TEnum)constantValue))
                    {
                        continue;
                    }

                    if (flag)
                    {
                        sb.Append('|');
                    }
                    else
                    {
                        flag = true;
                    }

                    sb.Append(info.GetDescription());
                }

                return sb.ToString();
            }

            if (Enum.IsDefined(type, @enum))
            {
                foreach (var info in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    var constantValue = info.GetRawConstantValue();

                    if (!Nested<TEnum>.Equals(@enum, (TEnum)constantValue))
                    {
                        continue;
                    }

                    return info.GetDescription();
                }
            }

            return "N/A";
        }

        /// <summary>
        /// 转换为 <see cref="int"/>。
        /// </summary>
        /// <typeparam name="TEnum">枚举类型。</typeparam>
        /// <param name="enum">枚举值。</param>
        /// <exception cref="InvalidCastException">枚举基类型不能隐式转化为<see cref="int"/>。</exception>
        /// <returns>枚举的 <see cref="int"/> 值。</returns>
        public static int ToInt32<TEnum>(this TEnum @enum) where TEnum : struct, Enum => Convert<TEnum>.ToInt(@enum);

        /// <summary>
        /// 转换为 <see cref="long"/>。
        /// </summary>
        /// <typeparam name="TEnum">枚举类型。</typeparam>
        /// <param name="enum">枚举值。</param>
        /// <exception cref="InvalidCastException">枚举基类型不能隐式转化为<see cref="long"/>。</exception>
        /// <returns>枚举的 <see cref="long"/> 值。</returns>
        public static long ToInt64<TEnum>(this TEnum @enum) where TEnum : struct, Enum => Convert<TEnum>.ToLong(@enum);

        /// <summary>
        /// 获取枚举基础数据类型值的字符串。
        /// </summary>
        /// <typeparam name="TEnum">枚举类型。</typeparam>
        /// <param name="enum">枚举值。</param>
        /// <returns>枚举基础数据类型值的字符串</returns>
        public static string ToValueString<TEnum>(this TEnum @enum) where TEnum : struct, Enum => @enum.ToString("D");

        /// <summary>
        /// 获取所有枚举项，标记<see cref="FlagsAttribute"/>的枚举，会返回多个枚举项。
        /// </summary>
        /// <typeparam name="TEnum">枚举类型。</typeparam>
        /// <param name="enum">值。</param>
        /// <returns>枚举项的数组。</returns>
        public static TEnum[] ToValues<TEnum>(this TEnum @enum) where TEnum : struct, Enum
        {
            var type = typeof(TEnum);

            if (Convert<TEnum>.IsFlags)
            {
                var results = new List<TEnum>();

                foreach (TEnum item in Enum.GetValues(type))
                {
                    if (Nested<TEnum>.Contains(@enum, item))
                    {
                        results.Add(item);
                    }
                }

                return results.ToArray();
            }

            if (!Enum.IsDefined(type, @enum))
            {
                return Array.Empty<TEnum>();
            }

            return new TEnum[1] { @enum };
        }

        private static class Nested<TEnum> where TEnum : struct, Enum
        {
            private static readonly Func<TEnum, TEnum, bool> equals;
            private static readonly Func<TEnum, TEnum, bool> contains;

            static Nested()
            {
                var type = typeof(TEnum);

                var leftEx = Parameter(type);
                var rightEx = Parameter(type);

                var underlyingType = Enum.GetUnderlyingType(type);

                var variableLeftEx = Variable(underlyingType);
                var variableRightEx = Variable(underlyingType);

                var bodyEx = OrElse(Equal(variableLeftEx, variableRightEx), AndAlso(NotEqual(variableLeftEx, Default(underlyingType)), AndAlso(NotEqual(variableRightEx, Default(underlyingType)), Equal(And(variableLeftEx, variableRightEx), variableRightEx))));

                var lambdaEx = Lambda<Func<TEnum, TEnum, bool>>(Block(typeof(bool), new[] { variableLeftEx, variableRightEx }, Assign(variableLeftEx, Convert(leftEx, underlyingType)), Assign(variableRightEx, Convert(rightEx, underlyingType)), bodyEx), leftEx, rightEx);

                contains = lambdaEx.Compile();

                var lambdaEqualEx = Lambda<Func<TEnum, TEnum, bool>>(Equal(leftEx, rightEx), leftEx, rightEx);

                equals = lambdaEqualEx.Compile();
            }

            public static bool Equals(TEnum left, TEnum right) => equals.Invoke(left, right);
            public static bool Contains(TEnum left, TEnum right) => contains.Invoke(left, right);
        }

        private static class Convert<TEnum> where TEnum : struct, Enum
        {
            private static readonly Type conversionType;
            private static readonly bool allowConvertToInt = false;
            private static readonly bool allowConvertToLong = false;

            static Convert()
            {
                switch (Type.GetTypeCode(conversionType = typeof(TEnum)))
                {
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                        allowConvertToInt = allowConvertToLong = true;
                        break;
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                        allowConvertToLong = true;
                        break;
                }

                IsFlags = conversionType.IsDefined(typeof(FlagsAttribute), false);
            }

            public static bool IsFlags { get; }

            public static int ToInt(TEnum @enum)
            {
                if (allowConvertToInt)
                {
                    return @enum.GetHashCode();
                }

                throw new InvalidCastException($"{@enum}的基础数据类型为“{conversionType.Name}”，不能安全转换为Int32！");
            }

            public static long ToLong(TEnum @enum)
            {
                if (allowConvertToInt)
                {
                    return @enum.GetHashCode();
                }

                if (allowConvertToLong)
                {
                    return (long)Convert.ChangeType(@enum, conversionType);
                }

                throw new InvalidCastException($"{@enum}的基础数据类型为“{conversionType.Name}”，不能安全转换为Int64！");
            }
        }
    }
}
