#nullable enable

using E.Standard.Api.App.Services;
using E.Standard.Api.App.Services.Cache;
using E.Standard.Caching.Services;
using E.Standard.DbConnector.Schema;
using E.Standard.WebGIS.SubscriberDatabase.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace E.Standard.Api.App;

public class Setup
{
    public string Start(CacheService cacheService,
                        KeyValueCacheService? keyValueCacke,
                        SubscriberDatabaseService? subscriberDb,
                        IEnumerable<IExpectableUserRoleNamesProvider> expectableUserRolesNamesProviders)
    {
        bool succeeded = true;

        cacheService.Init(expectableUserRolesNamesProviders);

        Dictionary<string, object?> instances = new Dictionary<string, object?>();

        instances.Add("Cache", keyValueCacke?.KeyValueCacheInstance);
        instances.Add("Cache Aside", keyValueCacke?.KeyValueCacheAsideInstance);
        instances.Add("Subscriber Db", subscriberDb?.CreateInstance());
        instances.Add("P4", ApiGlobals.SRefStore.SpatialReferences);

        StringBuilder console = new StringBuilder();

        console.Append("SETUP\n");
        console.Append("############################################################\n");

        foreach (string key in instances.Keys)
        {
            console.Append("\n\n" + key + "\n");
            console.Append("------------------------------------------------------------\n");

            var instance = instances[key];
            if (instance == null)
            {
                console.Append("not set\n");
                continue;
            }
            console.Append("Type: " + instance.GetType().ToString() + "\n");

            if (instance is IDbSchemaProvider)
            {
                //console.Append(((IDbSchemaProvider)instance).DbConnectionString + "\n");
                var schemaProvider = (IDbSchemaProvider)instance;
                if (schemaProvider.DbConnectionString == "#")
                {
                    console.Append("in memory");
                    continue;
                }

                var schemaHelper = new SchemaHelper();
                try
                {
                    if (schemaHelper.SchemaExists(schemaProvider))
                    {
                        console.Append("Schema exists\n");
                    }
                    else
                    {
                        console.Append("Creating DB Schema...\n");

                        schemaHelper.Create(schemaProvider);
                        console.Append("succeeded\n");
                    }
                }
                catch (Exception ex)
                {
                    console.Append("************************************************************\n");
                    console.Append("EXCEPTION:\n");
                    console.Append(ex.Message + "\n");
                    console.Append("************************************************************\n\n");
                    succeeded = false;
                }
            }

        }
        console.Append("\n\nfinished " + (succeeded ? "successfull" : " with errors") + "\n");

        if (succeeded)
        {
            cacheService.Clear(expectableUserRolesNamesProviders);
        }

        return console.ToString();
    }
}

