using System.Text.RegularExpressions;

namespace CleanArch.Cli.Generators.Shared;

internal sealed class Directives(IReadOnlySet<string> flags, FlagSet vocabulary)
{
    private static readonly Regex _directive = new(
        @"^[ \t]*#(?<keyword>if|elif|else|endif)\b(?<rest>.*)$",
        RegexOptions.None,
        TimeSpan.FromSeconds(2));

    private readonly Stack<Branch> _open = new();

    private bool _emitting = true;

    internal static string Apply(string text, IReadOnlySet<string> flags, FlagSet vocabulary)
    {
        var directives = new Directives(flags, vocabulary);
        string[] lines = text.Split('\n');
        var kept = new List<string>();

        for (int index = 0; index < lines.Length; index++)
        {
            if (directives.Keeps(index + 1, lines[index]))
            {
                kept.Add(lines[index]);
            }
        }

        return directives._open.TryPeek(out var unclosed)
            ? throw Refusal(unclosed.Number, unclosed.Line, "is never closed by an #endif")
            : string.Join('\n', kept);
    }

    private static InvalidOperationException Refusal(int number, string line, string problem) =>
        new($"line {number}: '{line.Trim()}' {problem}.");

    private bool Keeps(int number, string line)
    {
        var directive = _directive.Match(line);

        if (!directive.Success)
        {
            return _emitting;
        }

        string keyword = directive.Groups["keyword"].Value;
        string rest = directive.Groups["rest"].Value.Trim();

        if (keyword is "else" or "endif" && rest.Length > 0)
        {
            throw Refusal(number, line, $"takes nothing after #{keyword}");
        }

        switch (keyword)
        {
            case "if":
                Open(number, line, rest);
                break;

            case "endif":
                _emitting = Pop(number, line).Parent;
                break;

            default:
                Alternate(number, line, Pop(number, line), keyword == "else" ? null : rest);
                break;
        }

        return false;
    }

    private Branch Pop(int number, string line) =>
        _open.Count > 0 ? _open.Pop() : throw Refusal(number, line, "belongs to no #if");

    private void Open(int number, string line, string condition)
    {
        bool holds = Holds(number, condition);

        _open.Push(new Branch(number, line, _emitting, holds, Else: false));
        _emitting = _emitting && holds;
    }

    private void Alternate(int number, string line, Branch branch, string? condition)
    {
        if (branch.Else)
        {
            throw Refusal(number, line, "follows the #else of its #if");
        }

        bool taken = (condition is null || Holds(number, condition)) && !branch.Taken;

        _open.Push(branch with { Taken = branch.Taken || taken, Else = condition is null });
        _emitting = branch.Parent && taken;
    }

    private bool Holds(int number, string condition)
    {
        try
        {
            return Condition.Parse(condition, vocabulary).Evaluate(flags);
        }
        catch (InvalidOperationException exception)
        {
            throw new InvalidOperationException($"line {number}: {exception.Message}", exception);
        }
    }

    private readonly record struct Branch(int Number, string Line, bool Parent, bool Taken, bool Else);
}
