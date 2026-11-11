using CleanArch.Cli.Generators.Shared;

namespace CleanArch.Cli.Generators.AddSetting;

internal static class SettingFlags
{
    internal static FlagSet Vocabulary { get; } = new(
        "dotnet cleanarch add setting",
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["configured"] = "The feature's `Add<Features>` method takes the configuration, so the tests pass it the one they build.",
            ["required"] = "A key is required: a `secret`, or a `string` without a default, so a valid section gives it a value.",
        });

    internal static HashSet<string> Of(SettingSpec spec)
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
}
