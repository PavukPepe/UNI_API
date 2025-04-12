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
    public class ReviewsController : ControllerBase
    {
        private readonly UniContext _context;

        public ReviewsController(UniContext context)
        {
            _context = context;
        }

        // GET: api/Reviews/course/{courseId} - Получить все отзывы для конкретного курса
        [HttpGet("course/{courseId}")]
        public async Task<ActionResult<IEnumerable<Review>>> GetReviewsForCourse(int courseId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.CourseId == courseId)
                .Include(r => r.User) // Подгружаем пользователя, если нужен
                .Select(r => new
                {
                    id = r.ReviewId,
                    text = r.ReviewText,
                    userName = r.User.FullName ?? "Аноним", // Имя пользователя или "Аноним"
                    date = r.SubmissionDate,
                    rating = r.UserRating // Рейтинг, если есть
                })
                .ToListAsync();

            //if (reviews == null || !reviews.Any())
            //{
            //    return NotFound(new { message = "Отзывов для этого курса не найдено." });
            //}

            return Ok(reviews);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Review>>> GetComments()
        {
            var comments = await _context.Reviews.ToListAsync();

            if (comments == null || !comments.Any())
            {
                return NotFound(new { message = "Комментарии не найдены" });
            }

            return Ok(comments);
        }

        // GET: api/Reviews/5 - Получить конкретный отзыв по ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Review>> GetReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);

            if (review == null)
            {
                return NotFound();
            }

            return review;
        }

        // PUT: api/Reviews/5 - Обновить отзыв
        [HttpPut("{id}")]
        public async Task<IActionResult> PutReview(int id, Review review)
        {
            if (id != review.ReviewId)
            {
                return BadRequest();
            }

            _context.Entry(review).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReviewExists(id))
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

        // POST: api/Reviews/course/{courseId} - Добавить новый отзыв для конкретного курса
        [HttpPost("course/{courseId}")]
        public async Task<ActionResult<Review>> PostReview(int courseId, [FromBody] Review review)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
            {
                return NotFound(new { message = "Курс не найден." });
            }

            // Устанавливаем данные для нового отзыва
            review.CourseId = courseId;
            review.SubmissionDate = DateTime.Now;
            review.UserRating = review.UserRating ?? 0; // Устанавливаем рейтинг по умолчанию, если null

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // Возвращаем отзыв в формате, который ожидает фронтенд
            var returnReview = new
            {
                id = review.ReviewId,
                text = review.ReviewText,
                userName = review.User?.FullName ?? "Аноним",
                date = review.SubmissionDate,
                rating = review.UserRating
            };

            return CreatedAtAction(nameof(GetReview), new { id = review.ReviewId }, returnReview);
        }

        // DELETE: api/Reviews/5 - Удалить отзыв
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
            {
                return NotFound();
            }

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ReviewExists(int id)
        {
            return _context.Reviews.Any(e => e.ReviewId == id);
        }
    }
}