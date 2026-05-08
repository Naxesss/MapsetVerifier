namespace MapsetVerifier.Server.Model;

public readonly struct ApiError(string message, string? details, string? stackTrace = null)
{
    public string Message { get; } = message;
    public string? Details { get; } = details;
    public string? StackTrace { get; } = stackTrace;
}

public static class ApiErrorFactory
{
    public static ApiError FromException(Exception ex, string message)
    {
        var root = Unwrap(ex);

        return new ApiError(message, root.Message, ex.ToString());
    }

    private static Exception Unwrap(Exception ex)
    {
        if (ex is AggregateException agg)
            return agg.Flatten().InnerExceptions.FirstOrDefault() ?? ex;

        return ex.InnerException ?? ex;
    }
}
