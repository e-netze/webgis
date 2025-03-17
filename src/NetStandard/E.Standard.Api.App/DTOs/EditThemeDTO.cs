using E.Standard.Api.App.Extensions;
using E.Standard.Api.App.Models.Abstractions;
using E.Standard.Api.App.Services.Cache;
using E.Standard.CMS.Core;
using E.Standard.WebGIS.CMS;
using E.Standard.WebMapping.Core.Api.Bridge;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace E.Standard.Api.App.DTOs;

public sealed class EditThemeDTO : VersionDTO, IHtml, IEditThemeBridge
{
    [JsonProperty(PropertyName = "name")]
    [System.Text.Json.Serialization.JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonProperty(PropertyName = "themeid")]
    [System.Text.Json.Serialization.JsonPropertyName("themeid")]
    public string ThemeId { get; set; }

    [JsonProperty(PropertyName = "layerid")]
    [System.Text.Json.Serialization.JsonPropertyName("layerid")]
    public string LayerId { get; set; }

    [JsonProperty(PropertyName = "visible")]
    [System.Text.Json.Serialization.JsonPropertyName("visible")]
    public bool Visible { get; set; }

    [JsonProperty(PropertyName = "snapping")]
    [System.Text.Json.Serialization.JsonPropertyName("snapping")]
    public IEnumerable<SnappingScheme> Snapping { get; set; }

    [JsonProperty(PropertyName = "dbrights", NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonPropertyName("dbrights")]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string DbRightsString => this.DbRights == EditingRights.Unknown ? null : this.DbRights.ToDbRightsString();

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool CanGenerateMaskXml { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public int Srs { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public EditingRights DbRights { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsEditServiceTheme { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public IEnumerable<InsertAction> InsertActions { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public bool AutoExplodeMultipartFeatures { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public IEnumerable<AuthObject<EditField>> EditFields { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public IEnumerable<AuthObject<EditCategory>> EditCategories { get; set; }

    [JsonIgnore]
    [System.Text.Json.Serialization.JsonIgnore]
    public IEnumerable<AuthObject<MaskValidation>> MaskValidations { get; set; }

    public XmlNode GenerateMaskXml(CmsDocument.UserIdentification ui)
    {
        if (!CanGenerateMaskXml)
        {
            return null;
        }

        StringBuilder xml = new StringBuilder();
        xml.Append(@"<?xml version=""1.0"" encoding=""utf-8""?>");
        xml.Append(@"<editthemes xmlns:edit=""http://www.e-steiermark.com/webgis/edit"" xmlns:webgis=""http://www.e-steiermark.com/webgis"">");

        // ToDo: Xml Encode Values

        xml.Append($@"<edit:edittheme id=""{this.ThemeId.EscapeXmlString()}"" name=""{this.Name.EscapeXmlString()}"" dbrights=""{this.DbRights.ToDbRightsString()}"" srs=""{this.Srs}"" >");
        xml.Append(@"<edit:connection id=""#"" />");

        xml.Append(@"<edit:mask>");
        Dictionary<string, string> categories = new Dictionary<string, string>();

        foreach (var editField in AuthObject<EditField>.QueryObjectArray(this.EditFields, ui))
        {
            var category = AuthObject<EditCategory>.QueryObjectArray(this.EditCategories, ui)?.Where(c => c.Id == editField.CategoryId).FirstOrDefault();
            if (category == null)
            {
                continue;
            }

            if (editField.FieldType == "info")
            {
                xml.Append($@"<edit:attribute type=""info""");
                if (!String.IsNullOrEmpty(editField.Field))
                {
                    xml.Append($@" label=""{editField.Field.EscapeXmlString()}""");
                }
                else
                {
                    xml.Append($@" label=""{"???".EscapeXmlString()}""");
                }
            }
            else
            {
                xml.Append($@"<edit:attribute prompt=""{editField.Prompt.EscapeXmlString()}""");
                xml.Append($@" field=""{editField.Field.EscapeXmlString()}""");
                if (editField.LegendField == true)
                {
                    xml.Append($@" legend_field=""{editField.Field.EscapeXmlString()}""");
                }
                xml.Append($@" type=""{editField.FieldType.EscapeXmlString()}""");

                xml.Append($@" visible=""{editField.Visible}""");
                xml.Append($@" readonly=""{editField.Readonly}""");

                if (editField.Resistant)
                {
                    xml.Append($@" resistant=""{editField.Resistant}""");
                }

                if (editField.MassAttributable)
                {
                    xml.Append($@" massattributable=""{editField.MassAttributable}""");
                }

                xml.Append($@" required=""{editField.Required}""");
                if (editField.MinLength > 0)
                {
                    xml.Append($@" minlen=""{editField.MinLength}""");
                }
                if (!String.IsNullOrEmpty(editField.RegexPattern))
                {
                    xml.Append($@" regex=""{editField.RegexPattern.EscapeXmlString()}""");
                }
                if (!String.IsNullOrEmpty(editField.ValidationErrorMessage))
                {
                    xml.Append($@" validation_error=""{editField.ValidationErrorMessage.EscapeXmlString()}""");
                }
                if (editField.ClientSideValidation)
                {
                    xml.Append($@" clientside_validation = ""true""");
                }

                if (!String.IsNullOrWhiteSpace(editField.AutoValue))
                {
                    xml.Append($@" autovalue=""{editField.AutoValue.EscapeXmlString()}""");
                    if (!String.IsNullOrEmpty(editField.AutoValueCustom1))
                    {
                        xml.Append($@" autovalue_custom1=""{editField.AutoValueCustom1.EscapeXmlString()}""");
                    }
                    if (!String.IsNullOrEmpty(editField.AutoValueCustom2))
                    {
                        xml.Append($@" autovalue_custom2=""{editField.AutoValueCustom2.EscapeXmlString()}""");
                    }
                }
                if (!String.IsNullOrWhiteSpace(editField.DbConnectionString))
                {
                    xml.Append($@" db_connectionstring=""{editField.DbConnectionString.EscapeXmlString()}""");
                    xml.Append($@" db_table=""{editField.DbTable.EscapeXmlString()}""");
                    xml.Append($@" db_valuefield=""{editField.DbValueField.EscapeXmlString()}""");
                    xml.Append($@" db_aliasfield=""{editField.DbAliasField.EscapeXmlString()}""");
                    xml.Append($@" db_where=""{editField.DbWhereClause.EscapeXmlString()}""");
                    xml.Append($@" db_orderby=""{editField.DbOrderBy.EscapeXmlString()}""");
                }
                else if (!String.IsNullOrWhiteSpace(editField.DomainList))
                {
                    xml.Append($@" domain_list=""{editField.DomainList.EscapeXmlString()}""");
                }
                else if (!String.IsNullOrEmpty(editField.AttributePickerServiceId) &&
                         !String.IsNullOrEmpty(editField.AttributePickerQueryId) &&
                         !String.IsNullOrEmpty(editField.AttributePickerField))
                {
                    xml.Append($@" attribute_picker_service=""{editField.AttributePickerServiceId.EscapeXmlString()}""");
                    xml.Append($@" attribute_picker_query=""{editField.AttributePickerQueryId.EscapeXmlString()}""");
                    xml.Append($@" attribute_picker_field=""{editField.AttributePickerField.EscapeXmlString()}""");
                }
            }

            if (!categories.ContainsKey(editField.CategoryId))
            {
                categories.Add(editField.CategoryId, editField.Category);
            }
            xml.Append($@" category=""{editField.CategoryId.EscapeXmlString()}""");

            xml.Append("/>");
        }

        #region Mask Validations

        if (AuthObject<MaskValidation>.QueryObjectArray(MaskValidations, ui).Any())
        {
            xml.Append("<edit:validations>");
            foreach (var maskValidation in AuthObject<MaskValidation>.QueryObjectArray(MaskValidations, ui))
            {
                xml.Append($@"<edit:validation field=""{maskValidation.FieldName.EscapeXmlString()}"" operator=""{maskValidation.Operator.ToXmlOperatorString()}"" validator=""{maskValidation.Validator.EscapeXmlString()}"" message=""{maskValidation.Message.EscapeXmlString()}"" />");
            }
            xml.Append("</edit:validations>");
        }

        #endregion

        xml.Append(@"</edit:mask>");

        xml.Append(@"<edit:categories>");

        bool firstCategory = true;
        foreach (var categoryId in categories.Keys)
        {
            xml.Append($@"<edit:category name=""{categories[categoryId]}"" id=""{categoryId}""");

            var category = AuthObject<EditCategory>.QueryObjectArray(this.EditCategories, ui)?.Where(c => c.Id == categoryId).FirstOrDefault();
            if (category != null)
            {
                if (firstCategory == true && AuthObject<EditCategory>.QueryObjectArray(this.EditCategories, ui)?.Where(c => c.IsDefault == true).Count() != 1)
                {
                    firstCategory = false;
                    xml.Append(@" is_default=""true""");
                }
                else if (category.IsDefault == true)
                {
                    firstCategory = false;
                    xml.Append(@" is_default=""true""");
                }

                if (!String.IsNullOrEmpty(category.QuickSearchService))
                {
                    xml.Append($@" quick_search_service=""{category.QuickSearchService.EscapeXmlString()}""");
                }
                if (!String.IsNullOrEmpty(category.QuickSearchCategory))
                {
                    xml.Append($@" quick_search_category=""{category.QuickSearchCategory.EscapeXmlString()}""");
                }
                if (!String.IsNullOrEmpty(category.QuickSearchPlaceholder))
                {
                    xml.Append($@" quick_search_placeholder=""{category.QuickSearchPlaceholder.EscapeXmlString()}""");
                }
                if (category.QuickSearchSetGeometry == true)
                {
                    xml.Append(@" quick_search_setgeometry=""true""");
                }
            }
            else
            {
                if (firstCategory == true)
                {
                    firstCategory = false;
                    xml.Append(@" is_default=""true""");
                }
            }
            xml.Append("/>");
        }
        xml.Append(@"</edit:categories>");

        #region Insert Actions

        xml.Append($@"<edit:insertactions auto_explode_multipart_features=""{this.AutoExplodeMultipartFeatures.ToString().ToLower()}"">");

        foreach (var insertAction in this.InsertActions)
        {
            xml.Append($@"<edit:insertaction action=""{insertAction.Action}"" is_default=""{insertAction.IsDefault.ToString().ToLower()}"" action_text=""{insertAction.ButtonText.EscapeXmlString()}"" />");
        }

        xml.Append(@"</edit:insertactions>");

        #endregion

        xml.Append("</edit:edittheme>");
        xml.Append("</editthemes>");

        XmlDocument doc = new XmlDocument();
        doc.LoadXml(xml.ToString());
        XmlNamespaceManager ns = new XmlNamespaceManager(doc.NameTable);
        ns.AddNamespace("edit", "http://www.e-steiermark.com/webgis/edit");
        ns.AddNamespace("webgis", "http://www.e-steiermark.com/webgis");

        return doc.SelectSingleNode($"//edit:edittheme[@id='{this.ThemeId}']", ns);
    }

    #region Classes

    public class EditField
    {
        public EditField() { }
        public EditField(CmsNode cmsNode, string cmsName)
        {
            this.Prompt = cmsNode.Name;
            this.Field = cmsNode.LoadString("field");
            this.FieldType = ((EditingFieldType)(int)cmsNode.Load("type", (int)EditingFieldType.Text)).ToString().ToLower();

            this.Category = cmsNode.ParentNode.Name;
            this.CategoryId = cmsNode.ParentNode.NodeName;

            this.Visible = (bool)cmsNode.Load("visible", true);
            this.Readonly = (bool)cmsNode.Load("readonly", false);
            this.LegendField = (bool)cmsNode.Load("legendfield", false);
            this.MassAttributable = (bool)cmsNode.Load("massattributable", false);
            this.Resistant = (bool)cmsNode.Load("resistant", false);

            this.Required = (bool)cmsNode.Load("required", false);
            this.MinLength = (int)cmsNode.Load("minlen", 0);
            this.RegexPattern = cmsNode.LoadString("regex");
            this.ValidationErrorMessage = cmsNode.LoadString("validation_error");
            this.ClientSideValidation = (bool)cmsNode.Load("clientside_validation", false);

            var autoValue = (EditingFieldAutoValue)(int)cmsNode.Load("autovalue", (int)EditingFieldAutoValue.none);
            switch (autoValue)
            {
                case EditingFieldAutoValue.none:
                    this.AutoValue = String.Empty;
                    break;
                case EditingFieldAutoValue.custom:
                    this.AutoValue = cmsNode.LoadString("customautovalue");
                    break;
                default:
                    this.AutoValue = autoValue.ToString();
                    this.AutoValueCustom1 = cmsNode.LoadString("customautovalue");
                    this.AutoValueCustom2 = cmsNode.LoadString("customautovalue2");
                    break;
            }


            this.DbConnectionString = cmsNode.LoadString("db_connectionstring");
            this.DbTable = cmsNode.LoadString("db_table");
            this.DbValueField = cmsNode.LoadString("db_valuefield");
            this.DbAliasField = cmsNode.LoadString("db_aliasfield");
            this.DbWhereClause = cmsNode.LoadString("db_where");
            this.DbOrderBy = cmsNode.LoadString("db_orderby");

            this.DomainList = cmsNode.LoadString("domain_list");

            string attributePickerServiceQuery = cmsNode.LoadString("attribute_picker_query");
            if (!String.IsNullOrEmpty(attributePickerServiceQuery) &&
                attributePickerServiceQuery.Contains("@"))
            {
                this.AttributePickerServiceId = attributePickerServiceQuery.Split('@')[0];
                this.AttributePickerQueryId = attributePickerServiceQuery.Split('@')[1];
                this.AttributePickerField = cmsNode.LoadString("attribute_picker_field");

                if (!String.IsNullOrEmpty(cmsName))
                {
                    this.AttributePickerServiceId = $"{this.AttributePickerServiceId}@{cmsName}";
                }
            }
        }

        public string Prompt { get; set; }
        public string Field { get; set; }
        public string FieldType { get; set; }

        public string Category { get; set; }
        public string CategoryId { get; set; }

        public bool Visible { get; set; }
        public bool Readonly { get; set; }
        public bool LegendField { get; set; }
        public bool MassAttributable { get; set; }
        public bool Resistant { get; set; }

        public bool Required { get; set; }
        public int MinLength { get; set; }
        public string RegexPattern { get; set; }
        public string ValidationErrorMessage { get; set; }
        public bool ClientSideValidation { get; set; }

        public string AutoValue { get; set; }
        public string AutoValueCustom1 { get; set; }
        public string AutoValueCustom2 { get; set; }

        public string DbConnectionString { get; set; }
        public string DbTable { get; set; }
        public string DbValueField { get; set; }
        public string DbAliasField { get; set; }
        public string DbWhereClause { get; set; }
        public string DbOrderBy { get; set; }

        public string DomainList { get; set; }

        public string AttributePickerServiceId { get; set; }
        public string AttributePickerQueryId { get; set; }
        public string AttributePickerField { get; set; }
    }

    public class EditCategory
    {
        public EditCategory() { }
        public EditCategory(CmsNode cmsNode)
        {
            this.Id = cmsNode.NodeName;
            this.Name = cmsNode.Name;

            this.IsDefault = (bool)cmsNode.Load("is_default", false);

            this.QuickSearchService = cmsNode.LoadString("quick_search_service");
            this.QuickSearchCategory = cmsNode.LoadString("quick_search_category");
            this.QuickSearchPlaceholder = cmsNode.LoadString("quick_search_placeholder");
            this.QuickSearchSetGeometry = (bool)cmsNode.Load("quick_search_setgeometry", false);
        }

        public string Id { get; set; }
        public string Name { get; set; }

        public bool IsDefault { get; set; }

        public string QuickSearchService { get; set; }
        public string QuickSearchCategory { get; set; }
        public string QuickSearchPlaceholder { get; set; }
        public bool QuickSearchSetGeometry { get; set; }
    }

    public class SnappingScheme
    {
        [JsonProperty("id")]
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonProperty("serviceid")]
        [System.Text.Json.Serialization.JsonPropertyName("serviceid")]
        public string ServiceId { get; set; }

        [JsonProperty("types")]
        [System.Text.Json.Serialization.JsonPropertyName("types")]
        public string[] Types { get; set; }

        [JsonProperty("fixto", NullValueHandling = NullValueHandling.Ignore)]
        [System.Text.Json.Serialization.JsonPropertyName("fixto")]
        [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
        public FixToDefintion[] FixTo { get; set; }

        #region Classes

        public class FixToDefintion
        {
            public FixToDefintion() { }
            public FixToDefintion(string fromString)
            {
                int index = fromString.LastIndexOf(":");

                if (index < 0)
                {
                    this.Name = fromString;
                    this.Types = new string[] { "nodes" };
                }
                else
                {
                    this.Name = fromString.Substring(0, index);
                    if (int.TryParse(fromString.Substring(index + 1), out int flag))
                    {
                        UniqueList types = new UniqueList();
                        if ((flag & 1) == 1)
                        {
                            types.Add("nodes");
                            types.Add("endpoints");
                        }
                        if ((flag & 2) == 2)
                        {
                            types.Add("edges");
                        }
                        if ((flag & 4) == 4)
                        {
                            types.Add("endpoints");
                        }

                        this.Types = types.ToArray();
                    }
                    else
                    {
                        this.Types = new string[] { "nodes" };
                    }
                }
            }

            [JsonProperty("name")]
            [System.Text.Json.Serialization.JsonPropertyName("name")]
            public string Name { get; set; }
            [JsonProperty("types")]
            [System.Text.Json.Serialization.JsonPropertyName("types")]
            public string[] Types { get; set; }
        }

        #endregion
    }

    public class InsertAction
    {
        public string ButtonText { get; set; }
        public EditingInsertAction Action { get; set; }
        public bool IsDefault { get; set; }
    }

    public class MaskValidation
    {
        public string FieldName { get; set; }
        public MaskValidationOperators Operator { get; set; }
        public string Validator { get; set; }
        public string Message { get; set; }
    }

    #endregion

    #region IHtml Member

    public string ToHtmlString()
    {
        StringBuilder sb = new StringBuilder();

        sb.Append(HtmlHelper.ToTable(
            new string[] { "Name", "ThemeId", "LayerId" },
            new object[] { this.Name, this.ThemeId, this.LayerId }
        ));

        return sb.ToString();
    }

    #endregion
}