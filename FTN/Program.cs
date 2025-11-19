using FluentValidation;
using FTN.Dtos;
using FTN.Models;
using FTN.Services;
using FTN.Validators;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Configuracion de FluenValidation
builder.Services.AddValidatorsFromAssemblyContaining<StageEntrancesDtoValidator>();

//Validadores
builder.Services.AddScoped<IValidator<StageEntrancesDto>, StageEntrancesDtoValidator>();
builder.Services.AddScoped<IValidator<StageEntrances>, StageEntrancesValidator>();

//Servicios
builder.Services.AddScoped<IExcelReportService, ExcelReportService>();

var connection = builder.Configuration.GetConnectionString("Connection");
builder.Services.AddDbContext<AppDbContext>(option =>
{
    option.UseSqlServer(connection);
});

var allowedConnection = builder.Configuration.GetValue<string>("OrigenesPermitidos")!.Split(',');
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(allowedConnection)
           .AllowAnyHeader()
           .AllowAnyMethod();
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseCors("AllowSpecificOrigins");

app.UseAuthorization();

app.MapControllers();

app.Run();
