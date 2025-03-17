#nullable enable

using E.DataLinq.Core.Models;
using E.DataLinq.Core.Services.Persistance.Abstraction;
using E.Standard.Api.App.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Core.AppCode.Services.DataLinq;

class DataLinqExpectableUserRoleNamesProvider : IExpectableUserRoleNamesProvider
{
    private List<string>? _userNames = null;
    private List<string>? _userRoles = null;

    private readonly ILogger<DataLinqExpectableUserRoleNamesProvider> _logger;
    private readonly IPersistanceProviderService _persistanceProvider;

    public DataLinqExpectableUserRoleNamesProvider(
            ILogger<DataLinqExpectableUserRoleNamesProvider> logger,
            IPersistanceProviderService persistanceProvider
        )
    {
        _logger = logger;
        _persistanceProvider = persistanceProvider;
    }

    public string GroupName => "datalinq";

    async public Task<IEnumerable<string>> ExpectableUserNames()
    {

        if (_userNames == null)
        {
            await DeterminePosibleUserRoleNames();
        }

        return _userNames!.Distinct().ToArray();
    }

    async public Task<IEnumerable<string>> ExpectableUserRoles()
    {

        if (_userRoles == null)
        {
            await DeterminePosibleUserRoleNames();
        }

        return _userRoles!.Distinct().ToArray();
    }

    async private Task DeterminePosibleUserRoleNames()
    {
        _userNames = new List<string>();
        _userRoles = new List<string>();

        foreach (string endPointId in await _persistanceProvider.GetEndPointIds(null) ?? new string[0])
        {
            DataLinqEndPoint? endPoint = null;

            try
            {
                endPoint = await _persistanceProvider.GetEndPoint(endPointId);
                _logger.LogInformation("Successfully readed DataLinq Endpoint {endPointId}", endPointId);
            }
            catch (Exception ex)
            {
                _logger.LogError("Try read DataLinq Endpoint {endPointId}: {message}", endPointId, ex.Message);
            }

            if (endPoint == null)
            {
                continue;
            }

            AddAccessNames(endPoint.Access);

            foreach (var queryId in await _persistanceProvider.GetQueryIds(endPointId) ?? new string[0])
            {
                DataLinqEndPointQuery? query = null;

                try
                {
                    query = await _persistanceProvider.GetEndPointQuery(endPointId, queryId);
                    _logger.LogInformation("Successfully readed DataLinq Query {endPointId}@{queryId}", endPointId, queryId);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Try read DataLinq Query {endPointId}@{queryId}: {message}", endPointId, queryId, ex.Message);
                }

                AddAccessNames(query?.Access);
            }
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("Determined {count} datalinq users: [{datalinqUsers}]", _userNames.Count, String.Join(",", _userNames));
            _logger.LogInformation("Determined {count} datalinq roles: [{datalinqRoles}]", _userRoles.Count, String.Join(",", _userRoles));
        }
    }

    private void AddAccessNames(string[]? accessRoles)
    {
        if (accessRoles == null || _userNames == null || _userRoles == null)
        {
            return;
        }

        foreach (var name in accessRoles)
        {
            if (String.IsNullOrEmpty(name) || name == "*")
            {
                continue;
            }

            if (!name.Contains("::"))
            {
                if (!_userNames.Contains(name))
                {
                    _userNames.Add(name);
                }

                if (!_userRoles.Contains(name))
                {
                    _userRoles.Add(name);
                }
            }
            else
            {
                string prefix = name.Substring(0, name.IndexOf("::"));

                if (prefix.Contains("-role") ||
                   prefix.Contains("-group"))
                {
                    if (!_userRoles.Contains(name))
                    {
                        _userRoles.Add(name);
                    }
                }
                else
                {
                    if (!_userNames.Contains(name))
                    {
                        _userNames.Add(name);
                    }
                }
            }
        }
    }
}
