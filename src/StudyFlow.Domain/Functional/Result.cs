namespace StudyFlow.Domain.Functional;

public sealed record Result<T>
{
    public T? Value { get; }
    public string? Error { get; }
    public bool IsSuccess => Error is null;
    private Result(T? value, string? error) => (Value, Error) = (value, error);
    public static Result<T> Success(T value) => new(value, null);
    public static Result<T> Failure(string error) => new(default, error);
    public Result<U> Map<U>(Func<T, U> map) => IsSuccess ? Result<U>.Success(map(Value!)) : Result<U>.Failure(Error!);
    public Result<U> Bind<U>(Func<T, Result<U>> bind) => IsSuccess ? bind(Value!) : Result<U>.Failure(Error!);
}
