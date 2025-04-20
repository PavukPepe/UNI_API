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
            var query = _context.Courses.Include(c => c.Category).AsQueryable();

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
                    isApproved = c.IsApproved,
                    category = c.Category.CategoryName,
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
                    image = !string.IsNullOrEmpty(c.CourseLogo) ? c.CourseLogo : "/course.png",
                    status = c.IsApproved,
                    categoryId = c.CategoryId // Добавляем для фронтенда
                })
                .ToListAsync();

            return Ok(courses);
        }

        [HttpGet("sales")]
        public async Task<ActionResult> GetSales([FromQuery] int authorId, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            // Устанавливаем диапазон за последнюю неделю, если даты не переданы
            var end = endDate ?? DateTime.Now;
            var start = startDate ?? end.AddDays(-7);

            // Получаем курсы автора
            var courses = await _context.Courses
                .Where(c => c.AuthorId == authorId)
                .Select(c => new { c.CourseId, c.CourseTitle })
                .ToListAsync();

            // Получаем данные о продажах
            var sales = await _context.Payments
                .Where(p => p.PaymentDate >= start && p.PaymentDate <= end && (courses.Select(c => c.CourseId)).Any(c => c == p.CourseId))
                .GroupBy(p => p.CourseId)
                .Select(g => new
                {
                    CourseId = g.Key,
                    SalesCount = g.Count(),
                    TotalRevenue = g.Sum(p => p.PaymentAmount), // Предполагаем, что Amount есть в модели Payment
                    Sales = g.Select(p => new
                    {
                        Date = p.PaymentDate,
                        Amount = p.PaymentAmount
                    }).ToList()
                })
                .ToListAsync();

            // Формируем результат, включая курсы без продаж
            var result = courses.Select(c => new
            {
                CourseId = c.CourseId,
                CourseTitle = c.CourseTitle,
                SalesCount = sales.FirstOrDefault(s => s.CourseId == c.CourseId)?.SalesCount ?? 0,
                TotalRevenue = sales.FirstOrDefault(s => s.CourseId == c.CourseId)?.TotalRevenue ?? 0,
                Sales = sales.FirstOrDefault(s => s.CourseId == c.CourseId)?.Sales
            }).ToList();

            return Ok(result);
        }

        // GET: api/courses/{id}/progress
        [HttpGet("{id}/progress")]
        public async Task<ActionResult> GetCourseProgress(int id)
        {
            // Проверяем существование курса
            var course = await _context.Courses
                .Include(c => c.Blocks)
                    .ThenInclude(b => b.Topics)
                        .ThenInclude(t => t.Steps)
                .FirstOrDefaultAsync(c => c.CourseId == id);

            if (course == null)
            {
                return NotFound();
            }

            // Получаем всех студентов курса
            var studentIds = await _context.Payments
                .Where(p => p.CourseId == id)
                .Select(p => p.UserId)
                .Distinct()
                .ToListAsync();

            // Получаем общее количество шагов в курсе
            var totalSteps = course.Blocks
                .SelectMany(b => b.Topics)
                .SelectMany(t => t.Steps)
                .Count();

            // Получаем прогресс каждого студента
            var progressData = await _context.Userprogresses
                .Where(up => up.Step.Topic.Block.CourseId == id && studentIds.Contains(up.UserId))
                .GroupBy(up => up.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    CompletedSteps = g.Count(up => up.IsCompleted ?? false)
                })
                .ToListAsync();

            // Рассчитываем средний прогресс и количество завершивших
            var totalStudents = studentIds.Count;
            var averageProgress = totalStudents > 0 && totalSteps > 0
                ? Math.Round(progressData.Sum(p => (double)p.CompletedSteps / totalSteps * 100) / totalStudents, 2)
                : 0;
            var completedCount = progressData.Count(p => p.CompletedSteps == totalSteps);

            var result = new
            {
                CourseId = id,
                TotalStudents = totalStudents,
                AverageProgress = averageProgress,
                CompletedCount = completedCount
            };

            return Ok(result);
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
                instructorImg = course.Author?.ProfilePicture ?? "/course.png",
                description = course.CourseDescription,
                logo = !string.IsNullOrEmpty(course.CourseLogo) ? course.CourseLogo : "/course.png",
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

        [HttpPut("approved/{id}")]
        public async Task<IActionResult> PutCourse(int id)
        {
            var existingCourse = await _context.Courses.FirstOrDefaultAsync(c => c.CourseId == id);

            if (existingCourse == null)
            {
                return NotFound();
            }

            existingCourse.IsApproved = !existingCourse.IsApproved;
            await _context.SaveChangesAsync();

            return Ok(new { id = existingCourse.CourseId });
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
            existingCourse.CoursePrice = Convert.ToInt32(courseDto.Price);
            existingCourse.CourseLogo = courseDto.ImageUrl;

            // Обработка блоков
            var existingBlockIds = existingCourse.Blocks.Select(b => b.BlockId).ToList();
            var newBlockIds = courseDto.Blocks.Where(b => b.Id.HasValue).Select(b => b.Id.Value).ToList();

            // Удаляем блоки, которых нет в новом DTO
            var blocksToRemove = existingCourse.Blocks
                .Where(b => !newBlockIds.Contains(b.BlockId))
                .ToList();
            foreach (var block in blocksToRemove)
            {
                foreach (var topic in block.Topics.ToList())
                {
                    _context.Steps.RemoveRange(topic.Steps);
                    _context.Topics.Remove(topic);
                }
                _context.Blocks.Remove(block);
            }

            // Обрабатываем каждый блок из DTO
            foreach (var blockDto in courseDto.Blocks)
            {
                Block block;
                if (blockDto.Id.HasValue && existingCourse.Blocks.Any(b => b.BlockId == blockDto.Id.Value))
                {
                    // Обновляем существующий блок
                    block = existingCourse.Blocks.First(b => b.BlockId == blockDto.Id.Value);
                    block.BlockTitle = blockDto.Title;
                    block.DisplayOrder = blockDto.Order;
                }
                else
                {
                    // Создаем новый блок
                    block = new Block
                    {
                        CourseId = id,
                        BlockTitle = blockDto.Title,
                        DisplayOrder = blockDto.Order
                    };
                    _context.Blocks.Add(block);
                    await _context.SaveChangesAsync(); // Сохраняем, чтобы получить BlockId
                }

                // Обработка тем в блоке
                var existingTopicIds = block.Topics.Select(t => t.TopicId).ToList();
                var newTopicIds = blockDto.Topics.Where(t => t.Id.HasValue).Select(t => t.Id.Value).ToList();

                // Удаляем темы, которых нет в новом DTO
                var topicsToRemove = block.Topics
                    .Where(t => !newTopicIds.Contains(t.TopicId))
                    .ToList();
                foreach (var topic in topicsToRemove)
                {
                    _context.Steps.RemoveRange(topic.Steps);
                    _context.Topics.Remove(topic);
                }

                foreach (var topicDto in blockDto.Topics)
                {
                    Topic topic;
                    if (topicDto.Id.HasValue && block.Topics.Any(t => t.TopicId == topicDto.Id.Value))
                    {
                        // Обновляем существующую тему
                        topic = block.Topics.First(t => t.TopicId == topicDto.Id.Value);
                        topic.TopicTitle = topicDto.Title;
                        topic.DisplayOrder = topicDto.Order;
                    }
                    else
                    {
                        // Создаем новую тему
                        topic = new Topic
                        {
                            BlockId = block.BlockId,
                            TopicTitle = topicDto.Title,
                            DisplayOrder = topicDto.Order
                        };
                        _context.Topics.Add(topic);
                        await _context.SaveChangesAsync(); // Сохраняем, чтобы получить TopicId
                    }

                    // Обработка шагов в теме
                    var existingStepIds = topic.Steps.Select(s => s.StepId).ToList();
                    var newStepIds = topicDto.Steps.Where(s => s.Id.HasValue).Select(s => s.Id.Value).ToList();

                    // Удаляем шаги, которых нет в новом DTO
                    var stepsToRemove = topic.Steps
                        .Where(s => !newStepIds.Contains(s.StepId))
                        .ToList();
                    _context.Steps.RemoveRange(stepsToRemove);

                    foreach (var stepDto in topicDto.Steps)
                    {
                        Step step;
                        if (stepDto.Id.HasValue && topic.Steps.Any(s => s.StepId == stepDto.Id.Value))
                        {
                            // Обновляем существующий шаг
                            step = topic.Steps.First(s => s.StepId == stepDto.Id.Value);
                            step.StepTitle = stepDto.Title;
                            step.ContentType = stepDto.Type;
                            step.StepContent = stepDto.Content;
                            step.DisplayOrder = stepDto.Order;
                        }
                        else
                        {
                            // Создаем новый шаг
                            step = new Step
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
            }

            await _context.SaveChangesAsync();
            return Ok(new { existingCourse.CourseId });
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
                CoursePrice = Convert.ToInt32(courseDto.Price),
                CreationDate = DateTime.Now,
                IsApproved = false,
                CourseLogo = courseDto.ImageUrl
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
            public int CategoryId { get; set; }
            public string UserId { get; set; }
            public int Price { get; set; }
            public string? ImageUrl { get; set; }
            public List<BlockDto> Blocks { get; set; }
        }

        public class BlockDto
        {
            public int? Id { get; set; } // Id для существующих блоков, null для новых
            public string Title { get; set; }
            public int Order { get; set; }
            public List<TopicDto> Topics { get; set; }
        }

        public class TopicDto
        {
            public int? Id { get; set; } // Id для существующих тем, null для новых
            public string Title { get; set; }
            public int Order { get; set; }
            public List<StepDto> Steps { get; set; }
        }

        public class StepDto
        {
            public int? Id { get; set; } // Id для существующих шагов, null для новых
            public string Title { get; set; }
            public string Type { get; set; }
            public string Content { get; set; }
            public int Order { get; set; }
        }
    }
}