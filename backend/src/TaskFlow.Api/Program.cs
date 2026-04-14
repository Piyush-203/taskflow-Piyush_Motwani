using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using TaskFlow.Api;
using TaskFlow.Api.Data;
using TaskFlow.Api.Middleware;
using TaskFlow.Api.Services;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    
    builder.Host.UseSerilog((ctx, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    );

    
    var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is required");

    builder.Services.AddDbContext<AppDbContext>(opt =>
        opt.UseNpgsql(connStr));

    
    var jwtSecret = builder.Configuration["Jwt:Secret"]
        ?? throw new InvalidOperationException("Jwt:Secret is required");

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opt =>
        {
            opt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ClockSkew = TimeSpan.Zero
            };
        });

    builder.Services.AddAuthorization();

    
    builder.Services.AddScoped<ITokenService, TokenService>();

   
    builder.Services.AddControllers()
        .ConfigureApiBehaviorOptions(opt =>
        {
            
            opt.SuppressModelStateInvalidFilter = true;
        });

   
    builder.Services.AddCors(opt =>
        opt.AddDefaultPolicy(p =>
            p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

    
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "TaskFlow API", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    var app = builder.Build();

    app.UseMiddleware<ExceptionMiddleware>();

    app.UseSerilogRequestLogging();

    
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskFlow API v1");
        c.RoutePrefix = "swagger";
    });

    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();

    
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        await DbSeeder.SeedAsync(db, logger);
    }

    
    var lifetime = app.Lifetime;
    lifetime.ApplicationStopping.Register(() =>
        Log.Information("Application is shutting down..."));

    Log.Information("TaskFlow API starting up on {Env}", app.Environment.EnvironmentName);
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;
