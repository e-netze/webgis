using System.Reflection;
using System.Text.RegularExpressions;

using Spectre.Console;
using Spectre.Console.Rendering;

using webgis.deploy.Extensions;
using webgis.deploy.Reflection;

namespace webgis.deploy.Services;

class ConsoleService
{
    private const string NewItemChoice = "<new...>";

    private ProgressTask _task;
    private int _lastProgressTotal;
    private readonly List<string> _pendingLog = new();

    public ConsoleService(bool nonInteractive = false, bool noColor = false, bool verbose = false)
    {
        IsInteractive = !nonInteractive
                        && !Console.IsInputRedirected
                        && !Console.IsOutputRedirected;
        Verbose = verbose;

        if (!Console.IsOutputRedirected)
        {
            // otherwise symbols like ✔ are rendered as '?' on Windows consoles
            Console.OutputEncoding = System.Text.Encoding.UTF8;
        }

        if (noColor || !IsInteractive)
        {
            AnsiConsole.Profile.Capabilities.ColorSystem = ColorSystem.NoColors;
            AnsiConsole.Profile.Capabilities.Ansi = false;
        }
    }

    public bool IsInteractive { get; }

    public bool Verbose { get; }

    #region Output

    public void WriteBanner(string title, string workDirectory, bool isInternal)
    {
        var panel = new Panel(new Markup($"[bold green]WebGIS.Deploy Tool[/] [yellow]{Markup.Escape(title)}[/]"
                                         + (isInternal ? "\n[bold red]!! INTERNAL !![/]" : "")))
        {
            Border = BoxBorder.Double,
            Padding = new Padding(4, 0),
            Expand = false
        };

        AnsiConsole.Write(panel);
        AnsiConsole.MarkupLine($"[grey]Work-Directory:[/] {Markup.Escape(workDirectory)}");
        AnsiConsole.WriteLine();
    }

    public void WriteSummary(params (string Name, string Value)[] rows)
    {
        var table = new Table().Border(TableBorder.Rounded).HideHeaders();
        table.AddColumn("name");
        table.AddColumn("value");

        foreach (var (name, value) in rows)
        {
            table.AddRow($"[grey]{Markup.Escape(name)}[/]", $"[bold]{Markup.Escape(value)}[/]");
        }

        AnsiConsole.Write(table);
    }

    public void Section(string title)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule($"[bold]{Markup.Escape(title)}[/]").LeftJustified().RuleStyle("grey"));
    }

    public void Info(string message) => AnsiConsole.MarkupLine(Markup.Escape(message));

    public void Step(string message) => AnsiConsole.MarkupLine($"[blue]»[/] {Markup.Escape(message)}");

    public void Success(string message, string detail = "")
        => AnsiConsole.MarkupLine($"[green]✔[/] {Markup.Escape(message)}"
                                  + (String.IsNullOrEmpty(detail) ? "" : $" [grey]{Markup.Escape(detail)}[/]"));

    // Renders relative file paths as a tree below rootName (indented under the last step line)
    public void WriteFileTree(string rootName, IEnumerable<string> relativePaths)
    {
        var tree = new Tree($"[grey]{Markup.Escape(rootName)}[/]").Guide(TreeGuide.Line);
        var nodes = new Dictionary<string, TreeNode>(StringComparer.OrdinalIgnoreCase);

        foreach (var parts in relativePaths
            .Select(p => p.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries))
            .Order(Comparer<string[]>.Create(CompareTreePaths)))
        {
            IHasTreeNodes parent = tree;
            string key = "";

            for (int i = 0; i < parts.Length; i++)
            {
                key += "/" + parts[i];
                bool isFile = i == parts.Length - 1;

                if (!nodes.TryGetValue(key, out var node))
                {
                    node = parent.AddNode(isFile
                        ? Markup.Escape(parts[i])
                        : $"[blue]{Markup.Escape(parts[i])}[/]");
                    nodes[key] = node;
                }
                parent = node;
            }
        }

        AnsiConsole.Write(new Padder(tree, new Padding(2, 0, 0, 0)));
    }

    // folders before files on each level, then alphabetical
    private static int CompareTreePaths(string[] a, string[] b)
    {
        for (int i = 0; i < Math.Min(a.Length, b.Length); i++)
        {
            bool aIsFile = i == a.Length - 1, bIsFile = i == b.Length - 1;
            if (aIsFile != bIsFile)
            {
                return aIsFile ? 1 : -1;
            }

            int result = StringComparer.OrdinalIgnoreCase.Compare(a[i], b[i]);
            if (result != 0)
            {
                return result;
            }
        }

        return a.Length.CompareTo(b.Length);
    }

    public void Warning(string message) => AnsiConsole.MarkupLine($"[yellow]⚠ {Markup.Escape(message)}[/]");

    public void Error(string message) => AnsiConsole.MarkupLine($"[red]✘ {Markup.Escape(message)}[/]");

    // Detail messages (paths, single files, ...): only shown with --verbose
    public void Log(string message)
    {
        if (!Verbose)
        {
            return;
        }

        if (_task is not null)
        {
            _pendingLog.Add(message);
        }
        else
        {
            AnsiConsole.MarkupLine($"  [grey]{Markup.Escape(message)}[/]");
        }
    }

    // Runs code that writes to Console directly (eg. shared libraries) and routes its output to Log
    public void Quiet(Action action)
    {
        var original = Console.Out;
        using var writer = new StringWriter();

        Console.SetOut(writer);
        try
        {
            action();
        }
        finally
        {
            Console.SetOut(original);
        }

        foreach (var line in writer.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries))
        {
            Log(line);
        }
    }

    public void WriteResult(string title, bool success, params (string Name, string Value)[] rows)
    {
        var grid = new Grid().AddColumn(new GridColumn().NoWrap()).AddColumn();

        foreach (var (name, value) in rows)
        {
            grid.AddRow($"[grey]{Markup.Escape(name)}[/]", Markup.Escape(value));
        }

        IRenderable content = rows.Length > 0 ? grid : new Markup(Markup.Escape(title));

        var panel = new Panel(content)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(success ? Color.Green : Color.Red),
            Padding = new Padding(2, 1),
            Header = new PanelHeader(rows.Length > 0
                ? $"[bold {(success ? "green" : "red")}] {(success ? "✔" : "✘")} {Markup.Escape(title)} [/]"
                : $"[bold {(success ? "green" : "red")}] {(success ? "✔" : "✘")} [/]")
        };

        AnsiConsole.WriteLine();
        AnsiConsole.Write(panel);
    }

    public void WriteBlock(string message, bool success = true) => WriteResult(message, success);

    public void WaitForEnter()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Markup("[grey]Press [bold]ENTER[/] to quit...[/]");
        Console.ReadLine();
    }

    #endregion

    #region Progress

    public void Report(string message, int current, int total)
    {
        _lastProgressTotal = total;

        if (_task is null)
        {
            return;
        }

        _task.MaxValue = Math.Max(total, 1);
        _task.Value = Math.Min(current, _task.MaxValue);
        _task.Description = Truncate(message);
    }

    public void RunProgress(string description, Action work, string unit = "files")
        => RunProgressAsync(description, () => { work(); return Task.CompletedTask; }, unit).GetAwaiter().GetResult();

    public async Task RunProgressAsync(string description, Func<Task> work, string unit = "files")
    {
        _lastProgressTotal = 0;

        if (!IsInteractive)
        {
            await work();
            Success(description, ProgressDetail(unit));
            FlushLog();
            return;
        }

        await AnsiConsole.Progress()
            .AutoClear(true)
            .HideCompleted(false)
            .Columns(
                new TaskDescriptionColumn { Alignment = Justify.Left },
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn())
            .StartAsync(async ctx =>
            {
                _task = ctx.AddTask(Truncate(description));
                try
                {
                    await work();
                    _task.Value = _task.MaxValue;
                }
                finally
                {
                    _task = null;
                }
            });

        Success(description, ProgressDetail(unit));
        FlushLog();
    }

    private string ProgressDetail(string unit)
        => _lastProgressTotal > 0 && !String.IsNullOrEmpty(unit)
            ? $"({_lastProgressTotal:N0} {(_lastProgressTotal == 1 ? unit.TrimEnd('s') : unit)})"
            : "";

    private void FlushLog()
    {
        foreach (var message in _pendingLog)
        {
            AnsiConsole.MarkupLine($"  [grey]{Markup.Escape(message)}[/]");
        }

        _pendingLog.Clear();
    }

    private static string Truncate(string message)
    {
        int max = Math.Max(20, AnsiConsole.Profile.Width / 2 - 4);

        return Markup.Escape(message.Length <= max ? message : "…" + message[^(max - 1)..]);
    }

    #endregion

    #region Prompts

    public string ChooseFrom(IEnumerable<string> items, string label, bool allowNewVales = false, string examples = "")
    {
        var itemArray = items.ToArray();

        if (!IsInteractive)
        {
            throw new Exception($"No {label} specified (non-interactive mode)");
        }

        var newItemPrompt = $"Enter a name for the new {label}" + (string.IsNullOrEmpty(examples) ? "" : $" (eg. {examples})");

        if (itemArray.Length == 0 && allowNewVales)
        {
            return AskText(newItemPrompt);
        }

        var choices = allowNewVales ? itemArray.Append(NewItemChoice) : itemArray;

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"Choose a [green]{Markup.Escape(label)}[/]:")
                .PageSize(12)
                .MoreChoicesText("[grey](Move up and down to reveal more)[/]")
                .AddChoices(choices));

        return selected == NewItemChoice ? AskText(newItemPrompt) : selected;
    }

    public IReadOnlyList<string> ChooseMultiple(IEnumerable<string> items, string label, IEnumerable<string> preselected)
    {
        if (!IsInteractive)
        {
            return preselected.ToArray();
        }

        var prompt = new MultiSelectionPrompt<string>()
            .Title($"Select the {Markup.Escape(label)} to deploy:")
            .NotRequired()
            .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to accept)[/]")
            .AddChoices(items);

        foreach (var item in preselected)
        {
            prompt.Select(item);
        }

        return AnsiConsole.Prompt(prompt);
    }

    public bool DoYouWantToContinue() => DoYouWant("to continue");

    public bool DoYouWant(string prompt, bool defaultValue = true)
    {
        if (!IsInteractive)
        {
            return defaultValue;
        }

        return AnsiConsole.Confirm($"Do you want {Markup.Escape(prompt)}?", defaultValue);
    }

    public bool InputRequiredModelProperties(object model)
    {
        bool hasChanged = false;
        var modelType = model.GetType();

        foreach (var property in modelType.GetProperties())
        {
            var modelPropertyAttr = property.GetCustomAttribute<ModelPropertyAttribute>();
            if (modelPropertyAttr == null)
            {
                continue;
            }

            var val = property.GetValue(model, null)?.ToString();

            if (!String.IsNullOrEmpty(val))
            {
                AnsiConsole.MarkupLine($"[grey]{Markup.Escape(modelPropertyAttr.Prompt ?? property.Name)}:[/] {Markup.Escape(val)}");
                continue;
            }

            var defaultValue = modelPropertyAttr.GetDefaultValue(model);

            if (!IsInteractive)
            {
                if (String.IsNullOrEmpty(defaultValue))
                {
                    throw new Exception($"Missing required value '{modelPropertyAttr.Prompt}' (non-interactive mode)");
                }

                property.SetValue(model, defaultValue, null);
                hasChanged = true;
                continue;
            }

            var textPrompt = new TextPrompt<string>(Markup.Escape(modelPropertyAttr.Prompt ?? property.Name))
                .AllowEmpty();

            if (!String.IsNullOrEmpty(defaultValue))
            {
                textPrompt.DefaultValue(defaultValue).ShowDefaultValue(true);
            }

            if (!String.IsNullOrEmpty(modelPropertyAttr.RegexPattern))
            {
                textPrompt.Validate(v => Regex.IsMatch(v, modelPropertyAttr.RegexPattern)
                    ? ValidationResult.Success()
                    : ValidationResult.Error($"[red]Value don't match pattern: {Markup.Escape(modelPropertyAttr.RegexNotMatchMessage)}[/]"));
            }
            else
            {
                textPrompt.Validate(v => !String.IsNullOrWhiteSpace(v)
                    ? ValidationResult.Success()
                    : ValidationResult.Error("[red]Value required[/]"));
            }

            val = AnsiConsole.Prompt(textPrompt).Trim();

            property.SetValue(model, val, null);
            hasChanged = true;
        }

        return hasChanged;
    }

    private static string AskText(string prompt)
        => AnsiConsole.Prompt(
            new TextPrompt<string>(Markup.Escape(prompt))
                .Validate(v => !String.IsNullOrWhiteSpace(v)
                    ? ValidationResult.Success()
                    : ValidationResult.Error("[red]Value required[/]"))).Trim();

    #endregion

    public void WriteHelp()
    {
        var table = new Table().Border(TableBorder.None).HideHeaders();
        table.AddColumn("option");
        table.AddColumn("description");

        table.AddRow("-h, --help", "Show this help message and exit");
        table.AddRow("-p, --profile", "Choose a profile (runs without further questions)");
        table.AddRow("-d, --download-latest", "Download latest version");
        table.AddRow("-v, --version", "Choose a version, --version latest ... latest version");
        table.AddRow("--cms, --deploy-cms", "Deploy WebGIS CMS");
        table.AddRow("--portal, --deploy-portal", "Deploy WebGIS Portal");
        table.AddRow("--api, --deploy-api", "Deploy WebGIS API");
        table.AddRow("--portable", "Only list/use portable (self-contained) ZIPs, skip GitHub download");
        table.AddRow("--non-interactive", "Never prompt (also automatic if input/output is redirected)");
        table.AddRow("--no-color", "Disable colors and ANSI sequences");
        table.AddRow("--verbose", "Show detailed output (files, paths, ...)");

        AnsiConsole.MarkupLine("[bold]usage:[/] webgis.deploy [[options]]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Options:[/]");
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Examples:[/]");
        AnsiConsole.WriteLine("  webgis.deploy -p production -v latest --download-latest --deploy-cms --deploy-portal --deploy-api");
    }
}
