using System.Reflection;

using E.Standard.WebMapping.Core.Editing;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest;

namespace E.Standard.WebMapping.GeoServices.Tests.ArcServer.Rest;


public class FeatureServiceTests
{
    [Fact]
    public void AssertFeatureServiceHasParameterlessConstructor()
    {
        // Arrange & Act: create it like in old legacy code with workspaces.xml
        Assembly assembly = typeof(FeatureService).Assembly;
        IFeatureWorkspace? ws = assembly.CreateInstance(typeof(FeatureService).FullName!, false) as IFeatureWorkspace;

        // Assert
        Assert.NotNull(ws);
    }
}
