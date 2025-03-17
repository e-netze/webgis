using System.ComponentModel;

namespace Cms.Models;

public class ImportModel
{
    public string ErrorMesssage { get; set; }

    public object ProcDefinition { get; set; }

    public ImportType ImportType { get; set; }
}

public enum ImportType
{
    [Description("Nur neue vorhandene Knoten erstellen")]
    OnlyNew = 0,
    [Description("Vorhandene Knoten überschreiben, nicht vorhandene erstellen")]
    UpdateAll = 1
}
