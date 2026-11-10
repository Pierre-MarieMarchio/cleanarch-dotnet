using System.Globalization;

namespace CleanArch.Cli.Generators.Shared;

internal static class FieldBlocks
{
    internal static TemplateList For(IReadOnlyList<Field> fields) =>
        new(
            "field",
            fields,
            new Dictionary<string, Func<Field, string>>(StringComparer.Ordinal)
            {
                ["Name"] = field => field.Name,
                ["Type"] = FieldCode.Type,
                ["Parameter"] = FieldCode.Parameter,
                ["AnonymisedAccessors"] = field => FieldCode.IsAnonymised(field) ? "{ get; private set; }" : "{ get; }",
                ["Constants"] = field => FieldCode.Joined(FieldCode.ConstantsOf(field), "\n\n"),
                ["Ceiling"] = FieldCode.Ceiling,
                ["IntegerDigits"] = field => FieldCode.IntegerDigits(field).ToString(CultureInfo.InvariantCulture),
                ["Required"] = field => FieldCode.HasRequire(field) ? $"{FieldCode.RequireMethod(field)}({FieldCode.Parameter(field)})" : FieldCode.Parameter(field),
                ["Validated"] = field => FieldCode.HasRequire(field) ? "valid" + field.Name : FieldCode.Parameter(field),
                ["Anonymised"] = field => FieldCode.Anonymised(field) ?? string.Empty,
                ["Sample"] = field => FieldCode.Sample(field, 1),
                ["Require"] = FieldCode.Require,
                ["WireDocumentation"] = FieldCode.WireDocumentation,
                ["WireParameter"] = FieldCode.WireParameter,
                ["Trimmed"] = FieldCode.Trimmed,
                ["Stored"] = FieldCode.Stored,
                ["RecordProperty"] = FieldCode.RecordProperty,
                ["Configuration"] = FieldCode.Configuration,
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
