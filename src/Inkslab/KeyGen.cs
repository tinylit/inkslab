using Inkslab.Keys;

namespace Inkslab
{
    /// <summary>
    /// 主键生成器（默认：雪花算法）。
    /// </summary>
    public static class KeyGen
    {
        private static readonly IKeyGen _keyGen;

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        static KeyGen() => _keyGen = KeyGenFactory.Create();

        /// <summary>
        /// 生成主键。
        /// </summary>
        /// <returns></returns>
        public static long Id() => _keyGen.Id();

        /// <summary>
        /// 生成主键。
        /// </summary>
        /// <returns></returns>
        public static Key New() => _keyGen.New(_keyGen.Id());

        /// <summary>
        /// 生成主键。
        /// </summary>
        /// <returns></returns>
        public static Key New(long key) => _keyGen.New(key);
    }
}
