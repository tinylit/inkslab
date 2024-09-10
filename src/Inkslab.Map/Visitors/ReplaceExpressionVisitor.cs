using System;
using System.Linq.Expressions;

namespace Inkslab.Map.Visitors
{
    /// <summary>
    /// 替换表达式。
    /// </summary>
    public class ReplaceExpressionVisitor : ExpressionVisitor
    {
        private readonly Expression _original;
        private readonly Expression _node;

        /// <summary>
        /// 将 <paramref name="original"/> 替换为 <paramref name="node"/> 。 
        /// </summary>
        /// <param name="original">被替换的表达式。</param>
        /// <param name="node">用作替换的表达式。</param>
        public ReplaceExpressionVisitor(Expression original, Expression node)
        {
            _original = original ?? throw new ArgumentNullException(nameof(original));
            _node = node ?? throw new ArgumentNullException(nameof(node));
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="node"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public override Expression Visit(Expression node)
        {
            return node == _original ? _node : base.Visit(node);
        }
    }
}