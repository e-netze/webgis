//#define ADD_IDENTITYSERVER
//#define ADD_MESSAGEQUEUE
//#define ADD_REDIS

var builder = DistributedApplication.CreateBuilder(args);

#if ADD_IDENTITYSERVER
#region IdentityServerNET

var identityServer = builder.AddIdentityServerNET("is-net-dev", httpsPort: 44300)
       .WithConfiguration(config =>
       {
           config
                //.DenyRememberLogin()
                .RememberLoginDefaultValue(true)
                .DenyForgotPasswordChallange()
                .DenyManageAccount()
                //.DenyLocalLogin()
                ;
       })
       .WithMigrations(migrations =>
            migrations
               .AddAdminPassword("admin")
               .AddIdentityResources(["openid", "profile", "role"])
               .AddClient(ClientType.WebApplication,
                          "local-webgis-portal", "secret",
                          "https://localhost:44320",
                          [
                              "openid", "profile","role"
                          ])
               .AddClient(ClientType.WebApplication,
                          "local-webgis-api", "secret",
                          "https://localhost:44341",
                          [
                              "openid", "profile","role"
                          ])
               .AddClient(ClientType.WebApplication,
                          "local-webgis-cms", "secret",
                          "https://localhost:44359",
                          [
                              "openid", "profile","role"
                          ])
       )
       .WithExternalProviders(external =>
       {
           external.AddMicrosoftIdentityWeb(
               builder.Configuration.GetSection("IdentityServer:External:MicrosoftIdentityWeb"));
       })
       .Build()
       .WithContainerName("webgis-identityserver-net")
       .WithLifetime(ContainerLifetime.Persistent);

#endregion
#endif

#if ADD_MESSAGEQUEUE
#region MessageQueueNET

var mq = builder
            .AddMessageQueueNET("messagequeue", httpPort: 8001)
            //.WithBindMountPersistance()
            .Build()
            .WithContainerName("webgis-messagequeue")
            .WithLifetime(ContainerLifetime.Persistent);

var mqDashboard = builder
            .AddDashboardForMessageQueueNET("messagequeue-dashboard")
            .ConnectToMessageQueue(mq, "*")
            .WithMaxPollingSeconds(5)
            .Build()
            .WithContainerName("webgis-messagequeue-dashboard")
            .WithLifetime(ContainerLifetime.Persistent);

#endregion
#endif

#if ADD_REDIS
#region Redis

var redis = builder
            .AddRedis("redis", port: 6379)
            .WithContainerName("webgis-redis")
            .WithLifetime(ContainerLifetime.Persistent);

#endregion
#endif

#region WebGIS

var webgisApi = builder.AddProject<Projects.webgis_api>("webgis-api")
                        .WithReplicas(1)
#if ADD_MESSAGEQUEUE
                        .WaitFor(mq)
#endif
#if ADD_REDIS
                        .WaitFor(redis)
#endif
                        ;


var webgisPortal = builder.AddProject<Projects.webgis_portal>("webgis-portal")
                          .WaitFor(webgisApi)
#if ADD_IDENTITYSERVER
                          .WaitFor(identityServer)
#endif
                          ;

var webgisCms = builder.AddProject<Projects.webgis_cms>("webgis-cms")
                       .WaitFor(webgisPortal);

#endregion

builder.Build().Run();
