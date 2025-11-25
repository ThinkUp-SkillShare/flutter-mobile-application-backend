using Microsoft.AspNetCore.Mvc;
using SkillShareBackend.DTOs;
using SkillShareBackend.Models;
using SkillShareBackend.Services;

namespace SkillShareBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StudentController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly ILogger<StudentController> _logger;

        public StudentController(IStudentService studentService, ILogger<StudentController> logger)
        {
            _studentService = studentService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StudentDto>>> GetAllStudents()
        {
            try
            {
                var students = await _studentService.GetAllStudentsAsync();
                var studentDtos = students.Select(s => MapToDto(s));
                return Ok(studentDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all students");
                return StatusCode(500, new { message = "An error occurred while getting students" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<StudentDto>> GetStudentById(int id)
        {
            try
            {
                var student = await _studentService.GetStudentByIdAsync(id);
                if (student == null)
                {
                    return NotFound(new { message = "Student not found" });
                }

                return Ok(MapToDto(student));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student by id: {Id}", id);
                return StatusCode(500, new { message = "An error occurred while getting student" });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<StudentDto>> GetStudentByUserId(int userId)
        {
            try
            {
                var student = await _studentService.GetStudentByUserIdAsync(userId);
                if (student == null)
                {
                    return NotFound(new { message = "Student not found for this user" });
                }

                return Ok(MapToDto(student));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting student by user id: {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while getting student" });
            }
        }

        [HttpPost]
        public async Task<ActionResult<StudentDto>> CreateStudent([FromBody] CreateStudentDto studentDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var student = await _studentService.CreateStudentAsync(studentDto);
                return CreatedAtAction(nameof(GetStudentById), new { id = student.Id }, MapToDto(student));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating student");
                return StatusCode(500, new { message = "An error occurred while creating student" });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<StudentDto>> UpdateStudent(int id, [FromBody] UpdateStudentDto studentDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var student = await _studentService.UpdateStudentAsync(id, studentDto);
                if (student == null)
                {
                    return NotFound(new { message = "Student not found" });
                }

                return Ok(MapToDto(student));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating student with id: {Id}", id);
                return StatusCode(500, new { message = "An error occurred while updating student" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteStudent(int id)
        {
            try
            {
                var result = await _studentService.DeleteStudentAsync(id);
                if (!result)
                {
                    return NotFound(new { message = "Student not found" });
                }

                return Ok(new { message = "Student deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting student with id: {Id}", id);
                return StatusCode(500, new { message = "An error occurred while deleting student" });
            }
        }

        private static StudentDto MapToDto(Student student)
        {
            var studentDto = new StudentDto
            {
                Id = student.Id,
                FirstName = student.FirstName,
                LastName = student.LastName,
                Nickname = student.Nickname,
                DateBirth = student.DateBirth,
                Country = student.Country,
                EducationalCenter = student.EducationalCenter,
                Gender = student.Gender,
                UserType = student.UserType,
                UserId = student.UserId
            };

            // Mapear el User si está cargado
            if (student.User != null)
            {
                studentDto.User = new UserDto
                {
                    UserId = student.User.UserId,
                    Email = student.User.Email,
                    ProfileImage = student.User.ProfileImage,
                    CreatedAt = student.User.CreatedAt
                };
            }

            return studentDto;
        }
    }
}