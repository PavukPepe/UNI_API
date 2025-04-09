using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UNI.Models;

namespace UNI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CoursesController : ControllerBase
    {
        private readonly UniContext _context;

        public CoursesController(UniContext context)
        {
            _context = context;
        }

        // GET: api/Courses
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Course>>> GetCourses()
        {
            return await _context.Courses.ToListAsync();
        }

        //// GET: api/Courses/5
        //[HttpGet("{id}")]
        //public async Task<ActionResult<Course>> GetCourse(int id)
        //{
        //    var course = await _context.Courses.FindAsync(id);

        //    if (course == null)
        //    {
        //        return NotFound();
        //    }

        //    return course;
        //}

        [HttpGet("own")]
        public async Task<ActionResult> GetCourses([FromQuery] int? categoryId, [FromQuery] int? authorId)
        {
            var query = _context.Courses.Include(c => c.Author).AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(c => c.CategoryId == categoryId.Value);
            }
            if (authorId.HasValue)
            {
                query = query.Where(c => c.AuthorId == authorId.Value);
            }

            var courses = await query
                .Select(c => new
                {
                    id = c.CourseId,
                    title = c.CourseTitle,
                    description = c.CourseDescription,
                    students = _context.Users.Include(u => u.Payments).Count(uc => uc.Payments.Any(cr => cr.CourseId == c.CourseId)), // Количество студентов
                    rating = c.AverageRating ?? 0,
                    progress = 100, // Пока захардкодим, позже можно добавить логику
                    image = c.CourseLogo ?? "/placeholder.jpg"
                })
                .ToListAsync();

            return Ok(courses);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetCourse(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Blocks)
                    .ThenInclude(b => b.Topics)
                        .ThenInclude(t => t.Steps)
                .Include(c => c.Author)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; // Если авторизация включена
            var progress = userId != null
                ? await _context.Userprogresses
                    .Where(up => up.UserId == int.Parse(userId) && up.Step.Topic.Block.CourseId == id)
                    .ToListAsync()
                : null;

            var result = new
            {
                id = course.CourseId,
                title = course.CourseTitle,
                description = course.CourseDescription,
                blocks = course.Blocks.Select(b => new
                {
                    id = b.BlockId,
                    title = b.BlockTitle,
                    topics = b.Topics.Select(t => new
                    {
                        id = t.TopicId,
                        title = t.TopicTitle,
                        steps = t.Steps.Select(s => new
                        {
                            id = s.StepId,
                            title = s.StepTitle,
                            completed = progress != null && progress.Any(p => p.StepId == s.StepId && (p.IsCompleted ?? false))
                        })
                    })
                })
            };

            return Ok(result);
        }

        // PUT: api/Courses/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCourse(int id, Course course)
        {
            if (id != course.CourseId)
            {
                return BadRequest();
            }

            _context.Entry(course).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CourseExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        public IActionResult CreateCourse([FromBody] CourseDto courseDto)
        {
            var course = new Course
            {
                CourseTitle = courseDto.Title,
                CourseDescription = courseDto.Description,
                CategoryId = Convert.ToInt32(courseDto.CategoryId),
                AuthorId = Convert.ToInt32(courseDto.UserId),
                CreationDate = DateTime.Now,
                IsApproved = false
            };
            _context.Courses.Add(course);
            _context.SaveChanges();

            foreach (var blockDto in courseDto.Blocks)
            {
                var block = new Block
                {
                    CourseId = course.CourseId,
                    BlockTitle = blockDto.Title,
                    DisplayOrder = blockDto.Order
                };
                _context.Blocks.Add(block);
                _context.SaveChanges();

                foreach (var topicDto in blockDto.Topics)
                {
                    var topic = new Topic
                    {
                        BlockId = block.BlockId,
                        TopicTitle = topicDto.Title,
                        DisplayOrder = topicDto.Order
                    };
                    _context.Topics.Add(topic);
                    _context.SaveChanges();

                    foreach (var stepDto in topicDto.Steps)
                    {
                        var step = new Step
                        {
                            TopicId = topic.TopicId,
                            StepTitle = stepDto.Title,
                            ContentType = stepDto.Type,
                            StepContent = stepDto.Content,
                            DisplayOrder = stepDto.Order
                        };
                        _context.Steps.Add(step);
                    }
                }
            }
            _context.SaveChanges();
            return Ok(new { course.CourseId });
        }

        public class CourseDto
        {
            public string Title { get; set; }
            public string Description { get; set; }
            public string UserId { get; set; }
            public int CategoryId { get; set; }
            public List<BlockDto> Blocks { get; set; }
        }

        public class BlockDto
        {
            public string Title { get; set; }
            public int Order { get; set; }
            public List<TopicDto> Topics { get; set; }
        }

        public class TopicDto
        {
            public string Title { get; set; }
            public int Order { get; set; }
            public List<StepDto> Steps { get; set; }
        }

        public class StepDto
        {
            public string Title { get; set; }
            public string Type { get; set; }
            public string Content { get; set; }
            public int Order { get; set; }
        }

        // DELETE: api/Courses/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCourse(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null)
            {
                return NotFound();
            }

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CourseExists(int id)
        {
            return _context.Courses.Any(e => e.CourseId == id);
        }
    }
}
