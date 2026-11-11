using System.Text.RegularExpressions;

namespace CleanArch.Cli.Generators.Shared;

internal static class Template
{
    private static readonly (string Name, string Token)[] _referenceNames =
    [
        (ProjectFolders.ReferenceProduct, "P"), ("ApprovePlaceholder", "UseCase"), ("GetPlaceholderTotals", "UseCase"), ("PlaceholderTotals", "Answer"),
        ("PlaceholderNoted", "E"), ("PlaceholderLimits", "Section"), ("Placeholders", "F"), ("Placeholder", "A"), ("placeholder", "L"),
        ("NotifyListeners", "V"), ("Listeners", "C"),
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

    internal static string Render(string template, FlagSet vocabulary, HashSet<string> flags, Dictionary<string, string> tokens, TemplateList? list = null)
    {
        string selected = Directives.Apply(template.ReplaceLineEndings("\n"), flags, vocabulary);
        string expanded = TemplateBlocks.Expand(ReplaceReferenceNames(selected, tokens), list);

        return Tidy(PruneUsings(EveryTokenSet(Substitute(expanded, tokens))));
    }

    internal static string ReplaceReferenceNames(string text, Dictionary<string, string> tokens) =>
        _referenceName.Replace(text, match =>
            tokens.TryGetValue(_referenceNames.Single(reference => reference.Name == match.Value).Token, out string? value) ? value : match.Value);

    private static string EveryTokenSet(string text) =>
        _tokenPattern.Match(text) is { Success: true } unset
            ? throw new InvalidOperationException($"'{unset.Value}' is a token this generation does not set.")
            : text;

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

        string[] sorted = [.. usings.Distinct(StringComparer.Ordinal).Order(UsingOrder.Directives)];

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
