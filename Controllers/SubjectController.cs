using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SkillShareBackend.Data;
using SkillShareBackend.DTOs;
using SkillShareBackend.Models;

namespace SkillShareBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubjectController : ControllerBase
{
    private readonly AppDbContext _context;

    public SubjectController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Subject
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SubjectDto>>> GetAllSubjects()
    {
        var subjects = await _context.Subjects
            .Select(s => new SubjectDto
            {
                Id = s.Id,
                Name = s.Name
            })
            .ToListAsync();

        return Ok(subjects);
    }

    // GET: api/Subject/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<SubjectDto>> GetSubject(int id)
    {
        var subject = await _context.Subjects.FindAsync(id);

        if (subject == null) return NotFound(new { message = "Subject not found" });

        return Ok(new SubjectDto
        {
            Id = subject.Id,
            Name = subject.Name
        });
    }

    // POST: api/Subject
    [HttpPost]
    public async Task<ActionResult<SubjectDto>> CreateSubject([FromBody] SubjectDto dto)
    {
        var subject = new Subject
        {
            Name = dto.Name
        };

        _context.Subjects.Add(subject);
        await _context.SaveChangesAsync();

        dto.Id = subject.Id;
        return CreatedAtAction(nameof(GetSubject), new { id = subject.Id }, dto);
    }

    // PUT: api/Subject/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSubject(int id, [FromBody] SubjectDto dto)
    {
        var subject = await _context.Subjects.FindAsync(id);

        if (subject == null) return NotFound(new { message = "Subject not found" });

        subject.Name = dto.Name;
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/Subject/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSubject(int id)
    {
        var subject = await _context.Subjects.FindAsync(id);

        if (subject == null) return NotFound(new { message = "Subject not found" });

        _context.Subjects.Remove(subject);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("popular")]
    public async Task<ActionResult<IEnumerable<object>>> GetPopularSubjects()
    {
        var subjects = await _context.Subjects
            .Select(s => new
            {
                s.Id,
                s.Name,
                GroupCount = s.StudyGroups.Count()
            })
            .OrderByDescending(s => s.GroupCount)
            .Take(6)
            .ToListAsync();

        return Ok(subjects);
    }
}