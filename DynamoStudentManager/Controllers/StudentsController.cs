using Amazon.DynamoDBv2.DataModel;
using DynamoStudentManager.Models;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace DynamoStudentManager.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StudentsController : ControllerBase
{
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
            return NotFound();
        }
        Log.Information("Get a student {@student}", student);
        return Ok(student);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllStudents()
    {
        var student = await _context.ScanAsync<Student>(default).GetRemainingAsync();
        Log.Information("Get all students {@student}", student);
        return Ok(student);
    }

    [HttpPost]
    public async Task<IActionResult> CreateStudent(Student studentRequest)
    {
        var student = await _context.LoadAsync<Student>(studentRequest.Id);
        if (student != null)
        {
            Log.Warning("Student with Id {@student} already exists", student.Id);
            return BadRequest($"Student with Id {studentRequest.Id} Already Exists");
        }
        studentRequest.Created = DateTime.Now;
        Log.Information("Created a student {@student}", student);
        await _context.SaveAsync(studentRequest);
        return Ok(studentRequest);
    }

    [HttpDelete("{studentId}")]
    public async Task<IActionResult> DeleteStudent(int studentId)
    {
        var student = await _context.LoadAsync<Student>(studentId);
        if (student == null)
        {
            Log.Warning("StudentId {@studentId} not found", studentId);
            return NotFound();
        }
        Log.Information("Deleting a student {@student}", student);
        await _context.DeleteAsync(student);
        return NoContent();
    }

    [HttpPut]
    public async Task<IActionResult> UpdateStudent(Student studentRequest)
    {
        var student = await _context.LoadAsync<Student>(studentRequest.Id);
        if (student == null)
        {
            Log.Warning("StudentId {@studentRequest} not found", studentRequest.Id);
            return NotFound();
        }
        studentRequest.Updated = DateTime.Now;
        Log.Information("Updated a student {@student}", student);
        await _context.SaveAsync(studentRequest);
        return Ok(studentRequest);
    }
}