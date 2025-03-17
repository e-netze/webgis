using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace E.Standard.Security.Services.Database
{
    [Obsolete("Use E.Standard.Security.Internal assembly")]
    public class TicketDbService
    {
        private readonly string _connectionString;

        public TicketDbService(
            ISecurityConfigurationService config,
            IOptionsMonitor<TicketDbServiceOptions> options)
        {
            _connectionString = config[options.CurrentValue.ConnectionStringConfigurationKey];
        }

        public TicketDb CreateInstance(object context = null)
        {
            if(String.IsNullOrEmpty(_connectionString))
            {
                return null;
            }

            return new TicketDb(context, _connectionString);
        }
    }
}
