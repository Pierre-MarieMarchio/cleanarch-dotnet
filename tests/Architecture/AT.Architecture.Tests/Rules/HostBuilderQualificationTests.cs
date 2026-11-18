using System.Text.RegularExpressions;
using AppTemplate.Architecture.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace AppTemplate.Architecture.Tests.Rules;

public sealed class HostBuilderQualificationTests
{
    private static readonly Regex _bareHostBuilder = new(
        @"(?<![.\w])Host\s*\.\s*Create\w*Builder\b",
        RegexOptions.None,
        TimeSpan.FromSeconds(5));

    [Fact]
    public void NoSourceFile_CallsTheHostBuildersThroughABareHost()
    {
        var sources = RepositoryTree.SourceFiles(
                ProjectReferenceGraph.RepositoryRoot, "*.cs", ["bin", "obj", "node_modules", ".git"])
            .ToList();

        sources.Count.ShouldBeGreaterThanOrEqualTo(
            100,
            "Far fewer C# files were found than this template ships, so this rule is reading the " +
            "wrong tree and passing for the wrong reason.");

        var offenders = sources
            .Where(file => Path.GetFileName(file) != $"{nameof(HostBuilderQualificationTests)}.cs")
            .Where(file => _bareHostBuilder.IsMatch(File.ReadAllText(file)))
            .Select(RepositoryTree.Relative)
            .Order(StringComparer.Ordinal)
            .ToList();

        offenders.ShouldBeEmpty(
            "A project named <Company>.Host has a namespace Host, which a bare Host resolves to " +
            "before the using directives are read, so the call does not compile. Write " +
            "Microsoft.Extensions.Hosting.Host.");
    }

    [Fact]
    public void TheHostBuilderRule_IsSensitive_AndSeesOnlyTheBareType()
    {
        _bareHostBuilder.IsMatch("var builder = Host.CreateApplicationBuilder(args);").ShouldBeTrue();
        _bareHostBuilder.IsMatch("var builder = Host.CreateEmptyApplicationBuilder(settings);").ShouldBeTrue();
        _bareHostBuilder.IsMatch("var builder = Host.CreateDefaultBuilder(args);").ShouldBeTrue();
        _bareHostBuilder.IsMatch("var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder(args);").ShouldBeFalse();
        _bareHostBuilder.IsMatch("var builder = WebHost.CreateDefaultBuilder(args);").ShouldBeFalse();
        _bareHostBuilder.IsMatch("var services = Host.Services;").ShouldBeFalse();
    }
}
