using System;
using System.Diagnostics;
using System.Xml.Serialization;

namespace Inkslab.Serialize.Xml
{
    /// <summary>
    /// Xml&lt;![CDATA[...]]&gt;
    /// </summary>
    [DebuggerDisplay("{value}")]
    public struct CData : IComparable<CData>, IEquatable<CData>, IComparable<string>, IEquatable<string>, IXmlSerializable
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
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
        /// 相等运算符重载。
        /// </summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        /// <returns>是否相等。</returns>
        public static bool operator ==(CData a, CData b) => a.Equals(b);

        /// <summary>
        /// 不相等运算符重载。
        /// </summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        /// <returns>是否不相等。</returns>
        public static bool operator !=(CData a, CData b) => !a.Equals(b);

        /// <summary>
        /// 相等运算符重载。
        /// </summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        /// <returns>是否相等。</returns>
        public static bool operator ==(CData a, string b) => a.Equals(b);

        /// <summary>
        /// 不相等运算符重载。
        /// </summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        /// <returns>是否不相等。</returns>
        public static bool operator !=(CData a, string b) => !a.Equals(b);

        /// <summary>
        /// 相等运算符重载。
        /// </summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        /// <returns>是否相等。</returns>
        public static bool operator ==(string a, CData b) => b.Equals(a);

        /// <summary>
        /// 不相等运算符重载。
        /// </summary>
        /// <param name="a">左值。</param>
        /// <param name="b">右值。</param>
        /// <returns>是否不相等。</returns>
        public static bool operator !=(string a, CData b) => !b.Equals(a);

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="value">字符串。</param>
        public CData(string value) => this.value = value;

        /// <summary>
        /// 相同。
        /// </summary>
        /// <param name="other">其它的。</param>
        /// <returns>是否相同。</returns>
        public bool Equals(CData other) => value.Equals(other.value);

        /// <summary>
        /// 比较。
        /// </summary>
        /// <param name="other">其它的。</param>
        /// <returns>比较结果。</returns>
        public int CompareTo(CData other) => value.CompareTo(other.value);

        /// <summary>
        /// 比较。
        /// </summary>
        /// <param name="other">其它的。</param>
        /// <returns>比较结果。</returns>
        public int CompareTo(string other) => value.CompareTo(other);

        /// <summary>
        /// 相同。
        /// </summary>
        /// <param name="other">其它的。</param>
        /// <returns>是否相同。</returns>
        public bool Equals(string other) => value.Equals(other);

        /// <summary>
        /// 生成字符串。
        /// </summary>
        /// <returns>返回当前值。</returns>
        public override string ToString() => value;

        System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema() => null;

        void IXmlSerializable.ReadXml(System.Xml.XmlReader reader) => value = reader.ReadElementString();

        void IXmlSerializable.WriteXml(System.Xml.XmlWriter writer) => writer.WriteCData(value);

        /// <summary>
        /// 重写比较器。
        /// </summary>
        /// <param name="obj">对象。</param>
        /// <returns>是否相同。</returns>
        public override bool Equals(object obj)
        {
            if (obj is CData data)
            {
                return Equals(data);
            }

            if (obj is string text)
            {
                return Equals(text);
            }

            return false;
        }

        /// <summary>
        /// 重写哈希值。
        /// </summary>
        /// <returns>哈希值。</returns>
        public override int GetHashCode() => value is null ? 0 : value.GetHashCode();
    }
}
