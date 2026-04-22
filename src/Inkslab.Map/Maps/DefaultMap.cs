using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Inkslab.Map.Maps
{
    using static Expression;

    /// <summary>
    /// 默认映射，根据属性名称（不区分大小写）匹配。
    /// </summary>
    public class DefaultMap : AbstractMap
    {
        /// <summary>
        /// 目标类型 <paramref name="destinationType"/> 不是抽象类型，且源类型 <paramref name="sourceType"/> 和目标类型 <paramref name="destinationType"/> 均不是基础类型。
        /// </summary>
        /// <param name="sourceType"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public override bool IsMatch(Type sourceType, Type destinationType)
        {
            if (destinationType.IsAbstract)
            {
                return false;
            }

            return !sourceType.IsSimple() && !destinationType.IsSimple();
        }

        /// <inheritdoc />
        public sealed override Expression ToSolve(Expression sourceExpression, Type destinationType, IMapApplication application)
            => ToSolveCore(sourceExpression, destinationType, new MapApplication(sourceExpression.Type, destinationType, application));

        /// <summary>
        /// 解决。
        /// </summary>
        /// <param name="sourceExpression">源对象表达式。</param>
        /// <param name="destinationType">目标对象表达式。</param>
        /// <param name="application">映射程序。</param>
        /// <returns>目标对象<paramref name="destinationType"/>的映射逻辑表达式。</returns>
        protected virtual Expression ToSolveCore(Expression sourceExpression, Type destinationType, IMapApplication application)
            => base.ToSolve(sourceExpression, destinationType, application);

        /// <inheritdoc/>
        protected sealed override Expression ToSolve(Expression sourceExpression, Type sourceType, ParameterExpression destinationExpression, Type destinationType, IMapApplication application)
        {
            var expressions = new List<Expression>();

            expressions.AddRange(ToSolveCore(sourceExpression, sourceType, destinationExpression, destinationType, application));

            if (expressions.Count > 0)
            {
                return Block(expressions);
            }

            return Empty();
        }

        /// <summary>
        /// 解决映射关系。
        /// </summary>
        /// <param name="sourceExpression">源对象表达式。</param>
        /// <param name="sourceType">源类型。</param>
        /// <param name="destinationExpression">目标对象表达式。</param>
        /// <param name="destinationType">目标类型。</param>
        /// <param name="application">映射配置。</param>
        /// <returns>赋值表达式迭代器。</returns>
        /// <exception cref="InvalidCastException">类型不能被转换。</exception>
        protected virtual IEnumerable<Expression> ToSolveCore(Expression sourceExpression, Type sourceType, ParameterExpression destinationExpression, Type destinationType, IMapApplication application)
        {
            //? 按名称构建源属性索引，将 O(M*N) 的属性名匹配降为 O(M+N)。
            var propertyInfos = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var sourceLookup = BuildPropertyLookup(propertyInfos);

            foreach (var propertyInfo in destinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (propertyInfo.IsIgnore())
                {
                    continue;
                }

                if (!sourceLookup.TryGetValue(propertyInfo.Name, out var memberInfo))
                {
                    continue;
                }

                if (!memberInfo.CanRead)
                {
                    continue;
                }

                var sourcePrt = Property(sourceExpression, memberInfo);
                var destinationPrt = Property(destinationExpression, propertyInfo);

                if (TrySolve(destinationPrt,
                        sourcePrt,
                        application,
                        out Expression destinationRs))
                {
                    yield return destinationRs;
                }
            }
        }

        /// <summary>
        /// 构建按名称不区分大小写索引的属性字典，供属性映射快速查找。
        /// </summary>
        /// <param name="properties">源属性集合。</param>
        /// <returns>属性名到 <see cref="PropertyInfo"/> 的字典。</returns>
        internal static Dictionary<string, PropertyInfo> BuildPropertyLookup(PropertyInfo[] properties)
        {
            var lookup = new Dictionary<string, PropertyInfo>(properties.Length, StringComparer.OrdinalIgnoreCase);

            foreach (var info in properties)
            {
                //? 同名按大小写碰撞时，保留首个可读属性，尽量选择可用字段。
                if (!lookup.ContainsKey(info.Name))
                {
                    lookup[info.Name] = info;
                }
            }

            return lookup;
        }

        /// <summary>
        /// 解决属性赋值。
        /// </summary>
        /// <param name="destinationPrt">目标属性表达式。</param>
        /// <param name="sourcePrt">数据源表达式。</param>
        /// <param name="application">映射程序。</param>
        /// <param name="destinationRs">目标表达式。</param>
        /// <returns>是否可以处理。</returns>
        protected virtual bool TrySolve(MemberExpression destinationPrt, Expression sourcePrt, IMapApplication application, out Expression destinationRs)
        {
            var destinationType = destinationPrt.Type;

            if (destinationPrt.Member is PropertyInfo { CanWrite: true } or FieldInfo { IsInitOnly: false })
            {
                destinationRs = Assign(destinationPrt, application.Map(sourcePrt, destinationType));

                return true;
            }

            destinationRs = null;

            if (!destinationType.IsGenericType)
            {
                return false;
            }

            var genericArguments = destinationType.GetGenericArguments();

            if (genericArguments.Length != 1)
            {
                return false;
            }

            var destinationItemType = genericArguments[0];

            if (destinationType.IsAbstract)
            {
                var destinationCollectionType = typeof(ICollection<>).MakeGenericType(destinationItemType);

                if (!destinationCollectionType.IsAssignableFrom(destinationType))
                {
                    return false;
                }

                destinationRs = FinishingExpression(destinationType, destinationItemType.MakeArrayType(), destinationPrt, sourcePrt, application, _customAddFn.MakeGenericMethod(destinationType, destinationItemType), true);

                return true;
            }

            var addFn = destinationType.GetMethod("Add", MapConstants.InstanceBindingFlags, null, new Type[] { destinationItemType }, null);

            if (addFn is null)
            {
                return false;
            }

            destinationRs = FinishingExpression(destinationType, destinationItemType.MakeArrayType(), destinationPrt, sourcePrt, application, addFn, false);

            return true;
        }

        #region 内嵌类。

        private class MapApplication : IMapApplication
        {
            private static readonly EnumerableMap _enumerableMap = new EnumerableMap();

            private readonly Type _sourceHostType;
            private readonly Type _destinationHostType;
            private readonly IMapApplication _application;

            public MapApplication(Type sourceHostType, Type destinationHostType, IMapApplication application)
            {
                _sourceHostType = sourceHostType;
                _destinationHostType = destinationHostType;
                _application = application;
            }

            public Expression Map(Expression sourceExpression, Type destinationType)
            {
                Type sourceType = sourceExpression.Type;

                if (IsRecursive(_sourceHostType, _destinationHostType, sourceType, destinationType))
                {
                    throw new NotSupportedException($"将类型“{_sourceHostType}”转换为“{_destinationHostType}”类型的表达式中，存在递归关系！");
                }

                if (!_enumerableMap.IsMatch(sourceType, destinationType))
                {
                    return _application.Map(sourceExpression, destinationType);
                }

                return MapConfiguration.IgnoreIfNull(_enumerableMap.ToSolve(sourceExpression, destinationType, this), NotEqual(sourceExpression, Constant(null, sourceType)));
            }

            private static bool IsRecursive(Type sourceHostType, Type destinationHostType, Type sourceType, Type destinationType)
                => sourceHostType == sourceType && destinationHostType == destinationType;
        }

        #endregion

        #region 私有方法

        private static readonly MethodInfo _customAddFn = typeof(DefaultMap).GetMethod(nameof(Add), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

        private static void Add<TCollection, TItem>(TCollection collection, TItem[] items) where TCollection : ICollection<TItem>
        {
            foreach (var item in items)
            {
                collection.Add(item);
            }
        }

        private static Expression FinishingExpression(Type destinationType, Type destinationMultiType, MemberExpression destinationPrt, Expression sourcePrt, IMapApplication application, MethodInfo addFn, bool isMulti)
        {
            var sourceEx = Variable(sourcePrt.Type, "source");

            var destinationEx = Variable(destinationType, "destination");

            var destinationArr = Variable(destinationMultiType, "destinationArr");

            var sourceTest = NotEqual(sourceEx, sourceEx.Type.IsValueType ? Default(sourceEx.Type) : Constant(null, sourceEx.Type));

            var destinationTest = NotEqual(destinationEx, destinationType.IsValueType ? Default(destinationType) : Constant(null, destinationType));

            var coreEx = isMulti
                ? Call(addFn, destinationEx, destinationArr)
                : VirtualAddRange(destinationEx, addFn, destinationArr);

            var bodyExp = Block(new ParameterExpression[] { destinationArr }, Assign(destinationArr, application.Map(sourceEx, destinationMultiType)), IfThen(AndAlso(NotEqual(destinationArr, Constant(null, destinationMultiType)), GreaterThan(ArrayLength(destinationArr), Constant(0))), coreEx));

            return Block(new ParameterExpression[] { sourceEx, destinationEx }, Assign(sourceEx, sourcePrt), Assign(destinationEx, destinationPrt), IfThen(AndAlso(sourceTest, destinationTest), bodyExp));
        }

        private static Expression VirtualAddRange(ParameterExpression destinationExpression, MethodInfo methodInfo, ParameterExpression sourceExpression)
        {
            var indexExp = Variable(typeof(int), "i");

            var lengthExp = Variable(typeof(int), "len");

            LabelTarget breakLabel = Label(MapConstants.VoidType, "label_break");
            LabelTarget continueLabel = Label(MapConstants.VoidType, "label_continue");

            return Block(new ParameterExpression[]
            {
                indexExp,
                lengthExp
            }, new Expression[]
            {
                Assign(indexExp, Constant(0)),
                Assign(lengthExp, ArrayLength(sourceExpression)),
                Loop(
                    IfThenElse(
                        GreaterThan(lengthExp, indexExp),
                        Block(
                            Call(destinationExpression, methodInfo, ArrayIndex(sourceExpression, indexExp)),
                            AddAssign(indexExp, Constant(1)),
                            Continue(continueLabel)
                        ),
                        Break(breakLabel)), // push to eax/rax --> return value
                    breakLabel, continueLabel
                )
            });
        }

        #endregion
    }
}