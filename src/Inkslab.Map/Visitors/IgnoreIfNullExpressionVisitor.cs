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
        /// <param name="keepNullable">保持可空。</param>
        /// <returns>新的表达式。</returns>
        internal Expression VisitIgnoreIfNull(Expression node, bool keepNullable)
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

            return nullable 
                ? keepNullable 
                    ? node 
                    : Property(node, "Value") 
                : node;
        }

        public static Expression IgnoreIfNull(Expression node, bool keepNullable = false) => new IgnoreIfNullExpression(node, keepNullable);

        /// <summary>
        /// 忽略表达式。
        /// </summary>
        private class IgnoreIfNullExpression : Expression
        {
            private readonly Type type;
            private readonly Expression node;
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
                }
                else if (keepNullable || !node.Type.IsValueType)
                {
                    this.node = node;
                    type = node.Type;
                }
                else if (node.Type.IsNullable())
                {
                    this.node = node;
                    type = keepNullable ? node.Type : Nullable.GetUnderlyingType(node.Type);
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

            public override bool CanReduce => false;

            public override Expression Reduce() => this;

            /// <summary>
            /// 分配为默认值。
            /// </summary>
            /// <param name="visitor">访问器。</param>
            /// <returns></returns>
            protected override Expression Accept(ExpressionVisitor visitor)
            {
                if (visitor is IgnoreIfNullExpressionVisitor ignoreVisitor)
                {
                    return ignoreVisitor.VisitIgnoreIfNull(node, keepNullable);
                }

                if (keepNullable)
                {
                    goto label_original;
                }

                if (node.Type.IsNullable())
                {
                    return visitor.Visit(Property(node, "Value"));
                }

label_original:
                return visitor.Visit(node);
            }
        }
    }
}
