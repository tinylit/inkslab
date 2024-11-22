using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Inkslab
{
    /// <summary>
    /// 懒加载。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class LazyList<T> : IEnumerable<T>, IEnumerable
    {
        private readonly IEnumerable<T> _datas;

        /// <summary>
        /// 空集合。
        /// </summary>
        public static readonly LazyList<T> Empty = new LazyList<T>();

        /// <summary>
        /// 空集合。
        /// </summary>
        public LazyList() => _datas = Enumerable.Empty<T>();

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="datas">集合。</param>
        /// <param name="offset">下一页偏移量。</param>
        /// <param name="hasNext">是否有下一页</param>
        public LazyList(IEnumerable<T> datas, int offset, bool hasNext)
        {

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }

            _datas = datas ?? throw new ArgumentNullException(nameof(datas));

            Offset = offset;
            HasNext = hasNext;
        }

        /// <summary>
        /// 下一页的偏移量。
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// 是否有下一个。
        /// </summary>
        public bool HasNext { get; }

        /// <inheritdoc/>
        public IEnumerator<T> GetEnumerator() => _datas.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
