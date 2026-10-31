namespace FZScout.Site;

/// <param name="Value">Set only on <see cref="ApiOutcome.Success"/>.</param>
/// <param name="Error">What went wrong, for the log. Set on every failure.</param>
record ApiResult<T>(ApiOutcome Outcome, T? Value, string? Error)
{
    public bool IsSuccess => Outcome == ApiOutcome.Success;

    public static ApiResult<T> Success(T value) => new(ApiOutcome.Success, value, null);

    public static ApiResult<T> Failure(ApiOutcome outcome, string error) => new(outcome, default, error);
}
