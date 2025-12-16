using Api.Core.AppCode.Extensions;
using E.DataLinq.Core.Models.Authentication;
using E.DataLinq.Core.Services.Abstraction;
using E.Standard.Security.Cryptography.Services;
using E.Standard.WebGIS.SubscriberDatabase.Services;
using Microsoft.Extensions.Primitives;
using System.Collections.Generic;
using System.Linq;

namespace Api.Core.AppCode.Services.DataLinq;

public class DataLinqCodeIdentityProvider : IDataLinqCodeIdentityProvider
{
    private readonly SubscriberDatabaseService _subscriberDb;
    private readonly JwtAccessTokenService _jwTokenService;

    public DataLinqCodeIdentityProvider(SubscriberDatabaseService subscriberDb, JwtAccessTokenService jwTokenService)
    {
        _subscriberDb = subscriberDb;
        _jwTokenService = jwTokenService;
    }

    public DataLinqCodeIdentity TryGetIdentity(string name, string password)
    {
        var db = _subscriberDb.CreateInstance();

        var subscriber = db.GetSubscriberByName(name);
        if (subscriber != null)
        {
            if (subscriber.VerifyPassword(password))
            {
                return new DataLinqCodeIdentity()
                {
                    Name = subscriber.FullName,
                    DisplayName = $"{subscriber.FirstName} {subscriber.LastName}",
                    Id = subscriber.Id,
                    Roles = new string[] { "datalinq-code(*,_*)" }
                };
            }
        }

        return null;
    }

    public DataLinqCodeIdentity TryGetIdentity(IEnumerable<KeyValuePair<string, StringValues>> parameters)
    {
        try
        {
            var token = parameters.FirstOrDefault(k => k.Key == "token").Value;
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            var subscriberName = _jwTokenService.ValidatedName(token);
            if (string.IsNullOrEmpty(subscriberName))
            {
                return null;
            }

            var db = _subscriberDb.CreateInstance();
            var subscriber = db.GetSubscriberByName(subscriberName.RemoveSubscriberPrefix());

            if (subscriber != null)
            {
                return new DataLinqCodeIdentity()
                {
                    Name = subscriber.FullName,
                    DisplayName = $"{subscriber.FirstName} {subscriber.LastName}",
                    Id = subscriber.Id,
                    Roles = new string[] { "datalinq-code(*,_*)" }
                };
            }
        }
        catch { }

        return null;
    }
}
