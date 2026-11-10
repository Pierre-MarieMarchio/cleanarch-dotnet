namespace CleanArch.Cli.Generators.Shared;

internal sealed record TemplateList(
    string Name,
    IReadOnlyList<Field> Items,
    IReadOnlyDictionary<string, Func<Field, string>> Tokens,
    IReadOnlyDictionary<string, Func<Field, bool>> Traits);
