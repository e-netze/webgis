using E.Standard.Extensions.Compare;
using E.Standard.Json;
using E.Standard.Security.Cryptography;
using E.Standard.Security.Cryptography.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace E.Standard.WebGIS.Core.Extensions;

static public class HmacExtensions
{
    static public HmacVersion HmacVersionFromPublicKey(this string publicKey)
    {
        if (publicKey.Contains("."))
        {
            return HmacVersion.V2;
        }

        return HmacVersion.V1;
    }

    static public string ToHmacCacheKey(this string username, HmacVersion hmacVersion = HmacVersion.Current)
    {
        hmacVersion = hmacVersion.OrTakeEnum<HmacVersion>(HmacResponseDTO.CurrentVersion);

        switch (hmacVersion)
        {
            case HmacVersion.V2:
                return $"{username}:hmac_v2";
        }

        return username;
    }

    static public IEnumerable<string> PublicKeyToCacheKeys(this string publicKey, HmacVersion hmacVersion = HmacVersion.Current)
    {
        if (String.IsNullOrEmpty(publicKey))
        {
            return new string[0];
        }

        hmacVersion = hmacVersion.OrTakeEnum<HmacVersion>(HmacResponseDTO.CurrentVersion);

        if (hmacVersion == HmacVersion.V2 && publicKey.Contains("."))
        {
            var parts = publicKey.Split('.');
            int chunks = int.Parse(parts[1]);
            publicKey = parts[0];

            return Enumerable.Range(0, chunks + 1)
                             .Select(x => $"hmac:{publicKey}.{x}");
        }

        return new string[] { $"hmac:{publicKey}" };
    }

    static public string ConcatenateChunks(this IEnumerable<string> chunks)
    {
        return String.Concat(chunks);
    }

    static public IEnumerable<string> PrivateKeyToUserPrincipalChunks(this string privateKey,
                                                                      ICryptoService crypto,
                                                                      int maxChunkSize,
                                                                      string username,
                                                                      string[] userRoles,
                                                                      string[] roleParameters,
                                                                      HmacVersion hmacVersion = HmacVersion.Current)
    {
        hmacVersion = hmacVersion.OrTakeEnum<HmacVersion>(HmacResponseDTO.CurrentVersion);

        StringBuilder sb = new StringBuilder();

        if (hmacVersion == HmacVersion.V2)
        {
            sb.Append(crypto.EncryptTextDefault(privateKey, CryptoResultStringType.Base64));
            if (!String.IsNullOrWhiteSpace(username))
            {
                sb.Append(".");
                sb.Append(username.ToChunkPart(hmacVersion));

                if (userRoles != null)
                {
                    sb.Append(".");
                    sb.Append(JSerializer.Serialize(userRoles).ToChunkPart(hmacVersion));

                    if (roleParameters != null)
                    {
                        sb.Append(".");
                        sb.Append(JSerializer.Serialize(roleParameters).ToChunkPart(hmacVersion));
                    }
                }
            }

            var result = sb.ToString();

            if (result.Length < maxChunkSize)
            {
                return new[] { result };
            }

            return Enumerable.Range(0, result.Length / maxChunkSize + 1)
                             .Select(x => result.Substring(x * maxChunkSize, Math.Min(result.Length - x * maxChunkSize, maxChunkSize)));
        }
        else // old
        {
            #region Old

            sb.Append(privateKey);
            sb.Append("|");
            sb.Append(username);

            if (userRoles != null && userRoles.Length > 0)
            {
                for (int i = 0; i < userRoles.Length; i++)
                {
                    var role = userRoles[i];
                    if (String.IsNullOrWhiteSpace(role))
                    {
                        continue;
                    }

                    if (role.Contains("|"))
                    {
                        throw new Exception("Forbidden charater (|) in role: " + role);
                    }

                    sb.Append($"|{role}");
                }
            }

            return new[] { sb.ToString() };

            #endregion
        }
    }

    static public string AppendChunkLength(this string publicKey, IEnumerable<string> privateKeyChunks, HmacVersion hmacVersion = HmacVersion.Current)
    {
        hmacVersion = hmacVersion.OrTakeEnum<HmacVersion>(HmacResponseDTO.CurrentVersion);

        if (hmacVersion == HmacVersion.V2)
        {
            if (publicKey.Contains("."))
            {
                publicKey = publicKey.Split('.')[0];
            }

            return $"{publicKey}.{privateKeyChunks.Count() - 1}";
        }

        return publicKey;
    }

    static public string ToChunkPart(this string stringValue, HmacVersion hmacVersion = HmacVersion.Current)
    {
        hmacVersion = hmacVersion.OrTakeEnum<HmacVersion>(HmacResponseDTO.CurrentVersion);

        var encoding = Encoding.UTF8;

        return Convert.ToBase64String(encoding.GetBytes(stringValue));
    }

    static public string FromChunkPart(this string chunkPart, HmacVersion hmacVersion = HmacVersion.Current)
    {
        hmacVersion = hmacVersion.OrTakeEnum<HmacVersion>(HmacResponseDTO.CurrentVersion);

        var encoding = Encoding.UTF8;

        return encoding.GetString(Convert.FromBase64String(chunkPart));
    }

    static public (string privateKey,
                   string username,
                   string[] userRoles,
                   string[] roleParametrs) ToUserPrincipal(this string privateKeyString,
                                                           ICryptoService crypto,
                                                           HmacVersion hmacVersion = HmacVersion.Current)
    {
        hmacVersion = hmacVersion.OrTakeEnum<HmacVersion>(HmacResponseDTO.CurrentVersion);

        if (hmacVersion == HmacVersion.V2)
        {
            var parts = privateKeyString.Split('.');

            return (
                privateKey: crypto.DecryptTextDefault(parts[0]),
                username: parts.Length < 2 ? String.Empty : parts[1].FromChunkPart(hmacVersion),
                userRoles: parts.Length < 3 ? null : JSerializer.Deserialize<string[]>(parts[2].FromChunkPart(hmacVersion)),
                roleParametrs: parts.Length < 4 ? null : JSerializer.Deserialize<string[]>(parts[3].FromChunkPart(hmacVersion))
                );
        }
        else // old
        {
            var parts = privateKeyString.Split('|');

            return (
                privateKey: parts[0],
                username: parts.Length < 2 ? String.Empty : parts[1],
                userRoles: parts.Length < 3 ? null : parts.Skip(2).ToArray(),
                roleParametrs: null
                );
        }
    }

    static public void ValidateHmacHash(this string hmacHash, string privateKey, string hmacData, long ticks)
    {
        byte[] hmacHashBytes = hmacHash.BytesFromBase64();
        byte[] computedHash = null;

        if (hmacHashBytes.Length == 64)
        {
            using (var hmacsha512 = new HMACSHA512(Encoding.UTF8.GetBytes(privateKey)))
            {
                computedHash = hmacsha512.ComputeHash(Encoding.UTF8.GetBytes(ticks.ToString() + hmacData));
            }
        }
        else if (hmacHashBytes.Length == 32)
        {
            using (var hmacsha256 = new HMACSHA256(Encoding.UTF8.GetBytes(privateKey)))
            {
                computedHash = hmacsha256.ComputeHash(Encoding.UTF8.GetBytes(ticks.ToString() + hmacData));
            }
        }
        else //if (hmacHashBytes.Length == 20)
        {

            using (var hmacsha1 = new HMACSHA1(Encoding.UTF8.GetBytes(privateKey)))
            {
                computedHash = hmacsha1.ComputeHash(Encoding.UTF8.GetBytes(ticks.ToString() + hmacData));
            }
        }

        if (!hmacHashBytes.SequenceEqual(computedHash))
        {
            throw new Exception("Forbidden: Invalid hash!");
        }
    }

    static private byte[] BytesFromBase64(this string base64)
    {
        try
        {
            return Convert.FromBase64String(base64);
        }
        catch { }

        try
        {
            return Convert.FromBase64String($"{base64}==");
        }
        catch { }

        return Convert.FromBase64String($"{base64}=");
    }
}
