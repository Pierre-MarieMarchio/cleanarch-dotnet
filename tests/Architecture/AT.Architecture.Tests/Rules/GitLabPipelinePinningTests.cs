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

    private static readonly Regex _optionImage = new(@"(?:^|[\s,=])(?:image|generator)=(?<ref>[^\s,'""]+)", RegexOptions.None, _budget);

    private static readonly Regex _dockerRun = new(@"(?:^|[\s;&|(])docker\s+run\s+(?<rest>.*)$", RegexOptions.None, _budget);

    private static readonly Regex _existsKey = new(@"^(?<indent>\s*)(?:-\s+)?exists:\s*$", RegexOptions.None, _budget);

    private static readonly Regex _nodeArchive = new(@"^\s*node=v(?<version>\S+)\s*$", RegexOptions.None, _budget);

    /*#if (templateMaintenance)*/
    private static string PipelineRoot => Path.Combine(ProjectReferenceGraph.RepositoryRoot, ".template.config", "variants", "code-host-gitlab");
    /*#else
    private static string PipelineRoot => ProjectReferenceGraph.RepositoryRoot;
    #endif*/

    private static string Pipeline => Path.Combine(PipelineRoot, ".gitlab-ci.yml");

    [Fact]
    public void EveryImageThePipelineRuns_IsPinnedByDigest()
    {
        var images = Images(PipelineLines()).ToList();

        images.ShouldContain(image => image.Reference.StartsWith(_sdkImage, StringComparison.Ordinal), "The build job runs on the .NET SDK image; a read of .gitlab-ci.yml that finds none is broken.");
        images
            .Where(image => !_pinnedImage.IsMatch(image.Reference))
            .Select(image => $"{image.Where}: {image.Reference}")
            .ShouldBeEmpty("Write each image a job runs, its image:, every services: entry, and an image its script runs with docker run or names in an image= or generator= option, as name:tag@sha256:<digest>; resolve it with `docker buildx imagetools inspect name:tag`.");
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
    public void EveryLocalInclude_NamesAFile_OrIsGuardedByExists()
    {
        var includes = Includes(PipelineLines()).Where(include => include.Kind == "local").ToList();

        includes.ShouldNotBeEmpty("The pipeline includes the web workspace's file; a read of .gitlab-ci.yml that finds no local include is broken.");
        includes
            .Where(include => !IsResolved(PipelineRoot, include.Value, include.Guards))
            .Select(include => $"{include.Where}: local: {include.Value}")
            .ShouldBeEmpty(
                "A local include names a file the project holds, or a pattern that matches one, or is guarded by rules: - exists: naming it; "
                + "GitLab refuses a pipeline whose local include is missing.");
    }

    [Fact]
    public void EveryFileUnderGitLabCi_IsIncludedByThePipeline()
    {
        string root = PipelineRoot;
        var included = Includes(PipelineLines()).Where(include => include.Kind == "local").Select(include => include.Value.TrimStart('/')).ToList();

        IncludedFiles()
            .Select(file => Path.GetRelativePath(root, file).Replace('\\', '/'))
            .Where(file => !included.Any(path => Glob(path).IsMatch(file)))
            .ShouldBeEmpty("A file under .gitlab/ci runs only when .gitlab-ci.yml includes it.");
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
    public void TheNodeArchive_IsTheVersionInNodeVersion()
    {
        string web = Path.Combine(PipelineRoot, ".gitlab", "ci", "web.yml");
        string nodeVersion = Path.Combine(ProjectReferenceGraph.RepositoryRoot, "src", "Web", ".node-version");
        if (!File.Exists(web))
        {
            return;
        }

        File.Exists(nodeVersion).ShouldBeTrue("The web jobs install the Node.js src/Web/.node-version names; the web workspace holds none.");
        string expected = File.ReadAllText(nodeVersion).Trim();
        var archives = NodeArchives(File.ReadAllLines(web));

        archives.ShouldNotBeEmpty("The web jobs install Node.js from its release archive; a read of .gitlab/ci/web.yml that finds no node= is broken.");
        archives.Where(version => version != expected).ShouldBeEmpty(
            $"src/Web/.node-version names Node.js {expected}, which the web images run, and the web jobs must test it: in the merge request that moves it, "
            + $"set node=v{expected} in .gitlab/ci/web.yml, and node_sha256 to the SHA-256 https://nodejs.org/dist/v{expected}/SHASUMS256.txt lists for "
            + $"node-v{expected}-linux-x64.tar.gz.");
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
        Images(["job:", "  script:", "    - docker buildx create --driver docker-container --driver-opt image=moby/buildkit:v0.33.1 --use ctx"])
            .Single().Reference.ShouldBe("moby/buildkit:v0.33.1", "the BuildKit image a builder runs is an image the pipeline runs");
        Images(["job:", "  script:", "    - |", "      docker buildx build \\", "        --sbom=generator=docker/buildkit-syft-scanner:1.12.0 \\"])
            .Single().Reference.ShouldBe("docker/buildkit-syft-scanner:1.12.0", "the SBOM scanner runs as an image");
        Images(["job:", "  script:", "    - docker run --privileged --rm tonistiigi/binfmt:qemu-v10.2.3 --install all"])
            .Single().Reference.ShouldBe("tonistiigi/binfmt:qemu-v10.2.3");
        Images(["job:", "  script:", "    - image=\"$CI_REGISTRY_IMAGE/$COMPONENT\"", "    - docker compose run --rm api"])
            .ShouldBeEmpty("a variable named image and a Compose service are no image reference");
        _pinnedImage.IsMatch($"docker:29.9.3-dind@{digest}").ShouldBeTrue();
        _pinnedImage.IsMatch("docker:29.9.3-dind").ShouldBeFalse();
        _pinnedImage.IsMatch($"docker@{digest}").ShouldBeFalse("the tag is kept for the reader");
        _pinnedImage.IsMatch("$IMAGE").ShouldBeFalse("an image from a variable is pinned by nothing in the file");
        NodeArchives([".node:", "  before_script:", "    - |", "      node=v26.13.0", "      node_sha256=5848", "      echo \"node=v1\""]).ShouldBe(["26.13.0"]);

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

        string[] guarded = ["include:", "  - local: .gitlab/ci/web.yml", "    rules:", "      - exists:", "          - .gitlab/ci/web.yml", "  - local: ci/other.yml", "stages:", "  - test"];
        Includes(guarded).Select(include => $"{include.Kind}={include.Value}|{string.Join(',', include.Guards)}")
            .ShouldBe(["local=.gitlab/ci/web.yml|.gitlab/ci/web.yml", "local=ci/other.yml|"], "a rules: list under an include is not an include of its own");

        string folder = Directory.CreateTempSubdirectory().FullName;
        try
        {
            Directory.CreateDirectory(Path.Combine(folder, "ci"));
            File.WriteAllText(Path.Combine(folder, "ci", "present.yml"), "");
            IsResolved(folder, "ci/present.yml", []).ShouldBeTrue();
            IsResolved(folder, "/ci/present.yml", []).ShouldBeTrue("GitLab reads a leading slash from the project's root");
            IsResolved(folder, "ci/*.yml", []).ShouldBeTrue("a pattern that matches a file includes it");
            IsResolved(folder, "ci/absent.yml", []).ShouldBeFalse();
            IsResolved(folder, "ci/*.absent.yml", []).ShouldBeFalse("a pattern that matches nothing includes nothing");
            IsResolved(folder, "ci/absent.yml", ["ci/absent.yml"]).ShouldBeTrue("rules: exists keeps the include out while the file is absent");
            IsResolved(folder, "ci/absent.yml", ["ci/present.yml"]).ShouldBeFalse("a guard on another file lets the missing one through");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    private static bool IsResolved(string root, string path, IReadOnlyList<string> guards)
    {
        string relative = path.TrimStart('/');
        if (guards.Any(guard => guard.TrimStart('/') == relative))
        {
            return true;
        }

        if (!relative.Contains('*', StringComparison.Ordinal))
        {
            return File.Exists(Path.Combine(root, relative));
        }

        string fixedPart = relative[..relative.IndexOf('*', StringComparison.Ordinal)];
        string folder = Path.Combine(root, fixedPart[..(fixedPart.LastIndexOf('/') + 1)]);
        return Directory.Exists(folder)
            && Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories)
                .Select(file => Path.GetRelativePath(root, file).Replace('\\', '/'))
                .Any(file => Glob(relative).IsMatch(file));
    }

    private static Regex Glob(string pattern) => new(
        "^" + Regex.Escape(pattern.TrimStart('/'))
            .Replace(@"\*\*/", "(?:.*/)?", StringComparison.Ordinal)
            .Replace(@"\*\*", ".*", StringComparison.Ordinal)
            .Replace(@"\*", "[^/]*", StringComparison.Ordinal) + "$",
        RegexOptions.None,
        _budget);

    private static IEnumerable<string> IncludedFiles()
    {
        string folder = Path.Combine(PipelineRoot, ".gitlab", "ci");
        return Directory.Exists(folder)
            ? Directory.EnumerateFiles(folder, "*.yml", SearchOption.AllDirectories).Order(StringComparer.Ordinal)
            : [];
    }

    private static List<string> NodeArchives(string[] lines) =>
        [.. lines.Select(line => _nodeArchive.Match(line)).Where(match => match.Success).Select(match => match.Groups["version"].Value)];

    private static bool IsAllowed(string kind, string value) =>
        kind == "local" || (kind == "template" && _scannerTemplates.Contains(value, StringComparer.Ordinal));

    private static string[] PipelineLines()
    {
        File.Exists(Pipeline).ShouldBeTrue($"{Pipeline} is missing: the project's gate runs from it.");
        return [.. IncludedFiles().Prepend(Pipeline).SelectMany(File.ReadAllLines)];
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

            foreach (Match option in _optionImage.Matches(line))
            {
                yield return ($"line {number}", Unquoted(option.Groups["ref"].Value));
            }

            if (_dockerRun.Match(line) is { Success: true } run
                && run.Groups["rest"].Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(word => !word.StartsWith('-')) is { } started)
            {
                yield return ($"line {number}", Unquoted(started));
            }

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

    private static IEnumerable<(string Where, string Kind, string Value, List<string> Guards)> Includes(string[] lines)
    {
        bool inside = false;
        int? itemIndent = null;
        int? existsIndent = null;
        (string Where, string Kind, string Value, List<string> Guards)? current = null;

        foreach (var (line, number) in Content(lines))
        {
            int indent = line.Length - line.TrimStart().Length;

            if (indent == 0)
            {
                if (current is { } finished)
                {
                    yield return finished;
                    current = null;
                }

                inside = false;
                itemIndent = null;
                existsIndent = null;

                if (_includeKey.Match(line) is { Success: true } key)
                {
                    if (key.Groups["value"].Success)
                    {
                        string value = Unquoted(key.Groups["value"].Value);
                        yield return ($"line {number}", Kind(value), value, []);
                    }
                    else
                    {
                        inside = true;
                    }
                }

                continue;
            }

            if (!inside)
            {
                continue;
            }

            existsIndent = indent > existsIndent ? existsIndent : null;
            itemIndent ??= line.TrimStart().StartsWith('-') ? indent : null;

            if (indent == itemIndent && _listItem.Match(line) is { Success: true } item)
            {
                if (current is { } finished)
                {
                    yield return finished;
                }

                current = item.Groups["value"].Success
                    ? ($"line {number}", item.Groups["key"].Success ? item.Groups["key"].Value : Kind(Unquoted(item.Groups["value"].Value)), Unquoted(item.Groups["value"].Value), [])
                    : null;
            }
            else if (current is not null && _existsKey.Match(line) is { Success: true } exists)
            {
                existsIndent = exists.Groups["indent"].Length;
            }
            else if (current is { } open && existsIndent is not null && _listItem.Match(line) is { Success: true } guard && guard.Groups["value"].Success && !guard.Groups["key"].Success)
            {
                open.Guards.Add(Unquoted(guard.Groups["value"].Value));
            }
        }

        if (current is { } last)
        {
            yield return last;
        }
    }

    private static string Kind(string path) => path.Contains("://", StringComparison.Ordinal) ? "remote" : "local";

    private static IEnumerable<(string Line, int Number)> Content(string[] lines) =>
        lines
            .Select((line, index) => (Line: line.TrimEnd(), Number: index + 1))
            .Where(entry => entry.Line.Length > 0 && !entry.Line.TrimStart().StartsWith('#'));

    private static string Unquoted(string value) => value.Trim().Trim('\'', '"');
}
