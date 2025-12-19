using Expenses.API.framework.Data;
using Expenses.API.Framework.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// configurre entity
//string conn = "Data Source=DESKTOP-R25ASQT;Initial Catalog=asp_db_transactions;Integrated Security=True;Encrypt=False;Trust Server Certificate=True";
string? conn = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrEmpty(conn))
    throw new InvalidOperationException("La chaîne de connexion 'Default' est manquante ou vide dans la configuration.");

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(conn));

// add dependency injection for services
builder.Services.AddApplicationServices();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();
// add support for swagger #1
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    //app.MapOpenApi();
    // add support for swagger#2
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
