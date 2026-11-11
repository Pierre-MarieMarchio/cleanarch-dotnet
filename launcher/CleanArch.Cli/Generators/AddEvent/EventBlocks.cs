using CleanArch.Cli.Generators.Shared;

namespace CleanArch.Cli.Generators.AddEvent;

internal static class EventBlocks
{
    private sealed record EventField(Field Field, int Index);

    internal static TemplateList For(IReadOnlyList<Field> fields) =>
        TemplateList.Of(
            "field",
            fields.Select((field, index) => new EventField(field, index)),
            new Dictionary<string, Func<EventField, string>>(StringComparer.Ordinal)
            {
                ["Name"] = item => item.Field.Name,
                ["Type"] = item => item.Field.ClrType,
                ["Sample"] = Sample,
            },
            new Dictionary<string, Func<EventField, bool>>(StringComparer.Ordinal)
            {
                ["guid"] = item => item.Field.Kind == FieldKind.Guid,
            });

    private static string Sample(EventField item) => item.Field.Kind switch
    {
        FieldKind.Guid => $"guid{item.Index}",
        FieldKind.String => $"\"{CSharpNames.Separated(item.Field.Name, ' ')}\"",
        FieldKind.Int => "1",
        FieldKind.Long => "1L",
        FieldKind.Bool => "true",
        FieldKind.Decimal => "1m",
        FieldKind.DateOnly => "new DateOnly(2026, 1, 1)",
        _ => "new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)",
    };
}
