namespace CleanArch.Cli.Generators.Shared;

internal static class ToolTemplates
{
    internal const string Folder = "code-templates";

    internal const string AddFeature = "add-feature";

    private const string _extension = ".txt";

    private static readonly Lazy<Dictionary<string, Dictionary<string, string>>> _generators = new(Load);

    internal static IReadOnlyDictionary<string, string> Of(string generator) => _generators.Value[generator];

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

        return Normalise(reader.ReadToEnd());
    }

    private static string Normalise(string text)
    {
        string lines = text.ReplaceLineEndings("\n");

        return lines.EndsWith('\n') ? lines[..^1] : lines;
    }
}
