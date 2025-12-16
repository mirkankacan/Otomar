using FluentValidation;

namespace Otomar.WebApi.Filters
{
    public class ValidationFilter<T> : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
            if (validator is null)
                return await next(context);

            // Tek obje kontrolü
            var model = context.Arguments.OfType<T>().FirstOrDefault();
            if (model is not null)
            {
                var result = await validator.ValidateAsync(model);
                if (!result.IsValid)
                    return Results.ValidationProblem(result.ToDictionary());
            }

            // Liste kontrolü
            var listModel = context.Arguments.OfType<IEnumerable<T>>().FirstOrDefault();
            if (listModel is not null)
            {
                var errors = new Dictionary<string, string[]>();
                int index = 0;

                foreach (var item in listModel)
                {
                    var result = await validator.ValidateAsync(item);
                    if (!result.IsValid)
                    {
                        foreach (var error in result.Errors)
                        {
                            var key = $"[{index}].{error.PropertyName}";
                            if (!errors.ContainsKey(key))
                                errors[key] = Array.Empty<string>();
                            errors[key] = errors[key].Append(error.ErrorMessage).ToArray();
                        }
                    }
                    index++;
                }

                if (errors.Any())
                    return Results.ValidationProblem(errors);
            }

            return await next(context);
        }
    }
}