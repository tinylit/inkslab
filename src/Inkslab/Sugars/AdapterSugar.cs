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
    /// 反射信息缓存，所有泛型类型共享
    /// </summary>
    internal static class ReflectionCache
    {
        public static readonly PropertyInfo CaptureValueProp = typeof(Capture).GetProperty("Value")!;
        public static readonly PropertyInfo MatchGroupsProp = typeof(Match).GetProperty("Groups")!;
        public static readonly PropertyInfo GroupSuccessProp = typeof(Group).GetProperty("Success")!;
        public static readonly PropertyInfo GroupCapturesProp = typeof(Group).GetProperty("Captures")!;
        public static readonly MethodInfo GroupCollectionItemMtd = typeof(GroupCollection).GetMethod("get_Item", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, new Type[] { typeof(string) }, null)!;
        public static readonly MethodInfo CaptureCollectionItemMtd = typeof(CaptureCollection).GetMethod("get_Item", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly, null, new Type[] { typeof(int) }, null)!;
        public static readonly PropertyInfo CaptureCollectionCountProp = typeof(CaptureCollection).GetProperty("Count")!;

        // 类型缓存
        public static readonly Type BoolType = typeof(bool);
        public static readonly Type IntType = typeof(int);
        public static readonly Type StringType = typeof(string);
        public static readonly Type MatchType = typeof(Match);
        public static readonly Type GroupType = typeof(Group);
        public static readonly Type GroupCollectionType = typeof(GroupCollection);
        public static readonly Type CaptureType = typeof(Capture);
        public static readonly Type CaptureCollectionType = typeof(CaptureCollection);
    }

    /// <summary>
    /// 构建适配器所需的反射信息
    /// </summary>
    internal readonly struct ReflectionContext
    {
        public ReflectionContext(
            Type matchType,
            Type groupCollectionType,
            Type groupType,
            Type boolType,
            Type stringType,
            Type captureCollectionType,
            Type captureType,
            Type intType,
            PropertyInfo matchGroupsProp,
            MethodInfo groupCollectionItemMtd,
            PropertyInfo groupSuccessProp,
            PropertyInfo captureValueProp,
            PropertyInfo groupCapturesProp,
            PropertyInfo captureCollectionCountProp,
            MethodInfo captureCollectionItemMtd)
        {
            MatchType = matchType;
            GroupCollectionType = groupCollectionType;
            GroupType = groupType;
            BoolType = boolType;
            StringType = stringType;
            CaptureCollectionType = captureCollectionType;
            CaptureType = captureType;
            IntType = intType;
            MatchGroupsProp = matchGroupsProp;
            GroupCollectionItemMtd = groupCollectionItemMtd;
            GroupSuccessProp = groupSuccessProp;
            CaptureValueProp = captureValueProp;
            GroupCapturesProp = groupCapturesProp;
            CaptureCollectionCountProp = captureCollectionCountProp;
            CaptureCollectionItemMtd = captureCollectionItemMtd;
        }

        public readonly Type MatchType;
        public readonly Type GroupCollectionType;
        public readonly Type GroupType;
        public readonly Type BoolType;
        public readonly Type StringType;
        public readonly Type CaptureCollectionType;
        public readonly Type CaptureType;
        public readonly Type IntType;
        public readonly PropertyInfo MatchGroupsProp;
        public readonly MethodInfo GroupCollectionItemMtd;
        public readonly PropertyInfo GroupSuccessProp;
        public readonly PropertyInfo CaptureValueProp;
        public readonly PropertyInfo GroupCapturesProp;
        public readonly PropertyInfo CaptureCollectionCountProp;
        public readonly MethodInfo CaptureCollectionItemMtd;

        /// <summary>
        /// 从反射缓存创建上下文
        /// </summary>
        public static ReflectionContext FromCache()
        {
            return new ReflectionContext(
                ReflectionCache.MatchType,
                ReflectionCache.GroupCollectionType,
                ReflectionCache.GroupType,
                ReflectionCache.BoolType,
                ReflectionCache.StringType,
                ReflectionCache.CaptureCollectionType,
                ReflectionCache.CaptureType,
                ReflectionCache.IntType,
                ReflectionCache.MatchGroupsProp,
                ReflectionCache.GroupCollectionItemMtd,
                ReflectionCache.GroupSuccessProp,
                ReflectionCache.CaptureValueProp,
                ReflectionCache.GroupCapturesProp,
                ReflectionCache.CaptureCollectionCountProp,
                ReflectionCache.CaptureCollectionItemMtd);
        }
    }

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

        private static readonly Adapter[] _adapterCachings;

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        static AdapterSugar()
        {
            var contextType = typeof(T);
            var reflectionContext = ReflectionContext.FromCache();
            var parameterExp = Parameter(reflectionContext.MatchType, "item");
            var contextExp = Parameter(contextType, "context");

            var methodInfos = contextType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            
            // 预分配容量并使用数组以提高性能
            var adapters = new List<Adapter>(methodInfos.Length);

            foreach (var methodInfo in methodInfos)
            {
                var parameterInfos = methodInfo.GetParameters();

                if (parameterInfos.Length == 0 || methodInfo.ReturnType != reflectionContext.StringType)
                {
                    continue;
                }

                var adapter = BuildAdapter(methodInfo, parameterInfos, parameterExp, contextExp, reflectionContext);
                
                adapters.Add(adapter);
            }

            _adapterCachings = adapters.ToArray(); // 转换为数组以提高遍历性能
        }

        private static Adapter BuildAdapter(
            MethodInfo methodInfo, 
            ParameterInfo[] parameterInfos, 
            ParameterExpression parameterExp, 
            ParameterExpression contextExp,
            ReflectionContext context)
        {
            var conditions = new List<Expression>();
            var variables = new List<ParameterExpression>();
            var arguments = new List<Expression>();
            var expressions = new List<Expression>();

            foreach (var parameterInfo in parameterInfos)
            {
                if (parameterInfo.ParameterType == context.MatchType)
                {
                    arguments.Add(parameterExp);
                    continue;
                }

                if (parameterInfo.ParameterType == context.GroupCollectionType)
                {
                    arguments.Add(Property(parameterExp, context.MatchGroupsProp));
                    continue;
                }

                var groupExp = Variable(context.GroupType, parameterInfo.Name);
                variables.Add(groupExp);

                var matchAttr = parameterInfo.GetCustomAttribute<MatchAttribute>(false);
                var groupName = matchAttr?.Name ?? parameterInfo.Name;
                
                expressions.Add(Assign(groupExp, Call(Property(parameterExp, context.MatchGroupsProp), context.GroupCollectionItemMtd, Constant(groupName))));

                if (parameterInfo.ParameterType == context.GroupType)
                {
                    conditions.Add(Property(groupExp, context.GroupSuccessProp));
                    arguments.Add(groupExp);
                    continue;
                }

                if (parameterInfo.ParameterType == context.BoolType)
                {
                    arguments.Add(Property(groupExp, context.GroupSuccessProp));
                    continue;
                }

                if (parameterInfo.ParameterType == context.StringType)
                {
                    conditions.Add(Property(groupExp, context.GroupSuccessProp));
                    arguments.Add(Property(groupExp, context.CaptureValueProp));
                    continue;
                }

                var capturesExp = Variable(context.CaptureCollectionType, $"{parameterInfo.Name}Captures");
                variables.Add(capturesExp);
                expressions.Add(Assign(capturesExp, Property(groupExp, context.GroupCapturesProp)));

                if (parameterInfo.ParameterType == context.CaptureCollectionType)
                {
                    conditions.Add(Property(groupExp, context.GroupSuccessProp));
                    arguments.Add(capturesExp);
                    continue;
                }

                if (parameterInfo.ParameterType == context.CaptureType)
                {
                    conditions.Add(AndAlso(Property(groupExp, context.GroupSuccessProp), Equal(Property(capturesExp, context.CaptureCollectionCountProp), Constant(1, context.IntType))));
                    arguments.Add(Call(capturesExp, context.CaptureCollectionItemMtd, Constant(0, context.IntType)));
                    continue;
                }

                throw new NotSupportedException($"仅支持类型：{context.MatchType}、{context.GroupCollectionType}、{context.GroupType}、{context.CaptureCollectionType}、{context.CaptureType}、{context.StringType}、{context.BoolType}类型的处理。");
            }

            // 处理 MismatchAttribute
            foreach (var attr in methodInfo.GetCustomAttributes<MismatchAttribute>(true))
            {
                conditions.Add(Not(Property(Call(Property(parameterExp, context.MatchGroupsProp), context.GroupCollectionItemMtd, Constant(attr.Name)), context.GroupSuccessProp)));
            }

            Func<Match, bool> canConvertFn;

            if (conditions.Count > 0)
            {
                var condition = conditions[0];
                for (int i = 1; i < conditions.Count; i++)
                {
                    condition = AndAlso(condition, conditions[i]);
                }

                var conditionExpressions = new Expression[expressions.Count + 1];
                expressions.CopyTo(conditionExpressions, 0);
                conditionExpressions[expressions.Count] = condition;

                var invoke = Lambda<Func<Match, bool>>(Block(variables, conditionExpressions), parameterExp);
                canConvertFn = invoke.Compile();
            }
            else
            {
                canConvertFn = static _ => true; // 使用静态lambda提高性能
            }

            expressions.Add(Call(contextExp, methodInfo, arguments));
            var bodyExp = Block(variables, expressions);
            var lambdaExp = Lambda<Func<T, Match, string>>(bodyExp, contextExp, parameterExp);
            var convertFn = lambdaExp.Compile();

            return new Adapter(canConvertFn, convertFn);
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

            // 使用 for 循环遍历数组，比 foreach 性能更好
            for (int i = 0; i < _adapterCachings.Length; i++)
            {
                var adapter = _adapterCachings[i];
                if (adapter.CanConvert(match))
                {
                    try
                    {
                        string value = adapter.Convert((T)this, match);

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
