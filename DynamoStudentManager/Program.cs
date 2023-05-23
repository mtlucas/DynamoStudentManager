using System;
using System.Net;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Serilog;
using Prometheus;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.AspNetCore;
using StackExchange.Redis.Extensions.Core.Configuration;
using StackExchange.Redis.Extensions.Newtonsoft;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using OpenTelemetry.Metrics;
using OpenTelemetry.Exporter;
using OpenTelemetry.Extensions.Hosting;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Exporter.OpenTelemetryProtocol;
using OpenTelemetry.Instrumentation.StackExchangeRedis;
using OpenTelemetry.Logs;
using OpenTelemetry;

/* Install Nuget packages:

    dotnet add package AWSSDK.DynamoDBv2
    dotnet add package AWSSDK.Extensions.NETCore.Setup
    dotnet add package serilog.aspnetcore
    dotnet add package serilog.sinks.seq
    dotnet add package serilog.expressions
    dotnet add package serilog.Settings.Configuration
    dotnet add package serilog.Settings.AppSettings
    dotnet add package serilog.sinks.Debug
    dotnet add package serilog.Exceptions
    dotnet add package serilog.sinks.Elasticsearch
    dotnet add package serilog.Enrichers.Environment
    dotnet add package StackExchange.Redis
    dotnet add package OpenTelemetry.Exporter.Console
    dotnet add package OpenTelemetry.Exporter.OpenTelemetryProtocol
    dotnet add package OpenTelemetry.Extensions.Hosting
    dotnet add package OpenTelemetry.Instrumentation.AspNetCore --prerelease
    dotnet add package OpenTelemetry.Instrumentation.Http --prerelease
    dotnet add package OpenTelemetry.Instrumentation.StackExchangeRedis --prerelease

    dotnet tool install Nuke.GlobalTool
    nuke :setup
    nuke :addpackage nuget.commandline
*/

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Init ConfigruationHelper for static classes
    ConfigurationHelper.Initialize(builder.Configuration);
    Uri _jaegerUri = new Uri(builder.Configuration.GetSection("Jaeger")["ConnectionString"] ?? "http://localhost:4317");
    // Add logging - see below
    ConfigureLogging();
    // Add services to the container.
    builder.Host.UseSerilog();
    builder.Services.AddHealthChecks();
    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    var awsOptions = builder.Configuration.GetAWSOptions();
    builder.Services.AddDefaultAWSOptions(awsOptions);
    builder.Services.AddAWSService<IAmazonDynamoDB>();
    builder.Services.AddScoped<IDynamoDBContext, DynamoDBContext>();
    // OpenTelemetry if enabled
    if (builder.Configuration.GetValue<bool>("Jaeger:OpenTelemetryEnable"))
    {
        var assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        builder.Services.AddOpenTelemetry()
            .WithTracing(tracerProviderBuilder => tracerProviderBuilder
                .SetSampler(new AlwaysOnSampler())
                .AddSource(DiagnosticsConfig.ActivitySource.Name)
                .ConfigureResource(resource => resource
                    .AddService(DiagnosticsConfig.ServiceName))
                .AddAspNetCoreInstrumentation(o =>
                {
                    o.EnrichWithHttpRequest = (activity, httpRequest) =>
                    {
                        activity.SetTag("requestProtocol", httpRequest.Protocol);
                    };
                    o.EnrichWithHttpResponse = (activity, httpResponse) =>
                    {
                        activity.SetTag("responseLength", httpResponse.ContentLength);
                    };
                    o.EnrichWithException = (activity, exception) =>
                    {
                        activity.SetTag("exceptionType", exception.GetType().ToString());
                    };
                    o.Filter = (httpContext) =>
                    {
                    // Filter Swagger, Metrics, Health and Favicon webrequests
                    return !(new[] { "swagger", "_framework", "_vs", "metrics", "health", "favicon" }.Any(c => httpContext.Request.Path.ToString().Contains(c)));
                    };
                })
                .AddHttpClientInstrumentation(o =>
                    o.FilterHttpRequestMessage = (httpContext) =>
                    {
                    // Filter Serilog endpoints
                    return !(new[] { "elastic", "newrelic", "mike-rig" }.Any(c => httpContext.RequestUri.ToString().Contains(c)));
                    }
                //*/
                )
                .AddRedisInstrumentation(RedisConnectorHelper.Connection, options => options.SetVerboseDatabaseStatements = true)
                .AddConsoleExporter()
                .AddOtlpExporter(opt =>
                    {
                        opt.Endpoint = _jaegerUri;
                        opt.Protocol = OtlpExportProtocol.Grpc;
                    })
            )
            .WithMetrics(metricsProviderBuilder => metricsProviderBuilder
                    .ConfigureResource(resource => resource
                        .AddService(DiagnosticsConfig.ServiceName))
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddConsoleExporter()
                        .AddOtlpExporter(opt =>
                            {
                                opt.Endpoint = _jaegerUri;
                                opt.Protocol = OtlpExportProtocol.Grpc;
                            })
            );
    }

    /*
        // Adds the OpenTracing with Jaeger -- deprecated
        builder.Services.AddOpenTracing();
        builder.Services.AddSingleton<ITracer>(sp =>
        {
            var serviceName = sp.GetRequiredService<IWebHostEnvironment>().ApplicationName;
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var reporter = new RemoteReporter.Builder()
                .WithLoggerFactory(loggerFactory)
                .WithSender(
                    new HttpSender(
                        // Value set by Tye
                        _jaegerUri.ToString()))
                .Build();
            var tracer = new Jaeger.Tracer.Builder(serviceName)
                // The constant sampler reports every span.
                .WithSampler(new ConstSampler(true))
                // LoggingReporter prints every reported span to the logging framework.
                .WithReporter(reporter)
                .Build();
            GlobalTracer.Register(tracer);
            return tracer;
        });

    builder.Services.Configure<HttpHandlerDiagnosticOptions>(options =>
    {
        options.IgnorePatterns.Add(request => _jaegerUri.IsBaseOf(request.RequestUri));
        options.IgnorePatterns.Add(request => request.RequestUri.ToString().Contains("elastic"));
    });

    builder.Services.Configure<AspNetCoreDiagnosticOptions>(options =>
    {
        options.Hosting.IgnorePatterns.Add(x =>
        {
            return x.Request.Path == "/health" || 
                x.Request.Path == "/metrics" ||
                x.Request.Path.ToString().Contains("swagger") ||
                x.Request.Path.ToString().Contains("_framework") ||
                x.Request.Path.ToString().Contains("_vs");
        });
    });
*/
    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseExceptionHandler("/Home/Error");
    }
    app.UseHttpsRedirection();
    app.UseAuthorization();
    app.MapHealthChecks("/health");
    app.UseRouting();
    app.UseMetricServer();
    app.MapControllers();
    app.MapMetrics();
    app.Run();
    Log.Information("DynamoStudentManager application started.");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception");
}
finally
{
    Log.Information("Shut down complete.");
    Log.CloseAndFlush();
}

void ConfigureLogging()
{
    var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    var configuration = new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
        .Build();

    Log.Logger = new LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .CreateLogger();

    Log.Debug("Starting logging....");
}

public static class ConfigurationHelper
{
    public static IConfiguration? config;
    public static void Initialize(IConfiguration Configuration)
    {
        config = Configuration;
    }
}
public static class DiagnosticsConfig
{
    public const string ServiceName = "DynamoStudentManager";
    public static ActivitySource ActivitySource = new ActivitySource(ServiceName);
}