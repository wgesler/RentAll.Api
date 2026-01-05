using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RentAll.Domain.Configuration;
using RentAll.Domain.Interfaces.Auth;
using RentAll.Domain.Interfaces.Repositories;
using RentAll.Domain.Interfaces.Services;
using RentAll.Domain.Managers;
using RentAll.Infrastructure.Configuration;
using RentAll.Infrastructure.HealthChecks;
using RentAll.Infrastructure.Repositories.Companies;
using RentAll.Infrastructure.Repositories.Contacts;
using RentAll.Infrastructure.Repositories.Properties;
using RentAll.Infrastructure.Repositories.Reservations;
using RentAll.Infrastructure.Repositories.Agents;
using RentAll.Infrastructure.Repositories.RefreshTokens;
using RentAll.Infrastructure.Repositories.Common;
using RentAll.Infrastructure.Repositories.Users;
using RentAll.Infrastructure.Repositories.Areas;
using RentAll.Infrastructure.Repositories.Buildings;
using RentAll.Infrastructure.Repositories.Regions;
using RentAll.Infrastructure.Repositories.Offices;
using RentAll.Infrastructure.Repositories.OfficeConfigurations;
using RentAll.Infrastructure.Repositories.Organizations;
using RentAll.Infrastructure.Repositories.Documents;
using RentAll.Infrastructure.Repositories.CodeSequences;
using RentAll.Infrastructure.Repositories.Colors;
using RentAll.Infrastructure.Repositories.PropertySelections;
using RentAll.Infrastructure.Repositories.PropertyLetters;
using RentAll.Infrastructure.Repositories.PropertyWelcomes;
using RentAll.Infrastructure.Repositories.LeaseInformations;
using RentAll.Infrastructure.Repositories.Vendors;
using RentAll.Infrastructure.Services;
using RentAll.Domain.Interfaces.Managers;

var builder = WebApplication.CreateBuilder(args);

//get some app settings
var appSettings = builder.Configuration.GetSection("AppSettings");
builder.Services.Configure<AppSettings>(appSettings);
builder.Services.AddScoped<AppSettings>();

var allowedHosts = appSettings.GetSection("AllowedHostNames").Get<string[]>()!;
var environment = appSettings.GetSection("Environment").Get<string>()!;
var isDev = environment.ToLower() == "development";

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

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
builder.Services.AddScoped<IFileService>(sp =>
{
	var environment = sp.GetRequiredService<IWebHostEnvironment>();
	var logger = sp.GetRequiredService<ILogger<FileService>>();
	var wwwRootPath = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
	return new FileService(wwwRootPath, logger);
});
builder.Services.AddScoped<IPdfGenerationService, PdfGenerationService>();

builder.Services.AddScoped<IContactManager, ContactManager>();
builder.Services.AddScoped<IOrganizationManager, OrganizationManager>();

builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();
builder.Services.AddScoped<IPropertySelectionRepository, PropertySelectionRepository>();
builder.Services.AddScoped<IPropertyLetterRepository, PropertyLetterRepository>();
builder.Services.AddScoped<IPropertyWelcomeRepository, PropertyWelcomeRepository>();
builder.Services.AddScoped<ILeaseInformationRepository, LeaseInformationRepository>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<IVendorRepository, VendorRepository>();
builder.Services.AddScoped<IContactRepository, ContactRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IAgentRepository, AgentRepository>();
builder.Services.AddScoped<ICommonRepository, CommonRepository>();

builder.Services.AddScoped<IOfficeRepository, OfficeRepository>();
builder.Services.AddScoped<IOfficeConfigurationRepository, OfficeConfigurationRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IAreaRepository, AreaRepository>();
builder.Services.AddScoped<IBuildingRepository, BuildingRepository>();
builder.Services.AddScoped<IRegionRepository, RegionRepository>();
builder.Services.AddScoped<ICodeSequenceRepository, CodeSequenceRepository>();
builder.Services.AddScoped<IColorRepository, ColorRepository>();

// Configure Swagger/OpenAPI with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "RentAll API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
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
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

// Enable static files (for images, logos, etc.)
app.UseStaticFiles();

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
