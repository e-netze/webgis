namespace E.Standard.Web.UserAgents.Detection;

using System;

using E.Standard.Web.UserAgents.Browsers;

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
