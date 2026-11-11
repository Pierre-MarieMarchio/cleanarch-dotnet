using CleanArch.Cli.Generators.Shared;
using Xunit;
using static CleanArch.Cli.Generators.AddFeature.NewFeature;
using static CleanArch.Cli.Generators.Shared.FeatureCode;

namespace CleanArch.Cli.Tests.Generators;

public sealed partial class NewFeatureTests
{
    [Fact]
    public void EveryTemplateIsWritten() => Expectations.Hold(EveryTemplateIsWrittenCases);

    private static void EveryTemplateIsWrittenCases(string scratch, Action<bool, string> expect)
    {
        var written = new HashSet<string>(StringComparer.Ordinal);
        int groups = WithFieldsDigests(scratch, expect, _ => (path, _) => written.Add(path)).Count();
        string external = ExternallyAuthenticatedTree(scratch);
        var spec = DigestSpec(external, ["--access", "owner", "--deletion", "delete"], _operationNames, "none", fields: null, [])!;

        written.UnionWith(Plan(spec, external, Overrides(external), out _).Select(emission => emission.Path));

        var tokens = Tokens(spec);
        string[] unwritten =
        [
            .. ToolTemplates.Of(TemplateFolder).Keys
                .Where(reference => reference != ToolTemplates.Report && !written.Contains(PathOf(spec, reference, tokens)))
                .Select(reference => ToolTemplates.PathOf(TemplateFolder, reference))
                .Order(StringComparer.Ordinal),
        ];

        expect(
            unwritten.Length == 0,
            $"add feature wrote none of these templates in the {groups} digest groups with --fields nor into a tree whose callers sign in "
            + $"with an external provider, so each is text no generation reads: write it from the plan, or delete it. {string.Join(", ", unwritten)}");
    }
}
