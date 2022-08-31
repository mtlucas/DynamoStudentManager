using Amazon.DynamoDBv2.DataModel;
using DynamoStudentManager.Models;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace DynamoClassesManager.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClassesController : ControllerBase
{
    private readonly IDynamoDBContext _context;

    public ClassesController(IDynamoDBContext context)
    {
        _context = context;
    }

    [HttpGet("{classesId}")]
    public async Task<IActionResult> GetById(int classesId)
    {
        var classes = await _context.LoadAsync<Classes>(classesId);
        if (classes == null)
        {
            Log.Warning("ClassesId {@classesId} not found", classesId);
            return NotFound();
        }
        Log.Information("Get a Class {@classes}", classes);
        return Ok(classes);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllClasses()
    {
        var classes = await _context.ScanAsync<Classes>(default).GetRemainingAsync();
        Log.Information("Get all Classes {@classes}", classes);
        return Ok(classes);
    }

    [HttpPost]
    public async Task<IActionResult> CreateClasses(Classes classesRequest)
    {
        var classes = await _context.LoadAsync<Classes>(classesRequest.Id);
        if (classes != null)
        {
            Log.Warning("Classes with Id {@classes} already exists", classes.Id);
            return BadRequest($"Classes with Id {classesRequest.Id} Already Exists");
        }
        Log.Information("Created a Class {@classes}", classes);
        await _context.SaveAsync(classesRequest);
        return Ok(classesRequest);
    }

    [HttpDelete("{classesId}")]
    public async Task<IActionResult> DeleteClasses(int classesId)
    {
        var classes = await _context.LoadAsync<Classes>(classesId);
        if (classes == null)
        {
            Log.Warning("ClassesId {@classesId} not found", classesId);
            return NotFound();
        }
        Log.Information("Deleting a Class {@classes}", classes);
        await _context.DeleteAsync(classes);
        return NoContent();
    }

    [HttpPut]
    public async Task<IActionResult> UpdateClasses(Classes classesRequest)
    {
        var classes = await _context.LoadAsync<Classes>(classesRequest.Id);
        if (classes == null)
        {
            Log.Warning("ClassesId {@classesRequest} not found", classesRequest.Id);
            return NotFound();
        }
        Log.Information("Updated a Class {@classes}", classes);
        await _context.SaveAsync(classesRequest);
        return Ok(classesRequest);
    }
}
