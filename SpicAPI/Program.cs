using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Spic.Infrastructure.Data;
using Spic.Infrastructure.Services;
using SPIC.Core.Entities;
using SPIC.Core.Interfaces;
using System.Security.Claims;
using System.Text;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHealthChecks();

// In Azure the API runs as several replicas. Data-protection keys (Identity
// tokens) must be shared between them, so when DataProtection:KeysPath is
// configured the key ring is persisted to that (mounted) folder. Without the
// setting the default per-machine key ring is used, exactly as before.
var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
{
    Directory.CreateDirectory(dataProtectionKeysPath);
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b =>
        {
            b.MigrationsAssembly("Spic.Infrastructure");
            b.CommandTimeout(600);
        }));

// IFMS automation uses a separate database. The API reads the IFMS data but
// the portal and automation retain separate ownership of their own tables.
builder.Services.AddDbContext<IfmsDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("IfmsConnection")
            ?? builder.Configuration.GetConnectionString("DefaultConnection"),
        b =>
        {
            b.MigrationsAssembly("Spic.Infrastructure");
            b.CommandTimeout(600);
        }));

builder.Services.AddIdentity<UserInfo, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = false;
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+& ";
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IExcelBulkUploadService, ExcelBulkUploadService>();
builder.Services.AddScoped<IIfmsAccountStore, IfmsAccountStore>();
builder.Services.AddScoped<IIfmsRelayDeviceStore, IfmsRelayDeviceStore>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IStockReportService, StockReportService>();
builder.Services.AddScoped<IPendingAckService, PendingAckService>();
builder.Services.AddScoped<IAgeingReportService, AgeingReportService>();
builder.Services.AddScoped<IAckCycleService, AckCycleService>();
builder.Services.AddScoped<ILiquidationCycleService, LiquidationCycleService>();
builder.Services.AddScoped<IProductStockAvailabilityService, ProductStockAvailabilityService>();
builder.Services.AddScoped<IStockDetailsService, StockDetailsService>();

// Shares the IFMS portal-password encryption keys with the automation service.
// The application name is part of the key derivation, so it must match the
// automation exactly or neither can read what the other wrote.
// The IFMS credential protector has its OWN key ring (spiconeifms). The API's
// Data Protection stays on its default key ring, as production ran before.
builder.Services.AddSingleton<IIfmsDataProtection>(_ =>
    new IfmsDataProtection(
        builder.Configuration.GetConnectionString("IfmsConnection")
        ?? builder.Configuration.GetConnectionString("DefaultConnection")
        ?? string.Empty));

// Fail fast at startup when required JWT settings are missing.
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("Jwt:Audience is not configured.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)),
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.Name,

        // Expired JWTs are rejected immediately, with no default grace period.
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) &&
                (path.StartsWithSegments("/api/DealerFile/view") ||
                 path.StartsWithSegments("/api/LogisticsFile/view") ||
                 path.StartsWithSegments("/api/LogisticsFile/download") ||
                 path.StartsWithSegments("/api/SDWAWelfareApplication/document") ||
                 path.StartsWithSegments("/api/WelfareSchemeApproval/document") ||
                 path.StartsWithSegments("/api/GuestHouseBooking/image")))
            {
                context.Token = accessToken;
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SPIC API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    c.AddSecurityRequirement(document =>
    {
        return new OpenApiSecurityRequirement
        {
            [
                new OpenApiSecuritySchemeReference("Bearer", document)
            ] = new List<string>()
        };
    });
});

// Kept unchanged to avoid altering the current production flow.
// For production hardening, replace AllowAll with explicit trusted origins.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Kept unchanged from the existing application behavior.
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => "SPIC API is running");
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();
