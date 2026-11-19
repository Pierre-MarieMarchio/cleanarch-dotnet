using System.Globalization;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AppTemplate.Architecture.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace AppTemplate.Architecture.Tests.Rules;

public sealed class ContainerImagePinningTests
{
    private static readonly string[] _dockerfileRoots = ["src", "tools"];

    private static readonly Regex _pinned = new(
        @"^[a-z0-9][a-z0-9._/:-]*:[A-Za-z0-9._-]+@sha256:[0-9a-f]{64}$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Regex _composeImage = new(
        @"^\s+image:\s*(?<ref>\S+)\s*$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly string[] _skippedFolders = ["node_modules", "bin", "obj"];

    private static readonly Regex _composeService = new(
        @"^  (?<name>[a-z0-9][a-z0-9_-]*):\s*$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Regex _from = new(
        @"^\s*FROM\s+(?:--platform=\S+\s+)?(?<ref>\S+)(?:\s+AS\s+(?<alias>\S+))?\s*$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
        TimeSpan.FromSeconds(1));

    private static readonly Regex _syntax = new(
        @"^#\s*syntax\s*=\s*(?<ref>\S+)\s*$",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
        TimeSpan.FromSeconds(1));

    private static readonly Regex _composeInclude = new(
        @"^\s+-\s+(?:path:\s*)?(?<path>\S+compose\.ya?ml)\s*$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Regex _fixtureImage = new(
        @"(?:Builder\(\s*""(?<ref>[^""]+)""|const\s+string\s+\w*Image\w*\s*=\s*""(?<ref>[^""]+)"")",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Regex _dotnetImage = new(
        @"(?<ref>mcr\.microsoft\.com/dotnet/(?<name>[a-z-]+):(?<major>\d+)\.\S*)",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));
    //#if (templateMaintenance)

    private static readonly Regex _include = new(
        @"^(?<indent>\s*)#include\s+(?<path>\S+)\s*$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Regex _newestLine = new(
        @"^\(dotnet == ""net\d+\.0""\)$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Regex _marker = new(
        @"^\s*#(?<keyword>if|elseif|else|endif)\b(?<condition>.*)$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));
    //#endif

    [Fact]
    public void EveryComposeImage_IsPinnedByDigest()
    {
        var images = ComposeImages();

        images.ShouldNotBeEmpty("docker-compose.yml runs at least the mail catcher; a parse that finds none is broken.");

        images
            .Where(image => !_pinned.IsMatch(image))
            .ShouldBeEmpty(
                "Write each image in docker-compose.yml, docker-compose.database.yml and every web app's compose.yml as "
                + "name:tag@sha256:<multi-arch index digest>. "
                + "Resolve it with `docker buildx imagetools inspect name:tag`.");
    }

    [Fact]
    public void EveryDockerfileBase_AndFrontend_IsPinnedByDigest()
    {
        var dockerfiles = Dockerfiles();

        dockerfiles.ShouldNotBeEmpty("the API's Dockerfile exists; a search that finds none is broken.");

        var offenders = dockerfiles.SelectMany(UnpinnedReferences).ToList();

        offenders.ShouldBeEmpty(
            "Write each FROM, and the `# syntax=` frontend, as name:tag@sha256:<digest>, literally: "
            + "Dependabot rewrites a literal FROM and skips one built from an ARG.");
    }

    private static IEnumerable<string> UnpinnedReferences(string dockerfile)
    {
        string[] lines = File.ReadAllLines(dockerfile);
        string path = RepositoryTree.Relative(dockerfile);

        return UnpinnedFrontend(lines).Concat(UnpinnedStages(lines)).Select(line => $"{path}: {line}");
    }

    private static IEnumerable<string> UnpinnedFrontend(string[] lines) =>
        lines.Length > 0 && _syntax.Match(lines[0]) is { Success: true } syntax && !_pinned.IsMatch(syntax.Groups["ref"].Value)
            ? [lines[0]]
            : [];

    private static IEnumerable<string> UnpinnedStages(string[] lines)
    {
        var stages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string line in lines)
        {
            var match = _from.Match(line);

            if (!match.Success)
            {
                continue;
            }

            string reference = match.Groups["ref"].Value;

            if (!stages.Contains(reference) && !_pinned.IsMatch(reference))
            {
                yield return line.Trim();
            }

            if (match.Groups["alias"].Success)
            {
                stages.Add(match.Groups["alias"].Value);
            }
        }
    }

    [Fact]
    public void EveryDotnetImage_RunsTheProjectsDotnetLine()
    {
        int line = ProjectDotnetMajor();
        var images = Dockerfiles()
            .SelectMany(file => DotnetImages(File.ReadLines(file)).Select(image => (File: RepositoryTree.Relative(file), Image: image)))
            .ToList();

        images.ShouldNotBeEmpty("the API's Dockerfile builds on the .NET SDK; a search that finds none is broken.");
        images
            .Where(image => DotnetMajor(image.Image) != line)
            .Select(image => $"{image.File}: {image.Image}")
            .ShouldBeEmpty(
                $"cleanarch.json records net{line}.0, and a project stays on its .NET line: every mcr.microsoft.com/dotnet image runs .NET {line}. "
                + "Moving to another .NET is a migration, which moves the target framework, global.json and every image together.");
    }

    //#if (templateMaintenance)
    [Fact]
    public void EachLinesReferenceImages_ArePinnedOnTheirLine_AndDependabotUpdatesThem()
    {
        string dependabot = File.ReadAllText(Path.Combine(ProjectReferenceGraph.RepositoryRoot, ".github", "dependabot.yml"));
        var lines = Directory.EnumerateDirectories(ImageReferences).Order(StringComparer.Ordinal).ToList();

        lines.ShouldNotBeEmpty("the newest .NET line keeps its images under .template.config/images; a search that finds none is broken.");
        lines.SelectMany(folder => LineProblems(folder, dependabot)).ShouldBeEmpty(
            "Each folder of .template.config/images holds one .NET line's images, pinned as name:tag@sha256:<digest> on that line's major, "
            + "each image at one reference across the folder, and is a docker directory of .github/dependabot.yml, which moves them together.");
    }

    [Fact]
    public void AProjectOnTheNewestLine_KeepsNoImageOfThisCheckoutsLine()
    {
        string root = ProjectReferenceGraph.RepositoryRoot;
        int checkoutLine = ProjectDotnetMajor();
        var images = TemplateFilesNamingADotnetImage()
            .SelectMany(file => DotnetImages(AsNewestLine(File.ReadAllLines(file), root)).Select(image => (File: RepositoryTree.Relative(file), Image: image)))
            .ToList();

        images.ShouldNotBeEmpty("the API's Dockerfile builds on the .NET SDK; a search that finds none is broken.");
        images
            .Where(image => DotnetMajor(image.Image) == checkoutLine || !_pinned.IsMatch(image.Image))
            .Select(image => $"{image.File}: {image.Image}")
            .ShouldBeEmpty(
                $"Generated on the newest .NET line, a file still names a .NET {checkoutLine} image, or one not pinned by digest. Wrap each "
                + "mcr.microsoft.com/dotnet line as #if (dotnet == \"net<N>.0\"), #include ../../.template.config/images/<tfm>/<form>, #else, the line, #endif; "
                + "the path is read from an item template's root, and the project template's include token takes the ../../ off.");
    }

    [Fact]
    public void ThisCheckoutsDotnetImages_AreTheOnesItsLinesFolderPins()
    {
        string tfm = $"net{ProjectDotnetMajor()}.0";
        var pins = Directory.EnumerateFiles(Path.Combine(ImageReferences, tfm))
            .SelectMany(file => DotnetImages(File.ReadLines(file)))
            .DistinctBy(ImageName)
            .ToDictionary(ImageName);
        var images = TemplateFilesNamingADotnetImage()
            .SelectMany(file => DotnetImages(File.ReadLines(file)).Select(image => (File: RepositoryTree.Relative(file), Image: image)))
            .ToList();

        images.ShouldNotBeEmpty("the API's Dockerfile builds on the .NET SDK; a search that finds none is broken.");
        images
            .Where(image => pins.GetValueOrDefault(ImageName(image.Image)) != image.Image)
            .Select(image => $"{image.File}: {image.Image}")
            .ShouldBeEmpty(
                $"Dependabot moves .template.config/images/{tfm} and this checkout's files in one pull request, so each .NET image this checkout "
                + "names is the one that folder pins; a file only generation reads includes the folder's form instead of a copy of the line.");
    }

    private static string ImageName(string image) => _dotnetImage.Match(image).Groups["name"].Value;

    private static string ImageReferences => Path.Combine(ProjectReferenceGraph.RepositoryRoot, ".template.config", "images");

    private static IEnumerable<string> LineProblems(string folder, string dependabot)
    {
        string tfm = Path.GetFileName(folder);
        int major = int.Parse(tfm["net".Length..tfm.IndexOf('.', StringComparison.Ordinal)], CultureInfo.InvariantCulture);
        var images = Directory.EnumerateFiles(folder)
            .SelectMany(file => DotnetImages(File.ReadLines(file)).Select(image => (File: RepositoryTree.Relative(file), Image: image)))
            .ToList();

        if (!dependabot.Contains($"- /.template.config/images/{tfm}\n", StringComparison.Ordinal))
        {
            yield return $".github/dependabot.yml lists no docker directory /.template.config/images/{tfm}";
        }

        foreach (var (file, image) in images.Where(image => !_pinned.IsMatch(image.Image) || DotnetMajor(image.Image) != major))
        {
            yield return $"{file}: {image}";
        }

        foreach (var name in images.GroupBy(image => _dotnetImage.Match(image.Image).Groups["name"].Value).Where(name => name.Select(image => image.Image).Distinct().Count() > 1))
        {
            yield return $"{tfm} names {name.Key} at more than one reference: {string.Join(", ", name.Select(image => image.Image).Distinct())}";
        }
    }

    private static List<string> TemplateFilesNamingADotnetImage() =>
        [.. RepositoryTree.SourceFiles(ProjectReferenceGraph.RepositoryRoot, "*", [.. _skippedFolders, ".git"])
            .Where(file => !RepositoryTree.Relative(file).StartsWith(".template.config/images/", StringComparison.Ordinal))
            .Where(file => Path.GetFileName(file).Contains("Dockerfile", StringComparison.Ordinal) || Path.GetExtension(file) is ".yml" or ".yaml")
            .Where(file => _dotnetImage.IsMatch(File.ReadAllText(file)) || File.ReadLines(file).Any(_include.IsMatch))
            .Order(StringComparer.Ordinal)];

    private static IEnumerable<string> AsNewestLine(IEnumerable<string> lines, string root)
    {
        var branches = new Stack<bool?>();

        foreach (string line in lines)
        {
            bool newestMarker = _marker.Match(line) is { Success: true } marker && NewestMarker(branches, marker);

            if (newestMarker || branches.Contains(false))
            {
                continue;
            }

            if (_include.Match(line) is { Success: true } include)
            {
                foreach (string included in File.ReadLines(Path.GetFullPath(Path.Combine(root, ".template.config", "presentation", include.Groups["path"].Value))))
                {
                    yield return include.Groups["indent"].Value + included;
                }

                continue;
            }

            yield return line;
        }
    }

    private static bool NewestMarker(Stack<bool?> branches, Match marker)
    {
        switch (marker.Groups["keyword"].Value)
        {
            case "if":
                branches.Push(_newestLine.IsMatch(marker.Groups["condition"].Value.Trim()) ? true : null);
                return branches.Peek() is not null;
            case "endif":
                return branches.Pop() is not null;
            default:
                bool? branch = branches.Pop();
                branches.Push(branch is null ? null : false);
                return branch is not null;
        }
    }
    //#endif

    private static int ProjectDotnetMajor()
    {
        string tfm = JsonNode.Parse(File.ReadAllText(Path.Combine(ProjectReferenceGraph.RepositoryRoot, "cleanarch.json")))!["targetFramework"]!.GetValue<string>();

        return int.Parse(tfm["net".Length..tfm.IndexOf('.', StringComparison.Ordinal)], CultureInfo.InvariantCulture);
    }

    private static IEnumerable<string> DotnetImages(IEnumerable<string> lines) =>
        lines.SelectMany(line => _dotnetImage.Matches(line).Select(match => match.Groups["ref"].Value));

    private static int DotnetMajor(string image) =>
        int.Parse(_dotnetImage.Match(image).Groups["major"].Value, CultureInfo.InvariantCulture);

    [Fact]
    public void EveryWebApp_IsIncludedInTheRootComposeFile()
    {
        var included = File.ReadAllLines(RootComposeFile())
            .Select(line => _composeInclude.Match(line))
            .Where(match => match.Success)
            .Select(match => match.Groups["path"].Value)
            .ToHashSet(StringComparer.Ordinal);

        WebAppComposeFiles()
            .Select(RepositoryTree.Relative)
            .Where(file => !included.Contains(file))
            .ShouldBeEmpty(
                "A web app's compose.yml is not under include: in docker-compose.yml, so docker compose up "
                + "never starts it. Add `  - path: src/Web/apps/<Name>/compose.yml` there.");
    }

    [Fact]
    public void EveryFixtureImage_IsPinned_AndIsOneTheDevelopmentStackRuns()
    {
        var compose = ComposeImages().ToHashSet(StringComparer.Ordinal);
        var fixtures = FixtureImages();

        //#if ((api && (serverDatabase || files)) || (localAuth && serverDatabase))
        fixtures.ShouldNotBeEmpty("the integration suites start a container; a search that finds none is broken.");
        //#endif

        fixtures
            .Where(fixture => !_pinned.IsMatch(fixture.Image) || !compose.Contains(fixture.Image))
            .Select(fixture => $"{fixture.File}: {fixture.Image}")
            .ShouldBeEmpty(
                "A fixture runs exactly an image docker-compose.yml runs, tag and digest alike, so the "
                + "store a test exercises is the one a developer has. Copy the image line from there.");
    }

    [Fact]
    public void TheMatchers_AreSensitive()
    {
        const string digest = "sha256:1a0c23d53c1ad77468c8653fa6eed4f4105cfbef9e6f027bc1dfeb16949a8c29";

        _pinned.IsMatch($"postgres:18.6-alpine3.23@{digest}").ShouldBeTrue();
        _pinned.IsMatch($"mcr.microsoft.com/dotnet/sdk:10.0.302-noble@{digest}").ShouldBeTrue();
        _pinned.IsMatch("postgres:18.6-alpine3.23").ShouldBeFalse();
        _pinned.IsMatch($"postgres@{digest}").ShouldBeFalse("the tag is kept for the reader");

        _from.Match("FROM --platform=$BUILDPLATFORM sdk:1 AS build").Groups["alias"].Value.ShouldBe("build");
        _syntax.IsMatch("# syntax=docker/dockerfile:1").ShouldBeTrue();
        UnpinnedFrontend(["# syntax=docker/dockerfile:1", "FROM sdk:1"]).ShouldBe(["# syntax=docker/dockerfile:1"]);
        UnpinnedFrontend([$"# syntax=docker/dockerfile:1@{digest}"]).ShouldBeEmpty();
        UnpinnedFrontend(["FROM sdk:1"]).ShouldBeEmpty();
        _fixtureImage.IsMatch("new ContainerBuilder(\"store:17-alpine\")").ShouldBeTrue();
        _fixtureImage.IsMatch("private const string _storeImage =\n        \"a:b\";").ShouldBeTrue();
        _composeInclude.Match("  - path: src/Web/apps/Site/compose.yml").Groups["path"].Value.ShouldBe("src/Web/apps/Site/compose.yml");
        _composeInclude.IsMatch("  - src/Web/apps/Site/compose.yaml").ShouldBeTrue();
        _composeInclude.IsMatch("  - db").ShouldBeFalse();
        _dotnetImage.Match($"FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:11.0.100-resolute@{digest} AS build").Groups["ref"].Value
            .ShouldBe($"mcr.microsoft.com/dotnet/sdk:11.0.100-resolute@{digest}");
        DotnetMajor($"mcr.microsoft.com/dotnet/aspnet:10.0.15-alpine3.24@{digest}").ShouldBe(10);
        //#if (templateMaintenance)
        AsNewestLine(["a", "#if (dotnet == \"net11.0\")", "b", "#else", "c", "#endif", "#if (x)", "d", "#else", "e", "#endif"], ".")
            .ShouldBe(["a", "b", "#if (x)", "d", "#else", "e", "#endif"]);
        AsNewestLine(["#if (x)", "  #if (dotnet == \"net11.0\")", "b", "  #else", "c", "  #endif", "#endif"], ".").ShouldBe(["#if (x)", "b", "#endif"]);
        //#endif
    }

    private static List<string> ComposeImages() =>
        [.. new[] { RootComposeFile(), DatabaseComposeFile() }
            .Where(File.Exists)
            .Concat(WebAppComposeFiles())
            .SelectMany(ComposeImages)];

    private static string RootComposeFile() => Path.Combine(ProjectReferenceGraph.RepositoryRoot, "docker-compose.yml");

    private static string DatabaseComposeFile() => Path.Combine(ProjectReferenceGraph.RepositoryRoot, "docker-compose.database.yml");

    private static List<string> WebAppComposeFiles()
    {
        string apps = Path.Combine(ProjectReferenceGraph.RepositoryRoot, "src", "Web", "apps");

        return Directory.Exists(apps)
            ? [.. Directory.EnumerateDirectories(apps)
                .Select(app => Path.Combine(app, "compose.yml"))
                .Where(File.Exists)
                .Order(StringComparer.Ordinal)]
            : [];
    }

    private static List<string> ComposeImages(string composeFile)
    {
        var images = new List<string>();
        string? image = null;
        bool built = false;
        bool inServices = false;

        void Flush()
        {
            if (image is not null && !built)
            {
                images.Add(image);
            }

            image = null;
            built = false;
        }

        foreach (string line in File.ReadAllLines(composeFile))
        {
            if (line.Length > 0 && !char.IsWhiteSpace(line[0]) && line[0] != '#')
            {
                Flush();
                inServices = line.StartsWith("services:", StringComparison.Ordinal);
                continue;
            }

            if (!inServices)
            {
                continue;
            }

            if (_composeService.IsMatch(line))
            {
                Flush();
            }
            else if (_composeImage.Match(line) is { Success: true } match)
            {
                image = match.Groups["ref"].Value;
            }
            else if (line.StartsWith("    build:", StringComparison.Ordinal))
            {
                built = true;
            }
        }

        Flush();

        return images;
    }

    private static List<string> Dockerfiles() =>
        [.. _dockerfileRoots
            .Select(directory => Path.Combine(ProjectReferenceGraph.RepositoryRoot, directory))
            .Where(Directory.Exists)
            .SelectMany(directory => RepositoryTree.SourceFiles(directory, "*", _skippedFolders))
            .Where(file => Path.GetFileName(file) is var name
                && (name == "Dockerfile" || name.EndsWith(".Dockerfile", StringComparison.Ordinal)))
            .Order(StringComparer.Ordinal)];

    private static string IntegrationTests => Path.Combine(ProjectReferenceGraph.RepositoryRoot, "tests", "Integration");

    private static List<(string File, string Image)> FixtureImages() =>
        [.. (Directory.Exists(IntegrationTests) ? RepositoryTree.SourceFiles(IntegrationTests, "*.cs", _skippedFolders) : [])
            .SelectMany(file => _fixtureImage
                .Matches(File.ReadAllText(file))
                .Select(match => (RepositoryTree.Relative(file), match.Groups["ref"].Value)))];
}
