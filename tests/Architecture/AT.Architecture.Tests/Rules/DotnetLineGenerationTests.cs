using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using AppTemplate.Architecture.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace AppTemplate.Architecture.Tests.Rules;

public sealed class DotnetLineGenerationTests
{
    private const string _newestLine = "net11.0";

    private const string _newestLineCondition = "(dotnet == \"net11.0\")";

    private const string _runtimePackage = "Microsoft.EntityFrameworkCore";

    private static readonly TimeSpan _budget = TimeSpan.FromSeconds(5);

    private static readonly string[] _outsideTheProject =
        [".git", ".nuget", "launcher", "artifacts", "bin", "obj", "node_modules", "TestResults", ".sonarqube", ".vs", ".claude", ".idea", "template"];

    private static readonly string[] _lineConditions = ["(dotnet11)", _newestLineCondition];

    private const string _thisRepositoryOnly = "(templateMaintenance)";

    private static readonly Regex _marker = new(@"^\s*(?:<!--|//|/\*)?#(?<keyword>if|elseif|else|endif)\b\s*(?<condition>\([^)]*\))?", RegexOptions.None, _budget);

    private static readonly Dictionary<string, string> _keptOnPurpose = new(StringComparer.Ordinal)
    {
        ["docs/guides/upgrading.md"] = "names net10.0 as an example of an LTS",
        ["docs/reference/project-layout.md"] = "names net10.0 as what a project file without targetFramework reads as",
        ["tests/Architecture/AT.Architecture.Tests/Rules/CoverageSettingsTests.cs"] = "builds its sample paths on net10.0",
        ["tests/Architecture/AT.Architecture.Tests/Rules/WindowsTestProjectTests.cs"] = "multi-targets a sample on net10.0",
    };

    private static readonly Regex _packageVersion = new(@"<PackageVersion\s+Include=""(?<name>[^""]+)""\s+Version=""(?<version>[^""]+)""", RegexOptions.None, _budget);

    private static string Root => ProjectReferenceGraph.RepositoryRoot;

    private static string LtsLine => XDocument.Load(Path.Combine(Root, "Directory.Build.props")).Descendants("TargetFramework").Single().Value;

    [Fact]
    public void ANewestLineGeneration_KeepsNoValueOfTheLtsLine()
    {
        string lts = LtsLine;
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, ".template.config", "template.json")));
        List<LineReplacement> replacements = NewestLineReplacements(manifest.RootElement.GetProperty("symbols"));
        List<Regex> excluded = [.. ExcludedOnTheNewestLine(manifest.RootElement).Select(Glob)];
        List<string> values = LtsValues(lts);

        replacements.Count.ShouldBeGreaterThanOrEqualTo(4, "template.json declares far fewer replacements on the newest line than it has, so this rule reads the wrong symbols.");
        values.Count.ShouldBeGreaterThan(5, "The LTS line's band reads far fewer values than it holds, so this rule would hold by reading nothing.");

        var leftovers = GeneratedFiles(manifest.RootElement)
            .Select(file => (Path: RepositoryTree.Relative(file), File: file))
            .Where(file => file.Path.StartsWith(".template.config/", StringComparison.Ordinal) || !excluded.Any(glob => glob.IsMatch(file.Path)))
            .SelectMany(file => Kept(LineReplacement.Apply(OnTheNewestLine(File.ReadAllLines(file.File)), replacements), values, lts, file.Path)
                .Select(value => $"{file.Path}: {value}"))
            .ToList();

        leftovers.ShouldBeEmpty(
            $"A project generated with --dotnet {_newestLine} keeps these values of {lts}: replace each through a symbol of template.json conditioned on "
            + $"{_newestLineCondition}, move it to the line's Directory.Packages.<tfm>.props, or name the file in _keptOnPurpose with why.");
    }

    [Fact]
    public void EachLineBeyondTheLts_HasAProbeThatRestoresItsBand_AndDependabotReadsIt()
    {
        string[] lines = [.. Directory.EnumerateFiles(Root, "Directory.Packages.net*.props")
            .Select(file => Path.GetFileName(file)["Directory.Packages.".Length..^".props".Length])
            .Where(line => line != LtsLine)];
        string dependabot = File.ReadAllText(Path.Combine(Root, ".github", "dependabot.yml"));

        lines.ShouldNotBeEmpty("No line beside the LTS has a band, so this rule reads nothing.");
        foreach (string line in lines)
        {
            string probe = Path.Combine(Root, ".template.config", "lines", line);
            XDocument project = XDocument.Load(Path.Combine(probe, "DotnetLine.csproj"));

            project.Descendants("TargetFramework").Single().Value.ShouldBe(line);
            project.Descendants("PackageReference").Select(reference => reference.Attribute("Include")!.Value).Order(StringComparer.Ordinal).ShouldBe(
                Band(line).Keys.Concat(Versions(Path.Combine(probe, "Directory.Packages.props")).Keys).Order(StringComparer.Ordinal),
                $"Dependabot updates only the packages a build restores, so .template.config/lines/{line}/DotnetLine.csproj references every package of Directory.Packages.{line}.props and of its own Directory.Packages.props, and no other.");
            dependabot.ShouldContain($"- /.template.config/lines/{line}\n", Case.Sensitive, $".github/dependabot.yml does not read the probe of {line}, so its band never moves.");
        }
    }

    [Fact]
    public void TheNewestLinesToolManifest_PinsTheToolsTheGeneratedProjectsDoes()
    {
        var options = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip };
        JsonObject Tools(string manifest) => JsonNode.Parse(File.ReadAllText(manifest), documentOptions: options)!["tools"]!.AsObject();
        static string Commands(JsonNode? tool) => string.Join(" ", tool!["commands"]!.AsArray().Select(command => command!.GetValue<string>()));

        JsonObject lts = Tools(Path.Combine(Root, ".template.config", "variants", "project", ".config", "dotnet-tools.json"));
        JsonObject newest = Tools(Path.Combine(Root, ".template.config", "lines", _newestLine, ".config", "dotnet-tools.json"));

        newest.Select(tool => $"{tool.Key}: {Commands(tool.Value)}").Order(StringComparer.Ordinal).ShouldBe(
            lts.Select(tool => $"{tool.Key}: {Commands(tool.Value)}").Order(StringComparer.Ordinal),
            $".template.config/lines/{_newestLine}/.config/dotnet-tools.json, the manifest a project on {_newestLine} receives, pins other tools or commands than "
            + ".template.config/variants/project/.config/dotnet-tools.json: a tool joins or leaves both, each at its own line's version.");
    }

    [Theory]
    [InlineData("presentation")]
    [InlineData("webapp")]
    public void AnItemTemplate_TakesTheLineAsTheProjectTemplateDoes(string item)
    {
        JsonObject project = Symbols(Path.Combine(Root, ".template.config", "template.json"));
        JsonObject added = Symbols(Path.Combine(Root, ".template.config", item, ".template.config", "template.json"));
        string[] line = [.. project.Where(symbol => IsOfTheLine(symbol.Key, symbol.Value)).Select(symbol => symbol.Key)];

        line.Length.ShouldBeGreaterThanOrEqualTo(5, "template.json declares far fewer line symbols than it has, so this rule reads the wrong ones.");
        line.Where(name => !JsonNode.DeepEquals(Behaviour(project[name]), Behaviour(added[name]))).ShouldBeEmpty(
            $"The {item} item template writes a starter's files into a project of either line, so it takes --dotnet and replaces as template.json does: "
            + "copy these symbols from template.json, descriptions aside.");
    }

    [Fact]
    public void TheReplacement_FollowsOnlyIf()
    {
        var replacement = new LineReplacement("net10.0", "net11.0", [("<TargetFramework>", null), (null, "-windows")]);

        LineReplacement.Apply("<TargetFramework>net10.0</TargetFramework> net10.0-windows `net10.0`", [replacement])
            .ShouldBe("<TargetFramework>net11.0</TargetFramework> net11.0-windows `net10.0`");
    }

    private static bool IsOfTheLine(string name, JsonNode? symbol) =>
        name == "dotnet"
        || symbol?["value"]?.GetValue<string>() == _newestLineCondition
        || symbol?["parameters"]?["cases"]?.AsArray().Any(@case => @case?["condition"]?.GetValue<string>() == _newestLineCondition) == true;

    private static JsonObject Symbols(string manifest) =>
        JsonNode.Parse(File.ReadAllText(manifest))!["symbols"]!.AsObject();

    private static JsonObject? Behaviour(JsonNode? symbol)
    {
        if (symbol?.DeepClone() is not JsonObject behaviour)
        {
            return null;
        }

        behaviour.Remove("displayName");
        behaviour.Remove("description");
        foreach (JsonObject choice in behaviour["choices"]?.AsArray().OfType<JsonObject>() ?? [])
        {
            choice.Remove("description");
        }

        return behaviour;
    }

    private static IEnumerable<string> Kept(string text, List<string> values, string lts, string path) =>
        values.Where(value => text.Contains(value, StringComparison.Ordinal))
            .Where(value => !(value == lts && _keptOnPurpose.ContainsKey(path)));

    private static List<string> LtsValues(string lts)
    {
        Dictionary<string, string> band = Band(lts);
        Dictionary<string, string> newest = Band(_newestLine);
        string major = lts[3..lts.IndexOf('.', StringComparison.Ordinal)];

        return
        [
            lts,
            $"{major}.0.100",
            $"ProjectTemplates.{major}.0",
            $".NET {major} ",
            $"\"version\": \"{band[_runtimePackage]}\"",
            .. band.Where(package => newest.GetValueOrDefault(package.Key) != package.Value).Select(package => $"Include=\"{package.Key}\" Version=\"{package.Value}\""),
        ];
    }

    private static Dictionary<string, string> Band(string framework) => Versions(Path.Combine(Root, $"Directory.Packages.{framework}.props"));

    private static Dictionary<string, string> Versions(string props) =>
        _packageVersion.Matches(File.ReadAllText(props))
            .ToDictionary(match => match.Groups["name"].Value, match => match.Groups["version"].Value, StringComparer.Ordinal);

    private static List<LineReplacement> NewestLineReplacements(JsonElement symbols) =>
        [.. symbols.EnumerateObject()
            .Where(symbol => symbol.Value.TryGetProperty("replaces", out _))
            .Select(symbol => (Symbol: symbol.Value, Value: NewestLineValue(symbol)))
            .Where(symbol => symbol.Value is not null)
            .Select(symbol => new LineReplacement(symbol.Symbol.GetProperty("replaces").GetString()!, symbol.Value!, OnlyIf(symbol.Symbol)))];

    private static string? NewestLineValue(JsonProperty symbol) =>
        symbol.Name == "dotnet"
            ? _newestLine
            : symbol.Value.TryGetProperty("parameters", out var parameters) && parameters.TryGetProperty("cases", out var cases)
                ? cases.EnumerateArray().FirstOrDefault(@case => @case.GetProperty("condition").GetString() == _newestLineCondition) is { ValueKind: JsonValueKind.Object } chosen
                    ? chosen.GetProperty("value").GetString()
                    : null
                : null;

    private static List<(string? After, string? Before)> OnlyIf(JsonElement symbol) =>
        symbol.TryGetProperty("onlyIf", out var onlyIf)
            ? [.. onlyIf.EnumerateArray().Select(context => (Text(context, "after"), Text(context, "before")))]
            : [];

    private static string? Text(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) ? value.GetString() : null;

    private static IEnumerable<string> ExcludedOnTheNewestLine(JsonElement manifest) =>
        manifest.GetProperty("sources")[0].GetProperty("modifiers").EnumerateArray()
            .Where(modifier => Text(modifier, "condition") is not { } condition || condition is _newestLineCondition or "(!templateMaintenance)")
            .Where(modifier => modifier.TryGetProperty("exclude", out _))
            .SelectMany(modifier => modifier.GetProperty("exclude").EnumerateArray().Select(pattern => pattern.GetString()!));

    private static Regex Glob(string pattern) =>
        new("^" + Regex.Escape(pattern).Replace(@"\*\*", ".*", StringComparison.Ordinal).Replace(@"\*", "[^/]*", StringComparison.Ordinal) + "$", RegexOptions.None, _budget);

    private static IEnumerable<string> GeneratedFiles(JsonElement manifest) =>
        RepositoryTree.SourceFiles(Root, "*", _outsideTheProject)
            .Where(file => !RepositoryTree.Relative(file).StartsWith(".template.config/", StringComparison.Ordinal))
            .Concat(manifest.GetProperty("sources").EnumerateArray().Skip(1).Where(IsWrittenOnTheNewestLine).SelectMany(SourceFiles))
            .Where(file => !File.ReadAllText(file).Contains('\0', StringComparison.Ordinal));

    private static bool IsWrittenOnTheNewestLine(JsonElement source) =>
        Text(source, "condition") is not { } condition || !condition.Contains("dotnet != \"net11.0\"", StringComparison.Ordinal);

    private static IEnumerable<string> SourceFiles(JsonElement source)
    {
        string folder = Path.GetFullPath(Path.Combine(Root, Text(source, "source")!));
        Regex[] included = source.TryGetProperty("include", out var include) ? [.. include.EnumerateArray().Select(pattern => Glob(pattern.GetString()!))] : [];
        Regex[] excluded = source.TryGetProperty("modifiers", out var modifiers)
            ? [.. modifiers.EnumerateArray()
                .Where(modifier => Text(modifier, "condition")?.Contains(_newestLineCondition[1..^1], StringComparison.Ordinal) == true && modifier.TryGetProperty("exclude", out _))
                .SelectMany(modifier => modifier.GetProperty("exclude").EnumerateArray().Select(pattern => Glob(pattern.GetString()!)))]
            : [];

        return !Directory.Exists(folder)
            ? []
            : RepositoryTree.SourceFiles(folder, "*", _outsideTheProject)
                .Where(file => RepositoryTree.Relative(folder, file) is var relative
                    && (included.Length == 0 || included.Any(glob => glob.IsMatch(relative)))
                    && !excluded.Any(glob => glob.IsMatch(relative)));
    }

    private static string OnTheNewestLine(string[] lines)
    {
        var branches = new Stack<bool?>();
        var kept = new List<string>();

        foreach (string line in lines)
        {
            if (_marker.Match(line) is { Success: true } marker)
            {
                Branch(branches, marker.Groups["keyword"].Value, marker.Groups["condition"].Value);
            }
            else if (branches.All(branch => branch != false))
            {
                kept.Add(line);
            }
        }

        return string.Join('\n', kept);
    }

    private static void Branch(Stack<bool?> branches, string keyword, string condition)
    {
        switch (keyword)
        {
            case "if":
                branches.Push(_lineConditions.Contains(condition, StringComparer.Ordinal) ? true
                    : _lineConditions.Contains(condition.Replace("(!", "(", StringComparison.Ordinal), StringComparer.Ordinal) || condition == _thisRepositoryOnly ? false
                    : null);
                break;
            case "else" when branches.TryPop(out bool? taken):
                branches.Push(taken is null ? null : !taken);
                break;
            case "elseif" when branches.TryPop(out bool? taken):
                branches.Push(taken is null ? null : false);
                break;
            default:
                branches.TryPop(out _);
                break;
        }
    }

    private sealed record LineReplacement(string Replaces, string Value, List<(string? After, string? Before)> OnlyIf)
    {
        public static string Apply(string text, List<LineReplacement> replacements) =>
            replacements.Aggregate(text, (current, replacement) => replacement.Apply(current));

        private string Apply(string text)
        {
            var result = new System.Text.StringBuilder(text.Length);
            int start = 0;

            for (int found = text.IndexOf(Replaces, StringComparison.Ordinal); found >= 0; found = text.IndexOf(Replaces, found + Replaces.Length, StringComparison.Ordinal))
            {
                result.Append(text, start, found - start).Append(Applies(text, found) ? Value : Replaces);
                start = found + Replaces.Length;
            }

            return result.Append(text, start, text.Length - start).ToString();
        }

        private bool Applies(string text, int found) =>
            OnlyIf.Count == 0 || OnlyIf.Any(context =>
                (context.After is null || text.AsSpan(0, found).EndsWith(context.After, StringComparison.Ordinal))
                && (context.Before is null || text.AsSpan(found + Replaces.Length).StartsWith(context.Before, StringComparison.Ordinal)));
    }
}
