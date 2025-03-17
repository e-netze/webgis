using E.Standard.Extensions.Collections;
using System.Collections.Generic;
using Xunit;

namespace E.Standard.Extensions.Test.Collections;

public class UnitTest_CollectionsExtensions
{
    [Theory]
    [InlineData(new int[] { 1, 2, 3 }, new int[] { 4, 5, 6 }, new int[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(new int[] { 1, 2, 3 }, null, new int[] { 1, 2, 3 })]
    [InlineData(null, new int[] { 1, 2, 3 }, new int[] { 1, 2, 3 })]
    public void AppendArray_ShouldAppendArrays(int[] x, int[] y, int[] expected)
    {
        // Act
        int[] result = x.AppendArray(y);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(new int[] { 1, 2, 3 }, new int[] { 4, 5, 6 }, new int[] { 1, 2, 3, 4, 5, 6 })]
    [InlineData(new int[] { 1, 2, 3 }, null, new int[] { 1, 2, 3 })]
    [InlineData(null, new int[] { 1, 2, 3 }, new int[] { 1, 2, 3 })]
    public void TryAppendItems_ShouldAppendCollections(ICollection<int> x, IEnumerable<int> y, int[] expected)
    {
        // Act
        ICollection<int> result = x.TryAppendItems(y);

        // Assert
        Assert.Equal(expected, result);
    }

}
