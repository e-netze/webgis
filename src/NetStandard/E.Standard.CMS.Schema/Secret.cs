using E.Standard.CMS.Core.Reflection;
using E.Standard.CMS.Core.Schema;
using E.Standard.CMS.Core.Schema.Abstraction;
using E.Standard.CMS.Core.UI.Abstraction;
using E.Standard.CMS.Schema.UI;
using E.Standard.Extensions.Compare;
using System.ComponentModel;
using System.Threading.Tasks;

namespace E.Standard.CMS.Schema;

public class Secret : CopyableXml, IEditable, IUI
{
    #region Properties

    [Category("General")]
    [DisplayName("Placeholder")]
    public string Placeholder => $"{{{{{this.Url}}}}}";

    [Category("Secrets")]
    [CmsPersistable("secret-default")]
    [SecretProperty]
    [DisplayName("Secret (Default)")]
    public string SecretStringDefault { get; set; }

    [Category("Secrets")]
    [CmsPersistable("secret-test")]
    [SecretProperty]
    [DisplayName("Secret (Test)")]
    public string SecretStringTest { get; set; }

    [Category("Secrets")]
    [CmsPersistable("secret-staging")]
    [SecretProperty]
    [DisplayName("Secret (Staging)")]
    public string SecretStringStaging { get; set; }

    [Category("Secrets")]
    [CmsPersistable("secret-production")]
    [SecretProperty]
    [DisplayName("Secret (Production)")]
    public string SecretStringProduction { get; set; }

    #endregion

    #region ICreatable Member

    override public string CreateAs(bool appendRoot)
    {
        return this.Url;
    }

    override public Task<bool> CreatedAsync(string FullName)
    {
        return Task<bool>.FromResult(true);
    }

    #endregion

    #region IUI

    public IUIControl GetUIControl(bool create)
    {
        var ctrl = new SecretControl();
        ctrl.InitParameter = this;

        return ctrl;
    }

    #endregion

    public string GetSecret(DeployEnvironment environment)
    {
        switch (environment)
        {
            case DeployEnvironment.Test:
                return this.SecretStringTest.OrTake(this.SecretStringDefault);
            case DeployEnvironment.Staging:
                return this.SecretStringStaging.OrTake(this.SecretStringDefault);
            case DeployEnvironment.Production:
                return this.SecretStringProduction.OrTake(this.SecretStringDefault);
            default:
                return this.SecretStringDefault;
        }
    }

    [Browsable(false)]
    public override string NodeTitle
    {
        get { return "Secret"; }
    }
}
