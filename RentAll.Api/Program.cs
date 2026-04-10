using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Auth;
using RentAll.Domain.Interfaces.Managers;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Managers;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.HealthChecks;
using RentAll.Infrastructure.Repositories.Accounting;
using RentAll.Infrastructure.Repositories.Common;
using RentAll.Infrastructure.Repositories.Contacts;
using RentAll.Infrastructure.Repositories.Documents;
using RentAll.Infrastructure.Repositories.Emails;
using RentAll.Infrastructure.Repositories.Maintenances;
using RentAll.Infrastructure.Repositories.Organizations;
using RentAll.Infrastructure.Repositories.Properties;
using RentAll.Infrastructure.Repositories.Reservations;
using RentAll.Infrastructure.Repositories.Users;
using RentAll.Infrastructure.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Get some app settings and bind to IOptions<AppSettings>
var appSettingsSection = builder.Configuration.GetSection("AppSettings");
builder.Services.Configure<AppSettings>(appSettingsSection);
// Resolve AppSettings from configured options so injected AppSettings has Environment, etc.
builder.Services.AddScoped(sp => sp.GetRequiredService<IOptions<AppSettings>>().Value);

// Configure Storage Settings
var storageSettings = builder.Configuration.GetSection("StorageSettings");
builder.Services.Configure<StorageSettings>(storageSettings);
builder.Services.AddScoped<StorageSettings>();

// Configure SendGrid settings
var sendGridSettings = builder.Configuration.GetSection("SendGridSettings");
builder.Services.Configure<SendGridSettings>(sendGridSettings);

builder.Services.Configure<ImageUploadSettings>(builder.Configuration.GetSection("ImageUpload"));

var allowedHosts = appSettingsSection.GetSection("AllowedHostNames").Get<string[]>()!;
var environment = appSettingsSection.GetSection("Environment").Get<string>()!;
var isDev = environment.ToLower() == "development";

// Add services to the container.
builder.Services.AddControllers();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedHosts)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Configure Database Connection
builder.Services.AddScoped<IDatabaseConnectionFactory, DatabaseConnectionFactory>();

// Configure Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "db", "sql", "ready" });

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

// Register services
builder.Services.AddScoped<AuthManager>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IAuthTokenService, AuthTokenService>();
builder.Services.AddScoped<IDailyQuoteService, DailyQuoteService>();
builder.Services.AddScoped<ICalendarService, CalendarService>();
builder.Services.AddScoped<IEmailService, SendGridEmailService>();

// Configure File Storage Service (FileSystem or AzureBlob)
var storageConfig = builder.Configuration.GetSection("StorageSettings");
var storageProvider = storageConfig["Provider"] ?? "FileSystem";

if (string.Equals(storageProvider, "AzureBlob", StringComparison.OrdinalIgnoreCase))
{
    // Register Azure Blob Service Client
    // Read configuration directly to avoid scoped service resolution in singleton factory
    var connectionString = storageConfig["AzureBlobConnectionString"];
    var baseUrl = storageConfig["AzureBlobBaseUrl"];
    var accountName = storageConfig["AzureBlobAccountName"];

    builder.Services.AddSingleton(sp =>
    {
        // Prefer connection string locally (or whenever provided) - avoids DefaultAzureCredential issues
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return new BlobServiceClient(connectionString);
        }

        // Otherwise fall back to managed identity / DefaultAzureCredential
        // Extract account name from BaseUrl if not explicitly provided
        if (string.IsNullOrWhiteSpace(accountName) && !string.IsNullOrWhiteSpace(baseUrl))
        {
            var uri = new Uri(baseUrl);
            accountName = uri.Host.Split('.')[0];
        }

        if (string.IsNullOrWhiteSpace(accountName))
            throw new InvalidOperationException("AzureBlobAccountName or AzureBlobBaseUrl is required when using managed identity.");

        var blobUri = new Uri($"https://{accountName}.blob.core.windows.net");
        return new BlobServiceClient(blobUri, new DefaultAzureCredential());
    });

    // Register Azure Blob Storage Service
    builder.Services.AddScoped<IFileService, AzureBlobStorageService>();
}
else
{
    // Register File System Storage Service (default)
    builder.Services.AddScoped<IFileService>(sp =>
    {
        var hostEnv = sp.GetRequiredService<IWebHostEnvironment>();
        var appSettings = sp.GetRequiredService<AppSettings>();
        var logger = sp.GetRequiredService<ILogger<FileService>>();
        var wwwRootPath = hostEnv.WebRootPath ?? Path.Combine(hostEnv.ContentRootPath, "wwwroot");
        return new FileService(wwwRootPath, appSettings, sp.GetRequiredService<IOptions<ImageUploadSettings>>(), logger);
    });
}

builder.Services.AddScoped<IFileAttachmentHelper, FileAttachmentHelper>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IPdfGenerationService, PdfGenerationService>();

builder.Services.AddScoped<IContactManager, ContactManager>();
builder.Services.AddScoped<IPropertyManager, PropertyManager>();
builder.Services.AddScoped<IEmailManager, EmailManager>();
builder.Services.AddScoped<IOrganizationManager, OrganizationManager>();
builder.Services.AddScoped<IAccountingManager, AccountingManager>();
builder.Services.AddScoped<IMaintenanceManager, MaintenanceManager>();
builder.Services.AddScoped<ICalendarManager, CalendarManager>();

builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();
builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<ICommonRepository, CommonRepository>();
builder.Services.AddScoped<IEmailRepository, EmailRepository>();

builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IPhotoRepository, PhotoRepository>();
builder.Services.AddScoped<IAccountingRepository, AccountingRepository>();
builder.Services.AddScoped<IMaintenanceRepository, MaintenanceRepository>();

// Configure Swagger/OpenAPI with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "RentAll API", Version = "v1" });
    var bearerSecurityScheme = new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    };
    c.AddSecurityDefinition("Bearer", bearerSecurityScheme);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
//app.UseHttpsRedirection();

// Enable static files (for images, logos, etc.)
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (ctx.File.Name == "index.html")
        {
            ctx.Context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate, max-age=0";
            ctx.Context.Response.Headers["Pragma"] = "no-cache";
            ctx.Context.Response.Headers["Expires"] = "0";
        }
    }
});
// Enable CORS (must be before UseAuthentication and UseAuthorization)
app.UseCors();

// Enable Swagger in all environments
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "RentAll API v1");
    c.DisplayRequestDuration();
});

// Map Health Check endpoints
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
