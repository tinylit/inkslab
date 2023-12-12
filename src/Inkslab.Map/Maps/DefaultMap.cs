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
        /// <param name="sourceType"></param>
        /// <param name="destinationType"></param>
        /// <returns></returns>
        public override bool IsMatch(Type sourceType, Type destinationType)
        {
            if (destinationType.IsAbstract)
            {
                return false;
            }

            if (sourceType.IsSimple() || destinationType.IsSimple())
            {
                return false;
            }

            return true;
        }

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
            var propertyInfos = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var propertyInfo in destinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (propertyInfo.IsIgnore())
                {
                    continue;
                }

                foreach (var property in propertyInfos)
                {
                    if (property.CanRead)
                    {
                        if (string.Equals(property.Name, propertyInfo.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            var sourcePrt = Property(sourceExpression, property);
                            var destinationPrt = Property(destinationExpression, propertyInfo);

                            if (TrySolve(propertyInfo, destinationPrt, sourcePrt, application, out Expression destinationRs))
                            {
                                yield return destinationRs;
                            }

                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 解决属性赋值。
        /// </summary>
        /// <param name="propertyInfo">目标属性。</param>
        /// <param name="destinationPrt">目标属性表达式。</param>
        /// <param name="sourcePrt">数据源表达式。</param>
        /// <param name="application">映射程序。</param>
        /// <param name="destinationRs">目标表达式。</param>
        /// <returns>是否可以处理。</returns>
        protected virtual bool TrySolve(PropertyInfo propertyInfo, MemberExpression destinationPrt, Expression sourcePrt, IMapApplication application, out Expression destinationRs)
        {
            var destinationType = propertyInfo.PropertyType;

            if (propertyInfo.CanWrite)
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

                destinationRs = FinishingExpression(destinationType, destinationItemType.MakeArrayType(), destinationPrt, sourcePrt, application, customAddFn.MakeGenericMethod(destinationType, destinationItemType), true);

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

        private static readonly MethodInfo customAddFn = typeof(DefaultMap).GetMethod(nameof(Add), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

        private static void Add<TCollection, TItem>(TCollection collection, TItem[] items) where TCollection : ICollection<TItem>
        {
            for (int i = 0; i < items.Length; i++)
            {
                collection.Add(items[i]);
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
    }
}
