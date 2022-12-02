using Inkslab.Map.Expressions;
using System;
using System.Linq.Expressions;

namespace Inkslab.Map.Visitors
{
    using static Expression;

    /// <summary>
    /// 为 null 时，忽略。
    /// </summary>
    public class IgnoreIfNullExpressionVisitor : ExpressionVisitor
    {
        private readonly ExpressionVisitor visitor;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="visitor">原始访问器。</param>
        public IgnoreIfNullExpressionVisitor(ExpressionVisitor visitor)
        {
            this.visitor = visitor ?? throw new ArgumentNullException(nameof(visitor));
        }

        /// <summary>
        /// 是否含有忽略条件。
        /// </summary>
        public bool HasIgnore { private set; get; }

        /// <summary>
        /// 是否不为空。
        /// </summary>
        public Expression Test { private set; get; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="node"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public override Expression Visit(Expression node)
        {
            if (node?.NodeType == IgnoreIfNullExpression.IgnoreIfNull)
            {
                return base.Visit(node);
            }

            return visitor.Visit(node);
        }

        /// <summary>
        /// 访问为空忽略表达式。
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected internal virtual Expression VisitIgnoreIfNull(IgnoreIfNullExpression node)
        {
            var reduceNode = node.CanReduce
                ? node.Reduce()
                : node;

            bool nullable = reduceNode.Type.IsNullable();

            Expression test = nullable
                ? Property(reduceNode, "HasValue")
                : NotEqual(reduceNode, Default(reduceNode.Type));

            if (HasIgnore)
            {
                Test = AndAlso(Test, test);
            }
            else
            {
                HasIgnore = true;

                Test = test;
            }

            return nullable
                ? Property(reduceNode, "Value")
                : reduceNode;
        }
    }
}
