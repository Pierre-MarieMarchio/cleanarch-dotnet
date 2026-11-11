using CleanArch.Cli.Generators.Shared;

namespace CleanArch.Cli.Generators.AddUseCase;

internal static class ParameterBlocks
{
    internal static TemplateList For(UseCaseSpec spec)
    {
        string? malformed = spec.Ruled.FirstOrDefault()?.Name;

        return TemplateList.Of(
            "parameter",
            spec.Parameters,
            new Dictionary<string, Func<Parameter, string>>(StringComparer.Ordinal)
            {
                ["Name"] = parameter => parameter.Name,
                ["Type"] = parameter => parameter.Type,
                ["Required"] = parameter => parameter.Required,
                ["Valid"] = parameter => parameter.Valid,
                ["Empty"] = parameter => parameter.Empty ?? string.Empty,
            },
            new Dictionary<string, Func<Parameter, bool>>(StringComparer.Ordinal)
            {
                ["input"] = spec.Inputs.Contains,
                ["ruled"] = parameter => parameter.Empty is not null,
                ["malformed"] = parameter => parameter.Name == malformed,
            });
    }
}
