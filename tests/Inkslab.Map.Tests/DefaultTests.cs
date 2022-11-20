using System;
using Xunit;

namespace Inkslab.Map.Tests
{
    public class DefaultTests
    {
        public DefaultTests()
        {
            //+ 引包即用：添加Nuget包或工程引用即可使用。
            using (var xstartup = new XStartup())
            {
                xstartup.DoStartup();
            }
        }

        [Fact]
        public void ConvertTest()
        {
            var now = DateTime.Now;

            var date = (DateTime)Mapper.Map(now.ToString("yyyy-MM-dd"), typeof(DateTime));

            Assert.True(now.Year == date.Year && now.Month == date.Month && now.Day == date.Day);
        }
    }
}
