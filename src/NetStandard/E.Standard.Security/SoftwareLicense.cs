using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace E.Standard.Security
{
    [Obsolete("Use E.Standard.Security.Internal assembly")]
    public class SoftwareLicense
    {
        public SoftwareLicense()
        {
            this.CreateTime = DateTime.UtcNow;
        }

        public SoftwareLicense(string name, string computer, string organisation, string creatorName, string createEmail)
            : this()
        {
            this.LicenseName = name;
            this.ComputerName = computer;
            this.Organisation = organisation;
            this.CreatorName = creatorName;
            this.CreatorEMail = createEmail;
        }

        [JsonProperty(PropertyName = "name")]
[System.Text.Json.Serialization.JsonPropertyName("name")]
        public string LicenseName { get; set; }

        [JsonProperty(PropertyName = "computer")]
[System.Text.Json.Serialization.JsonPropertyName("computer")]
        public string ComputerName { get; set; }

        [JsonProperty(PropertyName = "organisation")]
[System.Text.Json.Serialization.JsonPropertyName("organisation")]
        public string Organisation { get; set; }

        [JsonProperty(PropertyName = "creator_name")]
[System.Text.Json.Serialization.JsonPropertyName("creator_name")]
        public string CreatorName { get; set; }

        [JsonProperty(PropertyName = "creator_email")]
[System.Text.Json.Serialization.JsonPropertyName("creator_email")]
        public string CreatorEMail { get; set; }

        [JsonProperty(PropertyName = "create_time")]
[System.Text.Json.Serialization.JsonPropertyName("create_time")]
        public DateTime CreateTime { get; set; }

        // Passwort niemals ändern!!!!
        private const string CryptoPassword= "DdqQ+qtF*5XtqRgwk6U@FVe5vnuW2v2yWkaMR4yrJY+EF_Rsnh%cjD4ZFKp2eHmFwezbmU?wNg5Cz=B2qVP3bppwTD#2wcL35&xe_B6854LWg9zMeYSgHaPAp=eg*Wn_+y$mpqUGF+f=qYrFfy#qDv4H8yW2n_dpmLgqQXHTNTKRf-acPcKH^n##rL*-x6ZK-sYJ?$?S4QXB3ggR9KsNFf8mNGVcK!L=d3zMAdX2qJF4qC8EBXe3c@E&M3p@wKGH";

        #region Overrides

        public override string ToString()
        {
            var json = JsonConvert.SerializeObject(this);
            return new Crypto().EncryptText(json, CryptoPassword, Crypto.Strength.AES256, true, Crypto.ResultStringType.Hex);
        }

        #endregion

        #region Static Members

        static public SoftwareLicense FromCryptoString(string cryptoString)
        {
            var json = new Crypto().DecryptText(cryptoString, CryptoPassword, Crypto.Strength.AES256, true);
            return JsonConvert.DeserializeObject<SoftwareLicense>(json);
        }

        #endregion
    }
}
