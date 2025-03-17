namespace E.Standard.ActiveDirectory;

public class AdUser : AdObject
{
    public AdUser(string userName, string name)
    {
        this.Username = userName;
        this.Name = name;
    }

    public string Username { get; set; } = "";

    public string Email { get; set; } = "";
}
