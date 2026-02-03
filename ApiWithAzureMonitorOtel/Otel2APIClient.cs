using OpenTelemetry;

namespace ApiWithAzureMonitorOtel;

public class Otel2APIClient
{
    private readonly HttpClient _client;
    
    public Otel2APIClient(HttpClient client)
    {
        _client = client;   
    }

    public async Task<IEnumerable<WeatherForecast>?> Api2weatherforecast()
    {
        Baggage.SetBaggage("app-miguel", "ejemplo-otel");
         var response = await _client.GetFromJsonAsync<IEnumerable<WeatherForecast>>("/api2weatherforecast");
         return response;
    }
}