using MapsetVerifier.Server.Model;

namespace MapsetVerifier.Server.Service;

public static class ExceptionService
{
    public static ApiError GetApiError(Exception exception)
    {
        var printedException = exception;

        while (printedException.InnerException != null && printedException is AggregateException)
            printedException = printedException.InnerException;

        var apiError = new ApiError(
            message: printedException.Message,
            details: printedException.InnerException?.Message,
            stackTrace: printedException.StackTrace
        );

        return apiError;
    }
}