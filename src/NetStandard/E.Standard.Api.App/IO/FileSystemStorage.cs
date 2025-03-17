using E.Standard.Api.App.Extensions;
using E.Standard.Json;
using E.Standard.Security.Cryptography;
using E.Standard.WebGIS.Core.Extensions;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.IO;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace E.Standard.Api.App.IO;

public class FileSystemStorage : IStorage, IStorage2
{
    private const string EncPrefix = "storage:";  // use when openenig a projekt per link

    public FileSystemStorage(Bridge bridge, string storagePath)
    {
        if (String.IsNullOrEmpty(storagePath))
        {
            throw new ArgumentException("storagePath not set");
        }

        this.Bridge = bridge;
        this._rootPath = storagePath;
    }

    #region Properties

    private Bridge Bridge { get; set; }

    private readonly string _rootPath = String.Empty;

    private Type CurrentToolType(Bridge bridge, Type currentToolType)
    {
        if (currentToolType != null && !currentToolType.Equals(Bridge.CurrentTool.GetType()))
        {
            foreach (ToolStorageFriendTypeAttribute friendTypeAttribute in currentToolType.GetCustomAttributes(typeof(ToolStorageFriendTypeAttribute), false))
            {
                if (friendTypeAttribute.FriendType != null && friendTypeAttribute.FriendType.Equals(bridge.CurrentTool.GetType()))
                {
                    return currentToolType;
                }
            }
        }

        return bridge.CurrentTool.GetType();
    }

    private string CurrentPath(Type currentToolType = null)
    {
        var type = CurrentToolType(Bridge, currentToolType);

        //return _rootPath + @"/" + Bridge.CurrentLogonUsername.Replace(@"/",".") + @"/" + Bridge.CurrentTool.GetType().ToString().ToLower() + @"/";
        string toolStorageId = type.ToToolId(); //  .ToString().ToLower();

        var toolStorageIdAttribute = type.GetCustomAttribute<ToolStorageIdAttribute>();
        if (toolStorageIdAttribute != null)
        {
            toolStorageId = toolStorageIdAttribute.ToolStorageId;
        }

        string currentPath = PathFromToolStorageId(toolStorageId);

        bool toolIsolated = type.GetCustomAttributeOrDefault<ToolStorageIsolatedUserAttribute>().IsUserIsolated;
        if (toolIsolated)
        {
            if (currentPath.Contains("{user}"))
            {
                currentPath = currentPath.Replace("{user}", Bridge.CurrentUser.Username.Username2StorageDirectory());
            }
            else
            {
                currentPath += Bridge.CurrentUser.Username.Username2StorageDirectory() + @"/";
            }
        }

        var currentTool = Bridge.CurrentTool;
        if (!type.Equals(currentTool.GetType()))
        {
            currentTool = (IApiButton)Activator.CreateInstance(type);
        }
        if (currentTool is IStorageInteractions)
        {
            IStorageInteractions si = (IStorageInteractions)currentTool;

            if (currentPath.Contains("{2}") && currentPath.Contains("{1}") && currentPath.Contains("{0}"))
            {
                currentPath = String.Format(currentPath, si.StoragePathFormatParameter(Bridge, 0), si.StoragePathFormatParameter(Bridge, 1), si.StoragePathFormatParameter(Bridge, 2));
            }
            else if (currentPath.Contains("{1}") && currentPath.Contains("{0}"))
            {
                currentPath = String.Format(currentPath, si.StoragePathFormatParameter(Bridge, 0), si.StoragePathFormatParameter(Bridge, 1));
            }
            else if (currentPath.Contains("{0}"))
            {
                currentPath = String.Format(currentPath, si.StoragePathFormatParameter(Bridge, 0));
            }
        }

        return currentPath;
    }

    private string RawCurrentPath(Type currentToolType = null)
    {
        var type = CurrentToolType(Bridge, currentToolType);

        //return _rootPath + @"/" + Bridge.CurrentLogonUsername.Replace(@"/",".") + @"/" + Bridge.CurrentTool.GetType().ToString().ToLower() + @"/";
        string toolStorageId = Bridge.CurrentTool.GetType().ToToolId();  //.ToString().ToLower();
        object[] toolStorageIdAttributes = type.GetCustomAttributes(typeof(ToolStorageIdAttribute), true);
        if (toolStorageIdAttributes != null && toolStorageIdAttributes.Length == 1)
        {
            toolStorageId = ((ToolStorageIdAttribute)toolStorageIdAttributes[0]).ToolStorageId;
        }

        string currentPath = PathFromToolStorageId(toolStorageId);

        return currentPath.Replace("{0}", "").Replace("{1}", "").Replace("{2}", "");
    }

    private SecureDirectoryInfo CurrentPathInfo(bool create = true, Type currentToolType = null)
    {
        SecureDirectoryInfo di = new SecureDirectoryInfo(this.Bridge, CurrentPath(currentToolType));
        if (create && !di.Exists)
        {
            di.Create();
        }

        return di;
    }

    private SecureDirectoryInfo GetCurrentIndexPathInfo(bool create = false)
    {
        string toolStorageId = Bridge.CurrentTool.GetType().ToToolId(); //.ToString().ToLower();

        var toolStorageIdAttribute = Bridge.CurrentTool.GetType().GetCustomAttribute<ToolStorageIdAttribute>();
        if (toolStorageIdAttribute != null)
        {
            toolStorageId = toolStorageIdAttribute.ToolStorageId.Replace("{0}", "").Replace("{1}", "").Replace("{2}", "");
        }

        string currentPath = PathFromToolStorageId(toolStorageId);

        SecureDirectoryInfo di = new SecureDirectoryInfo(this.Bridge, currentPath);
        if (create && !di.Exists)
        {
            di.Create();
        }

        return di;
    }

    private SecureDirectoryInfo CurrentRawPathInfo(string subDir)
    {
        string path = this.RawCurrentPath(null) + subDir;

        bool isUnc = path.StartsWith(@"\\");

        while (path.Contains(@"\\"))
        {
            path = path.Replace(@"\\", @"\");
        }

        while (path.Contains("//"))
        {
            path = path.Replace("//", "/");
        }

        if (isUnc)
        {
            path = @"\" + path;
        }

        return new SecureDirectoryInfo(this.Bridge, path);
    }

    private void ValidateName(string name)
    {
        string regexString = $"[{Regex.Escape(new string(Path.GetInvalidPathChars()))}]";
        Regex containsABadCharacter = new Regex(regexString);

        if (containsABadCharacter.IsMatch(name))
        {
            throw new ArgumentException($"The charakters {new string(Path.GetInvalidPathChars())} are not allowed in names");
        }
    }

    private string FilenameWithExtentsion(string name, StorageBlobType type)
    {
        if (name.Contains(@"\"))
        {
            name = name.Replace(@"\", "/");
        }

        while (name.Contains("//"))
        {
            name = name.Replace("//", "/");
        }

        switch (type)
        {
            case StorageBlobType.Data:
                return $"{name}.dat";
            case StorageBlobType.Metadata:
                return $"{name}.mta";
            case StorageBlobType.Acl:
                return $"{name}.acl";
            default:
                return $"{name}.blb";
        }
    }

    private string PathFromToolStorageId(string toolStorageId)
    {
        if (toolStorageId.StartsWith("/") || toolStorageId.IndexOf(":") == 1 || toolStorageId.StartsWith(@"\\"))  // c:/temp or /opt/storage or \\dfs\storage
        {
            return toolStorageId + "/";
        }

        return _rootPath + @"/" + toolStorageId + @"/";
    }

    #endregion

    #region Members


    #endregion

    #region IStorage Member

    public bool Save(string name, byte[] data, StorageBlobType type = StorageBlobType.Normal)
    {
        using (var impersonateContext = this.Bridge.Impersonator().ImpersonateContext(true))
        {
            var pathInfo = this.CurrentPathInfo();

            if (name.StartsWith(EncPrefix))
            {
                name = name.Substring(EncPrefix.Length, name.Length - EncPrefix.Length);
                name = Bridge.SecurityDecryptString(name, (int)CustomPasswords.ApiStoragePassword, CryptoStrength.AES256);
                if (!String.IsNullOrWhiteSpace(Bridge.CurrentUser.Username))
                {
                    pathInfo = pathInfo.Parent;
                }
            }

            this.ValidateName(name);

            SecureFileInfo fi = new SecureFileInfo(this.Bridge, Path.Combine(pathInfo.FullName, FilenameWithExtentsion(name, type)));
            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }

            File.WriteAllBytes(fi.FullName, data);
        }

        return true;
    }

    public bool Save(string name, string dataString, StorageBlobType type = StorageBlobType.Normal)
    {
        using (var impersonateContext = this.Bridge.Impersonator().ImpersonateContext(true))
        {
            var pathInfo = this.CurrentPathInfo();

            if (name.StartsWith(EncPrefix))
            {
                name = name.Substring(EncPrefix.Length, name.Length - EncPrefix.Length);
                name = Bridge.SecurityDecryptString(name, (int)CustomPasswords.ApiStoragePassword, CryptoStrength.AES256);
                if (!String.IsNullOrWhiteSpace(Bridge.CurrentUser.Username))
                {
                    pathInfo = pathInfo.Parent;
                }
            }

            this.ValidateName(name);

            SecureFileInfo fi = new SecureFileInfo(this.Bridge, Path.Combine(pathInfo.FullName, FilenameWithExtentsion(name, type)));
            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }

            File.WriteAllText(fi.FullName, dataString);
        }

        return true;
    }

    public bool Exists(string name, StorageBlobType type = StorageBlobType.Normal)
    {
        using (var impersonateContext = this.Bridge.Impersonator().ImpersonateContext(true))
        {
            var pathInfo = this.CurrentPathInfo(false);

            if (name.StartsWith(EncPrefix))
            {
                name = name.Substring(EncPrefix.Length, name.Length - EncPrefix.Length);
                name = Bridge.SecurityDecryptString(name, (int)CustomPasswords.ApiStoragePassword, CryptoStrength.AES256);
                if (!String.IsNullOrWhiteSpace(Bridge.CurrentUser.Username))
                {
                    pathInfo = pathInfo.Parent;
                }
            }

            //Console.WriteLine($"Storage: { name } exists in { pathInfo.FullName }");
            return new SecureFileInfo(this.Bridge, Path.Combine(pathInfo.FullName, FilenameWithExtentsion(name, type))).Exists;
        }
    }

    public string OwnerName(string name)
    {
        using (var impersonateContext = this.Bridge.Impersonator().ImpersonateContext(true))
        {
            if (name.StartsWith(EncPrefix))
            {
                name = name.Substring(EncPrefix.Length, name.Length - EncPrefix.Length);
                name = Bridge.SecurityDecryptString(name, (int)CustomPasswords.ApiStoragePassword, CryptoStrength.AES256);
            }

            name = name.Replace("\\", "/");
            if (!name.Contains("/"))
            {
                return "";
            }

            var ownerName = name.Split("/").First();

            return ownerName.StorageDirectory2Username();
        }
    }

    public byte[] Load(string name, StorageBlobType type = StorageBlobType.Normal)
    {
        using (var impersonateContext = this.Bridge.Impersonator().ImpersonateContext(true))
        {
            var pathInfo = this.CurrentPathInfo(false);

            if (name.StartsWith(EncPrefix))
            {
                name = name.Substring(EncPrefix.Length, name.Length - EncPrefix.Length);
                name = Bridge.SecurityDecryptString(name, (int)CustomPasswords.ApiStoragePassword, CryptoStrength.AES256);
                if (!String.IsNullOrWhiteSpace(Bridge.CurrentUser.Username))
                {
                    pathInfo = pathInfo.Parent;
                }
            }

            if (!pathInfo.Exists)
            {
                return null;
            }

            SecureFileInfo fi = new SecureFileInfo(this.Bridge, Path.Combine(pathInfo.FullName, FilenameWithExtentsion(name, type)));

            if (!fi.Exists)
            {
                return null;
            }

            return File.ReadAllBytes(fi.FullName);
        }
    }

    public string LoadString(string name, StorageBlobType type = StorageBlobType.Normal)
    {
        using (var impersonateContext = this.Bridge.Impersonator().ImpersonateContext(true))
        {
            var pathInfo = this.CurrentPathInfo(false);

            if (name.StartsWith(EncPrefix))
            {
                name = name.Substring(EncPrefix.Length, name.Length - EncPrefix.Length);
                name = Bridge.SecurityDecryptString(name, (int)CustomPasswords.ApiStoragePassword, CryptoStrength.AES256);
                if (!String.IsNullOrWhiteSpace(Bridge.CurrentUser.Username))
                {
                    pathInfo = pathInfo.Parent;
                }
            }

            if (!pathInfo.Exists)
            {
                //Console.WriteLine($"LoadString: {pathInfo.FullName} not exists");
                return String.Empty;
            }

            SecureFileInfo fi = new SecureFileInfo(this.Bridge,
                Path.Combine(pathInfo.FullName, FilenameWithExtentsion(name, type)));

            if (!fi.Exists)
            {
                //Console.WriteLine($"LoadString: {fi.FullName} not exists");
                return null;
            }

            return File.ReadAllText(fi.FullName);
        }
    }

    public Dictionary<string, string> LoadStrings(string[] names, StorageBlobType type = StorageBlobType.Normal)
    {
        using (var impersonateContext = this.Bridge.Impersonator().ImpersonateContext(true))
        {
            var pathInfo = this.CurrentPathInfo(false);

            Dictionary<string, string> ret = new Dictionary<string, string>();
            foreach (string n in names)
            {
                string name = n;

                if (name.StartsWith(EncPrefix))
                {
                    name = name.Substring(EncPrefix.Length, name.Length - EncPrefix.Length);
                    name = Bridge.SecurityDecryptString(name, (int)CustomPasswords.ApiStoragePassword, CryptoStrength.AES256);
                    if (!String.IsNullOrWhiteSpace(Bridge.CurrentUser.Username))
                    {
                        pathInfo = pathInfo.Parent;
                    }
                }

                if (!pathInfo.Exists)
                {
                    continue;
                }

                SecureFileInfo fi = new SecureFileInfo(this.Bridge,
                    Path.Combine(pathInfo.FullName, FilenameWithExtentsion(name, type)));

                if (!fi.Exists)
                {
                    continue;
                }

                ret.Add(name, File.ReadAllText(fi.FullName));
            }

            return ret;
        }
    }

    public bool Remove(string name, StorageBlobType type = StorageBlobType.Normal, bool recursive = false)
    {
        using (var impersonateContext = this.Bridge.Impersonator().ImpersonateContext(true))
        {
            var pathInfo = this.CurrentPathInfo(false);

            if (name.StartsWith(EncPrefix))
            {
                name = name.Substring(EncPrefix.Length, name.Length - EncPrefix.Length);
                name = Bridge.SecurityDecryptString(name, (int)CustomPasswords.ApiStoragePassword, CryptoStrength.AES256);
                if (!String.IsNullOrWhiteSpace(Bridge.CurrentUser.Username))
                {
                    pathInfo = pathInfo.Parent;
                }
            }

            while (name.Contains(@"\\"))
            {
                name = name.Replace(@"\\", @"\");
            }

            while (name.Contains(@"//"))
            {
                name = name.Replace(@"//", @"/");
            }

            foreach (string slash in new string[] { "/", @"\" })
            {
                if (name.Contains(slash))
                {
                    int lastPos = name.LastIndexOf(slash);
                    pathInfo = new SecureDirectoryInfo(this.Bridge, pathInfo.FullName + @"/" + name.Substring(0, lastPos));
                    name = name.Substring(lastPos + 1, name.Length - lastPos - 1);
                }
            }

            if (!pathInfo.Exists)
            {
                return true;
            }

            if (type == StorageBlobType.Folder)
            {
                SecureDirectoryInfo folder = new SecureDirectoryInfo(this.Bridge, Path.Combine(pathInfo.FullName, name));
                folder.Remove(recursive);

                return true;
            }
            else
            {

                SecureFileInfo fi = new SecureFileInfo(this.Bridge,
                    Path.Combine(pathInfo.FullName, FilenameWithExtentsion(name, type)));

                fi.Delete();

                if (recursive)
                {
                    foreach (var rfiles in pathInfo.GetFiles(name + ".*"))
                    {
                        rfiles.Delete();
                    }
                }


                return true;
            }
        }
    }

    public string[] GetUserAccess(string name, Type storageToolType = null)
    {
        using (var impersonateContext = this.Bridge.Impersonator().ImpersonateContext(true))
        {
            var pathInfo = this.CurrentPathInfo(false, storageToolType);

            if (name.StartsWith(EncPrefix))
            {
                name = name.Substring(EncPrefix.Length, name.Length - EncPrefix.Length);
                name = Bridge.SecurityDecryptString(name, (int)CustomPasswords.ApiStoragePassword, CryptoStrength.AES256);
                if (!String.IsNullOrWhiteSpace(Bridge.CurrentUser.Username))
                {
                    pathInfo = pathInfo.Parent;
                }
            }

            while (name.Contains(@"\\"))
            {
                name = name.Replace(@"\\", @"\");
            }

            name = name.Replace("\\", "/");

            if (name.Contains(@"/"))
            {
                int lastPos = name.LastIndexOf(@"/");
                pathInfo = new SecureDirectoryInfo(this.Bridge, pathInfo.FullName + @"/" + name.Substring(0, lastPos));
                name = name.Substring(lastPos + 1, name.Length - lastPos - 1);
            }

            var directoryInfo = new SecureDirectoryInfo(this.Bridge, pathInfo.FullName + @"/" + name);
            if (directoryInfo.Exists)
            {
                pathInfo = directoryInfo;
                name = "_";   // Directory -> _.acl
            }

            var aclFile = new FileInfo(pathInfo.FullName + @"/" + name + ".acl");
            if (aclFile.Exists)
            {
                return JSerializer.Deserialize<string[]>(File.ReadAllText(aclFile.FullName));
            }
        }

        return null;
    }

    public bool SetUserAccess(string name, string[] access, Type storageToolType = null)
    {
        using (var impersonateContext = this.Bridge.Impersonator().ImpersonateContext(true))
        {
            var pathInfo = this.CurrentPathInfo(false, storageToolType);

            if (name.StartsWith(EncPrefix))
            {
                name = name.Substring(EncPrefix.Length, name.Length - EncPrefix.Length);
                name = Bridge.SecurityDecryptString(name, (int)CustomPasswords.ApiStoragePassword, CryptoStrength.AES256);
                if (!String.IsNullOrWhiteSpace(Bridge.CurrentUser.Username))
                {
                    pathInfo = pathInfo.Parent;
                }
            }

            while (name.Contains(@"\\"))
            {
                name = name.Replace(@"\\", @"\");
            }

            name = name.Replace("\\", "/");

            if (name.Contains(@"/"))
            {
                int lastPos = name.LastIndexOf(@"/");
                pathInfo = new SecureDirectoryInfo(this.Bridge, pathInfo.FullName + @"/" + name.Substring(0, lastPos));
                name = name.Substring(lastPos + 1, name.Length - lastPos - 1);
            }

            var directoryInfo = new SecureDirectoryInfo(this.Bridge, pathInfo.FullName + @"/" + name);
            if (directoryInfo.Exists)
            {
                pathInfo = directoryInfo;
                name = "_";   // Directory -> _.acl
            }

            var aclFile = new FileInfo(pathInfo.FullName + @"/" + name + ".acl");

            access = access.Where(a => !String.IsNullOrWhiteSpace(a))
                           .ToArray();

            if (access.Length > 0)
            {
                File.WriteAllText(aclFile.FullName, JSerializer.Serialize(access));
            }
            else
            {
                File.Delete(aclFile.FullName);
            }

            return true;
        }
    }

    public string[] GetNames(bool includeFiles = true, bool includeDirectories = true, StorageBlobType blobType = StorageBlobType.Normal)
    {
        List<string> names = new List<string>();

        using (var impersonateContext = this.Bridge.Impersonator().ImpersonateContext(true))
        {
            var pathInfo = this.CurrentPathInfo(false);

            if (pathInfo.Exists)
            {
                if (includeFiles)
                {
                    foreach (SecureFileInfo fi in pathInfo.GetFiles(FilenameWithExtentsion("*", blobType)))
                    {
                        if (fi.Name.StartsWith("_") || fi.Name.StartsWith("."))
                        {
                            continue;
                        }

                        names.Add(fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length));
                    }
                }

                if (includeDirectories)
                {
                    foreach (var di in pathInfo.GetDirectories())
                    {
                        if (di.Name.StartsWith("_") || di.Name.StartsWith("."))
                        {
                            continue;
                        }

                        names.Add(di.Name);
                    }
                }
            }
        }

        // sort alphabetic first
        names.Sort();

        return names.ToArray();
    }

    public bool SetItemOrder(string[] order)
    {
        using (var impersonateContext = this.Bridge.Impersonator().ImpersonateContext(true))
        {
            var pathInfo = this.CurrentPathInfo(false);
            if (pathInfo.Exists)
            {
                var fi = new SecureFileInfo(this.Bridge, $"{pathInfo.FullName}/.itemorder");
                try
                {
                    fi.Delete();
                }
                catch { }

                File.WriteAllLines(fi.FullName, order);
            }
        }

        return true;
    }

    public string[] GetItemOrder()
    {
        using (var impersonateContext = this.Bridge.Impersonator().ImpersonateContext(true))
        {
            try
            {
                var pathInfo = this.CurrentPathInfo(false);
                var fi = new SecureFileInfo(this.Bridge, $"{pathInfo.FullName}/.itemorder");
                if (fi.Exists)
                {
                    return File.ReadAllLines(fi.FullName);
                }
            }
            catch { }

            return null;
        }
    }

    public Dictionary<string, string[]> GetAllNames()
    {
        Dictionary<string, string[]> allNames = new Dictionary<string, string[]>();

        using (var impersonateContext = this.Bridge.Impersonator().ImpersonateContext(true))
        {
            var pathInfo = this.CurrentPathInfo(false);
            if (!String.IsNullOrWhiteSpace(this.Bridge.CurrentUser.Username))
            {
                pathInfo = pathInfo.Parent;
            }

            if (pathInfo.Exists)
            {
                foreach (SecureDirectoryInfo userDirectory in pathInfo.GetDirectories())
                {
                    List<string> names = new List<string>();
                    foreach (SecureFileInfo fi in userDirectory.GetFiles(FilenameWithExtentsion("*", StorageBlobType.Normal)))
                    {
                        names.Add(fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length));
                    }

                    names.Sort();
                    allNames.Add(userDirectory.Name.StorageDirectory2Username(), names.ToArray());
                }
            }
        }

        return allNames;
    }

    public string[] GetDirectoryNames(string subDir = "", string filter = "")
    {
        List<string> names = new List<string>();
        using (var impersonateContext = this.Bridge.Impersonator().ImpersonateContext(true))
        {
            var pathInfo = this.CurrentRawPathInfo(subDir);
            if (pathInfo.Exists)
            {
                foreach (SecureDirectoryInfo di in String.IsNullOrWhiteSpace(filter) ? pathInfo.GetDirectories() : pathInfo.GetDirectories(filter.Replace("%", "*")))
                {
                    names.Add(di.Name);
                }
            }
        }
        return names.ToArray();
    }

    public string CreateEncryptedName(string user, string name)
    {
        name = Bridge.SecurityEncryptString(
            user.Username2StorageDirectory() + @"\\" + name,
            (int)CustomPasswords.ApiStoragePassword,
            CryptoStrength.AES256);

        return EncPrefix + name;
    }

    public string AppendToName(string name, string appendix)
    {
        if (name.StartsWith(EncPrefix))
        {
            name = name.Substring(EncPrefix.Length, name.Length - EncPrefix.Length);
            name = Bridge.SecurityDecryptString(name, (int)CustomPasswords.ApiStoragePassword, CryptoStrength.AES256);
            if (name.Contains(@"\\"))
            {
                int pos = name.IndexOf(@"\\");
                string user = name.Substring(0, pos);
                name = name.Substring(pos + 2, name.Length - pos - 2);

                return CreateEncryptedName(user, $"{name}{appendix}");
            }
        }

        return $"{name}{appendix}";
    }

    public string DecryptName(string name)
    {
        if (name.StartsWith(EncPrefix))
        {
            name = name.Substring(EncPrefix.Length, name.Length - EncPrefix.Length);
            name = Bridge.SecurityDecryptString(name, (int)CustomPasswords.ApiStoragePassword, CryptoStrength.AES256);
            if (name.Contains(@"\\"))
            {
                int pos = name.IndexOf(@"\\");
                name = name.Substring(pos + 2, name.Length - pos - 2);
            }
        }

        return name;
    }

    public bool SetUniqueIndexItem(string indexName, string key, string val)
    {
        using (var impersonateContext = this.Bridge.Impersonator().ImpersonateContext(true))
        {
            var pathInfo = this.GetCurrentIndexPathInfo(false);

            indexName = indexName.Username2StorageDirectory(); // Index ist oft Username

            var indexDirectory = new SecureDirectoryInfo(this.Bridge, pathInfo.FullName + @"/_index/" + indexName);
            if (!indexDirectory.Exists)
            {
                indexDirectory.Create();
            }

            File.WriteAllText(indexDirectory.FullName + @"/" + key + ".idx", val);

            return true;
        }
    }

    public string GetUniqueIndexItem(string indexName, string key)
    {
        using (var impersonateContext = this.Bridge.Impersonator().ImpersonateContext(true))
        {
            var pathInfo = this.GetCurrentIndexPathInfo(false);

            indexName = indexName.Username2StorageDirectory(); // Index ist oft Username

            var fi = new SecureFileInfo(this.Bridge, pathInfo.FullName + @"/_index/" + indexName + @"/" + key + ".idx");
            if (!fi.Exists)
            {
                return String.Empty;
            }

            return File.ReadAllText(fi.FullName);
        }
    }

    public string[] GetUniqueIndexKeys(string indexName)
    {
        using (var impersonateContext = this.Bridge.Impersonator().ImpersonateContext(true))
        {
            var pathInfo = this.GetCurrentIndexPathInfo(false);

            indexName = indexName.Username2StorageDirectory(); // Index ist oft Username

            List<string> keys = new List<string>();
            var di = new SecureDirectoryInfo(this.Bridge, pathInfo.FullName + @"/_index/" + indexName + @"/");
            if (!di.Exists)
            {
                return keys.ToArray();
            }

            foreach (var fi in di.GetFiles("*.idx").Where(f => f.Extension.ToLower() == ".idx"))  //.NET Framework erkennt sonst auch "*.idx__" files, mit Core werden nur *.idx erkannt :/
            {
                keys.Add(fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length));
            }
            return keys.ToArray();
        }
    }

    public bool RemoveUniqueIndexItem(string indexName, string key)
    {
        using (var impersonateContext = this.Bridge.Impersonator().ImpersonateContext(true))
        {
            var pathInfo = this.GetCurrentIndexPathInfo(false);

            indexName = indexName.Username2StorageDirectory(); // Index ist oft Username

            var fi = new SecureFileInfo(this.Bridge, pathInfo.FullName + @"/_index/" + indexName + @"/" + key + ".idx");
            fi.Delete();

            var di = new SecureDirectoryInfo(this.Bridge, pathInfo.FullName + @"/_index/" + indexName);
            if (di.GetFiles("*.idx").Count() == 0)
            {
                di.Remove(false);
            }

            return true;
        }
    }

    public string SaveTempDataString(string dataString, int expireMinutes)
    {
        using (var impersonateContext = this.Bridge.Impersonator().ImpersonateContext(true))
        {
            var pathInfo = this.CurrentPathInfo(true);

            string name = "temp" + Guid.NewGuid().ToString("N").ToLower();

            SecureFileInfo fi = new SecureFileInfo(this.Bridge, pathInfo.FullName + @"/" + name + ".tmp");
            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }

            File.WriteAllText(fi.FullName, dataString);

            return name;
        }
    }

    public string LoadTempDataString(string id)
    {
        using (var impersonateContext = this.Bridge.Impersonator().ImpersonateContext(true))
        {
            var pathInfo = this.CurrentPathInfo(true);

            SecureFileInfo fi = new SecureFileInfo(this.Bridge, pathInfo.FullName + @"/" + id + ".tmp");
            if (fi.Exists)
            {
                return System.IO.File.ReadAllText(fi.FullName);
            }

            return String.Empty;
        }
    }

    #endregion

    #region IStorage2

    public string GetDecodedName(string encName)
    {
        if (encName.StartsWith(EncPrefix))
        {
            encName = encName.Substring(EncPrefix.Length, encName.Length - EncPrefix.Length);
            encName = Bridge.SecurityDecryptString(encName, (int)CustomPasswords.ApiStoragePassword, CryptoStrength.AES256);
        }

        return encName;
    }

    #endregion

    #region Helper

    #endregion

    #region HelperClasses

    public class AclSupport
    {
        protected bool CheckAcl(Bridge bridge, FileInfo fi)
        {
            FileInfo aclFile = new FileInfo(fi.FullName.Substring(0, fi.FullName.Length - fi.Extension.Length) + ".acl");
            if (aclFile.Exists)
            {
                string[] aclRoles = JSerializer.Deserialize<string[]>(File.ReadAllText(aclFile.FullName));
                if (aclRoles != null && aclRoles.Length > 0)
                {
                    if (aclRoles.Contains("*") || aclRoles.Contains("%"))
                    {
                        return true;
                    }

                    if (bridge.CurrentUser == null)
                    {
                        return false;
                    }

                    var currentUserName = bridge.CurrentUser.Username?.ToLower() ?? String.Empty;
                    if (currentUserName.Contains("::"))
                    {
                        string userNamePrefix = currentUserName.Substring(0, currentUserName.IndexOf("::") + 2);
                        if (aclRoles.Contains(userNamePrefix + "*") || aclRoles.Contains(userNamePrefix + "%"))
                        {
                            return true;
                        }
                    }

                    aclRoles = aclRoles.Select(m => m.ToLower()).ToArray();
                    var userRoles = bridge.CurrentUser.UserRoles != null ? bridge.CurrentUser.UserRoles.Select(m => m.ToLower()).ToArray() : new string[0];

                    return (aclRoles.Where(m =>
                    {
                        if (m == currentUserName)
                        {
                            return true;
                        }

                        if (userRoles.Contains(m))
                        {
                            return true;
                        }

                        if (WebGIS.Core.UserManagement.AllowWildcards == true && m.Contains("*"))
                        {
                            var pattern = WildCardToRegular(m);
                            if (Regex.IsMatch(currentUserName, pattern))
                            {
                                return true;
                            }

                            if (userRoles.Where(r => Regex.IsMatch(r, pattern)).Count() > 0)
                            {
                                return true;
                            }
                        }

                        return false;
                    }).FirstOrDefault() != null);

                    //return (aclRoles.Where(m => m == currentUserName || userRoles.Contains(m)).FirstOrDefault() != null);
                }
            }
            return true;
        }

        #region Wildcard Testing

        private static String WildCardToRegular(String value)
        {
            // If you want to implement both "*" and "?"
            //return "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$";

            // If you want to implement "*" only
            return "^" + Regex.Escape(value).Replace("\\*", ".*") + "$";
        }

        #endregion
    }

    public class SecureFileInfo : AclSupport
    {
        public SecureFileInfo(Bridge bridge, string path)
        {
            this.Bridge = bridge;
            this.Info = new FileInfo(path);
        }

        public SecureFileInfo(Bridge bridge, FileInfo fi)
        {
            this.Bridge = bridge;
            this.Info = fi;
        }

        private Bridge Bridge { get; set; }
        private FileInfo Info { get; set; }

        public bool Exists
        {
            get
            {
                if (Info.Exists)
                {
                    return CheckAcl(this.Bridge, this.Info);
                }

                return false;
            }
        }

        public string FullName { get { return this.Info.FullName; } }

        public string Name { get { return this.Info.Name; } }

        public string Extension { get { return this.Info.Extension; } }

        public void Delete()
        {
            if (this.Exists)
            {
                this.Info.Delete();
            }
        }

        public SecureDirectoryInfo Directory
        {
            get { return new SecureDirectoryInfo(this.Bridge, this.Info.Directory); }
        }
    }

    public class SecureDirectoryInfo : AclSupport
    {
        public SecureDirectoryInfo(Bridge bridge, string path)
        {
            this.Bridge = bridge;
            this.Info = new DirectoryInfo(path);
        }

        public SecureDirectoryInfo(Bridge bridge, DirectoryInfo di)
        {
            this.Bridge = bridge;
            this.Info = di;
        }

        private Bridge Bridge { get; set; }
        private DirectoryInfo Info { get; set; }

        public bool Exists
        {
            get
            {
                if (Info.Exists)
                {
                    return CheckAcl(this.Bridge, new FileInfo(Path.Combine(this.Info.FullName, "_.acl")));
                }

                return false;
            }
        }

        public void Create()
        {
            if (!this.Info.Exists)
            {
                this.Info.Create();
            }
        }

        public void Remove(bool recursive)
        {
            if (this.Info.Exists)
            {
                if (!recursive && this.Info.GetFiles().Length > 0)
                {
                    throw new Exception("Folder is not empty");
                }

                this.Info.Delete(recursive);
            }
        }

        public string FullName { get { return this.Info.FullName; } }

        public string Name { get { return this.Info.Name; } }

        public SecureDirectoryInfo Parent
        {
            get { return new SecureDirectoryInfo(this.Bridge, this.Info.Parent); }
        }

        public IEnumerable<SecureFileInfo> GetFiles(string searchPattern)
        {
            List<SecureFileInfo> files = new List<SecureFileInfo>();

            foreach (var fi in this.Info.GetFiles(searchPattern))
            {
                var sfi = new SecureFileInfo(this.Bridge, fi);
                if (sfi.Exists)
                {
                    files.Add(sfi);
                }
            }

            return files;
        }

        public IEnumerable<SecureDirectoryInfo> GetDirectories()
        {
            List<SecureDirectoryInfo> directories = new List<SecureDirectoryInfo>();

            foreach (var di in this.Info.GetDirectories())
            {
                var sdi = new SecureDirectoryInfo(this.Bridge, di);
                if (sdi.Exists)
                {
                    directories.Add(sdi);
                }
            }

            return directories;
        }

        public IEnumerable<SecureDirectoryInfo> GetDirectories(string searchPattern)
        {
            List<SecureDirectoryInfo> directories = new List<SecureDirectoryInfo>();

            foreach (var di in this.Info.GetDirectories(searchPattern))
            {
                var sdi = new SecureDirectoryInfo(this.Bridge, di);
                if (sdi.Exists)
                {
                    directories.Add(sdi);
                }
            }

            return directories;
        }
    }

    #endregion
}