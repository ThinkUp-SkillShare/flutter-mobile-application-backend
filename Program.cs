using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using SkillShareBackend.Data;
using SkillShareBackend.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
                       ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Environment.WebRootPath = Path.Combine(builder.Environment.ContentRootPath, "wwwroot");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Servicios registrados
builder.Services.AddScoped<ICallService, CallService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IGroupManagementService, GroupManagementService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddSingleton<WebSocketHandler>();
builder.Services.AddSingleton<ChatWebSocketHandler>();
builder.Services.AddScoped<IFirebaseStorageService, FirebaseStorageService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            NameClaimType = ClaimTypes.NameIdentifier
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"🔴 JWT Authentication Failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine($"✅ JWT Token Validated for: {context.Principal.Identity.Name}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddHttpClient();
builder.Services.AddAuthorization();

var app = builder.Build();

// Crear directorios necesarios para almacenamiento de archivos
var wwwrootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
if (!Directory.Exists(wwwrootPath))
{
    Directory.CreateDirectory(wwwrootPath);
    Console.WriteLine($"✅ Created wwwroot directory: {wwwrootPath}");
}

// Directorios para diferentes tipos de archivos
var directoriesToCreate = new[]
{
    Path.Combine(wwwrootPath, "uploads", "images"),
    Path.Combine(wwwrootPath, "uploads", "audio"),
    Path.Combine(wwwrootPath, "uploads", "files"),
    Path.Combine(wwwrootPath, "uploads", "documents")
};

foreach (var dir in directoriesToCreate)
    if (!Directory.Exists(dir))
    {
        Directory.CreateDirectory(dir);
        Console.WriteLine($"✅ Created directory: {dir}");
    }

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");
//app.UseHttpsRedirection();
app.UseStaticFiles(); // Importante para servir archivos estáticos

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseWebSockets();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(builder.Environment.WebRootPath, "uploads")),
    RequestPath = "/uploads",
    ServeUnknownFileTypes = true
});

// Endpoint de prueba para verificar directorios
app.MapGet("/api/test-static-files", () =>
{
    var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
    var imageDir = Path.Combine(uploadsDir, "images");
    var audioDir = Path.Combine(uploadsDir, "audio");
    var filesDir = Path.Combine(uploadsDir, "files");

    return new
    {
        uploadsDirectoryExists = Directory.Exists(uploadsDir),
        imageDirectoryExists = Directory.Exists(imageDir),
        audioDirectoryExists = Directory.Exists(audioDir),
        filesDirectoryExists = Directory.Exists(filesDir),
        currentDirectory = Directory.GetCurrentDirectory(),
        wwwrootPath = app.Environment.WebRootPath,
        totalImagesFiles = Directory.Exists(imageDir) ? Directory.GetFiles(imageDir).Length : 0,
        totalAudioFiles = Directory.Exists(audioDir) ? Directory.GetFiles(audioDir).Length : 0,
        totalOtherFiles = Directory.Exists(filesDir) ? Directory.GetFiles(filesDir).Length : 0
    };
});

// WebSocket para llamadas
app.Map("/ws/call/{callId}", async (HttpContext context, string callId) =>
{
    var handler = context.RequestServices.GetRequiredService<WebSocketHandler>();
    await handler.HandleCallWebSocket(context, callId);
});


app.Map("/ws/chat/{groupId}", async (HttpContext context, int groupId) =>
{
    var handler = context.RequestServices.GetRequiredService<ChatWebSocketHandler>();
    await handler.HandleChatWebSocket(context, groupId);
});

app.MapGet("/api/debug/uploads", () =>
{
    var wwwrootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
    var uploadsPath = Path.Combine(wwwrootPath, "uploads");
    var imagesPath = Path.Combine(uploadsPath, "images");
    var audioPath = Path.Combine(uploadsPath, "audio");
    var filesPath = Path.Combine(uploadsPath, "files");

    return new
    {
        wwwrootExists = Directory.Exists(wwwrootPath),
        uploadsExists = Directory.Exists(uploadsPath),
        imagesExists = Directory.Exists(imagesPath),
        audioExists = Directory.Exists(audioPath),
        filesExists = Directory.Exists(filesPath),
        currentDirectory = Directory.GetCurrentDirectory(),
        wwwrootPath,
        uploadsPath
    };
});

app.MapGet("/", () => "🚀 SkillShare Flutter Backend is Running!");

app.Run();
