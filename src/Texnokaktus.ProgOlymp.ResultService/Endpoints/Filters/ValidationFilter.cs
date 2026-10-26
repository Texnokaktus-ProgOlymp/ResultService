using FluentValidation;
using FluentValidation.Results;

namespace Texnokaktus.ProgOlymp.ResultService.Endpoints.Filters;

internal static class ValidationFilter
{
    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        var parameters = context.MethodInfo.GetParameters()
                                .Select(parameter => new
                                {
                                    parameter.Position,
                                    ValidatorType = typeof(IValidator<>).MakeGenericType(parameter.ParameterType)
                                })
                                .ToArray();

        return async invocationContext =>
        {
            List<ValidationFailure> errors = [];

            foreach (var parameter in parameters)
            {
                if (invocationContext.Arguments[parameter.Position] is not { } argument
                 || invocationContext.HttpContext.RequestServices.GetService(parameter.ValidatorType) is not IValidator validator)
                {
                    continue;
                }

                var result = await validator.ValidateAsync(
                                 new ValidationContext<object>(argument),
                                 invocationContext.HttpContext.RequestAborted
                             );

                errors.AddRange(result.Errors);
            }

            if (errors.Count > 0)
            {
                return TypedResults.ValidationProblem(
                    errors.GroupBy(error => error.PropertyName)
                          .ToDictionary(
                               group => group.Key,
                               group => group.Select(error => error.ErrorMessage).ToArray()
                           )
                );
            }

            return await next(invocationContext);
        };
    }
}
