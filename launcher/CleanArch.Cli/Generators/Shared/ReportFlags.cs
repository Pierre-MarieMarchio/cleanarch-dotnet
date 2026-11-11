namespace CleanArch.Cli.Generators.Shared;

internal static class ReportFlags
{
    internal static FlagSet Modes { get; } = new(
        "every report",
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["applied"] = "The run wrote its files and made its edits.",
            ["dryrun"] = "`--dry-run`: the run wrote nothing, and says what it would write.",
            ["refused"] = "An edit found no line to stand beside, so the run wrote nothing, and says what to make by hand.",
        });

    internal static FlagSet For(FlagSet generator, FlagSet shapes)
    {
        var meanings = new Dictionary<string, string>(generator.Meanings, StringComparer.Ordinal);

        foreach (var flag in Modes.Meanings.Concat(shapes.Meanings))
        {
            meanings.Add(flag.Key, flag.Value);
        }

        return new FlagSet(generator.Generator, meanings);
    }

    internal static string Of(ReportMode mode) => mode switch
    {
        ReportMode.Applied => "applied",
        ReportMode.DryRun => "dryrun",
        _ => "refused",
    };
}
