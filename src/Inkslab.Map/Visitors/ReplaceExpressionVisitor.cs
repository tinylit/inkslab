using System;
using System.Linq.Expressions;

namespace Inkslab.Map.Visitors
{
    /// <summary>
    /// 替换表达式。
    /// </summary>
    public class ReplaceExpressionVisitor : ExpressionVisitor
    {
        private readonly Expression[] originals;
        private readonly Expression[] nodes;

        /// <summary>
        /// 将 <paramref name="original"/> 替换为 <paramref name="node"/> 。 
        /// </summary>
        /// <param name="original">被替换的表达式。</param>
        /// <param name="node">用作替换的表达式。</param>
        public ReplaceExpressionVisitor(Expression original, Expression node)
        {
            if (original is null)
            {
                throw new ArgumentNullException(nameof(original));
            }

            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            originals = new Expression[] { original };
            nodes = new Expression[] { node };
        }

        /// <summary>
        /// 将 <paramref name="originals"/> 替换为 <paramref name="nodes"/>，按照索引下标一对一替换。 
        /// </summary>
        /// <param name="originals">被替换的表达式。</param>
        /// <param name="nodes">用作替换的表达式。</param>
        public ReplaceExpressionVisitor(Expression[] originals, Expression[] nodes)
        {
            this.originals = originals ?? throw new ArgumentNullException(nameof(originals));
            this.nodes = nodes ?? throw new ArgumentNullException(nameof(nodes));

            if (originals.Length != nodes.Length)
            {
                throw new ArgumentException("源表达式和新表达式数组长度不相等！");
            }
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="node"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public override Expression Visit(Expression node)
        {
            for (int i = 0; i < originals.Length; i++)
            {
                if (node == originals[i])
                {
                    return nodes[i];
                }
            }

            return base.Visit(node);
        }
    }
}
