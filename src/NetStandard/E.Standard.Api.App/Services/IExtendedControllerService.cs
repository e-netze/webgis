using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace E.Standard.Api.App.Services;

public interface IExtendedControllerService
{
    Task<IActionResult> ApiObject(Controller controller, object obj);

    Task<IActionResult> JsonObject(Controller controller, object obj, bool pretty = false);
}
