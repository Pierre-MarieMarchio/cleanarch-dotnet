using System.Globalization;

namespace CleanArch.Cli.Generators.Shared;

internal static class FieldBlocks
{
    internal static TemplateList For(IReadOnlyList<Field> fields, HashSet<string> flags) =>
        new(
            "field",
            fields,
            new Dictionary<string, Func<Field, string>>(StringComparer.Ordinal)
            {
                ["Name"] = field => field.Name,
                ["Type"] = FieldCode.Type,
                ["Parameter"] = FieldCode.Parameter,
                ["Accessors"] = field => flags.Contains("update") || (flags.Contains("anonymise") && FieldCode.IsAnonymised(field)) ? "{ get; private set; }" : "{ get; }",
                ["Constants"] = field => FieldCode.Joined(FieldCode.ConstantsOf(field), "\n\n"),
                ["Ceiling"] = FieldCode.Ceiling,
                ["IntegerDigits"] = field => FieldCode.IntegerDigits(field).ToString(CultureInfo.InvariantCulture),
                ["Required"] = field => FieldCode.HasRequire(field) ? $"Require{field.Name}({FieldCode.Parameter(field)})" : FieldCode.Parameter(field),
                ["Validated"] = field => FieldCode.HasRequire(field) ? "valid" + field.Name : FieldCode.Parameter(field),
                ["Anonymised"] = field => FieldCode.Anonymised(field) ?? string.Empty,
                ["Sample"] = field => FieldCode.Sample(field, 1),
                ["Require"] = FieldCode.Require,
                ["WireDocumentation"] = FieldCode.WireDocumentation,
                ["WireParameter"] = FieldCode.WireParameter,
                ["Trimmed"] = field => FieldCode.Trimmed(field, "content"),
                ["Stored"] = field => FieldCode.Store(field, "content"),
                ["RecordProperty"] = FieldCode.RecordProperty,
                ["Configuration"] = field => FieldCode.Configuration(field, "%Limits%"),
                ["SortIndex"] = field => FieldCode.SortIndex("%F%", field, flags.Contains("owner")),
                ["CursorKey"] = FieldCode.CursorKey,
                ["KeysetMethod"] = FieldCode.KeysetMethod,
            },
            new Dictionary<string, Func<Field, bool>>(StringComparer.Ordinal)
            {
                ["constants"] = FieldCode.HasConstants,
                ["decimal"] = field => field.Kind == FieldKind.Decimal,
                ["require"] = FieldCode.HasRequire,
                ["anonymised"] = FieldCode.IsAnonymised,
                ["summary"] = FieldCode.InSummary,
                ["string"] = field => field.Kind == FieldKind.String,
                ["sortable"] = FieldCode.IsSortable,
                ["keyset"] = FieldCode.IsKeyset,
            });
}
