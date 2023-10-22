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
        /// <param name="source">数据源对象。</param>
        /// <param name="settings">语法糖设置。</param>
        /// <returns>处理 <see cref="RegularExpression"/> 语法的语法糖。</returns>
        ISugar CreateSugar(object source, DefaultSettings settings);
    }
}
