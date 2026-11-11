using CleanArch.Cli.Generators.AddUseCase;
using CleanArch.Cli.Generators.Shared;
using CleanArch.Tooling.Markdown;
using Shouldly;
using Xunit;

namespace CleanArch.Cli.Tests.Generators;

public sealed class FeatureFlagsTests
{
    private const string _page = "docs/guides/changing-generated-code.md";

    [Fact]
    public void TheGuideToCodeTemplates_ListsEachFlagOfAddFeature_WithItsMeaning_AndNoOther() =>
        ListsEachFlag("add feature", FeatureFlags.Vocabulary);

    [Fact]
    public void TheGuideToCodeTemplates_ListsEachFlagOfAddUseCase_WithItsMeaning_AndNoOther() =>
        ListsEachFlag("add use-case", UseCaseFlags.Vocabulary);

    private static void ListsEachFlag(string generator, FlagSet flags)
    {
        string heading = $"### The flags of {generator}";
        string section = MarkdownSection.Of(Path.Combine(Repository.Root, _page), heading)
            .ShouldNotBeNull($"{_page} has no section '{heading}', where the flags of {generator} are listed.");
        string[] listed = [.. section.ReplaceLineEndings("\n").Split('\n').Where(line => line.StartsWith("| `", StringComparison.Ordinal))];
        string[] vocabulary = [.. flags.Meanings.Select(flag => $"| `{flag.Key}` | {flag.Value} |")];

        listed.ShouldNotBeEmpty($"The section '{heading}' of {_page} holds no row, so this comparison reads nothing.");

        string[] offenders =
        [
            .. vocabulary.Except(listed, StringComparer.Ordinal).Select(row => $"missing from the page, or listed with another meaning: {row}"),
            .. listed.Except(vocabulary, StringComparer.Ordinal).Select(row => $"listed, but not a flag of {generator} with this meaning: {row}"),
        ];

        offenders.ShouldBeEmpty(
            $"The table under '{heading}' in {_page} is the vocabulary of {flags.Generator}, row for row: "
            + "a flag renamed, added, removed or reworded changes both.");
    }
}
