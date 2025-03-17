#nullable enable

using E.Standard.Json;
using E.Standard.Security.Cryptography;
using E.Standard.Security.Cryptography.Models;
using E.Standard.Security.Cryptography.Services;
using System.IO.Compression;

namespace webgis.deploy.Services;

internal class SecurityService
{
    private const string Filename = "security-do-not-touch.dat";
    private readonly DeployRepositoryService _deployRepositoryService;

    public SecurityService(DeployRepositoryService deployRepositoryService)
    {
        _deployRepositoryService = deployRepositoryService;
    }

    public void Init()
    {
        var repoDirectoryInfo = _deployRepositoryService.RepositoryRootDirectoryInfo();
        var securityFile = Path.Combine(repoDirectoryInfo.FullName, ".security-do-not-touch");

        if (!File.Exists(securityFile))
        {
            var keysConfigFileInfo = new FileInfo(Path.Combine(repoDirectoryInfo.FullName, "keys.config"));
            if (!keysConfigFileInfo.Exists)
            {
                Dictionary<int, string>? customLegacyPasswords = null;
                byte[]? salt = null;
                string? legacyDataLinqDefaultPassword = null;

                var legacyKeysFileInfo = new FileInfo(Path.Combine(repoDirectoryInfo.Parent!.FullName, "keys.legacy.config"));
                if (legacyKeysFileInfo.Exists)
                {
                    var keysAsText = File.ReadAllText(legacyKeysFileInfo.FullName);
                    var keys = JSerializer.Deserialize<KeysModel>(CryptoImpl.PseudoDecryptString(keysAsText));

                    if (keys is not null)
                    {
                        customLegacyPasswords = new();

                        Console.WriteLine("######## Legacy Crypto Keys ###############");
                        if (keys.HashBytesSalt is not null && keys.HashBytesSalt.Length > 0)
                        {
                            salt = keys.HashBytesSalt;
                            Console.WriteLine($"salt: {Convert.ToBase64String(salt).Substring(0, 4)}...");
                        }
                        if (!String.IsNullOrEmpty(keys.Custom_ApiStoragePassword))
                        {
                            customLegacyPasswords.Add(
                                    (int)CustomPasswords.ApiStoragePassword,
                                    keys.Custom_ApiStoragePassword);
                            Console.WriteLine($"storage-pw: {keys.Custom_ApiStoragePassword.Substring(0, 8)}...");
                        }
                        if (!String.IsNullOrEmpty(keys.Custom_ApiBridgeUserCryptoPassword))
                        {
                            customLegacyPasswords.Add(
                                    (int)CustomPasswords.ApiBridgeUserCryptoPassword,
                                    keys.Custom_ApiBridgeUserCryptoPassword);
                            Console.WriteLine($"bridge-user-pw: {keys.Custom_ApiBridgeUserCryptoPassword.Substring(0, 8)}...");
                        }
                        if (!String.IsNullOrEmpty(keys.DefaultPasswordDataLinq))
                        {
                            legacyDataLinqDefaultPassword = keys.DefaultPasswordDataLinq;
                            Console.WriteLine($"datalinq-default-pw: {keys.DefaultPasswordDataLinq.Substring(0, 8)}...");
                        }
                        Console.WriteLine("##############################################");
                    }

                    if (!keysAsText.StartsWith("enc:"))
                    {
                        File.Delete(legacyKeysFileInfo.FullName);
                        File.WriteAllText(legacyKeysFileInfo.FullName, CryptoImpl.PseudoEncryptString(keysAsText));
                    }
                }

                var cryptoOptions = new CryptoServiceOptions();
                cryptoOptions.LoadOrCreate(new string[] { repoDirectoryInfo.FullName },
                                           typeof(CustomPasswords),
                                           customLegacyPasswords: customLegacyPasswords,
                                           customLegacySalt: salt,
                                           legacyDataLinqDefaultPassword: legacyDataLinqDefaultPassword);
            }

            using (var ms = new MemoryStream())
            {
                using (var zipArchive = new ZipArchive(ms, ZipArchiveMode.Create))
                {
                    var entry = zipArchive.CreateEntry("keys.config");
                    using (var entryStream = entry.Open())
                    {
                        entryStream.Write(File.ReadAllBytes(keysConfigFileInfo.FullName));
                    }
                }

                File.WriteAllText(securityFile, CryptoImpl.PseudoEncryptString(
                    Convert.ToBase64String(ms.ToArray())));
            }

            keysConfigFileInfo.Delete();
        }
    }

    public void CopyKeysConfigTo(string targetPath)
    {
        if (File.Exists(Path.Combine(targetPath, "keys.config")))
        {
            return;
        }

        var repoDirectoryInfo = _deployRepositoryService.RepositoryRootDirectoryInfo();
        var securityFile = Path.Combine(repoDirectoryInfo.FullName, ".security-do-not-touch");

        if (File.Exists(securityFile))
        {
            using (var ms = new MemoryStream(Convert.FromBase64String(CryptoImpl.PseudoDecryptString(File.ReadAllText(securityFile)))))
            using (var zipArchive = new ZipArchive(ms, ZipArchiveMode.Read))
            {
                var entry = zipArchive.GetEntry("keys.config");
                if (entry == null)
                {
                    throw new Exception($"Invalid {Filename}: keys.config not included");
                }

                if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                }

                entry.ExtractToFile(Path.Combine(targetPath, "keys.config"));
            }
        }
        else
        {
            throw new Exception($"{Filename} not exists");
        }
    }
}
