#:property UseAppHost=false
#:property PublishAot=false
#:property ManagePackageVersionsCentrally=false
#:package YamlDotNet@16.*

using System.Text;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

try
{
    Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
}
catch (IOException)
{
}

if (args is ["--help"] or ["-h"])
{
    Console.WriteLine(GitLabPipelineGate.Help);
    return 0;
}

if (args.Contains("--self-test"))
{
    return GitLabPipelineGate.SelfTest();
}

return GitLabPipelineGate.Run(Path.GetFullPath(args.Length > 0 ? args[0] : "."));

internal static class GitLabPipelineGate
{
    internal const string Help =
        """
        Usage: dotnet run tools/CheckGitLabCi.cs [<root>]
               dotnet run tools/CheckGitLabCi.cs --self-test
               dotnet run tools/CheckGitLabCi.cs -- --help

        Checks <root>/.gitlab-ci.yml, a generated project's pipeline, and
        <root>/.template.config/variants/code-host-gitlab/.gitlab-ci.yml, the overlay a template
        checkout holds, for what a YAML parse does not catch:
          - a job whose stage, test when it names none, is not declared in stages:, and an
            include of GitLab's scanner templates without the test stage their jobs run in
          - a needs: or dependencies: naming a job the file does not define
          - an extends: naming a hidden job or job the file does not define
          - an image: or services: entry, at the top level, under default: or in a job, without
            a digest pin
          - an include: that is neither local: nor one of GitLab's SAST and Secret Detection
            templates, the only ones allowed to float
          - a job with no timeout:, its own or one it extends
          - a script line whose first word, past VAR=value assignments, is dotnet, in a job whose
            image, its own, one it extends or default:'s, is not the .NET SDK image
          - a local file a script names (script, project, settings) or a cache key file that is
            absent from disk
        A job is a top-level key that is neither a keyword nor hidden by a leading dot; the
        pipeline's paths are read from <root>, the project's root or the checkout's.

        Exit code 0 when every pipeline is sound; 1 on a problem, and on a root holding no
        pipeline, which proves nothing.
        """;

    private const string _overlay = ".template.config/variants/code-host-gitlab/.gitlab-ci.yml";

    private const string _sdkImage = "mcr.microsoft.com/dotnet/sdk:";

    private const string _defaultStage = "test";

    private static readonly string[] _defaultStages = [".pre", "build", "test", "deploy", ".post"];

    private static readonly string[] _allowedTemplates = ["Jobs/SAST.gitlab-ci.yml", "Jobs/Secret-Detection.gitlab-ci.yml"];

    private static readonly HashSet<string> _keywords = new(StringComparer.Ordinal)
    {
        "default", "include", "stages", "variables", "workflow", "image", "services", "cache",
        "before_script", "after_script", "spec",
    };

    private static readonly string[] _localOnly = ["local"];

    private static readonly string[] _scriptKeys = ["before_script", "script", "after_script"];

    private static readonly Regex _digestPin = new(
        @"^[a-z0-9][a-z0-9._/:-]*:[A-Za-z0-9._-]+@sha256:[0-9a-f]{64}$",
        RegexOptions.Compiled);

    private static readonly Regex _fileRef = new(
        @"(?:\./)?("
        + @"(?:src/|tests/|tools/)[\w\-./]+\.(?:py|csproj|cs|xml|sh)"
        + @"|coverage\.(?:runsettings|minimum)"
        + @"|[\w\-./]+/Dockerfile"
        + @")",
        RegexOptions.Compiled);

    private static readonly Regex _dotnetCommand = new(@"^(?:[A-Za-z_][A-Za-z0-9_]*=\S*\s+)*dotnet(?:\.exe)?(?:\s|$)", RegexOptions.Compiled);

    private static readonly UTF8Encoding _utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public static List<string> Discover(string root)
        => new[] { ".gitlab-ci.yml", _overlay }
            .Select(relative => Path.Combine(root, relative))
            .Where(File.Exists)
            .ToList();

    public static List<string> Validate(string root, string name, string text)
    {
        var problems = new List<string>();

        YamlNode? document;

        try
        {
            var stream = new YamlStream();
            stream.Load(new StringReader(text));
            document = stream.Documents.Count > 0 ? stream.Documents[0].RootNode : null;
        }
        catch (YamlException exception)
        {
            return [$"{name}: does not parse — {exception.Message}"];
        }

        if (document is not YamlMappingNode top)
        {
            return [$"{name}: top level is not a mapping"];
        }

        var definitions = top.Children
            .Where(entry => entry.Key is YamlScalarNode { Value: { } key } && !_keywords.Contains(key))
            .ToDictionary(entry => ScalarText(entry.Key), entry => entry.Value, StringComparer.Ordinal);
        var jobs = definitions.Where(entry => !entry.Key.StartsWith('.')).ToList();

        if (jobs.Count == 0)
        {
            return [$"{name}: defines no jobs"];
        }

        var stagesNode = Get(top, "stages");
        var stages = new HashSet<string>(
            stagesNode is YamlSequenceNode declared ? declared.Children.Select(ScalarText) : _defaultStages,
            StringComparer.Ordinal) { ".pre", ".post" };

        var includeProblems = IncludeProblems(Get(top, "include"), out var scanners);
        foreach (var problem in includeProblems)
        {
            problems.Add($"{name}: {problem}");
        }

        if (scanners && !stages.Contains(_defaultStage))
        {
            problems.Add($"{name}: includes GitLab's scanner templates, whose jobs run in stage '{_defaultStage}', which stages: does not declare");
        }

        var defaults = Get(top, "default");
        var defaultImage = ImageName(Get(defaults, "image")) ?? ImageName(Get(top, "image"));

        foreach (var (where, image) in Images("the top level", top).Concat(Images("default:", defaults)))
        {
            if (!_digestPin.IsMatch(image))
            {
                problems.Add($"{name}: {where} runs '{image}' without a digest pin");
            }
        }

        foreach (var (jobName, node) in jobs)
        {
            if (node is not YamlMappingNode job)
            {
                problems.Add($"{name}: job '{jobName}' is not a mapping");
                continue;
            }

            var chain = Chain(definitions, job, out var unknownParents);

            foreach (var parent in unknownParents)
            {
                problems.Add($"{name}: job '{jobName}' extends '{parent}', which this file does not define");
            }

            var stage = chain.Select(link => Get(link, "stage")).FirstOrDefault(found => !IsNull(found)) is { } stageNode
                ? ScalarText(stageNode)
                : _defaultStage;
            if (!stages.Contains(stage))
            {
                problems.Add($"{name}: job '{jobName}' runs in stage '{stage}', which stages: does not declare");
            }

            foreach (var key in (string[])["needs", "dependencies"])
            {
                foreach (var dependency in JobNames(Get(job, key)))
                {
                    if (!jobs.Any(other => other.Key == dependency))
                    {
                        problems.Add($"{name}: job '{jobName}' {key} '{dependency}', which is not a job in this file");
                    }
                }
            }

            if (chain.All(link => IsNull(Get(link, "timeout"))))
            {
                problems.Add($"{name}: job '{jobName}' sets no timeout:");
            }

            foreach (var (where, image) in chain.SelectMany(link => Images($"job '{jobName}'", link)).Distinct())
            {
                if (!_digestPin.IsMatch(image))
                {
                    problems.Add($"{name}: {where} runs '{image}' without a digest pin");
                }
            }

            var jobImage = chain.Select(link => ImageName(Get(link, "image"))).FirstOrDefault(found => found is not null) ?? defaultImage;
            var lines = chain.SelectMany(link => _scriptKeys.SelectMany(key => ScriptLines(Get(link, key)))).ToList();

            if (lines.Any(line => _dotnetCommand.IsMatch(line.Trim()))
                && (jobImage is null || !jobImage.StartsWith(_sdkImage, StringComparison.Ordinal)))
            {
                problems.Add(
                    $"{name}: job '{jobName}' runs dotnet on '{jobImage ?? "the runner's default image"}'; "
                    + $"give it the {_sdkImage}<version> image the Dockerfiles build with, pinned by digest");
            }

            foreach (Match match in lines.SelectMany(line => _fileRef.Matches(line)))
            {
                var reference = match.Groups[1].Value;
                if (reference.Contains('*') || reference.Contains('$'))
                {
                    continue;
                }

                if (!Exists(root, reference))
                {
                    problems.Add($"{name}: job '{jobName}' references '{reference}', which does not exist on disk");
                }
            }

            foreach (var file in chain.SelectMany(CacheKeyFiles).Distinct(StringComparer.Ordinal))
            {
                if (!file.Contains('*') && !file.Contains('$') && !Exists(root, file))
                {
                    problems.Add($"{name}: job '{jobName}' keys its cache on '{file}', which does not exist on disk");
                }
            }
        }

        return problems;
    }

    public static int Run(string root)
    {
        var pipelines = Discover(root);

        var listed = pipelines.Count > 0
            ? string.Join(", ", pipelines.Select(path => Path.GetRelativePath(root, path).Replace('\\', '/')))
            : "(none)";
        Console.WriteLine($"Validated {pipelines.Count} pipeline(s): {listed}");
        Console.WriteLine();

        if (pipelines.Count == 0)
        {
            Console.WriteLine("PROBLEM: no pipeline file was examined, so this check proves nothing.");
            return 1;
        }

        var problems = pipelines
            .SelectMany(path => Validate(root, Path.GetRelativePath(root, path).Replace('\\', '/'), ReadText(path)))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

        if (problems.Count > 0)
        {
            Console.WriteLine($"PROBLEMS ({problems.Count}):");
            foreach (var problem in problems)
            {
                Console.WriteLine($"  ! {problem}");
            }

            return 1;
        }

        Console.WriteLine("No structural problems found.");
        return 0;
    }

    private const string _sdk =
        "mcr.microsoft.com/dotnet/sdk:10.0.403-noble@sha256:212fe4cb74627d5adf9817a4088906ae3edff1bd853bb27565d4b46731efe61e";

    private const string _dind =
        "docker:29.9.3-dind@sha256:08a98015fce594af7c382883bea080ea206de74402a95da9dc52b6428c8cd121";

    private const string _cleanFixture = $$"""
        include:
          - template: Jobs/SAST.gitlab-ci.yml
          - template: Jobs/Secret-Detection.gitlab-ci.yml
          - local: ci/extra.yml
        stages:
          - build
          - test
        variables:
          SOLUTION: App.slnx
        default:
          interruptible: true
        .dotnet:
          image: {{_sdk}}
          cache:
            key:
              files:
                - global.json
            paths:
              - .nuget/packages
        build:
          extends: .dotnet
          stage: build
          timeout: 10 minutes
          services:
            - name: {{_dind}}
              alias: docker
          script:
            - dotnet build "$SOLUTION"
            - DOTNET_NOLOGO=1 dotnet run tools/Present.cs
        test:
          extends: .dotnet
          timeout: 10 minutes
          needs: [build]
          dependencies:
            - build
          script:
            - dotnet test --solution "$SOLUTION"
        lint:
          image:
            name: {{_dind}}
          stage: test
          timeout: 5 minutes
          services:
            - {{_dind}}
          script:
            - docker info
            - echo "dotnet runs elsewhere"

        """;

    private static readonly (string Label, string Text)[] _mustFire =
    [
        ("a job in a stage stages: does not declare", _cleanFixture.Replace("  stage: build\n", "  stage: deploy\n")),
        (
            "a job without a stage when stages: does not declare test",
            _cleanFixture.Replace("  - test\n", "").Replace("  stage: test\n", "  stage: build\n")
        ),
        (
            "the scanner templates without the test stage",
            _cleanFixture.Replace("  - test\n", "").Replace("  stage: test\n", "  stage: build\n").Replace("test:\n  extends: .dotnet\n  timeout", "unit:\n  extends: .dotnet\n  stage: build\n  timeout")
        ),
        ("needs: naming a job that does not exist", _cleanFixture.Replace("needs: [build]", "needs: [absent]")),
        ("needs: naming a job as a mapping that does not exist", _cleanFixture.Replace("needs: [build]", "needs:\n    - job: absent")),
        ("dependencies: naming a job that does not exist", _cleanFixture.Replace("    - build\n  script:", "    - absent\n  script:")),
        ("extends: naming nothing the file defines", _cleanFixture.Replace("build:\n  extends: .dotnet", "build:\n  extends: .absent")),
        ("a job image without a digest", _cleanFixture.Replace(".dotnet:\n  image: " + _sdk, ".dotnet:\n  image: mcr.microsoft.com/dotnet/sdk:10.0")),
        ("an image as a mapping without a digest", _cleanFixture.Replace("  image:\n    name: " + _dind, "  image:\n    name: docker:29")),
        ("a service as a mapping without a digest", _cleanFixture.Replace("    - name: " + _dind, "    - name: docker:29-dind")),
        ("a service as a string without a digest", _cleanFixture.Replace("    - " + _dind + "\n", "    - docker:29-dind\n")),
        ("a default image without a digest", _cleanFixture.Replace("default:\n", "default:\n  image: alpine:3\n")),
        ("a top-level image without a digest", _cleanFixture.Replace("stages:\n", "image: alpine:3\nstages:\n")),
        ("an image named from a variable", _cleanFixture.Replace("  image:\n    name: " + _dind, "  image: $IMAGE")),
        ("a remote include", _cleanFixture.Replace("  - local: ci/extra.yml", "  - remote: https://example.com/ci.yml")),
        ("a component include", _cleanFixture.Replace("  - local: ci/extra.yml", "  - component: gitlab.com/components/sast/sast@1.0.0")),
        ("a project include", _cleanFixture.Replace("  - local: ci/extra.yml", "  - project: group/ci\n    file: ci.yml")),
        ("a template outside the allowlist", _cleanFixture.Replace("  - local: ci/extra.yml", "  - template: Jobs/Dependency-Scanning.gitlab-ci.yml")),
        ("an include as a URL string", _cleanFixture.Replace("  - local: ci/extra.yml", "  - https://example.com/ci.yml")),
        ("a job without a timeout", _cleanFixture.Replace("  timeout: 5 minutes\n", "")),
        ("dotnet on an image that is not the SDK", _cleanFixture.Replace("    - echo \"dotnet runs elsewhere\"", "    - dotnet --info")),
        ("dotnet in a job with no image", _cleanFixture.Replace("test:\n  extends: .dotnet\n", "test:\n")),
        ("a local script absent from disk", _cleanFixture.Replace("tools/Present.cs", "tools/Absent.cs")),
        ("a cache key file absent from disk", _cleanFixture.Replace("        - global.json\n", "        - Directory.Packages.props\n")),
        ("no jobs", "stages:\n  - test\n.hidden:\n  script:\n    - echo hidden\n"),
        ("a job that is not a mapping", _cleanFixture + "odd: 3\n"),
        ("a top level that is not a mapping", "- one\n- two\n"),
        ("YAML that does not parse", "build: [\n"),
    ];

    private static readonly (string Label, string Text)[] _mustStaySilent =
    [
        ("a pipeline with nothing wrong with it", _cleanFixture),
        ("no stages: and jobs in the default ones", _cleanFixture.Replace("stages:\n  - build\n  - test\n", "")),
        ("an include as a local string", _cleanFixture.Replace("  - local: ci/extra.yml", "  - ci/extra.yml")),
        ("dotnet on the SDK image default: names", _cleanFixture.Replace("default:\n", $"default:\n  image: {_sdk}\n").Replace("test:\n  extends: .dotnet\n", "test:\n")),
        ("dotnet only in a comment and a container's command line", _cleanFixture.Replace("    - echo \"dotnet runs elsewhere\"", "    - docker run --rm sdk dotnet --info\n    - '# dotnet test runs in the test job'")),
        ("a timeout the job inherits through extends:", _cleanFixture.Replace("test:\n  extends: .dotnet\n  timeout: 10 minutes\n", "test:\n  extends: .dotnet\n").Replace(".dotnet:\n", ".dotnet:\n  timeout: 10 minutes\n")),
    ];

    private static void FixtureRoot(string root)
    {
        var tools = Path.Combine(root, "tools");
        Directory.CreateDirectory(tools);
        File.WriteAllText(Path.Combine(tools, "Present.cs"), "", _utf8NoBom);
        File.WriteAllText(Path.Combine(root, "global.json"), "{}", _utf8NoBom);
    }

    private static int SilentRun(string root)
    {
        var original = Console.Out;
        try
        {
            Console.SetOut(new StringWriter());
            return Run(root);
        }
        finally
        {
            Console.SetOut(original);
        }
    }

    public static int SelfTest()
    {
        var failures = new List<string>();

        var fixtures = Directory.CreateTempSubdirectory().FullName;
        try
        {
            FixtureRoot(fixtures);

            foreach (var (label, text) in _mustFire)
            {
                if (Validate(fixtures, "fixture.yml", text).Count == 0)
                {
                    failures.Add($"should have fired but did not: {label}");
                }
            }

            foreach (var (label, text) in _mustStaySilent)
            {
                var problems = Validate(fixtures, "fixture.yml", text);
                if (problems.Count > 0)
                {
                    failures.Add($"fired on a sound pipeline ({problems[0]}): {label}");
                }
            }
        }
        finally
        {
            Directory.Delete(fixtures, recursive: true);
        }

        var faulted = _cleanFixture.Replace("needs: [build]", "needs: [absent]");
        var discovery = 0;

        foreach (var location in (string[])[".gitlab-ci.yml", _overlay])
        {
            var root = Directory.CreateTempSubdirectory().FullName;
            try
            {
                FixtureRoot(root);
                discovery++;

                if (SilentRun(root) == 0)
                {
                    failures.Add("passed over a root holding no pipeline");
                }

                var target = Path.Combine(root, location);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.WriteAllText(target, faulted, _utf8NoBom);
                discovery++;

                if (SilentRun(root) == 0)
                {
                    failures.Add($"never read '{location}', so its dangling needs: went unreported");
                }

                File.WriteAllText(target, _cleanFixture, _utf8NoBom);
                discovery++;

                if (SilentRun(root) != 0)
                {
                    failures.Add($"rejected '{location}', which carries no fault");
                }
            }
            finally
            {
                Directory.Delete(root, recursive: true);
            }
        }

        var total = _mustFire.Length + _mustStaySilent.Length + discovery;
        Console.WriteLine(
            $"Self-test: {_mustFire.Length} faulted pipeline(s), {_mustStaySilent.Length} sound one(s), "
            + $"{discovery} discovery claim(s).");
        Console.WriteLine();

        if (failures.Count > 0)
        {
            Console.WriteLine($"SELF-TEST FAILURES ({failures.Count} of {total}):");
            foreach (var failure in failures)
            {
                Console.WriteLine($"  ! {failure}");
            }

            return 1;
        }

        Console.WriteLine(
            "The gate fires on every faulted pipeline, on none of the sound ones, and reads both locations.");
        return 0;
    }

    private static List<string> IncludeProblems(YamlNode? include, out bool scanners)
    {
        var problems = new List<string>();
        scanners = false;

        IEnumerable<YamlNode> entries = include switch
        {
            YamlSequenceNode sequence => sequence.Children,
            null => [],
            _ when IsNull(include) => [],
            _ => [include],
        };

        foreach (var entry in entries)
        {
            switch (entry)
            {
                case YamlScalarNode { Value: { } path } when !path.Contains("://", StringComparison.Ordinal):
                    break;
                case YamlMappingNode mapping when Keys(mapping).SequenceEqual(_localOnly):
                    break;
                case YamlMappingNode mapping when Get(mapping, "template") is YamlScalarNode { Value: { } template }
                    && _allowedTemplates.Contains(template, StringComparer.Ordinal):
                    scanners = true;
                    break;
                default:
                    problems.Add(
                        $"includes '{Describe(entry)}', which is neither local: nor one of {string.Join(" and ", _allowedTemplates)}; "
                        + "write it in this repository, since nothing else can be pinned on every instance");
                    break;
            }
        }

        return problems;
    }

    private static string Describe(YamlNode node) => node switch
    {
        YamlScalarNode scalar => scalar.Value ?? "",
        YamlMappingNode mapping => string.Join(", ", mapping.Children.Select(entry => $"{ScalarText(entry.Key)}: {ScalarText(entry.Value)}")),
        _ => node.ToString(),
    };

    private static List<YamlMappingNode> Chain(
        Dictionary<string, YamlNode> definitions, YamlMappingNode job, out List<string> unknown)
    {
        var chain = new List<YamlMappingNode>();
        var missing = new List<string>();
        var seen = new HashSet<YamlNode>();
        var pending = new Queue<YamlMappingNode>([job]);

        while (pending.TryDequeue(out var link))
        {
            if (!seen.Add(link))
            {
                continue;
            }

            chain.Add(link);

            foreach (var parent in JobNames(Get(link, "extends")))
            {
                if (definitions.TryGetValue(parent, out var found) && found is YamlMappingNode mapping)
                {
                    pending.Enqueue(mapping);
                }
                else
                {
                    missing.Add(parent);
                }
            }
        }

        unknown = missing;
        return chain;
    }

    private static IEnumerable<(string Where, string Image)> Images(string owner, YamlNode? node)
    {
        if (ImageName(Get(node, "image")) is { } image)
        {
            yield return ($"{owner} image", image);
        }

        if (Get(node, "services") is YamlSequenceNode services)
        {
            foreach (var service in services.Children)
            {
                if (ImageName(service) is { } name)
                {
                    yield return ($"{owner} service", name);
                }
            }
        }
    }

    private static string? ImageName(YamlNode? node) => node switch
    {
        YamlScalarNode scalar when !IsNull(scalar) => scalar.Value!.Trim(),
        YamlMappingNode mapping when Get(mapping, "name") is YamlScalarNode name && !IsNull(name) => name.Value!.Trim(),
        _ => null,
    };

    private static IEnumerable<string> JobNames(YamlNode? node) => node switch
    {
        YamlScalarNode scalar when !IsNull(scalar) => [scalar.Value!],
        YamlSequenceNode sequence => sequence.Children
            .Select(item => item is YamlMappingNode mapping ? Get(mapping, "job") : item)
            .OfType<YamlScalarNode>()
            .Where(item => !IsNull(item))
            .Select(item => item.Value!),
        _ => [],
    };

    private static IEnumerable<string> ScriptLines(YamlNode? node) => node switch
    {
        YamlScalarNode scalar when !IsNull(scalar) => (scalar.Value ?? "").Split('\n'),
        YamlSequenceNode sequence => sequence.Children.SelectMany(ScriptLines),
        _ => [],
    };

    private static IEnumerable<string> CacheKeyFiles(YamlMappingNode job)
    {
        IEnumerable<YamlNode> caches = Get(job, "cache") switch
        {
            YamlSequenceNode sequence => sequence.Children,
            YamlMappingNode mapping => [mapping],
            _ => [],
        };

        return caches
            .Select(cache => Get(Get(cache, "key"), "files"))
            .OfType<YamlSequenceNode>()
            .SelectMany(files => files.Children.Select(ScalarText));
    }

    private static bool Exists(string root, string relative)
    {
        var candidate = Path.Combine(root, relative);
        return File.Exists(candidate) || Directory.Exists(candidate);
    }

    private static YamlNode? Get(YamlNode? node, string key)
    {
        if (node is not YamlMappingNode mapping)
        {
            return null;
        }

        foreach (var entry in mapping.Children)
        {
            if (entry.Key is YamlScalarNode scalar && string.Equals(scalar.Value, key, StringComparison.Ordinal))
            {
                return entry.Value;
            }
        }

        return null;
    }

    private static bool IsNull(YamlNode? node)
        => node is null
            || (node is YamlScalarNode scalar
                && scalar.Style == ScalarStyle.Plain
                && scalar.Value is null or "" or "~" or "null" or "Null" or "NULL");

    private static IEnumerable<string> Keys(YamlMappingNode mapping)
        => mapping.Children.Select(entry => ScalarText(entry.Key));

    private static string ScalarText(YamlNode node)
        => node is YamlScalarNode scalar ? scalar.Value ?? "" : node.ToString();

    private static string ReadText(string path)
        => File.ReadAllText(path).Replace("\r\n", "\n").Replace("\r", "\n");
}
