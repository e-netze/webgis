using E.Standard.GeoCoding.GeoCode;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.GeoCoding.Extensions.DependencyInjection;

static public class ServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddGeoCoders(string[] names)
        {
            foreach(var geoCoder in GeoCodingGeneralExtensions.AllGeoCoders())
            {
                if (names.Contains("*") || names.Contains(geoCoder.Name, StringComparer.OrdinalIgnoreCase))
                {
                    services.AddTransient(typeof(IGeoCoder), geoCoder.GetType());
                }
            }

            return services;
        }
    }
}
