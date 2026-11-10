using System.Text.RegularExpressions;

namespace CleanArch.Cli.Generators.Shared;

internal static class TemplateBlocks
{
    private const string _opening = @"each (?<item>[a-z]+)(?: where (?<trait>[a-z]+))?(?: separator ""(?<separator>[^""\n]*)"")?";

    private const string _closing = @"end(?: ""(?<tail>[^""\n]*)"")?";

    private static readonly Regex _lineBlock = new(
        $@"^[ \t]*//#{_opening}[ \t]*\n(?<body>.*?)\n[ \t]*//#{_closing}[ \t]*(?:\n|\z)",
        RegexOptions.Multiline | RegexOptions.Singleline,
        TimeSpan.FromSeconds(2));

    private static readonly Regex _inlineBlock = new(
        $@"/\*#{_opening}\*/(?<body>[^\n]*?)/\*#{_closing}\*/",
        RegexOptions.None,
        TimeSpan.FromSeconds(2));

    private static readonly Regex _itemToken = new(@"%(?<item>[a-z]+)\.(?<token>[A-Za-z]+)%", RegexOptions.None, TimeSpan.FromSeconds(2));

    private static readonly Regex _stray = new(@"(?://|/\*)#(?:each|end)\b|%[a-z]+\.[A-Za-z]+%", RegexOptions.None, TimeSpan.FromSeconds(2));

    internal static string Expand(string text, TemplateList? list)
    {
        string lines = _lineBlock.Replace(text, block => Repeat(block, list, "\n") is { Length: > 0 } items ? items + "\n" : string.Empty);
        string expanded = _inlineBlock.Replace(lines, block => Repeat(block, list, string.Empty));
        var stray = _stray.Match(expanded);

        if (stray.Success)
        {
            throw new InvalidOperationException($"'{stray.Value}' stands outside a block, or opens one it never closes.");
        }

        return expanded;
    }

    private static string Repeat(Match block, TemplateList? list, string defaultSeparator)
    {
        string item = block.Groups["item"].Value;

        if (list is null || list.Name != item)
        {
            throw new InvalidOperationException($"A block repeats over '{item}', which this generation does not list.");
        }

        string body = block.Groups["body"].Value;
        var unknown = _itemToken.Matches(body).FirstOrDefault(token => token.Groups["item"].Value != item || !list.Tokens.ContainsKey(token.Groups["token"].Value));

        if (unknown is not null)
        {
            throw new InvalidOperationException($"'{unknown.Value}' is not a token of a {item}.");
        }

        var selected = Selection(block.Groups["trait"], list);
        string[] items = [.. list.Items.Where(selected).Select(each => _itemToken.Replace(body, token => list.Tokens[token.Groups["token"].Value](each)))];
        string separator = block.Groups["separator"].Success ? block.Groups["separator"].Value.Replace(@"\n", "\n", StringComparison.Ordinal) : defaultSeparator;

        return items.Length == 0 ? string.Empty : string.Join(separator, items) + block.Groups["tail"].Value;
    }

    private static Func<Field, bool> Selection(Group trait, TemplateList list)
    {
        if (!trait.Success)
        {
            return _ => true;
        }

        return list.Traits.TryGetValue(trait.Value, out var holds)
            ? holds
            : throw new InvalidOperationException($"'{trait.Value}' is not a trait a {list.Name} has.");
    }
}
