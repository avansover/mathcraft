using MediatR;
using Mathcraft.Server.Common;

namespace Mathcraft.Server.Pipeline;

public class ErrorHandlingBehavior<TRequest, TResponse>(ILogger<ErrorHandlingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : class
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception in handler for {Request}", typeof(TRequest).Name);

            var resultType = typeof(TResponse);
            if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Result<>))
            {
                var innerType = resultType.GetGenericArguments()[0];
                var failMethod = typeof(Result<>)
                    .MakeGenericType(innerType)
                    .GetMethod(nameof(Result<object>.Fail), [typeof(string), typeof(ErrorCode)]);

                return (TResponse)failMethod!.Invoke(null, ["An unexpected error occurred.", ErrorCode.None])!;
            }

            throw;
        }
    }
}
