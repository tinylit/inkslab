using Inkslab.Map.Expressions;

namespace Inkslab.Map
{
    /// <summary>
    /// 映射实例。
    /// </summary>
    public class MapperInstance : ProfileExpression<MapperInstance, MapConfiguration>, IMapConfiguration, IConfiguration, IProfile
    {
        /// <summary>
        /// 构造函数。
        /// </summary>
        public MapperInstance() : base(MapConfiguration.Instance)
        {
        }
    }
}
