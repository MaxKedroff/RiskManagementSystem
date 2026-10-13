using System.Text;
using Application;
using Application.Abstractions;
using Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Presentation.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddApplication();

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(options.Issuer) &&
            !string.IsNullOrWhiteSpace(options.Audience) &&
            !string.IsNullOrWhiteSpace(options.SecretKey) &&
            Encoding.UTF8.GetByteCount(options.SecretKey) >= 32 &&
            options.ExpirationMinutes > 0,
        "JWT configuration is invalid. Check Jwt settings and use a secret key of at least 32 bytes.")
    .ValidateOnStart();

builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

var jwt = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "The Jwt configuration section is missing.");

if (string.IsNullOrWhiteSpace(jwt.SecretKey) ||
    Encoding.UTF8.GetByteCount(jwt.SecretKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:SecretKey must contain at least 32 bytes.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,

            ValidateAudience = true,
            ValidAudience = jwt.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwt.SecretKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),

            NameClaimType = System.Security.Claims.ClaimTypes.NameIdentifier,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Risk Management API",
        Version = "v1",
        Description = "API for risk management"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT access token. The Bearer prefix is handled by Swagger."
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "Risk Management API v1");

        options.RoutePrefix = "swagger";
    });

    app.MapGet("/_dev/hash/{password}", (string password) =>
    {
        var hasher = new PasswordHasher<object>();
        return Results.Text(hasher.HashPassword(new object(), password));
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();