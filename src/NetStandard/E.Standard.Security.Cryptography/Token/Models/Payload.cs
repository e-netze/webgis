using System;

namespace E.Standard.Security.Cryptography.Token.Models;

public class Payload
{
    public Payload()
    {
        this.iat = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }

    public Payload(int expireSeconds)
        : this()
    {
        exp = iat + expireSeconds;
    }

    public string iis { get; set; }
    public string sub { get; set; }
    public string aud { get; set; }
    public string name { get; set; }
    public string roles { get; set; }
    public int exp { get; set; }
    public int iat { get; set; }
}
