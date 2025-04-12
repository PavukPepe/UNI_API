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
        public async Task<ActionResult<IEnumerable<Course>>> GetCourses([FromQuery] int? categoryId)
        {
            var query = _context.Courses.AsQueryable();

            if (categoryId.HasValue)
            {
                query = query.Where(c => c.CategoryId == categoryId.Value);
            }

            var courses = await query
                .Select(c => new
                {
                    courseId = c.CourseId, // Соответствует ожидаемому полю в CourseCard
                    title = c.CourseTitle,
                    description = c.CourseDescription,
                    image = c.CourseLogo ?? "/course.png",
                    rating = c.Reviews.Select(c => c.UserRating).Average() ?? 0,
                    review = c.Reviews.Count(),
                    students = _context.Users.Include(u => u.Payments).Count(uc => uc.Payments.Any(cr => cr.CourseId == c.CourseId)),
                    instructor = c.Author.FullName,
                    price = c.CoursePrice
                })
                .ToListAsync();

            return Ok(courses);
        }

        // GET: api/Courses/own
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
                    students = _context.Users.Include(u => u.Payments).Count(uc => uc.Payments.Any(cr => cr.CourseId == c.CourseId)),
                    rating = Math.Round(c.Reviews.Select(c => c.UserRating).Average() ?? 0d, 2),
                    progress = 100, // Пока захардкодим
                    image = c.CourseLogo ?? "/course.png",
                    categoryId = c.CategoryId // Добавляем для фронтенда
                })
                .ToListAsync();

            return Ok(courses);
        }

        // GET: api/Courses/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult> GetCourse(int id, [FromQuery] int? userId = null)
        {
            var course = await _context.Courses
                .Include(c => c.Blocks)
                    .ThenInclude(b => b.Topics)
                        .ThenInclude(t => t.Steps)
                .Include(c => c.Author)
                .Include(c => c.Reviews)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null) return NotFound();

            var progress = userId.HasValue
                ? await _context.Userprogresses
                    .Where(up => up.UserId == userId && up.Step.Topic.Block.CourseId == id)
                    .ToListAsync()
                : null;

            var result = new
            {
                id = course.CourseId,
                title = course.CourseTitle,
                price = course.CoursePrice,
                instructor = course.Author?.FullName ?? "Неизвестный автор",
                description = course.CourseDescription,
                logo = course.CourseLogo ?? "/course.png",
                categoryId = course.CategoryId,
                reviews = course.Reviews.Select(r => new
                {
                    id = r.ReviewId,
                    text = r.ReviewText,
                    userName = r.User?.FullName ?? "Аноним",
                    date = r.SubmissionDate,
                    rating = r.UserRating
                }).ToList(),
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
                            content = s.StepContent,
                            type = s.ContentType,
                            completed = progress != null && progress.Any(p => p.StepId == s.StepId && (p.IsCompleted ?? false))
                        })
                    })
                })
            };

            return Ok(result);
        }

        // PUT: api/Courses/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCourse(int id, [FromBody] CourseDto courseDto)
        {
            var existingCourse = await _context.Courses
                .Include(c => c.Blocks)
                    .ThenInclude(b => b.Topics)
                        .ThenInclude(t => t.Steps)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (existingCourse == null)
            {
                return NotFound();
            }

            // Обновляем основные поля курса
            existingCourse.CourseTitle = courseDto.Title;
            existingCourse.CourseDescription = courseDto.Description;
            existingCourse.CategoryId = courseDto.CategoryId;
            existingCourse.AuthorId = Convert.ToInt32(courseDto.UserId);

            // Удаляем старые блоки, темы и шаги
            foreach (var block in existingCourse.Blocks.ToList())
            {
                foreach (var topic in block.Topics.ToList())
                {
                    _context.Steps.RemoveRange(topic.Steps);
                    _context.Topics.Remove(topic);
                }
                _context.Blocks.Remove(block);
            }

            // Добавляем новые блоки, темы и шаги
            foreach (var blockDto in courseDto.Blocks)
            {
                var block = new Block
                {
                    CourseId = id,
                    BlockTitle = blockDto.Title,
                    DisplayOrder = blockDto.Order
                };
                _context.Blocks.Add(block);
                await _context.SaveChangesAsync(); // Сохраняем, чтобы получить BlockId

                foreach (var topicDto in blockDto.Topics)
                {
                    var topic = new Topic
                    {
                        BlockId = block.BlockId,
                        TopicTitle = topicDto.Title,
                        DisplayOrder = topicDto.Order
                    };
                    _context.Topics.Add(topic);
                    await _context.SaveChangesAsync(); // Сохраняем, чтобы получить TopicId

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

            try
            {
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при обновлении курса: {ex.Message}");
            }
        }

        // POST: api/Courses
        [HttpPost]
        public IActionResult CreateCourse([FromBody] CourseDto courseDto)
        {
            var course = new Course
            {
                CourseTitle = courseDto.Title,
                CourseDescription = courseDto.Description,
                CategoryId = courseDto.CategoryId,
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

        // DELETE: api/Courses/{id}
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

        // DTO классы остаются без изменений
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
    }
}