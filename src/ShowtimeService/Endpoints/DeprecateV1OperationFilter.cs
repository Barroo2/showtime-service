using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ShowtimeService.Endpoints;

// Markiert alle v1-Operationen im OpenAPI-Contract als deprecated
public class DeprecateV1OperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.DocumentName == "v1")
        {
            operation.Deprecated = true;
        }
    }
}
