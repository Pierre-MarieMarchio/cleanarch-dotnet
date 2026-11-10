using System.Text.RegularExpressions;
using AppTemplate.Architecture.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace AppTemplate.Architecture.Tests.Rules;

public sealed class CodeTemplatesFolderTests
{
    private const string _folder = "code-templates";

    private static readonly (string File, string List)[] _treeWalkers =
    [
        /*#if (templateMaintenance)*/
        ("tools/CheckComments.cs", "_skippedDirectoryNames"),
        ("tools/CheckDocPaths.cs", "_skippedDirectoryNames"),
        ("launcher/CleanArch.Cli.Commands/Format/FormatCommand.cs", "_skippedDirectories"),
        /*#endif*/
        ("tests/Architecture/AT.Architecture.Tests/Rules/DocumentationCitationTests.cs", "_skippedDirectories"),
        ("tests/Architecture/AT.Architecture.Tests/Rules/MarkdownEncodingTests.cs", "_skippedDirectories"),
        ("tests/Architecture/AT.Architecture.Tests/Rules/DotnetTestTargetTests.cs", "_skippedFolders"),
    ];

    [Fact]
    public void TheListedTreeWalkers_SkipTheCodeTemplatesFolder()
    {
        string[] walkingIn = [.. _treeWalkers.Where(walker => !Skipped(walker)).Select(walker => $"{walker.File} ({walker.List})")];

        walkingIn.ShouldBeEmpty(
            $"{_folder}/ holds the text of templates, not the project's files: add \"{_folder}\" to the list each walker named skips.");
    }

    private static bool Skipped((string File, string List) walker)
    {
        string text = File.ReadAllText(Path.Combine(ProjectReferenceGraph.RepositoryRoot, walker.File));
        var declaration = Regex.Match(text, $@"{walker.List}\s*=\s*(?:new\()?\[(?<entries>[^\]]*)\]", RegexOptions.None, TimeSpan.FromSeconds(5));

        declaration.Success.ShouldBeTrue($"{walker.File} no longer declares {walker.List} as a list of folder names.");

        return declaration.Groups["entries"].Value.Contains($"\"{_folder}\"", StringComparison.Ordinal);
    }
}
