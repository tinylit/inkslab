using System.Linq;
using System.Text;

namespace System.Collections
{
    /// <summary>
    /// 迭代扩展。
    /// </summary>
    public static class IEnumerableExtentions
    {
        /// <summary>
        /// 字符串拼接(null对象自动忽略)。
        /// </summary>
        /// <param name="source">数据源。</param>
        /// <param name="separator">分隔符。</param>
        /// <returns></returns>
        public static string Join(this IEnumerable source, string separator = ",")
        {
            var enumerator = source.GetEnumerator();

            if (!enumerator.MoveNext())
            {
                return string.Empty;
            }

            while (enumerator.Current is null)
            {
                if (!enumerator.MoveNext())
                {
                    return string.Empty;
                }
            }

            var sb = new StringBuilder();

            sb.Append(enumerator.Current);

            while (enumerator.MoveNext())
            {
                if (enumerator.Current is null)
                {
                    continue;
                }

                sb.Append(separator)
                    .Append(enumerator.Current);
            }

            return sb.ToString();
        }

        /// <summary>
        /// 对数据中的每个元素执行指定操作。
        /// </summary>
        /// <typeparam name="TSource">元素类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <param name="eachIterator">要对数据源的每个元素执行的委托。</param>
        public static void ForEach<TSource>(this IEnumerable source, Action<TSource> eachIterator)
        {
            if (eachIterator is null)
            {
                throw new ArgumentNullException(nameof(eachIterator));
            }

            foreach (TSource item in source)
            {
                eachIterator.Invoke(item);
            }
        }

        /// <summary>
        /// 对数据中的每个元素执行指定操作。
        /// </summary>
        /// <typeparam name="TSource">元素类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <param name="eachIterator">要对数据源的每个元素执行的委托。</param>
        public static void ForEach<TSource>(this IEnumerable source, Action<TSource, int> eachIterator)
        {
            if (eachIterator is null)
            {
                throw new ArgumentNullException(nameof(eachIterator));
            }

            int index = -1;

            foreach (TSource item in source)
            {
                eachIterator.Invoke(item, ++index);
            }
        }
    }
}

namespace System.Collections.Generic
{
    /// <summary>
    /// 迭代扩展。
    /// </summary>
    public static class IEnumerableExtentions
    {
        private struct Slot<TElement>
        {
            public int hashCode;
            public TElement value;
        }

        private static int InternalGetHashCode<TKey>(IEqualityComparer<TKey> comparer, TKey key)
        {
            //Microsoft DevDivBugs 171937. work around comparer implementations that throw when passed null
            return (key == null) ? 0 : comparer.GetHashCode(key) & 0x7FFFFFFF;
        }

        /// <summary>
        /// 对数据中的每个元素执行指定操作。
        /// </summary>
        /// <typeparam name="TSource">元素类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <param name="eachIterator">要对数据源的每个元素执行的委托。</param>
        public static void ForEach<TSource>(this IEnumerable<TSource> source, Action<TSource> eachIterator)
        {
            if (eachIterator is null)
            {
                throw new ArgumentNullException(nameof(eachIterator));
            }

            foreach (TSource item in source)
            {
                eachIterator.Invoke(item);
            }
        }

        /// <summary>
        /// 对数据中的每个元素执行指定操作。
        /// </summary>
        /// <typeparam name="TSource">元素类型。</typeparam>
        /// <param name="source">数据源。</param>
        /// <param name="eachIterator">要对数据源的每个元素执行的委托。</param>
        public static void ForEach<TSource>(this IEnumerable<TSource> source, Action<TSource, int> eachIterator)
        {
            if (eachIterator is null)
            {
                throw new ArgumentNullException(nameof(eachIterator));
            }

            int index = -1;

            foreach (TSource item in source)
            {
                eachIterator.Invoke(item, ++index);
            }
        }

        /// <summary>
        /// 将指定的函数应用于两个序列的相应元素，进行迭代。
        /// </summary>
        /// <typeparam name="TFirst">第一个输入序列的元素的类型。</typeparam>
        /// <typeparam name="TSecond">第二个输入序列的元素的类型。</typeparam>
        /// <param name="first">第一个要迭代的序列。</param>
        /// <param name="second">第二个要迭代的序列。</param>
        /// <param name="eachIterator">迭代器。</param>
        public static void ZipEach<TFirst, TSecond>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, Action<TFirst, TSecond> eachIterator)
        {
            using (IEnumerator<TFirst> e1 = first.GetEnumerator())
            {
                using (IEnumerator<TSecond> e2 = second.GetEnumerator())
                {
                    while (e1.MoveNext() && e2.MoveNext())
                    {
                        eachIterator.Invoke(e1.Current, e2.Current);
                    }
                }
            }
        }

        /// <summary>
        /// 将<paramref name="source"/>序列，根据匹配的键，按照<paramref name="keySelector"/>生成的<typeparamref name="TKey"/>键去重。
        /// </summary>
        /// <typeparam name="TSource">序列元素的类型。</typeparam>
        /// <typeparam name="TKey">键选择器函数返回的键的类型。</typeparam>
        /// <param name="source">要去重的序列。</param>
        /// <param name="keySelector">从序列的每个元素中提取连接键的函数。</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<TSource> Distinct<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) 
            => source.Distinct(keySelector, null);

        /// <summary>
        /// 将<paramref name="source"/>序列，根据匹配的键，按照<paramref name="keySelector"/>生成的<typeparamref name="TKey"/>键去重。使用<see cref="IEqualityComparer{T}"/>相等比较器用于比较键。
        /// </summary>
        /// <typeparam name="TSource">序列元素的类型。</typeparam>
        /// <typeparam name="TKey">键选择器函数返回的键的类型。</typeparam>
        /// <param name="source">要去重的序列。</param>
        /// <param name="keySelector">从序列的每个元素中提取连接键的函数。</param>
        /// <param name="comparer">一个<see cref="IEqualityComparer{T}"/>来哈希和比较键。</param>
        /// <exception cref="ArgumentNullException"></exception>
        public static IEnumerable<TSource> Distinct<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (keySelector is null)
            {
                throw new ArgumentNullException(nameof(keySelector));
            }

            HashSet<TKey> set = new HashSet<TKey>(comparer);

            foreach (TSource element in source)
                if (set.Add(keySelector.Invoke(element)))
                    yield return element;

            set.Clear();
        }

        /// <summary>
        /// 将<paramref name="outer"/>序列，根据匹配的键，按照<paramref name="inner"/>序列的顺序对齐。
        /// </summary>
        /// <typeparam name="TKey">第一个序列元素的类型。</typeparam>
        /// <param name="outer">第一个要连接的序列。</param>
        /// <param name="inner">要连接到第一个序列的序列。</param>
        public static IEnumerable<TKey> AlignOverall<TKey>(this IEnumerable<TKey> outer, IEnumerable<TKey> inner)
            => AlignOverall<TKey>(outer, inner, null);

        /// <summary>
        /// 将<paramref name="outer"/>序列，根据匹配的键，按照<paramref name="inner"/>序列的顺序对齐。使用<see cref="IEqualityComparer{T}"/>相等比较器用于比较键。
        /// </summary>
        /// <typeparam name="TKey">第一个序列元素的类型。</typeparam>
        /// <param name="outer">第一个要连接的序列。</param>
        /// <param name="inner">要连接到第一个序列的序列。</param>
        /// <param name="comparer">一个<see cref="IEqualityComparer{T}"/>来哈希和比较键。</param>
        public static IEnumerable<TKey> AlignOverall<TKey>(this IEnumerable<TKey> outer, IEnumerable<TKey> inner, IEqualityComparer<TKey> comparer)
        {
            if (outer is null)
            {
                throw new ArgumentNullException(nameof(outer));
            }

            if (inner is null)
            {
                throw new ArgumentNullException(nameof(inner));
            }

            var outerResults = outer as List<TKey> ?? outer.ToList();

            int length = outerResults.Count;

            if (length == 0)
            {
                yield break;
            }

            bool slotFlag = true;

            if (comparer is null)
            {
                comparer = EqualityComparer<TKey>.Default;
            }

            var keySlots = new Slot<TKey>[outerResults.Count];

            foreach (var x in inner)
            {
                var hashCode = InternalGetHashCode(comparer, x);

                for (int i = 0; i < length; i++)
                {
                    Slot<TKey> slot;

                    if (slotFlag)
                    {
                        var yKey = outerResults[i];
                        var yCode = InternalGetHashCode(comparer, yKey);

                        keySlots[i] = slot = new Slot<TKey>
                        {
                            value = yKey,
                            hashCode = yCode
                        };
                    }
                    else
                    {
                        slot = keySlots[i];
                    }

                    if (slot.hashCode == hashCode && comparer.Equals(x, slot.value))
                    {
                        yield return outerResults[i];
                    }
                }

                slotFlag = false;
            }
        }

        /// <summary>
        /// 将<paramref name="outer"/>序列，根据匹配的键，按照<paramref name="inner"/>序列的顺序对齐。
        /// </summary>
        /// <typeparam name="TOuter">第一个序列元素的类型。</typeparam>
        /// <typeparam name="TKey">键选择器函数返回的键的类型。</typeparam>
        /// <param name="outer">第一个要连接的序列。</param>
        /// <param name="inner">要连接到第一个序列的序列。</param>
        /// <param name="outerKeySelector">从第一个序列的每个元素中提取连接键的函数。</param>
        public static IEnumerable<TOuter> Align<TOuter, TKey>(this IEnumerable<TOuter> outer, IEnumerable<TKey> inner, Func<TOuter, TKey> outerKeySelector)
            => Align(outer, inner, outerKeySelector, null);

        /// <summary>
        /// 将<paramref name="outer"/>序列，根据匹配的键，按照<paramref name="inner"/>序列的顺序对齐。使用<see cref="IEqualityComparer{T}"/>相等比较器用于比较键。
        /// </summary>
        /// <typeparam name="TOuter">第一个序列元素的类型。</typeparam>
        /// <typeparam name="TKey">键选择器函数返回的键的类型。</typeparam>
        /// <param name="outer">第一个要连接的序列。</param>
        /// <param name="inner">要连接到第一个序列的序列。</param>
        /// <param name="outerKeySelector">从第一个序列的每个元素中提取连接键的函数。</param>
        /// <param name="comparer">一个<see cref="IEqualityComparer{T}"/>来哈希和比较键。</param>
        public static IEnumerable<TOuter> Align<TOuter, TKey>(this IEnumerable<TOuter> outer, IEnumerable<TKey> inner, Func<TOuter, TKey> outerKeySelector, IEqualityComparer<TKey> comparer)
        {
            if (outer is null)
            {
                throw new ArgumentNullException(nameof(outer));
            }

            if (inner is null)
            {
                throw new ArgumentNullException(nameof(inner));
            }

            if (outerKeySelector is null)
            {
                throw new ArgumentNullException(nameof(outerKeySelector));
            }

            var outerResults = outer as List<TOuter> ?? outer.ToList();

            int length = outerResults.Count;

            if (length == 0)
            {
                yield break;
            }

            bool slotFlag = true;

            if (comparer is null)
            {
                comparer = EqualityComparer<TKey>.Default;
            }

            var keySlots = new Slot<TKey>[outerResults.Count];

            foreach (var x in inner)
            {
                var hashCode = InternalGetHashCode(comparer, x);

                for (int i = 0; i < length; i++)
                {
                    Slot<TKey> slot;

                    if (slotFlag)
                    {
                        var yKey = outerKeySelector.Invoke(outerResults[i]);
                        var yCode = InternalGetHashCode(comparer, yKey);

                        keySlots[i] = slot = new Slot<TKey>
                        {
                            value = yKey,
                            hashCode = yCode
                        };
                    }
                    else
                    {
                        slot = keySlots[i];
                    }

                    if (slot.hashCode == hashCode && comparer.Equals(x, slot.value))
                    {
                        yield return outerResults[i];
                    }
                }

                slotFlag = false;
            }
        }

        /// <summary>
        /// 将<paramref name="outer"/>序列，根据匹配的键，按照<paramref name="inner"/>序列的顺序对齐。
        /// </summary>
        /// <typeparam name="TOuter">第一个序列元素的类型。</typeparam>
        /// <typeparam name="TKey">键选择器函数返回的键的类型。</typeparam>
        /// <typeparam name="TResult">结果元素的类型。</typeparam>
        /// <param name="outer">第一个要连接的序列。</param>
        /// <param name="inner">要连接到第一个序列的序列。</param>
        /// <param name="outerKeySelector">从第一个序列的每个元素中提取连接键的函数。</param>
        /// <param name="resultSelector">从序列匹配元素创建结果元素的函数。</param>
        public static IEnumerable<TResult> Align<TOuter, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TKey> inner, Func<TOuter, TKey> outerKeySelector, Func<TOuter, TResult> resultSelector)
            => Align(outer, inner, outerKeySelector, resultSelector, null);

        /// <summary>
        /// 将<paramref name="outer"/>序列，根据匹配的键，按照<paramref name="inner"/>序列的顺序对齐。使用<see cref="IEqualityComparer{T}"/>相等比较器用于比较键。
        /// </summary>
        /// <typeparam name="TOuter">第一个序列元素的类型。</typeparam>
        /// <typeparam name="TKey">键选择器函数返回的键的类型。</typeparam>
        /// <typeparam name="TResult">结果元素的类型。</typeparam>
        /// <param name="outer">第一个要连接的序列。</param>
        /// <param name="inner">要连接到第一个序列的序列。</param>
        /// <param name="outerKeySelector">从第一个序列的每个元素中提取连接键的函数。</param>
        /// <param name="resultSelector">从序列匹配元素创建结果元素的函数。</param>
        /// <param name="comparer">一个<see cref="IEqualityComparer{T}"/>来哈希和比较键。</param>
        public static IEnumerable<TResult> Align<TOuter, TKey, TResult>(this IEnumerable<TOuter> outer, IEnumerable<TKey> inner, Func<TOuter, TKey> outerKeySelector, Func<TOuter, TResult> resultSelector, IEqualityComparer<TKey> comparer)
        {
            if (outer is null)
            {
                throw new ArgumentNullException(nameof(outer));
            }

            if (inner is null)
            {
                throw new ArgumentNullException(nameof(inner));
            }

            if (outerKeySelector is null)
            {
                throw new ArgumentNullException(nameof(outerKeySelector));
            }

            if (resultSelector is null)
            {
                throw new ArgumentNullException(nameof(resultSelector));
            }

            var outerResults = outer as List<TOuter> ?? outer.ToList();

            bool slotFlag = true;

            int length = outerResults.Count;

            if (length == 0)
            {
                yield break;
            }

            if (comparer is null)
            {
                comparer = EqualityComparer<TKey>.Default;
            }

            var keySlots = new Slot<TKey>[outerResults.Count];

            foreach (var x in inner)
            {
                var hashCode = InternalGetHashCode(comparer, x);

                for (int i = 0; i < length; i++)
                {
                    Slot<TKey> slot;

                    if (slotFlag)
                    {
                        var yKey = outerKeySelector.Invoke(outerResults[i]);
                        var yCode = InternalGetHashCode(comparer, yKey);

                        keySlots[i] = slot = new Slot<TKey>
                        {
                            value = yKey,
                            hashCode = yCode
                        };
                    }
                    else
                    {
                        slot = keySlots[i];
                    }

                    if (hashCode == slot.hashCode && comparer.Equals(x, slot.value))
                    {
                        yield return resultSelector.Invoke(outerResults[i]);
                    }
                }

                slotFlag = false;
            }
        }

        /// <summary>
        /// 将<paramref name="outer"/>序列，根据匹配的键，按照<paramref name="inner"/>序列的顺序的元素迭代器。
        /// </summary>
        /// <typeparam name="TOuter">第一个序列元素的类型。</typeparam>
        /// <typeparam name="TKey">键选择器函数返回的键的类型。</typeparam>
        /// <param name="outer">第一个要连接的序列。</param>
        /// <param name="inner">要连接到第一个序列的序列。</param>
        /// <param name="outerKeySelector">从第一个序列的每个元素中提取连接键的函数。</param>
        /// <param name="eachIterator">从两个匹配的元素使用的迭代器。</param>
        public static void AlignEach<TOuter, TKey>(this IEnumerable<TOuter> outer, IEnumerable<TKey> inner, Func<TOuter, TKey> outerKeySelector, Action<TOuter> eachIterator)
            => AlignEach(outer, inner, outerKeySelector, eachIterator, null);

        /// <summary>
        /// 将<paramref name="outer"/>序列，根据匹配的键，按照<paramref name="inner"/>序列的顺序的元素迭代器。使用<see cref="IEqualityComparer{T}"/>相等比较器用于比较键。
        /// </summary>
        /// <typeparam name="TOuter">第一个序列元素的类型。</typeparam>
        /// <typeparam name="TKey">键选择器函数返回的键的类型。</typeparam>
        /// <param name="outer">第一个要连接的序列。</param>
        /// <param name="inner">要连接到第一个序列的序列。</param>
        /// <param name="outerKeySelector">从第一个序列的每个元素中提取连接键的函数。</param>
        /// <param name="eachIterator">从两个匹配的元素使用的迭代器。</param>
        /// <param name="comparer">一个<see cref="IEqualityComparer{T}"/>来哈希和比较键。</param>
        public static void AlignEach<TOuter, TKey>(this IEnumerable<TOuter> outer, IEnumerable<TKey> inner, Func<TOuter, TKey> outerKeySelector, Action<TOuter> eachIterator, IEqualityComparer<TKey> comparer)
        {
            if (outer is null)
            {
                throw new ArgumentNullException(nameof(outer));
            }

            if (inner is null)
            {
                throw new ArgumentNullException(nameof(inner));
            }

            if (outerKeySelector is null)
            {
                throw new ArgumentNullException(nameof(outerKeySelector));
            }

            if (eachIterator is null)
            {
                throw new ArgumentNullException(nameof(eachIterator));
            }

            var outerResults = outer as List<TOuter> ?? outer.ToList();

            int length = outerResults.Count;

            if (length == 0)
            {
                return;
            }

            bool slotFlag = true;

            if (comparer is null)
            {
                comparer = EqualityComparer<TKey>.Default;
            }

            var keySlots = new Slot<TKey>[outerResults.Count];

            foreach (var x in inner)
            {
                var hashCode = InternalGetHashCode(comparer, x);

                for (int i = 0; i < length; i++)
                {
                    Slot<TKey> slot;

                    if (slotFlag)
                    {
                        var yKey = outerKeySelector.Invoke(outerResults[i]);
                        var yCode = InternalGetHashCode(comparer, yKey);

                        keySlots[i] = slot = new Slot<TKey>
                        {
                            value = yKey,
                            hashCode = yCode
                        };
                    }
                    else
                    {
                        slot = keySlots[i];
                    }

                    if (slot.hashCode == hashCode && comparer.Equals(x, slot.value))
                    {
                        eachIterator.Invoke(outerResults[i]);
                    }
                }

                slotFlag = false;
            }
        }

        /// <summary>
        /// 基于键和组的相等来关联两个序列的元素迭代器。默认的相等比较器用于比较键。
        /// </summary>
        /// <typeparam name="TOuter">第一个序列元素的类型。</typeparam>
        /// <typeparam name="TInner">第二个序列元素的类型。</typeparam>
        /// <typeparam name="TKey">键选择器函数返回的键的类型。</typeparam>
        /// <param name="outer">第一个要连接的序列。</param>
        /// <param name="inner">要连接到第一个序列的序列。</param>
        /// <param name="outerKeySelector">从第一个序列的每个元素中提取连接键的函数。</param>
        /// <param name="innerKeySelector">从第二个序列的每个元素中提取连接键的函数。</param>
        /// <param name="eachIterator">从两个匹配的元素使用的迭代器。</param>
        public static void JoinEach<TOuter, TInner, TKey>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Action<TOuter, TInner> eachIterator)
            => JoinEach(outer, inner, outerKeySelector, innerKeySelector, eachIterator, null);

        /// <summary>
        /// 基于键和组的相等来关联两个序列的元素迭代器。使用<see cref="IEqualityComparer{T}"/>相等比较器用于比较键。
        /// </summary>
        /// <typeparam name="TOuter">第一个序列元素的类型。</typeparam>
        /// <typeparam name="TInner">第二个序列元素的类型。</typeparam>
        /// <typeparam name="TKey">键选择器函数返回的键的类型。</typeparam>
        /// <param name="outer">第一个要连接的序列。</param>
        /// <param name="inner">要连接到第一个序列的序列。</param>
        /// <param name="outerKeySelector">从第一个序列的每个元素中提取连接键的函数。</param>
        /// <param name="innerKeySelector">从第二个序列的每个元素中提取连接键的函数。</param>
        /// <param name="eachIterator">从两个匹配的元素使用的迭代器。</param>
        /// <param name="comparer">一个<see cref="IEqualityComparer{T}"/>来哈希和比较键。</param>
        public static void JoinEach<TOuter, TInner, TKey>(this IEnumerable<TOuter> outer, IEnumerable<TInner> inner, Func<TOuter, TKey> outerKeySelector, Func<TInner, TKey> innerKeySelector, Action<TOuter, TInner> eachIterator, IEqualityComparer<TKey> comparer)
        {
            if (outer is null)
            {
                throw new ArgumentNullException(nameof(outer));
            }

            if (inner is null)
            {
                throw new ArgumentNullException(nameof(inner));
            }

            if (outerKeySelector is null)
            {
                throw new ArgumentNullException(nameof(outerKeySelector));
            }

            if (innerKeySelector is null)
            {
                throw new ArgumentNullException(nameof(innerKeySelector));
            }

            if (eachIterator is null)
            {
                throw new ArgumentNullException(nameof(eachIterator));
            }

            var innerResults = inner as List<TInner> ?? inner.ToList();

            int length = innerResults.Count;

            if (length == 0)
            {
                return;
            }

            bool slotFlag = true;

            if (comparer is null)
            {
                comparer = EqualityComparer<TKey>.Default;
            }

            var keySlots = new Slot<TKey>[innerResults.Count];

            foreach (var x in outer)
            {
                TKey xKey = outerKeySelector.Invoke(x);
                var xHashCode = InternalGetHashCode(comparer, xKey);

                for (int i = 0; i < length; i++)
                {
                    Slot<TKey> slot;
                    TInner y = innerResults[i];

                    if (slotFlag)
                    {
                        var yKey = innerKeySelector.Invoke(y);
                        var yCode = InternalGetHashCode(comparer, yKey);

                        keySlots[i] = slot = new Slot<TKey>
                        {
                            value = yKey,
                            hashCode = yCode
                        };
                    }
                    else
                    {
                        slot = keySlots[i];
                    }

                    if (slot.hashCode == xHashCode && comparer.Equals(xKey, slot.value))
                    {
                        eachIterator.Invoke(x, y);
                    }
                }

                slotFlag = false;
            }
        }
    }
}
