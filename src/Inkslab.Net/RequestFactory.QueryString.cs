using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Inkslab.Net
{
    public partial class RequestFactory
    {
        private class QueryString<TRequestable> where TRequestable : IRequestableBase
        {
            private bool hasQueryString;
            private readonly StringBuilder _sb;
            private readonly TRequestable _requestable;

            public QueryString(TRequestable requestable, string requestUri)
            {
                _requestable = requestable;

                hasQueryString = requestUri.Contains('?');

                _sb = new StringBuilder(requestUri);
            }

            public int Length => _sb.Length;

            public TRequestable AppendQueryString(string param)
            {
                if (param is null)
                {
                    return _requestable;
                }

                int startIndex = 0;
                int length = param.Length;

                for (; startIndex < length; startIndex++)
                {
                    var c = param[startIndex];

                    if (c is ' ' or '?' or '&')
                    {
                        continue;
                    }

                    break;
                }

                if (startIndex >= length)
                {
                    return _requestable;
                }

                if (hasQueryString)
                {
                    _sb.Append('&');
                }
                else
                {
                    _sb.Append('?');

                    hasQueryString = true;
                }

                _sb.Append(param, startIndex, length - startIndex);

                return _requestable;
            }

            public TRequestable AppendQueryString(string name, string value)
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException($"“{nameof(name)}”不能为 null 或空。", nameof(name));
                }

                return value.IsEmpty()
                    ? _requestable
                    : AppendQueryString(string.Concat(name, "=", HttpUtility.UrlEncode(value)));
            }

            public TRequestable AppendQueryString(string name, DateTime value, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK") => AppendQueryString(name, value.ToString(dateFormatString ?? "yyyy-MM-dd HH:mm:ss.FFFFFFFK"));

            public TRequestable AppendQueryString(string name, object value, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK")
            {
                AppendTo(name, value, dateFormatString ?? "yyyy-MM-dd HH:mm:ss.FFFFFFFK", false);

                return _requestable;
            }

            private void AppendTo(string name, object value, string dateFormatString, bool throwErrorsIfEnumerable)
            {
                switch (value)
                {
                    case string text:
                        AppendQueryString(name, text);

                        break;
                    case DateTime date:
                        AppendQueryString(name, date, dateFormatString);

                        break;
                    case IEnumerable enumerable:
                        if (throwErrorsIfEnumerable)
                        {
                            throw new InvalidOperationException("不支持多维数组的参数传递!");
                        }

                        foreach (var data in enumerable)
                        {
                            AppendTo(name, data, dateFormatString, true);
                        }

                        break;
                    default:
                        if (value is null)
                        {
                            break;
                        }

                        AppendQueryString(name, value.ToString());

                        break;
                }
            }

            public TRequestable AppendQueryString<TParam>(TParam param) where TParam : IEnumerable<KeyValuePair<string, object>> => AppendQueryString(param, "yyyy-MM-dd HH:mm:ss.FFFFFFFK");

            public TRequestable AppendQueryString<TParam>(TParam param, string dateFormatString) where TParam : IEnumerable<KeyValuePair<string, object>>
            {
                if (param is null)
                {
                    return _requestable;
                }

                dateFormatString ??= "yyyy-MM-dd HH:mm:ss.FFFFFFFK";

                foreach (var kv in param)
                {
                    AppendTo(kv.Key, kv.Value, dateFormatString, false);
                }

                return _requestable;
            }

            public TRequestable AppendQueryString<TParam>(TParam param, NamingType namingType = NamingType.SnakeCase, string dateFormatString = "yyyy-MM-dd HH:mm:ss.FFFFFFFK") where TParam : class
            {
                if (param is null)
                {
                    return _requestable;
                }

                dateFormatString ??= "yyyy-MM-dd HH:mm:ss.FFFFFFFK";

                var results = _cachings.GetOrAdd(typeof(TParam), MakeTypeResults)
                    .Invoke(param);

                return AppendQueryString(namingType == NamingType.Normal
                        ? results
                        : results.ConvertAll(x => new KeyValuePair<string, object>(x.Key.ToNamingCase(namingType), x.Value))
                    , dateFormatString);
            }

            public override string ToString() => _sb.ToString();
        }
    }
}
