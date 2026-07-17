using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using FluentValidation;
using FluentValidation.AspNetCore;
using EstateSync.Api.Data;
using EstateSync.Api.Repositories.Interfaces;
using EstateSync.Api.Repositories;
using EstateSync.Api.Middleware;
using EstateSync.Api.Services.Properties;
using EstateSync.Api.Services.Auth;
using EstateSync.Api.Services.Tokens;
using EstateSync.Api.Services.Storage;
using EstateSync.Api.Services.Documents;
using EstateSync.Api.Interfaces.Services.Documents;
using EstateSync.Api.Interfaces.Services.Properties;
using EstateSync.Api.Interfaces.Services.Auth;
using EstateSync.Api.Interfaces.Services.Tokens;
using EstateSync.Api.Interfaces.Services.Storage;
using EstateSync.Api.Interfaces.Services.Shared;
using EstateSync.Api.Services.Shared;
using EstateSync.Api.Interfaces.Services.Kyc;
using EstateSync.Api.Services.Kyc;
using EstateSync.Api.Interfaces.Services.RentalApplications;
using EstateSync.Api.Services.RentalApplications;
using EstateSync.Api.Interfaces.Services.Leases;
using EstateSync.Api.Services.Leases;
using QuestPDF.Infrastructure;
using EstateSync.Api.Interfaces.Services.AuditLogs;
using EstateSync.Api.Interfaces.Services.Ai;
using EstateSync.Api.Services.Ai;
using EstateSync.Api.Services.AuditLogs;
using EstateSync.Api.Interfaces.Services.Payments;
using EstateSync.Api.Services.Payments;
using EstateSync.Api.Interfaces.Services.Complaints;
using EstateSync.Api.Services.Complaints;
using EstateSync.Api.Interfaces.Services.SiteVisits;
using EstateSync.Api.Services.SiteVisits;
using EstateSync.Api.Interfaces.Services.Users;
using EstateSync.Api.Services.Users;
using EstateSync.Api.Interfaces.Services.Admin;
using EstateSync.Api.Services.Admin;
using EstateSync.Api.Services.Background;
using Hangfire;
using Hangfire.PostgreSql;
using EstateSync.Api.Services.Notifications;
using EstateSync.Api.Interfaces.Services.Notifications;
using EstateSync.Api.Hubs;
using EstateSync.Api.Extensions;
using EstateSync.Api.Validators;
using Serilog;
using System.Diagnostics;
using Azure.Identity;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Microsoft.Extensions.Azure;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Load secrets from Key Vault before any service registration reads config
if (builder.Environment.IsProduction())
{
    var kvUri = builder.Configuration["Azure:KeyVaultUri"]
        ?? throw new InvalidOperationException("Azure:KeyVaultUri is not configured.");
    builder.Configuration.AddAzureKeyVault(new Uri(kvUri), new DefaultAzureCredential());
}

builder.Host.UseSerilog((context, loggerConfiguration) =>
{
    loggerConfiguration.ReadFrom.Configuration(context.Configuration);
});

// Add services to the container.
var signalRBuilder = builder.Services.AddSignalR();
if (builder.Environment.IsProduction())
    signalRBuilder.AddAzureSignalR(builder.Configuration["Azure:SignalRConnectionString"]
        ?? throw new InvalidOperationException("Azure:SignalRConnectionString is not configured."));
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

// CORS — frontend origin comes from config so deployment only changes appsettings
var frontendUrl = builder.Configuration["App:FrontendUrl"] ?? "http://localhost:4200";
builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular", policy =>
    {
        policy.WithOrigins(frontendUrl)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Register DbContext with PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

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
builder.Services.AddScoped<ILeaseVacateNoticeRepository, LeaseVacateNoticeRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<ISiteVisitRepository, SiteVisitRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IKycVerificationRepository, KycVerificationRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IStripeConnectedAccountRepository, StripeConnectedAccountRepository>();
builder.Services.AddScoped<IStripeCustomerProfileRepository, StripeCustomerProfileRepository>();
builder.Services.AddScoped<IOwnerSettlementRepository, OwnerSettlementRepository>();
builder.Services.AddScoped<ISavedPropertyRepository, SavedPropertyRepository>();
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
builder.Services.AddSingleton<EstateSync.Api.Mappers.AuditLogMapper>();
builder.Services.AddSingleton<EstateSync.Api.Mappers.ComplaintMapper>();
builder.Services.AddSingleton<EstateSync.Api.Mappers.SiteVisitMapper>();

// Register Services
builder.Services.AddScoped<IPropertyService, PropertyService>();
builder.Services.AddScoped<IPropertyImageService, PropertyImageService>();
builder.Services.AddScoped<IPropertyAmenityService, PropertyAmenityService>();
builder.Services.AddScoped<IPropertyApprovalService, PropertyApprovalService>();
builder.Services.AddScoped<IPropertyDocumentService, PropertyDocumentService>();
builder.Services.AddScoped<ISavedPropertyService, SavedPropertyService>();
if (builder.Environment.IsProduction())
{
    builder.Services.AddAzureClients(clients =>
        clients.AddBlobServiceClient(builder.Configuration["Azure:BlobConnectionString"]
            ?? throw new InvalidOperationException("Azure:BlobConnectionString is not configured.")));
    builder.Services.AddScoped<IFileStorageService, AzureBlobFileStorageService>();
}
else
{
    builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
}
builder.Services.AddScoped<IKycService, KycService>();
builder.Services.AddScoped<IDocumentUploadService, DocumentUploadService>();
builder.Services.AddScoped<IRentalApplicationService, RentalApplicationService>();
builder.Services.AddScoped<ILeaseService, LeaseService>();
builder.Services.AddScoped<ISiteVisitService, SiteVisitService>();
builder.Services.AddScoped<ILeaseDocumentService, LeaseDocumentService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IStripeConnectedAccountService, StripeConnectedAccountService>();
builder.Services.AddScoped<ITenantPaymentService, TenantPaymentService>();
builder.Services.AddScoped<IStripeWebhookService, StripeWebhookService>();
builder.Services.AddScoped<IComplaintService, ComplaintService>();
builder.Services.AddScoped<IUrlResolverService, UrlResolverService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IAdminDashboardService, AdminDashboardService>();
builder.Services.AddScoped<IAdminFinancialService, AdminFinancialService>();
builder.Services.AddScoped<IStatisticsRepository, StatisticsRepository>();
builder.Services.AddScoped<INotificationSender, NotificationSender>();

// Register AI Service
builder.Services.AddHttpClient();
builder.Services.AddScoped<IAiService, AiService>();

// Register Auth Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<IPasswordService, PasswordService>();
builder.Services.AddSingleton<ITokenService, TokenService>();

// ── Rate limiting (email-sending endpoints) ──────────────────
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth-email", httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(15)
            }));

    options.AddPolicy("ai-chat", httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anonymous",
            factory: _ => new System.Threading.RateLimiting.SlidingWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                SegmentsPerWindow = 4
            }));
});

// ── Redis (password reset tokens) ─────────────────────────────
builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ =>
    StackExchange.Redis.ConnectionMultiplexer.Connect(
        builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379,abortConnect=false"));
builder.Services.AddSingleton<IPasswordResetTokenStore, RedisPasswordResetTokenStore>();
builder.Services.AddSingleton<IEmailVerificationTokenStore, RedisEmailVerificationTokenStore>();

// Configure JWT Authentication via Extension Method
builder.Services.AddCustomAuthentication(builder.Configuration);

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(o => o.UseNpgsqlConnection(
        builder.Configuration.GetConnectionString("DefaultConnection")!))
    .WithJobExpirationTimeout(TimeSpan.FromDays(3)));
builder.Services.AddHangfireServer();

builder.Services.AddScoped<LeaseExpiryJob>();
builder.Services.AddScoped<LeaseBillJob>();
builder.Services.AddScoped<PenaltyJob>();
builder.Services.AddScoped<PropertyStatusUpdateJob>();
builder.Services.AddScoped<RefreshTokenCleanupJob>();
builder.Services.AddScoped<VacateNoticeJob>();
builder.Services.AddScoped<EstateSync.Api.Services.Email.EmailSenderJob>();

// ── Email (SMTP via Hangfire — durable, survives pod restarts) ──
builder.Services.Configure<EstateSync.Api.Common.EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<EstateSync.Api.Interfaces.Services.Email.IEmailQueue, EstateSync.Api.Services.Email.EmailQueue>();

var app = builder.Build();

// Apply any pending EF Core migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// Seed test data in Development only — admin + roles are seeded via EF migrations (HasData)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await DbSeeder.SeedAsync(scope.ServiceProvider);
}

if (app.Environment.IsDevelopment())
    app.UseHangfireDashboard("/hangfire");

// Register recurring Hangfire jobs using DI-based manager (avoids static JobStorage.Current)
var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
recurringJobs.AddOrUpdate<LeaseExpiryJob>("lease-expiry", j => j.RunAsync(), Cron.Daily(1));
recurringJobs.AddOrUpdate<LeaseBillJob>("lease-bill", j => j.RunAsync(), Cron.Daily);
recurringJobs.AddOrUpdate<PenaltyJob>("penalty", j => j.RunAsync(), Cron.Daily(2));
recurringJobs.AddOrUpdate<PropertyStatusUpdateJob>("prop-status", j => j.RunAsync(), Cron.Daily);
recurringJobs.AddOrUpdate<RefreshTokenCleanupJob>("token-cleanup", j => j.RunAsync(), "30 0 * * *");
recurringJobs.AddOrUpdate<VacateNoticeJob>("vacate-notice", j => j.RunAsync(), Cron.Daily);

// Configure Stripe Global API Key
Stripe.StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];


// Configure the HTTP request pipeline.
app.UseSerilogRequestLogging(options =>
{
    // Drop CORS preflight noise; keep errors at Error level
    options.GetLevel = (httpContext, _, ex) =>
        ex != null || httpContext.Response.StatusCode >= 500 ? Serilog.Events.LogEventLevel.Error
        : HttpMethods.IsOptions(httpContext.Request.Method) ? Serilog.Events.LogEventLevel.Verbose
        : Serilog.Events.LogEventLevel.Information;
});

app.UseExceptionHandler();



if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Local dev only — in production files are served from Azure Blob Storage
if (app.Environment.IsDevelopment())
{
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
            Path.Combine(app.Environment.ContentRootPath, "uploads")),
        RequestPath = "/uploads"
    });
}

app.UseCors("Angular");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHealthChecks("/health");

app.Run();
