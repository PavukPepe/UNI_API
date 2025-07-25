﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.EntityFrameworkCore;
using UNI.Models;
using UNI.Models.DTO;
using BCrypt.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace UNI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UniContext _context;

        public UsersController(UniContext context)
        {
            _context = context;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        public class UserDto
        {
            public int UserId { get; set; }
            public string Email { get; set; }
            public string FullName { get; set; }
            public string? ProfilePicture { get; set; }
            public DateTime? RegistrationDate { get; set; }
            public bool? IsBlocked { get; set; }
            public List<CertificateDto> Certificates { get; set; } = new List<CertificateDto>();
        }

        public class CertificateDto
        {
            public int CertificateId { get; set; }
            public int? CourseId { get; set; }
            public string CourseTitle { get; set; }
            public DateTime? IssueDate { get; set; }
            public string CertificateCode { get; set; }
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            var user =  _context.Users
                .Include(c => c.Certificates)
                    .ThenInclude(c => c.Course) // Загружаем данные курса
                .FirstOrDefault(u => u.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            var userDto = new UserDto
            {
                UserId = user.UserId,
                Email = user.Email,
                FullName = user.FullName,
                ProfilePicture = user.ProfilePicture,
                RegistrationDate = user.RegistrationDate,
                IsBlocked = user.IsBlocked,
                Certificates = user.Certificates.Select(c => new CertificateDto
                {
                    CertificateId = c.CertificateId,
                    CourseId = c.CourseId,
                    CourseTitle = c.Course?.CourseTitle ?? "Название курса",
                    IssueDate = c.IssueDate,
                    CertificateCode = c.CertificateCode
                }).ToList()
            };

            return Ok(userDto);
        }

        // PUT: api/Users/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserDto userDto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Обновляем поля пользователя
            user.FullName = userDto.FullName;
            user.Email = userDto.Email;
            user.ProfilePicture = userDto.ProfilePicture; // Сохраняем URL фото

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Users.Any(e => e.UserId == id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        // DTO для обновления пользователя

        [HttpPost("register")]
        public async Task<ActionResult<User>> Register([FromBody] RegisterData request)
        {
            if (_context.Users.Any(u => u.Email == request.Email))
            {
                return BadRequest(new { message = "Пользователь с таким email уже существует" });
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Email = request.Email,
                PasswordHash = passwordHash,
                FullName = request.FullName,
                RegistrationDate = DateTime.Now,
                IsBlocked = false
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "student");
            if (studentRole == null)
            {
                return StatusCode(500, new { message = "Роль 'student' не найдена в базе данных" });
            }

            user.Roles.Add(studentRole);

            if (request.IsAuthor)
            {
                var authorRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "author");
                if (authorRole == null)
                {
                    return StatusCode(500, new { message = "Роль 'author' не найдена в базе данных" });
                }
                user.Roles.Add(authorRole);
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUser", new { id = user.UserId }, new { message = "Регистрация успешна", userId = user.UserId });
        }

        [HttpPost("login")]
        public async Task<ActionResult<User>> Login([FromBody] LoginData request)
        {
            var user = await _context.Users.Include("Roles").FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Неверный email или пароль" });
            }


            var roles = user.Roles.Select(ur => ur.RoleName).ToList();

            // Генерация JWT-токена
            var token = GenerateJwtToken(user, roles);

            return Ok(new
            {
                userId = user.UserId,
                status = user.IsBlocked,
                email = user.Email,
                token = token,
                roles = roles
            });
        }

        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUser", new { id = user.UserId }, user);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }


        private string GenerateJwtToken(User user, List<string> roles)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email)
            };

            // Добавляем роли в claims
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("your-very-secure-secret-key-here-32-chars")); // Замени на свой секретный ключ (длина >= 16)
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: "YourApp",
                audience: "YourApp",
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
