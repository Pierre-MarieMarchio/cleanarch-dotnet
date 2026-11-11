using CleanArch.Cli.Generators.Shared;

namespace CleanArch.Cli.Generators.AddUseCase;

internal static class UseCaseFlags
{
    internal static FlagSet Vocabulary { get; } = new(
        NewUseCase.Generator,
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["id"] = "`id` is in `--input`, as by default: the message carries the id of the feature's aggregate, first.",
            ["parameters"] = "The message carries a value: the id, an input, or both.",
        });

    internal static HashSet<string> Of(UseCaseSpec spec)
    {
        var flags = new HashSet<string>(StringComparer.Ordinal);

        if (spec.HasId)
        {
            flags.Add("id");
        }

        if (spec.Parameters.Count > 0)
        {
            flags.Add("parameters");
        }

        return flags;
    }
}
