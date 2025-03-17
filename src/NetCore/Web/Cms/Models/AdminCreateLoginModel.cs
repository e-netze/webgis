namespace Cms.Models;

public class AdminCreateLoginModel
{
    public string Username { get; set; }
    public string Password { get; set; }

    public string Json { get; set; }

    public string ErrorMessage { get; set; }
}
