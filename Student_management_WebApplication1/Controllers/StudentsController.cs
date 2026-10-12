
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Student_management_WebApplication1.Data;
using Student_management_WebApplication1.Models;

namespace Student_management_WebApplication1.Controllers
{
    [ApiController]
    [Route("api/students")]
    public class StudentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public StudentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/students?search=computer
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Student>>> GetStudents(
            [FromQuery] string? search)
        {
            IQueryable<Student> query =
                _context.Students.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string term = search.Trim();

                query = query.Where(s =>
                    s.Name.Contains(term) ||
                    s.Department.Contains(term));
            }

            return Ok(await query.OrderBy(s => s.Id).ToListAsync());
        }

        // GET: api/students/1
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Student>> GetStudentById(int id)
        {
            var student = await _context.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
            {
                return NotFound(new
                {
                    message = $"Student with ID {id} was not found."
                });
            }

            return Ok(student);
        }

        // POST: api/students
        [HttpPost]
        public async Task<ActionResult<Student>> CreateStudent(Student student)
        {
            student.Name = student.Name.Trim();
            student.Email = student.Email.Trim().ToLowerInvariant();
            student.Department = student.Department.Trim();

            if (await _context.Students.AnyAsync(
                s => s.Email == student.Email))
            {
                return Conflict(new
                {
                    message = "A student with this email already exists."
                });
            }

            _context.Students.Add(student);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsDuplicateEmail(ex))
            {
                return Conflict(new
                {
                    message = "A student with this email already exists."
                });
            }

            return CreatedAtAction(
                nameof(GetStudentById),
                new { id = student.Id },
                student);
        }

        // PUT: api/students/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateStudent(
            int id, Student updatedStudent)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
            {
                return NotFound(new
                {
                    message = $"Student with ID {id} was not found."
                });
            }

            string email = updatedStudent.Email.Trim().ToLowerInvariant();

            if (await _context.Students.AnyAsync(
                s => s.Email == email && s.Id != id))
            {
                return Conflict(new
                {
                    message = "A student with this email already exists."
                });
            }

            student.Name = updatedStudent.Name.Trim();
            student.Email = email;
            student.Department = updatedStudent.Department.Trim();
            student.Age = updatedStudent.Age;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (IsDuplicateEmail(ex))
            {
                return Conflict(new
                {
                    message = "A student with this email already exists."
                });
            }

            return Ok(student);
        }

        // DELETE: api/students/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
            {
                return NotFound(new
                {
                    message = $"Student with ID {id} was not found."
                });
            }

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private static bool IsDuplicateEmail(DbUpdateException exception)
        {
            Exception? current = exception;

            while (current != null)
            {
                if (current is SqlException sqlException &&
                    (sqlException.Number == 2601 ||
                     sqlException.Number == 2627))
                {
                    return sqlException.Message.Contains(
                        "IX_Students_Email",
                        StringComparison.OrdinalIgnoreCase);
                }

                current = current.InnerException;
            }

            return false;
        }
    }
}
