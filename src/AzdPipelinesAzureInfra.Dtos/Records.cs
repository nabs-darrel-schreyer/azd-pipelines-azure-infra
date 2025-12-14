namespace AzdPipelinesAzureInfra.Dtos;

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

public record Person(Guid Id, string Username, string FirstName, string LastName);
