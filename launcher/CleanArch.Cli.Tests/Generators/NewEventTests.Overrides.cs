using System.Text.Json.Nodes;
using CleanArch.Cli.CodeTemplates;
using CleanArch.Cli.Generators.AddEvent;
using CleanArch.Cli.Generators.Shared;
using Xunit;

namespace CleanArch.Cli.Tests.Generators;

public sealed partial class NewEventTests
{
    private const string _consumerReference =
        "src/Application/AT.Application/Features/Listeners/Services/Consumers/PlaceholderNoted/NotifyListenersOnPlaceholderNotedConsumer.cs";

    private const string _eventTestsReference =
        "tests/Domain/AT.Domain.UnitTests/Features/Placeholders/Model/Events/PlaceholderNotedDomainEventTests.cs";

    private const string _writtenConsumer = "src/Application/A.Application/Features/Reminders/Services/Consumers/InvoicePaid/CancelRemindersOnInvoicePaidConsumer.cs";

    private const string _overriddenMarker = "// written from this project's code template";

    private static readonly string[] _consumedEvent = ["Invoices", "InvoicePaidDomainEvent", "--consumer", "Reminders:CancelReminders", "--fields", "InvoiceId:Guid, Reason:string"];

    [Fact]
    public void OverrideWinsForItsFileOnly() => Expectations.Hold(OverrideWinsForItsFileOnlyCases);

    [Fact]
    public void ABrokenOverrideIsRefusedNamingIt() => Expectations.Hold(ABrokenOverrideIsRefusedNamingItCases);

    [Fact]
    public void ReadsOnlyItsOwnFolder() => Expectations.Hold(ReadsOnlyItsOwnFolderCases);

    [Fact]
    public void LeavesTheRecordAddFieldReads() => Expectations.Hold(LeavesTheRecordAddFieldReadsCases);

    [Fact]
    public void AnUnchangedCopyWritesTheSameFiles() => Expectations.Hold(AnUnchangedCopyWritesTheSameFilesCases);

    [Fact]
    public void AGeneratorWarnsWhenTheToolChangedACopiedTemplate() => Expectations.Hold(AGeneratorWarnsWhenTheToolChangedACopiedTemplateCases);

    private static string ToolText(string reference) => ToolTemplates.Of(EventPlan.TemplateFolder)[reference];

    private static string OverridePath(string reference) => ToolTemplates.PathOf(EventPlan.TemplateFolder, reference);

    private static string Marked(string reference)
    {
        List<string> lines = [.. ToolText(reference).Split('\n')];

        lines.Insert(lines.FindIndex(line => line.StartsWith("namespace ", StringComparison.Ordinal)) + 1, _overriddenMarker);

        return string.Join('\n', lines) + "\n";
    }

    private static void Put(string root, string relative, string content)
    {
        string full = Path.Combine(root, relative);

        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, content);
    }

    private static Dictionary<string, string> WrittenFiles(string root) =>
        Snapshot(root)
            .Where(pair => !pair.Key.StartsWith(FeatureManifest.Folder, StringComparison.Ordinal) && !pair.Key.StartsWith(ToolTemplates.Folder, StringComparison.Ordinal))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

    private static string Differing(Dictionary<string, string> want, Dictionary<string, string> got) =>
        string.Join(", ", want.Keys.Union(got.Keys).Where(path => !want.TryGetValue(path, out string? was) || !got.TryGetValue(path, out string? now) || was != now).Order(StringComparer.Ordinal));

    private static void OverrideWinsForItsFileOnlyCases(string scratch, Action<bool, string> expect)
    {
        string plain = NewTree(scratch);
        string overridden = NewTree(scratch);

        Put(overridden, OverridePath(_consumerReference), Marked(_consumerReference));

        var expected = Invoke(plain, _consumedEvent);
        var result = Invoke(overridden, _consumedEvent);

        expect(expected.Code == 0 && result.Code == 0, $"add event refused a run the override test compares: {expected.Error}{result.Error}");
        expect(Read(overridden, _writtenConsumer).Contains(_overriddenMarker, StringComparison.Ordinal), "The consumer was not written from the project's override of its template.");
        expect(
            Differing(WrittenFiles(plain), WrittenFiles(overridden)) == _writtenConsumer,
            $"The override of the consumer's template changed another file than the consumer, or none: {Differing(WrittenFiles(plain), WrittenFiles(overridden))}");
        expect(
            result.Output.Contains($"Written from this project's code templates:\n  {OverridePath(_consumerReference)}\n", StringComparison.Ordinal)
                && !expected.Output.Contains("code templates", StringComparison.Ordinal),
            "The report does not list the override it wrote from, or lists one in a run that used none.");
    }

    private static void ABrokenOverrideIsRefusedNamingItCases(string scratch, Action<bool, string> expect)
    {
        string consumer = ToolText(_consumerReference);
        string eventTests = ToolText(_eventTestsReference);
        string stray = "src/Domain/AT.Domain/Features/Placeholders/Model/Events/PlaceholderNoted.cs";

        (string Label, string Reference, string Text, string Message)[] broken =
        [
            ("an unknown flag", _consumerReference, consumer.Replace("\nnamespace ", "\n#if nonsense\n#endif\nnamespace ", StringComparison.Ordinal), "'nonsense' is not a flag of dotnet cleanarch add event"),
            ("an unknown field token", _eventTestsReference, eventTests.Replace("%field.Name%", "%field.Colour%", StringComparison.Ordinal), "'%field.Colour%' is not a token of a field"),
            ("an unknown token", _consumerReference, consumer + "\n// %Limts%\n", "'%Limts%' is a token this generation does not set"),
            ("an #if never closed", _consumerReference, consumer + "\n#if configured\n", "is never closed by an #endif"),
            ("a file naming no template", stray, "namespace N;\n", "names no template of add event; dotnet cleanarch templates copy add-event --dry-run lists them."),
        ];

        foreach (var (label, reference, text, message) in broken)
        {
            string root = NewTree(scratch);

            Put(root, OverridePath(reference), text);

            var before = Snapshot(root);
            var result = Invoke(root, _consumedEvent);

            expect(result.Code == 1, $"[{label}] add event wrote an event from a broken override: {result.Output}");
            expect(
                result.Error.Contains($"error: {OverridePath(reference)}: ", StringComparison.Ordinal) && result.Error.Contains(message, StringComparison.Ordinal),
                $"[{label}] the refusal does not name the override and say '{message}': {result.Error}");
            expect(Same(before, Snapshot(root)), $"[{label}] a refusal over a broken override changed the tree.");
        }
    }

    private static void ReadsOnlyItsOwnFolderCases(string scratch, Action<bool, string> expect)
    {
        string plain = NewTree(scratch);
        string beside = NewTree(scratch);
        string another = $"{ToolTemplates.Folder}/{FeatureCode.TemplateFolder}/src/Domain/AT.Domain/Features/Placeholders/Model/Events/PlaceholderNotedDomainEvent.cs.txt";

        Put(beside, another, "#if nonsense\n");

        var expected = Invoke(plain, _consumedEvent);
        var result = Invoke(beside, _consumedEvent);

        expect(expected.Code == 0, $"add event refused the run the folder test compares: {expected.Error}");
        expect(result.Code == 0, $"add event read a file under another generator's code templates: {result.Error}");
        expect(Same(WrittenFiles(plain), WrittenFiles(beside)), $"A file under another generator's code templates changed what add event wrote: {Differing(WrittenFiles(plain), WrittenFiles(beside))}");
    }

    private static void LeavesTheRecordAddFieldReadsCases(string scratch, Action<bool, string> expect)
    {
        string root = NewTree(scratch);
        string manifest = Path.Combine(root, FeatureManifest.PathOf("Invoices"));
        string recorded = """[{"path":"code-templates/add-feature/src/Domain/AT.Domain/Features/Placeholders/Model/Entities/Placeholder.cs.txt","sha256":"0000"}]""";

        File.WriteAllText(manifest, $"{{\n  \"feature\": \"Invoices\",\n  \"files\": [],\n  \"templates\": {recorded}\n}}\n");
        Put(root, OverridePath(_consumerReference), Marked(_consumerReference));

        var result = Invoke(root, _consumedEvent);
        string? templates = JsonNode.Parse(File.ReadAllText(manifest))!["templates"]?.ToJsonString();

        expect(result.Code == 0, $"add event refused the run the manifest test reads: {result.Error}");
        expect(
            templates == JsonNode.Parse(recorded)!.ToJsonString(),
            $"add event changed the templates the feature's manifest records for add field, which records only add feature's: {templates}");
    }

    private static int Copy(string root, params string[] templates) =>
        TemplateCopy.Run(GeneratorRun.Project(root), new TemplateCopyRequest(EventPlan.TemplateFolder, templates, DryRun: false), TextWriter.Null, TextWriter.Null);

    private static void AnUnchangedCopyWritesTheSameFilesCases(string scratch, Action<bool, string> expect)
    {
        string plain = NewTree(scratch);
        string copied = NewTree(scratch);
        int copy = Copy(copied);
        string listed = string.Concat(ToolTemplates.Of(EventPlan.TemplateFolder).Keys.Where(reference => reference != ToolTemplates.Report).Order(StringComparer.Ordinal).Select(reference => $"\n  {OverridePath(reference)}"));

        var expected = Invoke(plain, _consumedEvent);
        var result = Invoke(copied, _consumedEvent);

        expect(copy == 0, "templates copy add-event refused the copy the test compares.");
        expect(expected.Code == 0 && result.Code == 0, $"add event refused a run the copy test compares: {expected.Error}{result.Error}");
        expect(
            Same(WrittenFiles(plain), WrittenFiles(copied)),
            $"After templates copy add-event, add event did not write the files it writes without the copies: {Differing(WrittenFiles(plain), WrittenFiles(copied))}");
        expect(
            result.Output.Contains($"Written from this project's code templates:{listed}\n", StringComparison.Ordinal),
            $"The report of add event does not list each copy it wrote from: {result.Output}");
        expect(!result.Error.Contains("warning:", StringComparison.Ordinal), $"add event warned over copies the tool has not changed since: {result.Error}");
    }

    private static void AGeneratorWarnsWhenTheToolChangedACopiedTemplateCases(string scratch, Action<bool, string> expect)
    {
        string root = NewTree(scratch);
        string stale =
            $"warning: {OverridePath(_consumerReference)} overrides a template the tool has changed since version {GeneratorCommand.Version}, which it was copied from; "
            + $"the files keep your text. To take the change: move your file aside, run dotnet cleanarch templates copy {EventPlan.TemplateFolder} NotifyListenersOnPlaceholderNotedConsumer.cs, "
            + "and carry your changes over.\n";

        Copy(root, _consumerReference);

        string manifest = Path.Combine(root, TemplateCopies.Manifest);
        var copies = JsonNode.Parse(File.ReadAllText(manifest))!;

        copies["templates"]![0]!["sha256"] = new string('0', 64);
        File.WriteAllText(manifest, copies.ToJsonString());

        var dryRun = Invoke(root, [.. _consumedEvent, "--dry-run"]);
        var result = Invoke(root, _consumedEvent);

        expect(dryRun.Code == 0 && result.Code == 0, $"add event refused over a copy the tool has changed since: {dryRun.Error}{result.Error}");
        expect(
            dryRun.Error.Contains(stale, StringComparison.Ordinal) && result.Error.Contains(stale, StringComparison.Ordinal),
            $"add event, under --dry-run or not, did not warn that the tool changed the template copied: {dryRun.Error}{result.Error}");
        expect(Read(root, _writtenConsumer).Length > 0, "add event did not write the event it warned over.");
    }
}
