// Program.cs
using System.Text;
using Bakalauras.API.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using Bakalauras.API.Services;
using Resend;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// â”€â”€ DbContext â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

var dbConnectionString = builder.Configuration.GetConnectionString("Default");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        dbConnectionString,
        ServerVersion.AutoDetect(dbConnectionString),
        mySql => mySql.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
    )
);

// â”€â”€ Services â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

builder.Services.AddOptions();

var resendApiKey = builder.Configuration["EmailSettings:ResendApiKey"];
if (!string.IsNullOrWhiteSpace(resendApiKey))
{
    builder.Services.AddHttpClient<ResendClient>();
    builder.Services.Configure<ResendClientOptions>(options =>
    {
        options.ApiToken = resendApiKey;
        options.ThrowExceptions = true;
    });
    builder.Services.AddTransient<IResend, ResendClient>();
    builder.Services.AddSingleton<EmailBackgroundQueue>();
    builder.Services.AddHostedService<ResendEmailBackgroundWorker>();
    builder.Services.AddScoped<IEmailService, ResendEmailService>();
}
else
{
    builder.Services.AddScoped<IEmailService, NoOpEmailService>();
}
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<CourierProviderFactory>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient();


var jwtSettings = builder.Configuration.GetSection("Jwt");
var jwtKey = jwtSettings["Key"];

if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Jwt:Key is missing in configuration.");

var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),

            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
            ClockSkew = TimeSpan.Zero,
        };

        // â”€â”€ Read JWT from the httpOnly cookie instead of Authorization header â”€
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                if (ctx.Request.Cookies.TryGetValue("auth_token", out var cookieToken))
                    ctx.Token = cookieToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();


builder.Services.AddCors(options =>
{
    var frontendOrigins = new[]
    {
        "http://localhost:3000",
        "http://localhost:5173",
        builder.Configuration["FrontendBaseUrl"]
    }
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin!.TrimEnd('/'))
    .Distinct()
    .ToArray();

    options.AddPolicy("Frontend", policy =>
        policy
            .WithOrigins(frontendOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});


var encKey = builder.Configuration["Encryption:Key"]
    ?? throw new InvalidOperationException("Encryption:Key is missing.");
IntegrationSecrets.Configure(encKey);

QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddHostedService<ClientSyncWorker>();
builder.Services.AddHostedService<ProductSyncWorker>();
builder.Services.AddHostedService<OrderSyncWorker>();


var app = builder.Build();

app.UseRouting();
app.UseCors("Frontend");       // must be before UseAuthentication
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
