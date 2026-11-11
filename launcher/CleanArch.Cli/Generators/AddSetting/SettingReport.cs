using CleanArch.Cli.Generators.Shared;

namespace CleanArch.Cli.Generators.AddSetting;

internal static class SettingReport
{
    internal static FlagSet Shapes { get; } = new(SettingFlags.Vocabulary.Generator, new Dictionary<string, string>(StringComparer.Ordinal));

    internal static FlagSet Vocabulary { get; } = ReportFlags.For(SettingFlags.Vocabulary, Shapes);

    internal static void Write(TextWriter output, TemplateOverrides templates, SettingSpec spec, IReadOnlyList<Emission> emissions, IEnumerable<string> edited, List<string> hosts, ReportMode mode)
    {
        var tokens = SettingPlan.Tokens(spec);

        tokens["Count"] = $"{emissions.Count}";
        tokens["Files"] = ReportLines.Of(emissions.Select(emission => emission.Path));
        tokens["Edited"] = ReportLines.Of(edited.Order(StringComparer.Ordinal));
        tokens["Hosts"] = string.Join(", ", hosts);
        tokens["Unset"] = ReportLines.Of(spec.Keys.Where(key => key.IsRequired).SelectMany(key => Unset(spec, key)));

        templates.WriteReport(output, mode, SettingFlags.Of(spec), tokens);
    }

    private static string[] Unset(SettingSpec spec, SettingKey key) =>
    [
        $"- {key.Name} has no value in the file: `dotnet user-secrets set {spec.Section}:{key.Name} <value>` locally, or {spec.Variable(key)} in .env for Compose, whose .env.example now lists it empty;",
        "  the host refuses to start without it, and so does any test that starts a host: give it a value there, as ValidSettings in HostComposition does for the architecture tests.",
    ];
}
