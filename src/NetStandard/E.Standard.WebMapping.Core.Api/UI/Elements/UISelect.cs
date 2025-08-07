using E.Standard.WebMapping.Core.Api.Bridge;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace E.Standard.WebMapping.Core.Api.UI.Elements;

public class UISelect : UIValidation
{
    #region Constructors

    public UISelect()
        : this(UIButton.UIButtonType.clientbutton, ApiClientButtonCommand.unknown)
    {
    }

    public UISelect(UIButton.UIButtonType changeType = UIButton.UIButtonType.clientbutton, string changeCommand = "")
        : base("select")
    {
        this.changetype = changeType.ToString();
        this.changecommand = changeCommand.ToString();
    }

    public UISelect(UIButton.UIButtonType changeType = UIButton.UIButtonType.clientbutton, ApiClientButtonCommand changeCommand = ApiClientButtonCommand.unknown)
        : this(changeType, changeCommand.ToString())
    {
        this.changetype = changeType.ToString();
        this.changecommand = changeCommand.ToString();
    }

    public UISelect(string[] options)
        : this()
    {
        if (options == null || options.Length == 0)
        {
            return;
        }

        List<Option> list = new List<Option>();
        foreach (string option in options)
        {
            list.Add(new Option()
            {
                value = option,
                label = option
            });
        }

        this.options = list.ToArray();
    }

    #endregion

    public string changetype { get; set; }
    public string changetool { get; set; }
    public string changecommand { get; set; }

    public string defaultvalue { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string[] dependency_field_ids { get; set; }
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public string dependency_field_ids_callback_toolid { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? allow_addvalues { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public bool? allow_pro_behaviour { get; set; }

    #region Options

    public ICollection<Option> options { get; set; }
    public class Option
    {
        public string value { get; set; }
        public string label { get; set; }
    }

    #endregion

    public static UISelect PrintFormats(string id, IEnumerable<IPrintFormatBridge> formats, UIButton.UIButtonType changeType = UIButton.UIButtonType.clientbutton, string changeCommand = "", string defaultValue = "")
    {
        var select = new UISelect(changeType, changeCommand)
        {
            id = id,
            css = UICss.ToClass(new string[] { UICss.PrintToolFormat, UICss.ToolParameter, UICss.ToolParameterPersistent }),
        };

        if (formats != null)
        {
            List<Option> options = new List<Option>();
            foreach (var format in formats)
            {
                options.Add(UISelect.ToPrintFormatsOption(format));
            }
            select.options = options.ToArray();
        }

        if (!String.IsNullOrEmpty(defaultValue))
        {
            select.defaultvalue = defaultValue;
        }

        return select;
    }

    public static UISelect PrintQuality(string id,
                                        Dictionary<int, string> qualities,
                                        UIButton.UIButtonType changeType = UIButton.UIButtonType.clientbutton,
                                        string changeCommand = "")
    {
        if (qualities == null || qualities.Keys.Count() == 0)
        {
            qualities = new Dictionary<int, string>()
            {
                { 120, "Mittel (120 dpi)" },
                { 150, "Hoch (150 dpi)" }
            };
        }

        var select = new UISelect(changeType, changeCommand)
        {
            id = id,
            css = UICss.ToClass(new string[] { UICss.PrintToolQuality, UICss.ToolParameter, UICss.ToolParameterPersistent }),
            defaultvalue = qualities.Keys.First().ToString(),
            options = qualities.Keys.OrderBy(dpi => dpi)
                                    .Select(dpi => new Option()
                                    {
                                        value = dpi.ToString(),
                                        label = qualities[dpi]
                                    })
                                    .ToArray()
        };

        return select;
    }

    public static UISelect Scales(string id, UIButton.UIButtonType changeType = UIButton.UIButtonType.clientbutton, string changeCommand = "", bool allowAddValues = false, IEnumerable<int> scales = null)
    {
        var select = new UISelect(changeType, changeCommand)
        {
            id = id,
            css = UICss.ToClass(new string[] { UICss.MapScalesSelect, UICss.ToolParameter })
        };

        if (allowAddValues)
        {
            select.allow_addvalues = true;
        }

        select.options = scales?
            .Select(s => new Option()
            {
                value = s.ToString(),
                label = string.Format("1:{0:0,0.}", s).Replace(" ", ".")
            }
            )
            .ToList();

        return select;
    }

    public static Option ToPrintFormatsOption(IPrintFormatBridge format)
    {
        if (format.Size.ToString().Contains("_"))
        {
            return new Option()
            {
                value = format.Size.ToString() + "." + format.Orientation,
                label = format.Size.ToString().Replace("_", " x ")
            };
        }
        else
        {
            return new Option()
            {
                value = format.Size + "." + format.Orientation,
                label = format.Size.ToString() + " " + (format.Orientation == PageOrientation.Portrait ? "Hochformat" : "Querformat")
            };
        }
    }
}
