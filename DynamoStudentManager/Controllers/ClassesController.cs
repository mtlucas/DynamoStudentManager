using System;
using System.Reflection;
using System.Diagnostics;
using System.Configuration;
using System.Security.Principal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting.Server;
using Amazon.DynamoDBv2.DataModel;
using StackExchange.Redis;
using Prometheus;
using Serilog;
using Newtonsoft.Json;
using DynamoStudentManager.Models;

namespace DynamoClassesManager.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClassesController : ControllerBase
{
    private static readonly Counter metricStatusCode = Metrics.CreateCounter("api_statuscode", "Endpoint StatusCode counter", new CounterConfiguration
    {
        LabelNames = new[] { "controller", "endpoint", "statuscode", "cached" }
    });

    private readonly IDynamoDBContext _context;
    private readonly IConfiguration _configuration;

    public ClassesController(IConfiguration configuration, IDynamoDBContext context)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpGet("{classesId}")]
    public async Task<IActionResult> GetById(int classesId)
    {
        var cache = RedisConnectorHelper.Connection.GetDatabase();
        var classesCache = cache.HashGet("classes", classesId);

        if (classesCache.IsNullOrEmpty)
        {
            var classes = await _context.LoadAsync<CollegeClasses>(classesId);
            if (classes == null)
            {
                Log.Warning("ClassesId {@classesId} not found", classesId);
                metricStatusCode.WithLabels("classes", "GetById", "404", "false").Inc();
                return NotFound();
            }
            cache.HashSet("classes", classesId, JsonConvert.SerializeObject(classes));
            Log.Information("DATABASE: Get classes {@classes}", classes);
            metricStatusCode.WithLabels("classes", "GetById", "200", "false").Inc();
            return Ok(classes);
        }
        Log.Information("CACHE: Get classes {@classesCache}", classesCache);
        metricStatusCode.WithLabels("classes", "GetById", "200", "true").Inc();
        return Ok(JsonConvert.DeserializeObject<Classes>(classesCache));

    }

    [HttpGet]
    public async Task<IActionResult> GetAllClasses()
    {
        var classes = await _context.ScanAsync<CollegeClasses>(default).GetRemainingAsync();
        Log.Information("Get all Classes {@classes}", classes);
        metricStatusCode.WithLabels("classes", "GetAll", "200", "false").Inc();
        return Ok(classes);
    }

    [HttpPost]
    public async Task<IActionResult> CreateClasses(CollegeClasses classesRequest)
    {
        var cache = RedisConnectorHelper.Connection.GetDatabase();
        var classes = await _context.LoadAsync<CollegeClasses>(classesRequest.Id);
        if (classes != null)
        {
            Log.Warning("Classes with Id {@classes} already exists", classes.Id);
            metricStatusCode.WithLabels("classes", "Create", "400", "false").Inc();
            return BadRequest($"Classes with Id {classesRequest.Id} Already Exists");
        }
        Log.Information("Created a Class {@classes}", classes);
        await _context.SaveAsync(classesRequest);
        cache.HashSet("classes", classesRequest.Id, JsonConvert.SerializeObject(classesRequest));
        metricStatusCode.WithLabels("classes", "Create", "200", "true").Inc();
        return Ok(classesRequest);
    }

    [HttpDelete("{classesId}")]
    public async Task<IActionResult> DeleteClasses(int classesId)
    {
        var cache = RedisConnectorHelper.Connection.GetDatabase();
        var classes = await _context.LoadAsync<CollegeClasses>(classesId);
        if (classes == null)
        {
            Log.Warning("ClassesId {@classesId} not found", classesId);
            metricStatusCode.WithLabels("classes", "Delete", "404", "false").Inc();
            return NotFound();
        }
        Log.Information("Deleting a Class {@classes}", classes);
        await _context.DeleteAsync(classes);
        cache.HashDelete("classes", classesId);
        metricStatusCode.WithLabels("classes", "Delete", "200", "true").Inc();
        return NoContent();
    }

    [HttpPut]
    public async Task<IActionResult> UpdateClasses(CollegeClasses classesRequest)
    {
        var cache = RedisConnectorHelper.Connection.GetDatabase();
        var classes = await _context.LoadAsync<CollegeClasses>(classesRequest.Id);
        if (classes == null)
        {
            Log.Warning("ClassesId {@classesRequest} not found", classesRequest.Id);
            metricStatusCode.WithLabels("classes", "Update", "404", "false").Inc();
            return NotFound();
        }
        Log.Information("Updated a Class {@classes}", classes);
        await _context.SaveAsync(classesRequest);
        cache.HashSet("classes", classesRequest.Id, JsonConvert.SerializeObject(classesRequest));
        metricStatusCode.WithLabels("classes", "Update", "200", "true").Inc();
        return Ok(classesRequest);
    }
}
