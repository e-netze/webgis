using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.UI.Controls;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace E.Standard.WebGIS.CmsSchema.UI;

public class ClientCertificateControl : Control, IEditableUIControl
{
    private GroupBox gbCert = new GroupBox() { Label = "Zertifikat (optional)", Collapsed = true };
    private ComboBox cmbCertificate = new ComboBox("cmbCertificate") { Label = "Zertifikat" };
    private Input txtPassword = new Input("txtPassword") { Label = "Passwort" };

    public ClientCertificateControl(string name)
        : base(name)
    {
        this.AddControl(gbCert);
        gbCert.AddControl(cmbCertificate);
        gbCert.AddControl(txtPassword);

        cmbCertificate.OnChange += CmbCertificate_OnChange;
        txtPassword.OnChange += TxtPassword_OnChange;
    }

    private void TxtPassword_OnChange(object sender, EventArgs e)
    {
        this.CertificatePassword = txtPassword.Value;
        FireChange();
    }

    private void CmbCertificate_OnChange(object sender, EventArgs e)
    {
        this.CertificateName = cmbCertificate.Value;
        FireChange();
    }

    #region Properties

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string CertificateName { get; set; }
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public string CertificatePassword { get; set; }
    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public X509Certificate X509Certificate { get; set; }

    public void FireChange()
    {
        this.OnChange?.Invoke(this, new EventArgs());
    }
    public event EventHandler OnChange;

    #endregion

    #region Static members

    static public X509Certificate X509CertificateByName(string name, string pwd)
    {
        if (String.IsNullOrEmpty(name))
        {
            return null;
        }

        FileInfo fi = new FileInfo(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"/cer/" + name);

        return (fi.Exists, fi.Extension.ToLowerInvariant()) switch
        {
            (true, ".p12") => X509CertificateLoader.LoadPkcs12FromFile(fi.FullName, pwd),
            (true, ".pfx") => X509CertificateLoader.LoadPkcs12FromFile(fi.FullName, pwd),
            (true, _) => X509CertificateLoader.LoadCertificateFromFile(fi.FullName),
            (false, _) => null
        };
    }

    #endregion
}
