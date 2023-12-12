using Inkslab.Annotations;
using Inkslab.Settings;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Inkslab.Sugars
{
    using static Expression;

    /// <summary>
    /// <see cref="IStringSugar"/> 默认实现。
    /// 支持属性空字符串【空字符串运算符（A?B 或 A??B），当属性A为 <see langword="null"/> 时，返回B内容，否则返回A内容】、属性内容合并(A+B)，属性非 <see langword="null"/> 合并【空试探合并符(A?+B)，当属性A为 <see langword="null"/> 时，返回 <see langword="null"/>，否则返回A+B的内容】，末尾表达式支持指定“format”，如：${Datetime:yyyy-MM}、${Enum:D}等。
    /// </summary>
    public class DefaultStringSugar : IStringSugar
    {
        private static readonly Type enumType = typeof(Enum);
        private static readonly MethodInfo enumToStringMtd = enumType.GetMethod(nameof(ToString), Type.EmptyTypes);
        private static readonly MethodInfo enumToStringFormatMtd = enumType.GetMethod(nameof(ToString), new Type[] { typeof(string) });

        private static readonly Regex regularExpression = new Regex("\\$\\{[\\x20\\t\\r\\n\\f]*((?<pre>[\\w\\u4e00-\\u9fa5]+(\\.[\\w\\u4e00-\\u9fa5]+)*)[\\x20\\t\\r\\n\\f]*(?<token>\\??[?+])[\\x20\\t\\r\\n\\f]*)?(?<name>[\\w\\u4e00-\\u9fa5]+(\\.[\\w\\u4e00-\\u9fa5]+)*)(:(?<format>[^\\}]+?))?[\\x20\\t\\r\\n\\f]*\\}",
            RegexOptions.Multiline);

        private static readonly HashSet<Type> simpleTypes = new HashSet<Type>()
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

        private static readonly ConcurrentDictionary<Type, SyntaxPool> sugarCachings = new ConcurrentDictionary<Type, SyntaxPool>();

        private static readonly ParameterExpression parameterOfSource;
        private static readonly ParameterExpression parameterOfSettings;
        private static readonly ParameterExpression parameterOfSugar;

        private static readonly MemberExpression undo;

        private static readonly MemberExpression strict;
        private static readonly MemberExpression nullValue;
        private static readonly MemberExpression preserveSyntax;

        private static readonly MethodInfo convertMtd;
        private static readonly MethodInfo toStringMtd;

        static DefaultStringSugar()
        {
            var objectType = typeof(object);
            var sugarType = typeof(StringSugar);
            var settingsType = typeof(DefaultSettings);

            parameterOfSource = Parameter(typeof(object), "source");
            parameterOfSettings = Parameter(settingsType, "settings");
            parameterOfSugar = Parameter(sugarType, "sugar");

            undo = Property(parameterOfSugar, nameof(StringSugar.Undo));

            strict = Property(parameterOfSettings, nameof(DefaultSettings.Strict));
            nullValue = Property(parameterOfSettings, nameof(DefaultSettings.NullValue));
            preserveSyntax = Property(parameterOfSettings, nameof(DefaultSettings.PreserveSyntax));

            convertMtd = settingsType.GetMethod(nameof(DefaultSettings.Convert), new Type[] { objectType });
            toStringMtd = settingsType.GetMethod(nameof(DefaultSettings.ToString), new Type[] { objectType });
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public Regex RegularExpression => regularExpression;

        /// <summary>
        /// 创建语法糖。
        /// </summary>
        /// <param name="source">数据源对象。</param>
        /// <param name="settings">语法糖设置。</param>
        /// <returns>处理 <see cref="RegularExpression"/> 语法的语法糖。</returns>
        public ISugar CreateSugar(object source, DefaultSettings settings) => new StringSugar(source, settings, sugarCachings.GetOrAdd(source.GetType(), sourceType => new SyntaxPool(sourceType)));

        private static Expression MakeAdd(Expression left, Expression right)
        {
            var leftType = left.Type;
            var rightType = right.Type;

            if (leftType.IsNullable())
            {
                leftType = Nullable.GetUnderlyingType(leftType)!;

                left = Condition(Equal(left, Constant(null, left.Type)), Default(leftType), Convert(left, leftType));
            }

            if (rightType.IsNullable())
            {
                rightType = Nullable.GetUnderlyingType(rightType)!;

                right = Condition(Equal(right, Constant(null, right.Type)), Default(rightType), Convert(right, rightType));
            }

            return leftType == rightType
                ? Same(leftType, left, right)
                : Diff(leftType, rightType, left, right);
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
                return Call(null, type.GetMethod(nameof(string.Concat), new Type[] { type, type })!, left, right);
            }

            return Add(left, right);
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
                return Same(leftType, leftExp, Call(parameterOfSettings, toStringMtd, rightExp));
            }

            if (rightType == typeof(string))
            {
                return Same(rightType, Call(parameterOfSettings, toStringMtd, leftExp), rightExp);
            }

            if (leftType.IsEnum || rightType.IsEnum)
            {
                return MakeAdd(leftType.IsEnum
                    ? Convert(leftExp, Enum.GetUnderlyingType(leftType))
                    : leftExp, rightType.IsEnum
                    ? Convert(rightExp, Enum.GetUnderlyingType(rightType))
                    : rightExp);
            }

            return Add(leftExp, rightExp);
        }

        private static Expression ToStringAuto(Expression instance)
        {
            var instanceType = instance.Type;

            if (instanceType == typeof(string))
            {
                return instance;
            }

            if (instanceType.IsEnum)
            {
                return Call(Convert(instance, enumType), enumToStringMtd);
            }

            if (instanceType.IsMini())
            {
                var stringMtd = instanceType.GetMethod(nameof(ToString), Type.EmptyTypes);

                if (stringMtd is null)
                {
                    return Call(parameterOfSettings, toStringMtd, Convert(instance, typeof(object)));
                }

                return Call(instance, stringMtd);
            }

            if (instanceType.IsValueType)
            {
                return Call(parameterOfSettings, toStringMtd, Convert(instance, typeof(object)));
            }

            return Call(parameterOfSettings, toStringMtd, instance);
        }

        private static Expression ConvertAuto(Expression instance)
        {
            var instanceType = instance.Type;

            if (instanceType.IsValueType)
            {
                return Call(parameterOfSettings, convertMtd, Convert(instance, typeof(object)));
            }

            return Call(parameterOfSettings, convertMtd, instance);
        }

        private class SyntaxPool
        {
            private readonly ConcurrentDictionary<string, Expression> blockCachings = new ConcurrentDictionary<string, Expression>(StringComparer.InvariantCultureIgnoreCase);
            private readonly ConcurrentDictionary<string, Func<StringSugar, object, DefaultSettings, string>> sugarCachings = new ConcurrentDictionary<string, Func<StringSugar, object, DefaultSettings, string>>(StringComparer.InvariantCultureIgnoreCase);

            private static readonly ConstructorInfo invalidOperationErrorCtor = typeof(InvalidOperationException).GetConstructor(new Type[] { typeof(string) });
            private static readonly ConstructorInfo missingMemberErrorCtor = typeof(MissingMemberException).GetConstructor(new Type[] { typeof(string), typeof(string) });
            private static readonly ConstructorInfo missingMethodErrorCtor = typeof(MissingMethodException).GetConstructor(new Type[] { typeof(string) });

            private readonly ParameterExpression instanceVariable;

            public SyntaxPool(Type sourceType)
            {
                instanceVariable = Variable(sourceType, "instance");
            }


            private Expression MakeExpression(string name) => blockCachings.GetOrAdd(name, expression =>
            {
                string[] names = expression.Split('.');

                Expression instance = instanceVariable;

                foreach (string s in names)
                {
                    if (TryGetMemberExpression(s, ref instance))
                    {
                        continue;
                    }

                    return Block(typeof(string), IfThenElse(strict, Throw(New(missingMemberErrorCtor, Constant(instance.Type.Name), Constant(expression))), Assign(undo, preserveSyntax)), Constant(null, typeof(string)));
                }

                return instance;
            });

            private Expression MakeExpression(string name, string format) => blockCachings.GetOrAdd($"{name}:{format}", expression =>
            {
                var indexOf = expression.IndexOf(':');

#if NET_Traditional

                var valueExp = MakeExpression(expression.Substring(0, indexOf));

                var formatExp = Constant(expression.Substring(indexOf + 1));
#else
                var valueExp = MakeExpression(expression[..indexOf]);

                var formatExp = Constant(expression[(indexOf + 1)..]);
#endif

                var destinationType = valueExp.Type;

                bool nullable = destinationType.IsNullable();

                if (nullable)
                {
                    destinationType = Nullable.GetUnderlyingType(destinationType)!;
                }
                else
                {
                    nullable = destinationType.IsClass;
                }

                var valueVar = Variable(valueExp.Type);

                var expressions = new List<Expression>
                {
                    Assign(instanceVariable, Convert(parameterOfSource, instanceVariable.Type)),
                    Assign(valueVar, valueExp)
                };

                if (destinationType.IsEnum)
                {
                    if (nullable)
                    {
                        expressions.Add(Condition(Equal(valueVar, Constant(null, valueVar.Type)), nullValue, Call(Convert(Convert(valueVar, destinationType), enumType), enumToStringFormatMtd, formatExp)));
                    }
                    else
                    {
                        expressions.Add(Call(Convert(valueVar, enumType), enumToStringFormatMtd, formatExp));
                    }
                }
                else
                {
                    var formatMtd = destinationType.GetMethod(nameof(ToString), new Type[] { typeof(string) });

                    if (formatMtd is null)
                    {
                        expressions.Add(IfThen(strict, Throw(New(missingMethodErrorCtor, Constant($"未找到“{destinationType.Name}.ToString(string format)”方法！")))));
                        expressions.Add(ToStringAuto(valueVar));
                    }
                    else if (nullable)
                    {
                        expressions.Add(Condition(Equal(valueVar, Constant(null, valueVar.Type)), nullValue, Call(destinationType.IsValueType ? Convert(valueVar, destinationType) : valueVar, formatMtd, formatExp)));
                    }
                    else
                    {
                        expressions.Add(Call(valueVar, formatMtd, formatExp));
                    }
                }

                return Block(new ParameterExpression[] { instanceVariable, valueVar }, expressions);
            });

            private Expression MakeExpressionByToken(Expression prevExp, string token, Expression nextExp)
            {
                var prevType = prevExp.Type;

                var nextType = nextExp.Type;

                var prevVar = Variable(prevType);

                var variables = new List<ParameterExpression>
                {
                    instanceVariable,
                    prevVar
                };
                var expressions = new List<Expression>
                {
                    Assign(instanceVariable, Convert(parameterOfSource, instanceVariable.Type)),
                    Assign(prevVar, prevExp)
                };

                if (token == "?" || token == "??" || token == "?+")
                {
                    if (prevType.IsClass || prevType.IsNullable())
                    {
                        var nextVar = Variable(nextType);

                        variables.Add(nextVar);

                        expressions.Add(Assign(nextVar, nextExp));

                        expressions.Add(token == "?+"
                            ? Condition(Equal(prevVar, Constant(null, prevType)), nullValue, ConvertAuto(MakeAdd(prevVar, nextVar)))
                            : Condition(Equal(prevVar, Constant(null, prevType)), ConvertAuto(nextVar), ConvertAuto(prevVar)));
                    }
                    else
                    {
                        expressions.Add(IfThen(strict, Throw(New(invalidOperationErrorCtor, Constant($"运算符“{token}”无法应用于“{prevType.Name}”和“{nextType.Name}”类型的操作！")))));

                        expressions.Add(ConvertAuto(prevVar));
                    }
                }
                else
                {
                    var nextVar = Variable(nextType);

                    variables.Add(nextVar);

                    expressions.Add(Assign(nextVar, nextExp));

                    expressions.Add(ConvertAuto(MakeAdd(prevVar, nextVar)));
                }

                return Block(variables, expressions);
            }

            public string GetValue(StringSugar sugar, object source, DefaultSettings settings, string pre, string token, string name, string format)
            {
                var valueGetter = sugarCachings.GetOrAdd($"{pre}-{token}-{name}:{format}", expression =>
                {
                    var indexOf = expression.IndexOf(':');

#if NET_Traditional
                    var format = expression.Substring(indexOf + 1);

                    var names = expression.Substring(0, indexOf).Split('-');
#else
                    var format = expression[(indexOf + 1)..];

                    var names = expression[..indexOf].Split('-');
#endif

                    var prevExp = MakeExpression(names[0]);

                    var token = names[1];

                    var nextExp = MakeExpression(names[2], format);

                    var lambdaExp = Lambda<Func<StringSugar, object, DefaultSettings, string>>(MakeExpressionByToken(prevExp, token, nextExp), parameterOfSugar, parameterOfSource, parameterOfSettings);

                    return lambdaExp.Compile();
                });

                return valueGetter.Invoke(sugar, source, settings);
            }

            public string GetValue(StringSugar sugar, object source, DefaultSettings settings, string pre, string token, string name)
            {
                var valueGetter = sugarCachings.GetOrAdd($"{pre}-{token}-{name}", expression =>
                {
                    var names = expression.Split('-');

                    var prevExp = MakeExpression(names[0]);

                    var token = names[1];

                    var nextExp = MakeExpression(names[2]);

                    var lambdaExp = Lambda<Func<StringSugar, object, DefaultSettings, string>>(MakeExpressionByToken(prevExp, token, nextExp), parameterOfSugar, parameterOfSource, parameterOfSettings);

                    return lambdaExp.Compile();
                });

                return valueGetter.Invoke(sugar, source, settings);
            }

            public string GetValue(StringSugar sugar, object source, DefaultSettings settings, string name, string format)
            {
                var valueGetter = sugarCachings.GetOrAdd($"{name}:{format}", _ =>
                {
                    var valueExp = MakeExpression(name, format);

                    var bodyExp = Block(new ParameterExpression[] { instanceVariable }, Assign(instanceVariable, Convert(parameterOfSource, instanceVariable.Type)), ConvertAuto(valueExp));

                    var lambdaExp = Lambda<Func<StringSugar, object, DefaultSettings, string>>(bodyExp, parameterOfSugar, parameterOfSource, parameterOfSettings);

                    return lambdaExp.Compile();
                });

                return valueGetter.Invoke(sugar, source, settings);
            }

            public string GetValue(StringSugar sugar, object source, DefaultSettings settings, string name)
            {
                var valueGetter = sugarCachings.GetOrAdd(name, _ =>
                {
                    var valueExp = MakeExpression(name);

                    var bodyExp = Block(new ParameterExpression[] { instanceVariable }, Assign(instanceVariable, Convert(parameterOfSource, instanceVariable.Type)), ConvertAuto(valueExp));

                    var lambdaExp = Lambda<Func<StringSugar, object, DefaultSettings, string>>(bodyExp, parameterOfSugar, parameterOfSource, parameterOfSettings);

                    return lambdaExp.Compile();
                });

                return valueGetter.Invoke(sugar, source, settings);
            }

            private static bool TryGetMemberExpression(string name, ref Expression instance)
            {
                var propertyInfo = instance.Type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy);

                if (propertyInfo is null)
                {
                    var fieldInfo = instance.Type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy);

                    if (fieldInfo is null)
                    {
                        return false;
                    }

                    instance = Field(instance, fieldInfo);

                    return true;
                }

                instance = Property(instance, propertyInfo);

                return true;
            }
        }

        private class StringSugar : AdapterSugar<StringSugar>
        {
            private readonly object source;
            private readonly DefaultSettings settings;
            private readonly SyntaxPool syntaxPool;

            public StringSugar(object source, DefaultSettings settings, SyntaxPool syntaxPool)
            {
                this.source = source ?? throw new ArgumentNullException(nameof(source));
                this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
                this.syntaxPool = syntaxPool ?? throw new ArgumentNullException(nameof(syntaxPool));
            }

            [Mismatch("token")] //? 不匹配 token。
            public string Single(string name, string format) => syntaxPool.GetValue(this, source, settings, name, format);

            [Mismatch("token")] //? 不匹配 token。
            public string Single(string name) => syntaxPool.GetValue(this, source, settings, name);

            public string Combination(string pre, string token, string name, string format) => syntaxPool.GetValue(this, source, settings, pre, token, name, format);

            public string Combination(string pre, string token, string name) => syntaxPool.GetValue(this, source, settings, pre, token, name);
        }
    }
}