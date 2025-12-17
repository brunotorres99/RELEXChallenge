using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace RELEX.InventoryManager.Api;

internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

        ProblemDetails problemDetails = new()
        {
            Status = httpContext.Response.StatusCode,
            Detail = exception.Message
        };

       if( exception is  FluentValidation.ValidationException ex)
        {
            problemDetails.Detail = "ValidationError";
            problemDetails.Status = StatusCodes.Status400BadRequest;
            problemDetails.Extensions["errors"] = new Dictionary<string, string[]>
            {
                {
                    "validationErrors",
                    ex.Errors.Select(e => e.ErrorMessage).ToArray()
                }
            };
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response
            .WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
