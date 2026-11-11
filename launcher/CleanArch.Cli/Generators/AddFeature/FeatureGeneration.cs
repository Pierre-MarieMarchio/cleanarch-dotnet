using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using CleanArch.Cli.Generators.Shared;
using Microsoft.CodeAnalysis;
using static CleanArch.Cli.Generators.Shared.FeatureCode;

namespace CleanArch.Cli.Generators.AddFeature;

internal sealed class FeatureGeneration(FeatureSpec spec, IReadOnlyList<string> checkedProblems, GenerationContext context, string version) : Generation(context)
{
    private List<Emission> _plan = [];

    private List<string> _skipped = [];

    private Emission? _page;

    private string? _unwritten;

    private TextEdits.Editor _editor = new(context.Root);

    private Emission? _manifest;

    protected override string Usage => NewFeature.Usage;

    protected override bool StopsAtDryRun => true;

    protected override TemplateOverrides Templates { get; } = Overrides(context.Root);

    protected override IReadOnlyList<string> EditFailures => _editor.Failures;

    protected override IReadOnlyList<(string Path, byte[] Bytes)> Created =>
        [.. _plan.Select(Written), .. _page is null ? Array.Empty<(string, byte[])>() : [Written(_page)], Written(_manifest!)];

    protected override IReadOnlyList<(string Path, byte[] Original, byte[] Bytes)> Replaced => [.. TextEdits.Replacements(_editor)];

    protected override int? Concluded()
    {
        if (!AlreadyGenerated(Root, spec))
        {
            return null;
        }

        Output.WriteLine(
            $"{ManifestPath(spec)} records {spec.Features} with these options already: its files and edits are written, "
            + "so nothing is written again.");

        return Context.DryRun ? Succeeded : Finish(Root, spec, Output, Error, Context.Executor, resumed: true);
    }

    protected override bool Draft(List<string> problems)
    {
        _plan = Plan(spec, Root, Templates, out _skipped);
        problems.AddRange(checkedProblems);
        problems.AddRange(Templates.Problems);
        problems.AddRange(NewFeature.Conflicts(Root, spec, _plan));

        if (problems.Count > 0)
        {
            return false;
        }

        _page = FeaturePage.PageOf(Root, spec, _plan, out _unwritten);
        _editor = FeatureEdits.Edits(Root, spec, _plan);
        _manifest = ManifestOf(spec, _plan, _page, _editor, version, Templates.Used);

        return true;
    }

    protected override int RefuseEdits(IReadOnlyList<string> failures) =>
        RefuseAfterReport(
            failures,
            "Nothing was written: every edit is made or none is. Put back the line each failed edit is placed beside, "
            + "or write the feature by hand from the edits above.");

    protected override string? Preconditions() => FeatureCode.Preconditions(Root, spec, Output, Context.Executor);

    protected override void Report(ReportMode mode) =>
        FeatureReport.Write(Output, Templates, new FeatureOutput(Root, spec, _plan, _manifest!.Path, _skipped, _page, _unwritten, _editor.Kept), mode);

    protected override int Finish() => Finish(Root, spec, Output, Error, Context.Executor, resumed: false);


    private static (string Path, byte[] Bytes) Written(Emission emission) => (emission.Path, emission.Bytes());

    private static bool AlreadyGenerated(string root, FeatureSpec spec)
    {
        if (FeatureManifest.Read(root, spec.Features, out _) is not { Options: { } options } manifest)
        {
            return false;
        }

        static string? Text(JsonNode? node) => node is JsonValue value && value.GetValueKind() == JsonValueKind.String ? value.GetValue<string>() : null;

        return manifest.SchemaVersion == FeatureManifest.EditsVersion
            && manifest.Type == spec.Type
            && Text(options["access"]) == spec.Access
            && Text(options["model"]) == spec.Model
            && Text(options["concurrency"]) == spec.Concurrency
            && (Text(options["deletion"]) ?? string.Empty) == spec.Deletion
            && options["operations"] is JsonArray operations && operations.Select(Text).SequenceEqual(spec.Operations)
            && manifest.Fields is { } fields
            && fields.Select(field => Text(field?["declaration"])).SequenceEqual(spec.Fields.Select(field => field.Declaration));
    }

    private static int Finish(string root, FeatureSpec spec, TextWriter output, TextWriter error, IExecutor executor, bool resumed)
    {
        string migrations = System.IO.Path.Combine(root, PersistenceProject(spec), "Migrations");
        bool migrated = resumed
            && Directory.Exists(migrations)
            && Directory.EnumerateFiles(migrations, $"*_Add{spec.Features}.cs").Any();

        string done = resumed
            ? "the feature's files and edits, written by an earlier run"
            : "the feature's files, its edits and its manifest, which are consistent with each other";

        return TextEdits.RunAll(Steps(root, spec, migrated), executor.Run, output, error, done) ? 0 : 1;
    }

    internal static List<TextEdits.Command> Steps(string root, FeatureSpec spec, bool migrated = false) =>
    [
        TextEdits.ToolCommand(root, "docs-index"),
        TextEdits.ToolCommand(root, "build"),
        .. migrated ? Array.Empty<TextEdits.Command>() : [TextEdits.FromLastBuild(root, "migrations", "add", $"Add{spec.Features}")],
        .. spec.HasApi ? TextEdits.ContractSteps(root, built: true) : [],
    ];

    internal static Emission ManifestOf(FeatureSpec spec, List<Emission> plan, Emission? page, TextEdits.Editor editor, string version, IReadOnlyList<UsedTemplate> templates)
    {
        var options = new JsonObject
        {
            ["access"] = spec.Access,
            ["operations"] = new JsonArray([.. spec.Operations.Select(operation => (JsonNode)JsonValue.Create(operation))]),
            ["model"] = spec.Model,
            ["concurrency"] = spec.Concurrency,
        };

        if (spec.Access == "owner")
        {
            options["deletion"] = spec.Deletion;
        }

        var manifest = FeatureManifest.Create(Generator, version, spec.Features, spec.Type, options, FieldsOf(spec.Fields), page?.Path);

        foreach (var emission in plan)
        {
            manifest.Record(emission.Path, emission.Bytes());
        }

        manifest.Record(editor);
        manifest.RecordTemplates(templates);

        return new Emission(manifest.Path, Encoding.UTF8.GetString(manifest.Bytes()));
    }
}
