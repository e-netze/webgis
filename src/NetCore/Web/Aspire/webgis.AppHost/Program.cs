//#define ADD_IDENTITYSERVER
//#define ADD_MESSAGEQUEUE
//#define ADD_REDIS
//#define ADD_MCP
//#define ADD_GVIEW
//#define ADD_POSTGRES
//#define ADD_DEVTUNNELS

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
       //.WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
       ;

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

#if ADD_POSTGRES

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

#if ADD_GVIEW
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
                        .WithUrlForEndpoint("https", ep =>
                        {
                            ep.DisplayText = "API";
                            ep.DisplayOrder = 1;
                            ep.DisplayLocation = UrlDisplayLocation.SummaryAndDetails;
                        })
                        .WithUrlForEndpoint("http", ep => { ep.DisplayLocation = UrlDisplayLocation.DetailsOnly; })
                        .WithUrlForEndpoint("https", ep => new()
                        {
                            DisplayText = "CachClear",
                            DisplayOrder = 0,
                            Url = $"{ep.Url}/cache/clear",
                            DisplayLocation = UrlDisplayLocation.SummaryAndDetails
                        })
                        ;


var webgisPortal = builder.AddProject<Projects.webgis_portal>("webgis-portal")
                          .WaitFor(webgisApi)
#if ADD_IDENTITYSERVER
                          .WaitFor(identityServer)
#endif
                          .WithUrlForEndpoint("https", ep =>
                          {
                              ep.DisplayText = "Login";
                              ep.DisplayOrder = 1;
                              ep.DisplayLocation = UrlDisplayLocation.SummaryAndDetails;
                          })
                         .WithUrlForEndpoint("http", ep => { ep.DisplayLocation = UrlDisplayLocation.DetailsOnly; })
                         .WithUrlForEndpoint("https", ep => new()
                         {
                             DisplayText = "Portal",
                             DisplayOrder = 0,
                             Url = $"{ep.Url}/default",
                             DisplayLocation = UrlDisplayLocation.SummaryAndDetails
                         })
                         ;

var webgisCms = builder.AddProject<Projects.webgis_cms>("webgis-cms")
                       .WaitFor(webgisPortal)
                       .WaitFor(webgisApi)
                       .WithUrlForEndpoint("https", ep =>
                        {
                            ep.DisplayText = "CMS";
                            ep.DisplayOrder = 1;
                            ep.DisplayLocation = UrlDisplayLocation.SummaryAndDetails;
                        })
                       .WithUrlForEndpoint("http", ep => { ep.DisplayLocation = UrlDisplayLocation.DetailsOnly; })
                       ;

#endregion

#if ADD_DEVTUNNELS

builder.AddDevTunnel("webgis-api-tunnel").WithReference(webgisApi);
builder.AddDevTunnel("webgis-portal-tunnel").WithReference(webgisPortal);
builder.AddDevTunnel("webgis-cms-tunnel").WithReference(webgisCms);

#endif

#region MCP

#if ADD_MCP

builder.AddProject<Projects.webgis_api_mcp>("webgis-api-mcp");

#endif

#endregion

builder.Build().Run();
