using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using tsu_absences_api.Data;
using tsu_absences_api.Interfaces;
using tsu_absences_api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IAbsenceService, AbsenceService>();
builder.Services.AddScoped<IFileService, FileService>();

// заглушки
//builder.Services.AddScoped<IFileService, MockFileService>();
builder.Services.AddScoped<IUserService, MockUserService>();

builder.Services
    .AddControllers()
    .AddJsonOptions(opts => { 
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); 
    });

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();