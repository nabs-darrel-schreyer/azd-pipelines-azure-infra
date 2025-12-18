using AzdPipelinesAzureInfra.Dtos;

namespace AzdPipelinesAzureInfra.Web;

public class ApiClients(HttpClient httpClient)
{
    public async Task<WeatherForecast[]> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        List<WeatherForecast>? forecasts = null;

        await foreach (var forecast in httpClient.GetFromJsonAsAsyncEnumerable<WeatherForecast>("/weatherforecast", cancellationToken))
        {
            if (forecasts?.Count >= maxItems)
            {
                break;
            }
            if (forecast is not null)
            {
                forecasts ??= [];
                forecasts.Add(forecast);
            }
        }

        return forecasts?.ToArray() ?? [];
    }
}

public class PeopleApiClient(HttpClient httpClient)
{
    public async Task<Person[]> GetPeopleAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        List<Person>? people = null;

        await foreach (var person in httpClient.GetFromJsonAsAsyncEnumerable<Person>("/people", cancellationToken))
        {
            if (people?.Count >= maxItems)
            {
                break;
            }
            if (person is not null)
            {
                people ??= [];
                people.Add(person);
            }
        }

        return people?.ToArray() ?? [];
    }
}

public class ConfigApiClient(HttpClient httpClient)
{
    public async Task<string> GetConfigAsync(string key, CancellationToken cancellationToken = default)
    {
        var keyValue = await httpClient.GetStringAsync($"/config/{key}", cancellationToken);

        return keyValue ?? $"No value found for {key}";
    }
}