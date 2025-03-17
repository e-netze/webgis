using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace E.Standard.CMS.Schema.UI;

internal class SecretControl : UserControl, IInitParameter, ISubmit
{
    private Input _txtUrl = new Input("Url") { Label = "Url (secret-...)", Placeholder = "Kurzname, für Url Aufrufe (nur Kleinbuchstaben und Nummern)", Required = true };
    private Input _txtSecret = new Input("SecretString") { Label = "Secret String", Placeholder = "The serect text...", Required = true };
    private Secret _secret = null;

    public SecretControl()
    {
        _txtUrl.ModifyMethods = new string[] { "toLowerCase" };
        List<KeyValuePair<string, string>> regexReplace = new List<KeyValuePair<string, string>>();
        regexReplace.Add(new KeyValuePair<string, string>("[ä]", "ae"));
        regexReplace.Add(new KeyValuePair<string, string>("[ö]", "oe"));
        regexReplace.Add(new KeyValuePair<string, string>("[ü]", "ue"));
        regexReplace.Add(new KeyValuePair<string, string>("[ß]", "ss"));
        regexReplace.Add(new KeyValuePair<string, string>("[ ]", "-"));
        regexReplace.Add(new KeyValuePair<string, string>("[^A-Za-z0-9_-]", ""));
        _txtUrl.RegexReplace = regexReplace;

        this.AddControl(_txtUrl);
        this.AddControl(_txtSecret);
    }

    #region Properties

    #endregion

    #region IInitParameter

    public object InitParameter
    {
        set
        {
            if (value is Secret)
            {
                _secret = (Secret)value;
            }
        }
    }

    #endregion

    #region ISubmit

    public void Submit(NameValueCollection secrets)
    {
        if (_secret != null)
        {
            var url = _txtUrl.Value.ToLower().Trim();

            if (_txtUrl.RegexReplace != null)
            {
                foreach (var regexReplace in _txtUrl.RegexReplace)
                {
                    url = Regex.Replace(url, regexReplace.Key, regexReplace.Value);
                }
            }

            if (!url.StartsWith("secret-"))
            {
                url = $"secret-{url}";
            }

            _secret.Url = url;
            _secret.SecretStringDefault = _txtSecret.Value;
        }
    }

    #endregion
}
