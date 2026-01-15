using System.Collections.Concurrent;
using Newtonsoft.Json;
using Assignment_A1_03.Models;

namespace Assignment_A1_03.Services;

public class OpenWeatherService
{
    readonly HttpClient _httpClient = new HttpClient();
    readonly ConcurrentDictionary<(double, double), (Forecast forecast, DateTime cachedAt)> _cachedGeoForecasts = 
        new ConcurrentDictionary<(double, double), (Forecast, DateTime)>();
    readonly ConcurrentDictionary<string, (Forecast forecast, DateTime cachedAt)> _cachedCityForecasts = 
        new ConcurrentDictionary<string, (Forecast, DateTime)>();

    readonly string apiKey = "92503e800b2d6cc0fa2b906aedfa52d8";

    public event EventHandler<string> WeatherForecastAvailable;
    protected virtual void OnWeatherForecastAvailable(string message)
    {
        WeatherForecastAvailable?.Invoke(this, message);
    }
    
    public async Task<Forecast> GetForecastAsync(string City)
    {
        if (_cachedCityForecasts.TryGetValue(City, out var cached))
        {
            if ((DateTime.Now - cached.cachedAt).TotalMinutes < 1)
            {
                OnWeatherForecastAvailable($"Cached weather forecast for {City} available");
                return cached.forecast;
            }
            _cachedCityForecasts.TryRemove(City, out _);
        }
        
        var language = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var uri = $"https://api.openweathermap.org/data/2.5/forecast?q={City}&units=metric&lang={language}&appid={apiKey}";

        Forecast forecast = await ReadWebApiAsync(uri);

        _cachedCityForecasts.TryAdd(City, (forecast, DateTime.Now));
        OnWeatherForecastAvailable($"Downloaded weather forecast for {City} available");

        return forecast;
    }
    
    public async Task<Forecast> GetForecastAsync(double latitude, double longitude)
    {
        var cacheKey = (latitude, longitude);
        
        if (_cachedGeoForecasts.TryGetValue(cacheKey, out var cached))
        {
            if ((DateTime.Now - cached.cachedAt).TotalMinutes < 1)
            {
                OnWeatherForecastAvailable($"Cached weather forecast for ({latitude}, {longitude}) available");
                return cached.forecast;
            }
            _cachedGeoForecasts.TryRemove(cacheKey, out _);
        }

        var language = System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var uri = $"https://api.openweathermap.org/data/2.5/forecast?lat={latitude}&lon={longitude}&units=metric&lang={language}&appid={apiKey}";

        Forecast forecast = await ReadWebApiAsync(uri);

        _cachedGeoForecasts.TryAdd(cacheKey, (forecast, DateTime.Now));
        OnWeatherForecastAvailable($"Downloaded weather forecast for ({latitude}, {longitude}) available");

        return forecast;
    }
    
    private async Task<Forecast> ReadWebApiAsync(string uri)
    {
        HttpResponseMessage response = await _httpClient.GetAsync(uri);
        response.EnsureSuccessStatusCode();

        string content = await response.Content.ReadAsStringAsync();
        WeatherApiData wd = JsonConvert.DeserializeObject<WeatherApiData>(content);

        var forecast = new Forecast()
        {
            City = wd.city.name,
            Items = wd.list.Select(item => new ForecastItem
            {
                DateTime = UnixTimeStampToDateTime(item.dt),
                Temperature = item.main.temp,
                WindSpeed = item.wind.speed,
                Description = item.weather.First().description,
                Icon = $"http://openweathermap.org/img/w/{item.weather.First().icon}.png"
            }).ToList()
        };
        
        return forecast;
    }

    private DateTime UnixTimeStampToDateTime(double unixTimeStamp) => 
        DateTime.UnixEpoch.AddSeconds(unixTimeStamp).ToLocalTime();
}