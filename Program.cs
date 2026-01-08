using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FinTechLiteAPI.Data;
using FinTechLiteAPI.Services;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using System.Text.Json;
using Microsoft.OpenApi.Models;

System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// ===== AWS Configuration =====
bool useAWS = builder.Configuration.GetValue<bool>("UseAWS", false);
string connectionString;

if (useAWS)
{
    // Retrieve connection string from AWS Secrets Manager
    var secretName = builder.Configuration["AWS:SecretName"] ?? "fintech-lite/db";
    var region = builder.Configuration["AWS:Region"] ?? "eu-north-1";

    using var client = new AmazonSecretsManagerClient(
        Amazon.RegionEndpoint.GetBySystemName(region));

    var request = new GetSecretValueRequest { SecretId = secretName };
    var response = await client.GetSecretValueAsync(request);

    var secret = JsonSerializer.Deserialize<Dictionary<string, string>>(response.SecretString);
    connectionString = secret!["ConnectionString"];
}
else
{
    // Local development - SQL Server
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Server = YOUR_SERVER; Database = FinTechLiteDB; User Id = YOUR_USER; Password = YOUR_PASSWORD; TrustServerCertificate = True";
}

// ===== Database Configuration - SQL SERVER =====
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sqlServerOptions =>
    {
        sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
        sqlServerOptions.CommandTimeout(30);
    }));

// ===== Services Registration =====
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// ===== JWT Authentication =====
var jwtKey = builder.Configuration["Jwt:Key"] ?? "YourSuperSecretKeyThatIsAtLeast32CharactersLong!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "FinTechLite";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "FinTechLiteUsers";

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
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ===== CORS Configuration =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorUI", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5001",      // Local Blazor dev
                "https://localhost:7001",     // Local Blazor HTTPS
                "http://localhost:5173",      // Vite dev server
                "https://localhost:7173",     // Vite HTTPS
                "https://yourdomain.com"      // Production domain
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ===== Controllers & Swagger =====
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FinTechLiteAPI", Version = "v1" });

    // 1. Define the Security Scheme (How the API expects the token)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter token (e.g., 'Bearer {token}')",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "bearer"
    });

    // 2. Apply the Security Requirement (Locking the endpoints)
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
            new string[]{}
        }
    });
});

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
