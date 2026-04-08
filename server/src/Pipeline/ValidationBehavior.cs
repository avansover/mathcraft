using FluentValidation;
using Mathcraft.Server.Common;
using MediatR;

namespace Mathcraft.Server.Pipeline;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : class
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);
        var failures = validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count == 0)
            return await next(cancellationToken);

        var errors = string.Join("; ", failures.Select(f => f.ErrorMessage));

        // TResponse is always Result<T> — construct the failure via reflection
        var resultType = typeof(TResponse);
        var failMethod = resultType.GetMethod("Fail");
        if (failMethod is not null)
        {
            var result = failMethod.Invoke(null, [errors, ErrorCode.Validation]);
            return (TResponse)result!;
        }

        throw new ValidationException(failures);
    }
}
