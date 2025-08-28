using E.Standard.Platform;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace WebGIS.API.MCP.Tools;

public class WebGISApiTools
{
    [McpServerTool]
    [Description("""
        Returns the current version of the WebGIS API.
        The major number represents the main product version.
        The minor number corresponds to the last two digits of the year when the version was published.
        The patch number indicates the calendar week of release, with an additional two digit sequential number if there are multiple releases in the same week.
        """)]
        
    public Version GetWebGISVersion() => WebGISVersion.Version;


}