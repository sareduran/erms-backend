using System.Text;
using System.Text.Json.Serialization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using ERMS.Api.Middleware;
using ERMS.Application;
using ERMS.Application.Interfaces;
using ERMS.Infrastructure;
using ERMS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddApplication().AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);
builder.Services.AddControllers().AddJsonOptions(x => x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// [FR-43/44] Controller'a ulaşmadan oluşan model-binding hatalarını da uygulamadaki
// diğer hatalarla aynı { code, message, errors } biçimine çevirir.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                x => string.IsNullOrWhiteSpace(x.Key) ? "request" : char.ToLowerInvariant(x.Key[0]) + x.Key[1..],
                x => x.Value!.Errors.Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage) ? "Geçersiz değer." : error.ErrorMessage).ToArray());
        return new BadRequestObjectResult(new { code = "VALIDATION_ERROR", message = "Doğrulama hatası.", errors });
    };
});
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy => policy
    .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
    .AllowAnyHeader()
    .AllowAnyMethod()));
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key tanımlanmalı.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };
    // Framework'ün ürettiği 401/403 yanıtları aksi halde boş gövdeli olurdu.
    // Frontend bu JSON mesajlarını doğrudan kullanıcıya gösterebilir.
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { code = "UNAUTHORIZED", message = "Oturum bilgileri eksik, geçersiz veya süresi dolmuş.", errors = (object?)null });
        },
        OnForbidden = async context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { code = "FORBIDDEN", message = "Bu işlem için yetkiniz yok.", errors = (object?)null });
        }
    };
});
builder.Services.AddAuthorization();
// [FR-47] Parola/token gövdelerini yazmadan metot, yol ve durum kodunu loglar.
builder.Services.AddHttpLogging(options => options.LoggingFields = HttpLoggingFields.RequestMethod | HttpLoggingFields.RequestPath | HttpLoggingFields.ResponseStatusCode);
var requestCreationPermitLimit = Math.Max(1, builder.Configuration.GetValue<int?>("RateLimiting:RequestCreation:PermitLimit") ?? 10);
var requestCreationWindowMinutes = Math.Max(1, builder.Configuration.GetValue<int?>("RateLimiting:RequestCreation:WindowMinutes") ?? 10);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("request-creation", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = requestCreationPermitLimit,
            Window = TimeSpan.FromMinutes(requestCreationWindowMinutes),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        var retryAfterSeconds = requestCreationWindowMinutes * 60;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            retryAfterSeconds = Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds));

        context.HttpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString();
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            code = "RATE_LIMITED",
            message = $"Çok kısa sürede fazla talep oluşturdunuz. Yaklaşık {Math.Max(1, (int)Math.Ceiling(retryAfterSeconds / 60d))} dakika sonra tekrar deneyin.",
            errors = (object?)null,
            retryAfterSeconds
        }, cancellationToken);
    };
});
builder.Services.AddEndpointsApiExplorer(); builder.Services.AddSwaggerGen(options => { options.SwaggerDoc("v1", new OpenApiInfo { Title = "ERMS API", Version = "v1", Description = "OYAK Dijital Çalışan Talep Yönetim Sistemi" }); options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Name = "Authorization", Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header }); options.AddSecurityRequirement(new OpenApiSecurityRequirement { [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = Array.Empty<string>() }); });
var app = builder.Build();
// Sıra önemlidir: HTTP logger, exception middleware'in sonradan verdiği gerçek 4xx/5xx
// durumunu görür; kimlik doğrulama da authorization'dan önce çalışır.
app.UseHttpLogging(); app.UseMiddleware<ExceptionMiddleware>(); app.UseSwagger(); app.UseSwaggerUI(); app.UseRouting(); app.UseCors("Frontend"); app.UseAuthentication(); app.UseMiddleware<PasswordChangeRequiredMiddleware>(); app.UseRateLimiter(); app.UseAuthorization(); app.MapControllers();
using (var scope = app.Services.CreateScope()) await DatabaseSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>(), scope.ServiceProvider.GetRequiredService<IPasswordService>());
app.Run();
public partial class Program;
