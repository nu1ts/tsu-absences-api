using System.Net;
using tsu_absences_api.Exceptions;
using tsu_absences_api.Models;

namespace tsu_absences_api.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ErrorHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            EmailException emailEx => ((int)HttpStatusCode.BadRequest, emailEx.Message),
            LoginException loginEx => ((int)HttpStatusCode.BadRequest, loginEx.Message),
            UserException userEx => ((int)HttpStatusCode.NotFound, userEx.Message),
            ArgumentException argumentEx => ((int)HttpStatusCode.BadRequest, argumentEx.Message),
            _ => ((int)HttpStatusCode.InternalServerError, exception.Message)
        };

        context.Response.StatusCode = statusCode;

        return context.Response.WriteAsJsonAsync(new Response
        {
            Status = $"{statusCode} {Enum.GetName(typeof(HttpStatusCode), statusCode)}",
            Message = message
        });
    }
}