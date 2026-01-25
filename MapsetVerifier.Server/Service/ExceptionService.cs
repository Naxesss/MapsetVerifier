using MapsetVerifier.Server.Model;

namespace MapsetVerifier.Server.Service;

public static class ExceptionService
{
    public static ApiError GetApiError(Exception exception)
    {
        var printedException = exception;
        var depth = 0;

        // Unwind AggregateException to get the most meaningful error
        while (printedException is AggregateException aggregateException && depth < 10) // Safety limit
        {
            Console.WriteLine($"Unwinding AggregateException at depth {depth}: {aggregateException.Message}");
            Console.WriteLine($"Inner exceptions count: {aggregateException.InnerExceptions.Count}");
        
            if (aggregateException.InnerExceptions.Count == 1)
            {
                // Single inner exception - use it directly
                printedException = aggregateException.InnerExceptions[0];
                depth++;
            }
            else if (aggregateException.InnerExceptions.Count > 1)
            {
                // Multiple exceptions - create a combined message
                var messages = aggregateException.InnerExceptions.Select(e => e.Message).ToArray();
                printedException = new Exception(
                    string.Join(" | ", messages),
                    aggregateException.InnerExceptions[0] // Keep first exception as inner for stack trace
                );
                break;
            }
            else
            {
                break; // No inner exceptions
            }
        }

        Console.WriteLine($"Final exception type: {printedException.GetType().Name}");
        Console.WriteLine($"Final exception message: {printedException.Message}");

        return new ApiError(
            message: printedException.Message,
            details: printedException.InnerException?.Message,
            stackTrace: printedException.StackTrace
        );
    }
}