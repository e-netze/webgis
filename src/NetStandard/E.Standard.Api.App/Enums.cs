using System;

namespace E.Standard.Api.App;

[Flags]
public enum AppRoles
{
    None = 0,
    All = 1,
    WebgisApi = 2,
    DataLinq = 4,
    SubscriberPages = 8,
    DataLinqStudio = 16
}

public enum CustomStaticFilesLocation
{
    InContainer = 0,
    InSharedStorage = 1
}
