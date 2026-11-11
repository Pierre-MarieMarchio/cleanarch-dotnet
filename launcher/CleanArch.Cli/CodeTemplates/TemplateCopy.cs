using System.Text;
using CleanArch.Cli.Generators.Shared;

namespace CleanArch.Cli.CodeTemplates;

internal sealed record TemplateCopyRequest(string Generator, IReadOnlyList<string> Templates, bool DryRun);

internal static class TemplateCopy
{
    internal static int Run(LocatedProject project, TemplateCopyRequest request, TextWriter output, TextWriter error)
    {
        if (Refusal(project, request, out Dictionary<string, TemplateCopies.Copy>? copies) is { } refusal)
        {
            error.WriteLine(refusal);
            return CleanArchCommand.Failed;
        }

        string[] selected = [.. Selected(request)];
        string[] kept = [.. selected.Where(reference => File.Exists(Path.Combine(project.Root, ToolTemplates.PathOf(request.Generator, reference))))];
        if (kept.Length == selected.Length)
        {
            error.WriteLine("Every template to copy exists in this project already, and none is ever overwritten: move one aside to copy it again.");
            error.WriteLine(Listed(request.Generator, kept));
            return CleanArchCommand.Failed;
        }

        string[] copied = [.. selected.Except(kept)];
        if (!request.DryRun)
        {
            Write(project.Root, request, copied, copies!);
        }

        Report(output, request, copied, kept);
        return CleanArchCommand.Succeeded;
    }

    private static string? Refusal(LocatedProject project, TemplateCopyRequest request, out Dictionary<string, TemplateCopies.Copy>? copies)
    {
        copies = null;
        if (!TemplatesCommand.Generators.TryGetValue(request.Generator, out TemplatedGenerator? generator))
        {
            return $"{request.Generator} has no code templates to copy; the generators that have some are {string.Join(", ", TemplatesCommand.Generators.Keys)}.";
        }

        if (generator.Refusal(project) is { } refused)
        {
            return $"{ToolTemplates.Spoken(request.Generator)} does not apply to this project, so there is nothing to copy: {refused}";
        }

        string[] unmatched = [.. request.Templates.Where(name => !ToolTemplates.Of(request.Generator).Keys.Any(reference => Names(name, reference)))];
        if (unmatched.Length > 0)
        {
            return string.Join('\n', unmatched.Select(name =>
                $"{name} names no template of {ToolTemplates.Spoken(request.Generator)}: {CleanArchCommand.Name} {TemplatesCommand.Name} copy {request.Generator} --dry-run lists them."));
        }

        copies = TemplateCopies.Read(project.Root, out string? problem);
        return problem is null
            ? null
            : $"{problem}. It records where each copy came from, so nothing is copied until it reads again: repair it, or move it aside.";
    }

    private static IEnumerable<string> Selected(TemplateCopyRequest request) =>
        ToolTemplates.Of(request.Generator).Keys
            .Where(reference => request.Templates.Count == 0 || request.Templates.Any(name => Names(name, reference)))
            .Order(StringComparer.Ordinal);

    private static bool Names(string name, string reference) =>
        reference == name || reference.EndsWith('/' + name, StringComparison.Ordinal);

    private static void Write(string root, TemplateCopyRequest request, string[] copied, Dictionary<string, TemplateCopies.Copy> copies)
    {
        List<(string Path, byte[] Bytes)> created = [];
        foreach (string reference in copied)
        {
            string path = ToolTemplates.PathOf(request.Generator, reference);
            string text = ToolTemplates.Of(request.Generator)[reference];
            created.Add((path, Encoding.UTF8.GetBytes(text + "\n")));
            copies[path] = new TemplateCopies.Copy(path, GeneratorCommand.Version, ToolTemplates.Digest(text));
        }

        string manifest = Path.Combine(root, TemplateCopies.Manifest);
        byte[] recorded = TemplateCopies.Written(copies);
        if (File.Exists(manifest))
        {
            TextEdits.Commit(root, created, [(TemplateCopies.Manifest, File.ReadAllBytes(manifest), recorded)]);
        }
        else
        {
            TextEdits.Commit(root, [.. created, (TemplateCopies.Manifest, recorded)], []);
        }
    }

    private static void Report(TextWriter output, TemplateCopyRequest request, string[] copied, string[] kept)
    {
        string templates = copied.Length == 1 ? "template" : "templates";
        output.WriteLine($"{(request.DryRun ? "Would write" : "Wrote")} {copied.Length} {templates} of {ToolTemplates.Spoken(request.Generator)} from version {GeneratorCommand.Version} of the tool, recorded in {TemplateCopies.Manifest}:");
        output.WriteLine(Listed(request.Generator, copied));
        if (kept.Length > 0)
        {
            output.WriteLine("Kept, as none is ever overwritten:");
            output.WriteLine(Listed(request.Generator, kept));
        }

        output.WriteLine();
        output.WriteLine(request.DryRun
            ? "--dry-run: nothing was written."
            : $"{TemplatesCommand.Generators[request.Generator].Writes} those files from them.");
    }

    private static string Listed(string generator, IEnumerable<string> references) =>
        string.Join('\n', references.Select(reference => $"  {ToolTemplates.PathOf(generator, reference)}"));
}
