using Amazon.DynamoDBv2.DataModel;
using DynamoStudentManager.Models;
using Microsoft.AspNetCore.Mvc;
using Prometheus;
using Serilog;

namespace DynamoClassesManager.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClassesController : ControllerBase
{
    private static readonly Counter metricStatusCode = Metrics.CreateCounter("api_statuscode", "Endpoint StatusCode counter", new CounterConfiguration
    {
        LabelNames = new[] { "controller", "endpoint", "statuscode" }
    });

    private readonly IDynamoDBContext _context;

    public ClassesController(IDynamoDBContext context)
    {
        _context = context;
    }

    [HttpGet("{classesId}")]
    public async Task<IActionResult> GetById(int classesId)
    {
        var classes = await _context.LoadAsync<CollegeClasses>(classesId);
        if (classes == null)
        {
            Log.Warning("ClassesId {@classesId} not found", classesId);
            metricStatusCode.WithLabels("classes", "GetById", "404").Inc();
            return NotFound();
        }
        Log.Information("Get a Class {@classes}", classes);
        metricStatusCode.WithLabels("classes", "GetById", "200").Inc();
        return Ok(classes);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllClasses()
    {
        var classes = await _context.ScanAsync<CollegeClasses>(default).GetRemainingAsync();
        Log.Information("Get all Classes {@classes}", classes);
        metricStatusCode.WithLabels("classes", "GetAll", "200").Inc();
        return Ok(classes);
    }

    [HttpPost]
    public async Task<IActionResult> CreateClasses(CollegeClasses classesRequest)
    {
        var classes = await _context.LoadAsync<CollegeClasses>(classesRequest.Id);
        if (classes != null)
        {
            Log.Warning("Classes with Id {@classes} already exists", classes.Id);
            metricStatusCode.WithLabels("classes", "Create", "400").Inc();
            return BadRequest($"Classes with Id {classesRequest.Id} Already Exists");
        }
        Log.Information("Created a Class {@classes}", classes);
        await _context.SaveAsync(classesRequest);
        metricStatusCode.WithLabels("classes", "Create", "200").Inc();
        return Ok(classesRequest);
    }

    [HttpDelete("{classesId}")]
    public async Task<IActionResult> DeleteClasses(int classesId)
    {
        var classes = await _context.LoadAsync<CollegeClasses>(classesId);
        if (classes == null)
        {
            Log.Warning("ClassesId {@classesId} not found", classesId);
            metricStatusCode.WithLabels("classes", "Delete", "404").Inc();
            return NotFound();
        }
        Log.Information("Deleting a Class {@classes}", classes);
        await _context.DeleteAsync(classes);
        metricStatusCode.WithLabels("classes", "Delete", "200").Inc();
        return NoContent();
    }

    [HttpPut]
    public async Task<IActionResult> UpdateClasses(CollegeClasses classesRequest)
    {
        var classes = await _context.LoadAsync<CollegeClasses>(classesRequest.Id);
        if (classes == null)
        {
            Log.Warning("ClassesId {@classesRequest} not found", classesRequest.Id);
            metricStatusCode.WithLabels("classes", "Update", "404").Inc();
            return NotFound();
        }
        Log.Information("Updated a Class {@classes}", classes);
        await _context.SaveAsync(classesRequest);
        metricStatusCode.WithLabels("classes", "Update", "200").Inc();
        return Ok(classesRequest);
    }
}
