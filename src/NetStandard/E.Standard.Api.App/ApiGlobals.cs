using E.Standard.WebMapping.Core;
using System;
using System.IO;

namespace E.Standard.Api.App;

static public class ApiGlobals
{
    #region Set in WebApp.Init()

    static public SpatialReferenceStore SRefStore = null;
    static public string AppConfigPath = String.Empty;
    static public string AppEtcPath = String.Empty;

    static public string AppRootPath = String.Empty;
    static public string AppPluginsPath = String.Empty;
    static public string WWWRootPath = String.Empty;
    static public string AppAssemblyPath = String.Empty;

    //static public string OutputPath = String.Empty;
    //static public string OutputUrl = String.Empty;

    static public string LogPath = String.Empty;
    static public string LogPerformanceColumns = String.Empty;

    static public bool IsDevelopmentEnvironment = false;

    static public CustomStaticFilesLocation CustomStaticFilesLocation = CustomStaticFilesLocation.InContainer;

    public const string MessageQueuePrefix = "webgis-api-queue-";
    static public string MessageQueueName = $"{MessageQueuePrefix}{Guid.NewGuid().ToString("N").ToLower()}";

    public static int HttpClientDefaultTimeoutSeconds = 0;

    public static int MaxFeatureHoverHighlightVerticesCount = 1000;

    #endregion

    static public bool IsInDeveloperMode
    {
        get
        {
            try
            {
                var path = Path.Combine(AppEtcPath, "developer-mode.xml");
                if (!new System.IO.FileInfo(path).Exists)
                {
                    return false;
                }

                return File.ReadAllText(path).Replace(" ", "").Trim().ToLower() == "e^(i*pi)+1=0";  // Eulersche Identität
            }
            catch { }

            return false;
        }
    }

    #region Logging

    public static string LoggingType { get; set; }

    #endregion
}