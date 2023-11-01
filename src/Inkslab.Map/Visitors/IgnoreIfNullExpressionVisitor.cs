using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Inkslab.Map.Visitors
{
    using static Expression;

    /// <summary>
    /// 为 null 时，忽略。
    /// </summary>
    internal class IgnoreIfNullExpressionVisitor : ExpressionVisitor
    {
        private readonly HashSet<Expression> ignoreIfNull = new HashSet<Expression>();

        public IgnoreIfNullExpressionVisitor()
        {
        }

        /// <summary>
        /// 忽略表达式枚举值。
        /// </summary>
        private const ExpressionType IgnoreIf = (ExpressionType)(-1);

        /// <summary>
        /// 是否含有忽略条件。
        /// </summary>
        public bool HasIgnore { private set; get; }

        /// <summary>
        /// 是否不为空。
        /// </summary>
        public Expression Test { private set; get; }

        /// <inheritdoc/>
        protected override Expression VisitSwitch(SwitchExpression node)
        {
            return node.Update(base.Visit(node.SwitchValue), node.Cases, node.DefaultBody);
        }

        /// <inheritdoc/>
        protected override Expression VisitBinary(BinaryExpression node) => node;

        /// <inheritdoc/>
        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node) => node;

        /// <summary>
        /// 访问为空忽略表达式。
        /// </summary>
        /// <param name="node">判空节点。</param>
        /// <returns>新的表达式。</returns>
        private Expression VisitIgnoreIfNull(IgnoreIfNullExpression node)
        {
            if (ignoreIfNull.Add(node.Test))
            {
                if (HasIgnore)
                {
                    Test = AndAlso(Test, node.Test);
                }
                else
                {
                    HasIgnore = true;

                    Test = node.Test;
                }
            }

            return node.CanReduce ? node.Reduce() : node;
        }

        /// <summary>
        /// 如果表达式值为空，忽略运算。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <param name="keepNullable">是否保持表达式<see cref="Nullable"/>类型。</param>
        /// <returns>表达式。</returns>
        public static Expression IgnoreIfNull(Expression node, bool keepNullable = false)
        {
            if (node.NodeType == IgnoreIf)
            {
                return node;
            }

            if (node.NodeType == ExpressionType.Parameter)
            {
                if (keepNullable || !node.Type.IsNullable())
                {
                    return node;
                }

                return Property(node, "Value");
            }

            Expression ignoreIf = node;

            while (ignoreIf is MethodCallExpression methodCall)
            {
                if (methodCall.Arguments.Count > (methodCall.Method.IsStatic ? 1 : 0))
                {
                    break;
                }

                ignoreIf = methodCall.Object ?? methodCall.Arguments[0];
            }

            if (ignoreIf.Type.IsValueType)
            {
                if (node.Type.IsNullable())
                {
                    return new IgnoreIfNullExpression(node, Property(ignoreIf, "HasValue"), keepNullable);
                }

                return node;
            }

            return new IgnoreIfNullExpression(node, NotEqual(ignoreIf, Constant(null, ignoreIf.Type)), keepNullable);
        }

        /// <summary>
        /// 如果表达式值为空，忽略运算。
        /// </summary>
        /// <param name="node">表达式。</param>
        /// <param name="test">表达式条件。</param>
        /// <param name="keepNullable">是否保持表达式<see cref="Nullable"/>类型。</param>
        /// <returns>表达式。</returns>
        public static Expression IgnoreIfNull(Expression node, Expression test, bool keepNullable = false) => new IgnoreIfNullExpression(node, test, keepNullable);

        /// <summary>
        /// 忽略表达式。
        /// </summary>
        private class IgnoreIfNullExpression : Expression
        {
            private readonly Type type;
            private readonly Expression node;
            private readonly Expression test;
            private readonly bool keepNullable;

            /// <summary>
            /// 构造函数。
            /// </summary>
            public IgnoreIfNullExpression(Expression node, Expression test, bool keepNullable)
            {
                this.node = node ?? throw new ArgumentNullException(nameof(node));
                this.test = test ?? throw new ArgumentNullException(nameof(test));
                this.keepNullable = keepNullable;

                if (!node.Type.IsValueType)
                {
                    type = node.Type;
                }
                else if (node.Type.IsNullable())
                {
                    type = keepNullable ? node.Type : Nullable.GetUnderlyingType(node.Type);
                }
            }

            /// <summary>
            /// 非空条件。
            /// </summary>
            public Expression Test => test;

            /// <summary>
            /// 类型。
            /// </summary>
            public override Type Type => type;

            /// <summary>
            /// 节点类型。
            /// </summary>
            public override ExpressionType NodeType => IgnoreIf;

            public override bool CanReduce => true;

            public override Expression Reduce()
            {
                if (keepNullable)
                {
                    goto label_original;
                }

                if (node.Type.IsNullable())
                {
                    return Property(node, "Value");
                }

label_original:
                return node;
            }

            /// <summary>
            /// 分配为默认值。
            /// </summary>
            /// <param name="visitor">访问器。</param>
            /// <returns></returns>
            protected override Expression Accept(ExpressionVisitor visitor)
            {
                if (visitor is IgnoreIfNullExpressionVisitor ignoreVisitor)
                {
                    return ignoreVisitor.VisitIgnoreIfNull(this);
                }

                return visitor.Visit(Reduce());
            }
        }
    }
}
