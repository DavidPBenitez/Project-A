using Assignment_A1_03.Models;
using Assignment_A1_03.Services;

namespace Assignment_A1_03;

class Program
{
    static async Task Main(string[] args)
    {
        OpenWeatherService service = new OpenWeatherService();
        service.WeatherForecastAvailable += Service_WeatherForecastAvailable;

        double latitude = 60.6749;
        double longitude = 17.1413;

        Console.WriteLine("First set of requests");
        var task1 = service.GetForecastAsync(latitude, longitude);
        var task2 = service.GetForecastAsync("Gävle");
        
        await Task.WhenAll(task1, task2);
        
        await PrintForecastTask(task1);
        await PrintForecastTask(task2);

        Console.WriteLine("Second set of requests");
        
        var task3 = service.GetForecastAsync(latitude, longitude);
        var task4 = service.GetForecastAsync("Gävle");
        
        await Task.WhenAll(task3, task4);
        
        await PrintForecastTask(task3);
        await PrintForecastTask(task4);

        Console.WriteLine("\nInvalid City Test");
        var task5 = service.GetForecastAsync("ThisIsNotACity");
        await PrintForecastTask(task5);
    }

    private static async Task PrintForecastTask(Task<Forecast> task)
    {
        try
        {
            Forecast forecast = await task;
            Console.WriteLine($"\nWeather forecast for {forecast.City}");
            
            var grouped = forecast.Items.GroupBy(item => item.DateTime.Date);
            
            foreach (var group in grouped)
            {
                Console.WriteLine($"{group.Key:yyyy-MM-dd}");
                
                foreach (var item in group)
                {
                    Console.WriteLine($"  {item.DateTime:HH:mm}: {item.Description}, " +
                    $"temperature: {item.Temperature:F1} degC, " + $"wind: {item.WindSpeed:F2} m/s");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nCould not retrieve forecast: {ex.GetBaseException().Message}");
        }
    }
    
    private static void Service_WeatherForecastAvailable(object sender, string message)
    {
        Console.WriteLine($"Event: {message}");
    }
}