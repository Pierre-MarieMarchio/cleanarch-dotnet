using System.Text.Json;
using System.Text.Json.Nodes;

namespace CleanArch.Cli.UpgradeReport;

internal sealed record Provenance(
    string Package,
    string Version,
    string Name,
    IReadOnlyList<(string Key, string Value)> Options,
    WebWorkspace? Web,
    IReadOnlyList<PresentationProject> Presentation,
    string TargetFramework)
{
    public static Provenance Of(LocatedProject project)
    {
        string path = Path.Combine(project.Root, ProjectLocator.FileName);
        JsonObject? options = JsonNode.Parse(File.ReadAllText(path))?["options"] as JsonObject;

        return new Provenance(
            project.File.Template.Package,
            project.File.Template.Version,
            project.File.Name,
            [.. (options ?? []).Select(option => (option.Key, Value(option.Value)))],
            project.File.Web,
            project.File.Presentation ?? [],
            project.File.TargetFramework);
    }

    private static string Value(JsonNode? node) => node is null ? string.Empty : node.GetValueKind() switch
    {
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => node.ToString(),
    };
}
