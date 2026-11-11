using CleanArch.Cli.Generators.AddSetting;
using CleanArch.Cli.Generators.Shared;
using Xunit;

namespace CleanArch.Cli.Tests.Generators;

public sealed partial class NewSettingTests
{
    private const string _overriddenMarker = "// written from this project's code template";

    private const string _optionsFile = "src/Application/A.Application/Features/Invoices/Configuration/Options/InvoiceLimitsOptions.cs";

    private static readonly string[] _overriddenSetting = ["Invoices", "InvoiceLimits", "--keys", _validKeys];

    [Fact]
    public void OverrideWinsForItsFileOnly() => Expectations.Hold(OverrideWinsForItsFileOnlyCases);

    [Fact]
    public void ABrokenOverrideIsRefusedNamingIt() => Expectations.Hold(ABrokenOverrideIsRefusedNamingItCases);

    private static string ToolText(string reference) => ToolTemplates.Of(SettingPlan.TemplateFolder)[reference];

    private static string OverridePath(string reference) => ToolTemplates.PathOf(SettingPlan.TemplateFolder, reference);

    private static void PutOverride(string root, string reference, string text)
    {
        string full = Path.Combine(root, OverridePath(reference));

        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, text);
    }

    private static string Marked(string reference) =>
        ToolText(reference).Replace(";\n\npublic sealed class", $";\n{_overriddenMarker}\n\npublic sealed class", StringComparison.Ordinal) + "\n";

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

        PutOverride(overridden, SettingPlan.OptionsTemplate, Marked(SettingPlan.OptionsTemplate));

        var expected = Invoke(plain, _overriddenSetting);
        var result = Invoke(overridden, _overriddenSetting);

        expect(expected.Code == 0 && result.Code == 0, $"add setting refused a run the override test compares: {expected.Error}{result.Error}");
        expect(Read(overridden, _optionsFile).Contains(_overriddenMarker, StringComparison.Ordinal), "The options class was not written from the project's override of its template.");
        expect(
            Differing(WrittenFiles(plain), WrittenFiles(overridden)) == _optionsFile,
            $"The override of the options class's template changed another file than the options class, or none: {Differing(WrittenFiles(plain), WrittenFiles(overridden))}");
        expect(
            result.Output.Contains($"Written from this project's code templates:\n  {OverridePath(SettingPlan.OptionsTemplate)}\n", StringComparison.Ordinal)
                && !expected.Output.Contains("code templates", StringComparison.Ordinal),
            $"The report does not list the override it wrote from, or lists one in a run that used none: {result.Output}");
        expect(
            !Read(overridden, ".feature-manifest/Invoices.json").Contains("\"templates\"", StringComparison.Ordinal),
            "The feature's manifest records the templates add setting wrote from, which only add field reads, and only for add feature's templates.");
    }

    private static void ABrokenOverrideIsRefusedNamingItCases(string scratch, Action<bool, string> expect)
    {
        string options = SettingPlan.OptionsTemplate;
        string tests = SettingPlan.TestsTemplate;

        (string Reference, string Text, string Message)[] broken =
        [
            (options.Replace("PlaceholderLimitsOptions", "PlaceholderLimitOptions", StringComparison.Ordinal), "namespace N;\n", "names no template of add setting; dotnet cleanarch templates copy add-setting --dry-run lists them."),
            (tests, ToolText(tests).Replace("#if configured", "#if owner", StringComparison.Ordinal), "'owner' is not a flag of dotnet cleanarch add setting"),
            (options, ToolText(options).Replace("%key.Default%", "%key.Initial%", StringComparison.Ordinal), "'%key.Initial%' is not a token of a key"),
            (tests, ToolText(tests).Replace("where supplied", "where secret", StringComparison.Ordinal), "'secret' is not a trait a key has"),
            (options, ToolText(options) + "\n// %Limts%\n", "'%Limts%' is a token this generation does not set"),
        ];

        foreach (var (reference, text, message) in broken)
        {
            string root = NewTree(scratch);

            PutOverride(root, reference, text);
            ExpectRefusal(root, _overriddenSetting, $"error: {OverridePath(reference)}: ", expect);
            ExpectRefusal(root, _overriddenSetting, message, expect);
        }
    }
}
