using Inkslab.Annotations;
using Inkslab.DI.Annotations;

namespace Inkslab.DI.Tests.DependencyInjections
{
    /// <summary>
    /// 测试实现选举。
    /// </summary>
    public interface ITestImplementElectionsByOne
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns><inheritdoc/></returns>
        int Test();
    }

    /// <summary>
    /// 测试实现选举。
    /// </summary>
    public interface ITestImplementElectionsByTwo
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns><inheritdoc/></returns>
        int Test();

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="i"><inheritdoc/></param>
        /// <param name="j"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        int Calc(int i, int j);
    }

    /// <summary>
    /// 测试实现选举。
    /// </summary>
    public class TestImplementElectionsByOne : ITestImplementElectionsByOne
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns><inheritdoc/></returns>
        public virtual int Test() => 1;
    }

    /// <summary>
    /// 泛型声明测试。
    /// </summary>
    /// <typeparam name="T">类型。</typeparam>
    [Transient]
    public interface ITypeDefinition<T>
    {
        
    }
    
    /// <summary>
    /// 泛型测试。
    /// </summary>
    /// <typeparam name="T">类型。</typeparam>
    [Export]
    public class TypeDefinition<T> : ITypeDefinition<T>
    {
        
    }

    /// <summary>
    /// 泛型实现。
    /// </summary>
    public class TypeDefinitionInt : ITypeDefinition<int>
    {
        
    }
    
    /// <summary>
    /// 泛型实现。
    /// </summary>
    public class TypeDefinitionLong : ITypeDefinition<long>
    {
        
    }
    
    /// <summary>
    /// 测试实现选举。
    /// </summary>
    public class TestImplementElectionsByTwo : TestImplementElectionsByOne, ITestImplementElectionsByTwo
    {
        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="i"><inheritdoc/></param>
        /// <param name="j"><inheritdoc/></param>
        /// <returns><inheritdoc/></returns>
        public int Calc(int i, int j) => i * j;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns><inheritdoc/></returns>
        public override int Test() => 2;
    }
}
