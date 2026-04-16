using lapo_vms_api.Data;
using lapo_vms_api.Interface;
using lapo_vms_api.Repository;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSqlServer<ApplicationDBContext>(builder.Configuration.GetConnectionString("DefaultConnection"));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});


builder.Services.AddScoped<IVisitorRepository, VisitorRepository>();
builder.Services.AddScoped<IVisitRepository, VisitRepository>();
builder.Services.AddScoped<IVisitItemRepository, VisitItemRepository>();


var app = builder.Build();

app.UseStaticFiles();

app.UseSwagger();
app.MapControllers();
app.UseSwaggerUI();
app.MapOpenApi();
app.MapScalarApiReference();

app.MapGet("/", () => "Hello World!");

app.Run();
