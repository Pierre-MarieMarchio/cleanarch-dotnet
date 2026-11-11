using CleanArch.Tooling.Projects;

namespace CleanArch.Cli.Generators.Shared;

internal static class ProjectFolders
{
    internal const string Src = "src";

    internal const string Presentation = "src/Presentation";

    internal const string ReferenceProduct = "AppTemplate";

    internal static string FolderOf(string product, string part) => ProjectFacts.FolderOf(product, part);

    internal static string Initials(string product) => FolderOf(product, string.Empty)[..^1];

    internal static string SourceProject(string product, string layer) => $"{Src}/{layer}/{FolderOf(product, layer)}";

    internal static string PathOf(string prefix, string reference, Dictionary<string, string> tokens)
    {
        string[] segments = reference.Split('/', 4);
        string referenceFolder = FolderOf(ReferenceProduct, string.Empty);

        if (segments.Length < 4 || !segments[2].StartsWith(referenceFolder, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"{reference} names no file of a {referenceFolder}<Part> project, so the tool cannot tell where it goes.");
        }

        return $"{segments[0]}/{segments[1]}/{FolderOf(prefix, segments[2][referenceFolder.Length..])}/{Template.ReplaceReferenceNames(segments[3], tokens)}";
    }

    internal static string ProjectFolderOf(string path) => string.Join('/', path.Split('/')[..3]);

    internal static bool ProjectExists(string root, string path) => Directory.Exists(Full(root, ProjectFolderOf(path)));

    internal static string Full(string root, string relative) => Path.Combine([root, .. relative.Split('/')]);
}
