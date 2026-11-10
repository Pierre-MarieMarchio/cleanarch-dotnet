namespace CleanArch.Cli.Generators.Shared;

internal sealed record FlagSet(string Generator, IReadOnlyDictionary<string, string> Meanings)
{
    internal bool Knows(string flag) => Meanings.ContainsKey(flag);
}
