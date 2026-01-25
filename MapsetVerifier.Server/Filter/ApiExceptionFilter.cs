using MapsetVerifier.Server.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;

namespace MapsetVerifier.Server.Filter;

public class ApiExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        Log.Information("Does this work?");
        var apiError = ExceptionService.GetApiError(context.Exception);

        context.Result = new JsonResult(apiError)
        {
            StatusCode = StatusCodes.Status500InternalServerError
        };

        context.ExceptionHandled = true;
    }
}
