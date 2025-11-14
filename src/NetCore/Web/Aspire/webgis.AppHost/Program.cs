//#define ADD_IDENTITYSERVER
//#define ADD_MESSAGEQUEUE
//#define ADD_REDIS
//#define POSTGRES
//#define GVIEW

var builder = DistributedApplication.CreateBuilder(args);

#if ADD_IDENTITYSERVER
#region IdentityServerNET

var identityServer = builder.AddIdentityServerNET("is-net-dev", httpsPort: 8443)
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
       .WithLifetime(ContainerLifetime.Persistent)
       .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

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

#if POSTGRES

var postgresPassword = builder.AddParameter("postgresql-password", "postgres");

// Add a PostgreSQL container using the PostGIS-enabled image
var postgres = builder
                    .AddPostgres("webgis-postgis", password: postgresPassword)
                    .WithImage("postgis/postgis")
                    .WithDataVolume("webgis-postgis")
                    //.WithInitBindMount(source: "C:\\postgres\\init")
                    .WithContainerName("webgis-postgis")
                    //.WithPgAdmin(containerName: "webgis-pgadmin")
                    .WithLifetime(ContainerLifetime.Persistent)
                    .WithHostPort(5432);

#endif

#if GVIEW
var gViewServer = builder
                    .AddgViewServer("gview-server", httpPort: 61656)
                    .Build()
                    .WithLifetime(ContainerLifetime.Persistent)
                    .WithContainerName("gview-server");

var gViewWebApps = builder
                    .AddgViewWebApps("gview-webapps")
                    .WithDrive("GEODATA", "/geodata", @"C:\temp\GeoData")
                    .WithgViewServer(gViewServer)
                    .Build()
                    .WithLifetime(ContainerLifetime.Persistent)
                    .WithContainerName("gview-webapps");
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
                       .WaitFor(webgisPortal)
                       .WaitFor(webgisApi);

#endregion

#region MCP

builder.AddProject<Projects.webgis_api_mcp>("webgis-api-mcp");

#endregion

builder.Build().Run();
