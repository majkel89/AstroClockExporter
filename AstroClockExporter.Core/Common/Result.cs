using System.Diagnostics.CodeAnalysis;

namespace AstroClockExporter.Core.Common;

public sealed record Result<TA>
{
    public TA? Value { get; }
    public Error? Err { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Err))]
    public bool IsSuccess { get; }

    [MemberNotNullWhen(true, nameof(Err))]
    [MemberNotNullWhen(false, nameof(Value))]
    public bool IsFail => Err is not null;

    private Result(in Error error)
    {
        IsSuccess = false;
        Err = error;
        Value = default;
    }

    private Result(in TA value)
    {
        IsSuccess = true;
        Err = null;
        Value = value;
    }

    public static Result<TA> Success(TA value) => new(in value);

    public static Result<TA> Fail(string error) => new(Error.New(error));
    public static Result<TA> Fail(int code, string error) => new(Error.New(code, error));
}
