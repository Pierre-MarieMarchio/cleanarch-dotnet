using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace AppTemplate.Api.Common.OpenApi;

internal sealed class OpenApiProblemMediaTypeTransformer : IOpenApiOperationTransformer
{
    private const string _problemMediaType = "application/problem+json";

    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        foreach (var response in operation.Responses?.Values.OfType<OpenApiResponse>() ?? [])
        {
            if (response.Content is { } content
                && content.Values.FirstOrDefault(media => media.Schema is OpenApiSchemaReference { Reference.Id: "ProblemDetails" or "ValidationProblemDetails" }) is { } problem)
            {
                content.Clear();
                content[_problemMediaType] = problem;
            }
        }

        return Task.CompletedTask;
    }
}
