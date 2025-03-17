using E.Standard.WebMapping.Core.Api.Bridge;
using System;

namespace E.Standard.WebMapping.Core.Api.UI.Setters;

public class UIAnonymousUserIdSetter : UISetter
{
    public UIAnonymousUserIdSetter(IBridge bridge, Guid anonymousUserGuid)
        : base(String.Empty, bridge.CreateAnonymousClientSideUserId(anonymousUserGuid))
    {
        this.name = "_anonymous-user-id";
    }
}
