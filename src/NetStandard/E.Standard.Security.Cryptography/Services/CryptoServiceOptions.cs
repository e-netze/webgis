using E.Standard.Json;
using E.Standard.Security.Cryptography.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace E.Standard.Security.Cryptography.Services;

public class CryptoServiceOptions
{
    private bool _sealed = false;
    private readonly ConcurrentDictionary<int, string> _customPasswords;

    public CryptoServiceOptions()
    {
        Saltsize = 4;
        Iterations = 1000;
        DefaultPassword = "PaS3W0rd";
        CookiePassword = "C0oKi3Pa3Sw0rd";
        HashBytesSalt = new Guid("74E79736-A940-4CDA-8BE2-3AB70D4B4A3F").ToByteArray();
        _customPasswords = new ConcurrentDictionary<int, string>();
    }

    public int Saltsize { get; private set; }
    public int Iterations { get; private set; }
    public string DefaultPassword { get; private set; }

    // For legacy reasons (old password to encrypt serverside connectionstrings ans statements)
    // should be null or emtpy => DefaultPassword will be used
    public string DefaultPasswordDataLinq { get; private set; }

    public string CookiePassword { get; private set; }

    public byte[] HashBytesSalt { get; private set; }

    public string GetCustomPassword(int index)
    {
        if (!_customPasswords.ContainsKey(index))
        {
            return DefaultPassword;
        }

        return _customPasswords[index];
    }

    public void Seal() { _sealed = true; }

    public void SetSaltSize(int size)
    {
        if (!_sealed)
        {
            this.Saltsize = size;
        }
    }
    public void SetIterations(int interations)
    {
        if (!_sealed)
        {
            this.Iterations = Iterations;
        }
    }
    public void SetDefaultPassword(string password)
    {
        if (!_sealed)
        {
            DefaultPassword = password;
        }
    }

    public void SetCustomPassword(int index, string password)
    {
        if (!_sealed)
        {
            _customPasswords[index] = password;
        }
    }

    public void SetCookiePassword(string password)
    {
        if (!_sealed)
        {
            CookiePassword = password;
        }
    }

    public void SetHashBytesSalt(byte[] salt)
    {
        if (!_sealed)
        {
            HashBytesSalt = salt;
        }
    }

    public void LoadOrCreate(string[] sharedKeysPaths,
                             Type customPasswordsEnumType = null,
                             Dictionary<int, string> customLegacyPasswords = null,
                             byte[] customLegacySalt = null,
                             string legacyDataLinqDefaultPassword = null)
    {
        Console.WriteLine("Load or create security keys");

        if (!_sealed && sharedKeysPaths != null)
        {
            for (int i = 0; i < sharedKeysPaths.Length; i++)
            {
                var sharedKeysPath = sharedKeysPaths[i];

                var fi = new FileInfo(Path.Combine(sharedKeysPath, "keys.config"));

                Console.WriteLine($"{fi.FullName} exists: {fi.Exists}");

                KeysModel keysModel = null;
                if (fi.Exists)
                {
                    Console.WriteLine($"Load security keys from {fi.FullName}");
                    keysModel = JSerializer.Deserialize<KeysModel>(CryptoImpl.PseudoDecryptString(File.ReadAllText(fi.FullName)));
                }
                else if (i == sharedKeysPaths.Length - 1) // last
                {
                    keysModel = new KeysModel()
                    {
                        Saltsize = 4,
                        Iterations = 1000,
                        DefaultPassword = CryptoImpl.GetRandomAlphanumericString(256),
                        CookiePassword = CryptoImpl.GetRandomAlphanumericString(256),
                        HashBytesSalt = customLegacySalt ?? CryptoImpl.GetRandomBytes(16),

                        Custom_ApiAdminQueryPassword = CryptoImpl.GetRandomAlphanumericString(256),

                        Custom_LicensePassword = CryptoImpl.GetRandomAlphanumericString(256),
                        Custom_PortalProxyRequests = CryptoImpl.GetRandomAlphanumericString(128),

                        DefaultPasswordDataLinq = String.IsNullOrWhiteSpace(legacyDataLinqDefaultPassword)
                                ? null
                                : legacyDataLinqDefaultPassword,

                        Custom_ApiBridgeUserCryptoPassword =
                              customLegacyPasswords?.ContainsKey((int)CustomPasswords.ApiBridgeUserCryptoPassword) == true
                                ? customLegacyPasswords[(int)CustomPasswords.ApiBridgeUserCryptoPassword]
                                : CryptoImpl.GetRandomAlphanumericString(128),
                        Custom_ApiStoragePassword =
                              customLegacyPasswords?.ContainsKey((int)CustomPasswords.ApiStoragePassword) == true
                                ? customLegacyPasswords[(int)CustomPasswords.ApiStoragePassword]
                                : CryptoImpl.GetRandomAlphanumericString(256),
                    };

                    Console.WriteLine($"Directory {fi.Directory.FullName} exists: {fi.Directory.Exists}");
                    if (!fi.Directory.Exists)
                    {
                        fi.Directory.Create();
                    }

                    Console.WriteLine($"Try Write security keys: {fi.FullName}");
                    try
                    {
                        File.WriteAllText(fi.FullName, CryptoImpl.PseudoEncryptString(JSerializer.Serialize(keysModel, pretty: true)));
                        Console.WriteLine("succeeded");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Can't persist security keys - {ex.Message}");
                    }
                }
                else
                {
                    continue;
                }

                this.Saltsize = keysModel != null && keysModel.Saltsize > 0 ? keysModel.Saltsize : this.Saltsize;
                this.Iterations = keysModel != null && keysModel.Iterations > 0 ? keysModel.Iterations : this.Iterations;
                this.DefaultPassword = keysModel?.DefaultPassword ?? this.DefaultPassword;
                this.CookiePassword = keysModel?.CookiePassword ?? this.CookiePassword;
                this.HashBytesSalt = customLegacySalt ?? keysModel?.HashBytesSalt ?? this.HashBytesSalt;
                this.DefaultPasswordDataLinq = legacyDataLinqDefaultPassword ?? keysModel?.DefaultPasswordDataLinq;

                if (customPasswordsEnumType != null)
                {
                    foreach (var customPassword in Enum.GetValues(customPasswordsEnumType))
                    {
                        #region Check if is an static password for older legacy things like cache/recovery etc..

                        if (customLegacyPasswords?.ContainsKey((int)customPassword) == true)
                        {
                            var customLegavcyPassword = customLegacyPasswords[(int)customPassword];

                            if (!String.IsNullOrEmpty(customLegavcyPassword))
                            {
                                this.SetCustomPassword((int)customPassword, customLegavcyPassword);
                                continue;
                            }
                        }

                        #endregion

                        var propertyInfo = typeof(KeysModel).GetProperty($"Custom_{customPassword}");

                        if (!String.IsNullOrEmpty(propertyInfo?.GetValue(keysModel)?.ToString()))
                        {
                            this.SetCustomPassword((int)customPassword, propertyInfo?.GetValue(keysModel)?.ToString());
                        }
                    }
                }

                this.Seal();
            }
        }
    }

    public string GenerateHashCode()
    {
        StringBuilder sb = new StringBuilder();

        sb.Append(this.Saltsize.ToString());
        sb.Append(this.Iterations.ToString());
        sb.Append(this.DefaultPassword);
        sb.Append(this.CookiePassword);
        sb.Append(Convert.ToBase64String(this.HashBytesSalt));
        if (_customPasswords != null)
        {
            foreach (var customPassword in _customPasswords.Values)
            {
                sb.Append(customPassword);
            }
        }

        var crypto = new CryptoImpl(this);

        return crypto.Hash64_SHA1(crypto.Hash64_SHA512(sb.ToString()));
    }
}
