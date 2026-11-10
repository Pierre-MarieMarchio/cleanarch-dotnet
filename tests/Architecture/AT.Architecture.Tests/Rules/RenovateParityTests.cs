using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AppTemplate.Architecture.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace AppTemplate.Architecture.Tests.Rules;

public sealed class RenovateParityTests
{
    private const string _probePackage = "Probe.Package";

    private static readonly TimeSpan _budget = TimeSpan.FromSeconds(5);

    private static readonly Dictionary<string, string> _managers = new(StringComparer.Ordinal)
    {
        ["nuget"] = "nuget",
        ["github-actions"] = "gitlabci",
        ["docker"] = "dockerfile",
        ["npm"] = "npm",
        ["docker-compose"] = "docker-compose",
    };

    private static readonly Dictionary<string, string> _probeFiles = new(StringComparer.Ordinal)
    {
        ["nuget"] = "Directory.Packages.props",
        ["gitlabci"] = ".gitlab-ci.yml",
        ["dockerfile"] = "Dockerfile",
        ["npm"] = "package.json",
        ["docker-compose"] = "docker-compose.yml",
    };

    private static readonly string[] _readInTheirFolderAlone = ["docker", "docker-compose"];

    private static readonly Dictionary<string, string> _gitHubNames = new(StringComparer.Ordinal)
    {
        ["github-actions"] = "gitlab-ci",
        ["actions"] = "pipeline-images",
    };

    private static readonly string[] _weekdays = ["sunday", "monday", "tuesday", "wednesday", "thursday", "friday", "saturday"];

    private static readonly Regex _marker = new(@"^\s*(?://)?#(?<keyword>if|elseif|else|endif)\b\s*(?<condition>.*)$", RegexOptions.None, _budget);

    private static readonly Regex _token = new(@"\s*(?<token>[A-Za-z_]\w*|&&|\|\||!|\(|\))", RegexOptions.None, _budget);

    private static readonly Regex _keyValue = new(@"^(?<key>[A-Za-z][\w-]*):(?:\s+(?<value>.*))?$", RegexOptions.None, _budget);

    private static readonly Regex _schedule = new(@"^\* (?<hour>\d+)(?:-\d+)? \* \* (?<day>\d)$", RegexOptions.None, _budget);

    private static readonly Regex _prefix = new(@"^(?<type>\w+)\((?<scope>[^)]+)\)$", RegexOptions.None, _budget);

    private static string DependabotFile => Path.Combine(ProjectReferenceGraph.RepositoryRoot, ".github", "dependabot.yml");

    [Fact]
    public void AGitLabProject_RenovateUpdatesWhatDependabotUpdates()
    {
        string[] dependabot = File.ReadAllLines(DependabotFile);
        string[] renovate = File.ReadAllLines(RenovateConfigurationTests.ConfigurationFile);
        var symbols = Symbols(dependabot).Union(Symbols(renovate)).Where(symbol => symbol != "templateMaintenance").Order(StringComparer.Ordinal).ToList();
        var problems = new List<string>();

        symbols.Count.ShouldBeGreaterThanOrEqualTo(5, "dependabot.yml conditions its entries on far more symbols than were read, so this rule is not reading its markers.");

        for (int mask = 0; mask < 1 << symbols.Count; mask++)
        {
            var values = symbols.Select((symbol, index) => (symbol, ((mask >> index) & 1) == 1)).ToDictionary(StringComparer.Ordinal);
            values["templateMaintenance"] = false;
            string shape = string.Join(" && ", symbols.Select(symbol => values[symbol] ? symbol : "!" + symbol));
            var updates = Yaml(Resolved(dependabot, values))["updates"]!.AsArray();
            using var config = RenovateConfigurationTests.Parse(string.Join('\n', Resolved(renovate, values)));

            problems.AddRange(Differences(updates, config.RootElement).Select(difference => $"({shape}) {difference}"));
        }

        problems.Take(40).ShouldBeEmpty(
            "A GitLab project's .gitlab/renovate.jsonc updates what .github/dependabot.yml updates on GitHub, under the same markers: each "
            + "ecosystem's manager, folders, schedule, labels and commit prefix, each group with its patterns and exclusions, and the limits added up. "
            + "Change the two files together.");
    }

    [Fact]
    public void TheRenovateEntriesTheWebReadmeLists_AreTheOnesWebWrites() =>
        AssertListedAsWritten(Path.Combine("src", "Web", "README.md"), "anyWeb", 4);

    [Fact]
    public void TheRenovateEntryTheWorkerReadmeLists_IsTheOneTheWorkerWrites() =>
        AssertListedAsWritten(Path.Combine(".template.config", "starters", "worker", "src", "Presentation", "AT.Worker", "README.md"), "jobsWorker", 1);

    [Fact]
    public void TheReaders_AreSensitive()
    {
        var values = new Dictionary<string, bool>(StringComparer.Ordinal) { ["a"] = true, ["b"] = false };

        Resolved(["x", "#if (a)", "y", "#else", "z", "#endif", "  //#if (a && !b)", "w", "//#endif", "#if (b || !a)", "v", "#endif"], values)
            .ShouldBe(["x", "y", "w"]);
        Resolved(["#if (b)", "#if (a)", "y", "#endif", "#else", "z", "#endif"], values).ShouldBe(["z"], "a branch inside one not taken is not taken");
        Should.Throw<InvalidOperationException>(() => Resolved(["#if (a == \"x\")", "#endif"], values));
        Symbols(["#if (templateMaintenance && anyWeb)", "  //#if (!api)", "x: #if (no)"]).ShouldBe(["templateMaintenance", "anyWeb", "api"]);

        var node = Yaml(
        [
            "version: 2",
            "updates:",
            "  # a comment",
            "  - package-ecosystem: nuget",
            "    directories:",
            "      - /src/A",
            "    schedule:",
            "      time: \"06:00\"",
            "",
            "    groups:",
            "      lint:",
            "        patterns:",
            "          - \"@eslint/*\"",
            "          - eslint",
            "  - package-ecosystem: npm",
        ]);

        node["version"]!.GetValue<string>().ShouldBe("2");
        node["updates"]!.AsArray().Count.ShouldBe(2);
        node["updates"]![0]!["directories"]![0]!.GetValue<string>().ShouldBe("/src/A");
        node["updates"]![0]!["schedule"]!["time"]!.GetValue<string>().ShouldBe("06:00");
        node["updates"]![0]!["groups"]!["lint"]!["patterns"]!.AsArray().Select(pattern => pattern!.GetValue<string>()).ShouldBe(["@eslint/*", "eslint"]);
        node["updates"]![1]!["package-ecosystem"]!.GetValue<string>().ShouldBe("npm");

        Probe("/src/Web/apps/*", "dockerfile").ShouldBe("src/Web/apps/Probe/Dockerfile");
        Probe("/", "docker-compose").ShouldBe("docker-compose.yml");
        FolderOf("src/Web/apps/*/*").ShouldBe("/src/Web/apps/*");
        FolderOf("*.yml").ShouldBe("/");
    }

    private static void AssertListedAsWritten(string readme, string symbol, int blocks)
    {
        string root = ProjectReferenceGraph.RepositoryRoot;
        var written = TemplatePackagingTests.ConditionedBlocks(
            [.. File.ReadAllLines(RenovateConfigurationTests.ConfigurationFile).Select(line => line.Replace("//#", "#", StringComparison.Ordinal))], symbol);
        var listed = TemplatePackagingTests.AddedBlocksUnder(File.ReadAllLines(Path.Combine(root, readme)), "- in `.gitlab/renovate.jsonc`");

        written.Count.ShouldBe(blocks, $".gitlab/renovate.jsonc was expected to hold {blocks} blocks conditioned on {symbol} alone.");
        listed.ShouldBe(
            written,
            $"{readme} lists Renovate entries for a GitLab project that joins it which differ from what generation writes into .gitlab/renovate.jsonc. "
            + $"Copy the {symbol} blocks into the README, as they are.");
    }

    private static IEnumerable<string> Differences(JsonArray updates, JsonElement config)
    {
        var ecosystems = updates.Select(update => Text(update, "package-ecosystem")).ToList();
        var expected = ecosystems.Select(ecosystem => _managers[ecosystem]).ToList();
        expected.AddRange(ecosystems.Contains("npm") ? ["nodenv"] : []);
        var managers = Strings(config, "enabledManagers");

        if (!managers.Order(StringComparer.Ordinal).SequenceEqual(expected.Order(StringComparer.Ordinal)))
        {
            yield return $"enabledManagers is [{string.Join(", ", managers)}], where Dependabot's ecosystems ask for [{string.Join(", ", expected)}].";
        }

        int limit = updates.Sum(update => int.Parse(Text(update, "open-pull-requests-limit"), System.Globalization.CultureInfo.InvariantCulture));
        if (!config.TryGetProperty("prConcurrentLimit", out var concurrent) || concurrent.GetInt32() != limit)
        {
            yield return $"prConcurrentLimit is not {limit}, Dependabot's limits added up.";
        }

        if (config.TryGetProperty("vulnerabilityAlerts", out _) || (config.TryGetProperty("osvVulnerabilityAlerts", out var osv) && osv.GetBoolean()))
        {
            yield return "the groups apply to version updates alone, as applies-to: version-updates says: leave vulnerabilityAlerts and osvVulnerabilityAlerts out.";
        }

        foreach (var update in updates)
        {
            foreach (string difference in Differences(update!, config))
            {
                yield return $"{Text(update, "package-ecosystem")}: {difference}";
            }
        }
    }

    private static IEnumerable<string> Differences(JsonNode update, JsonElement config)
    {
        string ecosystem = Text(update, "package-ecosystem");
        string manager = _managers[ecosystem];
        var folders = update.AsObject().ContainsKey("directories")
            ? update["directories"]?.AsArray().Select(folder => folder!.GetValue<string>()).ToList() ?? []
            : [Text(update, "directory")];
        var schedule = update["schedule"]!;

        if (Text(schedule, "interval") != "weekly"
            || !config.TryGetProperty("schedule", out var window) || window.GetArrayLength() != 1
            || _schedule.Match(window[0].GetString()!) is not { Success: true } cron
            || int.Parse(cron.Groups["day"].Value, System.Globalization.CultureInfo.InvariantCulture) != Array.IndexOf(_weekdays, Text(schedule, "day"))
            || $"{int.Parse(cron.Groups["hour"].Value, System.Globalization.CultureInfo.InvariantCulture):00}:00" != Text(schedule, "time")
            || config.GetProperty("timezone").GetString() != Text(schedule, "timezone"))
        {
            yield return "the schedule does not open from the weekly day, hour and time zone Dependabot runs at.";
        }

        foreach (string file in folders.Select(folder => Probe(folder, manager)))
        {
            var dependency = new RenovateDependency(manager, _probePackage, file);
            if (!RenovateConfigurationTests.Effective(config, dependency).Enabled)
            {
                yield return $"Renovate reads no {file}, which Dependabot reads.";
            }

            var labels = update["labels"]!.AsArray().Select(label => Named(label!.GetValue<string>())).ToList();
            if (RenovateConfigurationTests.Setting(config, dependency, "labels") is not { } set || !set.EnumerateArray().Select(label => label.GetString()).SequenceEqual(labels))
            {
                yield return $"the labels of {file} are not [{string.Join(", ", labels)}].";
            }

            foreach (var (prefix, type) in new[] { ("prefix", (string?)null), ("prefix-development", "devDependencies") })
            {
                if (update["commit-message"]?[prefix] is not { } wanted || (type is not null && manager != "npm"))
                {
                    continue;
                }

                var commit = _prefix.Match(wanted.GetValue<string>());
                var typed = dependency with { DepType = type };
                if (RenovateConfigurationTests.Setting(config, typed, "semanticCommitType")?.GetString() != commit.Groups["type"].Value
                    || RenovateConfigurationTests.Setting(config, typed, "semanticCommitScope")?.GetString() != commit.Groups["scope"].Value)
                {
                    yield return $"the commits of {file}{(type is null ? "" : $" for {type}")} do not start with {wanted.GetValue<string>()}.";
                }
            }
        }

        if (_readInTheirFolderAlone.Contains(ecosystem, StringComparer.Ordinal))
        {
            string elsewhere = Probe("/elsewhere", manager);
            if (RenovateConfigurationTests.Effective(config, new RenovateDependency(manager, _probePackage, elsewhere)).Enabled)
            {
                yield return $"Renovate reads {elsewhere}, which lies outside every folder Dependabot reads.";
            }

            var named = Rules(config, manager).Where(rule => rule.TryGetProperty("matchFileNames", out _) && rule.TryGetProperty("enabled", out var on) && on.GetBoolean())
                .SelectMany(rule => Strings(rule, "matchFileNames")).Select(FolderOf).ToList();
            if (!named.SequenceEqual(folders))
            {
                yield return $"Renovate enables the folders [{string.Join(", ", named)}], where Dependabot reads [{string.Join(", ", folders)}], in that order.";
            }
        }

        var groups = update["groups"]?.AsObject() ?? [];
        foreach (var (name, group) in groups)
        {
            foreach (string difference in GroupDifferences(config, manager, Probe(folders.FirstOrDefault() ?? "/", manager), Named(name), group!))
            {
                yield return $"group {name}: {difference}";
            }
        }

        var extra = Rules(config, manager).Where(rule => rule.TryGetProperty("groupName", out _)).Select(rule => rule.GetProperty("groupName").GetString()!)
            .Except(groups.Select(group => Named(group.Key)), StringComparer.Ordinal).ToList();
        if (extra.Count > 0)
        {
            yield return $"Renovate groups [{string.Join(", ", extra)}] of this manager alone, which Dependabot does not.";
        }
    }

    private static IEnumerable<string> GroupDifferences(JsonElement config, string manager, string file, string name, JsonNode group)
    {
        var patterns = group["patterns"]?.AsArray().Select(pattern => pattern!.GetValue<string>()).ToList() ?? [];
        var excluded = group["exclude-patterns"]?.AsArray().Select(pattern => pattern!.GetValue<string>()).ToList() ?? [];
        var rules = Rules(config, manager).Where(rule => rule.TryGetProperty("groupName", out var groupName) && groupName.GetString() == name).ToList();

        if (Text(group, "applies-to") != "version-updates")
        {
            yield return "applies to other updates than version updates, which Renovate's groups do not stand for.";
        }

        if (rules.Count != 1)
        {
            yield return $"{rules.Count} rules of this manager alone name the group {name}, not one.";
            yield break;
        }

        var names = Strings(rules[0], "matchPackageNames");
        if (!names.SequenceEqual(patterns.Concat(excluded.Select(pattern => "!" + pattern))))
        {
            yield return $"matchPackageNames is [{string.Join(", ", names)}], not the patterns then the exclusions, in order.";
        }

        foreach (string pattern in patterns)
        {
            string package = pattern.Replace("*", "Probe", StringComparison.Ordinal);
            if (RenovateConfigurationTests.Effective(config, new RenovateDependency(manager, package, file)).Group != name)
            {
                yield return $"{package}, which {pattern} matches, ends in another group.";
            }
        }

        foreach (string pattern in excluded)
        {
            string package = pattern.Replace("*", "Probe", StringComparison.Ordinal);
            if (RenovateConfigurationTests.Effective(config, new RenovateDependency(manager, package, file)).Group == name)
            {
                yield return $"{package}, which the exclusion {pattern} leaves out, ends in the group.";
            }
        }
    }

    private static List<JsonElement> Rules(JsonElement config, string manager) =>
        [.. config.GetProperty("packageRules").EnumerateArray()
            .Where(rule => rule.TryGetProperty("matchManagers", out var managers) && managers.GetArrayLength() == 1 && managers[0].GetString() == manager)];

    private static List<string> Strings(JsonElement element, string property) =>
        element.TryGetProperty(property, out var values) ? [.. values.EnumerateArray().Select(value => value.GetString()!)] : [];

    private static string Text(JsonNode? node, string key) =>
        node?[key]?.GetValue<string>() ?? throw new InvalidOperationException($"dependabot.yml has no {key} where this rule reads one.");

    private static string Named(string gitHubName) => _gitHubNames.GetValueOrDefault(gitHubName, gitHubName);

    private static string Probe(string folder, string manager)
    {
        string relative = folder.Trim('/').Replace("*", "Probe", StringComparison.Ordinal);
        return relative.Length == 0 ? _probeFiles[manager] : $"{relative}/{_probeFiles[manager]}";
    }

    private static string FolderOf(string glob) =>
        glob.Contains('/', StringComparison.Ordinal) ? "/" + glob[..glob.LastIndexOf('/')] : "/";

    private static List<string> Symbols(IEnumerable<string> lines) =>
        [.. lines.Select(line => _marker.Match(line)).Where(marker => marker.Success)
            .SelectMany(marker => _token.Matches(marker.Groups["condition"].Value).Select(token => token.Groups["token"].Value))
            .Where(token => char.IsLetter(token[0]) || token[0] == '_')
            .Distinct(StringComparer.Ordinal)];

    private static List<string> Resolved(IEnumerable<string> lines, IReadOnlyDictionary<string, bool> values)
    {
        var branches = new Stack<(bool Parent, bool Active, bool Taken)>();
        bool active = true;
        var kept = new List<string>();

        foreach (string line in lines)
        {
            Match marker = _marker.Match(line);
            if (!marker.Success)
            {
                if (active)
                {
                    kept.Add(line);
                }

                continue;
            }

            switch (marker.Groups["keyword"].Value)
            {
                case "if":
                    bool holds = active && Evaluate(marker.Groups["condition"].Value, values);
                    branches.Push((active, holds, holds));
                    active = holds;
                    break;
                case "elseif":
                    var open = branches.Pop();
                    bool next = open.Parent && !open.Taken && Evaluate(marker.Groups["condition"].Value, values);
                    branches.Push((open.Parent, next, open.Taken || next));
                    active = next;
                    break;
                case "else":
                    var last = branches.Pop();
                    branches.Push((last.Parent, last.Parent && !last.Taken, true));
                    active = last.Parent && !last.Taken;
                    break;
                default:
                    active = branches.Pop().Parent;
                    break;
            }
        }

        return kept;
    }

    private static bool Evaluate(string condition, IReadOnlyDictionary<string, bool> values)
    {
        var tokens = new Queue<string>(_token.Matches(condition).Select(token => token.Groups["token"].Value));
        if (string.Concat(tokens) != Regex.Replace(condition, @"\s+", "", RegexOptions.None, _budget))
        {
            throw new InvalidOperationException($"'{condition}' is not a condition of symbols, !, && and || this rule reads.");
        }

        bool result = Or(tokens, values);
        return tokens.Count == 0 ? result : throw new InvalidOperationException($"'{condition}' has tokens left over.");
    }

    private static bool Or(Queue<string> tokens, IReadOnlyDictionary<string, bool> values)
    {
        bool result = And(tokens, values);
        while (tokens.TryPeek(out string? token) && token == "||")
        {
            tokens.Dequeue();
            result = And(tokens, values) | result;
        }

        return result;
    }

    private static bool And(Queue<string> tokens, IReadOnlyDictionary<string, bool> values)
    {
        bool result = Not(tokens, values);
        while (tokens.TryPeek(out string? token) && token == "&&")
        {
            tokens.Dequeue();
            result = Not(tokens, values) & result;
        }

        return result;
    }

    private static bool Not(Queue<string> tokens, IReadOnlyDictionary<string, bool> values)
    {
        string token = tokens.Dequeue();
        if (token == "!")
        {
            return !Not(tokens, values);
        }

        if (token == "(")
        {
            bool inner = Or(tokens, values);
            tokens.Dequeue().ShouldBe(")");
            return inner;
        }

        return values.TryGetValue(token, out bool value) ? value : throw new InvalidOperationException($"No value was given to the symbol {token}.");
    }

    private static JsonObject Yaml(IEnumerable<string> lines)
    {
        var content = lines.Where(line => line.Trim().Length > 0 && !line.TrimStart().StartsWith('#')).ToList();
        int index = 0;
        return (JsonObject)Block(content, ref index, Indent(content[0]));
    }

    private static JsonNode Block(List<string> lines, ref int index, int indent)
    {
        if (lines[index].TrimStart().StartsWith("- ", StringComparison.Ordinal))
        {
            var list = new JsonArray();
            while (index < lines.Count && Indent(lines[index]) == indent && lines[index].TrimStart().StartsWith("- ", StringComparison.Ordinal))
            {
                string item = lines[index].Trim()[2..];
                if (_keyValue.IsMatch(item))
                {
                    lines[index] = new string(' ', indent + 2) + item;
                    list.Add(Block(lines, ref index, indent + 2));
                }
                else
                {
                    list.Add(Scalar(item));
                    index++;
                }
            }

            return list;
        }

        var map = new JsonObject();
        while (index < lines.Count && Indent(lines[index]) == indent && _keyValue.Match(lines[index].Trim()) is { Success: true } entry)
        {
            index++;
            map[entry.Groups["key"].Value] = entry.Groups["value"].Success && entry.Groups["value"].Value.Length > 0
                ? Scalar(entry.Groups["value"].Value)
                : index < lines.Count && Indent(lines[index]) > indent ? Block(lines, ref index, Indent(lines[index])) : null;
        }

        return map;
    }

    private static JsonValue Scalar(string text) => JsonValue.Create(text.Trim().Trim('"'));

    private static int Indent(string line) => line.Length - line.TrimStart().Length;
}
