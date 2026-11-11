using CleanArch.Cli.Generators.Shared;

namespace CleanArch.Cli.Generators.AddEvent;

internal sealed class EventGeneration(EventSpec spec, GenerationContext context) : FileGeneration(context, spec.Features, spec.Manifest)
{
    protected override string Usage => NewEvent.Usage;

    protected override TemplateOverrides Templates { get; } = EventPlan.Overrides(context.Root);

    protected override IReadOnlyList<Emission> Plan() => EventPlan.Emissions(spec, Root, Templates);

    protected override TextEdits.Editor Edit(List<string> problems) => EventEdits.Edits(spec, Root, problems);

    protected override void Report(ReportMode mode)
    {
        EventReport.Write(Output, spec, Emissions, Editor.Changed.Select(sheet => sheet.Path), mode == ReportMode.DryRun);
    }
}
