using Inkslab.Annotations;
using Inkslab.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Inkslab.Sugars
{
    using static Expression;

    /// <summary>
    /// <see cref="IStringSugar"/> 默认实现。
    /// 支持属性空字符串【空字符串运算符（A?B 或 A??B），当属性A为“null”时，返回B内容，否则返回A内容】、属性内容合并(A+B)，属性非“null”合并【空试探合并符(A?+B)，当属性A为“null”时，返回null，否则返回A+B的内容】，末尾表达式支持指定“format”，如：${Datetime:yyyy-MM}、${Enum:D}等。
    /// </summary>
    public class DefaultStringSugar : IStringSugar
    {
        private static readonly Type objectType = typeof(object);
        private static readonly MethodInfo toStringMtd = objectType.GetMethod(nameof(ToString), Type.EmptyTypes);
        private static readonly Regex regularExpression = new Regex("\\$\\{\\{[\\x20\\t\\r\\n\\f]*((?<pre>([_a-zA-Z]\\w*)|[\\u4e00-\\u9fa5]+)[\\x20\\t\\r\\n\\f]*(?<token>(\\??[?+]))[\\x20\\t\\r\\n\\f]*)?(?<name>([_a-zA-Z]\\w*)|[\\u4e00-\\u9fa5]+)(:(?<format>[^\\}]+?))?[\\x20\\t\\r\\n\\f]*\\}\\}", RegexOptions.Multiline);

        private readonly static HashSet<Type> simpleTypes = new HashSet<Type>()
        {
            typeof(sbyte),
            typeof(byte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double)
        };
        private readonly static System.Collections.Concurrent.ConcurrentDictionary<Tuple<Type, Type>, Func<object, object, object>> operationOfAdditionCachings = new System.Collections.Concurrent.ConcurrentDictionary<Tuple<Type, Type>, Func<object, object, object>>();
        private readonly static System.Collections.Concurrent.ConcurrentDictionary<Type, Func<object, string, string>> formatCachings = new System.Collections.Concurrent.ConcurrentDictionary<Type, Func<object, string, string>>();

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public Regex RegularExpression => regularExpression;

        /// <summary>
        /// 创建语法糖。
        /// </summary>
        /// <typeparam name="TSource">数据源类型。</typeparam>
        /// <param name="source">数据源对象。</param>
        /// <param name="settings">语法糖设置。</param>
        /// <returns>处理 <see cref="RegularExpression"/> 语法的语法糖。</returns>
        public virtual ISugar CreateSugar<TSource>(TSource source, DefaultSettings settings) => new StringSugar<TSource>(source, settings);

        /// <summary>
        /// 加法运算。
        /// </summary>
        /// <param name="left">左对象。</param>
        /// <param name="right">右对象。</param>
        /// <returns>运算结果。</returns>
        public static object Add(object left, object right)
        {
            if (left is null)
            {
                return right;
            }

            if (right is null)
            {
                return left;
            }

            return operationOfAdditionCachings.GetOrAdd(Tuple.Create(left.GetType(), right.GetType()), tuple => MakeAddition(tuple.Item1, tuple.Item2)).Invoke(left, right);
        }

        /// <summary>
        /// <paramref name="source"/>.ToString(<paramref name="format"/>).
        /// </summary>
        /// <param name="source">对象。</param>
        /// <param name="format">格式化。</param>
        /// <returns>格式化字符串。</returns>
        public static string Format(object source, string format)
        {
            if (source is null)
            {
                return null;
            }

            if (format.IsEmpty())
            {
                return source.ToString();
            }

            var sourceType = source.GetType();

            if (sourceType.IsNullable())
            {
                sourceType = Nullable.GetUnderlyingType(sourceType);
            }

            return formatCachings.GetOrAdd(sourceType.IsEnum ? typeof(Enum) : sourceType, destinationType =>
             {
                 var formatFn = destinationType.GetMethod(nameof(ToString), new Type[] { typeof(string) }) ?? throw new MissingMethodException($"未找到“{destinationType.Name}.ToString(string format)”方法！");

                 var sourceExp = Parameter(typeof(object));
                 var formatExp = Parameter(typeof(string));

                 var bodyExp = Call(Convert(sourceExp, destinationType), formatFn, formatExp);

                 var lambdaExp = Lambda<Func<object, string, string>>(bodyExp, sourceExp, formatExp);

                 return lambdaExp.Compile();
             })
             .Invoke(source, format);
        }

        private static Func<object, object, object> MakeAddition(Type left, Type right)
        {
            var objectType = typeof(object);

            var leftExp = Parameter(objectType);
            var rightExp = Parameter(objectType);

            if (left.IsNullable())
            {
                left = Nullable.GetUnderlyingType(left);
            }

            if (right.IsNullable())
            {
                right = Nullable.GetUnderlyingType(right);
            }

            var leftRealExp = Parameter(left);
            var rightRealExp = Parameter(right);

            var expressions = new List<Expression>
            {
                Assign(leftRealExp, Convert(leftExp, left)),
                Assign(rightRealExp, Convert(rightExp, right))
            };

            var bodyExp = left == right
                ? Same(left, leftRealExp, rightRealExp)
                : Diff(left, right, leftRealExp, rightRealExp);

            expressions.Add(bodyExp.Type.IsValueType ? Convert(bodyExp, objectType) : bodyExp);

            var lambdaExp = Lambda<Func<object, object, object>>(Block(new ParameterExpression[2] { leftRealExp, rightRealExp }, expressions), leftExp, rightExp);

            return lambdaExp.Compile();
        }

        private static Expression Same(Type type, Expression left, Expression right)
        {
            if (type == typeof(bool))
            {
                return BooleanAdd(left, right);
            }

            if (type.IsEnum)
            {
                var underlyingType = Enum.GetUnderlyingType(type);

                return Convert(Same(underlyingType, Convert(left, underlyingType), Convert(right, underlyingType)), type);
            }

            if (type == typeof(string))
            {
                return Call(null, type.GetMethod(nameof(string.Concat), new Type[] { type, type }), left, right);
            }

            return Expression.Add(left, right);
        }

        private static Expression BooleanAdd(Expression left, Expression right) => AndAlso(left, right);

        private static Expression Diff(Type leftType, Type rightType, Expression leftExp, Expression rightExp)
        {
            if (leftType == typeof(bool) && simpleTypes.Contains(rightType))
            {
                return Same(rightType, Condition(leftExp, Constant(1, rightType), Constant(0, rightType)), rightExp);
            }

            if (rightType == typeof(bool) && simpleTypes.Contains(leftType))
            {
                return Same(leftType, leftExp, Condition(rightExp, Constant(1, leftType), Constant(0, leftType)));
            }

            if (leftType == rightType)
            {
                return Same(rightType, leftExp, rightExp);
            }

            if (leftType == typeof(string))
            {
                return Same(leftType, leftExp, Call(rightExp, rightType.GetMethod(nameof(ToString), Type.EmptyTypes) ?? toStringMtd));
            }

            if (rightType == typeof(string))
            {
                return Same(rightType, Call(leftExp, leftType.GetMethod(nameof(ToString), Type.EmptyTypes) ?? toStringMtd), rightExp);
            }

            return Expression.Add(leftExp, rightExp);
        }

        private class StringSugar<TSource> : AdapterSugar<StringSugar<TSource>>
        {
            static StringSugar()
            {
                var type = typeof(TSource);

                var objectType = typeof(object);

                var settingsType = typeof(DefaultSettings);

                var defaultCst = Constant(null, objectType);

                var parameterExp = Parameter(type, "source");

                var nameExp = Parameter(typeof(string), "name");

                var settingsExp = Parameter(settingsType, "settings");

                var strictExp = Property(settingsExp, nameof(DefaultSettings.Strict));

                var nullValueExp = Property(settingsExp, nameof(DefaultSettings.NullValue));

                var namingCaseExp = Property(settingsExp, nameof(DefaultSettings.NamingCase));

                var sysConvertMethod = typeof(Convert).GetMethod(nameof(System.Convert.ChangeType), new Type[] { objectType, typeof(Type) });

                var toNamingCaseMtd = typeof(StringExtentions).GetMethod(nameof(StringExtentions.ToNamingCase), BindingFlags.Public | BindingFlags.Static);

                var propertyInfos = type.GetProperties();

                var namingTypeCases = new List<SwitchCase>(4);

                var missingMemberErrorCtor = typeof(MissingMemberException).GetConstructor(new Type[] { typeof(string), typeof(string) });

                var defaultBodyExp = Block(IfThen(strictExp, Throw(New(missingMemberErrorCtor, Constant(type.Name), nameExp))), defaultCst);

                foreach (NamingType namingType in Enum.GetValues(typeof(NamingType)))
                {
                    if (namingType == NamingType.Normal)
                    {
                        continue;
                    }

                    var namingTypeConverts = propertyInfos
                        .Where(x => x.CanRead)
                        .Select(propertyInfo =>
                        {
                            MemberExpression propertyExp = Property(parameterExp, propertyInfo);

                            ConstantExpression nameCst = Constant(propertyInfo.Name.ToNamingCase(namingType));

                            if (propertyInfo.PropertyType.IsValueType)
                            {
                                return SwitchCase(Convert(propertyExp, typeof(object)), nameCst);
                            }

                            return SwitchCase(propertyExp, nameCst);
                        });

                    var namingTypeCst = Constant(namingType);

                    var namingTypeBodyExp = Switch(objectType, Call(null, toNamingCaseMtd, nameExp, namingTypeCst), defaultBodyExp, null, namingTypeConverts);

                    namingTypeCases.Add(SwitchCase(namingTypeBodyExp, namingTypeCst));
                }

                var normalCaseConverts = propertyInfos
                        .Where(x => x.CanRead)
                        .Select(propertyInfo =>
                        {
                            MemberExpression propertyExp = Property(parameterExp, propertyInfo);

                            ConstantExpression nameCst = Constant(propertyInfo.Name);

                            if (propertyInfo.PropertyType.IsValueType)
                            {
                                return SwitchCase(Convert(propertyExp, typeof(object)), nameCst);
                            }

                            return SwitchCase(propertyExp, nameCst);
                        });

                var normalCaseBodyExp = Switch(objectType, nameExp, defaultBodyExp, null, normalCaseConverts);

                var switchConvertExp = Switch(objectType, namingCaseExp, normalCaseBodyExp, null, namingTypeCases);

                var lamdaConvert = Lambda<Func<TSource, string, DefaultSettings, object>>(switchConvertExp, parameterExp, nameExp, settingsExp);

                valueGetter = lamdaConvert.Compile();
            }

            private static readonly Func<TSource, string, DefaultSettings, object> valueGetter;

            private readonly TSource source;
            private readonly DefaultSettings settings;

            public StringSugar(TSource source, DefaultSettings settings)
            {
                this.source = source ?? throw new ArgumentNullException(nameof(source));
                this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            }

            [Mismatch("token")]//? 不匹配 token。
            public string Single(string name, string format) => settings.Convert(DefaultStringSugar.Format(valueGetter.Invoke(source, name, settings), format));

            [Mismatch("token")]//? 不匹配 token。
            public string Single(string name) => settings.Convert(valueGetter.Invoke(source, name, settings));

            public string Combination(string pre, string token, string name, string format)
            {
                object result = valueGetter.Invoke(source, pre, settings);

                if (result is null)
                {
                    if (token == "?+")
                    {
                        goto label_core;
                    }
                }
                else if (token == "?" || token == "??")
                {
                    goto label_core;
                }

                result = Add(result, DefaultStringSugar.Format(valueGetter.Invoke(source, name, settings), format));
label_core:
                return settings.Convert(result);
            }

            public string Combination(string pre, string token, string name)
            {
                object result = valueGetter.Invoke(source, pre, settings);

                if (result is null)
                {
                    if (token == "?+")
                    {
                        goto label_core;
                    }
                }
                else if (token == "?" || token == "??")
                {
                    goto label_core;
                }

                result = Add(result, valueGetter.Invoke(source, name, settings));
label_core:
                return settings.Convert(result);
            }
        }
    }
}
