using System.Text.RegularExpressions;

namespace CleanArch.Cli.Generators.Shared;

internal static class Template
{
    internal static readonly IReadOnlySet<string> KnownFlags = new HashSet<string>(StringComparer.Ordinal)
    {
        "get", "list", "create", "update", "delete", "owner", "shared", "rec", "agg", "etag",
        "content", "change", "wq", "dto", "single", "rq", "notfound", "clock", "stage",
        "purge", "anon", "fields", "anontext", "trim", "policy", "prefixed",
    };

    private static readonly (string Name, string Token)[] _referenceNames =
    [
        ("AppTemplate", "P"), ("Placeholders", "F"), ("Placeholder", "A"), ("placeholder", "L"),
    ];

    private static readonly Regex _referenceName = new(
        string.Join('|', _referenceNames.Select(reference => reference.Name)),
        RegexOptions.None,
        TimeSpan.FromSeconds(2));

    private static readonly Regex _tokenPattern = new("%([A-Za-z]+)%", RegexOptions.None, TimeSpan.FromSeconds(2));

    private static readonly Regex _markedUsing = new(
        @"^using (?<namespace>[^;]+);\s*//\?(?<types>.+)$",
        RegexOptions.None,
        TimeSpan.FromSeconds(2));

    internal static string Substitute(string text, Dictionary<string, string> tokens)
    {
        for (int pass = 0; pass < 3; pass++)
        {
            string next = _tokenPattern.Replace(
                text,
                match => tokens.TryGetValue(match.Groups[1].Value, out string? value) ? value : match.Value);

            if (string.Equals(next, text, StringComparison.Ordinal))
            {
                break;
            }

            text = next;
        }

        return text;
    }

    internal static string Render(string template, HashSet<string> flags, Dictionary<string, string> tokens, TemplateList? list = null)
    {
        string selected = Directives.Apply(template.ReplaceLineEndings("\n"), flags, KnownFlags);
        string expanded = TemplateBlocks.Expand(ReplaceReferenceNames(selected, tokens), list);

        return Tidy(PruneUsings(Substitute(expanded, tokens)));
    }

    private static string ReplaceReferenceNames(string text, Dictionary<string, string> tokens) =>
        _referenceName.Replace(text, match =>
            tokens.TryGetValue(_referenceNames.Single(reference => reference.Name == match.Value).Token, out string? value) ? value : match.Value);

    private static string PruneUsings(string text)
    {
        string[] lines = text.Split('\n');
        int declaration = Array.FindIndex(lines, line => line.StartsWith("namespace ", StringComparison.Ordinal));

        if (declaration < 0)
        {
            throw new InvalidOperationException("A template declares no namespace.");
        }

        string body = string.Join('\n', lines[declaration..]);
        var usings = new List<string>();

        foreach (string line in lines[..declaration].Where(line => line.StartsWith("using ", StringComparison.Ordinal)))
        {
            var marked = _markedUsing.Match(line);

            if (!marked.Success)
            {
                usings.Add(line);

                continue;
            }

            if (Regex.IsMatch(body, $@"\b(?:{marked.Groups["types"].Value.Trim()})\b", RegexOptions.None, TimeSpan.FromSeconds(2)))
            {
                usings.Add($"using {marked.Groups["namespace"].Value};");
            }
        }

        string[] sorted =
        [
            .. usings.Distinct(StringComparer.Ordinal)
                .OrderBy(line => !line.StartsWith("using System", StringComparison.Ordinal))
                .ThenBy(line => line.TrimEnd(';'), StringComparer.Ordinal),
        ];

        return sorted.Length == 0 ? body : string.Join('\n', sorted) + "\n\n" + body;
    }

    private static string Tidy(string text)
    {
        var lines = new List<string>();

        foreach (string raw in text.Split('\n'))
        {
            string line = raw.TrimEnd();

            if (line.Length == 0 && (lines.Count == 0 || lines[^1].Length == 0 || lines[^1].EndsWith('{')))
            {
                continue;
            }

            if (line.TrimStart().StartsWith('}') && lines.Count > 0 && lines[^1].Length == 0)
            {
                lines.RemoveAt(lines.Count - 1);
            }

            lines.Add(line);
        }

        while (lines.Count > 0 && lines[^1].Length == 0)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        return string.Join('\n', lines) + "\n";
    }
}
