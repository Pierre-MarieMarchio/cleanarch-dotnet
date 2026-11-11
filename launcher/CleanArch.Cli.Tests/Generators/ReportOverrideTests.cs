using System.Security.Cryptography;
using System.Text.Json.Nodes;
using CleanArch.Cli.CodeTemplates;
using CleanArch.Cli.Generators.Shared;
using Xunit;

namespace CleanArch.Cli.Tests.Generators;

public sealed class ReportOverrideTests
{
    private const string _marker = "Reported from this project's copy.";

    public static TheoryData<string> Generators => ["add-feature", "add-field", "add-use-case", "add-event", "add-setting"];

    [Theory]
    [MemberData(nameof(Generators))]
    public void ACopyOfTheReport_ChangesTheReportOnly(string generator) => Expectations.Hold((scratch, expect) =>
    {
        var (tree, run) = Case(generator);
        string plain = tree(scratch);
        string copied = tree(scratch);

        Copy(copied, generator);
        File.AppendAllText(Path.Combine(copied, Report(generator)), $"#if applied\n{_marker}\n#endif\n");

        var expected = run(plain);
        var result = run(copied);
        string output = Rooted(result.Output, copied);

        expect(expected.Code == 0 && result.Code == 0, $"[{generator}] refused a run the report's copy is compared over: {expected.Error}{result.Error}");
        expect(
            output.Contains($"\n{_marker}\n", StringComparison.Ordinal) && output.Replace($"{_marker}\n", string.Empty, StringComparison.Ordinal) == Rooted(expected.Output, plain),
            $"[{generator}] the copy of report.txt did not change the report by its line alone, the copy left out of 'Written from': {result.Output}");
        expect(Rooted(result.Error, copied) == Rooted(expected.Error, plain), $"[{generator}] the copy of report.txt changed what the run says on the error output: {result.Error}");
        expect(Same(Written(plain), Written(copied)), $"[{generator}] the copy of report.txt changed a file the run writes, a feature's manifest included.");
    });

    [Theory]
    [MemberData(nameof(Generators))]
    public void ABrokenReport_IsRefusedNamingIt_BeforeAnythingIsWritten(string generator) => Expectations.Hold((scratch, expect) =>
    {
        var (tree, run) = Case(generator);
        string root = tree(scratch);

        Put(root, generator, ToolTemplates.Of(generator)[ToolTemplates.Report] + "\n#if nonsense\n#endif\n");

        var before = Written(root);
        var result = run(root);

        expect(
            result.Code == 1 && result.Error.Contains($"{Report(generator)}: line ", StringComparison.Ordinal) && result.Error.Contains("'nonsense' is not a flag", StringComparison.Ordinal),
            $"[{generator}] a report.txt naming an unknown flag was not refused naming its path and line: {result.Error}");
        expect(Same(before, Written(root)), $"[{generator}] wrote something before refusing a broken report.txt.");
    });

    [Theory]
    [MemberData(nameof(Generators))]
    public void ACopyOfTheReport_WarnsOnceTheToolChangesIt(string generator) => Expectations.Hold((scratch, expect) =>
    {
        var (tree, run) = Case(generator);
        string root = tree(scratch);

        Copy(root, generator);

        var copies = JsonNode.Parse(File.ReadAllText(Path.Combine(root, TemplateCopies.Manifest)))!;

        copies["templates"]![0]!["sha256"] = new string('0', 64);
        File.WriteAllText(Path.Combine(root, TemplateCopies.Manifest), copies.ToJsonString());

        var result = run(root);

        expect(
            result.Code == 0 && result.Error.Contains($"warning: {Report(generator)} overrides a template the tool has changed since version", StringComparison.Ordinal),
            $"[{generator}] did not warn, and only warn, over a copy of report.txt the tool has changed since: {result.Error}");
    });

    [Fact]
    public void ACopyOfTheReport_HoldsWhatAReportReads_AndNothingElse() => Expectations.Hold((scratch, expect) =>
    {
        (string Label, string Added, string Refusal)[] broken =
        [
            ("a block", "//#each field\n%field.Name%\n//#end", "'//#each' opens a block, which a report never repeats"),
            ("a token no report sets", "%Limts%", "'%Limts%' is not a token of the report of add event"),
            ("a token of the event's files", "%E%", "'%E%' is not a token of the report of add event"),
            ("a flag of another generator", "#if list\n#endif", "'list' is not a flag of dotnet cleanarch add event"),
            ("an #if never closed", "#if consumed", "is never closed by an #endif"),
        ];

        foreach (var (label, added, refusal) in broken)
        {
            string root = NewEventTests.NewTree(scratch);

            Put(root, "add-event", ToolTemplates.Of("add-event")[ToolTemplates.Report] + $"\n{added}\n");

            var result = NewEventTests.Invoke(root, "Invoices", "InvoicePaidDomainEvent", "--unconsumed", "Nothing reacts to it");

            expect(
                result.Code == 1 && result.Error.Contains($"{Report("add-event")}: ", StringComparison.Ordinal) && result.Error.Contains(refusal, StringComparison.Ordinal),
                $"[{label}] a report.txt holding it was not refused with '{refusal}': {result.Error}");
        }

        string flagged = NewEventTests.NewTree(scratch);

        Put(flagged, "add-event", $"#if dryrun && !consumed && !configured\n{_marker} %Count%\n#endif\n");

        var read = NewEventTests.Invoke(flagged, "Invoices", "InvoicePaidDomainEvent", "--unconsumed", "Nothing reacts to it", "--dry-run");

        expect(
            read.Code == 0 && read.Output == $"{_marker} 2\n",
            $"A report.txt naming a mode flag, a flag of its report and one of its generator, and a token of the tool's report, was not read: {read.Error}{read.Output}");
    });

    private static (Func<string, string> Tree, Func<string, (int Code, string Output, string Error)> Run) Case(string generator) => generator switch
    {
        "add-feature" => (scratch => NewFeatureTests.NewTree(scratch), root => NewFeatureTests.Invoke(root, "Widgets", "Widget", "--access", "shared")),
        "add-field" => (FeatureToExtend, root => NewFeatureTests.InvokeField(root, _ => 0, "Things", "Notes:text?")),
        "add-use-case" => (scratch => NewUseCaseTests.NewTree(scratch), root => NewUseCaseTests.Invoke(root, "Invoices", "ApproveInvoice", "--kind", "command", "--input", "id, Reason:string")),
        "add-event" => (NewEventTests.NewTree, root => NewEventTests.Invoke(root, "Invoices", "InvoicePaidDomainEvent", "--consumer", "Reminders:CancelReminders")),
        _ => (NewSettingTests.NewTree, root => NewSettingTests.Invoke(root, "Invoices", "InvoiceLimits", "--keys", "Max:int(1..9)=1")),
    };

    private static string FeatureToExtend(string scratch)
    {
        string root = NewFeatureTests.FieldTree(scratch);

        NewFeatureTests.Invoke(root, "Things", "Thing", "--access", "shared", "--fields", "Title:string(120)");

        return root;
    }

    private static string Report(string generator) => ToolTemplates.PathOf(generator, ToolTemplates.Report);

    private static void Put(string root, string generator, string text)
    {
        string path = Path.Combine(root, Report(generator));

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, text);
    }

    private static void Copy(string root, string generator) =>
        TemplateCopy.Run(GeneratorRun.Project(root), new TemplateCopyRequest(generator, [ToolTemplates.Report], DryRun: false), TextWriter.Null, TextWriter.Null);

    private static string Rooted(string text, string root) => text.Replace(root, "<root>", StringComparison.Ordinal);

    private static Dictionary<string, string> Written(string root) =>
        Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
            .Where(path => !path.StartsWith(ToolTemplates.Folder + "/", StringComparison.Ordinal))
            .ToDictionary(path => path, path => Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(root, path)))), StringComparer.Ordinal);

    private static bool Same(Dictionary<string, string> before, Dictionary<string, string> after) =>
        before.Count == after.Count && before.All(pair => after.TryGetValue(pair.Key, out string? hash) && hash == pair.Value);
}
