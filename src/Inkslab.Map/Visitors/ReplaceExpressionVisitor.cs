using System.Linq.Expressions;

namespace Inkslab.Map.Visitors
{
    /// <summary>
    /// 替换表达式。
    /// </summary>
    public class ReplaceExpressionVisitor : ExpressionVisitor
    {
        private readonly Expression _oldExpression;
        private readonly Expression _newExpression;

        /// <summary>
        /// 将 <paramref name="oldExpression"/> 替换为 <paramref name="newExpression"/> 。 
        /// </summary>
        /// <param name="oldExpression">被替换的表达式。</param>
        /// <param name="newExpression">用作替换的表达式。</param>
        public ReplaceExpressionVisitor(Expression oldExpression, Expression newExpression)
        {
            _oldExpression = oldExpression;
            _newExpression = newExpression;
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="node"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public override Expression Visit(Expression node)
        {
            if (_oldExpression == node)
            {
                return base.Visit(_newExpression);
            }

            return base.Visit(node);
        }
    }
}
