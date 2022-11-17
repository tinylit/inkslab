using System.Collections;

namespace Inkslab.Map
{
    /// <summary>
    /// 映射集合。
    /// </summary>
    public class MapCollection : IReadOnlyCollection<IMap>
    {
        private readonly List<IMap> maps;

        /// <summary>
        /// 构造函数。
        /// </summary>
        public MapCollection()
        {
            maps = new List<IMap>();
        }

        /// <summary>
        /// 构造函数。
        /// </summary>
        /// <param name="maps">映射集合。</param>
        /// <exception cref="ArgumentNullException"><paramref name="maps"/>为 null.</exception>
        public MapCollection(List<IMap> maps)
        {
            this.maps = maps ?? throw new ArgumentNullException(nameof(maps));
        }

        /// <summary>
        /// 数量。
        /// </summary>
        public int Count => maps.Count;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns><inheritdoc/></returns>
        public IEnumerator<IMap> GetEnumerator() => maps.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
