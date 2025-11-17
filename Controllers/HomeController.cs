using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using WeatherDashboard.Models;

namespace WeatherDashboard.Controllers
{
    public class HomeController : Controller
    {
        private readonly HttpClient _httpClient;
        private const string apiKey = "37cd288e69f40d10b2d2dbd9b9c59c81"; 

        public HomeController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string city)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                ViewBag.Error = "Please enter a city name.";
                return View();
            }

            string url = $"https://api.openweathermap.org/data/2.5/weather?q={city}&appid={apiKey}&units=metric";

            try
            {
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    ViewBag.Error = "City not found or API request failed.";
                    return View();
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                var weather = doc.RootElement.GetProperty("weather")[0];
                var main = doc.RootElement.GetProperty("main");
                var wind = doc.RootElement.GetProperty("wind");
                var sys = doc.RootElement.GetProperty("sys");

                var model = new WeatherViewModel
                {
                    City = doc.RootElement.GetProperty("name").GetString(),
                    Country = sys.GetProperty("country").GetString(),
                    Description = weather.GetProperty("description").GetString(),
                    Icon = weather.GetProperty("icon").GetString(),
                    Temperature = main.GetProperty("temp").GetSingle(),
                    Humidity = main.GetProperty("humidity").GetInt32(),
                    WindSpeed = wind.GetProperty("speed").GetSingle()
                };

                string forecastUrl = $"https://api.openweathermap.org/data/2.5/forecast?q={city}&appid={apiKey}&units=metric";
                var forecastResponse = await _httpClient.GetAsync(forecastUrl);

                if (forecastResponse.IsSuccessStatusCode)
                {
                    var forecastJson = await forecastResponse.Content.ReadAsStringAsync();
                    using var forecastDoc = JsonDocument.Parse(forecastJson);

                    var list = forecastDoc.RootElement.GetProperty("list");

                    // get one forecast per day around 12:00pm
                    var forecastByDay = list.EnumerateArray()
                        .Where(x => x.GetProperty("dt_txt").GetString().Contains("12:00:00"))
                        .Take(5); // only 5 days

                    foreach (var item in forecastByDay)
                    {
                        var date = item.GetProperty("dt_txt").GetString(); // full timestamp
                        var temp = item.GetProperty("main").GetProperty("temp").GetSingle();
                        var desc = item.GetProperty("weather")[0].GetProperty("description").GetString();
                        var icon = item.GetProperty("weather")[0].GetProperty("icon").GetString();

                        model.Forecasts.Add(new ForecastViewModel
                        {
                            Date = date.Split(' ')[0], // just the date part (yyyy-mm-dd)
                            Temp = temp,
                            Description = desc,
                            Icon = icon
                        });
                    }
                }

                return View(model);

                // save search history
                List<string> history = TempData["History"] as List<string> ?? new List<string>();
                if (!history.Contains(city, StringComparer.OrdinalIgnoreCase))
                {
                    history.Insert(0, city);
                    if (history.Count > 5) history.RemoveAt(5); // keep last 5
                }
                TempData["History"] = history;
                TempData.Keep("History");

            }
            catch
            {
                ViewBag.Error = "An error occurred while fetching weather data.";
                return View();
            }
        }
    }
}
