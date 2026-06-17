using MaquiLease.API.Data;
using MaquiLease.API.Intelligence;
using MaquiLease.API.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using System.IO;
using Microsoft.OpenApi.Models;
// Set QuestPDF license
QuestPDF.Settings.License = LicenseType.Community;

// Cargar archivo .env local si existe (para desarrollo)
var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envPath))
{
    foreach (var line in File.ReadAllLines(envPath))
    {
        if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;
        var parts = line.Split('=', 2);
        if (parts.Length == 2)
        {
            var key = parts[0].Trim();
            var val = parts[1].Trim().Trim('"').Trim('\'');
            Environment.SetEnvironmentVariable(key, val);
        }
    }
}

var builder = WebApplication.CreateBuilder(args);

// Asegurar que se vuelvan a leer las variables de entorno inyectadas
builder.Configuration.AddEnvironmentVariables();

// Initialize Firebase Admin SDK
var firebaseConfigFile = Path.Combine(builder.Environment.ContentRootPath, "maquilease-firebase-adminsdk-fbsvc-c134770f08.json");
if (File.Exists(firebaseConfigFile))
{
    FirebaseApp.Create(new AppOptions
    {
        Credential = GoogleCredential.FromFile(firebaseConfigFile)
    });
}

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "MaquiLease API", Version = "v1" });

    // Configuración de Autenticación JWT para Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pega aquí tu token JWT de Firebase (sin la palabra 'Bearer')."
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });

    // Habilitar comentarios XML en español
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});
builder.Services.AddScoped<PdfService>();
builder.Services.AddHttpClient<OpenCodeService>();
builder.Services.AddScoped<IIntelligenceService, IntelligenceService>();

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

// Register Background Jobs
builder.Services.AddHostedService<MaquiLease.API.BackgroundJobs.DueDateMonitorJob>();
builder.Services.AddHostedService<MaquiLease.API.BackgroundJobs.RiskScoreRecalcJob>();

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

