using System.Globalization;
using System.Text.RegularExpressions;
using CleanArch.Tooling.Failures;

namespace CleanArch.Cli.UpgradeReport;

internal sealed partial record DotnetLine(string TargetFramework, int DotnetMajor)
{
    public static DotnetLine Of(string targetFramework)
    {
        Match match = TargetFrameworkPattern().Match(targetFramework);

        return match.Success
            ? new DotnetLine(targetFramework, int.Parse(match.Groups["major"].Value, CultureInfo.InvariantCulture))
            : throw new LauncherException(
                $"cleanarch.json records the target framework '{targetFramework}', which is not one the template generates, such as net10.0.");
    }

    public bool IsLongTermSupport => DotnetMajor % 2 == 0;

    public bool IsGeneratedBy(string version)
    {
        int templateMajor = SemanticVersion.Major(version);

        return IsLongTermSupport ? templateMajor < NextLongTermSupport : templateMajor == DotnetMajor;
    }

    private int NextLongTermSupport => DotnetMajor + 2;

    [GeneratedRegex(@"^net(?<major>\d+)\.0$")]
    private static partial Regex TargetFrameworkPattern();
}
