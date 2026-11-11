namespace CleanArch.Cli.Generators.Shared;

internal static class UsingOrder
{
    internal static IComparer<string> Directives { get; } =
        Comparer<string>.Create((left, right) => Compare(NamespaceOf(left), NamespaceOf(right)));

    internal static int Compare(string left, string right)
    {
        int system = IsSystem(right).CompareTo(IsSystem(left));

        if (system != 0)
        {
            return system;
        }

        int ignoringCase = StringComparer.OrdinalIgnoreCase.Compare(left, right);

        return ignoringCase != 0 ? ignoringCase : StringComparer.Ordinal.Compare(left, right);
    }

    private static string NamespaceOf(string directive) => directive["using ".Length..].TrimEnd(';');

    private static bool IsSystem(string space) => space == "System" || space.StartsWith("System.", StringComparison.Ordinal);
}
