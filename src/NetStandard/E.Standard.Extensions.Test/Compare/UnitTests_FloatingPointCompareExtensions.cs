using E.Standard.Extensions.Compare;
using Xunit;

namespace E.Standard.Extensions.Test.Compare;

public class UnitTests_FloatingPointCompareExtensions
{
    [Theory]
    [InlineData(1.0, 1.0, 1e-7, true)]
    [InlineData(1.0, 1.0000000999, 1e-7, true)]
    [InlineData(1.0, 1.00001, 1e-7, false)]
    [InlineData(1.0, 2.0, 1e-7, false)]
    public void EqualDoubleValue_ShouldCompareDoubleValues(double x, double y, double epsilon, bool expected)
    {
        // Act
        bool result = x.EqualDoubleValue(y, epsilon);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1.0f, 1.0f, 1e-7f, true)]
    [InlineData(1.0f, 1.000999f, 1e-3f, true)]
    [InlineData(1.0f, 1.001f, 1e-3f, false)]
    [InlineData(1.0f, 2.0f, 1e-7f, false)]
    public void EqualFloatValue_ShouldCompareFloatValues(float x, float y, float epsilon, bool expected)
    {
        // Act
        bool result = x.EqualFloatValue(y, epsilon);

        // Assert
        Assert.Equal(expected, result);
    }

}
