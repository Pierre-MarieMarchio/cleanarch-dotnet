using System.Text.RegularExpressions;
using AppTemplate.Architecture.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace AppTemplate.Architecture.Tests.Rules;

public sealed class CodeTemplatesFolderTests
{
    private const string _folder = "code-templates";

    private static readonly (string File, string List)[] _walkers =
    [
        /*#if (templateMaintenance)*/
        ("tools/CheckComments.cs", "_skippedDirectoryNames"),
        ("tools/CheckDocPaths.cs", "_skippedDirectoryNames"),
        ("launcher/CleanArch.Cli.Commands/Format/FormatCommand.cs", "_skippedDirectories"),
        /*#endif*/
        ("tests/Architecture/AT.Architecture.Tests/Rules/DocumentationCitationTests.cs", "_skippedDirectories"),
    ];

    [Fact]
    public void EveryTreeWalker_SkipsTheCodeTemplatesFolder()
    {
        foreach (var (file, list) in _walkers)
        {
            string text = File.ReadAllText(Path.Combine(ProjectReferenceGraph.RepositoryRoot, file));
            var declaration = Regex.Match(text, $@"{list}\s*=\s*(?:new\()?\[(?<entries>[^\]]*)\]", RegexOptions.None, TimeSpan.FromSeconds(5));

            declaration.Success.ShouldBeTrue($"{file} no longer declares {list} as a list of folder names.");
            declaration.Groups["entries"].Value.ShouldContain($"\"{_folder}\"", Case.Sensitive, $"{file} walks into the {_folder} folder: add it to {list}.");
        }
    }
}
