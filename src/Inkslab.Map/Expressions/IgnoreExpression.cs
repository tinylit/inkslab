using System.Linq.Expressions;
using static System.Linq.Expressions.Expression;

namespace Inkslab.Map.Expressions
{
    /// <summary>
    /// 忽略表达式。
    /// </summary>
    public sealed class IgnoreIfNullExpression : Expression
    {
        private readonly Expression node;

        /// <summary>
        /// 忽略表达式枚举值。
        /// </summary>
        public const ExpressionType IgnoreIfNull = (ExpressionType)(-1);

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="type">类型。</param>
        public IgnoreIfNullExpression(Expression node)
        {
            this.node = node ?? throw new ArgumentNullException(nameof(node));

            if(node is IgnoreIfNullExpression ignoreNode)
            {
                this.node = ignoreNode.node;
            }
        }

        /// <summary>
        /// 类型。
        /// </summary>
        public override Type Type => node.Type;

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

            return visitor.Visit(node);
        }
    }

    /// <summary>
    /// 为 null 时，忽略。
    /// </summary>
    public class IgnoreIfNullExpressionVisitor : ExpressionVisitor
    {
        public bool HasIgnore { private set; get; }

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

            if (HasIgnore)
            {
                Test = AndAlso(Test, NotEqual(reduceNode, Default(reduceNode.Type)));
            }
            else
            {
                HasIgnore = true;

                Test = NotEqual(reduceNode, Default(reduceNode.Type));
            }

            return reduceNode;
        }
    }
}
