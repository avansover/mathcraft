namespace Mathcraft.Server.Common;

public class Result<T>
{
    public bool Success { get; private init; }
    public T? Data { get; private init; }
    public string? Error { get; private init; }
    public ErrorCode ErrorCode { get; private init; }

    public static Result<T> Ok(T data) => new()
    {
        Success = true,
        Data = data,
        ErrorCode = ErrorCode.None
    };

    public static Result<T> Fail(string error, ErrorCode code) => new()
    {
        Success = false,
        Error = error,
        ErrorCode = code
    };
}
