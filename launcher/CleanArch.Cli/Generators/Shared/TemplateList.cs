namespace CleanArch.Cli.Generators.Shared;

internal sealed record TemplateList(
    string Name,
    IReadOnlyList<TemplateList.Item> Items,
    IReadOnlySet<string> Tokens,
    IReadOnlySet<string> Traits)
{
    internal static TemplateList Of<T>(
        string name,
        IEnumerable<T> items,
        IReadOnlyDictionary<string, Func<T, string>> tokens,
        IReadOnlyDictionary<string, Func<T, bool>> traits) =>
        new(
            name,
            [.. items.Select(item => new Item(token => tokens[token](item), trait => traits[trait](item)))],
            tokens.Keys.ToHashSet(StringComparer.Ordinal),
            traits.Keys.ToHashSet(StringComparer.Ordinal));

    internal sealed record Item(Func<string, string> Token, Func<string, bool> Has);
}
