using E.Standard.WebMapping.Core.Api.Abstraction;
using System;

namespace E.Standard.WebGIS.Tools;

public class Favorites : IApiButton
{
    public string Name => "Favorites";

    public string Container => String.Empty;

    public string Image => String.Empty;

    public string ToolTip => String.Empty;

    public bool HasUI => false;

    #region Static Members

    static private Favorites _instance = new Favorites();
    static public Favorites Instance => _instance;

    #endregion
}
