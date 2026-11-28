using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CleanArch.Tooling.Processes;

namespace CleanArch.Create.Commands.NewProject;

internal static partial class SdkLine
{
    public const string Symbol = "dotnet";

    [GeneratedRegex(@"^net(?<major>\d+)\.0$")]
    private static partial Regex Framework();

    public static void WarnWhenMissing(Session session, string? framework)
    {
        if (Framework().Match(framework ?? string.Empty) is not { Success: true } match)
        {
            return;
        }

        string major = match.Groups["major"].Value;
        if (InstalledMajors(session.Machine) is { Count: > 0 } installed && !installed.Contains(major))
        {
            session.Conversation.Warn(
                $"The .NET {major} SDK isn't installed, and the project builds on it: install it from https://dotnet.microsoft.com/download/dotnet/{major}.0 before building.");
        }
    }

    private static HashSet<string> InstalledMajors(Machine machine)
    {
        ProcessResult listed = machine.Processes.Run(
            new ProcessCall("dotnet", machine.WorkingDirectory, ["--list-sdks"], ReadOnlyDictionary<string, string>.Empty),
            null);

        return listed.ExitCode != 0
            ? []
            : [.. listed.Output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(line => line.Split('.')[0])];
    }
}
