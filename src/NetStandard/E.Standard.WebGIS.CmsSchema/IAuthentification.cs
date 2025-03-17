namespace E.Standard.WebGIS.CmsSchema;

public interface IAuthentification
{
    string Username
    {
        get;
        set;
    }
    string Password
    {
        get;
        set;
    }

    string Token { get; set; }
}

public interface IClientCertification
{
    string ClientCertificate { get; set; }
    string ClientCertificatePassword { get; set; }
}
