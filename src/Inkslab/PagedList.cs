using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Inkslab
{
    /// <summary>
    /// 分页的列表。
    /// </summary>
    /// <typeparam name="T">元素类型。</typeparam>
    public sealed class PagedList<T> : IEnumerable<T>, IEnumerable
    {
        private readonly IEnumerable<T> _datas;

        /// <summary>
        /// 空集合。
        /// </summary>
        public static readonly PagedList<T> Empty = new PagedList<T>();

        /// <summary>
        /// 空集合。
        /// </summary>
        public PagedList() => _datas = Enumerable.Empty<T>();

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="datas">数据。</param>
        /// <param name="pageIndex">页码（索引从1开始）。</param>
        /// <param name="pageSize">分页条数。</param>
        /// <param name="total">总数。</param>
        public PagedList(IReadOnlyCollection<T> datas, int pageIndex, int pageSize, int total)
        {
            _datas = datas ?? throw new ArgumentNullException(nameof(datas));

            if (pageIndex < 1)
            {
                throw new IndexOutOfRangeException("页码不能小于1。");
            }

            if (pageSize < 1)
            {
                throw new IndexOutOfRangeException("分页条目不能小于1。");
            }

            if (total < 0)
            {
                throw new IndexOutOfRangeException("总数不能小于0。");
            }

            if (datas.Count > pageSize)
            {
                throw new IndexOutOfRangeException("集合元素总数不能大于分页条数。");
            }

            if (datas.Count > total)
            {
                throw new IndexOutOfRangeException("集合元素总数不能大于总条数。");
            }

            PageIndex = pageIndex;

            PageSize = pageSize;

            Total = total;
        }

        /// <summary>
        /// 当前页码（索引从1开始）。
        /// </summary>
        public int PageIndex { get; }

        /// <summary>
        /// 分页条数。
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// 总数。
        /// </summary>
        public int Total { get; }

        /// <summary>
        /// 获取迭代器。
        /// </summary>
        /// <returns></returns>
        public IEnumerator<T> GetEnumerator() => _datas.GetEnumerator();

        /// <summary>
        /// 获取迭代器。
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
