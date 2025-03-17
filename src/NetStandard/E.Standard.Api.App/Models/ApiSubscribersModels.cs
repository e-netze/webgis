using E.Standard.WebGIS.SubscriberDatabase;
using System;
using System.ComponentModel;

namespace E.Standard.Api.App.Models;

public class ApiClients
{
    public SubscriberDb.Client[] Clients { get; set; }
}

public class UpdateClient : ErrorHandlingModel
{
    public UpdateClient() { }
    public UpdateClient(SubscriberDb.Client client)
    {
        this.Id = client.Id;
        this.ClientName = client.ClientName;
        this.ClientId = client.ClientId;
        this.ClientSecret = client.ClientSecret;
        this.ClientReferer = client.ClientReferer;
        this.Created = client.Created;
        this.Expires = client.Expires;
        this.Locked = client.Locked;
        this.Subscriber = client.Subscriber;
    }

    public string Id { get; set; }
    public string ClientName { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string ClientReferer { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Expires { get; set; }
    public bool Locked { get; set; }
    public string Subscriber { get; set; }

    public SubscriberDb.Client ToClient()
    {
        return new SubscriberDb.Client()
        {
            Id = this.Id,
            ClientName = this.ClientName,
            ClientId = this.ClientId,
            ClientSecret = this.ClientSecret,
            ClientReferer = this.ClientReferer,
            Created = this.Created,
            Expires = this.Expires,
            Locked = this.Locked,
            Subscriber = this.Subscriber
        };
    }
}

public class ApiSubscribersLogin : ErrorHandlingModel
{
    public string Username { get; set; }
    public string Password { get; set; }
    public string Redirect { get; set; }

    public string CaptchaCodeEncrypted { get; set; }
    public string CaptchaDataBase64 { get; set; }
    [DisplayName("Captcha-Code")]
    public string CaptchaInput { get; set; }
}

public class RegisterSubscriberModel : ErrorHandlingModel
{
    public RegisterSubscriberModel()
    {

    }
    public RegisterSubscriberModel(SubscriberDb.Subscriber subscriber)
        : this()
    {
        this.Id = subscriber.Id;
        this.Username = subscriber.Name;
        this.FirstName = subscriber.FirstName;
        this.LastName = subscriber.LastName;
        this.Email = subscriber.Email;
    }

    public string Id { get; set; }
    public string Username { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }

    [DisplayName("Password")]
    public string Password1 { get; set; }
    [DisplayName("Repeat password")]
    public string Password2 { get; set; }
}