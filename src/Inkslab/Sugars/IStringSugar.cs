using Inkslab.Settings;
using System.Text.RegularExpressions;

namespace Inkslab.Sugars
{
    /// <summary>
    /// <see cref="string"/> 语法糖。
    /// </summary>
    public interface IStringSugar
    {
        /// <summary>
        /// 模式。
        /// </summary>
        Regex RegularExpression { get; }

        /// <summary>
        /// 创建语法糖。
        /// </summary>
        /// <typeparam name="TSource">数据源类型。</typeparam>
        /// <param name="source">数据源对象。</param>
        /// <param name="settings">语法糖设置。</param>
        /// <returns>处理 <see cref="RegularExpression"/> 语法的语法糖。</returns>
        ISugar CreateSugar<TSource>(TSource source, DefaultSettings settings);
    }
}
