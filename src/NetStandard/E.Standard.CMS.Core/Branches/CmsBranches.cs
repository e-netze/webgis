using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Newtonsoft.Json;

namespace E.Standard.CMS.Core.Branches;

// Branch deploys of a cms xml are stored next to the original file:
//   {dir}/branches/{encoded-branch}/{xml-filename}
//   {dir}/branches/{encoded-branch}/{xml-filename}.deploy.json
// The same branch of different cms items (same target dir) shares the branch folder => everything is per xml file
// The api registers them as cms name "{cmsName}${encoded-branch}"
static public class CmsBranches
{
    public const string BranchesFolder = "branches";
    public const string DeployInfoSuffix = ".deploy.json";
    public const char CmsNameSeparator = '$';

    #region Encoding

    // reversible, file system and url safe (also on case insensitive file systems):
    // a-z, 0-9 and '-' are kept, everything else (including '_' and upper case letters) => _xx (utf-8 hex)
    static public string Encode(string branchName)
    {
        if (String.IsNullOrEmpty(branchName))
        {
            return String.Empty;
        }

        var sb = new StringBuilder();
        foreach (var b in Encoding.UTF8.GetBytes(branchName))
        {
            var c = (char)b;
            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-')
            {
                sb.Append(c);
            }
            else
            {
                sb.Append('_').Append(b.ToString("x2"));
            }
        }

        return sb.ToString();
    }

    static public string Decode(string encodedBranch)
    {
        if (!IsValidEncoded(encodedBranch))
        {
            throw new ArgumentException($"Invalid encoded branch name: {encodedBranch}");
        }

        var bytes = new List<byte>();
        for (int i = 0; i < encodedBranch.Length; i++)
        {
            var c = encodedBranch[i];
            if (c == '_')
            {
                bytes.Add(Convert.ToByte(encodedBranch.Substring(i + 1, 2), 16));
                i += 2;
            }
            else
            {
                bytes.Add((byte)c);
            }
        }

        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    static public bool IsValidEncoded(string encodedBranch)
    {
        if (String.IsNullOrEmpty(encodedBranch) || encodedBranch.Length > 200)
        {
            return false;
        }

        for (int i = 0; i < encodedBranch.Length; i++)
        {
            var c = encodedBranch[i];
            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-')
            {
                continue;
            }

            if (c == '_' && i + 2 < encodedBranch.Length && IsLowerHex(encodedBranch[i + 1]) && IsLowerHex(encodedBranch[i + 2]))
            {
                i += 2;
                continue;
            }

            return false;
        }

        return true;
    }

    static public string TryDecode(string encodedBranch)
    {
        try
        {
            return Decode(encodedBranch);
        }
        catch
        {
            return encodedBranch;
        }
    }

    private static bool IsLowerHex(char c) => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f');

    #endregion

    #region Cms Names

    static public string ToCmsName(string mainCmsName, string encodedBranch)
        => String.IsNullOrEmpty(encodedBranch)
            ? mainCmsName ?? String.Empty
            : $"{mainCmsName}{CmsNameSeparator}{encodedBranch}";

    // "default$abc" => ("default", "abc"); "default" => ("default", "")
    static public (string mainCmsName, string encodedBranch) SplitCmsName(string cmsName)
    {
        cmsName = cmsName ?? String.Empty;
        var pos = cmsName.IndexOf(CmsNameSeparator);

        return pos < 0
            ? (cmsName, String.Empty)
            : (cmsName.Substring(0, pos), cmsName.Substring(pos + 1));
    }

    static public bool IsBranchCmsName(string cmsName)
        => !String.IsNullOrEmpty(cmsName) && cmsName.Contains(CmsNameSeparator);

    #endregion

    #region Files

    static public string BranchesDirectory(string mainFilePath)
        => Path.Combine(new FileInfo(mainFilePath).Directory!.FullName, BranchesFolder);

    static public string BranchDirectory(string mainFilePath, string encodedBranch)
    {
        if (!IsValidEncoded(encodedBranch))
        {
            throw new ArgumentException($"Invalid encoded branch name: {encodedBranch}");
        }

        return Path.Combine(BranchesDirectory(mainFilePath), encodedBranch);
    }

    static public string BranchFilePath(string mainFilePath, string encodedBranch)
        => Path.Combine(BranchDirectory(mainFilePath, encodedBranch), new FileInfo(mainFilePath).Name);

    static public string DeployInfoFilePath(string mainFilePath, string encodedBranch)
        => BranchFilePath(mainFilePath, encodedBranch) + DeployInfoSuffix;

    // encoded names of all branches with a deployed xml file
    static public IEnumerable<string> FindBranches(string mainFilePath)
    {
        if (String.IsNullOrWhiteSpace(mainFilePath))
        {
            return Array.Empty<string>();
        }

        var directory = new DirectoryInfo(BranchesDirectory(mainFilePath));
        if (!directory.Exists)
        {
            return Array.Empty<string>();
        }

        var filename = new FileInfo(mainFilePath).Name;

        return directory.GetDirectories()
                        .Where(d => IsValidEncoded(d.Name) && File.Exists(Path.Combine(d.FullName, filename)))
                        .Select(d => d.Name)
                        .OrderBy(n => n)
                        .ToArray();
    }

    static public IEnumerable<CmsBranchDeployInfo> ReadDeployInfos(string mainFilePath)
        => FindBranches(mainFilePath).Select(b => ReadDeployInfo(mainFilePath, b)).ToArray();

    static public CmsBranchDeployInfo ReadDeployInfo(string mainFilePath, string encodedBranch)
    {
        var branchFile = new FileInfo(BranchFilePath(mainFilePath, encodedBranch));
        CmsBranchDeployInfo info = null;

        try
        {
            var infoFile = DeployInfoFilePath(mainFilePath, encodedBranch);
            if (File.Exists(infoFile))
            {
                info = JsonConvert.DeserializeObject<CmsBranchDeployInfo>(File.ReadAllText(infoFile));
            }
        }
        catch { }

        info ??= new CmsBranchDeployInfo();
        info.EncodedBranch = encodedBranch;
        if (String.IsNullOrEmpty(info.Branch))
        {
            info.Branch = TryDecode(encodedBranch);
        }
        if (info.Date == null && branchFile.Exists)
        {
            info.Date = branchFile.LastWriteTimeUtc;
        }

        return info;
    }

    static public void WriteBranch(string mainFilePath, CmsBranchDeployInfo info, Action<string> writeXml)
    {
        var directory = new DirectoryInfo(BranchDirectory(mainFilePath, info.EncodedBranch));
        if (!directory.Exists)
        {
            directory.Create();
        }

        writeXml(BranchFilePath(mainFilePath, info.EncodedBranch));
        File.WriteAllText(DeployInfoFilePath(mainFilePath, info.EncodedBranch), JsonConvert.SerializeObject(info, Formatting.Indented));
    }

    // removes only the files of this xml; the folder is removed, if no other cms xml is left
    static public bool DeleteBranch(string mainFilePath, string encodedBranch)
    {
        var branchFile = new FileInfo(BranchFilePath(mainFilePath, encodedBranch));
        var infoFile = new FileInfo(DeployInfoFilePath(mainFilePath, encodedBranch));
        var existed = branchFile.Exists;

        if (branchFile.Exists)
        {
            branchFile.Delete();
        }
        if (infoFile.Exists)
        {
            infoFile.Delete();
        }

        var directory = branchFile.Directory!;
        if (directory.Exists && !directory.EnumerateFileSystemInfos().Any())
        {
            directory.Delete();
        }

        return existed;
    }

    #endregion
}

public class CmsBranchDeployInfo
{
    [JsonProperty("branch")]
    [System.Text.Json.Serialization.JsonPropertyName("branch")]
    public string Branch { get; set; }

    [JsonProperty("encoded_branch")]
    [System.Text.Json.Serialization.JsonPropertyName("encoded_branch")]
    public string EncodedBranch { get; set; }

    [JsonProperty("user")]
    [System.Text.Json.Serialization.JsonPropertyName("user")]
    public string User { get; set; }

    [JsonProperty("commit")]
    [System.Text.Json.Serialization.JsonPropertyName("commit")]
    public string Commit { get; set; }

    [JsonProperty("date")]
    [System.Text.Json.Serialization.JsonPropertyName("date")]
    public DateTime? Date { get; set; }
}

// item of the branch list (rest/branches): one entry per branch over all cms; "" => main
public class CmsBranchListItem
{
    [JsonProperty("encoded")]
    [System.Text.Json.Serialization.JsonPropertyName("encoded")]
    public string EncodedBranch { get; set; }

    [JsonProperty("name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Branch { get; set; }

    [JsonProperty("user")]
    [System.Text.Json.Serialization.JsonPropertyName("user")]
    public string User { get; set; }

    [JsonProperty("commit")]
    [System.Text.Json.Serialization.JsonPropertyName("commit")]
    public string Commit { get; set; }

    [JsonProperty("date")]
    [System.Text.Json.Serialization.JsonPropertyName("date")]
    public DateTime? Date { get; set; }

    [JsonProperty("cms_count")]
    [System.Text.Json.Serialization.JsonPropertyName("cms_count")]
    public int CmsCount { get; set; }
}
