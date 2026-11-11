using System.Globalization;

namespace CleanArch.Cli.Generators.AddSetting;

internal static class KeyCode
{
    internal static string Default(SettingKey key) => key.IsRequired ? "string.Empty" : key.DefaultLiteral;

    internal static string Condition(SettingKey key) => key.Kind switch
    {
        KeyKind.Int or KeyKind.Long => $"options.{key.Name} is < {key.Bound(key.Min)} or > {key.Bound(key.Max)}",
        KeyKind.Duration => $"options.{key.Name} < {key.Literal(key.Min)} || options.{key.Name} > {key.Literal(key.Max)}",
        KeyKind.Text => $"options.{key.Name}.Length is < {key.Min} or > {key.Max}",
        _ => $"string.IsNullOrWhiteSpace(options.{key.Name})",
    };

    internal static string Message(SettingKey key) => key.Kind switch
    {
        KeyKind.Int or KeyKind.Long or KeyKind.Duration => $"must be between {key.Bound(key.Min)} and {key.Bound(key.Max)}.",
        KeyKind.Text => $"must be between {key.Min} and {key.Max} characters{(key.IsRequired ? ", and has no default" : string.Empty)}.",
        _ => "is required and has no default: supply it as the environment variable {section}__" + key.Name + ", never in a tracked file.",
    };

    internal static string PassingCases(SettingKey key) =>
        key.Kind == KeyKind.Secret ? "    [Fact]" : "    [Theory]\n" + Cases([Number(key, key.Min), Number(key, key.Max)]);

    internal static string Passing(SettingKey key) => key.Kind == KeyKind.Secret ? "WhenPresent" : "AtItsBounds";

    internal static string PassingParameter(SettingKey key) => key.Kind == KeyKind.Secret ? string.Empty : Parameter(key);

    internal static string PassingValue(SettingKey key) => key.Kind == KeyKind.Secret ? "\"a-secret\"" : Value(key);

    internal static string FailingCases(SettingKey key) => Cases(key.Kind switch
    {
        KeyKind.Secret => ["\"\"", "\"   \""],
        KeyKind.Text when key.Min < 1 => [Number(key, key.Max + 1)],
        _ => [Number(key, key.Min - 1), Number(key, key.Max + 1)],
    });

    internal static string Failing(SettingKey key) => key.Kind == KeyKind.Secret ? "WhenBlank" : "PastItsBounds";

    internal static string Parameter(SettingKey key) => key.Kind switch
    {
        KeyKind.Int => "int value",
        KeyKind.Long => "long value",
        KeyKind.Duration => "int seconds",
        KeyKind.Secret => "string value",
        _ => "int length",
    };

    internal static string Value(SettingKey key) => key.Kind switch
    {
        KeyKind.Duration => "TimeSpan.FromSeconds(seconds)",
        KeyKind.Text => "new string('a', length)",
        _ => "value",
    };

    internal static string Valid(SettingKey key) => key.Kind == KeyKind.Secret ? "\"a-secret\"" : $"new string('a', {key.Min})";

    internal static string Setting(SettingKey key) => key.Kind switch
    {
        KeyKind.Secret => "\"a-secret\"",
        KeyKind.Text => $"new string('b', {OtherLength(key)})",
        KeyKind.Bool => key.Default == "true" ? "\"false\"" : "\"true\"",
        _ => $"\"{key.Bound(OtherNumber(key))}\"",
    };

    internal static string Expected(SettingKey key) => key.Kind switch
    {
        KeyKind.Secret => "\"a-secret\"",
        KeyKind.Text => $"new string('b', {OtherLength(key)})",
        KeyKind.Bool => key.Default == "true" ? "false" : "true",
        KeyKind.Duration => SettingKey.SpanExpression(OtherNumber(key)),
        KeyKind.Long => Num(OtherNumber(key)) + "L",
        _ => Num(OtherNumber(key)),
    };

    internal static string BrokenValue(SettingKey broken) => broken.Kind switch
    {
        KeyKind.Int or KeyKind.Long or KeyKind.Duration => $"\"{broken.Bound(broken.Max + 1)}\"",
        KeyKind.Text => $"new string('a', {broken.Max + 1})",
        _ => "string.Empty",
    };

    private static string Cases(string[] values) => string.Join('\n', values.Select(value => $"    [InlineData({value})]"));

    private static string Number(SettingKey key, long value) => Num(value) + (key.Kind == KeyKind.Long ? "L" : string.Empty);

    private static string Num(long value) => value.ToString(CultureInfo.InvariantCulture);

    private static long OtherNumber(SettingKey key) => long.Parse(key.Default!, CultureInfo.InvariantCulture) == key.Min ? key.Max : key.Min;

    private static long OtherLength(SettingKey key) => key.Default is { } value && value.Length == key.Min ? key.Max : key.Min;
}
