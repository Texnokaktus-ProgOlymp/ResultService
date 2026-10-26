namespace Texnokaktus.ProgOlymp.ResultService.Endpoints.Filters;

internal static class EnumValidationFilter
{
    public static EndpointFilterDelegate Create(EndpointFilterFactoryContext context, EndpointFilterDelegate next)
    {
        var parameters = context.MethodInfo.GetParameters()
            .Where(parameter => (Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType).IsEnum)
            .ToArray();

        if (parameters.Length == 0)
            return next;

        return async invocationContext =>
        {
            Dictionary<string, string[]> errors = [];
            foreach (var parameter in parameters)
                if (invocationContext.Arguments[parameter.Position] is Enum value && !Enum.IsDefined(value.GetType(), value))
                    errors.Add(parameter.Name!, [$"The value '{value}' is not valid for {value.GetType().Name}."]);

            if (errors.Count > 0)
                return TypedResults.ValidationProblem(errors);

            return await next(invocationContext);
        };
    }
}
