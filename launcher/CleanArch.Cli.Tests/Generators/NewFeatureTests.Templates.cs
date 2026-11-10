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
        Action<string, string> seen = (path, _) => written.Add(path);

        int groups = WithoutFieldsDigests(scratch, seen).Concat(WithFieldsDigests(scratch, expect, seen)).Count();
        string external = ExternallyAuthenticatedTree(scratch);
        var spec = DigestSpec(external, ["--access", "owner", "--deletion", "delete"], _operationNames, "none", fields: null, [])!;

        written.UnionWith(Plan(spec, external, out _).Select(emission => emission.Path));

        var tokens = Tokens(spec);

        foreach (string reference in ToolTemplates.Of(ToolTemplates.AddFeature).Keys)
        {
            string path = PathOf(spec, reference, host: null, tokens);

            expect(
                written.Contains(path),
                $"{ToolTemplates.Folder}/{ToolTemplates.AddFeature}/{reference}.txt is embedded in the tool, yet add feature wrote {path} in none of "
                + $"the {groups} digest groups nor into a tree whose callers sign in with an external provider: a template no generation writes is "
                + "dead text a project could override to no effect. Write it from the plan, or delete it.");
        }
    }
}
