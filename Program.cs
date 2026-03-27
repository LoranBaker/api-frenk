using Frank.Application.Interfaces.Services;
using Frank.Application.Services;
using Frank.Infrastructure.Data;
using Frank.Infrastructure.Repositories;
using Frank.Infrastructure.Services;
using FrankApi.Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsql => npgsql.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null)));

// ── JWT Authentication via Supabase ───────────────────────
var supabaseUrl = builder.Configuration["Supabase:Url"]
    ?? throw new InvalidOperationException("Supabase:Url not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"{supabaseUrl}/auth/v1";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"{supabaseUrl}/auth/v1",
            ValidateAudience = true,
            ValidAudience = "authenticated",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // Let the middleware fetch the public key from Supabase JWKS endpoint
        // automatically — no need to configure the signing key manually
        options.MetadataAddress = $"{supabaseUrl}/auth/v1/.well-known/openid-configuration";
        options.RequireHttpsMetadata = false; // dev only
    });

builder.Services.AddAuthorization();

// ── HttpClients ───────────────────────────────────────────
builder.Services.AddHttpClient<IAuthService, AuthService>();
builder.Services.AddHttpClient<IClaudeService, ClaudeService>();

// ── Repositories ──────────────────────────────────────────
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<ITapOptionRepository, TapOptionRepository>();
builder.Services.AddScoped<IAppSettingRepository, AppSettingRepository>();
builder.Services.AddScoped<IAnswerRepository, AnswerRepository>();
builder.Services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IMorningRepository, MorningRepository>();
builder.Services.AddScoped<IUrgeRepository, UrgeRepository>();
builder.Services.AddScoped<IEveningRepository, EveningRepository>();
builder.Services.AddScoped<IWeeklyRepository, WeeklyRepository>();
builder.Services.AddScoped<IUserSummaryRepository, UserSummaryRepository>();
builder.Services.AddScoped<IPreUrgeVersionRepository, PreUrgeVersionRepository>();
builder.Services.AddScoped<IMiniMirrorRepository, MiniMirrorRepository>();
builder.Services.AddHttpClient<INotificationService, FcmNotificationService>();
builder.Services.AddScoped<IDeviceTokenRepository, DeviceTokenRepository>();

// ── Application Services ──────────────────────────────────
builder.Services.AddSingleton<IAppSettingsService, AppSettingsService>();
builder.Services.AddScoped<IOnboardingService, OnboardingService>();
builder.Services.AddScoped<IQuestionService, QuestionService>();
builder.Services.AddScoped<ICrisisDetectionService, CrisisDetectionService>();
builder.Services.AddScoped<IInterventionService, InterventionService>();
builder.Services.AddScoped<IMorningService, MorningService>();
builder.Services.AddScoped<IUrgeService, UrgeService>();
builder.Services.AddScoped<IEveningService, EveningService>();
builder.Services.AddScoped<IWeeklyService, WeeklyService>();
builder.Services.AddScoped<IHistoryService, HistoryService>();
builder.Services.AddScoped<IMiniMirrorService, MiniMirrorService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddHttpClient<ISupabaseAdminService, SupabaseAdminService>();

// ── Notification service placeholder ─────────────────────
// TODO: Replace with real push notification provider (Expo/FCM)

// ── Controllers ───────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Required for DateOnly serialization in responses
        options.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// ── Swagger ───────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Frank API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Enter: Bearer {token}",
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
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ── CORS ──────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrankApp", policy =>
        policy
            .WithOrigins(
                "http://localhost:8081",  // Expo dev
                "http://localhost:3000",
                "https://localhost:7199",
                "https://api-frenk-production.up.railway.app")  // local web
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

// ── Seed AppSettings on startup ───────────────────────────
using (var scope = app.Services.CreateScope())
{
    var settings = scope.ServiceProvider.GetRequiredService<IAppSettingsService>();
    await settings.RefreshAsync();
}

// ── Middleware pipeline ───────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<Frank.API.Middleware.ErrorHandlingMiddleware>();
// app.UseHttpsRedirection();
app.UseCors("FrankApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";
app.Run($"http://0.0.0.0:{port}");

// ── Placeholder services ──────────────────────────────────

/// <summary>
/// Placeholder until real push notification provider is wired.
/// Logs intent — does not send real notifications.
/// Replace with Expo Push / FCM in Week 5.
/// </summary>

