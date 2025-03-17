namespace cms.tools;
public class CommandLine
{
    public CommandLine(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "-c":
                case "--command":
                    this.Command = args[++i];
                    break;
                case "-id":
                case "--cmsid":
                    this.CmsId = args[++i];
                    break;
                case "-d":
                case "--deployment":
                    this.Deployment = args[++i];
                    break;
                case "-u":
                case "--username":
                    this.UserName = args[++i];
                    break;
                case "-o":
                case "--outputpath":
                    this.OutputPath = args[++i];
                    break;

            }
        }
    }

    public void Usage()
    {
        Console.WriteLine("Usage: cms.tools --c [command] -id [cms_id] -d {deployment} -u {username} -o {output path}");
        Console.WriteLine("       -c, --command    : tool command [deploy|solve-warning|clear|realod-scheme|export");
        Console.WriteLine("       -id, --cmsid     : the cms id (from _config/cms.config");
        Console.WriteLine("       optional:");
        Console.WriteLine("       -d, --deployment : the deployment name (from _config/cms.config");
        Console.WriteLine("       -u, --username   : the username (for logging) default: Environment.UserName");
        Console.WriteLine("       -o, --outputPath : the output path (eg. for exporting ZIP)");
    }

    public string Command { get; } = "";
    public string CmsId { get; } = "";
    public string Deployment { get; } = "";
    public string UserName { get; } = "";

    public string OutputPath { get; } = "";
}
