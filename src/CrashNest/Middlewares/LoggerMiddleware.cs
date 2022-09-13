using CrashNest.Common.ResponseModels;
using Serilog.Context;
using System.Net.Mime;
using System.Text.Json;

namespace CrashNest.Middlewares {

    /// <summary>
    /// Middleware for enrich logger with additional parameters.
    /// </summary>
    public class LoggerMiddleware {

        private readonly RequestDelegate _next;

        private readonly ILogger<LoggerMiddleware> _logger;

        public LoggerMiddleware ( RequestDelegate next, ILogger<LoggerMiddleware> logger ) {
            _next = next;
            _logger = logger ?? throw new ArgumentNullException ( nameof ( logger ) );
        }

        public async Task InvokeAsync ( HttpContext context ) {
            var endpointInformation = $"{context.Request.Protocol} {context.Request.Method} {context.Request.Host}{context.Request.Path}{context.Request.QueryString.Value} {context.Request.ContentType} {context.Request.ContentLength}";

            var actionId = Guid.NewGuid ().ToString ();

            using ( var property = LogContext.PushProperty ( "ActionId", actionId ) ) {
                _logger.LogInformation ( $"Endpoint started {endpointInformation}" );

                try {
                    await _next ( context );
                } catch ( Exception e ) {
                    _logger.LogError ( e, "" );
                    _logger.LogInformation ( $"Endpoint finished with errors crashId({actionId}) {endpointInformation}" );
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = MediaTypeNames.Application.Json;

                    await JsonSerializer.SerializeAsync (
                        context.Response.Body,
                        new ResponseErrorModel ( "Something went wrong",/*TODO: implement error codes */ "", actionId )
                    );
                    return;
                }

                _logger.LogInformation ( $"Endpoint finished {endpointInformation}" );
            }
        }

    }

}
