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
                case IgnoreIf:
                    return Visit(node.Reduce());
                case ExpressionType.Block:
                case ExpressionType.Lambda:
                case ExpressionType.Switch:
                case ExpressionType.Throw:
                case ExpressionType.Try:
                case ExpressionType.Unbox:
                case ExpressionType.Loop:
                case ExpressionType.Extension:
                case ExpressionType.Goto:
                case ExpressionType.Conditional:
                    return node;
                default:
                    return base.Visit(node);
            }
        }

        /// <inheritdoc/>
        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.Assign)
            {
                if (node.Right?.NodeType == IgnoreIf)
                {
                    return node.Update(base.Visit(node.Left), node.Conversion, base.Visit(node.Right));
                }
            }

            return base.VisitBinary(node);
        }

        /// <inheritdoc/>
        protected override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            if (node.Expression?.NodeType == IgnoreIf)
            {
                return node.Update(base.Visit(node.Expression));
            }

            return base.VisitMemberAssignment(node);
        }

        /// <summary>
        /// 访问为空忽略表达式。
        /// </summary>
        /// <param name="node">判空节点。</param>
        /// <returns>新的表达式。</returns>
        private Expression VisitIgnoreIfNull(IgnoreIfNullExpression node)
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

            return node.CanReduce ? node.Reduce() : node;
        }

        public static Expression IgnoreIfNull(Expression node, bool keepNullable = false) => new IgnoreIfNullExpression(node, keepNullable);

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
            /// 忽略表达式枚举值。
            /// </summary>
            private const ExpressionType IgnoreIfNull = (ExpressionType)(-1);

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
            public override ExpressionType NodeType => IgnoreIfNull;

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
