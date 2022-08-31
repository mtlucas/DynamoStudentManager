using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Serilog;
using Serilog.Settings.Configuration;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;
using Serilog.Sinks.Seq;
using System.Reflection;

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

    dotnet tool install Nuke.GlobalTool
    nuke :setup
    nuke :addpackage nuget.commandline
*/

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add logging - see below
    ConfigureLogging();
    // Add services to the container.
    builder.Host.UseSerilog();
    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    var awsOptions = builder.Configuration.GetAWSOptions();
    builder.Services.AddDefaultAWSOptions(awsOptions);
    builder.Services.AddAWSService<IAmazonDynamoDB>();
    builder.Services.AddScoped<IDynamoDBContext, DynamoDBContext>();

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
    app.MapControllers();
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
