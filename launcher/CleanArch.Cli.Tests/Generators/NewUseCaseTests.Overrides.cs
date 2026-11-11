using System.Security.Cryptography;
using System.Text.Json.Nodes;
using CleanArch.Cli.Generators.AddUseCase;
using CleanArch.Cli.Generators.Shared;
using Xunit;

namespace CleanArch.Cli.Tests.Generators;

public sealed partial class NewUseCaseTests
{
    private const string _referenceCommand = "src/Application/AT.Application/Features/Placeholders/UseCases/Commands/ApprovePlaceholder/ApprovePlaceholder.cs";

    private const string _referenceRequest = "src/Presentation/AT.Api/Features/Placeholders/Contracts/Requests/ApprovePlaceholderRequest.cs";

    private const string _referenceValidatorTests =
        "tests/Application/AT.Application.UnitTests/Features/Placeholders/UseCases/Commands/ApprovePlaceholder/ApprovePlaceholderCommandValidatorTests.cs";

    private const string _approveCommand = "src/Application/A.Application/Features/Invoices/UseCases/Commands/ApproveInvoice/ApproveInvoice.cs";

    private const string _overriddenMarker = "// written from this project's code template";

    private static readonly string[] _approving = ["Invoices", "ApproveInvoice", "--kind", "command", "--input", "id, Reason:string"];

    [Fact]
    public void OverrideWinsForItsFileOnly() => Expectations.Hold(OverrideWinsForItsFileOnlyCases);

    [Fact]
    public void TheReportShowsTheOverriddenRequest() => Expectations.Hold(TheReportShowsTheOverriddenRequestCases);

    [Fact]
    public void ABrokenOverrideIsRefusedNamingIt() => Expectations.Hold(ABrokenOverrideIsRefusedNamingItCases);

    [Fact]
    public void TheManifestRecordsTheWrittenBytesAndNoTemplatesKey() => Expectations.Hold(TheManifestRecordsTheWrittenBytesAndNoTemplatesKeyCases);

    private static string ToolText(string reference) => ToolTemplates.Of(NewUseCase.TemplateFolder)[reference];

    private static string OverridePath(string reference) => ToolTemplates.PathOf(NewUseCase.TemplateFolder, reference);

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

    private static string Differing(Dictionary<string, string> expected, Dictionary<string, string> actual) =>
        string.Join(", ", expected.Keys.Union(actual.Keys, StringComparer.Ordinal)
            .Where(path => !expected.TryGetValue(path, out string? was) || !actual.TryGetValue(path, out string? now) || was != now)
            .Order(StringComparer.Ordinal));

    private static void OverrideWinsForItsFileOnlyCases(string scratch, Action<bool, string> expect)
    {
        string plain = NewTree(scratch);
        string overridden = NewTree(scratch);

        Put(overridden, OverridePath(_referenceCommand), Marked(_referenceCommand));

        var expected = Invoke(plain, _approving);
        var result = Invoke(overridden, _approving);

        expect(expected.Code == 0 && result.Code == 0, $"add use-case refused a run the override test compares: {expected.Error}{result.Error}");
        expect(
            File.ReadAllText(Path.Combine(overridden, _approveCommand)).Contains(_overriddenMarker, StringComparison.Ordinal),
            "The command was not written from the project's override of its template.");
        expect(
            Differing(WrittenFiles(plain), WrittenFiles(overridden)) == _approveCommand,
            $"The override of the command's template changed another file than the command, or none: {Differing(WrittenFiles(plain), WrittenFiles(overridden))}");
        expect(
            result.Output.Contains($"Written from this project's code templates:\n  {OverridePath(_referenceCommand)}\n", StringComparison.Ordinal)
                && !expected.Output.Contains("code templates", StringComparison.Ordinal),
            "The report does not list the override it wrote from, or lists one in a run that used none.");
    }

    private static void TheReportShowsTheOverriddenRequestCases(string scratch, Action<bool, string> expect)
    {
        string root = NewTree(scratch);

        Put(root, OverridePath(_referenceRequest), Marked(_referenceRequest));

        var result = Invoke(root, [.. _approving, "--dry-run"]);

        expect(result.Code == 0, $"add use-case refused a dry run over an override of the request: {result.Error}");
        expect(
            result.Output.Contains(
                "  and src/Presentation/A.Api/Features/Invoices/Contracts/Requests/ApproveInvoiceRequest.cs, with what a client sends:\n"
                    + "      namespace Acme.Api.Features.Invoices.Contracts.Requests;\n"
                    + $"      {_overriddenMarker}\n",
                StringComparison.Ordinal),
            $"The report does not show the request as the project's override writes it: {result.Output}");
    }

    private static void ABrokenOverrideIsRefusedNamingItCases(string scratch, Action<bool, string> expect)
    {
        int namespaceLine = Array.FindIndex(ToolText(_referenceCommand).Split('\n'), line => line.StartsWith("namespace ", StringComparison.Ordinal)) + 1;
        string stray = $"{ToolTemplates.Folder}/{NewUseCase.TemplateFolder}/src/Application/AT.Application/Features/Placeholders/UseCases/Commands/ApprovePlaceholder/ApprovePlaceholderHandler.cs.txt";

        (string Label, string Path, string Text, string Message)[] broken =
        [
            (
                "an unknown flag",
                OverridePath(_referenceCommand),
                ToolText(_referenceCommand).Replace("\nnamespace ", "\n#if nonsense\n#endif\nnamespace ", StringComparison.Ordinal),
                $"line {namespaceLine}: 'nonsense' is not a flag of dotnet cleanarch add use-case"),
            ("a flag of add feature", OverridePath(_referenceCommand), ToolText(_referenceCommand) + "\n#if list\n#endif\n", "'list' is not a flag of dotnet cleanarch add use-case"),
            (
                "an unknown parameter token",
                OverridePath(_referenceValidatorTests),
                ToolText(_referenceValidatorTests).Replace("%parameter.Valid%", "%parameter.Colour%", StringComparison.Ordinal),
                "'%parameter.Colour%' is not a token of a parameter"),
            ("an unknown token", OverridePath(_referenceRequest), ToolText(_referenceRequest) + "\n// %Limits%\n", "'%Limits%' is a token this generation does not set"),
            ("a file naming no template", stray, "namespace N;\n", "names no template of add use-case; dotnet cleanarch templates copy add-use-case --dry-run lists them."),
        ];

        foreach (var (label, path, text, message) in broken)
        {
            string root = NewTree(scratch);

            Put(root, path, text);

            var before = Snapshot(root);
            var result = Invoke(root, _approving);

            expect(result.Code == 1, $"[{label}] add use-case wrote a use case beside a broken override: {result.Output}");
            expect(
                result.Error.Contains($"error: {path}: ", StringComparison.Ordinal) && result.Error.Contains(message, StringComparison.Ordinal),
                $"[{label}] the refusal does not name the override and say '{message}': {result.Error}");
            expect(Same(before, Snapshot(root)), $"[{label}] a refusal over a broken override changed the tree.");
        }
    }

    private static void TheManifestRecordsTheWrittenBytesAndNoTemplatesKeyCases(string scratch, Action<bool, string> expect)
    {
        string root = NewTree(scratch);

        Put(
            root,
            FeatureManifest.PathOf("Invoices"),
            "{\n  \"schemaVersion\": 1,\n  \"generator\": \"Tools/NewFeature.cs\",\n  \"feature\": \"Invoices\",\n  \"type\": \"Invoice\",\n  \"files\": []\n}\n");
        Put(root, OverridePath(_referenceCommand), Marked(_referenceCommand));

        var result = Invoke(root, _approving);
        var manifest = JsonNode.Parse(File.ReadAllText(Path.Combine(root, FeatureManifest.PathOf("Invoices"))))!;
        string? recorded = manifest["files"]!.AsArray().SingleOrDefault(file => file!["path"]!.GetValue<string>() == _approveCommand)?["sha256"]?.GetValue<string>();

        expect(result.Code == 0, $"add use-case refused a run over an override in a feature with a manifest: {result.Error}");
        expect(
            recorded == Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(Path.Combine(root, _approveCommand)))),
            "The manifest does not record the bytes the override wrote, so remove feature would take the command for edited.");
        expect(manifest["templates"] is null, $"add use-case recorded the templates it wrote from, which only add field reads: {manifest["templates"]?.ToJsonString()}");
    }
}
