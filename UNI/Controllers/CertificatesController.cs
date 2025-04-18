using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.Elfie.Diagnostics;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UNI.Models;

namespace UNI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CertificatesController : ControllerBase
    {
        private readonly UniContext _context;

        public CertificatesController(UniContext context)
        {
            _context = context;
        }

        // GET: api/Certificates
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Certificate>>> GetCertificates([FromQuery] int? userId, [FromQuery] int? courseId)
        {
            var query = _context.Certificates
                .Include(c => c.User)
                .Include(c => c.Course)
                .AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(c => c.UserId == userId.Value);
            }

            if (courseId.HasValue)
            {
                query = query.Where(c => c.CourseId == courseId.Value);
            }

            var certificates = await query.ToListAsync();
            return Ok(certificates);
        }

        [HttpGet("{certificateId}/pdf")]
        public async Task<IActionResult> GenerateCertificatePdf(int certificateId)
        {
            // Получаем userId из токена
            //var userId = int.Parse(User.FindFirst("nameid")?.Value ?? "0");
            //if (userId == 0)
            //{
            //    return Unauthorized(new { message = "Пользователь не авторизован" });
            //}

            // Находим сертификат
            var certificate = await _context.Certificates
                .Include(c => c.User)
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c => c.CertificateId == certificateId);

            if (certificate == null)
            {
                return NotFound(new { message = "Сертификат не найден" });
            }

            // Проверяем, принадлежит ли сертификат пользователю
            //if (certificate.UserId != userId)
            //{
            //    return Forbid("У вас нет доступа к этому сертификату");
            //}

            // Генерация PDF
            QuestPDF.Settings.License = LicenseType.Community;
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12).FontFamily("Times New Roman"));
                    // Декоративная рамка
                    page.Content()
                        .Border(2)
                        .BorderColor(Colors.Orange.Medium)
                        .Padding(10)
                        .Background(Colors.White)
                        .Layers(layers =>
                        {
                            layers.PrimaryLayer().Column(column =>
                            {
                                column.Spacing(20);

                                // Заголовок с логотипом или декоративным элементом
                                column.Item()
                                    .AlignCenter()
                                    .PaddingTop(20)
                                    .Text("Сертификат об окончании курса")
                                    .FontSize(30)
                                    .Bold()
                                    .FontColor(Colors.Blue.Darken4);

                                // Подзаголовок
                                column.Item()
                                    .AlignCenter()
                                    .Text("UNI Platform")
                                    .FontSize(16)
                                    .Italic()
                                    .FontColor(Colors.Grey.Darken2);

                                // Основной контент
                                column.Item()
                                    .PaddingTop(30)
                                    .AlignCenter()
                                    .Text($"Выдан")
                                    .FontSize(18)
                                    .FontColor(Colors.Black);

                                column.Item()
                                    .AlignCenter()
                                    .Text(certificate.User.FullName)
                                    .FontSize(24)
                                    .Bold()
                                    .FontColor(Colors.Orange.Darken2);

                                column.Item()
                                    .PaddingTop(20)
                                    .AlignCenter()
                                    .Text($"За успешное завершение курса:")
                                    .FontSize(16)
                                    .FontColor(Colors.Black);

                                column.Item()
                                    .AlignCenter()
                                    .Text(certificate.Course?.CourseTitle ?? "Название курса")
                                    .FontSize(20)
                                    .Bold()
                                    .FontColor(Colors.Blue.Darken3);

                                column.Item()
                                    .PaddingTop(20)
                                    .AlignCenter()
                                    .Text($"Дата выдачи: {certificate.IssueDate?.ToString("dd.MM.yyyy") ?? "Не указана"}")
                                    .FontSize(14)
                                    .FontColor(Colors.Black);

                                column.Item()
                                    .AlignCenter()
                                    .Text($"Код сертификата: {certificate.CertificateCode}")
                                    .FontSize(14)
                                    .FontColor(Colors.Black);
                            });

                            // Декоративные элементы (например, орнамент)
                            layers.Layer();
                        });

                    // Нижний колонтитул
                    page.Footer()
                        .AlignCenter()
                        .Text("© UNI Platform")
                        .FontSize(10)
                        .FontColor(Colors.Grey.Darken2);
                });
            });

            // Создаём поток для PDF
            var pdfStream = new MemoryStream();
            document.GeneratePdf(pdfStream);
            pdfStream.Position = 0;

            return File(pdfStream, "application/pdf", $"certificate_{certificateId}.pdf");
        }

        // GET: api/Certificates/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Certificate>> GetCertificate(int id)
        {
            var certificate = await _context.Certificates
                .Include(c => c.User)
                .Include(c => c.Course)
                .FirstOrDefaultAsync(c => c.CertificateId == id);

            if (certificate == null)
            {
                return NotFound();
            }

            return certificate;
        }

        // PUT: api/Certificates/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCertificate(int id, Certificate certificate)
        {
            if (id != certificate.CertificateId)
            {
                return BadRequest();
            }

            _context.Entry(certificate).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CertificateExists(id))
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

        // POST: api/Certificates
        [HttpPost]
        public async Task<ActionResult<Certificate>> PostCertificate(Certificate certificate)
        {
            certificate.IssueDate = DateTime.Now;
            if (certificate.UserId == null || certificate.CourseId == null)
            {
                return BadRequest(new { message = "UserId и CourseId обязательны" });
            }

            // Проверяем, существует ли уже сертификат для этого пользователя и курса
            var existingCertificate = await _context.Certificates
                .FirstOrDefaultAsync(c => c.UserId == certificate.UserId && c.CourseId == certificate.CourseId);

            if (existingCertificate != null)
            {
                return Conflict(new { message = "Сертификат для этого курса уже выдан пользователю" });
            }

            // Устанавливаем дату выдачи, если не указана
            if (certificate.IssueDate == null)
            {
                certificate.IssueDate = DateTime.UtcNow;
            }

            // Генерируем уникальный код, если не указан
            if (string.IsNullOrEmpty(certificate.CertificateCode))
            {
                certificate.CertificateCode = $"CERT-{certificate.CourseId}-{certificate.UserId}-{DateTime.Now.Ticks}";
            }

            _context.Certificates.Add(certificate);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetCertificate", new { id = certificate.CertificateId }, certificate);
        }

        // DELETE: api/Certificates/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCertificate(int id)
        {
            var certificate = await _context.Certificates.FindAsync(id);
            if (certificate == null)
            {
                return NotFound();
            }

            _context.Certificates.Remove(certificate);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool CertificateExists(int id)
        {
            return _context.Certificates.Any(e => e.CertificateId == id);
        }
    }
}