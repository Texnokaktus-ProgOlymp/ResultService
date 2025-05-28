using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace Texnokaktus.ProgOlymp.ResultService.Converters;

internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (authenticationSchemes.Any(authScheme => authScheme.Name == "Bearer"))
        {
            var requirements = new Dictionary<string, OpenApiSecurityScheme>
            {
                ["Bearer"] = new()
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer", // "bearer" refers to the header name here
                    In = ParameterLocation.Header,
                    BearerFormat = "Json Web Token"
                }
            };
            document.Components ??= new();
            document.Components.SecuritySchemes = requirements;

            foreach (var operation in document.Paths.Values
                                              .SelectMany(path => path.Operations)
                                              .Where(operation => context.GetActionDescriptor(operation.Value) is { } descriptor
                                                               && descriptor.RequiresAuthorization()))
            {
                operation.Value.Security.Add(new()
                {
                    [new() { Reference = new() { Id = "Bearer", Type = ReferenceType.SecurityScheme } }] = []
                });
            }
            
            
        }
    }
}

file static class OpenApiExtensions
{
    public static bool RequiresAuthorization(this ActionDescriptor actionDescriptor) =>
        actionDescriptor.EndpointMetadata
                        .OfType<AuthorizeAttribute>()
                        .Any()
     && !actionDescriptor.EndpointMetadata
                         .OfType<AllowAnonymousAttribute>()
                         .Any();

    public static ActionDescriptor? GetActionDescriptor(this OpenApiDocumentTransformerContext context, OpenApiOperation apiOperation) =>
        apiOperation.Annotations.TryGetValue("x-aspnetcore-id", out var obj)
     && obj is string id
            ? context.DescriptionGroups
                     .SelectMany(group => group.Items)
                     .FirstOrDefault(description => description.ActionDescriptor.Id == id)
                    ?.ActionDescriptor
            : null;
}
