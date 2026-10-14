using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using E.Standard.WebGIS.Core.Reflection;
using E.Standard.WebMapping.Core.Api;
using E.Standard.WebMapping.Core.Api.Abstraction;
using E.Standard.WebMapping.Core.Api.Bridge;
using E.Standard.WebMapping.Core.Api.EventResponse;
using E.Standard.WebMapping.Core.Api.Extensions;
using E.Standard.WebMapping.Core.Api.Reflection;
using E.Standard.WebMapping.Core.Api.UI.Abstractions;
using E.Standard.WebMapping.Core.Api.UI.Elements;

namespace E.Standard.WebGIS.Tools.Debug.UI;

/// <summary>
/// Debug tool: tables
/// (table/tr/th/td, cell elements and values, table as (persistent) tool parameter,
/// insert_type append/replace, parameterservercommands).
/// </summary>
#if DEBUG
[Export(typeof(IApiButton))]
#endif
public class DebugUITables : DebugUIToolBase
{
    private const string IdPrefix = "debug-ui-tables-";
    private const string DynamicTableId = IdPrefix + "dynamic";
    private const string PersistentTableId = IdPrefix + "persistent";
    private const string RestrictedTableId = IdPrefix + "restricted";
    private const string UnknownTableId = IdPrefix + "unknown";

    private const string AppendRowCommand = "append-row";
    private const string ReplaceRowsCommand = "replace-rows";
    private const string ClearRowsCommand = "clear-rows";
    private const string AppendUnknownCommand = "append-unknown";
    private const string AppendPersistentRowCommand = "append-persistent-row";
    private const string ClearPersistentRowsCommand = "clear-persistent-rows";
    private const string EchoWithoutTableCommand = "echo-without-table";

    private const int ColumnCount = 3;  // #, text, time

    public override string Name => "UI: Tables";

    public override string ToolTip => "Debug: tables, table values, append/replace and persistent tables";

    public override IEnumerable<Type> DemonstratedTypes => [typeof(UITable), typeof(UITableRow)];

    public override Task<ApiEventResponse> OnButtonClick(IBridge bridge, ApiToolEventArguments e)
    {
        var response = new ApiEventResponse()
            .AddUIElements(
                SketchSection(),

                Section("table: cells",
                    Hint("Header row (th) and rows with different cell elements. The table has the alternate-row-color style. UITableRow moves the css of a cell element to its td."),
                    CellsTable()),

                Section("table: value (tool parameter)",
                    Hint("A table with the 'webgis-tool-parameter' class: the value is the data-value of all td cells joined with ';' (th cells are ignored). Row 3 has no values => empty entries."),
                    new UITable([
                            HeaderRow(),
                            DataRow(1, "a", "x"),
                            DataRow(2, "b", "y"),
                            new UITableRow([Literal("3"), Literal("(no values)"), Literal("-")])
                        ])
                        .WithId(IdPrefix + "value")
                        .WithStyles(UICss.ToolParameter, UICss.TableAlternateRowColor),
                    EchoButton()),

                Section("table: append / replace (insert_type)",
                    Hint("Commands return a table with the same id and insert_type 'append' (adds rows) or 'replace' (removes all rows incl. header, then inserts). The response uses AppendUIElementsMode.Append, so the tool UI is not rebuilt."),
                    DynamicTable(DynamicTableId, 0, UICss.ToolParameter, UICss.TableAlternateRowColor),
                    new UIButtonContainer([
                        EchoButton("Append row", command: AppendRowCommand),
                        EchoButton("Replace (3 rows)", command: ReplaceRowsCommand),
                        EchoButton("Clear", command: ClearRowsCommand).AsCancelButton(),
                    ]),
                    Hint("insert_type 'append' with an id that does not exist yet => a new table is appended at the end of the tool UI."),
                    EchoButton("Append to unknown table id", command: AppendUnknownCommand)),

                Section("table: persistent",
                    Hint("Persistent tool parameter: append rows, close the tool (select another tool) and open it again => the rows are restored from the persistent value."),
                    DynamicTable(PersistentTableId, 0, UICss.ToolParameter, UICss.ToolParameterPersistent)
                        .WithRows(ParseRows(e[PersistentTableId])),
                    new UIButtonContainer([
                        EchoButton("Append row", command: AppendPersistentRowCommand),
                        EchoButton("Clear", command: ClearPersistentRowsCommand).AsCancelButton(),
                    ])),

                Section("table: parameterservercommands",
                    Hint($"The table value is only sent for the listed server commands ('{EchoCommand}'). Check the echo dialogs: '{RestrictedTableId}' is only listed for the first button."),
                    new UITable([HeaderRow(), DataRow(1, "only", "echo")])
                        .WithId(RestrictedTableId)
                        .WithStyles(UICss.ToolParameter)
                        .WithParameterForServerCommands(EchoCommand),
                    new UIButtonContainer([
                        EchoButton($"Echo ({EchoCommand})"),
                        EchoButton($"Echo ({EchoWithoutTableCommand})", command: EchoWithoutTableCommand)
                    ])));

        return Task.FromResult(response);
    }

    #region Commands

    [ServerToolCommand(AppendRowCommand)]
    public ApiEventResponse OnAppendRow(IBridge bridge, ApiToolEventArguments e)
        => TableResponse(new UITable()
        {
            InsertTypeValue = UITable.TableInsertType.Append
        }
        .WithId(DynamicTableId)
        .WithRows([NewRow(ParseRows(e[DynamicTableId]).Count() + 1)]));

    [ServerToolCommand(ReplaceRowsCommand)]
    public ApiEventResponse OnReplaceRows(IBridge bridge, ApiToolEventArguments e)
        => TableResponse(new UITable(HeaderRow())
        {
            InsertTypeValue = UITable.TableInsertType.Replace
        }
        .WithId(DynamicTableId)
        .WithRows([NewRow(1), NewRow(2), NewRow(3)]));

    [ServerToolCommand(ClearRowsCommand)]
    public ApiEventResponse OnClearRows(IBridge bridge, ApiToolEventArguments e)
        => TableResponse(new UITable(HeaderRow())
        {
            InsertTypeValue = UITable.TableInsertType.Replace
        }
        .WithId(DynamicTableId));

    [ServerToolCommand(AppendUnknownCommand)]
    public ApiEventResponse OnAppendUnknown(IBridge bridge, ApiToolEventArguments e)
        => TableResponse(new UITable(NewRow(1))
        {
            InsertTypeValue = UITable.TableInsertType.Append
        }
        .WithId(UnknownTableId)
        .WithStyles(UICss.TableAlternateRowColor));

    [ServerToolCommand(AppendPersistentRowCommand)]
    public ApiEventResponse OnAppendPersistentRow(IBridge bridge, ApiToolEventArguments e)
        => TableResponse(new UITable()
        {
            InsertTypeValue = UITable.TableInsertType.Append
        }
        .WithId(PersistentTableId)
        .WithRows([NewRow(ParseRows(e[PersistentTableId]).Count() + 1)]));

    [ServerToolCommand(ClearPersistentRowsCommand)]
    public ApiEventResponse OnClearPersistentRows(IBridge bridge, ApiToolEventArguments e)
        => TableResponse(new UITable(HeaderRow())
        {
            InsertTypeValue = UITable.TableInsertType.Replace
        }
        .WithId(PersistentTableId));

    [ServerToolCommand(EchoWithoutTableCommand)]
    public ApiEventResponse OnEchoWithoutTable(IBridge bridge, ApiToolEventArguments e)
        => EchoResponse(e);

    #endregion

    #region Helpers

    static private ApiEventResponse TableResponse(UITable table)
        => new ApiEventResponse()
            .UIElementsBehavoir(AppendUIElementsMode.Append)
            .AddUIElement(table);

    static private UILiteral Literal(string text) => new UILiteral() { literal = text };

    static private UITableRow HeaderRow()
        => new UITableRow([Literal("#"), Literal("text"), Literal("value")], isHeader: true);

    static private UITableRow DataRow(int number, string text, string value)
        => new UITableRow(
            [Literal(number.ToString()), Literal(text), Literal(value)],
            values: [number.ToString(), text, value]);

    static private UITableRow NewRow(int number)
        => DataRow(number, $"row {number}", DateTime.Now.ToString("HH:mm:ss"));

    static private UITable DynamicTable(string id, int rowCount, params string[] styles)
        => new UITable(HeaderRow())
            .WithId(id)
            .WithStyles(styles)
            .WithRows(Enumerable.Range(1, rowCount).Select(NewRow));

    /// <summary>
    /// Parses a table value (td data-values joined with ';') into rows.
    /// </summary>
    static private IEnumerable<UITableRow> ParseRows(string tableValue)
    {
        if (String.IsNullOrEmpty(tableValue))
        {
            return [];
        }

        var values = tableValue.Split(';');

        return Enumerable.Range(0, values.Length / ColumnCount)
            .Select(r => values.Skip(r * ColumnCount).Take(ColumnCount).ToArray())
            .Select(v => new UITableRow([Literal(v[0]), Literal(v[1]), Literal(v[2])], values: v))
            .ToArray();
    }

    private UITable CellsTable()
    {
        var table = new UITable(new UITableRow(
            [Literal("element"), Literal("cell")],
            isHeader: true));

        table.AddRow(new UITableRow([Literal("literal"), Literal("plain text")]));
        table.AddRow(new UITableRow([Literal("bold"), new UILiteralBold() { literal = "bold text" }]));
        table.AddRow(new UITableRow([
            Literal("css on cell"),
            new UILiteral() { literal = "td gets the element css (inspect: class='debug-ui-cell-css')", css = "debug-ui-cell-css" }
        ]));
        table.AddRow(new UITableRow([
            Literal("button"),
            new UIButton(UIButton.UIButtonType.servertoolcommand, EchoCommand)
            {
                text = "echo",
                buttoncommand_argument = "cell-button",
                css = UICss.ToClass([UICss.DefaultButtonStyle])
            }
        ]));
        table.AddRow(new UITableRow([
            Literal("input (tool parameter)"),
            new UIInputText() { id = IdPrefix + "cell-input", value = "input in cell", css = UICss.ToolParameter }
        ]));

        var row = new UITableRow([Literal("AddCell")]);
        row.AddCell(new UILiteral() { literal = "cell added with AddCell()" });
        table.AddRow(row);

        return table.WithStyles(UICss.TableAlternateRowColor);
    }

    #endregion
}

static file class DebugUITableExtensions
{
    static public UITable WithRows(this UITable table, IEnumerable<UITableRow> rows)
    {
        foreach (var row in rows)
        {
            table.AddRow(row);
        }
        return table;
    }

    static public UIButton AsCancelButton(this UIButton button)
    {
        button.css = UICss.ToClass([UICss.CancelButtonStyle]);
        return button;
    }
}
