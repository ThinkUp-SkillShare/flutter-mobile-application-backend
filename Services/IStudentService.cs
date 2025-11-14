using SkillShareBackend.DTOs;
using SkillShareBackend.Models;

namespace SkillShareBackend.Services
{
    public interface IStudentService
    {
        Task<IEnumerable<Student>> GetAllStudentsAsync();
        Task<Student?> GetStudentByIdAsync(int id);
        Task<Student?> GetStudentByUserIdAsync(int userId);
        Task<Student> CreateStudentAsync(CreateStudentDto studentDto);
        Task<Student?> UpdateStudentAsync(int id, UpdateStudentDto studentDto);
        Task<bool> DeleteStudentAsync(int id);
    }
}