using System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using FluentValidation;
using FluentValidation.AspNetCore;
using EstateSync.Api.Data;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Repositories;
using EstateSync.Api.Models;
using EstateSync.Api.Middleware;
using EstateSync.Api.Services.Properties;
using EstateSync.Api.Services.Auth;
using EstateSync.Api.Services.Tokens;
using EstateSync.Api.Services.Storage;
using EstateSync.Api.Services.OwnerVerification;
using EstateSync.Api.Interfaces.Services.Properties;
using EstateSync.Api.Interfaces.Services.Auth;
using EstateSync.Api.Interfaces.Services.Tokens;
using EstateSync.Api.Interfaces.Services.Storage;
using EstateSync.Api.Interfaces.Services.OwnerVerification;
using EstateSync.Api.Interfaces.Services.IdentityVerification;
using EstateSync.Api.Services.IdentityVerification;
using EstateSync.Api.Interfaces.Services.RentalApplications;
using EstateSync.Api.Services.RentalApplications;
using EstateSync.Api.Validators;
using Serilog;
using System.Diagnostics;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration.ReadFrom.Configuration(context.Configuration);
});

// Add services to the container.
builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();


builder.Services.AddEndpointsApiExplorer();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        var traceId = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
        context.ProblemDetails.Extensions["traceId"] = traceId;

        context.ProblemDetails.Extensions["timestamp"] = DateTime.UtcNow;

        context.ProblemDetails.Instance = context.HttpContext.Request.Path;
    };
});

// Configure Swagger with JWT support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "EstateSync API", Version = "v1" });
    
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter JWT Bearer token only",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    };
    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", doc),
            new List<string>()
        }
    });
});

// Register DbContext with PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register Repositories
#region Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();
builder.Services.AddScoped<ILeaseRepository, LeaseRepository>();
builder.Services.AddScoped<ILeaseBillRepository, LeaseBillRepository>();
builder.Services.AddScoped<IRentalApplicationRepository, RentalApplicationRepository>();
builder.Services.AddScoped<IComplaintRepository, ComplaintRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IOwnerVerificationRequestRepository, OwnerVerificationRequestRepository>();
builder.Services.AddScoped<IIdentityVerificationRequestRepository, IdentityVerificationRequestRepository>();
builder.Services.AddScoped<ITenantVerificationRequestRepository, TenantVerificationRequestRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IStripeConnectedAccountRepository, StripeConnectedAccountRepository>();
builder.Services.AddScoped<IStripeCustomerProfileRepository, StripeCustomerProfileRepository>();
builder.Services.AddScoped<IOwnerSettlementRepository, OwnerSettlementRepository>();
#endregion

// Register FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Register Mappers
builder.Services.AddSingleton<EstateSync.Api.Mappers.AuthMapper>();
builder.Services.AddSingleton<EstateSync.Api.Mappers.PropertyMapper>();
builder.Services.AddSingleton<EstateSync.Api.Mappers.UserMapper>();
builder.Services.AddSingleton<EstateSync.Api.Mappers.RentalApplicationMapper>();
builder.Services.AddSingleton<EstateSync.Api.Mappers.LeaseMapper>();
builder.Services.AddSingleton<EstateSync.Api.Mappers.TenantVerificationMapper>();
builder.Services.AddSingleton<EstateSync.Api.Mappers.AuditLogMapper>();
builder.Services.AddSingleton<EstateSync.Api.Mappers.ComplaintMapper>();

// Register Services
builder.Services.AddScoped<IPropertyService, PropertyService>();
builder.Services.AddScoped<IPropertyImageService, PropertyImageService>();
builder.Services.AddScoped<IPropertyAmenityService, PropertyAmenityService>();
builder.Services.AddScoped<IPropertyApprovalService, PropertyApprovalService>();
builder.Services.AddScoped<IPropertyDocumentService, PropertyDocumentService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IOwnerVerificationService, OwnerVerificationService>();
builder.Services.AddScoped<IIdentityVerificationService, IdentityVerificationService>();
builder.Services.AddScoped<IRentalApplicationService, RentalApplicationService>();
builder.Services.AddScoped<EstateSync.Api.Interfaces.Services.TenantVerification.ITenantVerificationService, EstateSync.Api.Services.TenantVerification.TenantVerificationService>();
builder.Services.AddScoped<EstateSync.Api.Interfaces.Services.Leases.ILeaseService, EstateSync.Api.Services.Leases.LeaseService>();
builder.Services.AddScoped<EstateSync.Api.Interfaces.Services.AuditLogs.IAuditLogService, EstateSync.Api.Services.AuditLogs.AuditLogService>();
builder.Services.AddScoped<EstateSync.Api.Interfaces.Services.Payments.IPaymentService, EstateSync.Api.Services.Payments.PaymentService>();
builder.Services.AddScoped<EstateSync.Api.Interfaces.Services.Complaints.IComplaintService, EstateSync.Api.Services.Complaints.ComplaintService>();

// Register Auth Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddSingleton<ITokenService, TokenService>();
builder.Services.AddScoped<EstateSync.Api.Interfaces.Services.Auth.ICurrentUserService, EstateSync.Api.Services.Auth.CurrentUserService>();

// Configure HS256 JWT Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "SuperSecretKeyForEstateSyncApp12345678901234567890!";
var validationKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSecret));

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
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "EstateSyncApi",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "EstateSyncClient",
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = validationKey,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // options.Events = new JwtBearerEvents
    // {
    //     OnMessageReceived = context =>
    //     {
    //         var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
    //         var authorizationHeader = context.Request.Headers.Authorization.ToString();
    //         logger.LogInformation("Incoming Authorization Header: {Header}", authorizationHeader);
    //         return Task.CompletedTask;
    //     },
    //     OnAuthenticationFailed = context =>
    //     {
    //         var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
    //         logger.LogError(context.Exception, "JWT Authentication failed");
    //         return Task.CompletedTask;
    //     },
    //     OnTokenValidated = context =>
    //     {
    //         var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
    //         logger.LogInformation("JWT Token validated successfully for user");
    //         return Task.CompletedTask;
    //     }
    // };
});

builder.Services.AddAuthorization();

builder.Services.AddHostedService<EstateSync.Api.Services.Background.LeaseBillScheduler>();

var app = builder.Build();

// Configure Stripe Global API Key
Stripe.StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// Database migrations or seeding can go here if needed.

// Configure the HTTP request pipeline.
app.UseSerilogRequestLogging();

app.UseExceptionHandler();



if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
