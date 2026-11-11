using CleanArch.Cli.Generators.Shared;
using Microsoft.CodeAnalysis;
using static CleanArch.Cli.Generators.Shared.CSharpNames;
using static CleanArch.Cli.Generators.Shared.FeatureCode;

namespace CleanArch.Cli.Generators.AddField;

internal static class FieldReport
{
    internal const string TemplateFolder = "add-field";

    internal static FlagSet Shapes { get; } = new(
        FeatureFlags.Vocabulary.Generator,
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["sorted"] = "The list sorts by a field added: `list` is among the operations, and a field added is sortable.",
        });

    internal static FlagSet Vocabulary { get; } = ReportFlags.For(FeatureFlags.Vocabulary, Shapes);

    internal static TemplateOverrides Overrides(string root) => TemplateOverrides.Read(root, TemplateFolder, FeatureFlags.Vocabulary, Vocabulary);

    internal static void Write(TextWriter output, TemplateOverrides templates, string root, Addition addition, List<FieldChange> changes, ReportMode mode)
    {
        var spec = addition.After;
        var tokens = NameTokens(spec);
        var flags = Flags(spec);
        string[] sortable = spec.Has("list") ? [.. addition.Added.Where(FieldCode.IsSortable).Select(field => field.Name)] : [];

        tokens["Added"] = string.Join(", ", addition.Added.Select(field => field.Declared));
        tokens["Options"] = Described(spec);
        tokens["Count"] = $"{changes.Count}";
        tokens["Changes"] = string.Join('\n', changes.SelectMany(change => change.Members.Select(member => $"      {member}").Prepend($"  {change.Path}")));
        tokens["Manifest"] = ManifestPath(spec);
        tokens["Compatibility"] = string.Join("\n\n", addition.Added.Select(field => Compatibility(spec, field)));
        tokens["Sortable"] = string.Join(", ", sortable);
        tokens["Steps"] = ReportLines.Of(FieldGeneration.FieldSteps(root, addition).Select(step => step.Display));

        if (sortable.Length > 0)
        {
            flags.Add("sorted");
        }

        templates.WriteReport(output, mode, flags, tokens);
    }

    internal static string Compatibility(FeatureSpec spec, Field field)
    {
        string route = $"/api/v1/{Kebab(spec.Features)}";
        string item = $"{route}/{{{CSharpNames.Lower(spec.Type)}Id}}";
        string create = spec.HasApi ? $"POST {route}" : "the create operation";
        string update = spec.HasApi ? $"PUT {item}" : "the update operation";
        string[] writes =
        [
            .. spec.Has("create") ? [create] : Array.Empty<string>(),
            .. spec.Has("update") ? [update] : Array.Empty<string>(),
        ];

        if (writes.Length == 0)
        {
            return $"{field.Name}: only read, so it adds a member to what the feature returns and breaks no caller.";
        }

        if (!field.Nullable)
        {
            return spec.HasApi
                ? $"Breaking change: {field.Name} is required, so {string.Join(" and ", writes)} answer 400 to a request without it from now "
                    + "on, and every client that does not send it yet breaks. Release the clients with the API, or declare the field "
                    + $"nullable ({(field with { Nullable = true }).Declaration}), which breaks none. The rows already stored take {field.Default!.Literal}."
                : $"Breaking change: {field.Name} is required, so every caller of {string.Join(" and ", writes)} passes it from now on. "
                    + $"The rows already stored take {field.Default!.Literal}.";
        }

        return $"{field.Name} is nullable: a {(spec.HasApi ? "request" : "caller")} may leave it out, so none breaks; the rows already stored hold null"
            + (spec.Has("update")
                ? $". An update replaces the whole {Separated(spec.Type, ' ')}, so one that leaves {field.Name} out stores null: a client that does not know the field yet clears it."
                : ".");
    }
}
