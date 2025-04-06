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
    public class UserprogressesController : ControllerBase
    {
        private readonly UniContext _context;

        public UserprogressesController(UniContext context)
        {
            _context = context;
        }

        // GET: api/Userprogresses
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Userprogress>>> GetUserprogresses()
        {
            return await _context.Userprogresses.ToListAsync();
        }

        // GET: api/Userprogresses/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Userprogress>> GetUserprogress(int id)
        {
            var userprogress = await _context.Userprogresses.FindAsync(id);

            if (userprogress == null)
            {
                return NotFound();
            }

            return userprogress;
        }

        // PUT: api/Userprogresses/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUserprogress(int id, Userprogress userprogress)
        {
            if (id != userprogress.ProgressId)
            {
                return BadRequest();
            }

            _context.Entry(userprogress).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserprogressExists(id))
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

        // POST: api/Userprogresses
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Userprogress>> PostUserprogress(Userprogress userprogress)
        {
            _context.Userprogresses.Add(userprogress);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUserprogress", new { id = userprogress.ProgressId }, userprogress);
        }

        // DELETE: api/Userprogresses/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserprogress(int id)
        {
            var userprogress = await _context.Userprogresses.FindAsync(id);
            if (userprogress == null)
            {
                return NotFound();
            }

            _context.Userprogresses.Remove(userprogress);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserprogressExists(int id)
        {
            return _context.Userprogresses.Any(e => e.ProgressId == id);
        }
    }
}
