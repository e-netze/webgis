using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Reflection;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace E.Standard.WebMapping.Core.Api;

public class ApiToolConfirmation
{
    public string Command { get; set; }
    public string Message { get; set; }
    public ApiToolConfirmationType Type { get; set; }
    public ApiToolConfirmationEventType EventType { get; set; }

    #region Static Members

    public static ApiToolConfirmation[] CommandComfirmations(IApiTool tool)
    {
        if (tool == null)
        {
            return new ApiToolConfirmation[0];
        }

        return CommandComfirmations(tool.GetType());
    }

    public static ApiToolConfirmation[] CommandComfirmations(Type type)
    {
        List<ApiToolConfirmation> confirmations = new List<ApiToolConfirmation>();

        foreach (var methodInfo in type.GetMethods())
        {
            ServerToolCommandAttribute[] commandAttributes = (ServerToolCommandAttribute[])methodInfo.GetCustomAttributes(typeof(ServerToolCommandAttribute), true);
            if (commandAttributes == null || commandAttributes.Length != 1)
            {
                continue;
            }

            var confirmAttributes = methodInfo.GetCustomAttributes<ToolCommandConfirmationAttribute>(true);
            if (commandAttributes == null || commandAttributes.Length == 0)
            {
                continue;
            }

            foreach (var confirmAttribute in confirmAttributes)
            {
                confirmations.Add(new ApiToolConfirmation()
                {
                    Command = commandAttributes[0].Method,
                    Message = confirmAttribute.Message,
                    Type = confirmAttribute.Type,
                    EventType = confirmAttribute.EventType
                });
            }
        }

        return confirmations.ToArray();
    }

    #endregion
}
