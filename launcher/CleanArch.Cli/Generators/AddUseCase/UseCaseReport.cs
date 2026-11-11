using CleanArch.Cli.Generators.Shared;
using static CleanArch.Cli.Generators.Shared.ProjectFolders;

namespace CleanArch.Cli.Generators.AddUseCase;

internal sealed class UseCaseReport
{
    private readonly string _root;

    private readonly UseCaseSpec _spec;

    private readonly List<Emission> _plan;

    private readonly List<Emission> _contracts;

    private readonly string _api;

    private readonly string _page;

    private readonly HashSet<string> _flags;

    private readonly Dictionary<string, string> _tokens;

    private UseCaseReport(string root, UseCaseSpec spec, List<Emission> plan, List<Emission> contracts)
    {
        _root = root;
        _spec = spec;
        _plan = plan;
        _contracts = contracts;
        _api = $"{Presentation}/{FolderOf(spec.Prefix, "Api")}";
        _page = $"docs/features/{CSharpNames.Kebab(spec.Features)}.md";
        _flags = UseCaseFlags.Of(spec);
        _tokens = UseCasePlan.Tokens(spec);
    }

    internal static FlagSet Shapes { get; } = new(
        UseCaseFlags.Vocabulary.Generator,
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["command"] = "`--kind command`; a query otherwise.",
            ["api"] = "The API serves the feature, so the run edits its controller and writes its contracts.",
            ["request"] = "The message carries an input, so a request contract carries it over HTTP.",
            ["validator"] = "An input has a rule, so a validator is written.",
            ["validatorTests"] = "The validator's tests are written.",
            ["handlerTests"] = "The handler's test is written: the application's tests hold the `ValidatedHandler` double.",
            ["skipped"] = "The application's test project is not in the tree, so no test is written.",
            ["unknown"] = "An input is of a type the command knows no rule and no test value for.",
            ["dtoDeclared"] = "The query answers a DTO the feature declares already.",
            ["manifest"] = "A feature manifest names the feature, and records the files and the edits.",
            ["page"] = "The feature has its page under `docs/features`.",
            ["fullRoute"] = "The route's row is written in full, from `/api`, not relative to its table's prefix.",
        });

    internal static FlagSet Vocabulary { get; } = ReportFlags.For(UseCaseFlags.Vocabulary, Shapes);

    internal static void Write(TextWriter output, TemplateOverrides templates, string root, UseCaseSpec spec, List<Emission> plan, List<Emission> contracts, string? skipped, ReportMode mode)
    {
        var report = new UseCaseReport(root, spec, plan, contracts);

        report.Files(skipped);
        report.Inputs();
        report.Http();
        templates.WriteReport(output, mode, report._flags, report._tokens);
    }

    private void Hold(string flag, bool holds)
    {
        if (holds)
        {
            _flags.Add(flag);
        }
    }

    private bool Writes(string file) => _plan.Any(emission => emission.Path.EndsWith($"/{file}", StringComparison.Ordinal));

    private void Files(string? skipped)
    {
        _tokens["Count"] = $"{_plan.Count}";
        _tokens["Files"] = ReportLines.Of(_plan.Select(emission => emission.Path));
        _tokens["Kind"] = _spec.Kind;
        _tokens["Input"] = _spec.Input;
        _tokens["Manifest"] = _spec.Manifest ?? string.Empty;
        _tokens["Skipped"] = skipped ?? string.Empty;
        _tokens["Page"] = _page;
        Hold("command", _spec.IsCommand);
        Hold("dtoDeclared", _spec.DtoExists);
        Hold("manifest", _spec.Manifest is not null);
        Hold("skipped", skipped is not null);
        Hold("validatorTests", Writes($"{_spec.Input}ValidatorTests.cs"));
        Hold("handlerTests", Writes($"{_spec.Name}Tests.cs"));
        Hold("page", File.Exists(Path.Combine(_root, _page)));
    }

    private void Inputs()
    {
        _tokens["Unknown"] = ReportLines.Of(_spec.Unknown.Select(parameter => $"{parameter.Name}  {parameter.Type}"));
        Hold("validator", _spec.HasValidator);
        Hold("request", _spec.HasRequest);
        Hold("unknown", _spec.Unknown.Length > 0);
    }

    private void Http()
    {
        bool api = Directory.Exists(Path.Combine(_root, _api));
        var request = _contracts.FirstOrDefault(contract => contract.Path.EndsWith($"/{_spec.Request}.cs", StringComparison.Ordinal));
        var response = _contracts.FirstOrDefault(contract => contract != request);

        Hold("api", api);
        Hold("fullRoute", _spec.Routing?.Documented.StartsWith("/api/", StringComparison.Ordinal) == true);
        _tokens["Controller"] = api ? Controller() : string.Empty;
        _tokens["Usings"] = api ? string.Join(";\n                       ", UseCaseHttp.ControllerUsings(_spec)) + ";" : string.Empty;
        _tokens["Action"] = api ? Indented(UseCaseHttp.ActionLines(_spec)) : string.Empty;
        _tokens["RequestPath"] = request?.Path ?? string.Empty;
        _tokens["Request"] = request is null ? string.Empty : Indented(Lines(request));
        _tokens["ResponsePath"] = response?.Path ?? string.Empty;
        _tokens["Response"] = response is null ? string.Empty : Indented(Lines(response));
        _tokens["Steps"] = ReportLines.Of(TextEdits.ContractSteps(_root, built: false).Select(step => step.Display));
        _tokens["RouteRow"] = api ? UseCaseHttp.RouteRowText(_spec) : string.Empty;
        _tokens["Construction"] = UseCaseHttp.Construction(_spec);
        _tokens["Hosts"] = ReportLines.Of(UseCaseHttp.Hosts(_root).Select(host => $"src/Presentation/{host}/"));
    }

    private string Controller()
    {
        string controller = $"{_api}/Features/{_spec.Features}/Controllers/{_spec.Features}Controller.cs";

        return File.Exists(Path.Combine(_root, controller)) ? controller : $"a controller under {_api}/Features/{_spec.Features}/Controllers/";
    }

    private static string[] Lines(Emission contract) => contract.Content.TrimEnd('\n').Split('\n');

    private static string Indented(IEnumerable<string> lines) => ReportLines.Of(lines, "      ");
}
