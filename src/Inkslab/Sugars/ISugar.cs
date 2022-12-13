using System.Text.RegularExpressions;

namespace Inkslab.Sugars
{
    /// <summary>
    /// 语法糖。
    /// </summary>
    public interface ISugar
    {
        /// <summary>
        /// 格式化。
        /// </summary>
        /// <param name="match">匹配到的内容。</param>
        /// <returns>格式化后的字符串。</returns>
        string Format(Match match);
    }
}
