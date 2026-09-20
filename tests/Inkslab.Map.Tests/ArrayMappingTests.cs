using MapCompatibility;
using Xunit;

#pragma warning disable CS1591

namespace Inkslab.Map.Tests
{
    public class ArrayMappingTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ReferenceArrayWithNulls(bool allowNulls) => ArrayMappingScenarios.ReferenceArrayWithNulls(allowNulls);

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ReferenceArrayWithoutNulls(bool allowNulls) => ArrayMappingScenarios.ReferenceArrayWithoutNulls(allowNulls);

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ReferenceArrayAllNulls(bool allowNulls) => ArrayMappingScenarios.ReferenceArrayAllNulls(allowNulls);

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void NullableArrayWithNulls(bool allowNulls) => ArrayMappingScenarios.NullableArrayWithNulls(allowNulls);

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void NullableArrayWithoutNulls(bool allowNulls) => ArrayMappingScenarios.NullableArrayWithoutNulls(allowNulls);

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void NullableArrayToValues(bool allowNulls) => ArrayMappingScenarios.NullableArrayToValues(allowNulls);

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void IntegerArray(bool allowNulls) => ArrayMappingScenarios.IntegerArray(allowNulls);

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Scalar(bool allowNulls) => ArrayMappingScenarios.Scalar(allowNulls);
    }
}
