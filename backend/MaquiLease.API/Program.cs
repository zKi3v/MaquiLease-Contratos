using MaquiLease.API.Data;
using MaquiLease.API.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

// Set QuestPDF license
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<PdfService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy =>
        {
            policy.WithOrigins(
                      "http://localhost:4200",
                      "https://maquilease.zki3v.com",
                      "http://maquilease.zki3v.com",
                      "https://maquilease.vercel.app"
                  )
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var projectId = builder.Configuration["Firebase:ProjectId"] ?? "maquilease";

builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://securetoken.google.com/{projectId}";
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"https://securetoken.google.com/{projectId}",
            ValidateAudience = true,
            ValidAudience = projectId,
            ValidateLifetime = true
        };
    });

builder.Services.AddAuthorization();

// ═══ Database: InMemory vs SQL Server ═══
// Permitimos forzar InMemory via variable de entorno para el deploy de prueba en Render
bool useInMemory = builder.Configuration.GetValue<bool>("USE_IN_MEMORY") || builder.Environment.IsDevelopment();

if (useInMemory)
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("MaquiLease_Dev"));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}

var app = builder.Build();

// ═══ Seed data ═══
if (useInMemory)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
    SeedData.Initialize(context);
}

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "MaquiLease API V1");
    c.RoutePrefix = "swagger"; // Swagger will be at /swagger
});

app.UseCors("AllowAngularApp");

// app.UseHttpsRedirection(); // Disable for docker http
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

