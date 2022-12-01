using Inkslab.Annotations;
using Inkslab.Serialize.Xml;
using System.Diagnostics;
using System.Xml.Serialization;
using Xunit;

namespace Inkslab.Tests
{
    [XmlRoot("xml")]
    public class XmlA
    {
        [Ignore] //? 忽略字段。
        public int A1 { get; set; } = 100;

        //!? 生成 <![CDATA[{value}]]>
        public CData A2 { get; set; }

        public string A3 { get; set; }

        public DateTime A4 { get; set; }
    }

    [XmlRoot("xml")]
    public class XmlB
    {
        [Ignore] //? 忽略字段。
        public int A1 { get; set; } = 100;

        public string A2 { get; set; }

        public string A3 { get; set; }

        public DateTime A4 { get; set; }
    }
    public class XmlHelperTests
    {
        [Fact]
        public void Test()
        {
            XmlA x = new XmlA
            {
                A1 = 200,
                A2 = "测试CData节点",
                A3 = "普通节点",
                A4 = DateTime.Now
            };

            var xml = XmlHelper.XmlSerialize(x);

            Debug.WriteLine(xml);

            var x2 = XmlHelper.XmlDeserialize<XmlB>(xml);

            Assert.True(x.A1 == 200);
            Assert.True(x2.A1 == 100);
            Assert.True(x.A2 == x2.A2);
            Assert.True(x.A3 == x2.A3);
        }
    }
}
