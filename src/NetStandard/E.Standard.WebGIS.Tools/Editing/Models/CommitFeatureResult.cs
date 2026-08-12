#nullable enable

using System.Collections.Generic;

namespace E.Standard.WebGIS.Tools.Editing.Models;

public class CommitFeatureResult
{
    private List<string>? _errorMessages = null;
    private List<string>? _infoMessages = null;

    public CommitFeatureResult()
    {
    }

    public CommitFeatureResult(bool success)
    {
        Success = success;
    }

    public bool Success { get; set; }

    public IEnumerable<string> ErrorMessages => _errorMessages ?? [];

    public IEnumerable<string> InfoMessages => _infoMessages ?? [];

    public void AddErrorMessage(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            (_errorMessages ??= new()).Add(message);
        }
    }

    public void AddErrorMessageIfNotExists(string message)
    {
        if (!string.IsNullOrWhiteSpace(message) && (_errorMessages == null || !_errorMessages.Contains(message)))
        {
            (_errorMessages ??= new()).Add(message);
        }
    }

    public void AddInfoMessage(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            (_infoMessages ??= new()).Add(message);
        }
    }

    public void AddInfoMessageIfNotExists(string message)
    {
        if (!string.IsNullOrWhiteSpace(message) && (_infoMessages == null || !_infoMessages.Contains(message)))
        {
            (_infoMessages ??= new()).Add(message);
        }
    }

    public static implicit operator CommitFeatureResult(bool success) => new(success);

    public static implicit operator bool(CommitFeatureResult result) => result?.Success ?? false;
}
