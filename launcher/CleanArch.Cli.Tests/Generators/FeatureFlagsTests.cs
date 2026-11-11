using CleanArch.Cli.Generators.AddEvent;
using CleanArch.Cli.Generators.AddField;
using CleanArch.Cli.Generators.AddSetting;
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

    [Fact]
    public void TheGuideToCodeTemplates_ListsEachFlagOfAddEvent_WithItsMeaning_AndNoOther() =>
        ListsEachFlag("add event", EventFlags.Vocabulary);

    [Fact]
    public void TheGuideToCodeTemplates_ListsEachFlagOfAddSetting_WithItsMeaning_AndNoOther() =>
        ListsEachFlag("add setting", SettingFlags.Vocabulary);

    [Fact]
    public void TheGuideToCodeTemplates_ListsEachFlagOfEveryReport_WithItsMeaning_AndNoOther() =>
        ListsEachFlag("every report", ReportFlags.Modes);

    [Theory]
    [MemberData(nameof(Reports))]
    public void TheGuideToCodeTemplates_ListsTheFlagsAndTokensOfEachReport_AndNoOther(string generator)
    {
        var (spoken, shapes) = _reports[generator];
        string heading = $"### The report of {spoken}";
        string section = Section(heading, $"the flags and tokens of the report of {spoken}");
        string[] named = [.. Template.TokensOf(ToolTemplates.Of(generator)[ToolTemplates.Report]).Order(StringComparer.Ordinal)];

        string[] listed = [.. Template.TokensOf(section).Order(StringComparer.Ordinal)];

        Compare(heading, Rows(section), [.. shapes.Meanings.Select(Row)], $"the report flags of {spoken}");
        listed.ShouldBe(
            named,
            $"The section '{heading}' of {_page} lists the tokens of {ToolTemplates.PathOf(generator, ToolTemplates.Report)}, each and no other.");
    }

    public static TheoryData<string> Reports => [.. _reports.Keys];

    private static readonly Dictionary<string, (string Spoken, FlagSet Shapes)> _reports = new(StringComparer.Ordinal)
    {
        ["add-feature"] = ("add feature", FeatureFlags.ReportShapes),
        ["add-field"] = ("add field", FieldReport.Shapes),
        ["add-use-case"] = ("add use-case", UseCaseReport.Shapes),
        ["add-event"] = ("add event", EventReport.Shapes),
        ["add-setting"] = ("add setting", SettingReport.Shapes),
    };

    private static string Row(KeyValuePair<string, string> flag) => $"| `{flag.Key}` | {flag.Value} |";

    private static string Section(string heading, string listed) =>
        MarkdownSection.Of(Path.Combine(Repository.Root, _page), heading)
            .ShouldNotBeNull($"{_page} has no section '{heading}', where {listed} are listed.");

    private static string[] Rows(string section) =>
        [.. section.ReplaceLineEndings("\n").Split('\n').Where(line => line.StartsWith("| `", StringComparison.Ordinal))];

    private static void ListsEachFlag(string generator, FlagSet flags)
    {
        string heading = $"### The flags of {generator}";
        string[] listed = Rows(Section(heading, $"the flags of {generator}"));

        listed.ShouldNotBeEmpty($"The section '{heading}' of {_page} holds no row, so this comparison reads nothing.");
        Compare(heading, listed, [.. flags.Meanings.Select(Row)], generator);
    }

    private static void Compare(string heading, string[] listed, string[] vocabulary, string generator)
    {
        string[] offenders =
        [
            .. vocabulary.Except(listed, StringComparer.Ordinal).Select(row => $"missing from the page, or listed with another meaning: {row}"),
            .. listed.Except(vocabulary, StringComparer.Ordinal).Select(row => $"listed, but not a flag of {generator} with this meaning: {row}"),
        ];

        offenders.ShouldBeEmpty(
            $"The table under '{heading}' in {_page} is the vocabulary of {generator}, row for row: "
            + "a flag renamed, added, removed or reworded changes both.");
    }
}
