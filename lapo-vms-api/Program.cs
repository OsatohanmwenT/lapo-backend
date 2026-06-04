using lapo_vms_api.API.Helpers;
using lapo_vms_api.Data;
using lapo_vms_api.Helpers;
using lapo_vms_api.Interface;
using lapo_vms_api.Repository;
using lapo_vms_api.Services;
using Scalar.AspNetCore;
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using System.Text.Json;
using lapo_vms_api.Middleware;
using Serilog;

var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envPath))
{
    foreach (var line in File.ReadAllLines(envPath))
    {
        var trimmedLine = line.Trim();
        if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
        {
            continue;
        }

        var separatorIndex = trimmedLine.IndexOf('=');
        if (separatorIndex <= 0)
        {
            continue;
        }

        var key = trimmedLine[..separatorIndex].Trim();
        var value = trimmedLine[(separatorIndex + 1)..].Trim().Trim('"');

        if (Environment.GetEnvironmentVariable(key) == null)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }
}

var builder = WebApplication.CreateBuilder(args);
// builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Host.UseSerilog((context, services, config) =>
{
    config
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Error)
        .Enrich.FromLogContext()
        .WriteTo.Console(
            outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} TraceId={TraceId} Method={Method} Path={Path} Status={StatusCode} Source={ErrorSource} {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: "logs/app-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            outputTemplate:
            "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext} TraceId={TraceId} Method={Method} Path={Path} Status={StatusCode} Source={ErrorSource} {Message:lj}{NewLine}{Exception}");
});

builder.Services.AddSqlServer<ApplicationDBContext>(builder.Configuration.GetConnectionString("DefaultConnection"));
builder.Services.AddControllers(options =>
    {
        options.Filters.Add(new AuthorizeFilter());
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter a valid JWT bearer token."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document, null),
            []
        }
    });
});
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info.Title = "Lapo VMS API";
        document.Info.Version = "v1";
        document.Info.Description = "API for Lapo VMS";
        return Task.CompletedTask;
    });
});


builder.Services.AddScoped<IVisitorRepository, VisitorRepository>();
builder.Services.AddScoped<IVisitRepository, VisitRepository>();
builder.Services.AddScoped<IVisitItemRepository, VisitItemRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<AdAuthHelper>();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var useLocalAuth = builder.Environment.IsDevelopment()
    && builder.Configuration.GetValue<bool>("LocalAuth:Enabled");
var authenticationScheme = useLocalAuth
    ? LocalAuthHandler.SchemeName
    : JwtBearerDefaults.AuthenticationScheme;

var authentication = builder.Services.AddAuthentication(authenticationScheme);

if (useLocalAuth)
{
    authentication.AddScheme<AuthenticationSchemeOptions, LocalAuthHandler>(
        LocalAuthHandler.SchemeName,
        _ => { });
}
else
{
    authentication
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
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!))
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                var message = context.AuthenticateFailure == null
                    ? "Authentication required. Please provide a valid bearer token."
                    : "Invalid or expired bearer token.";

                await context.Response.WriteAsync(JsonSerializer.Serialize(new { message }));
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { message = "You are not allowed to access this resource." }));
            }
        };
    });
}

builder.Services.AddAuthorization();

var frontendOrigins = builder.Configuration["FrontendOrigins"]?
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? ["http://localhost:5173", "https://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins(frontendOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

app.Logger.LogInformation(
    "Application starting. Environment={Environment} LocalAuthEnabled={LocalAuthEnabled}",
    app.Environment.EnvironmentName,
    useLocalAuth);

app.UseCors("AllowFrontend");

app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI();
app.MapOpenApi();
app.MapScalarApiReference();

app.UseMiddleware<TraceLoggingMiddleware>();
app.UseAuthentication();
app.UseMiddleware<AuditMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/", () => "Hello World!");

app.Run();
