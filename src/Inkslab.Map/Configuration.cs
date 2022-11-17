﻿namespace Inkslab.Map
{
    /// <summary>
    /// 配置。
    /// </summary>
    public class Configuration : IConfiguration
    {
        /// <summary>
        /// 是否深度映射。
        /// </summary>
        public bool IsDepthMapping { get; set; }

        /// <summary>
        /// 允许空值传播。
        /// </summary>
        public bool AllowPropagationNullValues { get; set; }
    }
}
