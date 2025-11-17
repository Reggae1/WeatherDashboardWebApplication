namespace WeatherDashboard.Models
{
    public class WeatherViewModel
    {
        public string City { get; set; }
        public string Country { get; set; }
        public string Description { get; set; }
        public float Temperature { get; set; }
        public int Humidity { get; set; }
        public float WindSpeed { get; set; }
        public string Icon { get; set; } // add this property
        public List<ForecastViewModel> Forecasts { get; set; } = new();


    }
}
