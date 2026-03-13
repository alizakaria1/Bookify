using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Serilog.Context;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace Bookify.Api.Middleware
{
    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class RequestContextLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestContextLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        private const string CorrelationIdHeaderName = "X-Correlation-Id";

        public Task Invoke(HttpContext httpContext)
        {

            using (LogContext.PushProperty("CorrelationId", GetCorrelationId(httpContext)))
            {
                return _next.Invoke(httpContext);
            }

        }

        private static string GetCorrelationId(HttpContext context)
        {
            context.Request.Headers.TryGetValue(
                CorrelationIdHeaderName,
                out StringValues correlationId);

            return correlationId.FirstOrDefault() ?? context.TraceIdentifier;
        }

    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class RequestContextLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestContextLoggingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestContextLoggingMiddleware>();
        }
    }
}
