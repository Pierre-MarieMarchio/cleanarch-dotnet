namespace CleanArch.Cli.Generators.Shared;

internal sealed record UsedTemplate(string Path, string Sha256);

internal sealed class TemplateOverrides
{
    private readonly string _generator;

    private readonly FlagSet _vocabulary;

    private readonly FlagSet _reportVocabulary;

    private readonly Dictionary<string, (string Text, string Sha256)> _overrides = new(StringComparer.Ordinal);

    private readonly SortedDictionary<string, string> _used = new(StringComparer.Ordinal);

    private readonly List<string> _problems = [];

    private TemplateOverrides(string generator, FlagSet vocabulary, FlagSet reportVocabulary)
    {
        _generator = generator;
        _vocabulary = vocabulary;
        _reportVocabulary = reportVocabulary;
    }

    internal string Generator => _generator;

    private string Spoken => ToolTemplates.Spoken(_generator);

    internal IReadOnlyList<UsedTemplate> Used => [.. _used.Select(used => new UsedTemplate(used.Key, used.Value))];

    internal IReadOnlyList<string> Problems => _problems;

    internal static TemplateOverrides Read(string root, string generator, FlagSet vocabulary, FlagSet reportVocabulary)
    {
        var templates = new TemplateOverrides(generator, vocabulary, reportVocabulary);
        string folder = ProjectFolders.Full(root, $"{ToolTemplates.Folder}/{generator}");

        if (Directory.Exists(folder))
        {
            foreach (string file in Directory.EnumerateFiles(folder, $"*{ToolTemplates.Extension}", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
            {
                templates.ReadOne(Path.GetRelativePath(folder, file).Replace('\\', '/'), file);
            }
        }

        return templates;
    }

    internal string Render(string reference, HashSet<string> flags, Dictionary<string, string> tokens, TemplateList? list)
    {
        string path = ToolTemplates.PathOf(_generator, reference);

        if (!_overrides.TryGetValue(reference, out var custom))
        {
            return RenderToolText(path, reference, flags, tokens, list);
        }

        try
        {
            string rendered = Template.Render(custom.Text, _vocabulary, flags, tokens, list);

            _used[path] = custom.Sha256;

            return rendered;
        }
        catch (InvalidOperationException exception)
        {
            Refuse($"{path}: {exception.Message}");

            return string.Empty;
        }
    }

    internal void WriteReport(TextWriter output, ReportMode mode, HashSet<string> flags, Dictionary<string, string> tokens)
    {
        string template = ToolTemplates.Of(_generator)[ToolTemplates.Report];

        if (Template.TokensOf(template).FirstOrDefault(token => !tokens.ContainsKey(token)) is { } unset)
        {
            throw new InvalidOperationException($"{ToolTemplates.PathOf(_generator, ToolTemplates.Report)} names %{unset}%, which {Spoken} does not set in every run.");
        }

        foreach (string line in Template.RenderReport(template, _reportVocabulary, [.. flags, ReportFlags.Of(mode)], tokens).Split('\n'))
        {
            output.WriteLine(line);
        }
    }

    internal IEnumerable<string> DifferingFrom(IReadOnlyDictionary<string, string> recorded) =>
        recorded.Keys.Union(_used.Keys, StringComparer.Ordinal)
            .Where(path => !recorded.TryGetValue(path, out string? was) || !_used.TryGetValue(path, out string? now) || was != now)
            .Order(StringComparer.Ordinal);

    internal void ReportUsed(TextWriter output, ReportMode mode)
    {
        if (_used.Count == 0 || mode == ReportMode.Refused)
        {
            return;
        }

        output.WriteLine();
        output.WriteLine("Written from this project's code templates:");

        foreach (string path in _used.Keys)
        {
            output.WriteLine($"  {path}");
        }
    }

    private void ReadOne(string relative, string file)
    {
        string path = $"{ToolTemplates.Folder}/{_generator}/{relative}";

        if (Path.GetFileName(relative).StartsWith('.'))
        {
            return;
        }

        string reference = relative[..^ToolTemplates.Extension.Length];

        if (!ToolTemplates.Of(_generator).ContainsKey(reference))
        {
            Refuse(
                $"{path}: names no template of {Spoken}; "
                + $"dotnet cleanarch templates copy {_generator} --dry-run lists them.");

            return;
        }

        try
        {
            string text = File.ReadAllText(file);

            _overrides[reference] = (ToolTemplates.Normalize(text), ToolTemplates.Digest(text));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Refuse($"{path}: cannot be read ({exception.Message}).");
        }
    }

    private string RenderToolText(string path, string reference, HashSet<string> flags, Dictionary<string, string> tokens, TemplateList? list)
    {
        string template = ToolTemplates.Of(_generator).TryGetValue(reference, out string? text)
            ? text
            : throw new InvalidOperationException($"{path} is not embedded in the tool, so {Spoken} has no text to write {reference} from.");

        try
        {
            return Template.Render(template, _vocabulary, flags, tokens, list);
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidOperationException($"{path}: {exception.Message}", exception);
        }
    }

    private void Refuse(string problem)
    {
        if (!_problems.Contains(problem, StringComparer.Ordinal))
        {
            _problems.Add(problem);
        }
    }
}
