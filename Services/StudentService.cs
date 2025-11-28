using Microsoft.EntityFrameworkCore;
using SkillShareBackend.Data;
using SkillShareBackend.DTOs;
using SkillShareBackend.Models;

namespace SkillShareBackend.Services;

public class StudentService : IStudentService
{
    private readonly AppDbContext _context;
    private readonly ILogger<StudentService> _logger;

    public StudentService(AppDbContext context, ILogger<StudentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Student>> GetAllStudentsAsync()
    {
        try
        {
            return await _context.Students
                .Include(s => s.User)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all students");
            throw;
        }
    }

    public async Task<Student?> GetStudentByIdAsync(int id)
    {
        try
        {
            return await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student by id: {Id}", id);
            throw;
        }
    }

    public async Task<Student?> GetStudentByUserIdAsync(int userId)
    {
        try
        {
            return await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student by user id: {UserId}", userId);
            throw;
        }
    }

    public async Task<Student> CreateStudentAsync(CreateStudentDto studentDto)
    {
        try
        {
            var student = new Student
            {
                FirstName = studentDto.FirstName,
                LastName = studentDto.LastName,
                Nickname = studentDto.Nickname,
                DateBirth = studentDto.DateBirth,
                Country = studentDto.Country,
                EducationalCenter = studentDto.EducationalCenter,
                Gender = studentDto.Gender,
                UserType = studentDto.UserType,
                UserId = studentDto.UserId
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            // Load the user relationship
            await _context.Entry(student)
                .Reference(s => s.User)
                .LoadAsync();

            return student;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating student");
            throw;
        }
    }

    public async Task<Student?> UpdateStudentAsync(int id, UpdateStudentDto studentDto)
    {
        try
        {
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null) return null;

            // Update only the provided fields
            if (!string.IsNullOrEmpty(studentDto.FirstName))
                student.FirstName = studentDto.FirstName;

            if (!string.IsNullOrEmpty(studentDto.LastName))
                student.LastName = studentDto.LastName;

            student.Nickname = studentDto.Nickname;
            student.DateBirth = studentDto.DateBirth;
            student.Country = studentDto.Country;
            student.EducationalCenter = studentDto.EducationalCenter;

            if (!string.IsNullOrEmpty(studentDto.Gender))
                student.Gender = studentDto.Gender;

            if (studentDto.UserType.HasValue)
                student.UserType = studentDto.UserType.Value;

            // Actualizar la imagen de perfil del usuario si se proporciona
            if (!string.IsNullOrEmpty(studentDto.ProfileImage) && student.User != null)
                student.User.ProfileImage = studentDto.ProfileImage;

            await _context.SaveChangesAsync();

            return student;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating student with id: {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteStudentAsync(int id)
    {
        try
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null) return false;

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting student with id: {Id}", id);
            throw;
        }
    }
}