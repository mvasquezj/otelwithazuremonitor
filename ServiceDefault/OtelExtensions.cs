using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
// using Serilog;

namespace ServiceDefault;

// Adds common Aspire services: service discovery, resilience, health checks, and OpenTelemetry.
// This project should be referenced by each service project in your solution.
// To learn more about using this project, see https://aka.ms/dotnet/aspire/service-defaults
public static class OtelExtensions
{
    private const string HealthEndpointPath = "/health";
    private const string AlivenessEndpointPath = "/alive";

    extension<TBuilder>(TBuilder builder) where TBuilder : IHostApplicationBuilder
    {
        public TBuilder AddBaggageItem(string key, string value)
        {
            Baggage.SetBaggage(key, value);
            return builder;
        }
        
        public TBuilder AddServiceDefaults()
        {
            builder.ConfigureOpenTelemetry();
            builder.AddDefaultHealthChecks();
            
            builder.Services.ConfigureHttpClientDefaults(http =>
            {
                http.AddStandardResilienceHandler();
            });
            return builder;
        }

        private void ConfigureOpenTelemetry()
        {
            builder.Logging.ClearProviders();
            builder.Logging.AddOpenTelemetry(logging =>
            {
                logging.IncludeFormattedMessage = true;
                logging.IncludeScopes = true;
            });

            builder.Services.AddOpenTelemetry()
                .WithLogging(loggin =>
                {
                    loggin.AddConsoleExporter();
                })
                .ConfigureResource(resource => resource
                    .AddService(
                        serviceName: builder.Configuration["OpenTelemetry:ServiceName"]!,
                        serviceVersion: builder.Configuration["OpenTelemetry:ServiceVersion"]!,
                        serviceInstanceId: Environment.MachineName))
                .WithMetrics(metrics =>
                {
                    metrics.AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation();
                })
                .WithTracing(tracing =>
                {
                    tracing.AddProcessor(new BaggageToTagsProcessor());
                    tracing.AddAspNetCoreInstrumentation(options =>
                            {
                                options.Filter = context =>
                                    !context.Request.Path.StartsWithSegments(HealthEndpointPath)
                                    && !context.Request.Path.StartsWithSegments(AlivenessEndpointPath);
                            }
                        )
                        .AddHttpClientInstrumentation();
                });

            builder.AddOpenTelemetryExporters();
        }

        private void AddOpenTelemetryExporters()
        {
            if (!string.IsNullOrEmpty(builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]))
            {
                builder.Services.AddOpenTelemetry()
                    .UseAzureMonitorExporter();
            }
        }

        private void AddDefaultHealthChecks()
        {
            builder.Services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
        }
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return app;
        app.MapHealthChecks(HealthEndpointPath);
        app.MapHealthChecks(AlivenessEndpointPath, new HealthCheckOptions
        {
            Predicate = r => r.Tags.Contains("live")
        });
        return app;
    }
}