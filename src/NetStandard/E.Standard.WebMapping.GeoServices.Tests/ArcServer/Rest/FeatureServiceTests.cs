using E.Standard.WebMapping.Core.Editing;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
