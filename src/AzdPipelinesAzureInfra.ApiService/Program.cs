using AzdPipelinesAzureInfra.Persistence;
using AzdPipelinesAzureInfra.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.AddAzureAppConfiguration("appconfig", configureOptions: options =>
{
    options.Select("*", "AzdPipelines");
});

builder.AddSqlServerDbContext<TestDbContext>(connectionName: "test-db");

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/", () => "API service is running. Navigate to /weatherforecast to see sample data.");

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.MapGet("/people", async ([FromServices]TestDbContext dbContext) =>
{
    var people = await dbContext.People
        .Select(x => new Person(x.Id, x.Username, x.FirstName, x.LastName, x.YearOfBirth))
        .ToListAsync();
    return people;
})
.WithName("GetPeople");

app.MapGet("/config/{key}", ([FromServices]IConfiguration configuration, string key) =>
{
    var configValue = configuration[key];
    return configValue;

}).WithName("GetOptions");

app.MapDefaultEndpoints();

app.Run();
