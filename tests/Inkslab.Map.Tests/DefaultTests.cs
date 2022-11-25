using System;
using Xunit;
using Inkslab.Map.Maps;
using System.Text;

namespace Inkslab.Map.Tests
{
    /// <summary>
    /// Ĭ�ϲ��ԡ�
    /// </summary>
    public class DefaultTests
    {
        /// <summary>
        /// ���캯����
        /// </summary>
        public DefaultTests()
        {
            //+ �������ã����Nuget���򹤳����ü���ʹ�á�
            using (var xstartup = new XStartup())
            {
                xstartup.DoStartup();
            }
        }

        /// <summary>
        /// �Զ��������пɿ����͡�
        /// </summary>
        [Fact]
        public void NullableTest()
        {
            var now = DateTime.Now;

            var date = Mapper.Map<DateTime?>(now);
            var date2 = (DateTime?)Mapper.Map(now.ToString("yyyy-MM-dd"), typeof(DateTime?));

            Assert.True(date.HasValue && date2.HasValue && date?.Date == date2);
        }

        /// <summary>
        /// <see cref="ConvertMap"/>��
        /// </summary>
        [Fact]
        public void ConvertTest()
        {
            var now = DateTime.Now;

            var date = Mapper.Map<DateTime>(now.ToString("yyyy-MM-dd"));

            Assert.True(now.Year == date.Year && now.Month == date.Month && now.Day == date.Day);
        }

        /// <summary>
        /// <see cref="ToStringMap"/>��
        /// </summary>
        [Fact]
        public void ToStringTest()
        {
            var newId = Guid.NewGuid().ToString("N");

            var sb = new StringBuilder();

            sb.Append(newId);

            var mapId = Mapper.Map<string>(newId);

            Assert.Equal(newId, mapId);
        }

        /// <summary>
        /// <see cref="ParseStringMap"/>.
        /// </summary>
        [Fact]
        public void ParseStringTest()
        {
            var newId = Guid.NewGuid();

            var mapId = Mapper.Map<Guid>(newId.ToString("N"));

            Assert.Equal(newId, mapId);
        }

        /// <summary>
        /// <see cref="EnumUnderlyingTypeMap"/>.
        /// </summary>
        [Fact]
        public void EnumUnderlyingTypeTest()
        {
            var timeKind = DateTimeKind.Local;

            var mapTimeKind = Mapper.Map<DateTimeKind>(timeKind.ToInt32());

            Assert.Equal(timeKind, mapTimeKind);
        }

        /// <summary>
        /// <see cref="StringToEnumMap"/>.
        /// </summary>
        [Fact]
        public void StringToEnumTest()
        {
            var timeKind = DateTimeKind.Local;

            var mapTimeKind = Mapper.Map<DateTimeKind>(timeKind.ToString());

            Assert.Equal(timeKind, mapTimeKind);
        }
    }
}
