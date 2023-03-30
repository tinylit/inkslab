using System.Xml;
using System;

namespace Inkslab.Net
{
    /// <summary>
    /// 反序列化能力。
    /// </summary>
    public interface IDeserializeRequestable
    {
        /// <summary>
        /// 数据返回JSON格式的结果，将转为指定类型。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <param name="namingType">命名规则。</param>
        /// <returns></returns>
        IJsonDeserializeRequestable<T> JsonCast<T>(NamingType namingType = NamingType.Normal) where T : class;

        /// <summary>
        /// 数据返回XML格式的结果，将转为指定类型。
        /// </summary>
        /// <typeparam name="T">返回类型。</typeparam>
        /// <returns></returns>
        IXmlDeserializeRequestable<T> XmlCast<T>() where T : class;

        /// <summary>
        /// 数据返回JSON格式的结果，将转为指定类型(匿名对象)。
        /// </summary>
        /// <typeparam name="T">匿名类型。</typeparam>
        /// <param name="anonymousTypeObject">匿名对象。</param>
        /// <param name="namingType">命名规范。</param>
        /// <returns></returns>
        IJsonDeserializeRequestable<T> JsonCast<T>(T anonymousTypeObject, NamingType namingType = NamingType.Normal) where T : class;

        /// <summary>
        /// 数据返回XML格式的结果，将转为指定类型(匿名对象)。
        /// </summary>
        /// <typeparam name="T">匿名类型。</typeparam>
        /// <param name="anonymousTypeObject">匿名对象。</param>
        /// <returns></returns>
        IXmlDeserializeRequestable<T> XmlCast<T>(T anonymousTypeObject) where T : class;
    }

    /// <summary>
    /// JSON反序列化请求能力。
    /// </summary>
    /// <typeparam name="T">结果数据。</typeparam>
    public interface IJsonDeserializeRequestable<T> : IRequestableExtend<T>
    {
        /// <summary>
        /// 捕获Web异常，并返回结果（返回最后一次的结果）。
        /// </summary>
        /// <param name="abnormalResultAnalysis">异常捕获,并返回异常情况下的结果。</param>
        /// <returns></returns>
        IRequestableExtend<T> JsonCatch(Func<Exception, T> abnormalResultAnalysis);
    }

    /// <summary>
    /// Xml反序列化请求能力。
    /// </summary>
    /// <typeparam name="T">结果数据。</typeparam>
    public interface IXmlDeserializeRequestable<T> : IRequestableExtend<T>
    {
        /// <summary>
        /// 捕获Xml异常，并返回结果（返回最后一次的结果）。
        /// </summary>
        /// <param name="abnormalResultAnalysis">异常捕获,并返回异常情况下的结果。</param>
        /// <returns></returns>
        IRequestableExtend<T> XmlCatch(Func<XmlException, T> abnormalResultAnalysis);
    }

    /// <summary>
    /// 异步异常处理能力。
    /// </summary>
    public interface IJsonDeserializeCatchRequestable<T> : IRequestableExtend<T>
    {
        /// <summary>
        /// 捕获Web异常，并返回结果（返回最后一次的结果）。
        /// </summary>
        /// <param name="abnormalResultAnalysis">异常捕获,并返回异常情况下的结果。</param>
        /// <returns></returns>
        IRequestableExtend<T> JsonCatch(Func<Exception, T> abnormalResultAnalysis);
    }

    /// <summary>
    /// 异步异常处理能力。
    /// </summary>
    public interface IXmlDeserializeCatchRequestable<T> : IRequestableExtend<T>
    {
        /// <summary>
        /// 捕获Web异常，并返回结果（返回最后一次的结果）。
        /// </summary>
        /// <param name="abnormalResultAnalysis">异常捕获,并返回异常情况下的结果。</param>
        /// <returns></returns>
        IRequestableExtend<T> XmlCatch(Func<XmlException, T> abnormalResultAnalysis);
    }
}
