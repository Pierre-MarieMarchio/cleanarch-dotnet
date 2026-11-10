using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CleanArch.Cli.Generators.Shared;

internal static class TemplateCopies
{
    internal const string Manifest = $"{ToolTemplates.Folder}/manifest.json";

    private const int _schemaVersion = 1;

    private static readonly JsonSerializerOptions _written = new() { WriteIndented = true, NewLine = "\n" };

    internal sealed record Copy(string Path, string Version, string Sha256);

    internal static Dictionary<string, Copy>? Read(string root, out string? problem)
    {
        string full = Path.Combine(root, Manifest);

        problem = null;

        if (!File.Exists(full))
        {
            return new Dictionary<string, Copy>(StringComparer.Ordinal);
        }

        try
        {
            if (CopiesIn(JsonNode.Parse(File.ReadAllBytes(full))) is { } copies)
            {
                return copies;
            }

            problem = $"{Manifest} does not read as the record of the templates copied: it needs schemaVersion {_schemaVersion} and one path, version and sha256 for each template";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            problem = $"{Manifest} cannot be read ({exception.Message})";
        }

        return null;
    }

    internal static byte[] Written(IReadOnlyDictionary<string, Copy> copies)
    {
        var top = new JsonObject
        {
            ["schemaVersion"] = _schemaVersion,
            ["templates"] = new JsonArray([.. copies.Values.OrderBy(copy => copy.Path, StringComparer.Ordinal).Select(copy => (JsonNode)new JsonObject
            {
                ["path"] = copy.Path,
                ["version"] = copy.Version,
                ["sha256"] = copy.Sha256,
            })]),
        };

        return Encoding.UTF8.GetBytes(top.ToJsonString(_written) + "\n");
    }

    private static Dictionary<string, Copy>? CopiesIn(JsonNode? top)
    {
        if (top is not JsonObject { } manifest
            || manifest["schemaVersion"] is not JsonValue version
            || !version.TryGetValue(out int schema)
            || schema != _schemaVersion
            || manifest["templates"] is not JsonArray templates)
        {
            return null;
        }

        Copy?[] copies = [.. templates.Select(CopyOf)];

        return copies.Contains(null) || copies.DistinctBy(copy => copy!.Path, StringComparer.Ordinal).Count() < copies.Length
            ? null
            : copies.ToDictionary(copy => copy!.Path, copy => copy!, StringComparer.Ordinal);
    }

    private static Copy? CopyOf(JsonNode? entry) =>
        entry is JsonObject template && Text(template["path"]) is { } path && Text(template["version"]) is { } version && Text(template["sha256"]) is { } sha256
            ? new Copy(path, version, sha256)
            : null;

    private static string? Text(JsonNode? node) => node is JsonValue value && value.TryGetValue(out string? text) ? text : null;
}
