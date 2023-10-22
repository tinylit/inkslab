namespace Inkslab
{
    /// <summary> 命名规范。 </summary>
    public enum NamingType
    {
        /// <summary> 默认命名(原样/业务自定义)。 </summary>
        Normal = 0,

        /// <summary> 驼峰命名,如：userName。 </summary>
        CamelCase = 1,

        /// <summary> 蛇形命名,如：user_name，注：反序列化时也需要指明。 </summary>
        SnakeCase = 2,

        /// <summary> 帕斯卡命名,如：UserName。 </summary>
        PascalCase = 3,

        /// <summary>短横线命名,如：user-name。</summary>
        KebabCase = 4
    }
}
