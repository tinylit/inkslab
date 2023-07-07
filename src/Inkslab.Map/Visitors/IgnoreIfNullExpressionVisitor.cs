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
                case ExpressionType.Block:
                case ExpressionType.Lambda:
                case ExpressionType.New:
                case ExpressionType.NewArrayBounds:
                case ExpressionType.NewArrayInit:
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

        /// <summary>
        /// 访问为空忽略表达式。
        /// </summary>
        /// <param name="node">判空节点。</param>
        /// <returns>新的表达式。</returns>
        internal Expression VisitIgnoreIfNull(Expression node)
        {
            bool nullable = node.Type.IsNullable();

            Expression test = nullable
                ? Property(node, "HasValue")
                : node.Type.IsClass
                    ? NotEqual(node, Constant(null, node.Type))
                    : NotEqual(node, Default(node.Type));

            if (HasIgnore)
            {
                Test = AndAlso(Test, test);
            }
            else
            {
                HasIgnore = true;

                Test = test;
            }

            return node.CanReduce ? node.Reduce() : node;
        }
    }
}
