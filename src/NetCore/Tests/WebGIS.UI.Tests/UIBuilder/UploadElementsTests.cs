using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;

using WebGIS.UI.Tests.Infrastructure;

namespace WebGIS.UI.Tests.UIBuilder;

/// <summary>
/// Characterization tests for the upload elements (package 6: upload-file, upload-file-control).
/// </summary>
[TestClass]
public class UploadElementsTests : UIBuilderPageTest
{
    private static readonly object TestTool = new { id = "test-tool", type = "clientbutton" };

    // a programmatic click on a file input would open the file chooser => record and prevent it
    private const string RecordFileInputClicks =
        "() => { window.__fileInputClicks = []; $(document).on('click', 'input[type=file]', function (e) { window.__fileInputClicks.push(this.id); e.preventDefault(); }); }";

    private static IUIElement Create(string type) => type switch
    {
        "upload-file" => new UIUploadFile(typeof(UploadElementsTests), "upload-cmd") { id = "upload" },
        "upload-file-control" => new UIUploadFileEdit("edit-service", "edit-theme", "FOTO") { id = "upload" },
        _ => throw new ArgumentException(type)
    };

    #region common variants

    [TestMethod]
    [DataRow("upload-file")]
    [DataRow("upload-file-control")]
    public async Task Upload_Default(string type)
    {
        await BuildAsync(Create(type));
        await AssertSnapshotAsync(type, "default");
    }

    [TestMethod]
    [DataRow("upload-file")]
    [DataRow("upload-file-control")]
    public async Task Upload_NoId(string type)
    {
        var element = Create(type);
        element.id = null;
        await BuildAsync(element);
        await AssertSnapshotAsync(type, "no-id");
    }

    [TestMethod]
    [DataRow("upload-file")]
    [DataRow("upload-file-control")]
    public async Task Upload_IdCssStyle(string type)
    {
        await BuildJsonAsync(UIJson.SerializeWith(Create(type), new { css = "custom-css", style = "width:100px", vis_dependency = "webgis-dependencies webgis-dependency-hasselection" }));
        await AssertSnapshotAsync(type, "id-css-style");
    }

    [TestMethod]
    [DataRow("upload-file")]
    [DataRow("upload-file-control")]
    public async Task Upload_Invisible(string type)
    {
        await BuildJsonAsync(UIJson.SerializeWith(Create(type), new { visible = false }));
        await AssertSnapshotAsync(type, "invisible");
    }

    [TestMethod]
    [DataRow("upload-file")]
    [DataRow("upload-file-control")]
    public async Task Upload_WithTool(string type)
    {
        await BuildAsync(Create(type), options: new { tool = TestTool });
        await AssertSnapshotAsync(type, "with-tool");
    }

    [TestMethod]
    [DataRow("upload-file")]
    [DataRow("upload-file-control")]
    public async Task Upload_InFormContainer(string type)
    {
        await BuildAsync(Create(type), setup: "h => $('#host').addClass('webgis-form-container')");
        await AssertSnapshotAsync(type, "form-container");
    }

    [TestMethod]
    [DataRow("upload-file")]
    [DataRow("upload-file-control")]
    public async Task Upload_Twice(string type)
    {
        // second element with the same id/field name
        await BuildAsync([Create(type), Create(type)]);
        await AssertSnapshotAsync(type, "twice");
    }

    #endregion

    #region upload-file

    [TestMethod]
    public async Task UploadFile_Text()
    {
        // text is not part of UIUploadFile, but read by the builder
        await BuildJsonAsync(UIJson.SerializeWith(Create("upload-file"), new { text = "Datei <b>hochladen</b>" }));
        await AssertSnapshotAsync("upload-file", "text");
    }

    [TestMethod]
    public async Task UploadFile_ButtonClicked()
    {
        await BuildAsync(Create("upload-file"));
        await Page.EvaluateAsync(RecordFileInputClicks);
        await ClearCallsAsync();
        await TriggerAsync("button#upload", "click");

        var clicks = await Page.EvaluateAsync<string[]>("() => window.__fileInputClicks");
        CollectionAssert.AreEqual(new[] { "upload_file_input" }, clicks);
        await AssertSnapshotAsync("upload-file", "button-clicked");
    }

    [TestMethod]
    public async Task UploadFile_FileSelected()
    {
        await BuildAsync(Create("upload-file"), setup: "h => h.spy('webgis.tools.uploadFile')");
        await ClearCallsAsync();

        await Page.SetInputFilesAsync("#upload_file_input", new Microsoft.Playwright.FilePayload
        {
            Name = "test.txt",
            MimeType = "text/plain",
            Buffer = "hello"u8.ToArray()
        });
        await FlushTimersAsync();

        await AssertSnapshotAsync("upload-file", "file-selected");
    }

    [TestMethod]
    public async Task UploadFile_EmptyFileSelected()
    {
        await BuildAsync(Create("upload-file"), setup: "h => h.spy('webgis.tools.uploadFile')");
        await ClearCallsAsync();

        await Page.SetInputFilesAsync("#upload_file_input", new Microsoft.Playwright.FilePayload
        {
            Name = "empty.txt",
            MimeType = "text/plain",
            Buffer = []
        });
        await FlushTimersAsync();

        await AssertSnapshotAsync("upload-file", "empty-file-selected");
    }

    #endregion

    #region upload-file-control

    [TestMethod]
    public async Task UploadFileControl_PreviewClicked()
    {
        await BuildAsync(Create("upload-file-control"));
        await Page.EvaluateAsync(RecordFileInputClicks);
        await ClearCallsAsync();
        await TriggerAsync("#host .webgis-control-upload .webgis-input", "click");

        var clicks = await Page.EvaluateAsync<string[]>("() => window.__fileInputClicks");
        Assert.AreEqual(1, clicks.Length);
        await AssertSnapshotAsync("upload-file-control", "preview-clicked");
    }

    [TestMethod]
    public async Task UploadFileControl_Uploaded()
    {
        // the server response arrives on the event channel (= id of the file input)
        await BuildAsync(Create("upload-file-control"));
        await ClearCallsAsync();
        await Page.EvaluateAsync("() => { const id = $('#__webgis-upload-div-container input[type=file]').attr('id'); webgis.events.fire(id, { url: '/rest/upload?id=1' }); }");
        await FlushTimersAsync();

        await AssertSnapshotAsync("upload-file-control", "uploaded");
    }

    #endregion
}
