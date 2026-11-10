using CleanArch.Cli.Generators.Shared;
using Xunit;

namespace CleanArch.Cli.Tests.Generators;

public sealed class DirectivesTests
{
    private const string _nested = "top\n#if get\nget\n    #if list\nget list\n    #else\nget alone\n    #endif\n#elif list\nlist\n#elif create\ncreate\n#else\nnone\n#endif\nbottom";

    private static readonly HashSet<string> _known = new(StringComparer.Ordinal) { "get", "list", "create" };

    private static readonly (string[] Flags, string Kept)[] _branches =
    [
        (["get", "list"], "top\nget\nget list\nbottom"),
        (["get"], "top\nget\nget alone\nbottom"),
        (["list", "create"], "top\nlist\nbottom"),
        (["create"], "top\ncreate\nbottom"),
        ([], "top\nnone\nbottom"),
    ];

    private static readonly (string Text, string Expected, string Control)[] _refusals =
    [
        ("a\n#else\nb", "line 2: '#else' belongs to no #if", "#if get\na\n#else\nb\n#endif"),
        ("a\n  #elif get\nb", "line 2: '#elif get' belongs to no #if", "#if list\na\n  #elif get\nb\n#endif"),
        ("#endif", "line 1: '#endif' belongs to no #if", "#if get\n#endif"),
        ("#if get\n#else\n#elif list\n#endif", "line 3: '#elif list' follows the #else of its #if", "#if get\n#elif list\n#else\n#endif"),
        ("#if get\n#else\n#else\n#endif", "line 3: '#else' follows the #else of its #if", "#if get\n#else\n#endif"),
        ("#if get\n#if list\n#endif", "line 1: '#if get' is never closed by an #endif", "#if get\n#if list\n#endif\n#endif"),
        ("a\n#if (get && nonsense)\n#endif", "line 2: 'nonsense' is not a known flag", "a\n#if (get && list)\n#endif"),
        ("#if get\n#elif nonsense\n#endif", "line 2: 'nonsense' is not a known flag", "#if get\n#elif list\n#endif"),
        ("#if list\n#if nonsense\n#endif\n#endif", "line 2: 'nonsense' is not a known flag", "#if list\n#if create\n#endif\n#endif"),
        ("#if\n#endif", "line 1: '' ends where a flag", "#if get\n#endif"),
        ("#if get\n#endif get", "line 2: '#endif get' takes nothing after #endif", "#if get\n#endif"),
        ("#if get\n#else list\n#endif", "line 2: '#else list' takes nothing after #else", "#if get\n#else\n#endif"),
    ];

    [Fact]
    public void ABranch_KeepsItsOwnLines_AtEveryDepth() => Expectations.Hold(expect =>
    {
        foreach (var (flags, kept) in _branches)
        {
            string applied = Apply(_nested, flags);

            expect(applied == kept, $"Over [{string.Join(", ", flags)}], the directives kept\n{applied}\nnot\n{kept}");
        }
    });

    [Fact]
    public void ADirective_StandsAtTheStartOfALine_AndAnythingElseIsText() => Expectations.Hold(expect =>
    {
        const string text = "int x = 1; #if get\n// #endif\n#ifdef get\n#nullable enable\n#region list\n\"#else\"";

        expect(Apply(text, []) == text, "A line that does not start with #if, #elif, #else or #endif was read as a directive, or dropped.");
    });

    [Fact]
    public void ADirective_RefusesAMisplacedOrUnreadableLine_NamingItsNumberAndText() => Expectations.Hold(expect =>
    {
        foreach (var (text, expected, control) in _refusals)
        {
            expect(Refusal(text) is { } message && message.Contains(expected, StringComparison.Ordinal), $"{expected}: the text was applied, or refused for another reason: {Refusal(text)}");
            expect(Refusal(control) is null, $"The control of '{expected}' was refused: {Refusal(control)}");
        }
    });

    private static string Apply(string text, string[] flags) =>
        Directives.Apply(text, new HashSet<string>(flags, StringComparer.Ordinal), _known);

    private static string? Refusal(string text)
    {
        try
        {
            Apply(text, ["get"]);

            return null;
        }
        catch (InvalidOperationException exception)
        {
            return exception.Message;
        }
    }
}
