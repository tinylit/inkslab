using System.Diagnostics;
using System.Xml.Serialization;

namespace Inkslab.Serialize.Xml
{
    /// <summary>
    /// Xml<![CDATA[]]>
    /// </summary>
    [DebuggerDisplay("<![CDATA[{value}]]>")]
    public struct CData : IXmlSerializable
    {
        private string value;

        /// <summary>
        /// Allow direct assignment from string:
        /// CData cdata = "abc";
        /// </summary>
        /// <param name="value">The string being cast to CData.</param>
        /// <returns>A CData object</returns>
        public static implicit operator CData(string value) => new CData(value);

        /// <summary>
        /// Allow direct assignment to string:
        /// string str = cdata;
        /// </summary>
        /// <param name="cdata">The CData being cast to a string</param>
        /// <returns>A string representation of the CData object</returns>
        public static implicit operator string(CData cdata) => cdata.value;

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="value">字符串。</param>
        public CData(string value) => this.value = value;

        /// <summary>
        /// 生成字符串。
        /// </summary>
        /// <returns>返回当前值。</returns>
        public override string ToString() => value;

        System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(System.Xml.XmlReader reader) => value = reader.ReadElementString();

        void IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) => writer.WriteCData(value);
    }
}
