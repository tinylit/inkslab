using Inkslab.Annotations;
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
    /// 语法糖适配器。
    /// </summary>
    public abstract class AdapterSugar<T> : ISugar where T : AdapterSugar<T>, ISugar
    {
        /// <summary>
        /// MVC。
        /// </summary>
        private class Adapter
        {
            public Adapter(Func<Match, bool> canConvert, Func<T, Match, string> convert)
            {
                CanConvert = canConvert;
                Convert = convert;
            }

            /// <summary>
            /// 能否解决。
            /// </summary>
            public Func<Match, bool> CanConvert { get; }

            /// <summary>
            /// 解决方案。
            /// </summary>
            public Func<T, Match, string> Convert { get; }
        }

        private static readonly List<Adapter> _adapterCachings;

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        static AdapterSugar()
        {
            var contextType = typeof(T);

            var boolType = typeof(bool);
            var intType = typeof(int);
            var stringType = typeof(string);
            var matchType = typeof(Match);
            var groupType = typeof(Group);
            var groupCollectionType = typeof(GroupCollection);
            var captureType = typeof(Capture);
            var captureCollectionType = typeof(CaptureCollection);

            var captureValueProp = captureType.GetProperty("Value");
            var matchGroupsProp = matchType.GetProperty("Groups");
            var groupSuccessProp = groupType.GetProperty("Success");
            var groupCapturesProp = groupType.GetProperty("Captures");

            var groupCollectionItemMtd = groupCollectionType.GetMethod("get_Item", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, new Type[] { stringType }, null);
            var captureCollectionItemMtd = captureCollectionType.GetMethod("get_Item", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, new Type[] { intType }, null);
            var captureCollectionCountProp = captureCollectionType.GetProperty("Count");

            var parameterExp = Parameter(matchType, "item");
            var contextExp = Parameter(contextType, "context");

            var methodInfos = contextType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            _adapterCachings = new List<Adapter>(methodInfos.Length);

            foreach (var methodInfo in methodInfos)
            {
                var parameterInfos = methodInfo.GetParameters();

                if (parameterInfos.Length == 0 || methodInfo.ReturnType != stringType)
                {
                    continue;
                }

                var conditions = new List<Expression>();
                var variables = new List<ParameterExpression>();
                var arguments = new List<Expression>();
                var expressions = new List<Expression>();

                foreach (var parameterInfo in parameterInfos)
                {
                    if (parameterInfo.ParameterType == matchType)
                    {
                        arguments.Add(parameterExp);

                        continue;
                    }

                    if (parameterInfo.ParameterType == groupCollectionType)
                    {
                        arguments.Add(Property(parameterExp, matchGroupsProp));

                        continue;
                    }

                    var groupExp = Variable(groupType, parameterInfo.Name);

                    variables.Add(groupExp);

                    var matchAttr = parameterInfo.GetCustomAttribute<MatchAttribute>(false);

                    expressions.Add(Assign(groupExp, Call(Property(parameterExp, matchGroupsProp), groupCollectionItemMtd, Constant(matchAttr is null ? parameterInfo.Name : matchAttr.Name))));

                    if (parameterInfo.ParameterType == groupType)
                    {
                        conditions.Add(Property(groupExp, groupSuccessProp));

                        arguments.Add(groupExp);

                        continue;
                    }

                    if (parameterInfo.ParameterType == boolType)
                    {
                        arguments.Add(Property(groupExp, groupSuccessProp));

                        continue;
                    }

                    if (parameterInfo.ParameterType == stringType)
                    {
                        conditions.Add(Property(groupExp, groupSuccessProp));

                        arguments.Add(Property(groupExp, captureValueProp));

                        continue;
                    }

                    var capturesExp = Variable(captureCollectionType, $"{parameterInfo.Name}Captures");

                    variables.Add(capturesExp);

                    expressions.Add(Assign(capturesExp, Property(groupExp, groupCapturesProp)));

                    if (parameterInfo.ParameterType == captureCollectionType)
                    {
                        conditions.Add(Property(groupExp, groupSuccessProp));

                        arguments.Add(capturesExp);

                        continue;
                    }

                    if (parameterInfo.ParameterType == captureType)
                    {
                        conditions.Add(AndAlso(Property(groupExp, groupSuccessProp), Equal(Property(capturesExp, captureCollectionCountProp), Constant(1, intType))));

                        arguments.Add(Call(capturesExp, captureCollectionItemMtd, Constant(0, intType)));

                        continue;
                    }

                    throw new NotSupportedException($"仅支持类型：{matchType}、{groupCollectionType}、{groupType}、{captureCollectionType}、{captureType}、{stringType}、{boolType}类型的处理。");
                }

                foreach (var attr in methodInfo.GetCustomAttributes<MismatchAttribute>(true))
                {
                    conditions.Add(Not(Property(Call(Property(parameterExp, matchGroupsProp), groupCollectionItemMtd, Constant(attr.Name)), groupSuccessProp)));
                }

                Func<Match, bool> canConvertFn;

                var enumerator = conditions.GetEnumerator();

                if (enumerator.MoveNext())
                {
                    var condition = enumerator.Current;

                    while (enumerator.MoveNext())
                    {
                        condition = AndAlso(condition, enumerator.Current);
                    }

                    var invoke = Lambda<Func<Match, bool>>(Block(variables, expressions.Concat(new Expression[1] { condition })), parameterExp);

                    canConvertFn = invoke.Compile();
                }
                else
                {
                    canConvertFn = _ => true;
                }

                expressions.Add(Call(contextExp, methodInfo, arguments));

                var bodyExp = Block(variables, expressions);

                var lambdaExp = Lambda<Func<T, Match, string>>(bodyExp, contextExp, parameterExp);

                var convertFn = lambdaExp.Compile();

                _adapterCachings.Add(new Adapter(canConvertFn, convertFn));
            }
        }

        /// <summary>
        /// 撤销。
        /// </summary>
        public bool Undo { get; set; }

        /// <summary>
        /// 格式化。
        /// </summary>
        /// <param name="match"></param>
        /// <returns></returns>
        public string Format(Match match)
        {
            if (Undo)
            {
                return match.Value;
            }

            foreach (Adapter mvc in _adapterCachings)
            {
                if (mvc.CanConvert(match))
                {
                    try
                    {
                        string value = mvc.Convert((T)this, match);

                        return Undo ? match.Value : value;
                    }
                    finally
                    {
                        Undo = false;
                    }
                }
            }

            return match.Value;
        }
    }
}
