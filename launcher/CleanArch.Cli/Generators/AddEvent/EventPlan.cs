using CleanArch.Cli.Generators.Shared;
using static CleanArch.Cli.Generators.Shared.ProjectFolders;

namespace CleanArch.Cli.Generators.AddEvent;

internal static class EventPlan
{
    internal const string TemplateFolder = "add-event";

    private const string _event = "src/Domain/AT.Domain/Features/Placeholders/Model/Events/PlaceholderNotedDomainEvent.cs";

    private const string _eventTests = "tests/Domain/AT.Domain.UnitTests/Features/Placeholders/Model/Events/PlaceholderNotedDomainEventTests.cs";

    private const string _consumer =
        "src/Application/AT.Application/Features/Listeners/Services/Consumers/PlaceholderNoted/NotifyListenersOnPlaceholderNotedConsumer.cs";

    private const string _consumerTests =
        "tests/Application/AT.Application.UnitTests/Features/Listeners/Services/Consumers/PlaceholderNoted/NotifyListenersOnPlaceholderNotedConsumerTests.cs";

    internal static TemplateOverrides Overrides(string root) => TemplateOverrides.Read(root, TemplateFolder, EventFlags.Vocabulary, EventReport.Vocabulary);

    internal static List<Emission> Emissions(EventSpec spec, string root, TemplateOverrides templates)
    {
        var tokens = Tokens(spec);
        var flags = EventFlags.Of(spec);
        var fields = EventBlocks.For(spec.Fields);
        var emissions = new List<Emission>();

        void Write(string reference, bool tests)
        {
            string path = PathOf(spec.Prefix, reference, tokens);

            if (!tests || ProjectExists(root, path))
            {
                emissions.Add(new Emission(path, templates.Render(reference, flags, tokens, fields)));
            }
        }

        Write(_event, tests: false);
        Write(_eventTests, tests: true);

        if (spec.ConsumerFeatures is not null)
        {
            Write(_consumer, tests: false);
            Write(_consumerTests, tests: true);
        }

        return emissions;
    }

    internal static Dictionary<string, string> Tokens(EventSpec spec)
    {
        var tokens = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["P"] = spec.Prefix,
            ["F"] = spec.Features,
            ["E"] = spec.Base,
        };

        if (spec is { ConsumerFeatures: { } consumer, Reaction: { } reaction })
        {
            tokens["C"] = consumer;
            tokens["V"] = reaction;
        }

        return tokens;
    }
}
