using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder ( args );

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

var app = builder.Build ();

if ( !app.Environment.IsDevelopment () ) app.UseExceptionHandler ( "/Error" );

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
