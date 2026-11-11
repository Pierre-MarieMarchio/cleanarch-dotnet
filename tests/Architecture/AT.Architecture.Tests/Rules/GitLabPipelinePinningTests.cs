using System.Text.RegularExpressions;
using AppTemplate.Architecture.Tests.Fixtures;
using Shouldly;
using Xunit;

namespace AppTemplate.Architecture.Tests.Rules;

public sealed class GitLabPipelinePinningTests
{
    private const string _sdkImage = "mcr.microsoft.com/dotnet/sdk:";

    private static readonly string[] _scannerTemplates = ["Jobs/SAST.gitlab-ci.yml", "Jobs/Secret-Detection.gitlab-ci.yml"];

    private static readonly string[] _dockerfileRoots = ["src", "tools"];

    private static readonly string[] _skippedFolders = ["node_modules", "bin", "obj"];

    private static readonly TimeSpan _budget = TimeSpan.FromSeconds(1);

    private static readonly Regex _pinnedImage = new(@"^[a-z0-9][a-z0-9._/:-]*:[A-Za-z0-9._-]+@sha256:[0-9a-f]{64}$", RegexOptions.None, _budget);

    private static readonly Regex _imageKey = new(@"^(?<indent>\s*)(?:-\s+)?image:\s*(?<ref>[^\s#][^#]*?)?\s*(?:#.*)?$", RegexOptions.None, _budget);

    private static readonly Regex _servicesKey = new(@"^(?<indent>\s*)services:\s*$", RegexOptions.None, _budget);

    private static readonly Regex _includeKey = new(@"^include:\s*(?<value>[^\s#][^#]*?)?\s*(?:#.*)?$", RegexOptions.None, _budget);

    private static readonly Regex _listItem = new(@"^\s*-\s+(?:(?<key>[a-z_]+):(?:\s+|$))?(?<value>[^\s#][^#]*?)?\s*(?:#.*)?$", RegexOptions.None, _budget);

    private static readonly Regex _nameKey = new(@"^\s*name:\s*(?<ref>[^\s#][^#]*?)\s*(?:#.*)?$", RegexOptions.None, _budget);

    private static readonly Regex _from = new(@"^\s*FROM\s+(?:--platform=\S+\s+)?(?<ref>\S+)", RegexOptions.IgnoreCase, _budget);

    /*#if (templateMaintenance)*/
    private static string Pipeline => Path.Combine(
        ProjectReferenceGraph.RepositoryRoot, ".template.config", "variants", "code-host-gitlab", ".gitlab-ci.yml");
    /*#else
    private static string Pipeline => Path.Combine(ProjectReferenceGraph.RepositoryRoot, ".gitlab-ci.yml");
    #endif*/

    [Fact]
    public void EveryImageThePipelineRuns_IsPinnedByDigest()
    {
        var images = Images(PipelineLines()).ToList();

        images.ShouldContain(image => image.Reference.StartsWith(_sdkImage, StringComparison.Ordinal), "The build job runs on the .NET SDK image; a read of .gitlab-ci.yml that finds none is broken.");
        images
            .Where(image => !_pinnedImage.IsMatch(image.Reference))
            .Select(image => $"{image.Where}: {image.Reference}")
            .ShouldBeEmpty("Write each image a job runs, its image: and every services: entry, as name:tag@sha256:<digest>; resolve it with `docker buildx imagetools inspect name:tag`.");
    }

    [Fact]
    public void EveryInclude_IsLocal_OrOneOfGitLabsScannerTemplates()
    {
        var includes = Includes(PipelineLines()).ToList();

        includes.ShouldContain(include => include.Kind == "template", "The pipeline includes GitLab's SAST template; a read of .gitlab-ci.yml that finds no include is broken.");
        includes
            .Where(include => !IsAllowed(include.Kind, include.Value))
            .Select(include => $"{include.Where}: {include.Kind}: {include.Value}")
            .ShouldBeEmpty(
                "A pipeline includes its own files with local:, and GitLab's SAST and Secret Detection templates, which follow the instance's version; "
                + "a remote file, another project's or a component cannot be pinned on every instance.");
    }

    [Fact]
    public void TheSdkImage_IsTheOneTheDockerfilesBuildWith()
    {
        string root = ProjectReferenceGraph.RepositoryRoot;
        var pipeline = Images(PipelineLines()).Where(image => image.Reference.StartsWith(_sdkImage, StringComparison.Ordinal)).Select(image => image.Reference).Distinct().ToList();
        var dockerfiles = _dockerfileRoots
            .Select(folder => Path.Combine(root, folder))
            .Where(Directory.Exists)
            .SelectMany(folder => RepositoryTree.SourceFiles(folder, "*Dockerfile", _skippedFolders))
            .SelectMany(file => File.ReadLines(file)
                .Select(line => _from.Match(line))
                .Where(match => match.Success && match.Groups["ref"].Value.StartsWith(_sdkImage, StringComparison.Ordinal))
                .Select(match => (File: RepositoryTree.Relative(file), Reference: match.Groups["ref"].Value)))
            .ToList();

        pipeline.ShouldHaveSingleItem("The pipeline's .NET jobs run one SDK image.");
        dockerfiles
            .Where(dockerfile => dockerfile.Reference != pipeline[0])
            .Select(dockerfile => $"{dockerfile.File}: {dockerfile.Reference}")
            .ShouldBeEmpty($"The pipeline builds and tests with {pipeline[0]}; move it and every Dockerfile's SDK together, so the gate judges the build the images make.");
    }

    [Fact]
    public void TheMatchers_AreSensitive()
    {
        const string digest = "sha256:08a98015fce594af7c382883bea080ea206de74402a95da9dc52b6428c8cd121";

        Images(["job:", $"  image: docker:29@{digest} # the daemon"]).Single().Reference.ShouldBe($"docker:29@{digest}");
        Images(["job:", "  image:", "    name: alpine:3", "    entrypoint: ['']"]).Single().Reference.ShouldBe("alpine:3");
        Images(["job:", "  services:", "    - name: docker:29-dind", "      alias: docker", "    - postgres:18", "  script:", "    - name: not-an-image"])
            .Select(image => image.Reference).ShouldBe(["docker:29-dind", "postgres:18"]);
        Images(["job:", "  services: []"]).ShouldBeEmpty("an empty list runs no image");
        _pinnedImage.IsMatch($"docker:29.9.3-dind@{digest}").ShouldBeTrue();
        _pinnedImage.IsMatch("docker:29.9.3-dind").ShouldBeFalse();
        _pinnedImage.IsMatch($"docker@{digest}").ShouldBeFalse("the tag is kept for the reader");
        _pinnedImage.IsMatch("$IMAGE").ShouldBeFalse("an image from a variable is pinned by nothing in the file");

        Includes(["include:", "  - template: Jobs/SAST.gitlab-ci.yml", "  - local: ci/build.yml", "  - remote: https://example.com/ci.yml", "  - project: group/ci", "    file: ci.yml", "stages:", "  - test"])
            .Select(include => $"{include.Kind}={include.Value}")
            .ShouldBe(["template=Jobs/SAST.gitlab-ci.yml", "local=ci/build.yml", "remote=https://example.com/ci.yml", "project=group/ci"]);
        Includes(["include: ci/build.yml"]).Single().Kind.ShouldBe("local");
        Includes(["include:", "  - https://example.com/ci.yml"]).Single().Kind.ShouldBe("remote");
        IsAllowed("template", "Jobs/SAST.gitlab-ci.yml").ShouldBeTrue();
        IsAllowed("template", "Jobs/Secret-Detection.gitlab-ci.yml").ShouldBeTrue();
        IsAllowed("template", "Jobs/Dependency-Scanning.gitlab-ci.yml").ShouldBeFalse();
        IsAllowed("local", "ci/build.yml").ShouldBeTrue();
        IsAllowed("remote", "https://example.com/ci.yml").ShouldBeFalse();
        IsAllowed("component", "gitlab.com/components/sast/sast@1.0.0").ShouldBeFalse();
        IsAllowed("project", "group/ci").ShouldBeFalse();
    }

    private static bool IsAllowed(string kind, string value) =>
        kind == "local" || (kind == "template" && _scannerTemplates.Contains(value, StringComparer.Ordinal));

    private static string[] PipelineLines()
    {
        File.Exists(Pipeline).ShouldBeTrue($"{Pipeline} is missing: the project's gate runs from it.");
        return File.ReadAllLines(Pipeline);
    }

    private static IEnumerable<(string Where, string Reference)> Images(string[] lines)
    {
        int? services = null;
        int? image = null;

        foreach (var (line, number) in Content(lines))
        {
            int indent = line.Length - line.TrimStart().Length;
            services = indent > services ? services : null;
            image = indent > image ? image : null;

            if (_imageKey.Match(line) is { Success: true } key)
            {
                if (key.Groups["ref"].Success)
                {
                    yield return ($"line {number}", Unquoted(key.Groups["ref"].Value));
                }
                else
                {
                    image = key.Groups["indent"].Length;
                }
            }
            else if (_servicesKey.Match(line) is { Success: true } list)
            {
                services = list.Groups["indent"].Length;
            }
            else if (services is not null && _listItem.Match(line) is { Success: true } item && item.Groups["value"].Success
                && (!item.Groups["key"].Success || item.Groups["key"].Value == "name"))
            {
                yield return ($"line {number}", Unquoted(item.Groups["value"].Value));
            }
            else if (image is not null && _nameKey.Match(line) is { Success: true } name)
            {
                yield return ($"line {number}", Unquoted(name.Groups["ref"].Value));
            }
        }
    }

    private static IEnumerable<(string Where, string Kind, string Value)> Includes(string[] lines)
    {
        bool inside = false;

        foreach (var (line, number) in Content(lines))
        {
            if (!char.IsWhiteSpace(line[0]))
            {
                inside = false;

                if (_includeKey.Match(line) is { Success: true } key)
                {
                    if (key.Groups["value"].Success)
                    {
                        string value = Unquoted(key.Groups["value"].Value);
                        yield return ($"line {number}", Kind(value), value);
                    }
                    else
                    {
                        inside = true;
                    }
                }
            }
            else if (inside && _listItem.Match(line) is { Success: true } item && item.Groups["value"].Success)
            {
                string value = Unquoted(item.Groups["value"].Value);
                yield return ($"line {number}", item.Groups["key"].Success ? item.Groups["key"].Value : Kind(value), value);
            }
        }
    }

    private static string Kind(string path) => path.Contains("://", StringComparison.Ordinal) ? "remote" : "local";

    private static IEnumerable<(string Line, int Number)> Content(string[] lines) =>
        lines
            .Select((line, index) => (Line: line.TrimEnd(), Number: index + 1))
            .Where(entry => entry.Line.Length > 0 && !entry.Line.TrimStart().StartsWith('#'));

    private static string Unquoted(string value) => value.Trim().Trim('\'', '"');
}
