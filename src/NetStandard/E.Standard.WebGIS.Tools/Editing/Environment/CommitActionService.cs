#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using E.Standard.Converters.Extensions;
using E.Standard.Extensions.Text;
using E.Standard.Security.Cryptography.Token.Models;
using E.Standard.WebGIS.CMS;

using static E.Standard.WebGIS.Tools.Editing.Environment.EditEnvironment;

namespace E.Standard.WebGIS.Tools.Editing.Environment;

internal class CommitActionService
{
    public enum Timing { Before, After }
    private readonly EditEnvironment _editEnvironment;
    public CommitActionService(EditEnvironment editEnvironment)
    {
        _editEnvironment = editEnvironment;
    }

    public async Task FireActions(
            EditTheme editTheme, 
            Timing timing, 
            EditFeatureCommand command,
            IEnumerable<WebMapping.Core.Feature> features)
    {
        var commitActions = editTheme?
            .CommitActions?
            .Where(c => c.Timing == GetCommitTiming(timing, command))
            .ToArray();

        if (commitActions?.Any() != true) { return; }  // nothing to do... OK

        foreach (var feature in features)
        {
            foreach (var commitAction in commitActions)
            {
                var commitActionTask = commitAction.Protocol switch
                {
                    EditCommitActionProtocol.Http_Get => FireHttpGet(commitAction, feature),
                    EditCommitActionProtocol.Http_Post => FireHttpPost(commitAction, feature),
                    _ => throw new Exception($"Commit action protocol handling for {commitAction.Protocol} is not implemented!")
                };

                try
                {
                    await commitActionTask;
                }
                catch (Exception ex)
                {
                    throw new Exception($"{commitAction.Timing} commit action '{commitAction.Name}' causes an error", ex);
                }
            }
        }
    }

    #region Perfom Protocol

    private async Task FireHttpGet(
        EditTheme.CommitAction commitAction,
        WebMapping.Core.Feature feature)
    {
        string payload = Globals.SolveExpression(feature, (commitAction.Payload ?? ""));
        string target = Globals.SolveExpression(feature, commitAction.Target)
            .AddUrlQueryString(payload);

        await _editEnvironment.Bridge.HttpService.GetDataAsync(
            commitAction.Target,
            headers: GetCommitActionHeaders(commitAction));
    }

    private async Task FireHttpPost(
        EditTheme.CommitAction commitAction,
        WebMapping.Core.Feature feature)
    {
        string payload = Globals.SolveExpression(feature, commitAction.Payload ?? "");

        if (payload.IsJson())
        {
            await _editEnvironment.Bridge.HttpService.PostJsonAsync(
                commitAction.Target,
                payload,
                headers: GetCommitActionHeaders(commitAction));

            return;
        }

        await _editEnvironment.Bridge.HttpService.PostDataAsync(
            commitAction.Target,
            Encoding.UTF8.GetBytes(payload),
            headers: GetCommitActionHeaders(commitAction));
    }

    #endregion

    #region Helper

    private EditCommitActionTiming GetCommitTiming(Timing timing, EditFeatureCommand command)
        => (timing, command) switch
        {
            (Timing.Before, EditFeatureCommand.Insert) => EditCommitActionTiming.Before_Insert,
            (Timing.After, EditFeatureCommand.Insert) => EditCommitActionTiming.After_Insert,
            (Timing.Before, EditFeatureCommand.Update) => EditCommitActionTiming.Before_Update,
            (Timing.After, EditFeatureCommand.Update) => EditCommitActionTiming.After_Update,
            (Timing.Before, EditFeatureCommand.Delete) => EditCommitActionTiming.Before_Delete,
            (Timing.After, EditFeatureCommand.Delete) => EditCommitActionTiming.After_Delete,
            _ => throw new ArgumentException($"No {timing} commit action possible with {command} statement!")
        };

    private IDictionary<string, string>? GetCommitActionHeaders(EditTheme.CommitAction commitAction)
    {
        if (commitAction?.Headers?.Any(h => !String.IsNullOrWhiteSpace(h)) != true)
        {
            return null;
        }

        var headers = new Dictionary<string, string>();

        // Headers[]:  ["AUTHENTICATION=Basic xxxxx", ...]
        foreach (var header in commitAction.Headers)
        {
            if (String.IsNullOrWhiteSpace(header))
            {
                continue;
            }

            var separatorIndex = header.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = header[..separatorIndex].Trim();
            var value = header[(separatorIndex + 1)..].Trim();

            if (!String.IsNullOrWhiteSpace(key))
            {
                headers[key] = value;
            }
        }

        return headers.Count > 0 ? headers : null;
    }

    #endregion
}
