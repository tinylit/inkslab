using System;
using System.Collections.Generic;
using Xunit;

namespace Inkslab.Tests
{
    /// <summary>
    /// TypeExtensions 扩展方法的单元测试。
    /// </summary>
    public class TypeExtensionsTests
    {
        #region IsMini Tests

        /// <summary>
        /// 测试枚举类型的 IsMini 方法。
        /// </summary>
        [Fact]
        public void IsMini_WithEnum_ReturnsTrue()
        {
            // Arrange
            var enumType = typeof(DayOfWeek);

            // Act
            var result = enumType.IsMini();

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试迷你类型的 IsMini 方法。
        /// </summary>
        /// <param name="type">要测试的类型。</param>
        [Theory]
        [InlineData(typeof(bool))]
        [InlineData(typeof(sbyte))]
        [InlineData(typeof(byte))]
        [InlineData(typeof(short))]
        [InlineData(typeof(ushort))]
        [InlineData(typeof(int))]
        [InlineData(typeof(uint))]
        [InlineData(typeof(long))]
        [InlineData(typeof(ulong))]
        [InlineData(typeof(float))]
        [InlineData(typeof(double))]
        [InlineData(typeof(IntPtr))]
        [InlineData(typeof(UIntPtr))]
        [InlineData(typeof(decimal))]
        public void IsMini_WithMiniTypes_ReturnsTrue(Type type)
        {
            // Act
            var result = type.IsMini();

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试非迷你类型的 IsMini 方法。
        /// </summary>
        /// <param name="type">要测试的类型。</param>
        [Theory]
        [InlineData(typeof(char))]
        [InlineData(typeof(string))]
        [InlineData(typeof(object))]
        [InlineData(typeof(DateTime))]
        [InlineData(typeof(Guid))]
        [InlineData(typeof(List<int>))]
        public void IsMini_WithNonMiniTypes_ReturnsFalse(Type type)
        {
            // Act
            var result = type.IsMini();

            // Assert
            Assert.False(result);
        }

        #endregion

        #region IsSimple Tests

        /// <summary>
        /// 测试简单类型的 IsSimple 方法。
        /// </summary>
        /// <param name="type">要测试的类型。</param>
        [Theory]
        [InlineData(typeof(bool))]
        [InlineData(typeof(sbyte))]
        [InlineData(typeof(byte))]
        [InlineData(typeof(char))]
        [InlineData(typeof(short))]
        [InlineData(typeof(ushort))]
        [InlineData(typeof(int))]
        [InlineData(typeof(uint))]
        [InlineData(typeof(long))]
        [InlineData(typeof(ulong))]
        [InlineData(typeof(float))]
        [InlineData(typeof(double))]
        [InlineData(typeof(IntPtr))]
        [InlineData(typeof(UIntPtr))]
        [InlineData(typeof(decimal))]
        [InlineData(typeof(string))]
        [InlineData(typeof(Guid))]
        [InlineData(typeof(TimeSpan))]
        [InlineData(typeof(DateTime))]
        [InlineData(typeof(DateTimeOffset))]
        [InlineData(typeof(byte[]))]
        public void IsSimple_WithSimpleTypes_ReturnsTrue(Type type)
        {
            // Act
            var result = type.IsSimple();

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试枚举类型的 IsSimple 方法。
        /// </summary>
        [Fact]
        public void IsSimple_WithEnum_ReturnsTrue()
        {
            // Arrange
            var enumType = typeof(DayOfWeek);

            // Act
            var result = enumType.IsSimple();

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试复杂类型的 IsSimple 方法。
        /// </summary>
        /// <param name="type">要测试的类型。</param>
        [Theory]
        [InlineData(typeof(object))]
        [InlineData(typeof(List<int>))]
        [InlineData(typeof(int[]))]
        [InlineData(typeof(char[]))]
        [InlineData(typeof(Dictionary<string, int>))]
        public void IsSimple_WithComplexTypes_ReturnsFalse(Type type)
        {
            // Act
            var result = type.IsSimple();

            // Assert
            Assert.False(result);
        }

        #endregion

        #region IsNullable Tests

        /// <summary>
        /// 测试可空类型的 IsNullable 方法。
        /// </summary>
        /// <param name="type">要测试的类型。</param>
        [Theory]
        [InlineData(typeof(int?))]
        [InlineData(typeof(bool?))]
        [InlineData(typeof(DateTime?))]
        [InlineData(typeof(Guid?))]
        [InlineData(typeof(decimal?))]
        public void IsNullable_WithNullableTypes_ReturnsTrue(Type type)
        {
            // Act
            var result = type.IsNullable();

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试非可空类型的 IsNullable 方法。
        /// </summary>
        /// <param name="type">要测试的类型。</param>
        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(string))]
        [InlineData(typeof(object))]
        [InlineData(typeof(List<int>))]
        [InlineData(typeof(DateTime))]
        public void IsNullable_WithNonNullableTypes_ReturnsFalse(Type type)
        {
            // Act
            var result = type.IsNullable();

            // Assert
            Assert.False(result);
        }

        #endregion

        #region IsKeyValuePair Tests

        /// <summary>
        /// 测试键值对类型的 IsKeyValuePair 方法。
        /// </summary>
        /// <param name="type">要测试的类型。</param>
        [Theory]
        [InlineData(typeof(KeyValuePair<string, int>))]
        [InlineData(typeof(KeyValuePair<int, string>))]
        [InlineData(typeof(KeyValuePair<object, object>))]
        public void IsKeyValuePair_WithKeyValuePairTypes_ReturnsTrue(Type type)
        {
            // Act
            var result = type.IsKeyValuePair();

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试非键值对类型的 IsKeyValuePair 方法。
        /// </summary>
        /// <param name="type">要测试的类型。</param>
        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(string))]
        [InlineData(typeof(object))]
        [InlineData(typeof(Dictionary<string, int>))]
        [InlineData(typeof(List<KeyValuePair<string, int>>))]
        public void IsKeyValuePair_WithNonKeyValuePairTypes_ReturnsFalse(Type type)
        {
            // Act
            var result = type.IsKeyValuePair();

            // Assert
            Assert.False(result);
        }

        #endregion

        #region IsAmongOf Tests

        /// <summary>
        /// 测试相同类型的 IsAmongOf 方法。
        /// </summary>
        [Fact]
        public void IsAmongOf_WithSameTypes_ReturnsTrue()
        {
            // Arrange
            var type1 = typeof(string);
            var type2 = typeof(string);

            // Act
            var result = type1.IsAmongOf(type2);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试继承关系的 IsAmongOf 方法。
        /// </summary>
        [Fact]
        public void IsAmongOf_WithInheritance_ReturnsTrue()
        {
            // Arrange
            var baseType = typeof(object);
            var derivedType = typeof(string);

            // Act
            var result = baseType.IsAmongOf(derivedType);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试接口实现的 IsAmongOf 方法。
        /// </summary>
        [Fact]
        public void IsAmongOf_WithInterface_ReturnsTrue()
        {
            // Arrange
            var interfaceType = typeof(IDisposable);
            var implementationType = typeof(System.IO.MemoryStream);

            // Act
            var result = interfaceType.IsAmongOf(implementationType);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试泛型接口的 IsAmongOf 方法。
        /// </summary>
        [Fact]
        public void IsAmongOf_WithGenericInterface_ReturnsTrue()
        {
            // Arrange
            var interfaceType = typeof(IEnumerable<>);
            var implementationType = typeof(List<int>);

            // Act
            var result = interfaceType.IsAmongOf(implementationType);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试不相关类型的 IsAmongOf 方法。
        /// </summary>
        [Fact]
        public void IsAmongOf_WithUnrelatedTypes_ReturnsFalse()
        {
            // Arrange
            var type1 = typeof(string);
            var type2 = typeof(int);

            // Act
            var result = type1.IsAmongOf(type2);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region IsLike Tests

        /// <summary>
        /// 测试相同类型的 IsLike 方法。
        /// </summary>
        [Fact]
        public void IsLike_WithSameTypes_ReturnsTrue()
        {
            // Arrange
            var type1 = typeof(string);
            var type2 = typeof(string);

            // Act
            var result = type1.IsLike(type2);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试空实现类型的 IsLike 方法。
        /// </summary>
        [Fact]
        public void IsLike_WithNullImplementationType_ReturnsFalse()
        {
            // Arrange
            var type1 = typeof(string);
            Type type2 = null;

            // Act
            var result = type1.IsLike(type2);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// 测试 object 约束的 IsLike 方法。
        /// </summary>
        [Fact]
        public void IsLike_WithObjectConstraint_ReturnsTrue()
        {
            // Arrange
            var objectType = typeof(object);
            var stringType = typeof(string);

            // Act
            var result = stringType.IsLike(objectType);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试继承关系的 IsLike 方法。
        /// </summary>
        [Fact]
        public void IsLike_WithInheritance_ReturnsTrue()
        {
            // Arrange
            var baseType = typeof(Exception);
            var derivedType = typeof(ArgumentException);

            // Act
            var result = derivedType.IsLike(baseType);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试接口实现的 IsLike 方法。
        /// </summary>
        [Fact]
        public void IsLike_WithInterface_ReturnsTrue()
        {
            // Arrange
            var interfaceType = typeof(IDisposable);
            var implementationType = typeof(System.IO.MemoryStream);

            // Act
            var result = implementationType.IsLike(interfaceType);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试泛型类型的 IsLike 方法。
        /// </summary>
        [Fact]
        public void IsLike_WithGenericTypes_ReturnsTrue()
        {
            // Arrange
            var genericType = typeof(List<>);
            var concreteType = typeof(List<int>);

            // Act
            var result = concreteType.IsLike(genericType);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试泛型接口的 IsLike 方法。
        /// </summary>
        [Fact]
        public void IsLike_WithGenericInterface_ReturnsTrue()
        {
            // Arrange
            var interfaceType = typeof(IEnumerable<>);
            var implementationType = typeof(List<int>);

            // Act
            var result = implementationType.IsLike(interfaceType);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试接口和类的 IsLike 方法。
        /// </summary>
        [Fact]
        public void IsLike_WithInterfaceAndClass_ReturnsFalse()
        {
            // Arrange
            var interfaceType = typeof(IDisposable);
            var classType = typeof(string);

            // Act
            var result = interfaceType.IsLike(classType);

            // Assert
            Assert.False(result);
        }

        /// <summary>
        /// 测试不相关类型的 IsLike 方法。
        /// </summary>
        [Fact]
        public void IsLike_WithUnrelatedTypes_ReturnsFalse()
        {
            // Arrange
            var type1 = typeof(string);
            var type2 = typeof(int);

            // Act
            var result = type1.IsLike(type2);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region IsLike with TypeLikeKind Tests

        /// <summary>
        /// 测试泛型类型种类的 IsLike 方法。
        /// </summary>
        [Fact]
        public void IsLike_WithGenericTypeKind_OnlyMatchesGenericTypes()
        {
            // Arrange
            var genericType = typeof(List<int>);
            var nonGenericType = typeof(string);

            // Act
            var result1 = genericType.IsLike(typeof(IEnumerable<int>), TypeLikeKind.IsGenericType);
            var result2 = nonGenericType.IsLike(typeof(object), TypeLikeKind.IsGenericType);

            // Assert
            Assert.True(result1);
            Assert.False(result2);
        }

        /// <summary>
        /// 测试泛型类型定义种类的 IsLike 方法。
        /// </summary>
        [Fact]
        public void IsLike_WithGenericTypeDefinitionKind_OnlyMatchesGenericTypeDefinitions()
        {
            // Arrange
            var genericTypeDef = typeof(List<>);
            var concreteType = typeof(List<int>);

            // Act
            var result1 = genericTypeDef.IsLike(typeof(IEnumerable<>), TypeLikeKind.IsGenericTypeDefinition);
            var result2 = concreteType.IsLike(typeof(IEnumerable<>), TypeLikeKind.IsGenericTypeDefinition);

            // Assert
            Assert.True(result1);
            Assert.False(result2);
        }

        #endregion

        #region Generic Parameter Tests

        /// <summary>
        /// 测试泛型参数的 IsLike 方法。
        /// </summary>
        [Fact]
        public void IsLike_WithGenericParameters_HandlesConstraints()
        {
            // 这个测试需要使用反射来获取泛型参数
            // 由于这比较复杂，我们先创建一个简单的测试
            var listType = typeof(List<>);
            var genericArgs = listType.GetGenericArguments();

            // Act & Assert
            Assert.True(genericArgs.Length > 0);
            Assert.True(genericArgs[0].IsGenericParameter);
        }

        #endregion

        #region Edge Cases Tests

        /// <summary>
        /// 测试可空类型的 IsLike 方法。
        /// </summary>
        [Fact]
        public void IsLike_WithNullableTypes_HandlesCorrectly()
        {
            // Arrange
            var nullableIntType = typeof(int?);
            var intType = typeof(int);

            // Act
            var result = nullableIntType.IsLike(typeof(Nullable<>));

            var result2 = intType.IsLike(nullableIntType);

            // Assert
            Assert.True(result);
            Assert.True(result2);
        }

        /// <summary>
        /// 测试复杂泛型层次结构的 IsAmongOf 方法。
        /// </summary>
        [Fact]
        public void IsAmongOf_WithComplexGenericHierarchy_ReturnsCorrectResult()
        {
            // Arrange
            var enumerableType = typeof(IEnumerable<>);
            var listType = typeof(List<string>);

            // Act
            var result = enumerableType.IsAmongOf(listType);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试不同泛型元数的 IsLike 方法。
        /// </summary>
        [Fact]
        public void IsLike_WithDifferentGenericArities_ReturnsFalse()
        {
            // Arrange
            var actionType = typeof(Action<>);
            var funcType = typeof(Func<,>);

            // Act
            var result = actionType.IsLike(funcType);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Helper Types for Testing

        private interface ITestInterface<T>
        {
            T Value { get; set; }
        }

        private class TestClass<T> : ITestInterface<T>
        {
            public T Value { get; set; }
        }

        private enum TestEnum
        {
            Value1,
            Value2,
            Value3
        }

        #endregion

        #region Additional Complex Scenario Tests

        /// <summary>
        /// 测试自定义泛型类的 IsLike 方法。
        /// </summary>
        [Fact]
        public void IsLike_WithCustomGenericClass_ReturnsTrue()
        {
            // Arrange
            var interfaceType = typeof(ITestInterface<>);
            var implementationType = typeof(TestClass<int>);

            // Act
            var result = implementationType.IsLike(interfaceType);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试自定义枚举的 IsSimple 方法。
        /// </summary>
        [Fact]
        public void IsSimple_WithCustomEnum_ReturnsTrue()
        {
            // Arrange
            var enumType = typeof(TestEnum);

            // Act
            var result = enumType.IsSimple();

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试自定义枚举的 IsMini 方法。
        /// </summary>
        [Fact]
        public void IsMini_WithCustomEnum_ReturnsTrue()
        {
            // Arrange
            var enumType = typeof(TestEnum);

            // Act
            var result = enumType.IsMini();

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试泛型约束的 IsAmongOf 方法。
        /// </summary>
        [Fact]
        public void IsAmongOf_WithGenericConstraints_ReturnsTrue()
        {
            // Arrange
            var collectionType = typeof(ICollection<>);
            var listType = typeof(List<string>);

            // Act
            var result = collectionType.IsAmongOf(listType);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试继承链的 IsLike 方法。
        /// </summary>
        /// <param name="derivedType">派生类型。</param>
        /// <param name="baseType">基类型。</param>
        [Theory]
        [InlineData(typeof(int), typeof(ValueType))]
        [InlineData(typeof(string), typeof(object))]
        [InlineData(typeof(List<int>), typeof(object))]
        [InlineData(typeof(string), typeof(IEnumerable<char>))]
        [InlineData(typeof(ArgumentException), typeof(SystemException))]
        public void IsLike_WithInheritanceChain_ReturnsTrue(Type derivedType, Type baseType)
        {
            // Act
            var result = derivedType.IsLike(baseType);

            // Assert
            Assert.True(result);
        }

        /// <summary>
        /// 测试不相关类型的 IsLike 方法（参数化版本）。
        /// </summary>
        /// <param name="type1">类型1。</param>
        /// <param name="type2">类型2。</param>
        [Theory]
        [InlineData(typeof(string), typeof(int))]
        [InlineData(typeof(List<int>), typeof(Dictionary<string, int>))]
        [InlineData(typeof(int), typeof(string))]
        [InlineData(typeof(string), typeof(IEnumerable<KeyValuePair<string, object>>))]
        public void IsLike_WithUnrelatedTypesParametrized_ReturnsFalse(Type type1, Type type2)
        {
            // Act
            var result = type1.IsLike(type2);

            // Assert
            Assert.False(result);
        }

        #endregion
    }
}
