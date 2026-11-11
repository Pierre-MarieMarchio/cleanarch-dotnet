using CleanArch.Cli.Generators.Shared;

namespace CleanArch.Cli.Generators.AddEvent;

internal sealed class EventGeneration(EventSpec spec, GenerationContext context) : FileGeneration(context, spec.Features, spec.Manifest)
{
    private readonly TemplateOverrides _templates = EventPlan.Overrides(context.Root);

    protected override string Usage => NewEvent.Usage;

    protected override IEnumerable<string> Warnings => TemplateCopies.Stale(Root, _templates);

    protected override IReadOnlyList<Emission> Plan() => EventPlan.Emissions(spec, Root, _templates);

    protected override bool Draft(List<string> problems)
    {
        bool drafted = base.Draft(problems);

        problems.AddRange(_templates.Problems);

        return drafted && _templates.Problems.Count == 0;
    }

    protected override TextEdits.Editor Edit(List<string> problems) => EventEdits.Edits(spec, Root, problems);

    protected override void Report(ReportMode mode)
    {
        EventReport.Write(Output, spec, Emissions, Editor.Changed.Select(sheet => sheet.Path), mode == ReportMode.DryRun);
        _templates.ReportUsed(Output, mode);
    }
}
