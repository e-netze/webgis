using E.Standard.Api.App.Extensions;
using E.Standard.WebGIS.SubscriberDatabase;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace Api.Core.AppCode.Services;

public class ViewDataHelperService
{
    public ViewDataHelperService()
    {
    }

    public void AddUsernameViewData(Controller controller, SubscriberDb.Subscriber subscriber = null)
    {
        if (controller?.User?.Identity != null && controller.User.Identity.IsAuthenticated)
        {
            var ui = controller.User.ToUserIdentification();

            if (!String.IsNullOrEmpty(ui.DisplayName))
            {
                controller.ViewData["append-subscriber-username"] = ui.DisplayName;

                controller.ViewData["subscriber-fullname"] =
                    subscriber?.FullName ?? $"subscriber::{ui.Username}";
            }
            else if (ui.Userroles != null && ui.Userroles.Contains("subscriber"))
            {
                controller.ViewData["append-subscriber-username"] =
                    subscriber != null ?
                    subscriber.FirstName + " " + subscriber.LastName + " (" + subscriber.Name + ")" :
                    ui.DisplayName ?? ui.Username;

                controller.ViewData["subscriber-fullname"] =
                    subscriber?.FullName ?? $"subscriber::{ui.Username}";
            }
            else if (ui.Userroles != null && ui.Userroles.Contains("owner")) // cloud datalinq endpoint owner
            {
                controller.ViewData["append-subscriber-username"] =
                    subscriber?.FullName ?? $"owner::{ui.Username}";

                controller.ViewData["subscriber-fullname"] =
                    subscriber?.FullName ?? $"owner::{ui.Username}";
            }
        }
    }
}
