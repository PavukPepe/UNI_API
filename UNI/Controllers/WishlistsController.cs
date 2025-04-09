using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UNI.Models;

namespace UNI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WishlistsController : ControllerBase
    {
        private readonly UniContext _context;

        public WishlistsController(UniContext context)
        {
            _context = context;
        }

        // GET: api/wishlists?userId={userId}
        [HttpGet]
        public async Task<ActionResult<IEnumerable<int>>> GetWishlists([FromQuery] int userId)
        {
            if (userId <= 0) return BadRequest("Неверный ID пользователя");

            var wishlistCourseIds = await _context.Wishlists
                .Where(w => w.UserId == userId)
                .Select(w => w.CourseId)
                .ToListAsync();

            return Ok(wishlistCourseIds);
        }

        // POST: api/wishlists
        [HttpPost]
        public async Task<ActionResult> AddToWishlist([FromBody] WishlistDto wishlistDto)
        {
            if (wishlistDto.UserId <= 0) return BadRequest("Неверный ID пользователя");

            // Проверяем, не добавлен ли курс уже в избранное
            var existingWishlist = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == wishlistDto.UserId && w.CourseId == wishlistDto.CourseId);

            if (existingWishlist != null)
            {
                return BadRequest("Курс уже добавлен в избранное");
            }

            var wishlist = new Wishlist
            {
                UserId = wishlistDto.UserId,
                CourseId = wishlistDto.CourseId,
                AddedDate = DateTime.Now
            };

            _context.Wishlists.Add(wishlist);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // DELETE: api/wishlists/{courseId}?userId={userId}
        [HttpDelete("{courseId}")]
        public async Task<IActionResult> DeleteWishlist(int courseId, [FromQuery] int userId)
        {
            if (userId <= 0) return BadRequest("Неверный ID пользователя");

            var wishlist = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.CourseId == courseId);

            if (wishlist == null)
            {
                return NotFound("Курс не найден в избранном");
            }

            _context.Wishlists.Remove(wishlist);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DTO для добавления в избранное
        public class WishlistDto
        {
            public int UserId { get; set; }
            public int CourseId { get; set; }
        }
    }
}