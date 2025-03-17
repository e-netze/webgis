using System;

namespace E.Standard.Custom.Core.Models;

public class CustomToken
{
    public string Token { get; set; }
    public string LongLivingToken { get; set; }
    public DateTime ExpireDate { get; set; }
    public DateTime ExpireDateLong { get; set; }
}
