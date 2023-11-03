using Inkslab.Annotations;
using Inkslab.Serialize.Xml;
using System;
using System.Diagnostics;
using System.Xml.Serialization;
using Xunit;

namespace Inkslab.Tests
{
    /// <summary>
    /// 测试 Xml。
    /// </summary>
    [XmlRoot("xml")]
    public class XmlA
    {
        /// <summary>
        /// 忽略字段。
        /// </summary>
        [Ignore] //? 忽略字段。
        public int A1 { get; set; } = 100;

        /// <summary>
        /// 生成 <![CDATA[{value}]]>
        /// </summary>
        [XmlElement("C1")]
        public CData A2 { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string A3 { get; set; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public DateTime A4 { get; set; }
    }
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    [XmlRoot("xml")]
    public class XmlB
    {
        /// <summary>
        /// 忽略字段。
        /// </summary>
        [Ignore] //? 忽略字段。
        public int A1 { get; set; } = 100;
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string C1 { get; set; }
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public string A3 { get; set; }
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public DateTime A4 { get; set; }
    }
    /// <summary>
    /// <see cref="XmlHelper"/> 测试。
    /// </summary>
    public class XmlHelperTests
    {
        /// <summary>
        /// 序列和反序列化。
        /// </summary>
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
            Assert.True(x.A2 == x2.C1);
            Assert.True(x.A3 == x2.A3);
        }
    }
}
