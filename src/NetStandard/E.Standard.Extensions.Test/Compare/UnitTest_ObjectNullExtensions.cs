using E.Standard.Extensions.Compare;
using System;
using Xunit;

namespace E.Standard.Extensions.Test.Compare;

public class UnitTest_ObjectNullExtensions
{
    [Fact]
    public void ThrowIfNull_ShouldThrowException_WhenObjectIsNull()
    {
        string input = null;

        Func<string> messageFunc = () => "Input is null";

        Assert.Throws<Exception>(() => input.ThrowIfNull(messageFunc));
    }

    [Fact]
    public void ThrowIfNull_ShouldNotThrowException_WhenObjectIsNotNull()
    {
        string input = "not null";

        Func<string> messageFunc = () =>
        {
            return "Input is null";
        };

        input.ThrowIfNull(messageFunc);
    }

    [Fact]
    public void ThrowNullOrEmpty_ShouldThrowException_WhenListIsNull()
    {
        int[] input = null;

        Func<string> messageFunc = () =>
        {
            return "Input is null";
        };

        Assert.Throws<Exception>(() => input.ThrowIfNullOrEmpty(messageFunc));
    }

    [Fact]
    public void ThrowNullOrEmpty_ShouldThrowException_WhenListIsEmpty()
    {
        int[] input = new int[0];

        Func<string> messageFunc = () => "Input is empty";

        Assert.Throws<Exception>(() => input.ThrowIfNullOrEmpty(messageFunc));
    }

    [Fact]
    public void ThrowNullOrEmpty_ShouldNotThrowException_WhenListIsNotNullOrEmpty()
    {
        int[] input = new int[] { 1, 2, 3 };

        Func<string> messageFunc = () => "Input is empty";

        input.ThrowIfNullOrEmpty(messageFunc);
    }
}
