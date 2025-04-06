using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UNI.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "UNI",
            ValidAudience = "UNI",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("K7p!m9Qw#xL2vT5r&nB8jY4uZ6tA3eH"))
        };
    });

// Подключение DbContext
builder.Services.AddDbContext<UniContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Контроллеры
builder.Services.AddControllers();

// Swagger (OpenAPI)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Online Courses API",
        Version = "v1",
        Description = "API для управления онлайн-курсами"
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", builder =>
    {
        builder.WithOrigins("http://localhost:3000")
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});


var app = builder.Build();
app.UseCors("AllowReactApp");
// Middleware
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Online Courses API V1");
    c.RoutePrefix = string.Empty; // Swagger по корневому URL
});



app.UseRouting();
app.UseAuthorization();
app.MapControllers();

app.Run();