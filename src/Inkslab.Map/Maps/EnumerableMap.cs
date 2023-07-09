using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using static System.Linq.Expressions.Expression;

namespace Inkslab.Map.Maps
{
    /// <summary>
    /// 迭代器。
    /// </summary>
    public class EnumerableMap : AbstractMap, IMap
    {
        private readonly static MethodInfo GetEnumeratorMtd = MapConstants.EnumerableType.GetMethod("GetEnumerator", Type.EmptyTypes);

        private static readonly PropertyInfo EnumeratorCurrentProp = MapConstants.EnumeratorType.GetProperty(nameof(IEnumerator.Current));

        /// <summary>
        /// 解决迭代器类型（<see cref="IEnumerable"/>）之间的转换。
        /// </summary>
        /// <param name="sourceType"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public override bool IsMatch(Type sourceType, Type destinationType)
            => !sourceType.IsPrimitive && !destinationType.IsPrimitive
                && sourceType != MapConstants.StirngType
                && destinationType != MapConstants.StirngType
                && (sourceType.IsArray || MapConstants.EnumerableType.IsAssignableFrom(sourceType))
                && (destinationType.IsArray || MapConstants.EnumerableType.IsAssignableFrom(destinationType));

        /// <inheritdoc/>
        public override Expression ToSolve(Expression sourceExpression, Type sourceType, Type destinationType, IMapApplication application)
        {
            if (destinationType.IsArray)
            {
                if (destinationType.GetArrayRank() > 1)
                {
                    throw new InvalidCastException($"暂不支持映射到多维数组({destinationType})!");
                }
            }

            if (sourceType.IsArray)
            {
                if (sourceType.GetArrayRank() > 1)
                {
                    throw new InvalidCastException($"暂不支持多维数组({sourceType})的映射!");
                }
            }

            if (sourceType.IsArray && destinationType.IsArray)
            {
                return ArrayToArray(sourceExpression, destinationType, application);
            }

            if (destinationType.IsArray)
            {
                return ToArray(sourceExpression, destinationType.GetElementType(), application);
            }

            return base.ToSolve(sourceExpression, sourceType, destinationType, application);
        }

        private static bool TryGet(Type destinationType, out Type elementType, out MethodInfo addElementMtd)
        {
            foreach (var interfaceType in destinationType.GetInterfaces())
            {
                if (interfaceType.IsGenericType)
                {
                    var typeDefinition = interfaceType.GetGenericTypeDefinition();

                    if (typeDefinition == typeof(IList<>)
                        || typeDefinition == typeof(IReadOnlyList<>)
                        || typeDefinition == typeof(ICollection<>)
                        || typeDefinition == typeof(IReadOnlyCollection<>)
                        || typeDefinition == typeof(IEnumerable<>))
                    {
                        elementType = interfaceType.GetGenericArguments()[0];

                        addElementMtd = interfaceType.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { elementType }, null);

                        if (addElementMtd is null)
                        {
                            continue;
                        }

                        return true;
                    }
                }
            }

            elementType = MapConstants.ObjectType;

            addElementMtd = destinationType.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { elementType }, null);

            return addElementMtd is not null;
        }

        private static Expression ArrayToArray(Expression sourceExpression, Type destinationType, IMapApplication application)
        {
            var indexExp = Variable(typeof(int));

            var lengthExp = Variable(typeof(int));

            var elementType = destinationType.GetElementType();

            var destinationListType = typeof(List<>).MakeGenericType(elementType);

            var addElementMtd = destinationListType.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { elementType }, null);

            var variableExp = Variable(destinationListType);

            LabelTarget break_label = Label(MapConstants.VoidType);
            LabelTarget continue_label = Label(MapConstants.VoidType);

            return Block(new ParameterExpression[]
              {
                indexExp,
                lengthExp
              }, new Expression[]
              {
                Assign(indexExp, Constant(0)),
                Assign(lengthExp, ArrayLength(sourceExpression)),
                Assign(variableExp, New(destinationListType.GetConstructor(new Type[]{ typeof(int) }), lengthExp)),
                Loop(
                    IfThenElse(
                        GreaterThan(lengthExp, indexExp),
                        Block(
                            Call(variableExp, addElementMtd, application.Map(ArrayIndex(sourceExpression, indexExp), elementType)),
                            AddAssign(indexExp, Constant(1)),
                            Continue(continue_label)
                        ),
                        Break(break_label)), // push to eax/rax --> return value
                    break_label, continue_label
                ),
                Call(MapConstants.ToArrayMtd.MakeGenericMethod(elementType), variableExp)
              });
        }

        private static Expression ToArray(Expression sourceExpression, Type destinationElementType, IMapApplication application)
        {
            var sourceType = sourceExpression.Type;

            foreach (var interfaceType in sourceType.GetInterfaces())
            {
                if (interfaceType.IsGenericType)
                {
                    var typeDefinition = interfaceType.GetGenericTypeDefinition();

                    if (typeDefinition == typeof(IList<>)
                        || typeDefinition == typeof(IReadOnlyList<>)
                        || typeDefinition == typeof(ICollection<>)
                        || typeDefinition == typeof(IReadOnlyCollection<>)
                        || typeDefinition == typeof(IEnumerable<>))
                    {
                        return ToArrayByGeneric(sourceExpression, interfaceType.GetGenericArguments()[0], destinationElementType, application);
                    }
                }
            }

            return ToArrayByGeneral(sourceExpression, destinationElementType, application);
        }

        private static Expression ToArrayByGeneral(Expression sourceExpression, Type destinationElementType, IMapApplication configuration)
        {
            LabelTarget break_label = Label(MapConstants.VoidType);
            LabelTarget continue_label = Label(MapConstants.VoidType);

            var conversionType = typeof(List<>).MakeGenericType(destinationElementType);

            var addElementMtd = conversionType.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { destinationElementType }, null);

            var variableExp = Variable(conversionType);
            var enumeratorExp = Variable(MapConstants.EnumeratorType);

            return Block(new ParameterExpression[]
             {
                variableExp,
                enumeratorExp
             }, new Expression[]
             {
                Assign(variableExp, New(conversionType)),
                Assign(enumeratorExp, Call(sourceExpression, GetEnumeratorMtd)),
                Loop(
                    IfThenElse(
                        Call(enumeratorExp, MapConstants.MoveNextMtd),
                        Block(
                            MapConstants.VoidType,
                            Call(variableExp, addElementMtd, configuration.Map(Property(enumeratorExp, EnumeratorCurrentProp), destinationElementType)),
                            Continue(continue_label)
                        ),
                        Break(break_label)), // push to eax/rax --> return value
                    break_label, continue_label
                 ),
                 Call(MapConstants.ToArrayMtd.MakeGenericMethod(destinationElementType), variableExp)
             });
        }

        private static Expression ToArrayByGeneric(Expression sourceExpression, Type sourceElementType, Type destinationElementType, IMapApplication configuration)
        {
            LabelTarget break_label = Label(MapConstants.VoidType);
            LabelTarget continue_label = Label(MapConstants.VoidType);

            var conversionType = typeof(List<>).MakeGenericType(destinationElementType);

            var enumerableType = MapConstants.Enumerable_T_Type.MakeGenericType(sourceElementType);
            var enumeratorType = MapConstants.Enumerator_T_Type.MakeGenericType(sourceElementType);

            var propertyInfo = enumeratorType.GetProperty("Current");

            var addElementMtd = conversionType.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public, null, new Type[] { destinationElementType }, null);

            var variableExp = Variable(conversionType);
            var enumeratorExp = Variable(enumeratorType);

            return Block(new ParameterExpression[]
             {
                variableExp,
                enumeratorExp
             }, new Expression[]
             {
                Assign(variableExp, New(conversionType)),
                Assign(enumeratorExp, Call(sourceExpression, enumerableType.GetMethod("GetEnumerator", Type.EmptyTypes))),
                Loop(
                    IfThenElse(
                        Call(enumeratorExp, MapConstants.MoveNextMtd),
                        Block(
                            Call(variableExp, addElementMtd, configuration.Map(Property(enumeratorExp, propertyInfo), destinationElementType)),
                            Continue(continue_label)
                        ),
                        Break(break_label)), // push to eax/rax --> return value
                    break_label, continue_label
                 ),
                 Call(MapConstants.ToArrayMtd.MakeGenericMethod(destinationElementType), variableExp)
             });
        }

        private static Expression ArrayTo(Expression sourceExpression, ParameterExpression destinationExpression, Type elementType, MethodInfo addElementMtd, IMapApplication configuration)
        {
            var indexExp = Variable(typeof(int));

            var lengthExp = Variable(typeof(int));

            LabelTarget break_label = Label(MapConstants.VoidType);
            LabelTarget continue_label = Label(MapConstants.VoidType);

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
                            Call(destinationExpression, addElementMtd, configuration.Map(ArrayIndex(sourceExpression, indexExp), elementType)),
                            AddAssign(indexExp, Constant(1)),
                            Continue(continue_label)
                        ),
                        Break(break_label)), // push to eax/rax --> return value
                    break_label, continue_label
                )
              });
        }

        private static Expression ToEnumerable(Expression sourceExpression, Type sourceType, ParameterExpression destinationExpression, Type destinationElementType, MethodInfo addElementMtd, IMapApplication configuration)
        {
            foreach (var interfaceType in sourceType.GetInterfaces())
            {
                if (interfaceType.IsGenericType)
                {
                    var typeDefinition = interfaceType.GetGenericTypeDefinition();

                    if (typeDefinition == typeof(IList<>)
                        || typeDefinition == typeof(IReadOnlyList<>)
                        || typeDefinition == typeof(ICollection<>)
                        || typeDefinition == typeof(IReadOnlyCollection<>)
                        || typeDefinition == typeof(IEnumerable<>))
                    {
                        return ToEnumerableByGeneric(sourceExpression, interfaceType.GetGenericArguments()[0], destinationExpression, destinationElementType, addElementMtd, configuration);
                    }
                }
            }

            return ToEnumerableByGeneral(sourceExpression, destinationExpression, destinationElementType, addElementMtd, configuration);
        }

        private static Expression ToEnumerableByGeneral(Expression sourceExpression, ParameterExpression destinationExpression, Type destinationElementType, MethodInfo addElementMtd, IMapApplication configuration)
        {
            LabelTarget break_label = Label(MapConstants.VoidType);
            LabelTarget continue_label = Label(MapConstants.VoidType);

            var enumeratorExp = Variable(MapConstants.EnumeratorType);

            return Block(new ParameterExpression[]
             {
                enumeratorExp
             }, new Expression[]
             {
                Assign(enumeratorExp, Call(sourceExpression, GetEnumeratorMtd)),
                Loop(
                    IfThenElse(
                        Call(enumeratorExp, MapConstants.MoveNextMtd),
                        Block(
                            Call(destinationExpression, addElementMtd, configuration.Map(Property(enumeratorExp, EnumeratorCurrentProp), destinationElementType)),
                            Continue(continue_label)
                        ),
                        Break(break_label)), // push to eax/rax --> return value
                    break_label, continue_label
                 )
             });
        }

        private static Expression ToEnumerableByGeneric(Expression sourceExpression, Type sourceElementType, ParameterExpression destinationExpression, Type destinationElementType, MethodInfo addElementMtd, IMapApplication configuration)
        {
            LabelTarget break_label = Label(MapConstants.VoidType);
            LabelTarget continue_label = Label(MapConstants.VoidType);

            var enumerableType = MapConstants.Enumerable_T_Type.MakeGenericType(sourceElementType);
            var enumeratorType = MapConstants.Enumerator_T_Type.MakeGenericType(sourceElementType);

            var propertyInfo = enumeratorType.GetProperty("Current");

            var enumeratorExp = Variable(enumeratorType);

            return Block(new ParameterExpression[]
             {
                enumeratorExp
             }, new Expression[]
             {
                Assign(enumeratorExp, Call(sourceExpression, enumerableType.GetMethod("GetEnumerator", Type.EmptyTypes))),
                Loop(
                    IfThenElse(
                        Call(enumeratorExp, MapConstants.MoveNextMtd),
                        Block(
                            Call(destinationExpression, addElementMtd, configuration.Map(Property(enumeratorExp, propertyInfo), destinationElementType)),
                            Continue(continue_label)
                        ),
                        Break(break_label)), // push to eax/rax --> return value
                    break_label, continue_label
                 )
             });
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="sourceExpression"><inheritdoc/></param>
        /// <param name="sourceType"><inheritdoc/></param>
        /// <param name="destinationExpression"><inheritdoc/></param>
        /// <param name="destinationType"><inheritdoc/></param>
        /// <param name="configuration"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        /// <exception cref="InvalidCastException">目标类型 <paramref name="destinationType"/> 为集合，但没有找到添加元素的方法！</exception>
        protected override Expression ToSolve(Expression sourceExpression, Type sourceType, ParameterExpression destinationExpression, Type destinationType, IMapApplication configuration)
        {
            if (TryGet(destinationType, out Type elementType, out MethodInfo addElementMtd))
            {
                if (sourceType.IsArray)
                {
                    return ArrayTo(sourceExpression, destinationExpression, elementType, addElementMtd, configuration);
                }

                return ToEnumerable(sourceExpression, sourceType, destinationExpression, elementType, addElementMtd, configuration);
            }

            throw new InvalidCastException($"目标【{destinationType}】为集合，但没有找到添加元素的方法！");
        }
    }
}
