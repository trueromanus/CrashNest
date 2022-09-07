using Serilog.Context;

namespace CrashNest.Middlewares {

    /// <summary>
    /// Middleware for enrich logger with additional parameters.
    /// </summary>
    public class LoggerMiddleware {

        private readonly RequestDelegate _next;

        private readonly ILogger<LoggerMiddleware> _logger;

        public LoggerMiddleware ( RequestDelegate next, ILogger<LoggerMiddleware> logger) {
            _next = next;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task InvokeAsync ( HttpContext context ) {
            var endpointInformation = $"{context.Request.Protocol} {context.Request.Method} {context.Request.Host}{context.Request.Path}{context.Request.QueryString.Value} {context.Request.ContentType} {context.Request.ContentLength}";

            using (var property = LogContext.PushProperty ( "ActionId", Guid.NewGuid().ToString() ) ) {
                _logger.LogInformation ( $"Endpoint started {endpointInformation}" );

                await _next ( context );

                _logger.LogInformation ( $"Endpoint finished {endpointInformation}" );
            }
        }

    }

}
