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
        private static readonly Type _enumType = typeof(Enum);
        private static readonly Type _stringType = typeof(string);

        private static readonly MethodInfo _enumToStringMtd;
        private static readonly MethodInfo _enumToStringFormatMtd;

        private static readonly Regex _index = new Regex("^[\\x20\\t\\r\\n\\f]*((?<len>#)|\\.\\.[\\x20\\t\\r\\n\\f]*(?<end>(0|[-]?[1-9][0-9]*))|(?<at>(0|[1-9][0-9]*))([\\x20\\t\\r\\n\\f]*(?<range>\\.\\.[\\x20\\t\\r\\n\\f]*)(?<to>(0|[-]?[1-9][0-9]*))?)?)[\\x20\\t\\r\\n\\f]*$", RegexOptions.Compiled);
        private static readonly Regex _regularExpression = new Regex("\\$\\{[\\x20\\t\\r\\n\\f]*((?<pre>[\\w\\u4e00-\\u9fa5]+(\\.[\\w\\u4e00-\\u9fa5]+)*)[\\x20\\t\\r\\n\\f]*(?<token>\\??[?+])[\\x20\\t\\r\\n\\f]*)?(?<name>[\\w\\u4e00-\\u9fa5]+(\\.[\\w\\u4e00-\\u9fa5]+)*)(:(?<format>[^\\}]+?))?[\\x20\\t\\r\\n\\f]*\\}",
            RegexOptions.Multiline);

        private static readonly HashSet<Type> _simpleTypes = new HashSet<Type>()
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

        private static readonly ConcurrentDictionary<Type, SyntaxPool> _sugarCachings = new ConcurrentDictionary<Type, SyntaxPool>();

        private static readonly ParameterExpression _parameterOfSource;
        private static readonly ParameterExpression _parameterOfSettings;
        private static readonly ParameterExpression _parameterOfSugar;

        private static readonly MemberExpression _undo;

        private static readonly MemberExpression _strict;
        private static readonly MemberExpression _nullValue;
        private static readonly MemberExpression _preserveSyntax;

        private static readonly MethodInfo _convertMtd;
        private static readonly MethodInfo _toStringMtd;

        static DefaultStringSugar()
        {
            var objectType = typeof(object);
            var sugarType = typeof(StringSugar);
            var settingsType = typeof(DefaultSettings);

            _parameterOfSource = Parameter(typeof(object), "source");
            _parameterOfSettings = Parameter(settingsType, "settings");
            _parameterOfSugar = Parameter(sugarType, "sugar");

            _undo = Property(_parameterOfSugar, nameof(StringSugar.Undo));

            _strict = Property(_parameterOfSettings, nameof(DefaultSettings.Strict));
            _nullValue = Property(_parameterOfSettings, nameof(DefaultSettings.NullValue));
            _preserveSyntax = Property(_parameterOfSettings, nameof(DefaultSettings.PreserveSyntax));

            _convertMtd = settingsType.GetMethod(nameof(DefaultSettings.Convert), new Type[] { objectType });
            _toStringMtd = settingsType.GetMethod(nameof(DefaultSettings.ToString), new Type[] { objectType });

            _enumToStringMtd = _enumType.GetMethod(nameof(ToString), Type.EmptyTypes);
            _enumToStringFormatMtd = _enumType.GetMethod(nameof(ToString), new Type[] { _stringType });
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public Regex RegularExpression => _regularExpression;

        /// <summary>
        /// 创建语法糖。
        /// </summary>
        /// <param name="source">数据源对象。</param>
        /// <param name="settings">语法糖设置。</param>
        /// <returns>处理 <see cref="RegularExpression"/> 语法的语法糖。</returns>
        public ISugar CreateSugar(object source, DefaultSettings settings) => new StringSugar(source, settings, _sugarCachings.GetOrAdd(source.GetType(), sourceType => new SyntaxPool(sourceType)));

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

            if (type == _stringType)
            {
                return Call(null, type.GetMethod(nameof(string.Concat), new Type[] { type, type })!, left, right);
            }

            return Add(left, right);
        }

        private static Expression BooleanAdd(Expression left, Expression right) => AndAlso(left, right);

        private static Expression Diff(Type leftType, Type rightType, Expression leftExp, Expression rightExp)
        {
            if (leftType == typeof(bool) && _simpleTypes.Contains(rightType))
            {
                return Same(rightType, Condition(leftExp, Constant(1, rightType), Constant(0, rightType)), rightExp);
            }

            if (rightType == typeof(bool) && _simpleTypes.Contains(leftType))
            {
                return Same(leftType, leftExp, Condition(rightExp, Constant(1, leftType), Constant(0, leftType)));
            }

            if (leftType == rightType)
            {
                return Same(rightType, leftExp, rightExp);
            }

            if (leftType == _stringType)
            {
                return Same(leftType, leftExp, Call(_parameterOfSettings, _toStringMtd, rightExp));
            }

            if (rightType == _stringType)
            {
                return Same(rightType, Call(_parameterOfSettings, _toStringMtd, leftExp), rightExp);
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

            if (instanceType == _stringType)
            {
                return instance;
            }

            if (instanceType.IsEnum)
            {
                return Call(Convert(instance, _enumType), _enumToStringMtd);
            }

            if (instanceType.IsMini())
            {
                var stringMtd = instanceType.GetMethod(nameof(ToString), Type.EmptyTypes);

                if (stringMtd is null)
                {
                    return Call(_parameterOfSettings, _toStringMtd, Convert(instance, typeof(object)));
                }

                return Call(instance, stringMtd);
            }

            if (instanceType.IsValueType)
            {
                return Call(_parameterOfSettings, _toStringMtd, Convert(instance, typeof(object)));
            }

            return Call(_parameterOfSettings, _toStringMtd, instance);
        }

        private static Expression ConvertAuto(Expression instance)
        {
            var instanceType = instance.Type;

            if (instanceType.IsValueType)
            {
                return Call(_parameterOfSettings, _convertMtd, Convert(instance, typeof(object)));
            }

            return Call(_parameterOfSettings, _convertMtd, instance);
        }

        private class SyntaxPool
        {
            private readonly ConcurrentDictionary<string, Func<StringSugar, object, DefaultSettings, string>> _sugarCachings = new ConcurrentDictionary<string, Func<StringSugar, object, DefaultSettings, string>>(StringComparer.InvariantCultureIgnoreCase);

            private static readonly MethodInfo _substringFn;

            private static readonly ConstructorInfo _invalidOperationErrorCtor;
            private static readonly ConstructorInfo _missingMemberErrorCtor;
            private static readonly ConstructorInfo _missingMethodErrorCtor;

            static SyntaxPool()
            {
                var intType = typeof(int);
                var missingMemberType = typeof(MissingMemberException);
                var missingMethodType = typeof(MissingMethodException);
                var invalidOperationType = typeof(InvalidOperationException);

                _substringFn = _stringType.GetMethod(nameof(string.Substring), new Type[] { intType, intType });

                _invalidOperationErrorCtor = invalidOperationType.GetConstructor(new Type[] { _stringType });
                _missingMemberErrorCtor = missingMemberType.GetConstructor(new Type[] { _stringType, _stringType });
                _missingMethodErrorCtor = missingMethodType.GetConstructor(new Type[] { _stringType });
            }

            private readonly ParameterExpression _instanceVariable;

            public SyntaxPool(Type sourceType)
            {
                _instanceVariable = Variable(sourceType, "instance");
            }


            private Expression MakeExpression(string name)
            {
                string[] names = name.Split('.');

                Expression instance = _instanceVariable;

                foreach (string s in names)
                {
                    if (TryGetMemberExpression(s, ref instance))
                    {
                        continue;
                    }

                    return Block(_stringType, IfThenElse(_strict, Throw(New(_missingMemberErrorCtor, Constant(instance.Type.Name), Constant(name))), Assign(_undo, _preserveSyntax)), Constant(null, _stringType));
                }

                return instance;
            }

            private Expression MakeExpression(string name, string format)
            {
                var valueExp = MakeExpression(name);

                var formatExp = Constant(format);

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
                    Assign(_instanceVariable, Convert(_parameterOfSource, _instanceVariable.Type)),
                    Assign(valueVar, valueExp)
                };

                if (destinationType.IsEnum)
                {
                    if (nullable)
                    {
                        expressions.Add(Condition(Equal(valueVar, Constant(null, valueVar.Type)), _nullValue, Call(Convert(Convert(valueVar, destinationType), _enumType), _enumToStringFormatMtd, formatExp)));
                    }
                    else
                    {
                        expressions.Add(Call(Convert(valueVar, _enumType), _enumToStringFormatMtd, formatExp));
                    }
                }
                else if (destinationType == _stringType)
                {
                    var mt = _index.Match(format);

                    if (mt.Success) //? # / ..5 / 0.. / 0..5
                    {
                        if (mt.Groups["len"].Success) //? #
                        {
                            expressions.Add(Property(valueVar, "Length"));
                        }
                        else //? 0..5 ..5 0..
                        {
                            var endGrp = mt.Groups["end"];
                            var rangeGrp = mt.Groups["range"];

                            if (endGrp.Success || rangeGrp.Success)
                            {
                                int fromStart = 0;
                                int toEnd = -1;
                                bool isFromEnd = false;

                                if (isFromEnd = endGrp.Success)
                                {
                                    toEnd = int.Parse(endGrp.Value);
                                }
                                else
                                {
                                    var atGrp = mt.Groups["at"];
                                    var toGrp = mt.Groups["to"];

                                    if (atGrp.Success)
                                    {
                                        fromStart = int.Parse(atGrp.Value);
                                    }

                                    if (isFromEnd = toGrp.Success)
                                    {
                                        toEnd = int.Parse(toGrp.Value);
                                    }
                                }

                                if (isFromEnd)
                                {
                                    if (toEnd > 0)
                                    {
                                        expressions.Add(Call(valueVar, _substringFn, Constant(fromStart), Constant(toEnd - fromStart)));
                                    }
                                    else
                                    {
                                        expressions.Add(Call(valueVar, _substringFn, Constant(fromStart), AddChecked(Property(valueVar, "Length"), Constant(toEnd - fromStart))));
                                    }
                                }
                                else
                                {
                                    expressions.Add(Call(valueVar, _substringFn, Constant(fromStart), SubtractChecked(Property(valueVar, "Length"), Constant(fromStart))));
                                }
                            }
                        }
                    }
                    else
                    {
                        expressions.Add(IfThen(_strict, Throw(New(_invalidOperationErrorCtor, Constant($"字符串的格式化值“{format}”暂不支持！")))));
                        expressions.Add(valueVar);
                    }
                }
                else
                {
                    var formatMtd = destinationType.GetMethod(nameof(ToString), new Type[] { _stringType });

                    if (formatMtd is null)
                    {
                        expressions.Add(IfThen(_strict, Throw(New(_missingMethodErrorCtor, Constant($"未找到“{destinationType.Name}.ToString(string format)”方法！")))));
                        expressions.Add(ToStringAuto(valueVar));
                    }
                    else if (nullable)
                    {
                        expressions.Add(Condition(Equal(valueVar, Constant(null, valueVar.Type)), _nullValue, Call(destinationType.IsValueType ? Convert(valueVar, destinationType) : valueVar, formatMtd, formatExp)));
                    }
                    else
                    {
                        expressions.Add(Call(valueVar, formatMtd, formatExp));
                    }
                }

                return Block(new ParameterExpression[] { _instanceVariable, valueVar }, expressions);
            }

            private Expression MakeExpressionByToken(Expression prevExp, string token, Expression nextExp)
            {
                var prevType = prevExp.Type;

                var nextType = nextExp.Type;

                var prevVar = Variable(prevType);

                var variables = new List<ParameterExpression>
                {
                    _instanceVariable,
                    prevVar
                };
                var expressions = new List<Expression>
                {
                    Assign(_instanceVariable, Convert(_parameterOfSource, _instanceVariable.Type)),
                    Assign(prevVar, prevExp)
                };

                if (token is "?" or "??" or "?+")
                {
                    if (prevType.IsClass || prevType.IsNullable())
                    {
                        var nextVar = Variable(nextType);

                        variables.Add(nextVar);

                        expressions.Add(Assign(nextVar, nextExp));

                        expressions.Add(token == "?+"
                            ? Condition(Equal(prevVar, Constant(null, prevType)), _nullValue, ConvertAuto(MakeAdd(prevVar, nextVar)))
                            : Condition(Equal(prevVar, Constant(null, prevType)), ConvertAuto(nextVar), ConvertAuto(prevVar)));
                    }
                    else
                    {
                        expressions.Add(IfThen(_strict, Throw(New(_invalidOperationErrorCtor, Constant($"运算符“{token}”无法应用于“{prevType.Name}”和“{nextType.Name}”类型的操作！")))));

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
                var valueGetter = _sugarCachings.GetOrAdd($"{pre}-{token}-{name}:{format}", expression =>
                {
                    var prevExp = MakeExpression(pre);

                    var nextExp = MakeExpression(name, format);

                    var lambdaExp = Lambda<Func<StringSugar, object, DefaultSettings, string>>(MakeExpressionByToken(prevExp, token, nextExp), _parameterOfSugar, _parameterOfSource, _parameterOfSettings);

                    return lambdaExp.Compile();
                });

                return valueGetter.Invoke(sugar, source, settings);
            }

            public string GetValue(StringSugar sugar, object source, DefaultSettings settings, string pre, string token, string name)
            {
                var valueGetter = _sugarCachings.GetOrAdd($"{pre}-{token}-{name}", expression =>
                {
                    var prevExp = MakeExpression(pre);

                    var nextExp = MakeExpression(name);

                    var lambdaExp = Lambda<Func<StringSugar, object, DefaultSettings, string>>(MakeExpressionByToken(prevExp, token, nextExp), _parameterOfSugar, _parameterOfSource, _parameterOfSettings);

                    return lambdaExp.Compile();
                });

                return valueGetter.Invoke(sugar, source, settings);
            }

            public string GetValue(StringSugar sugar, object source, DefaultSettings settings, string name, string format)
            {
                var valueGetter = _sugarCachings.GetOrAdd($"{name}:{format}", _ =>
                {
                    var valueExp = MakeExpression(name, format);

                    var bodyExp = Block(new ParameterExpression[] { _instanceVariable }, Assign(_instanceVariable, Convert(_parameterOfSource, _instanceVariable.Type)), ConvertAuto(valueExp));

                    var lambdaExp = Lambda<Func<StringSugar, object, DefaultSettings, string>>(bodyExp, _parameterOfSugar, _parameterOfSource, _parameterOfSettings);

                    return lambdaExp.Compile();
                });

                return valueGetter.Invoke(sugar, source, settings);
            }

            public string GetValue(StringSugar sugar, object source, DefaultSettings settings, string name)
            {
                var valueGetter = _sugarCachings.GetOrAdd(name, _ =>
                {
                    var valueExp = MakeExpression(name);

                    var bodyExp = Block(new ParameterExpression[] { _instanceVariable }, Assign(_instanceVariable, Convert(_parameterOfSource, _instanceVariable.Type)), ConvertAuto(valueExp));

                    var lambdaExp = Lambda<Func<StringSugar, object, DefaultSettings, string>>(bodyExp, _parameterOfSugar, _parameterOfSource, _parameterOfSettings);

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
            private readonly object _source;
            private readonly DefaultSettings _settings;
            private readonly SyntaxPool _syntaxPool;

            public StringSugar(object source, DefaultSettings settings, SyntaxPool syntaxPool)
            {
                _source = source ?? throw new ArgumentNullException(nameof(source));
                _settings = settings ?? throw new ArgumentNullException(nameof(settings));
                _syntaxPool = syntaxPool ?? throw new ArgumentNullException(nameof(syntaxPool));
            }

            [Mismatch("token")] //? 不匹配 token。
            public string Single(string name, string format) => _syntaxPool.GetValue(this, _source, _settings, name, format);

            [Mismatch("token")] //? 不匹配 token。
            public string Single(string name) => _syntaxPool.GetValue(this, _source, _settings, name);

            public string Combination(string pre, string token, string name, string format) => _syntaxPool.GetValue(this, _source, _settings, pre, token, name, format);

            public string Combination(string pre, string token, string name) => _syntaxPool.GetValue(this, _source, _settings, pre, token, name);
        }
    }
}