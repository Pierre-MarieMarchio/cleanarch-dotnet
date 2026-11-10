using CleanArch.Cli.Generators.Shared;
using CleanArch.Tooling.Markdown;
using Shouldly;
using Xunit;

namespace CleanArch.Cli.Tests.Generators;

public sealed class FeatureFlagsTests
{
    private const string _page = "docs/guides/changing-generated-code.md";

    private const string _section = "### The flags of add feature";

    [Fact]
    public void TheGuideToCodeTemplates_ListsEachFlagOfAddFeature_WithItsMeaning_AndNoOther()
    {
        string section = MarkdownSection.Of(Path.Combine(Repository.Root, _page), _section)
            .ShouldNotBeNull($"{_page} has no section '{_section}', where the flags of add feature are listed.");
        string[] listed = [.. section.ReplaceLineEndings("\n").Split('\n').Where(line => line.StartsWith("| `", StringComparison.Ordinal))];
        string[] vocabulary = [.. FeatureFlags.Vocabulary.Meanings.Select(flag => $"| `{flag.Key}` | {flag.Value} |")];

        listed.ShouldNotBeEmpty($"The section '{_section}' of {_page} holds no row, so this comparison reads nothing.");

        string[] offenders =
        [
            .. vocabulary.Except(listed, StringComparer.Ordinal).Select(row => $"missing from the page, or listed with another meaning: {row}"),
            .. listed.Except(vocabulary, StringComparer.Ordinal).Select(row => $"listed, but not a flag of add feature with this meaning: {row}"),
        ];

        offenders.ShouldBeEmpty(
            $"The table under '{_section}' in {_page} is the vocabulary of FeatureFlags, row for row: "
            + "a flag renamed, added, removed or reworded changes both.");
    }
}
