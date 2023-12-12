using Inkslab.Keys.Snowflake;

namespace Inkslab.Keys
{
    /// <summary>
    /// KeyGen构建器。
    /// </summary>
    public static class KeyGenFactory
    {
        private static readonly IKeyGenFactory keyGen;

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        static KeyGenFactory() => keyGen = SingletonPools.Singleton<IKeyGenFactory, SnowflakeFactory>();

        /// <summary>
        /// 生成主键工具箱（请用静态属性或字段接收）。
        /// </summary>
        /// <returns></returns>
        public static IKeyGen Create() => keyGen.Create();
    }
}
