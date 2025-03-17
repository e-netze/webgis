using Api.Core.AppCode.Extensions;
using Api.Core.AppCode.Mvc;
using E.Standard.Api.App;
using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Services.Cache;
using E.Standard.CMS.Core;
using E.Standard.WebGIS.Tools.Editing;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.EventResponse;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Services.Rest;

public class RestEditingHelperService
{
    private readonly UploadFilesService _upload;
    private readonly BridgeService _bridge;
    private readonly CacheService _cache;

    public RestEditingHelperService(BridgeService bridge,
                                    UploadFilesService upload,
                                    CacheService cache)
    {
        _bridge = bridge;
        _upload = upload;
        _cache = cache;
    }

    async public Task<IActionResult> PerformEditServiceRequest(ApiBaseController controller, string serviceId, string themeIds, string command, CmsDocument.UserIdentification ui, NameValueCollection form = null)
    {
        StringBuilder editThemeId = new StringBuilder();

        var httpRequest = controller.Request;

        // 
        var editThemeDefinition = new E.Standard.WebGIS.Tools.Editing.Models.EditThemeDefinition()
        {
            ServiceId = serviceId,
            EditThemeId = editThemeId.ToString()
            //,LayerId = "4"
        };

        StringBuilder extErrorMessage = new StringBuilder();
        foreach (var themeId in themeIds.Split(','))
        {
            var editTheme = _cache.GetEditTheme(serviceId, themeId, ui);
            if (editTheme == null)
            {
                extErrorMessage.Append($" Unknown edit theme: serviceId={serviceId}, themeId={themeId}");
                continue; //throw new Exception("Unknown Edit Theme");
            }
            //if (editTheme.IsEditServiceTheme == false)
            //    continue; // throw new Exception("Theme is not authorized for edit-service");

            editThemeDefinition.LayerId = editTheme.LayerId;
            editThemeDefinition.EditThemeName = editTheme.Name;
            editThemeDefinition.EditThemeId = themeId;

            if (editThemeId.Length > 0)
            {
                editThemeId.Append(",");
            }

            editThemeId.Append(themeId);
        }

        if (editThemeId.Length == 0)
        {
            throw new Exception($"No authorized edit-service-themes found: {extErrorMessage}");
        }

        ApiEventResponse response = null;

        if (command == "capabilities")
        {
            var editTool = new E.Standard.WebGIS.Tools.Editing.Edit();
            Bridge bridge = _bridge.CreateInstance(ui, editTool);

            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("serviceid", serviceId);
            nvc.Add("themeid", editThemeId.ToString());
            nvc.Add("_editfield_edittheme_def", ApiToolEventArguments.ToArgument(editThemeDefinition));

            var e = new ApiToolEventArguments(bridge, nvc);
            response = await new EditToolServiceMobile(editTool).OnEditServiceGetCapabilities(bridge, e);
        }
        else if (command == "insert")
        {
            var editTool = new E.Standard.WebGIS.Tools.Editing.Mobile.InsertFeature();
            Bridge bridge = _bridge.CreateInstance(ui, editTool);

            NameValueCollection nvc = new NameValueCollection(form != null ? form : httpRequest.Query.ToCollection());
            nvc["_editfield_themeid"] = editThemeId.ToString();
            nvc["serviceid"] = serviceId;
            nvc["_editfield_edittheme_def"] = ApiToolEventArguments.ToArgument(editThemeDefinition);

            ApiToolEventArguments args = new ApiToolEventArguments(bridge, nvc);

            response = await editTool.OnEditServiceSave(bridge, args);
        }
        else if (command == "delete")
        {
            var editTool = new E.Standard.WebGIS.Tools.Editing.Mobile.UpdateFeature();
            Bridge bridge = _bridge.CreateInstance(ui, editTool);

            NameValueCollection nvc = new NameValueCollection(form != null ? form : httpRequest.Query.ToCollection());
            nvc["_editfield_themeid"] = editThemeId.ToString();
            nvc["serviceid"] = serviceId;
            nvc["_editfield_oid"] = httpRequest.QueryOrForm("oid");
            nvc["_editfield_edittheme_def"] = ApiToolEventArguments.ToArgument(editThemeDefinition);

            ApiToolEventArguments args = new ApiToolEventArguments(bridge, nvc);

            response = await editTool.OnEditServiceDelete(bridge, args);
        }
        else if (command == "upload")
        {
            var editTool = new E.Standard.WebGIS.Tools.Editing.Mobile.Files();
            Bridge bridge = _bridge.CreateInstance(ui, editTool);

            var file = _upload.GetFiles(httpRequest)["file"];
            if (file == null)
            {
                throw new Exception("No file uploaded");
            }

            byte[] data = file.Data;
            string fileContentType = file.ContentType;

            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("edit-themeid", editThemeId.ToString());
            nvc.Add("edit-field-name", httpRequest.Query["field"]);
            nvc.Add("subdir-name", httpRequest.Query["subdir"]);
            nvc.Add("file-name", file.FileName);
            nvc.Add("file-content-type", file.ContentType);
            nvc.Add("file-data-b64", Convert.ToBase64String(data));
            ApiToolEventArguments args = new ApiToolEventArguments(bridge, nvc);

            response = editTool.OnUpload(bridge, args);
        }
        else if (command == "deletefile")
        {
            var editTool = new E.Standard.WebGIS.Tools.Editing.Mobile.Files();
            Bridge bridge = _bridge.CreateInstance(ui, editTool);

            NameValueCollection nvc = new NameValueCollection();
            nvc.Add("url", httpRequest.Query["url"]);

            ApiToolEventArguments args = new ApiToolEventArguments(bridge, nvc);

            response = editTool.DelteFile(bridge, args);
        }
        else
        {
            return await controller.JsonViewSuccess(false, "Unknown command: " + command);
        }

        if (response is ApiRawJsonEventResponse)
        {
            if (httpRequest.Query["responseformat"] == "framed")
            {
                return controller.FramedJsonObject(((ApiRawJsonEventResponse)response).RawJsonObject, httpRequest.Query["callbackchannel"]);
            }

            return await controller.JsonObject(((ApiRawJsonEventResponse)response).RawJsonObject);
        }

        return null;
    }
}
