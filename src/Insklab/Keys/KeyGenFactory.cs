using Insklab.Keys.Snowflake;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Insklab.Keys
{
    /// <summary>
    /// KeyGen构建器。
    /// </summary>
    public static class KeyGenFactory
    {
        private static readonly IKeyGenFactory _keyGen;

        /// <summary>
        /// 静态构造函数。
        /// </summary>
        static KeyGenFactory() => _keyGen = RuntimeServPools.Singleton<IKeyGenFactory, SnowflakeFactory>();

        /// <summary>
        /// 生成主键工具箱（请用静态属性或字段接收）。
        /// </summary>
        /// <returns></returns>
        public static IKeyGen Create() => _keyGen.Create();
    }
}
