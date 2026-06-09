using Inkslab.Map;
using System;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;

namespace Inkslab.Map.Tests
{
    /// <summary>
    /// 泛型装箱类型，用于构造带成员投影的 <see cref="NewExpression"/>。
    /// </summary>
    /// <typeparam name="T">值类型。</typeparam>
    public class Box<T>
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="value">值。</param>
        public Box(T value) => Value = value;

        /// <summary>
        /// 值。
        /// </summary>
        public T Value { get; }
    }

    /// <summary>
    /// 针对 Profile.PrepareMapExpressionVisitor.VisitNew 的白盒回归测试。
    ///
    /// 该 bug（从空列表 memberInfos[i] 取值）仅在 NewExpression.Members 非空且目标为泛型时触发，
    /// 而公开 API 无法构造出"具名泛型 + Members 非空"的表达式（Members 仅匿名类型才会填充），
    /// 故此处通过反射直接驱动私有 visitor 复现并守护该缺陷。
    /// </summary>
    public class VisitNewRegressionTests
    {
        /// <summary>
        /// 泛型目标 + 成员投影时，VisitNew 不应越界抛异常。
        /// </summary>
        [Fact]
        public void VisitNew_GenericDestinationWithProjectedMembers_DoesNotThrow()
        {
            var destinationType = typeof(Box<int>);

            var constructor = destinationType.GetConstructor(new[] { typeof(int) });
            var valueMember = (MemberInfo)destinationType.GetProperty(nameof(Box<int>.Value));

            //? 构造 new Box<int>(1) 且显式携带成员投影 Value（等价匿名类型形态）。
            var newExpression = Expression.New(
                constructor,
                new Expression[] { Expression.Constant(1) },
                new[] { valueMember });

            var visitorType = typeof(Profile).GetNestedType("PrepareMapExpressionVisitor", BindingFlags.NonPublic);

            Assert.NotNull(visitorType);

            var constructorInfo = visitorType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)[0];

            var visitor = (ExpressionVisitor)constructorInfo.Invoke(
                new object[]
                {
                    typeof(int),          // originalSourceType
                    destinationType,      // originalDestinationType（须等于 node.Type）
                    typeof(int),          // sourceType
                    destinationType,      // destinationType（须为泛型）
                    Array.Empty<Expression>(),
                    Array.Empty<Expression>()
                });

            //? 修复前：memberInfos[0] 越界抛 ArgumentOutOfRangeException；修复后正常返回。
            var result = visitor.Visit(newExpression);

            Assert.NotNull(result);
        }
    }
}
