using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace CleanArch.Cli.Tests.Generators;

public sealed partial class NewFeatureTests
{
    [Fact]
    public void GeneratedCodeParses() => Expectations.Hold(GeneratedCodeParsesCases);

    private static void GeneratedCodeParsesCases(string scratch, Action<bool, string> expect)
    {
        var parsed = new HashSet<string>(StringComparer.Ordinal);
        var broken = new List<string>();

        void Parse(string path, string content)
        {
            if (!path.EndsWith(".cs", StringComparison.Ordinal) || !parsed.Add(content))
            {
                return;
            }

            var first = CSharpSyntaxTree.ParseText(content).GetDiagnostics().FirstOrDefault(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);

            if (first is not null)
            {
                broken.Add($"{path}: {first}");
            }
        }

        void Flush(string group)
        {
            expect(broken.Count == 0, $"[{group}] generated C# does not parse: {string.Join(" | ", broken)}");
            broken.Clear();
        }

        foreach (var (group, _) in WithoutFieldsDigests(scratch, Parse))
        {
            Flush($"without --fields, {group}");
        }

        foreach (var (group, _) in WithFieldsDigests(scratch, expect, Parse))
        {
            Flush($"with --fields, {group}");
        }

        foreach (var (group, _) in FieldAdditionDigests(scratch, expect, Parse))
        {
            Flush($"add field, {group}");
        }
    }
}
