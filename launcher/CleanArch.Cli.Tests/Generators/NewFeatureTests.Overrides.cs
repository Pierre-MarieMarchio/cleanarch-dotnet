using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using CleanArch.Cli.Generators.Shared;
using Xunit;
using static CleanArch.Cli.Generators.Shared.FeatureCode;

namespace CleanArch.Cli.Tests.Generators;

public sealed partial class NewFeatureTests
{
    private const string _controller = "src/Presentation/AT.Api/Features/Placeholders/Controllers/PlaceholdersController.cs";

    private const string _record = "src/Infrastructure/AT.Infrastructure.Persistence/Features/Placeholders/Rows/Models/PlaceholderRecord.cs";

    private const string _overriddenMarker = "// written from this project's code template";

    private static readonly string[] _overriddenFeature =
        ["Things", "Thing", "--access", "owner", "--deletion", "delete", "--model", "aggregate", "--concurrency", "etag", "--fields", _existingFields];

    [Fact]
    public void OverrideWinsForItsFileOnly() => Expectations.Hold(OverrideWinsForItsFileOnlyCases);

    [Fact]
    public void AnUnchangedCopyWritesTheSameBytes() => Expectations.Hold(AnUnchangedCopyWritesTheSameBytesCases);

    [Fact]
    public void AnOverrideNamingNoTemplateIsRefused() => Expectations.Hold(AnOverrideNamingNoTemplateIsRefusedCases);

    [Fact]
    public void ABrokenOverrideIsRefusedNamingIt() => Expectations.Hold(ABrokenOverrideIsRefusedNamingItCases);

    [Fact]
    public void TheManifestRecordsEachOverride() => Expectations.Hold(TheManifestRecordsEachOverrideCases);

    [Fact]
    public void DotFilesAreIgnored() => Expectations.Hold(DotFilesAreIgnoredCases);

    [Fact]
    public void AddFieldUsesTheOverrideForBothRenders() => Expectations.Hold(AddFieldUsesTheOverrideForBothRendersCases);

    [Fact]
    public void AddFieldWarnsWhenTheOverrideChangedSinceAddFeature() => Expectations.Hold(AddFieldWarnsWhenTheOverrideChangedSinceAddFeatureCases);

    private static string ToolText(string reference) => ToolTemplates.Of(TemplateFolder)[reference];

    private static string OverridePath(string reference) => ToolTemplates.PathOf(TemplateFolder, reference);

    private static string Marked(string reference)
    {
        List<string> lines = [.. ToolText(reference).Split('\n')];

        lines.Insert(lines.FindIndex(line => line.StartsWith("namespace ", StringComparison.Ordinal)) + 1, _overriddenMarker);

        return string.Join('\n', lines) + "\n";
    }

    private static Dictionary<string, string> WrittenFiles(string root) =>
        Snapshot(root)
            .Where(pair => !pair.Key.StartsWith(FeatureManifest.Folder, StringComparison.Ordinal)
                && !pair.Key.StartsWith(ToolTemplates.Folder, StringComparison.Ordinal))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

    private static JsonNode ManifestOfThings(string root) =>
        JsonNode.Parse(File.ReadAllText(System.IO.Path.Combine(root, FeatureManifest.PathOf("Things"))))!;

    private static void OverrideWinsForItsFileOnlyCases(string scratch, Action<bool, string> expect)
    {
        string plain = FieldTree(scratch);
        string overridden = FieldTree(scratch);

        Put(overridden, OverridePath(_controller), Marked(_controller));

        var expected = Invoke(plain, _overriddenFeature);
        var result = Invoke(overridden, _overriddenFeature);
        string controller = "src/Presentation/A.Api/Features/Things/Controllers/ThingsController.cs";
        var want = WrittenFiles(plain);
        var got = WrittenFiles(overridden);

        expect(expected.Code == 0 && result.Code == 0, $"add feature refused a run the override test compares: {expected.Error}{result.Error}");
        expect(
            File.ReadAllText(System.IO.Path.Combine(overridden, controller)).Contains(_overriddenMarker, StringComparison.Ordinal),
            "The controller was not written from the project's override of its template.");
        expect(
            Differing(want, got) == controller,
            $"The override of the controller's template changed another file than the controller, or none: {Differing(want, got)}");
        expect(
            result.Output.Contains($"Written from this project's code templates:\n  {OverridePath(_controller)}\n", StringComparison.Ordinal)
                && !expected.Output.Contains("code templates", StringComparison.Ordinal),
            "The report does not list the override it wrote from, or lists one in a run that used none.");
    }

    private static void AnUnchangedCopyWritesTheSameBytesCases(string scratch, Action<bool, string> expect)
    {
        string plain = NewTree(scratch, reaction: false);
        string copied = NewTree(scratch, reaction: false);

        foreach (var (reference, text) in ToolTemplates.Of(TemplateFolder))
        {
            Put(copied, OverridePath(reference), text + "\n");
        }

        Put(copied, OverridePath(_controller), "﻿" + (ToolText(_controller) + "\n").ReplaceLineEndings("\r\n"));

        var expected = Invoke(plain, _overriddenFeature);
        var result = Invoke(copied, _overriddenFeature);
        var recorded = ManifestOfThings(copied)["templates"] as JsonArray ?? [];

        expect(expected.Code == 0 && result.Code == 0, $"add feature refused a run the copy test compares: {expected.Error}{result.Error}");
        expect(
            Same(WrittenFiles(plain), WrittenFiles(copied)),
            $"A copy of each template, unchanged, did not write the bytes the tool's own text writes: {Differing(WrittenFiles(plain), WrittenFiles(copied))}");
        expect(
            recorded.Count > 0
                && recorded.All(entry => entry!["sha256"]!.GetValue<string>() == ToolTemplates.Digest(ToolText(ReferenceOf(entry["path"]!.GetValue<string>())))),
            "The manifest records an unchanged copy, a BOM and CRLF line endings among them, with another digest than the tool's text.");

        static string ReferenceOf(string path) => path[$"{ToolTemplates.Folder}/{TemplateFolder}/".Length..^ToolTemplates.Extension.Length];
    }

    private static void AnOverrideNamingNoTemplateIsRefusedCases(string scratch, Action<bool, string> expect)
    {
        string root = FieldTree(scratch);
        string stray = $"{ToolTemplates.Folder}/{TemplateFolder}/src/Presentation/AT.Api/Features/Placeholders/Controllers/PlaceholderController.cs.txt";

        Put(root, stray, "namespace N;\n");

        var before = Snapshot(root);
        var result = Invoke(root, _overriddenFeature);

        expect(result.Code == 1, $"add feature wrote a feature beside a template that names no file it writes: {result.Output}");
        expect(
            result.Error.Contains(
                $"error: {stray}: names no template of add feature; dotnet cleanarch templates copy add-feature --dry-run lists them.",
                StringComparison.Ordinal),
            $"The refusal does not name the stray template and where the templates are listed: {result.Error}");
        expect(Same(before, Snapshot(root)), "A refusal over a stray template changed the tree.");
    }

    private static void ABrokenOverrideIsRefusedNamingItCases(string scratch, Action<bool, string> expect)
    {
        int namespaceLine = Array.FindIndex(ToolText(_controller).Split('\n'), line => line.StartsWith("namespace ", StringComparison.Ordinal)) + 1;

        (string Label, string Reference, string Text, string Message)[] broken =
        [
            (
                "an unknown flag",
                _controller,
                ToolText(_controller).Replace("\nnamespace ", "\n#if nonsense\n#endif\nnamespace ", StringComparison.Ordinal),
                $"line {namespaceLine}: 'nonsense' is not a flag of dotnet cleanarch add feature"),
            ("an unknown field token", _record, ToolText(_record).Replace("%field.RecordProperty%", "%field.Colour%", StringComparison.Ordinal), "'%field.Colour%' is not a token of a field"),
            ("an unknown token", _controller, ToolText(_controller) + "\n// %Limts%\n", "'%Limts%' is a token this generation does not set"),
            ("an #if never closed", _controller, ToolText(_controller) + "\n#if list\n", "is never closed by an #endif"),
        ];

        foreach (var (label, reference, text, message) in broken)
        {
            string root = FieldTree(scratch);

            Put(root, OverridePath(reference), text);

            var before = Snapshot(root);
            var result = Invoke(root, _overriddenFeature);

            expect(result.Code == 1, $"[{label}] add feature wrote a feature from a broken override: {result.Output}");
            expect(
                result.Error.Contains($"error: {OverridePath(reference)}: ", StringComparison.Ordinal) && result.Error.Contains(message, StringComparison.Ordinal),
                $"[{label}] the refusal does not name the override and say '{message}': {result.Error}");
            expect(Same(before, Snapshot(root)), $"[{label}] a refusal over a broken override changed the tree.");
        }

    }

    private static void TheManifestRecordsEachOverrideCases(string scratch, Action<bool, string> expect)
    {
        string root = FieldTree(scratch);
        string plain = FieldTree(scratch);

        Put(root, OverridePath(_record), Marked(_record));
        Put(root, OverridePath(_controller), Marked(_controller));

        var result = Invoke(root, _overriddenFeature);
        var plainResult = Invoke(plain, _overriddenFeature);
        string recorded = ManifestOfThings(root)["templates"]?.ToJsonString() ?? "none";
        string expected = new JsonArray(
            [.. new[] { _controller, _record }
                .Select(reference => (JsonNode)new JsonObject { ["path"] = OverridePath(reference), ["sha256"] = ToolTemplates.Digest(Marked(reference)) })
                .OrderBy(entry => entry["path"]!.GetValue<string>(), StringComparer.Ordinal)]).ToJsonString();

        expect(result.Code == 0 && plainResult.Code == 0, $"add feature refused a run the manifest test reads: {result.Error}{plainResult.Error}");
        expect(recorded == expected, $"The manifest does not record each override used, by path and digest, in path order: {recorded}");
        expect(ManifestOfThings(plain)["templates"] is null, "The manifest of a run that used no override records a templates key.");
    }

    private static void DotFilesAreIgnoredCases(string scratch, Action<bool, string> expect)
    {
        string plain = FieldTree(scratch);
        string dotted = FieldTree(scratch);

        Put(dotted, $"{ToolTemplates.Folder}/{TemplateFolder}/src/.notes.txt", "Not a template.\n");

        var expected = Invoke(plain, _overriddenFeature);
        var result = Invoke(dotted, _overriddenFeature);

        expect(expected.Code == 0, $"add feature refused the run the dot-file test compares: {expected.Error}");
        expect(result.Code == 0, $"add feature read a dot file in code-templates as a template: {result.Error}");
        expect(Same(WrittenFiles(plain), WrittenFiles(dotted)), $"A dot file in code-templates changed what add feature wrote: {Differing(WrittenFiles(plain), WrittenFiles(dotted))}");
    }

    private const string _fieldMarker = "    // a field this project's template marks";

    private static string MarkingEachField() =>
        ToolText(_record).Replace("%field.RecordProperty%", $"{_fieldMarker}\n%field.RecordProperty%", StringComparison.Ordinal) + "\n";

    private static void AddFieldUsesTheOverrideForBothRendersCases(string scratch, Action<bool, string> expect)
    {
        string[] options = [.. _fieldShapes[0].Options];
        string widened = FieldTree(scratch);
        string root = FieldTree(scratch);

        Put(widened, OverridePath(_record), MarkingEachField());
        Put(root, OverridePath(_record), MarkingEachField());

        var expected = Invoke(widened, ["Things", "Thing", .. options, "--fields", $"{_existingFields}, Notes:text?"]);
        var created = Invoke(root, ["Things", "Thing", .. options, "--fields", _existingFields]);
        var added = InvokeField(root, Migrating(root, [], "Things"), "Things", "Notes:text?");
        string record = File.ReadAllText(System.IO.Path.Combine(root, "src/Infrastructure/A.Infrastructure.Persistence/Features/Things/Rows/Models/ThingRecord.cs"));

        expect(expected.Code == 0 && created.Code == 0, $"add feature refused a run the add field override test compares: {expected.Error}{created.Error}");
        expect(added.Code == 0, $"add field refused a field over a feature written from an override: {added.Error}");
        expect(
            Regex.Count(record, Regex.Escape(_fieldMarker)) == 3,
            $"add field did not write the added field through the override's block, beside the two fields add feature wrote: {record}");
        expect(
            Same(FeatureFiles(widened), FeatureFiles(root)),
            $"add field did not leave each file as add feature writes it from the same override with the field added: {Differing(FeatureFiles(widened), FeatureFiles(root))}");
        expect(!added.Error.Contains("warning:", StringComparison.Ordinal), $"add field warned over an override that has not changed: {added.Error}");
        expect(
            ManifestOfThings(root)["templates"]?.ToJsonString() == ManifestOfThings(widened)["templates"]?.ToJsonString(),
            "add field did not record the override it wrote from as add feature records it.");
    }

    private static void AddFieldWarnsWhenTheOverrideChangedSinceAddFeatureCases(string scratch, Action<bool, string> expect)
    {
        string root = FieldTree(scratch);

        Put(root, OverridePath(_record), MarkingEachField());

        var created = Invoke(root, ["Things", "Thing", .. _fieldShapes[0].Options, "--fields", _existingFields]);
        string changed = MarkingEachField() + "#if list\n#endif\n";

        Put(root, OverridePath(_record), changed);

        var added = InvokeField(root, Migrating(root, [], "Things"), "Things", "Notes:text?");
        var recorded = ManifestOfThings(root)["templates"]!.AsArray().Single()!;

        expect(created.Code == 0, $"add feature refused the run the warning test reads: {created.Error}");
        expect(added.Code == 0, $"add field refused, or failed, over an override changed since add feature: {added.Error}");
        expect(
            added.Error.Contains(
                $"warning: {OverridePath(_record)} is not the text {FeatureManifest.PathOf("Things")} records the feature was written from; add field compares "
                    + "the files with what it writes today, so a member the difference reaches reads as changed by hand.\n",
                StringComparison.Ordinal),
            $"add field did not warn that the override changed since add feature wrote the feature: {added.Error}");
        expect(
            recorded["sha256"]!.GetValue<string>() == ToolTemplates.Digest(changed),
            "add field did not record the override's text it wrote from, once written.");
    }
}
