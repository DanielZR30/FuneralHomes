namespace FH.Financials.Application.Common;

/// <summary>
/// Resultado de un caso de uso (versión compacta del patrón Result de Customers):
/// el handler indica éxito o fracaso con el código HTTP sugerido y el controller lo traduce.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, string? errorMessage, int statusCode)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        StatusCode = statusCode;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string? ErrorMessage { get; }
    public int StatusCode { get; }

    public static Result Success() => new(true, null, 200);
    public static Result Failure(string errorMessage, int statusCode = 400) => new(false, errorMessage, statusCode);
    public static Result NotFound(string errorMessage) => new(false, errorMessage, 404);
    public static Result Conflict(string errorMessage) => new(false, errorMessage, 409);
    public static Result UnprocessableEntity(string errorMessage) => new(false, errorMessage, 422);
}

/// <summary>Result con payload de éxito.</summary>
public sealed class Result<T> : Result
{
    private Result(bool isSuccess, T? value, string? errorMessage, int statusCode)
        : base(isSuccess, errorMessage, statusCode)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value) => new(true, value, null, 200);
    public new static Result<T> Failure(string errorMessage, int statusCode = 400) => new(false, default, errorMessage, statusCode);
    public new static Result<T> NotFound(string errorMessage) => new(false, default, errorMessage, 404);
    public new static Result<T> Conflict(string errorMessage) => new(false, default, errorMessage, 409);
    public new static Result<T> UnprocessableEntity(string errorMessage) => new(false, default, errorMessage, 422);
}
