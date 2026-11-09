using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AppTemplate.Architecture.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace AppTemplate.Architecture.Tests.Rules;

public sealed class NodeVersionTests
{
    private static readonly string[] _skippedFolders = ["node_modules", "bin", "obj", "dist"];

    private static readonly Regex _version = new(
        @"^(?<major>\d+)\.\d+\.\d+$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Regex _from = new(
        @"^\s*FROM\s+(?:--platform=\S+\s+)?(?<ref>\S+)",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
        TimeSpan.FromSeconds(1));

    private static readonly Regex _nodeImage = new(
        @"^node:(?<tag>\S*)$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Regex _nodeTag = new(
        @"^(?<version>\d+\.\d+\.\d+)-[a-z0-9.]+@sha256:[0-9a-f]{64}$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Regex _engineMajor = new(
        @"^\s*(?:>=|\^|~)?\s*v?(?<major>\d+)(?:\.\d+){0,2}\s*$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Regex _typesMajor = new(
        @"^\s*[\^~]?(?<major>\d+)\.\d+\.\d+\s*$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly string[] _dependencySections = ["dependencies", "devDependencies"];

    private static string Workspace => Path.Combine(ProjectReferenceGraph.RepositoryRoot, "src", "Web");

    private static string NodeVersionFile => Path.Combine(Workspace, ".node-version");

    [Fact]
    public void TheNodeVersionFile_HoldsOneExactVersion()
    {
        if (!Directory.Exists(Workspace))
        {
            return;
        }

        File.Exists(NodeVersionFile).ShouldBeTrue("src/Web/.node-version names the Node.js CI and developers run; the web workspace holds none.");

        string text = File.ReadAllText(NodeVersionFile);

        (text.EndsWith('\n') && _version.IsMatch(text[..^1])).ShouldBeTrue(
            $"src/Web/.node-version holds '{text.ReplaceLineEndings("\\n")}': write one exact version, such as 26.13.0, then a line feed, "
            + "which is the form setup-node reads and the alignment workflow writes.");
    }

    [Fact]
    public void EveryNodeImage_RunsTheVersionInNodeVersion()
    {
        if (!File.Exists(NodeVersionFile))
        {
            return;
        }

        string expected = File.ReadAllText(NodeVersionFile).Trim();

        NodeImageOffenders(WebAppDockerfiles(), expected).ShouldBeEmpty(
            $"src/Web/.node-version names Node.js {expected}, and every FROM node: of a web app's Dockerfile must run exactly that version, "
            + "as node:<version>-<variant>@sha256:<digest>, so CI tests what the image ships. Align the Dockerfiles and "
            + "src/Web/.node-version on one version.");
    }

    [Fact]
    public void TheEnginesMajor_IsTheMajorInNodeVersion()
    {
        if (!File.Exists(NodeVersionFile))
        {
            return;
        }

        string expected = File.ReadAllText(NodeVersionFile).Trim();
        string engines = EnginesNode(Path.Combine(Workspace, "package.json"));

        EngineMajor(engines).ShouldBe(
            _version.Match(expected).Groups["major"].Value,
            $"src/Web/package.json requires Node.js '{engines}' in engines.node, on another major line than the {expected} of "
            + "src/Web/.node-version. A new major is a change made by hand: move .node-version, engines.node, @types/node and every FROM node: together.");
    }

    [Fact]
    public void EveryNodeTypesMajor_IsTheMajorInNodeVersion()
    {
        if (!File.Exists(NodeVersionFile))
        {
            return;
        }

        string expected = File.ReadAllText(NodeVersionFile).Trim();
        string major = _version.Match(expected).Groups["major"].Value;

        NodeTypesDeclarations()
            .Where(declaration => _typesMajor.Match(declaration.Version) is var match && (!match.Success || match.Groups["major"].Value != major))
            .Select(declaration => $"{declaration.File}: @types/node {declaration.Version}")
            .ShouldBeEmpty(
                $"These packages type Node.js on another major line than the {expected} of src/Web/.node-version, so the code is checked "
                + "against APIs the runtime does not have. A new major is a change made by hand: move .node-version, engines.node, "
                + "@types/node and every FROM node: together.");
    }

    [Fact]
    public void TheMatchers_AreSensitive()
    {
        const string digest = "sha256:8996bb5821d62a8fd9bb27c1cb07462f3fdce6e47d640c6a881ead0b064ce216";

        string[] dockerfile =
        [
            "FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0.403-noble@" + digest + " AS publish",
            "FROM node:26.13.0-alpine3.24@" + digest + " AS build",
            "FROM node:24.11.0-alpine3.24@" + digest + " AS runtime",
            "FROM node:26-alpine AS other",
            "FROM build AS final",
        ];

        NodeImageOffenders(dockerfile, "26.13.0").ShouldBe(
            ["FROM node:24.11.0-alpine3.24@" + digest + " AS runtime", "FROM node:26-alpine AS other"]);
        NodeImageOffenders(dockerfile[..2], "26.13.0").ShouldBeEmpty();

        EngineMajor(">=26.13.0").ShouldBe("26");
        EngineMajor("^24").ShouldBe("24");
        EngineMajor(">=24 <27").ShouldBeNull("a range spanning majors names no single line");
        _typesMajor.Match("26.6.6").Groups["major"].Value.ShouldBe("26");
        _typesMajor.Match("^27.0.0").Groups["major"].Value.ShouldBe("27");
        _typesMajor.IsMatch("latest").ShouldBeFalse();
        _version.IsMatch("26.13").ShouldBeFalse();
        _version.IsMatch("v26.13.0").ShouldBeFalse();
    }

    private static List<string> NodeImageOffenders(IEnumerable<string> dockerfiles, string expected) =>
        [.. dockerfiles.SelectMany(dockerfile => NodeImageOffenders(File.ReadAllLines(dockerfile), expected)
            .Select(line => $"{RepositoryTree.Relative(dockerfile)}: {line}"))];

    private static IEnumerable<string> NodeImageOffenders(string[] lines, string expected) =>
        lines
            .Select(line => (Line: line.Trim(), From: _from.Match(line)))
            .Where(entry => entry.From.Success && _nodeImage.Match(entry.From.Groups["ref"].Value) is { Success: true } image
                && _nodeTag.Match(image.Groups["tag"].Value) is var tag
                && (!tag.Success || tag.Groups["version"].Value != expected))
            .Select(entry => entry.Line);

    private static string? EngineMajor(string engines) =>
        _engineMajor.Match(engines) is { Success: true } match ? match.Groups["major"].Value : null;

    private static string EnginesNode(string packageJson) =>
        JsonNode.Parse(File.ReadAllText(packageJson))?["engines"]?["node"]?.GetValue<string>()
        ?? throw new InvalidOperationException("src/Web/package.json declares no engines.node: add \">=<the version in .node-version>\".");

    private static List<(string File, string Version)> NodeTypesDeclarations() =>
        [.. RepositoryTree.SourceFiles(Workspace, "package.json", _skippedFolders)
            .Order(StringComparer.Ordinal)
            .SelectMany(file => _dependencySections
                .Select(section => JsonNode.Parse(File.ReadAllText(file))?[section]?["@types/node"]?.GetValue<string>())
                .OfType<string>()
                .Select(version => (RepositoryTree.Relative(file), version)))];

    private static List<string> WebAppDockerfiles()
    {
        string apps = Path.Combine(Workspace, "apps");

        return Directory.Exists(apps)
            ? [.. RepositoryTree.SourceFiles(apps, "*", _skippedFolders)
                .Where(file => Path.GetFileName(file) is var name
                    && (name == "Dockerfile" || name.EndsWith(".Dockerfile", StringComparison.Ordinal)))
                .Order(StringComparer.Ordinal)]
            : [];
    }
}
