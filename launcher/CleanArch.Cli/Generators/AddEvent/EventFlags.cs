using CleanArch.Cli.Generators.Shared;

namespace CleanArch.Cli.Generators.AddEvent;

internal static class EventFlags
{
    internal static FlagSet Vocabulary { get; } = new(
        "dotnet cleanarch add event",
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["configured"] = "The feature of `--consumer` takes the configuration: its `Add<Features>` method has an `IConfiguration` parameter.",
        });

    internal static HashSet<string> Of(EventSpec spec) =>
        spec.ConsumerTakesConfiguration ? ["configured"] : [];
}
