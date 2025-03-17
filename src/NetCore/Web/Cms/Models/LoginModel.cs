namespace Cms.Models;

public class LoginModel
{
    public string Username { get; set; }
    public string ErrorMessage { get; set; }
}

public class LoginCreateTFAModel
{
    public string Secret { get; set; }
    public string QrCodeUri { get; set; }
}
