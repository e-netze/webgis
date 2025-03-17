using E.Standard.WebMapping.Core.Abstraction;
using System;
using System.Text;

namespace E.Standard.WebMapping.Core.ServiceResponses;

public class ErrorResponse : ServiceResponse
{
    private StringBuilder _messageBuilder;
    private StringBuilder _messageBuilder2;

    private bool _isWarning = true;

    public ErrorResponse(IMapService service)
        : this(service.Map.Services.IndexOf(service), service.ID)
    {
    }

    public ErrorResponse(int index, string serviceID)
        : this(index, serviceID, String.Empty, String.Empty)
    {
    }

    public ErrorResponse(int index, string serviceID, string message, string message2)
        : base(index, serviceID)
    {
        AppendMessage(message, message2);
    }

    virtual public string ErrorMessage
    {
        get { return _messageBuilder?.ToString() ?? String.Empty; }
    }
    virtual public string ErrorMessage2
    {
        get { return _messageBuilder2?.ToString() ?? String.Empty; }
    }

    public void AppendMessage(ErrorResponse errorResponse)
    {
        if (errorResponse != null && errorResponse.HasErrors)
        {
            AppendMessage(errorResponse.ErrorMessage, errorResponse.ErrorMessage2);
        }
    }

    public void AppendMessage(string message, bool asWarning = false)
    {
        AppendMessage(message, String.Empty, asWarning);
    }

    public void AppendMessage(string message, string message2, bool asWarning = false)
    {
        AppendLine(ref _messageBuilder, message);
        AppendLine(ref _messageBuilder2, message2);

        if (_messageBuilder is not null)
        {
            _isWarning = _isWarning ? asWarning : false;
        }
    }

    public void AppendWarningMessage(string message) => AppendMessage(message, asWarning: true);
    public void AppendWarningMessage(string message, string message2) => AppendMessage(message, message2, asWarning: true);

    virtual public bool HasErrors => (_messageBuilder?.Length ?? 0) + (_messageBuilder2?.Length ?? 0) > 0;
    virtual public bool IsWarning => _isWarning;

    #region Helper

    private void AppendLine(ref StringBuilder target, string message)
    {
        if (!String.IsNullOrWhiteSpace(message))
        {
            target = target ?? new();

            if (target.Length > 0)
            {
                target.Append("\n");
            }

            target.Append(message);
        }
    }

    #endregion
}
