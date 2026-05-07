using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

namespace E.Standard.Api.App.Services;

public interface IExtendedControllerService
{
    Task<IActionResult> ApiObject(Controller controller, object obj);

    Task<IActionResult> JsonObject(Controller controller, object obj, bool pretty = false);
}
