using E.Standard.WebGIS.Tools.Editing.Extensions;
using E.Standard.WebGIS.Tools.Editing.Models;
using E.Standard.WebMapping.Core.Api.EventResponse;

namespace E.Standard.WebGIS.Tools.Tests.Editing.Extensions;

public class ApplyCommitFeatureResultTests
{
    private static CommitFeatureResult ResultWithInfoMessages(params string[] messages)
    {
        var result = new CommitFeatureResult(true);
        foreach (var message in messages)
        {
            result.AddInfoMessage(message);
        }

        return result;
    }

    private static CommitFeatureResult ResultWithErrorMessages(params string[] messages)
    {
        var result = new CommitFeatureResult(false);
        foreach (var message in messages)
        {
            result.AddErrorMessage(message);
        }

        return result;
    }

    [Fact]
    public void NoDialogPrefix_JoinsMessagesWithNewLine()
    {
        var response = new ApiEventResponse();
        var result = ResultWithInfoMessages("Feature A deleted", "Feature B deleted");

        response.ApplyCommitFeatureResult(result);

        Assert.Equal($"Feature A deleted{System.Environment.NewLine}Feature B deleted", response.InfoMessage);
    }

    [Fact]
    public void ExistingResponseMessage_HasDialogPrefix_PrefixMovesOnceToStart()
    {
        var response = new ApiEventResponse { InfoMessage = "dialog:Please confirm" };
        var result = ResultWithInfoMessages("Feature deleted");

        response.ApplyCommitFeatureResult(result);

        Assert.Equal($"dialog:Please confirm{System.Environment.NewLine}Feature deleted", response.InfoMessage);
    }

    [Fact]
    public void NewMessage_HasDialogPrefix_PrefixMovesOnceToStart()
    {
        var response = new ApiEventResponse { InfoMessage = "Feature A updated" };
        var result = ResultWithInfoMessages("dialog:Feature B deleted");

        response.ApplyCommitFeatureResult(result);

        Assert.Equal($"dialog:Feature A updated{System.Environment.NewLine}Feature B deleted", response.InfoMessage);
    }

    [Fact]
    public void MultipleMessages_WithDialogPrefix_PrefixAppearsOnlyOnce()
    {
        var response = new ApiEventResponse { InfoMessage = "dialog:First message" };
        var result = ResultWithInfoMessages("dialog:Second message", "dialog:Third message");

        response.ApplyCommitFeatureResult(result);

        var expected = $"dialog:First message{System.Environment.NewLine}Second message{System.Environment.NewLine}Third message";
        Assert.Equal(expected, response.InfoMessage);
        Assert.Equal(1, response.InfoMessage.Split("dialog:").Length - 1);
    }

    [Fact]
    public void ErrorMessages_DialogPrefixHandledSameWay()
    {
        var response = new ApiEventResponse { ErrorMessage = "dialog:Something went wrong" };
        var result = ResultWithErrorMessages("dialog:Second error", "Third error without prefix");

        response.ApplyCommitFeatureResult(result);

        var expected = $"dialog:Something went wrong{System.Environment.NewLine}Second error{System.Environment.NewLine}Third error without prefix";
        Assert.Equal(expected, response.ErrorMessage);
    }

    [Fact]
    public void NullResult_ReturnsResponseUnchanged()
    {
        var response = new ApiEventResponse { InfoMessage = "unchanged" };

        var returned = response.ApplyCommitFeatureResult(null);

        Assert.Same(response, returned);
        Assert.Equal("unchanged", returned.InfoMessage);
    }

    [Fact]
    public void NullResponse_ReturnsNull()
    {
        ApiEventResponse? response = null;
        var result = ResultWithInfoMessages("Feature deleted");

        var returned = response.ApplyCommitFeatureResult(result);

        Assert.Null(returned);
    }

    [Fact]
    public void EmptyResultMessages_LeavesExistingMessageUntouched()
    {
        var response = new ApiEventResponse { InfoMessage = "dialog:Existing message" };
        var result = new CommitFeatureResult(true);

        response.ApplyCommitFeatureResult(result);

        Assert.Equal("dialog:Existing message", response.InfoMessage);
    }
}
