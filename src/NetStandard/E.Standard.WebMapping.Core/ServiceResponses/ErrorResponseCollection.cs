using E.Standard.WebMapping.Core.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace E.Standard.WebMapping.Core.ServiceResponses;

public class ErrorResponseCollection : ErrorResponse
{
    private readonly List<ErrorResponse> _errors = new List<ErrorResponse>();
    private readonly ServiceCollection _services;

    public ErrorResponseCollection(ServiceCollection services)
        : base(0, String.Empty)
    {
        _services = services;
    }

    public void Add(ErrorResponse error)
    {
        if (error?.HasErrors == true)
        {
            _errors.Add(error);
        }
    }

    public override bool HasErrors => _errors.Count > 0;
    public bool OnlyWarnings => _errors.All(e => e.IsWarning);

    public override string ErrorMessage
    {
        get
        {
            var sb = new StringBuilder();

            foreach (var error in _errors)
            {
                var header = _services?
                    .Where(s => s.ID == error.ServiceID)
                    .Select(s => $"{s.Name} ({s.Url})").FirstOrDefault() ?? error.ServiceID;

                if (!String.IsNullOrEmpty(header))
                {
                    sb.Append($"{header}:\n");
                }

                sb.Append($"{error.ErrorMessage}\n\n");
            }

            return sb.ToString().Trim();
        }
    }

    public override string ErrorMessage2
    {
        get
        {
            var sb = new StringBuilder();

            foreach (var error in _errors)
            {
                sb.Append($"{error.ServiceID}: {error.ErrorMessage2}{System.Environment.NewLine}");
            }

            return sb.ToString();
        }
    }
}
