namespace CleanArch.Cli.Generators.Shared;

internal static class ReportLines
{
    internal static string Of(IEnumerable<string> lines, string indent = "  ") =>
        string.Join('\n', lines.Select(line => line.Length == 0 ? line : indent + line));
}
