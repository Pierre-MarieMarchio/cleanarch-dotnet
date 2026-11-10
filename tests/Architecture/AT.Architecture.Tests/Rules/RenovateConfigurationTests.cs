using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AppTemplate.Architecture.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace AppTemplate.Architecture.Tests.Rules;

public sealed class RenovateConfigurationTests
{
    private const string _sdkImage = "mcr.microsoft.com/dotnet/sdk";

    private static readonly TimeSpan _budget = TimeSpan.FromSeconds(1);

    private static readonly string[] _dockerfileRoots = ["src", "tools"];

    private static readonly string[] _skippedFolders = ["node_modules", "bin", "obj", "dist"];

    private static readonly string[] _dependencySections = ["dependencies", "devDependencies"];

    private static readonly string[] _withinTheMajor = ["patch", "minor"];

    private static readonly Regex _from = new(@"^\s*FROM\s+(?:--platform=\S+\s+)?(?<image>[^\s:@]+)", RegexOptions.IgnoreCase, _budget);

    private static readonly Regex _pipelineImage = new(@"^\s*(?:-\s+)?(?:image|name):\s*(?<image>[^\s:@#]+)", RegexOptions.None, _budget);

    private static readonly JsonDocumentOptions _jsonc = new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

    /*#if (templateMaintenance)*/
    private static string GitLabRoot => Path.Combine(ProjectReferenceGraph.RepositoryRoot, ".template.config", "variants", "code-host-gitlab");
    /*#else
    private static string GitLabRoot => ProjectReferenceGraph.RepositoryRoot;
    #endif*/

    internal static string ConfigurationFile => Path.Combine(GitLabRoot, ".gitlab", "renovate.jsonc");

    private static string Workspace => Path.Combine(ProjectReferenceGraph.RepositoryRoot, "src", "Web");

    [Fact]
    public void TheWebImagesNode_NodeVersion_AndTypesNode_MoveInOneMergeRequest()
    {
        if (!Directory.Exists(Workspace))
        {
            return;
        }

        var dependencies = WebNodeImages()
            .Append(new RenovateDependency("nodenv", "node", "src/Web/.node-version"))
            .Concat(NodeTypes())
            .ToList();

        AssertMoveTogether(
            dependencies,
            "src/Web/.node-version, every FROM node: of a web app's Dockerfile and every @types/node of the workspace must move in one merge request, "
            + "or NodeVersionTests fails on each merge request that moves one of them alone: keep them in the node group of .gitlab/renovate.jsonc, "
            + "and keep nodenv and npm in its enabledManagers.");
    }

    [Fact]
    public void TheSdkImage_MovesInEveryDockerfileAndThePipelineInOneMergeRequest()
    {
        string root = ProjectReferenceGraph.RepositoryRoot;
        var dependencies = _dockerfileRoots
            .Select(folder => Path.Combine(root, folder))
            .Where(Directory.Exists)
            .SelectMany(folder => RepositoryTree.SourceFiles(folder, "*Dockerfile", _skippedFolders))
            .Where(file => File.ReadLines(file).Any(line => _from.Match(line) is { Success: true } from && from.Groups["image"].Value == _sdkImage))
            .Select(file => new RenovateDependency("dockerfile", _sdkImage, RepositoryTree.Relative(file)))
            .Concat(PipelineFiles()
                .Where(file => File.ReadLines(file).Any(line => _pipelineImage.Match(line) is { Success: true } image && image.Groups["image"].Value == _sdkImage))
                .Select(file => new RenovateDependency("gitlabci", _sdkImage, RepositoryTree.Relative(GitLabRoot, file))))
            .ToList();

        dependencies.ShouldContain(dependency => dependency.Manager == "dockerfile", "A Dockerfile builds on the .NET SDK image; a search that finds none is broken.");
        AssertMoveTogether(
            dependencies,
            "Every Dockerfile and the pipeline build on one .NET SDK image, which GitLabPipelinePinningTests holds, so a merge request that moves "
            + "the image in some of them alone fails: keep the SDK image enabled in every one, in one group of .gitlab/renovate.jsonc.");
    }

    [Fact]
    public void TheMatchers_AreSensitive()
    {
        using var config = JsonDocument.Parse(
            """
            {
              "enabledManagers": ["dockerfile", "npm"],
              "packageRules": [
                // a comment, as the file carries
                { "matchManagers": ["dockerfile"], "enabled": false },
                { "matchManagers": ["dockerfile"], "matchFileNames": ["src/Api/*", "src/Web/apps/*/*"], "enabled": true },
                { "groupName": "images", "matchManagers": ["dockerfile"], "matchPackageNames": ["*"] },
                { "groupName": "ms", "matchManagers": ["npm"], "matchPackageNames": ["Microsoft.*", "!Microsoft.Kiota.*"] },
                { "groupName": "minor", "matchManagers": ["npm"], "matchPackageNames": ["@types/*"], "matchUpdateTypes": ["minor", "patch"], "separateMajorMinor": true },
                { "matchManagers": ["npm"], "matchDepTypes": ["packageManager"], "enabled": false },
              ],
            }
            """,
            _jsonc);
        var rules = config.RootElement;

        Effective(rules, new("dockerfile", "mcr.microsoft.com/dotnet/sdk", "src/Api/Dockerfile")).ShouldBe(new RenovateOutcome(true, "images", false), "* matches a name with slashes");
        Effective(rules, new("dockerfile", "node", "src/Web/apps/Site/Dockerfile")).Enabled.ShouldBeTrue("a later rule enables what an earlier one disabled");
        Effective(rules, new("dockerfile", "node", "src/Other/Dockerfile")).Enabled.ShouldBeFalse("* in a file glob stops at a slash");
        Effective(rules, new("dockerfile", "node", "src/Api/nested/Dockerfile")).Enabled.ShouldBeFalse();
        Effective(rules, new("nuget", "Microsoft.Extensions.Http", "Directory.Packages.props")).Enabled.ShouldBeFalse("a manager not enabled reads nothing");
        Effective(rules, new("npm", "microsoft.extensions.http", "package.json")).Group.ShouldBe("ms", "package names compare without case");
        Effective(rules, new("npm", "Microsoft.Kiota.Bundle", "package.json")).Group.ShouldBeNull("a negated pattern leaves the name out");
        Effective(rules, new("npm", "@types/node", "package.json", "devDependencies", "minor")).ShouldBe(new RenovateOutcome(true, "minor", true));
        Effective(rules, new("npm", "@types/node", "package.json", "devDependencies", "major")).Group.ShouldBeNull("a rule for some update types leaves the others out");
        Effective(rules, new("npm", "pnpm", "package.json", "packageManager")).Enabled.ShouldBeFalse();
        Effective(rules, new("npm", "pnpm", "package.json")).Enabled.ShouldBeTrue("a rule for a dependency type matches no dependency of an unknown type");
    }

    internal static JsonDocument Parse(string text) => JsonDocument.Parse(text, _jsonc);

    internal static RenovateOutcome Effective(JsonElement config, RenovateDependency dependency)
    {
        bool enabled = !config.TryGetProperty("enabledManagers", out var managers)
            || managers.EnumerateArray().Any(manager => manager.GetString() == dependency.Manager);
        string? group = null;
        bool separate = config.TryGetProperty("separateMajorMinor", out var top) && top.GetBoolean();

        foreach (var rule in config.TryGetProperty("packageRules", out var rules) ? rules.EnumerateArray() : Enumerable.Empty<JsonElement>())
        {
            if (!Matches(rule, dependency))
            {
                continue;
            }

            enabled = rule.TryGetProperty("enabled", out var on) ? on.GetBoolean() : enabled;
            group = rule.TryGetProperty("groupName", out var name) ? name.GetString() : group;
            separate = rule.TryGetProperty("separateMajorMinor", out var split) ? split.GetBoolean() : separate;
        }

        return new RenovateOutcome(enabled, group, separate);
    }

    internal static JsonElement? Setting(JsonElement config, RenovateDependency dependency, string name)
    {
        JsonElement? value = config.TryGetProperty(name, out var top) ? top : null;

        foreach (var rule in config.TryGetProperty("packageRules", out var rules) ? rules.EnumerateArray() : Enumerable.Empty<JsonElement>())
        {
            value = Matches(rule, dependency) && rule.TryGetProperty(name, out var set) ? set : value;
        }

        return value;
    }

    internal static bool MatchesList(string input, IReadOnlyList<string> patterns)
    {
        var positive = patterns.Where(pattern => !pattern.StartsWith('!')).ToList();
        var negative = patterns.Where(pattern => pattern.StartsWith('!')).Select(pattern => pattern[1..]).ToList();
        return patterns.Count > 0
            && (positive.Count == 0 || positive.Any(pattern => MatchesGlob(input, pattern)))
            && !negative.Any(pattern => MatchesGlob(input, pattern));
    }

    private static bool Matches(JsonElement rule, RenovateDependency dependency) =>
        Holds(rule, "matchManagers", values => values.Contains(dependency.Manager, StringComparer.Ordinal))
        && Holds(rule, "matchPackageNames", values => MatchesList(dependency.Package, values))
        && Holds(rule, "matchFileNames", values => MatchesList(dependency.File, values))
        && Holds(rule, "matchDepTypes", values => dependency.DepType is { } type && values.Contains(type, StringComparer.Ordinal))
        && Holds(rule, "matchUpdateTypes", values => dependency.UpdateType is { } update && values.Contains(update, StringComparer.Ordinal));

    private static bool Holds(JsonElement rule, string matcher, Func<List<string>, bool> test) =>
        !rule.TryGetProperty(matcher, out var values) || test([.. values.EnumerateArray().Select(value => value.GetString()!)]);

    private static bool MatchesGlob(string input, string pattern) =>
        pattern == "*"
        || new Regex(
            "^" + Regex.Escape(pattern)
                .Replace(@"\*\*/", "(?:.*/)?", StringComparison.Ordinal)
                .Replace(@"\*\*", ".*", StringComparison.Ordinal)
                .Replace(@"\*", "[^/]*", StringComparison.Ordinal)
                .Replace(@"\?", "[^/]", StringComparison.Ordinal) + "$",
            RegexOptions.IgnoreCase,
            _budget).IsMatch(input);

    private static void AssertMoveTogether(List<RenovateDependency> dependencies, string because)
    {
        File.Exists(ConfigurationFile).ShouldBeTrue($"{ConfigurationFile} is missing: Renovate reads the project's dependency updates from it.");
        using var config = Parse(File.ReadAllText(ConfigurationFile));

        var outcomes = dependencies
            .SelectMany(dependency => _withinTheMajor.Select(update => (Dependency: dependency with { UpdateType = update }, Outcome: Effective(config.RootElement, dependency with { UpdateType = update }))))
            .ToList();

        outcomes
            .Where(entry => !entry.Outcome.Enabled || entry.Outcome.Group is null || entry.Outcome != outcomes[0].Outcome)
            .Select(entry => $"{entry.Dependency.File}: {entry.Dependency.Package} ({entry.Dependency.Manager}, {entry.Dependency.UpdateType}) -> {entry.Outcome}")
            .ShouldBeEmpty(because);
    }

    private static IEnumerable<RenovateDependency> WebNodeImages() =>
        Directory.Exists(Path.Combine(Workspace, "apps"))
            ? Directory.EnumerateDirectories(Path.Combine(Workspace, "apps"))
                .Select(app => Path.Combine(app, "Dockerfile"))
                .Where(File.Exists)
                .Where(file => File.ReadLines(file).Any(line => _from.Match(line) is { Success: true } from && from.Groups["image"].Value == "node"))
                .Select(file => new RenovateDependency("dockerfile", "node", RepositoryTree.Relative(file)))
            : [];

    private static IEnumerable<RenovateDependency> NodeTypes() =>
        RepositoryTree.SourceFiles(Workspace, "package.json", _skippedFolders)
            .SelectMany(file => _dependencySections
                .Where(section => JsonNode.Parse(File.ReadAllText(file))?[section]?["@types/node"] is not null)
                .Select(section => new RenovateDependency("npm", "@types/node", RepositoryTree.Relative(file), section)));

    private static IEnumerable<string> PipelineFiles()
    {
        string pipeline = Path.Combine(GitLabRoot, ".gitlab-ci.yml");
        string folder = Path.Combine(GitLabRoot, ".gitlab", "ci");
        return (File.Exists(pipeline) ? [pipeline] : Array.Empty<string>())
            .Concat(Directory.Exists(folder) ? Directory.EnumerateFiles(folder, "*.yml", SearchOption.AllDirectories) : []);
    }
}

internal sealed record RenovateDependency(string Manager, string Package, string File, string? DepType = null, string? UpdateType = null);

internal sealed record RenovateOutcome(bool Enabled, string? Group, bool SeparateMajorMinor);
