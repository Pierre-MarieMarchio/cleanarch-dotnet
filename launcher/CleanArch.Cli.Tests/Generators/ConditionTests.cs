using CleanArch.Cli.Generators.Shared;
using Xunit;

namespace CleanArch.Cli.Tests.Generators;

public sealed class ConditionTests
{
    private static readonly FlagSet _known = new("a test", new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["get"] = "Get.",
        ["list"] = "List.",
        ["create"] = "Create.",
    });

    private static readonly (string Text, string[] Flags, bool Holds)[] _readings =
    [
        ("get", ["get"], true),
        ("get", [], false),
        ("!get", [], true),
        ("!get", ["get"], false),
        ("!!get", ["get"], true),
        ("get || list && create", ["get"], true),
        ("get || list && create", ["list"], false),
        ("list && create || get", ["get"], true),
        ("get&&list", ["get", "list"], true),
        ("(get) && (list)", ["get", "list"], true),
        ("(get) && (list)", ["get"], false),
        ("((get || list)) && !create", ["list"], true),
        ("((get || list)) && !create", ["list", "create"], false),
        ("!(get || list)", [], true),
        ("!(get || list)", ["list"], false),
        ("  ( get||list )  ", ["list"], true),
    ];

    private static readonly (string Text, string Expected, string Control)[] _refusals =
    [
        ("get && nonsense", "'nonsense' is not a flag of a test in 'get && nonsense'", "get && list"),
        ("DEBUG", "'DEBUG' is not a flag of a test", "get"),
        ("", "'' ends where a flag", "get"),
        ("get ||", "'get ||' ends where a flag", "get || list"),
        ("!", "'!' ends where a flag", "!get"),
        ("(get", "'(get' ends where '&&', '||' or ')' belongs", "(get)"),
        ("get)", "')' stands where '&&', '||' or the end belongs", "(get)"),
        ("(get list)", "'list' stands where '&&', '||' or ')' belongs", "(get && list)"),
        ("get list", "'list' stands where '&&', '||' or the end belongs", "get && list"),
        ("get & list", "'&' stands where", "get && list"),
        ("get | list", "'|' stands where", "get || list"),
        ("get == list", "'==' stands where", "get || list"),
        ("get && || list", "'||' stands where a flag", "get && list"),
    ];

    [Fact]
    public void ACondition_ReadsAsCSharpDoes_AndBindsAndTighterThanOr() => Expectations.Hold(expect =>
    {
        foreach (var (text, flags, holds) in _readings)
        {
            expect(
                Condition.Parse(text, _known).Evaluate(new HashSet<string>(flags, StringComparer.Ordinal)) == holds,
                $"'{text}' over [{string.Join(", ", flags)}] did not read {holds}.");
        }
    });

    [Fact]
    public void ACondition_RefusesWhatItCannotRead_NamingTheOffendingText() => Expectations.Hold(expect =>
    {
        foreach (var (text, expected, control) in _refusals)
        {
            expect(Refusal(text) is { } message && message.Contains(expected, StringComparison.Ordinal), $"'{text}' was read, or refused without saying {expected}: {Refusal(text)}");
            expect(Refusal(control) is null, $"'{control}', the control of '{text}', was refused: {Refusal(control)}");
        }
    });

    private static string? Refusal(string text)
    {
        try
        {
            Condition.Parse(text, _known);

            return null;
        }
        catch (InvalidOperationException exception)
        {
            return exception.Message;
        }
    }
}
