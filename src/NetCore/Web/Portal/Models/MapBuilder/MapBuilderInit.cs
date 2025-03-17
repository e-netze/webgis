using E.Standard.Security.Cryptography;
using E.Standard.Security.Cryptography.Abstractions;
using System;

namespace Portal.Core.Models.MapBuilder;

public class MapBuilderInit
{
    private readonly ICryptoService _crypto;

    public MapBuilderInit(ICryptoService crypto)
    {
        _crypto = crypto;
    }

    public string[] Templates { get; set; }
    public string HMACObject { get; set; }
    public string PortalId { get; set; }
    public string PortalCategory { get; set; }
    public string MapName { get; set; }
    public bool IsPortalOwner { get; set; }
    public string CurrentUsername { get; set; }

    public string ClientIdPlus
    {
        get
        {
            if (HMACObject.ToLower().StartsWith("'http://") ||
                HMACObject.ToLower().StartsWith("'https://"))
            {
                return HMACObject;
            }

            if (HMACObject.StartsWith("{"))
            {
                return HMACObject;
            }

            return "'" + HMACObject + ":" +
                _crypto.EncryptTextDefault(HMACObject + "," + DateTime.UtcNow.Ticks.ToString(), CryptoResultStringType.Hex) + "'";
        }
    }
}
