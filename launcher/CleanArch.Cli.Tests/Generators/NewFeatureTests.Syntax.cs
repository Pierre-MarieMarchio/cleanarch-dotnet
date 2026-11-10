using System.Text.RegularExpressions;
using CleanArch.Cli.Generators.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CleanArch.Cli.Tests.Generators;

public sealed partial class NewFeatureTests
{
    private static string? FirstSyntaxError(string code, CSharpParseOptions? options = null) =>
        CSharpSyntaxTree.ParseText(code, options).GetDiagnostics().FirstOrDefault(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)?.ToString();

    private static IEnumerable<string> TokenFreeTemplatesThatDoNotParse(Action<string> tokenFree)
    {
        string[] vocabulary = [.. FeatureFlags.Vocabulary.Meanings.Keys];

        foreach (var (name, template) in ToolTemplates.Of(FeatureCode.TemplateFolder).OrderBy(entry => entry.Key, StringComparer.Ordinal))
        {
            if (Regex.IsMatch(template, @"%[A-Za-z.]+%", RegexOptions.None, TimeSpan.FromSeconds(2)))
            {
                continue;
            }

            tokenFree(name);

            string conditions = string.Join(' ', Regex.Matches(template, @"^[ \t]*#(?:if|elif)\b(?<condition>.*)$", RegexOptions.Multiline, TimeSpan.FromSeconds(2)).Select(match => match.Groups["condition"].Value));
            string[] named = [.. vocabulary.Where(flag => Regex.IsMatch(conditions, $@"\b{flag}\b", RegexOptions.None, TimeSpan.FromSeconds(2)))];

            for (int mask = 0; mask < 1 << named.Length; mask++)
            {
                string[] flags = [.. named.Where((_, index) => (mask & (1 << index)) != 0)];

                if (FirstSyntaxError(template, CSharpParseOptions.Default.WithPreprocessorSymbols(flags)) is { } error)
                {
                    yield return $"{name} under #if {(flags.Length == 0 ? "no flag" : string.Join(" && ", flags))}: {error}";
                }
            }
        }
    }

    private sealed class GeneratedSyntax
    {
        private readonly Dictionary<string, string?> _errors = new(StringComparer.Ordinal);

        private readonly Dictionary<string, SortedSet<string>> _writers = new(StringComparer.Ordinal);

        public Action<string, string> In(string group) => (path, content) => Read(group, path, content);

        public void Hold(string generation, Action<bool, string> expect)
        {
            string[] broken = [.. _writers.Select(entry => $"{_errors[entry.Key]}, written as {string.Join(", ", entry.Value)}")];

            expect(broken.Length == 0, $"{generation} wrote C# that does not parse: {string.Join(" | ", broken)}");
        }

        private void Read(string group, string path, string content)
        {
            if (!path.EndsWith(".cs", StringComparison.Ordinal))
            {
                return;
            }

            if (!_errors.TryGetValue(content, out string? error))
            {
                error = FirstSyntaxError(content);
                _errors.Add(content, error);
            }

            if (error is null)
            {
                return;
            }

            if (!_writers.TryGetValue(content, out var writers))
            {
                writers = new SortedSet<string>(StringComparer.Ordinal);
                _writers.Add(content, writers);
            }

            writers.Add($"[{group}] {path}");
        }
    }
}
