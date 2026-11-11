using System.CommandLine;
using CleanArch.Cli.Generators.Shared;

namespace CleanArch.Cli.CodeTemplates;

internal static class TemplatesCommand
{
    public const string Name = "templates";

    internal static IReadOnlyDictionary<string, TemplatedGenerator> Generators { get; } = new Dictionary<string, TemplatedGenerator>(StringComparer.Ordinal)
    {
        ["add-feature"] = new(project => GeneratorCommand.AuthenticationRefusal(project, "feature"), "add feature and add field now write"),
        ["add-use-case"] = new(_ => null, "add use-case now writes"),
    };

    public static readonly CommandRegistration Registration = new(ProjectCommand.Requiring(Refusal), Build);

    private static string? Refusal(LocatedProject project)
    {
        string?[] refusals = [.. Generators.Values.Select(generator => generator.Refusal(project))];

        return refusals.All(refusal => refusal is not null)
            ? $"This project has no {Name} command, as no generator that writes from code templates applies to it. {string.Join(' ', refusals)}"
            : null;
    }

    private static Command Build(CommandContext context)
    {
        var templates = new Command(Name, $"Copy a generator's code templates into {ToolTemplates.Folder}/, which it then writes its files from.");
        templates.Add(Copy(context));
        templates.SetAction(GeneratorCommand.ShowHelp);
        return templates;
    }

    private static Command Copy(CommandContext context)
    {
        var copy = new Command("copy", $"Copy a generator's templates into {ToolTemplates.Folder}/ and record their source in {TemplateCopies.Manifest}; none is ever overwritten.");
        Argument<string> generator = new("generator") { Description = $"The generator whose templates to copy: {string.Join(", ", Generators.Keys)}." };
        generator.CompletionSources.Add([.. Generators.Keys]);
        Argument<string[]> named = new("template")
        {
            Description = "A template's path in the reference feature, or its end after a /, such as PlaceholdersController.cs; every template by default.",
            Arity = ArgumentArity.ZeroOrMore,
        };
        Option<bool> dryRun = GeneratorCommand.DryRun("Print the templates the command would copy, and write nothing.");
        copy.Arguments.Add(generator);
        copy.Arguments.Add(named);
        copy.Options.Add(dryRun);
        if (context.Project is { } project)
        {
            copy.SetAction(parseResult => TemplateCopy.Run(
                project,
                new TemplateCopyRequest(parseResult.GetRequiredValue(generator), parseResult.GetValue(named) ?? [], parseResult.GetValue(dryRun)),
                parseResult.InvocationConfiguration.Output,
                parseResult.InvocationConfiguration.Error));
        }

        return copy;
    }
}

internal sealed record TemplatedGenerator(Func<LocatedProject, string?> Refusal, string Writes);
