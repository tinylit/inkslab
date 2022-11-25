using System;
using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;

namespace Inkslab.Map.Expressions
{
    /// <summary>
    /// 忽略表达式。
    /// </summary>
    public sealed class IgnoreIfNullExpression : Expression
    {
        private readonly Type type;
        private readonly Expression node;

        /// <summary>
        /// 忽略表达式枚举值。
        /// </summary>
        public const ExpressionType IgnoreIfNull = (ExpressionType)(-1);

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="node">表达式节点。</param>
        public IgnoreIfNullExpression(Expression node)
        {
            this.node = node ?? throw new ArgumentNullException(nameof(node));

            if (node is IgnoreIfNullExpression ignoreNode)
            {
                this.node = ignoreNode.node;
                this.type = ignoreNode.type;
            }
            else if (node.Type.IsClass)
            {
                this.node = node;
                this.type = node.Type;
            }
            else if (node.Type.IsNullable())
            {
                this.node = node;
                this.type = Nullable.GetUnderlyingType(node.Type);
            }
            else
            {
                throw new ArgumentException($"类型【{node.Type}】的值不可能为null！");
            }
        }

        /// <summary>
        /// 类型。
        /// </summary>
        public override Type Type => type;

        /// <summary>
        /// 节点类型。
        /// </summary>
        public override ExpressionType NodeType => IgnoreIfNull;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override bool CanReduce => true;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns><inheritdoc/></returns>
        public override Expression Reduce() => node;

        /// <summary>
        /// 分配为默认值。
        /// </summary>
        /// <param name="visitor">访问器。</param>
        /// <returns></returns>
        protected override Expression Accept(ExpressionVisitor visitor)
        {
            if (visitor is IgnoreIfNullExpressionVisitor ignore)
            {
                return ignore.VisitIgnoreIfNull(this);
            }

            if (node.Type.IsNullable())
            {
                return visitor.Visit(Property(node, "Value"));
            }

            return visitor.Visit(node);
        }
    }

    /// <summary>
    /// 为 null 时，忽略。
    /// </summary>
    public class IgnoreIfNullExpressionVisitor : ExpressionVisitor
    {
        /// <summary>
        /// 是否含有忽略条件。
        /// </summary>
        public bool HasIgnore { private set; get; }

        /// <summary>
        /// 是否不为空。
        /// </summary>
        public Expression Test { private set; get; }

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
