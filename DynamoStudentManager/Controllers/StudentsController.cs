using Amazon.DynamoDBv2.DataModel;
using DynamoStudentManager.Models;
using Microsoft.AspNetCore.Mvc;
using Prometheus;
using Serilog;

namespace DynamoStudentManager.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StudentsController : ControllerBase
{
    private static readonly Counter metricStatusCode = Metrics.CreateCounter("api_statuscode", "Endpoint StatusCode counter", new CounterConfiguration
    {
        LabelNames = new[] { "controller", "endpoint", "statuscode" }
    });

    private readonly IDynamoDBContext _context;

    public StudentsController(IDynamoDBContext context)
    {
        _context = context;
    }

    [HttpGet("{studentId}")]
    public async Task<IActionResult> GetById(int studentId)
    {
        var student = await _context.LoadAsync<Student>(studentId);
        if (student == null)
        {
            Log.Warning("StudentId {@studentId} not found", studentId);
            metricStatusCode.WithLabels("students", "GetById", "404").Inc();
            return NotFound();
        }
        Log.Information("Get a student {@student}", student);
        metricStatusCode.WithLabels("students", "GetById", "200").Inc();
        return Ok(student);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllStudents()
    {
        var student = await _context.ScanAsync<Student>(default).GetRemainingAsync();
        Log.Information("Get all students {@student}", student);
        metricStatusCode.WithLabels("students", "GetAll", "200").Inc();
        return Ok(student);
    }

    [HttpPost]
    public async Task<IActionResult> CreateStudent(Student studentRequest)
    {
        var student = await _context.LoadAsync<Student>(studentRequest.Id);
        if (student != null)
        {
            Log.Warning("Student with Id {@student} already exists", student.Id);
            metricStatusCode.WithLabels("students", "Create", "400").Inc();
            return BadRequest($"Student with Id {studentRequest.Id} Already Exists");
        }
        studentRequest.Created = DateTime.Now;
        Log.Information("Created a student {@student}", student);
        await _context.SaveAsync(studentRequest);
        metricStatusCode.WithLabels("students", "Create", "200").Inc();
        return Ok(studentRequest);
    }

    [HttpDelete("{studentId}")]
    public async Task<IActionResult> DeleteStudent(int studentId)
    {
        var student = await _context.LoadAsync<Student>(studentId);
        if (student == null)
        {
            Log.Warning("StudentId {@studentId} not found", studentId);
            metricStatusCode.WithLabels("students", "Delete", "404").Inc();
            return NotFound();
        }
        Log.Information("Deleting a student {@student}", student);
        await _context.DeleteAsync(student);
        metricStatusCode.WithLabels("students", "Delete", "200").Inc();
        return NoContent();
    }

    [HttpPut]
    public async Task<IActionResult> UpdateStudent(Student studentRequest)
    {
        var student = await _context.LoadAsync<Student>(studentRequest.Id);
        if (student == null)
        {
            Log.Warning("StudentId {@studentRequest} not found", studentRequest.Id);
            metricStatusCode.WithLabels("students", "Update", "404").Inc();
            return NotFound();
        }
        studentRequest.Updated = DateTime.Now;
        Log.Information("Updated a student {@student}", student);
        await _context.SaveAsync(studentRequest);
        metricStatusCode.WithLabels("students", "Update", "200").Inc();
        return Ok(studentRequest);
    }
}