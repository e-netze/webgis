using Api.Core.Models.Storage;
using Cms.Models;
using E.Standard.Api.App.DTOs;
using E.Standard.Caching.FileSystem;
using E.Standard.Cms.Configuration.Models;
using E.Standard.CMS.UI.Controls;
using E.Standard.Configuration.Providers;
using E.Standard.Custom.Core.Models;
using E.Standard.GeoJson;
using E.Standard.Json.Test.Extensions;
using E.Standard.Security.App.Json;
using E.Standard.Ticket.LoginService;
using E.Standard.WebGIS.Core.Models;
using E.Standard.WebGIS.SubscriberDatabase;
using E.Standard.WebGIS.Tools.Editing.Mobile;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.UI;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;
using E.Standard.WebMapping.GeoServices.ArcServer.Rest;
using E.Standard.WebMapping.GeoServices.LuceneServer.Models;
using Portal.Core.Models.Portal;
using Xunit.Abstractions;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace E.Standard.Json.Test;

public class JsonSerializerTests
{
    static JsonSerializerTests()
    {
        JsonOptions.SerializerOptions.AddServerDefaults();
    }

    private readonly ITestOutputHelper _testOutputHelper;

    public JsonSerializerTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Theory]
    [InlineData(typeof(ExtentDTO), true)]  // E.Standard.Api.App
    [InlineData(typeof(ApiAppDTO), true)]  // E.Standard.WebGIS.Core
    [InlineData(typeof(FileSystemCache), true)]  // E.Standard.Caching
    [InlineData(typeof(CmsConfig), true)]  // E.Standard.Cms.Configuration
    [InlineData(typeof(CheckBox), true)]   // E.Standard.CMS.UI
    [InlineData(typeof(HostingEnvironmentJsonConfigurationProvider), true)]  // E.Standard.Configuration
    [InlineData(typeof(EventMetadata), true)]  // E.Standard.Custom.Core
    [InlineData(typeof(GeoJsonFeature), true)]  // E.Standard.GeoJson
    [InlineData(typeof(ApplicationSecurityConfig), true)]  // E.Standard.Security.App
    [InlineData(typeof(Login), true)]  // E.Standard.Ticket.LoginService
    [InlineData(typeof(Files), true)]  // E.Standard.WebGIS.Tools
    [InlineData(typeof(IApiToolEvent), true)]  // E.Standard.WebMapping.Core.Api
    [InlineData(typeof(FeatureService), true)]   // E.Standard.WebMapping.GeoServices
    [InlineData(typeof(Meta), true)]  // E.Standard.WebMapping.Services.LuceneServer
    [InlineData(typeof(StorageResponse), true)]  // Api
    [InlineData(typeof(CmsModel), true)]   // Cms
    [InlineData(typeof(PortalModel), true)]   // Portal
    [InlineData(typeof(SubscriberDb), true)]   // E.Standard.WebGIS.SubscrierDatabase
    [InlineData(typeof(SubscriberDb.Client))]
    public void SerializeDTOs(Type dtoInputType, bool parseAssembly = false)
    {
        // Arrange
        Type[] dtoTypes = parseAssembly
            ? dtoInputType.GetAllAssemblyTypes(t =>
               (t.Name.EndsWith("DTO") || t.HasJsonAttribute())
               && t.GetConstructor([]) != null)
            : [dtoInputType];

        // Act
        foreach (var dtoType in dtoTypes)
        {
            var dtoInstance = Activator.CreateInstance(dtoType);

            FakeData.SetFakeProperties(dtoInstance!);

            JSerializer.SetEngine(JsonEngine.SytemTextJson);
            string json1 = JSerializer.Serialize(dtoInstance);

            JSerializer.SetEngine(JsonEngine.NewtonSoft);
            string json2 = JSerializer.Serialize(dtoInstance);

            // Assert
            _testOutputHelper.WriteLine($"Compare {dtoType}:");
            Assert.Equal(json1, json2);
        }

        _testOutputHelper.WriteLine($"{dtoTypes.Length}...succeeded");
    }

    [Fact]
    public void SerializeUIElements()
    {
        // Arrange
        var uiElementTypes = typeof(UIElement).GetAllAssemblyTypes(t =>
            typeof(IUIElement).IsAssignableFrom(t)
            && t.GetConstructor([]) != null);

        // Act
        foreach (var uiElementType in uiElementTypes)
        {
            var elementInstance = Activator.CreateInstance(uiElementType);

            FakeData.SetFakeProperties(elementInstance!);

            JSerializer.SetEngine(JsonEngine.SytemTextJson);
            string json1 = JSerializer.Serialize(elementInstance);

            JSerializer.SetEngine(JsonEngine.NewtonSoft);
            string json2 = JSerializer.Serialize(elementInstance);

            // Assert
            _testOutputHelper.WriteLine($"Compare {uiElementType}:");
            Assert.Equal(json1, json2);
        }

        _testOutputHelper.WriteLine($"{uiElementTypes.Length}...succeeded");
    }

    [Fact]
    public void SerializeUIUISetters()
    {
        // Arrange
        var uiSetterTypes = typeof(UISetter).GetAllAssemblyTypes(t =>
            typeof(IUISetter).IsAssignableFrom(t)
            && t.GetConstructor([]) != null);

        // Act
        foreach (var uiSetterType in uiSetterTypes)
        {
            var setterInstance = Activator.CreateInstance(uiSetterType);

            FakeData.SetFakeProperties(setterInstance!);

            JSerializer.SetEngine(JsonEngine.SytemTextJson);
            string json1 = JSerializer.Serialize(setterInstance);

            JSerializer.SetEngine(JsonEngine.NewtonSoft);
            string json2 = JSerializer.Serialize(setterInstance);

            // Assert
            _testOutputHelper.WriteLine($"Compare {uiSetterType}:");
            Assert.Equal(json1, json2);
        }

        _testOutputHelper.WriteLine($"{uiSetterTypes.Length}...succeeded");
    }

    [Fact]
    public void SerializeFeatures()
    {
        // Arrange
        var feaaturesDTO = new FeaturesDTO(new FakeData().CreateFeatureCollectionWithPointLinePolygon());

        // Act
        JSerializer.SetEngine(JsonEngine.SytemTextJson);
        string json1 = JSerializer.Serialize(feaaturesDTO);

        JSerializer.SetEngine(JsonEngine.NewtonSoft);
        string json2 = JSerializer.Serialize(feaaturesDTO);

        _testOutputHelper.WriteLine(JSerializer.Serialize(feaaturesDTO, pretty: true));

        // Assert
        Assert.Equal(json1, json2);
    }

    [Theory]
    [InlineData(@"{""type"":""FeatureCollection"",""features"":[{""type"":""Feature"",""geometry"":{""type"":""LineString"",""coordinates"":[[11.193379,48.096199],[10.687107,47.392914],[11.598396,47.221335],[10.619604,46.401636]]},""properties"":{""stroke"":""#ff0000"",""stroke-opacity"":0.8,""stroke-width"":2,""stroke-style"":""1"",""_meta"":{""tool"":""line"",""text"":""linie"",""source"":null}}},{""type"":""Feature"",""geometry"":{""type"":""Point"",""coordinates"":[10.822112844309359,48.24807513875295]},""properties"":{""font-color"":""#000"",""font-style"":"""",""font-size"":12,""_meta"":{""tool"":""text"",""text"":""bla"",""source"":null}}},{""type"":""Feature"",""geometry"":{""type"":""Point"",""coordinates"":[11.876845,48.203122]},""properties"":{""symbol"":""graphics/markers/pin1.png"",""_meta"":{""tool"":""symbol"",""symbol"":{""id"":""graphics/markers/pin1.png"",""icon"":""graphics/markers/pin1.png"",""iconSize"":[23,32],""iconAnchor"":[0,31],""popupAnchor"":[11,-31]}}}},{""type"":""Feature"",""geometry"":{""type"":""Point"",""coordinates"":[11.57308202596506,48.05111272661141]},""properties"":{""point-color"":""#ff0000"",""point-size"":10,""_meta"":{""tool"":""point"",""text"":""point""}}},{""type"":""Feature"",""geometry"":{""type"":""Polygon"",""coordinates"":[[[11.800904,46.46558],[11.328384,46.535252],[11.809342,46.870742],[12.551873,46.772625],[11.800904,46.46558]]]},""properties"":{""stroke"":""#ff0000"",""stroke-opacity"":0.8,""stroke-width"":2,""stroke-style"":""1"",""fill"":""#ffff00"",""fill-opacity"":0.2,""_meta"":{""tool"":""polygon"",""text"":null,""source"":null}}},{""type"":""Feature"",""geometry"":{""type"":""Point"",""coordinates"":[12.08779124035829,47.59238433643274]},""properties"":{""stroke"":""#000000"",""stroke-width"":2,""fill"":""#aaaaaa"",""fill-opacity"":0.8,""dc-radius"":30475.33207618734,""dc-steps"":3,""_meta"":{""tool"":""distance_circle"",""text"":null,""source"":null}}},{""type"":""Feature"",""geometry"":{""type"":""Point"",""coordinates"":[10.467722893415667,47.81375957008931]},""properties"":{""stroke"":""#000000"",""stroke-width"":2,""cr-radius"":25760.339922148218,""cr-steps"":36,""_meta"":{""tool"":""compass_rose"",""text"":null,""source"":null}}},{""type"":""Feature"",""geometry"":{""type"":""LineString"",""coordinates"":[[9.784256559549252,47.63787128843365],[9.683002287865357,47.88168593128883],[9.893948687206828,48.01727182615343],[10.088019374601013,48.14687459989826],[12.24811050385783,48.60070365209871],[12.577186886830555,48.544894193673535]]},""properties"":{""stroke"":""#000000"",""stroke-width"":2,""font-color"":""#000"",""font-style"":"""",""font-size"":""14"",""hl-unit"":""km"",""hl-interval"":100,""_meta"":{""tool"":""hectoline"",""text"":null,""source"":null}}},{""type"":""Feature"",""geometry"":{""type"":""LineString"",""coordinates"":[[10.1048950865483,48.337863385419],[10.771485708467425,48.47224978892186],[11.201816363124047,48.60070365209871]]},""properties"":{""stroke"":""#000000"",""stroke-width"":2,""font-color"":""#000"",""font-style"":"""",""font-size"":14,""_meta"":{""tool"":""dimline"",""text"":null,""source"":null}}}]}")]
    public void Serialize_And_Deserialze_RedlingObject(string graphicsJsonString)
    {
        // Act
        JSerializer.SetEngine(JsonEngine.SytemTextJson);
        var graphicsObject1 = JSerializer.Deserialize(graphicsJsonString, typeof(object));
        var formattedGraphics1 = JSerializer.Serialize(graphicsObject1, pretty: false);

        JSerializer.SetEngine(JsonEngine.NewtonSoft);
        var graphicsObject2 = JSerializer.Deserialize(graphicsJsonString, typeof(object));
        var formattedGraphics2 = JSerializer.Serialize(graphicsObject2, pretty: false);

        // Assert
        Assert.Equal(formattedGraphics1, formattedGraphics2);

    }
}
