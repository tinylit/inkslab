using Microsoft.AspNetCore.Mvc;

namespace Inkslab.DI.Tests
{
    /// <summary>
    /// 测试参数查找。
    /// </summary>
    public class TestSeekArguments : ITestSeekArguments
    {
        /// <summary>
        /// 调用。
        /// </summary>
        public void Invoke([FromServices] SeekArguments arguments)
        {
        }
    }

    /// <summary>
    /// 测试。
    /// </summary>
    public class SeekArguments
    {

    }
}