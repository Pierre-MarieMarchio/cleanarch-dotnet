namespace CleanArch.Cli.Generators.Shared;

internal static class ToolTemplates
{
    internal const string Folder = "code-templates";

    private const string _extension = ".txt";

    private static readonly Lazy<Dictionary<string, Dictionary<string, string>>> _generators = new(Load);

    internal static IReadOnlyDictionary<string, string> Of(string generator) =>
        _generators.Value.TryGetValue(generator, out var templates)
            ? templates
            : throw new InvalidOperationException($"The tool embeds no template under {Folder}/{generator}/.");

    internal static string PathOf(string generator, string reference) => $"{Folder}/{generator}/{reference}{_extension}";

    internal static string Normalize(string text)
    {
        string lines = text.TrimStart('﻿').ReplaceLineEndings("\n");

        return lines.EndsWith('\n') ? lines[..^1] : lines;
    }

    private static Dictionary<string, Dictionary<string, string>> Load()
    {
        var assembly = typeof(ToolTemplates).Assembly;

        return assembly.GetManifestResourceNames()
            .Select(resource => (Resource: resource, Segments: resource.Replace('\\', '/').Split('/', 3)))
            .Where(entry => entry.Segments is [Folder, _, var file] && file.EndsWith(_extension, StringComparison.Ordinal))
            .GroupBy(entry => entry.Segments[1], StringComparer.Ordinal)
            .ToDictionary(
                generator => generator.Key,
                generator => generator.ToDictionary(
                    entry => entry.Segments[2][..^_extension.Length],
                    entry => Text(assembly, entry.Resource),
                    StringComparer.Ordinal),
                StringComparer.Ordinal);
    }

    private static string Text(System.Reflection.Assembly assembly, string resource)
    {
        using var reader = new StreamReader(assembly.GetManifestResourceStream(resource)!);

        return Normalize(reader.ReadToEnd());
    }
}
