using CleanArch.Cli.Generators.Shared;

namespace CleanArch.Cli.Generators.AddEvent;

internal static class EventReport
{
    internal static FlagSet Shapes { get; } = new(
        EventFlags.Vocabulary.Generator,
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["consumed"] = "`--consumer`: a feature reacts to the event, and its consumer is written.",
        });

    internal static FlagSet Vocabulary { get; } = ReportFlags.For(EventFlags.Vocabulary, Shapes);

    internal static void Write(TextWriter output, TemplateOverrides templates, EventSpec spec, IReadOnlyList<Emission> emissions, IEnumerable<string> edited, ReportMode mode)
    {
        var tokens = EventPlan.Tokens(spec);

        tokens["Count"] = $"{emissions.Count}";
        tokens["Files"] = ReportLines.Of(emissions.Select(emission => emission.Path));
        tokens["Edited"] = ReportLines.Of(edited.Order(StringComparer.Ordinal));

        templates.WriteReport(output, mode, spec.ConsumerFeatures is null ? [] : ["consumed"], tokens);
    }
}
