using E.Standard.Security.Cryptography.Abstractions;
using System;

namespace E.Standard.WebGIS.Core.Extensions;

static public class CryptoServiceExtensions
{
    extension(ICryptoService crypto)
    {
        public string StaticDefaultDecrypt_Aes_or_Legacy3Des_or_Empty(string input)
        {
            try
            {
                return crypto.StaticDefaultDecrypt_Aes(input);
            }
            catch{ }

            try
            {
#pragma warning disable 0618
                return crypto.StaticDefaultDecrypt_3Des(input);
#pragma warning restore 0618
            }
            catch { }

            return String.Empty;
        }
    }
}