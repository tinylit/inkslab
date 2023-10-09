using System;

namespace Inkslab
{
    /// <summary>
    /// 查找文件。
    /// </summary>
    public interface IDirectory
    {
        /// <summary>
        /// 返回指定目录中与指定搜索模式匹配的文件名(包括其路径)。
        /// </summary>
        /// <param name="path">要搜索的目录的相对或绝对路径。这个字符串不区分大小写。</param>
        /// <param name="searchPattern">要与path中的文件名匹配的搜索字符串。该参数可以包含有效的文字路径和通配符(*和?)字符的组合，但不支持正则表达式。</param>
        /// <returns>指定目录中与指定搜索模式匹配的文件的全名(包括路径)的数组，如果没有找到文件，则为空数组。</returns>
        /// <exception cref="System.IO.IOException">“路径”为文件名。或网络错误。</exception>
        /// <exception cref="UnauthorizedAccessException">调用方没有所需的权限。</exception>
        /// <exception cref="ArgumentException">路径是一个零长度字符串，只包含空白，或包含一个或多个无效字符。您可以使用 <see cref="System.IO.Path.GetInvalidPathChars"/> 查询无效字符。或 <paramref name="searchPattern"/> 不包含有效的模式。</exception>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> 或 <paramref name="searchPattern"/> 为 null。</exception>
        /// <exception cref="System.IO.PathTooLongException">指定的路径、文件名或两者都超过了系统定义的最大长度。</exception>
        /// <exception cref="System.IO.DirectoryNotFoundException">指定的路径找不到或无效(例如，它在未映射的驱动器上)。</exception>
        string[] GetFiles(string path, string searchPattern);
    }
}
