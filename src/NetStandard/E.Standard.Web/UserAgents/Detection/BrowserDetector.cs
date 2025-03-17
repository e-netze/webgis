namespace E.Standard.Web.UserAgents.Detection;

using E.Standard.Web.UserAgents.Browsers;
using System;

/// <summary>
/// A class to get browser and platform information.
/// </summary>
public class BrowserDetector
{
    public IBrowser? GetBrowser(string userAgent)
    {
        if (!String.IsNullOrEmpty(userAgent))
        {
            return Detector.GetBrowser(userAgent.AsSpan());
        }

        return null;
    }
}
