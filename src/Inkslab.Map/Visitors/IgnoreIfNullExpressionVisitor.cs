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
        private readonly HashSet<Expression> _ignoreIfNull = new HashSet<Expression>();
        /// <summary>
        /// 构造函数。
        /// </summary>
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
        public override Expression Visit(Expression node)
        {
            if (node is null)
            {
                return base.Visit(node);
            }

            switch (node.NodeType)
            {
                case ExpressionType.Lambda:
                case ExpressionType.Block:
                    return node;
                default:
                    return base.Visit(node);
            }
        }

        protected override Expression VisitSwitch(SwitchExpression node)
        {
            return node.Update(base.Visit(node.SwitchValue), node.Cases, node.DefaultBody);
        }

        /// <inheritdoc/>
        protected override Expression VisitBinary(BinaryExpression node)
        {
            return node.Update(node.Left, node.Conversion, base.Visit(node.Right));
        }

        /// <inheritdoc/>
        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            return node.Update(base.Visit(node.Expression));
        }

        /// <summary>
        /// 访问为空忽略表达式。
        /// </summary>
        /// <param name="node">判空节点。</param>
        /// <returns>新的表达式。</returns>
        private Expression VisitIgnoreIfNull(IgnoreIfNullExpression node)
        {
            if (_ignoreIfNull.Add(node.Test))
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
            => node.NodeType == ExpressionType.Parameter
            ? keepNullable
                ? node
                : node.Type.IsNullable()
                    ? Property(node, "Value")
                    : node
            : new IgnoreIfNullExpression(node, keepNullable);

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
            /// <param name="node">表达式节点。</param>
            /// <param name="keepNullable"></param>
            public IgnoreIfNullExpression(Expression node, bool keepNullable)
            {
                this.node = node ?? throw new ArgumentNullException(nameof(node));
                this.keepNullable = keepNullable;

                if (node is IgnoreIfNullExpression ignoreNode)
                {
                    this.node = ignoreNode.node;
                    type = ignoreNode.type;
                    test = ignoreNode.test;
                }
                else if (!node.Type.IsValueType)
                {
                    this.node = node;
                    type = node.Type;
                    test = NotEqual(node, Constant(null, node.Type));
                }
                else if (node.Type.IsNullable())
                {
                    this.node = node;
                    type = keepNullable ? node.Type : Nullable.GetUnderlyingType(node.Type);
                    test = Property(node, "HasValue");
                }
                else
                {
                    throw new ArgumentException($"类型【{node.Type}】的值不可能为null！");
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
