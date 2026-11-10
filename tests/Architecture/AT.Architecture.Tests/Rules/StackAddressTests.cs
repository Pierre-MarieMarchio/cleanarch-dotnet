using System.Text.RegularExpressions;
using AppTemplate.Architecture.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace AppTemplate.Architecture.Tests.Rules;

public sealed partial class StackAddressTests
{
    private const string _publishedAddress = "${STACK_PUBLISH_ADDRESS:-127.0.0.1}:";

    private const string _hostVariable = "STACK_HOST";

    private static readonly string[] _skippedFolders = ["node_modules", "bin", "obj"];

    [GeneratedRegex(@"^(?<indent>\s*)ports:\s*$", RegexOptions.CultureInvariant)]
    private static partial Regex PortsKey();

    [GeneratedRegex(@"^\s*-\s*""(?<mapping>[^""]+)""", RegexOptions.CultureInvariant)]
    private static partial Regex QuotedItem();

    [GeneratedRegex(@"^\s*-\s*(?<mapping>[^\s#""]+)", RegexOptions.CultureInvariant)]
    private static partial Regex BareItem();

    [GeneratedRegex(@"https?://(?:localhost|127\.0\.0\.1):", RegexOptions.CultureInvariant)]
    private static partial Regex LoopbackUrl();

    [Fact]
    public void EveryPortTheStackPublishes_GoesThroughTheStackAddress()
    {
        var mappings = ComposeFiles().SelectMany(file => Mappings(File.ReadAllLines(file)).Select(mapping => (File: RepositoryTree.Relative(file), Mapping: mapping))).ToList();

        mappings.ShouldNotBeEmpty("The Compose files publish the API's port; a read that finds no ports: entry is broken.");
        mappings
            .Where(entry => !entry.Mapping.StartsWith(_publishedAddress, StringComparison.Ordinal))
            .Select(entry => $"{entry.File}: {entry.Mapping}")
            .ShouldBeEmpty(
                $"Publish every port as \"{_publishedAddress}${{<NAME>_PORT:-<port>}}:<container port>\": a developer's stack stays on 127.0.0.1, "
                + "and a pipeline whose daemon runs in another container sets STACK_PUBLISH_ADDRESS to reach it.");
    }

    [Fact]
    public void EveryClientOfTheStack_ReachesTheHostStackHostNames()
    {
        string root = ProjectReferenceGraph.RepositoryRoot;
        string[] clients =
        [
            .. new[] { Path.Combine(root, "tests", "EndToEnd") }.Where(Directory.Exists).SelectMany(folder => RepositoryTree.SourceFiles(folder, "*.cs", _skippedFolders)),
            .. new[] { Path.Combine(root, "src", "Web", "packages", "browser-smoke", "src") }.Where(Directory.Exists).SelectMany(folder => RepositoryTree.SourceFiles(folder, "*.ts", _skippedFolders)),
        ];

        clients
            .Where(file => LoopbackUrl().IsMatch(File.ReadAllText(file)))
            .Select(RepositoryTree.Relative)
            .ShouldBeEmpty($"The end-to-end suite and the browser smoke reach the stack on the host {_hostVariable} names, localhost or 127.0.0.1 without it; a URL that names the loopback misses a daemon elsewhere.");

        if (clients.Length > 0)
        {
            clients.Where(file => File.ReadAllText(file).Contains(_hostVariable, StringComparison.Ordinal)).ShouldNotBeEmpty(
                $"No client of the stack reads {_hostVariable}, so a pipeline whose daemon runs elsewhere cannot point it at the stack.");
        }
    }

    [Fact]
    public void TheReaders_AreSensitive()
    {
        Mappings(["services:", "  api:", "    ports:", "      - \"127.0.0.1:8080:8080\"", "      - 9000:9000   # bare", "    environment:", "      - KEY=value"])
            .ShouldBe(["127.0.0.1:8080:8080", "9000:9000"], "a list item after ports: ends is not a port");
        Mappings(["    ports: []"]).ShouldBeEmpty();
        LoopbackUrl().IsMatch("new($\"http://localhost:{port}/\")").ShouldBeTrue();
        LoopbackUrl().IsMatch("`http://127.0.0.1:${port}`").ShouldBeTrue();
        LoopbackUrl().IsMatch("`http://${host}:${port}`").ShouldBeFalse();
    }

    private static IEnumerable<string> ComposeFiles()
    {
        string root = ProjectReferenceGraph.RepositoryRoot;
        var atRoot = Directory.EnumerateFiles(root, "docker-compose*.yml");
        string apps = Path.Combine(root, "src", "Web", "apps");
        var inApps = Directory.Exists(apps) ? RepositoryTree.SourceFiles(apps, "compose*.yml", _skippedFolders) : [];
        return atRoot.Concat(inApps).Order(StringComparer.Ordinal);
    }

    private static IEnumerable<string> Mappings(IEnumerable<string> lines)
    {
        int? ports = null;

        foreach (string line in lines.Where(line => line.Trim().Length > 0 && !line.TrimStart().StartsWith('#')))
        {
            int indent = line.Length - line.TrimStart().Length;
            if (ports is not null && indent <= ports)
            {
                ports = null;
            }

            if (PortsKey().Match(line) is { Success: true } key)
            {
                ports = key.Groups["indent"].Length;
            }
            else if (ports is not null)
            {
                var item = QuotedItem().Match(line);
                item = item.Success ? item : BareItem().Match(line);
                if (item.Success)
                {
                    yield return item.Groups["mapping"].Value;
                }
            }
        }
    }
}
