using System.Diagnostics;

namespace MVC.Middlewares
{
    public class RequestTimingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestTimingMiddleware> _logger;

        public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
        {
            this._next = next;
            this._logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            await _next(context);
            stopwatch.Stop();

            var elapsedMS = stopwatch.ElapsedMilliseconds;
            var method = context.Request.Method;
            var path = context.Request.Path;
            var statusCode = context.Response.StatusCode;

            // log the timing 
            if (elapsedMS > 1000)
            {
                _logger.LogWarning(
                    "Slow request: {Method} {Path} responded {StatusCode} in {Elapsed} ms",
                    method, path, statusCode, elapsedMS);
            }
            else
            {
                _logger.LogInformation(
                    "Request: {Method} {Path} responded {StatusCode} in {Elapsed} ms",
                    method, path, statusCode, elapsedMS);
            }
        }
    }
}
