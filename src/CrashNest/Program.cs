using CrashNest.Middlewares;
using CrashNest.Storage.Migrator;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration ()
#if DEBUG
    .Enrich.FromLogContext ()
    .MinimumLevel.Information ()
    .WriteTo.Console ()
#endif
    .CreateBootstrapLogger ();

await new Migrator ().ApplyMigrations ();

var builder = WebApplication.CreateBuilder ( args );

builder.Host.UseSerilog (
#if !DEBUG
    ( context, service, configuration ) =>
        configuration
            .ReadFrom.Configuration ( builder.Configuration )
            .ReadFrom.Services ( service )
#endif
);
builder.Services.AddControllers ();
builder.Services.AddEndpointsApiExplorer ();
builder.Services.AddSwaggerGen (
    options => {
        options.SwaggerDoc (
            "v1",
            new OpenApiInfo {
                Version = "v1",
                Title = "CrashNest",
                Description = "WebAPI for CrashNest",
                TermsOfService = new Uri ( "https://example.com/terms" ),
                Contact = new OpenApiContact {
                    Name = "Example Contact",
                    Url = new Uri ( "https://example.com/contact" )
                },
                License = new OpenApiLicense {
                    Name = "Example License",
                    Url = new Uri ( "https://example.com/license" )
                }
            }
        );
    }
);
ServiceRegistration.RegistrateServices ( builder.Services );

var app = builder.Build ();

if ( !app.Environment.IsDevelopment () ) app.UseExceptionHandler ( "/Error" );

app.UseMiddleware<LoggerMiddleware> ();

app.UseSerilogRequestLogging ();

app.UseStaticFiles ();

app.UseSwagger ();
app.UseSwaggerUI (
    options => {
        options.SwaggerEndpoint ( "/swagger/v1/swagger.json", "v1" );
        options.RoutePrefix = "swagger";
    }
);

app.UseRouting ();

app.MapControllers ();

app.Run ();

Log.CloseAndFlush ();
