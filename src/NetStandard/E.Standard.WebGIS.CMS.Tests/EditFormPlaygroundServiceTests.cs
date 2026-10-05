using System.Xml;

using E.Standard.WebGIS.CMS.Expressions;

namespace E.Standard.WebGIS.CMS.Tests;

public class EditFormPlaygroundServiceTests
{
    [Fact]
    public void GetThemes_ReadsConfiguredFieldsAndDoesNotExposeDatabaseSettings()
    {
        var themes = EditFormPlaygroundService.GetThemes(CreateCmsDocument());

        var theme = Assert.Single(themes);
        Assert.Equal("maps/demo/services/mapserver/roads/Editing/roads-edit", theme.Path);
        Assert.Equal("Road editor", theme.Name);
        Assert.Equal("maps/demo/services/mapserver/roads/Themes/road-layer", theme.ServiceTheme);
        var field = Assert.Single(theme.Fields);
        Assert.Equal("Road name", field.Name);
        Assert.Equal("ROAD_NAME", field.FieldName);
        Assert.True(field.Visible);
        Assert.True(field.Required);
        Assert.False(field.Readonly);
        Assert.Equal("=upper([ROAD_NAME])", field.AutoValue);
    }

    [Fact]
    public void Evaluate_UsesConfiguredExpressionAndFeatureProperties()
    {
        var results = EditFormPlaygroundService.Evaluate(
            CreateCmsDocument(),
            new EditFormPlaygroundRequest
            {
                CmsId = "demo",
                ThemePath = "maps/demo/services/mapserver/roads/Editing/roads-edit",
                Operation = "insert",
                Fields = ["road-name-editor"],
                GeoJson = """
                    {"type":"Feature","geometry":null,"properties":{"ROAD_NAME":"Main Street"}}
                    """
            });

        var result = Assert.Single(results);
        Assert.Equal("ROAD_NAME", result.FieldName);
        Assert.Equal(1, result.Feature);
        Assert.Equal("MAIN STREET", result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Evaluate_SimulatesOperationAndUserForStandardAutoValues()
    {
        var cmsDocument = CreateCmsDocument(
            """
            <item name="USER_NAME" type="file"><![CDATA[<config><name>User</name><field>USER_NAME</field><autovalue>201</autovalue></config>]]></item>
            """);

        var results = EditFormPlaygroundService.Evaluate(
            cmsDocument,
            new EditFormPlaygroundRequest
            {
                CmsId = "demo",
                ThemePath = "maps/demo/services/mapserver/roads/Editing/roads-edit",
                Operation = "insert",
                Username = "domain::alice@example.com",
                Fields = ["USER_NAME"],
                GeoJson = """
                    {"type":"Feature","geometry":null,"properties":{}}
                    """
            });

        var result = Assert.Single(results);
        Assert.Equal("alice@example.com", result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Evaluate_UsesSimulatedRoleParametersAndOperationContext()
    {
        var cmsDocument = CreateCmsDocument(
            """
            <item name="DEPARTMENT" type="file"><![CDATA[<config><name>Department</name><field>DEPARTMENT</field><autovalue>1</autovalue><customautovalue>oninsert:role-parameter:department</customautovalue></config>]]></item>
            <item name="EDIT_OPERATION" type="file"><![CDATA[<config><name>Operation</name><field>EDIT_OPERATION</field><autovalue>402</autovalue></config>]]></item>
            """);

        var results = EditFormPlaygroundService.Evaluate(
            cmsDocument,
            new EditFormPlaygroundRequest
            {
                CmsId = "demo",
                ThemePath = "maps/demo/services/mapserver/roads/Editing/roads-edit",
                Operation = "insert",
                Fields = ["DEPARTMENT", "EDIT_OPERATION"],
                ValuesJson = """{"department":"West"}""",
                GeoJson = """
                    {"type":"Feature","geometry":null,"properties":{}}
                    """
            });

        Assert.Equal(2, results.Count);
        Assert.Equal("West", results[0].Value);
        Assert.Equal("insert", results[1].Value);
        Assert.All(results, result => Assert.Null(result.Error));
    }

    [Fact]
    public void Evaluate_ExecutesDbSelectAgainstSqliteWithGeoJsonProperties()
    {
        var dbFile = Path.Combine(Path.GetTempPath(), $"playground-{Guid.NewGuid():N}.db");
        try
        {
            var connectionString = $"sqlite:Data Source={dbFile}";
            using (var factory = new E.Standard.DbConnector.DBFactory(connectionString))
            using (var connection = factory.GetConnection())
            using (var command = factory.GetCommand(connection))
            {
                connection.Open();
                command.CommandText = "create table lookup (code text, label text); insert into lookup values ('A', 'Alpha');";
                command.ExecuteNonQuery();
            }

            var placeholder = "{{CODE}}";
            var cmsDocument = CreateCmsDocument(
                $$"""
                <item name="DB_VALUE" type="file"><![CDATA[<config><name>Database</name><field>DB_VALUE</field><autovalue>601</autovalue><customautovalue>{{connectionString}}</customautovalue><customautovalue2>select label from lookup where code = {{placeholder}}</customautovalue2></config>]]></item>
                """);

            var results = EditFormPlaygroundService.Evaluate(
                cmsDocument,
                new EditFormPlaygroundRequest
                {
                    CmsId = "demo",
                    ThemePath = "maps/demo/services/mapserver/roads/Editing/roads-edit",
                    Operation = "insert",
                    Fields = ["DB_VALUE"],
                    GeoJson = """
                        {"type":"FeatureCollection","features":[
                          {"type":"Feature","geometry":null,"properties":{"code":"A"}},
                          {"type":"Feature","geometry":null,"properties":{}}]}
                        """
                });

            Assert.Equal(2, results.Count);
            Assert.Equal("Alpha", results[0].Value);
            Assert.Null(results[0].Error);
            Assert.Null(results[1].Value);
            Assert.Contains("CODE", results[1].Error);
        }
        finally
        {
            System.Data.SQLite.SQLiteConnection.ClearAllPools();
            File.Delete(dbFile);
        }
    }

    [Fact]
    public async Task Evaluate_ExecutesDbSelectAgainstDataLinqEndpoint()
    {
        var port = new Random().Next(20000, 40000);
        var prefix = $"http://localhost:{port}/";
        using var listener = new System.Net.HttpListener();
        listener.Prefixes.Add(prefix);
        listener.Start();
        var requestUrl = String.Empty;
        var serverTask = Task.Run(async () =>
        {
            var context = await listener.GetContextAsync();
            requestUrl = context.Request.Url!.PathAndQuery;
            var bytes = System.Text.Encoding.UTF8.GetBytes("""[{"name":"Alpha","value":"1"}]""");
            await context.Response.OutputStream.WriteAsync(bytes);
            context.Response.Close();
        });

        var placeholder = "{{CODE}}";
        var cmsDocument = CreateCmsDocument(
            $$"""
            <item name="DB_VALUE" type="file"><![CDATA[<config><name>Database</name><field>DB_VALUE</field><autovalue>601</autovalue><customautovalue>{{prefix}}datalinq/q</customautovalue><customautovalue2>code={{placeholder}}</customautovalue2></config>]]></item>
            """);

        var results = await Task.Run(() => EditFormPlaygroundService.Evaluate(
            cmsDocument,
            new EditFormPlaygroundRequest
            {
                CmsId = "demo",
                ThemePath = "maps/demo/services/mapserver/roads/Editing/roads-edit",
                Operation = "insert",
                Fields = ["DB_VALUE"],
                GeoJson = """
                    {"type":"Feature","geometry":null,"properties":{"code":"A B"}}
                    """
            }));
        await serverTask;

        var result = Assert.Single(results);
        Assert.Equal("Alpha", result.Value);
        Assert.Null(result.Error);
        Assert.Equal("/datalinq/q?code=A%20B", requestUrl);
    }
    [Fact]
    public void Evaluate_DbSelectWithUnresolvedSecretReportsHint()
    {
        var cmsDocument = CreateCmsDocument(
            """
            <item name="DB_VALUE" type="file"><![CDATA[<config><name>Database</name><field>DB_VALUE</field><autovalue>601</autovalue><customautovalue>{{secret-datalinq}}@lookup</customautovalue><customautovalue2>code={{CODE}}</customautovalue2></config>]]></item>
            """);

        var results = EditFormPlaygroundService.Evaluate(
            cmsDocument,
            new EditFormPlaygroundRequest
            {
                CmsId = "demo",
                ThemePath = "maps/demo/services/mapserver/roads/Editing/roads-edit",
                Operation = "insert",
                Fields = ["DB_VALUE"],
                GeoJson = """
                    {"type":"Feature","geometry":null,"properties":{"code":"A"}}
                    """
            });

        var result = Assert.Single(results);
        Assert.Null(result.Value);
        Assert.Contains("unresolved CMS secret", result.Error);
    }
    [Fact]
    public void Evaluate_DbSelectWithoutConnectionStringReportsConfigurationError()
    {
        var cmsDocument = CreateCmsDocument(
            """
            <item name="DB_VALUE" type="file"><![CDATA[<config><name>Database</name><field>DB_VALUE</field><autovalue>601</autovalue></config>]]></item>
            """);

        var results = EditFormPlaygroundService.Evaluate(
            cmsDocument,
            new EditFormPlaygroundRequest
            {
                CmsId = "demo",
                ThemePath = "maps/demo/services/mapserver/roads/Editing/roads-edit",
                Operation = "insert",
                Fields = ["DB_VALUE"],
                GeoJson = """
                    {"type":"Feature","geometry":null,"properties":{}}
                    """
            });

        var result = Assert.Single(results);
        Assert.Null(result.Value);
        Assert.Contains("ConnectionString not set", result.Error);
    }
    [Fact]
    public void Evaluate_PassesThroughGeoJsonAttributeForFieldWithoutAutoValue()
    {
        var cmsDocument = CreateCmsDocument(
            """
            <item name="NOTE" type="file"><![CDATA[<config><name>Note</name><field>NOTE</field><autovalue>0</autovalue></config>]]></item>
            """);

        var results = EditFormPlaygroundService.Evaluate(
            cmsDocument,
            new EditFormPlaygroundRequest
            {
                CmsId = "demo",
                ThemePath = "maps/demo/services/mapserver/roads/Editing/roads-edit",
                Operation = "insert",
                Fields = ["NOTE"],
                GeoJson = """
                    {"type":"Feature","geometry":null,"properties":{"note":"hello"}}
                    """
            });

        var result = Assert.Single(results);
        Assert.Equal("hello", result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Evaluate_RejectsUnselectedOrUnknownField()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            EditFormPlaygroundService.Evaluate(
                CreateCmsDocument(),
                new EditFormPlaygroundRequest
                {
                    CmsId = "demo",
                    ThemePath = "maps/demo/services/mapserver/roads/Editing/roads-edit",
                    Operation = "insert",
                    Fields = ["OTHER"],
                    GeoJson = """
                        {"type":"Feature","geometry":null,"properties":{}}
                        """
                }));

        Assert.Contains("not in this editing theme", exception.Message);
    }

    private static XmlDocument CreateCmsDocument(string extraFields = "")
    {
        var cmsDocument = new XmlDocument();
        cmsDocument.LoadXml(
            $$"""
            <CMS root="demo">
              <item name="maps" type="directory">
                <item name="demo" type="directory">
                  <item name="services" type="directory">
                    <item name="mapserver" type="directory">
                      <item name="roads" type="directory">
                        <item name="Editing" type="directory">
                          <item name="roads-edit" displayname="Road editor" filtertype="edittheme" type="directory">
                            <item name="editingtheme" type="directory">
                              <item name="road-layer" type="link" target="maps/demo/services/mapserver/roads/Themes/road-layer" />
                            </item>
                            <item name="editingfields" type="directory">
                              <item name="General" displayname="General" type="directory">
                                <item name="road-name-editor" type="file"><![CDATA[<config><name>Road name</name><field>ROAD_NAME</field><type>0</type><visible>true</visible><readonly>false</readonly><required>true</required><autovalue>1</autovalue><customautovalue>=upper([ROAD_NAME])</customautovalue></config>]]></item>
                                {{extraFields}}
                              </item>
                            </item>
                          </item>
                        </item>
                      </item>
                    </item>
                  </item>
                </item>
              </item>
            </CMS>
            """);
        return cmsDocument;
    }
}
