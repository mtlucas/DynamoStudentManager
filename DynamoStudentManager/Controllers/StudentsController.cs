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

namespace DynamoStudentManager.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StudentsController : ControllerBase
{
    private static readonly Counter metricStatusCode = Metrics.CreateCounter("api_statuscode", "Endpoint StatusCode counter", new CounterConfiguration
    {
        LabelNames = new[] { "controller", "endpoint", "statuscode", "cached" }
    });

    private readonly IDynamoDBContext _context;
    private readonly IConfiguration _configuration;

    public StudentsController(IDynamoDBContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpGet("{studentId}")]
    public async Task<IActionResult> GetById(int studentId)
    {
        var cache = RedisConnectorHelper.Connection.GetDatabase();
        var studentCache = cache.HashGet("student", studentId);

        if (studentCache.IsNullOrEmpty)
        {
            var student = await _context.LoadAsync<Student>(studentId);
            if (student == null)
            {
                Log.Warning("StudentId {@studentId} not found", studentId);
                metricStatusCode.WithLabels("students", "GetById", "404", "false").Inc();
                return NotFound();
            }
            cache.HashSet("student", studentId, JsonConvert.SerializeObject(student));
            Log.Information("DATABASE: Get a student {@student}", student);
            metricStatusCode.WithLabels("students", "GetById", "200", "false").Inc();
            return Ok(student);
        }
        Log.Information("CACHE: Get a student {@studentCache}", studentCache);
        metricStatusCode.WithLabels("students", "GetById", "200", "true").Inc();
        return Ok(JsonConvert.DeserializeObject<Student>(studentCache));
    }

    [HttpGet]
    public async Task<IActionResult> GetAllStudents()
    {
        // Redis not implemented as cannot rely on it to have all Students cached
        var student = await _context.ScanAsync<Student>(default).GetRemainingAsync();
        Log.Information("Get all students {@student}", student);
        metricStatusCode.WithLabels("students", "GetAll", "200", "false").Inc();
        return Ok(student);
    }

    [HttpPost]
    public async Task<IActionResult> CreateStudent(Student studentRequest)
    {
        var cache = RedisConnectorHelper.Connection.GetDatabase();
        var student = await _context.LoadAsync<Student>(studentRequest.Id);
        if (student != null)
        {
            Log.Warning("Student with Id {@student} already exists", student.Id);
            metricStatusCode.WithLabels("students", "Create", "400", "false").Inc();
            return BadRequest($"Student with Id {studentRequest.Id} Already Exists");
        }
        studentRequest.Created = DateTime.Now;
        Log.Information("Created a student {@student}", student);
        await _context.SaveAsync(studentRequest);
        cache.HashSet("student", studentRequest.Id, JsonConvert.SerializeObject(studentRequest));
        metricStatusCode.WithLabels("students", "Create", "200", "true").Inc();
        return Ok(studentRequest);
    }

    [HttpDelete("{studentId}")]
    public async Task<IActionResult> DeleteStudent(int studentId)
    {
        var cache = RedisConnectorHelper.Connection.GetDatabase();
        var student = await _context.LoadAsync<Student>(studentId);
        if (student == null)
        {
            Log.Warning("StudentId {@studentId} not found", studentId);
            metricStatusCode.WithLabels("students", "Delete", "404", "false").Inc();
            return NotFound();
        }
        Log.Information("Deleting a student {@student}", student);
        await _context.DeleteAsync(student);
        cache.HashDelete("student", studentId);
        metricStatusCode.WithLabels("students", "Delete", "200", "true").Inc();
        return NoContent();
    }

    [HttpPut]
    public async Task<IActionResult> UpdateStudent(Student studentRequest)
    {
        var cache = RedisConnectorHelper.Connection.GetDatabase();
        var student = await _context.LoadAsync<Student>(studentRequest.Id);
        if (student == null)
        {
            Log.Warning("StudentId {@studentRequest} not found", studentRequest.Id);
            metricStatusCode.WithLabels("students", "Update", "404", "false").Inc();
            return NotFound();
        }
        studentRequest.Updated = DateTime.Now;
        Log.Information("Updated a student {student}", student);
        await _context.SaveAsync(studentRequest);
        cache.HashSet("student", studentRequest.Id, JsonConvert.SerializeObject(studentRequest));
        metricStatusCode.WithLabels("students", "Update", "200", "true").Inc();
        return Ok(studentRequest);
    }
}
