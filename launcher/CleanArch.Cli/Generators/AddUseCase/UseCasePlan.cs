using CleanArch.Cli.Generators.Shared;
using static CleanArch.Cli.Generators.Shared.ProjectFolders;

namespace CleanArch.Cli.Generators.AddUseCase;

internal sealed class UseCasePlan
{
    private const string _feature = "Features/Placeholders";

    private const string _application = $"src/Application/AT.Application/{_feature}";

    private const string _api = $"src/Presentation/AT.Api/{_feature}";

    private const string _tests = $"tests/Application/AT.Application.UnitTests/{_feature}";

    private readonly UseCaseSpec _spec;

    private readonly string _root;

    private readonly TemplateOverrides _templates;

    private readonly HashSet<string> _flags;

    private readonly Dictionary<string, string> _tokens;

    private readonly TemplateList _parameters;

    private readonly string _name;

    private readonly string _input;

    private readonly string _useCase;

    private UseCasePlan(UseCaseSpec spec, string root, TemplateOverrides templates)
    {
        _spec = spec;
        _root = root;
        _templates = templates;
        _flags = UseCaseFlags.Of(spec);
        _tokens = Tokens(spec);
        _parameters = ParameterBlocks.For(spec);
        _name = spec.IsCommand ? "ApprovePlaceholder" : "GetPlaceholderTotals";
        _input = _name + spec.Input[spec.Name.Length..];
        _useCase = $"UseCases/{spec.Folder}/{_name}";
    }

    internal static TemplateOverrides Overrides(string root) => TemplateOverrides.Read(root, NewUseCase.TemplateFolder, UseCaseFlags.Vocabulary);

    internal static List<Emission> Plan(UseCaseSpec spec, string root, TemplateOverrides templates, out string? skippedTests) =>
        new UseCasePlan(spec, root, templates).Application(out skippedTests);

    internal static List<Emission> Contracts(UseCaseSpec spec, string root, TemplateOverrides templates) =>
        Directory.Exists(Full(root, UseCaseHttp.ApiFolder(spec))) ? new UseCasePlan(spec, root, templates).Api() : [];

    private static Dictionary<string, string> Tokens(UseCaseSpec spec)
    {
        var tokens = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["P"] = spec.Prefix,
            ["F"] = spec.Features,
            ["UseCase"] = spec.Name,
            ["Spoken"] = CSharpNames.Separated(spec.Name, ' '),
        };

        if (spec.Aggregate is { } aggregate)
        {
            tokens["A"] = aggregate;
            tokens["SpokenAggregate"] = CSharpNames.Separated(aggregate, ' ');
        }

        if (spec.Dto is { } dto)
        {
            tokens["Answer"] = dto[..^"Dto".Length];
            tokens["SpokenAnswer"] = CSharpNames.Separated(tokens["Answer"], ' ');
        }

        return tokens;
    }

    internal static string Sentence(string pascal)
    {
        string spoken = CSharpNames.Separated(pascal, ' ');

        return char.ToUpperInvariant(spoken[0]) + spoken[1..];
    }

    private List<Emission> Application(out string? skippedTests)
    {
        List<Emission> emissions = [Render($"{_application}/{_useCase}/{_name}.cs")];

        skippedTests = null;

        if (_spec.HasValidator)
        {
            emissions.Add(Render($"{_application}/{_useCase}/{_input}Validator.cs"));
        }

        if (_spec.Dto is not null && !_spec.DtoExists)
        {
            emissions.Add(Render($"{_application}/Model/Dtos/PlaceholderTotalsDto.cs"));
        }

        if (_spec.HasValidator)
        {
            emissions.AddRange(Tests(out skippedTests));
        }

        return emissions;
    }

    private List<Emission> Tests(out string? skippedTests)
    {
        string validatorTests = $"{_tests}/{_useCase}/{_input}ValidatorTests.cs";
        string path = PathOf(_spec.Prefix, validatorTests, _tokens);

        skippedTests = ProjectExists(_root, path) ? null : ProjectFolderOf(path);

        if (skippedTests is not null)
        {
            return [];
        }

        return File.Exists(Full(_root, $"{ProjectFolderOf(path)}/TestDoubles/ValidatedHandler.cs"))
            ? [Render(validatorTests), Render($"{_tests}/{_useCase}/{_name}Tests.cs")]
            : [Render(validatorTests)];
    }

    private List<Emission> Api() =>
    [
        .. _spec.HasRequest ? [Render($"{_api}/Contracts/Requests/{_name}Request.cs")] : Array.Empty<Emission>(),
        .. _spec.IsCommand ? Array.Empty<Emission>() : [Render($"{_api}/Contracts/Responses/PlaceholderTotalsResponse.cs")],
    ];

    private Emission Render(string reference) =>
        new(PathOf(_spec.Prefix, reference, _tokens), _templates.Render(reference, _flags, _tokens, _parameters));
}
