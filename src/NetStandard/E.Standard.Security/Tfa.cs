using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using TwoFactorAuthNet.Providers.Qr;

namespace E.Standard.Security
{
    [Obsolete]
    public class Tfa
    {
        public Tfa(string appName)
        {
            this.AppName = appName;
        }

        public string AppName { get; private set; }

        public void Verify(string secret, string code)
        {
            var tfa = new TwoFactorAuthNet.TwoFactorAuth(this.AppName);
            if (!tfa.VerifyCode(new Crypto().DecryptTextDefault(secret), code))
                throw new SecurityException("Wrong Code");
        }

        public string QrCodeUri(string username, string secret, WebProxy webProxy = null)
        {
            var tfa = new TwoFactorAuthNet.TwoFactorAuth(this.AppName);
            var provider = tfa.QrCodeProvider;
            if(provider is BaseHttpQrCodeProvider)
            {
                ((BaseHttpQrCodeProvider)provider).Proxy = webProxy;
            }
            return tfa.GetQrCodeImageAsDataUri(username, new Crypto().DecryptTextDefault(secret));
        }

        public string CreateSecret()
        {
            var tfa = new TwoFactorAuthNet.TwoFactorAuth(this.AppName);
            var secret = tfa.CreateSecret(160);

            return new Crypto().EncryptTextDefault(secret, Crypto.ResultStringType.Hex);
        }
    }
}
