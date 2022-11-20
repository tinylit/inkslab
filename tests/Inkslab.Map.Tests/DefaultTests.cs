using System;
using Xunit;

namespace Inkslab.Map.Tests
{
    public class DefaultTests
    {
        public DefaultTests()
        {
            //+ �������ã����Nuget���򹤳����ü���ʹ�á�
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
