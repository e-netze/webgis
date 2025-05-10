#pragma warning disable CA1416

using Api.Core.AppCode.Extensions;
using E.Standard.Api.App;
using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Services;
using E.Standard.Custom.Core.Abstractions;
using E.Standard.Custom.Core.Extensions;
using E.Standard.Json;
using E.Standard.Platform;
using E.Standard.Web.Abstractions;
using E.Standard.Web.Extensions;
using E.Standard.WebGIS.Core.Models.Abstraction;
using E.Standard.WebMapping.Core;
using gView.GraphicsEngine;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Services;

public class ExtendedControllerService : IExtendedControllerService
{
    private readonly HttpContext _context;
    private readonly IHttpService _http;
    private readonly IEnumerable<ICustomApiService> _customServices;

    public ExtendedControllerService(IHttpContextAccessor context,
                                     IHttpService http,
                                     IEnumerable<ICustomApiService> customServices)
    {
        _context = context.HttpContext;
        _http = http;
        _customServices = customServices;
    }

    #region IExtendedControllerService

    async public Task<IActionResult> ApiObject(Controller controller, object obj)
    {
        NameValueCollection nvc = this.HasFormData ? _context.Request.Form.ToCollection() : _context.Request.Query.ToCollection();
        if (nvc["f"] == "json" || _context.Request.Query["f"] == "json" ||
            nvc["f"] == "pjson" || _context.Request.Query["f"] == "pjson")
        {
            return await JsonObject(controller, obj, nvc["f"] == "pjson" || _context.Request.Query["f"] == "pjson");
        }
        if (nvc["f"] == "bin" || _context.Request.Query["f"] == "bin")
        {
            if (obj is E.Standard.Api.App.DTOs.ImageLocationResponseDTO)
            {
                var ilr = (E.Standard.Api.App.DTOs.ImageLocationResponseDTO)obj;

                byte[] data = new byte[0];
                try
                {
                    if (!String.IsNullOrWhiteSpace(ilr.Path))
                    {
                        data = (await ilr.Path.BytesFromUri(_http)).ToArray();
                    }
                    else if (!String.IsNullOrWhiteSpace(ilr.url))
                    {
                        data = await _http.GetDataAsync(ilr.url);
                    }
                }
                catch (Exception ex)
                {
                    int.TryParse((nvc["width"] ?? _context.Request.Query["width"]).ToString(), out int width);
                    int.TryParse((nvc["height"] ?? _context.Request.Query["height"]).ToString(), out int height);

                    width = width == 0 ? 400 : width;
                    height = height == 0 ? 400 : height;

                    using (var bitmap = Current.Engine.CreateBitmap(width, height))
                    using (var canvas = bitmap.CreateCanvas())
                    using (var font = Current.Engine.CreateFont(SystemInfo.DefaultFontName, 8, FontStyle.Regular))
                    using (var redBrush = Current.Engine.CreateSolidBrush(ArgbColor.Red))
                    {
                        canvas.TextRenderingHint = TextRenderingHint.AntiAlias;
                        canvas.DrawText(ex.Message, font, redBrush, new CanvasPointF(0f, 0f));

                        MemoryStream ms = new MemoryStream();
                        bitmap.Save(ms, ImageFormat.Png);
                        data = ms.ToArray();
                    }
                }

                await _customServices.HandleApiResultObject(obj as IWatchable, data, _context.User?.Identity?.Name);

                //return View("_binary", new E.Standard.Api.App.Models.Binary()
                //{
                //    ContentType = "image/png",
                //    Data = data
                //});
                return BinaryResultStream(controller, data, "image/png");
            }
            if (obj is LayerLegendItem)
            {
                //return View("_binary", new E.Standard.Api.App.Models.Binary()
                //{
                //    ContentType = ((LayerLegendItem)obj).ContentType,
                //    Data = ((LayerLegendItem)obj).Data
                //});
                return BinaryResultStream(controller, ((LayerLegendItem)obj).Data, ((LayerLegendItem)obj).ContentType);
            }
        }

        return controller.View("_html", obj as E.Standard.Api.App.Models.Abstractions.IHtml);
    }

    async public Task<IActionResult> JsonObject(Controller controller, object obj, bool pretty = false)
    {
        var json = JSerializer.Serialize(obj, pretty || ApiGlobals.IsDevelopmentEnvironment);

        await _customServices.HandleApiResultObject(obj as IWatchable, json, _context.User?.Identity?.Name);

        return JsonView(controller, json);

        //MemoryStream ms = new MemoryStream();

        //var jw = new Newtonsoft.Json.JsonTextWriter(new StreamWriter(ms));
        //jw.Formatting = pretty || ApiGlobals.IsDevelopmentEnvironment ?
        //    Newtonsoft.Json.Formatting.Indented :
        //    Newtonsoft.Json.Formatting.None;
        //var serializer = new Newtonsoft.Json.JsonSerializer();
        //serializer.Serialize(jw, obj);
        //jw.Flush();
        //ms.Position = 0;

        //string json = System.Text.Encoding.UTF8.GetString(ms.GetBuffer());
        //json = json.Trim('\0');

        //await _customServices.HandleApiResultObject(obj as IWatchable, json, _context.User?.Identity?.Name);

        //return JsonView(controller, json);
    }

    #endregion

    #region Helper

    private bool HasFormData
    {
        get
        {
            return (_context.Request.Method.ToString().ToLower() == "post" && _context.Request.HasFormContentType);
        }
    }

    public IActionResult JsonView(Controller controller, string json)
    {
        return JsonResultStream(controller, json);
    }

    public IActionResult BinaryResultStream(Controller controller, byte[] data, string contentType, string fileName = "")
    {
        if (!String.IsNullOrWhiteSpace(fileName))
        {
            _context.Response.Headers.Append("Content-Disposition", "attachment; filename=\"" + fileName + "\"");
        }

        return controller.File(data, contentType);
    }

    public IActionResult JsonResultStream(Controller controller, string json)
    {
        json = json ?? String.Empty;

        _context.Response
            .AddNoCacheHeaders()
            .AddApiCorsHeaders(_context.Request);

        return BinaryResultStream(controller, Encoding.UTF8.GetBytes(json), "application/json; charset=utf-8");
    }

    #endregion
}
