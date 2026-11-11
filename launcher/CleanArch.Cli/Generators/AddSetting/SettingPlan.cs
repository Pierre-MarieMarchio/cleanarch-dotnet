using CleanArch.Cli.Generators.Shared;
using static CleanArch.Cli.Generators.Shared.ProjectFolders;

namespace CleanArch.Cli.Generators.AddSetting;

internal static class SettingPlan
{
    internal const string TemplateFolder = "add-setting";

    internal const string OptionsTemplate = "src/Application/AT.Application/Features/Placeholders/Configuration/Options/PlaceholderLimitsOptions.cs";

    internal const string TestsTemplate = "tests/Application/AT.Application.UnitTests/Features/Placeholders/Configuration/PlaceholderLimitsOptionsTests.cs";

    internal static FlagSet Vocabulary { get; } = new(
        "dotnet cleanarch add setting",
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["configured"] = "The feature's `Add<Features>` method takes the configuration, so the tests pass it the one they build.",
            ["required"] = "A key is required: a `secret`, or a `string` without a default, so a valid section gives it a value.",
        });

    internal static string Words(string pascal) =>
        string.Concat(pascal.Select((character, index) => char.IsUpper(character) && index > 0 ? " " + character : character.ToString()));

    internal static TemplateOverrides Overrides(string root) => TemplateOverrides.Read(root, TemplateFolder, Vocabulary);

    internal static List<Emission> Emissions(SettingSpec spec, string root, TemplateOverrides templates)
    {
        var flags = Flags(spec);
        var tokens = Tokens(spec);
        var keys = KeyBlocks.For(spec);

        Emission Written(string reference) => new(PathOf(spec.Prefix, reference, tokens), templates.Render(reference, flags, tokens, keys));

        var emissions = new List<Emission> { Written(OptionsTemplate) };

        if (ProjectExists(root, PathOf(spec.Prefix, TestsTemplate, tokens)))
        {
            emissions.Add(Written(TestsTemplate));
        }

        return emissions;
    }

    internal static HashSet<string> Flags(SettingSpec spec)
    {
        var flags = new HashSet<string>(StringComparer.Ordinal);

        if (spec.TakesConfiguration)
        {
            flags.Add("configured");
        }

        if (spec.Keys.Any(key => key.IsRequired))
        {
            flags.Add("required");
        }

        return flags;
    }

    internal static Dictionary<string, string> Tokens(SettingSpec spec)
    {
        var broken = KeyBlocks.Broken(spec);

        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["P"] = spec.Prefix,
            ["F"] = spec.Features,
            ["Section"] = spec.Section,
            ["Broken"] = broken.Name,
            ["BrokenValue"] = KeyCode.BrokenValue(broken),
        };
    }
}
