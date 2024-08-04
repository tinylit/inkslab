﻿using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Inkslab.Serialize.Xml
{
    /// <summary>
    /// XML序列化帮助类。
    /// </summary>
    public static class XmlHelper
    {
        /// <summary>
        /// XML序列化实现。
        /// </summary>
        /// <param name="stream">流。</param>
        /// <param name="obj">对象。</param>
        /// <param name="encoding">编码方式。</param>
        /// <param name="indented">是否缩进。</param>
        private static void XmlSerializeInternal(Stream stream, object obj, Encoding encoding, bool indented)
        {
            var serializer = new XmlSerializer(obj.GetType());

            var settings = new XmlWriterSettings
            {
                Indent = indented,
                Encoding = encoding
            };

            using (XmlWriter writer = XmlWriter.Create(stream, settings))
            {
                serializer.Serialize(writer, obj);

                writer.Close();
            }
        }

        /// <summary>
        /// 将一个对象序列化为XML字符串。
        /// </summary>
        /// <param name="obj">要序列化的对象。</param>
        /// <param name="encoding">编码方式，默认：UTF8。</param>
        /// <param name="indented">是否缩进。</param>
        /// <returns>序列化产生的XML字符串。</returns>
        public static string XmlSerialize(object obj, Encoding encoding = null, bool indented = false)
        {
            if (obj is null) return null;

            encoding ??= Encoding.UTF8;

            using (var stream = new MemoryStream())
            {
                XmlSerializeInternal(stream, obj, encoding, indented);

                stream.Seek(0, SeekOrigin.Begin);

                using (var reader = new StreamReader(stream, encoding))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        /// <summary>
        /// 从XML字符串中反序列化对象。
        /// </summary>
        /// <param name="xml">包含对象的XML字符串。</param>
        /// <param name="type">结果对象类型。</param>
        /// <param name="encoding">编码方式，默认：UTF8。</param>
        /// <returns>反序列化得到的对象。</returns>
        public static object XmlDeserialize(string xml, Type type, Encoding encoding = null)
        {
            if (type is null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (string.IsNullOrEmpty(xml))
                return null;

            encoding ??= Encoding.UTF8;

            try
            {
                var mySerializer = new XmlSerializer(type);

                using (var ms = new MemoryStream(encoding.GetBytes(xml)))
                {
                    using (var stream = new StreamReader(ms, encoding))
                    {
                        return mySerializer.Deserialize(stream);
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 从XML字符串中反序列化对象。
        /// </summary>
        /// <typeparam name="T">结果对象类型。</typeparam>
        /// <param name="xml">包含对象的XML字符串。</param>
        /// <param name="encoding">编码方式，默认：UTF8。</param>
        /// <returns>反序列化得到的对象。</returns>
        public static T XmlDeserialize<T>(string xml, Encoding encoding = null)
        {
            if (string.IsNullOrEmpty(xml))
                return default;

            encoding ??= Encoding.UTF8;

            try
            {
                var mySerializer = new XmlSerializer(typeof(T));

                using (var ms = new MemoryStream(encoding.GetBytes(xml)))
                {
                    using (var stream = new StreamReader(ms, encoding))
                    {
                        return (T)mySerializer.Deserialize(stream);
                    }
                }
            }
            catch
            {
                return default;
            }
        }
    }
}
