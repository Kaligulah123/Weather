using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Weather.MVVM.Models;

namespace Weather.MVVM.ViewModels
{
    public partial class WeatherViewModel : ObservableObject
    {
        [ObservableProperty]
        public WeatherData? weatherData;

        [ObservableProperty]
        public string? placeName;

        [ObservableProperty]
        public DateTime date =
             DateTime.Now;

        [ObservableProperty]
        public bool isVisible;

        [ObservableProperty]
        public bool isLoading;

        private HttpClient client;

        public WeatherViewModel()
        {
            client = new HttpClient();
        }

        [RelayCommand]
        public async Task Search(string searchText)
        {
            PlaceName = searchText.ToString();
            var location = await GetCoordinatesAsync(searchText.ToString());

            if (location == null) return;

            await GetWeather(location);
        }
      


        private async Task GetWeather(Location location)
        {                       
            var url = string.Format(CultureInfo.InvariantCulture,
                     "https://api.open-meteo.com/v1/forecast?latitude={0}&longitude={1}&current=temperature_2m,weather_code,wind_speed_10m&daily=weather_code,temperature_2m_max",
                       location.Latitude, location.Longitude);

            IsLoading = true;

            var response =
              await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                using (var responseStream = await response.Content.ReadAsStreamAsync())
                {
                    var data = await JsonSerializer
                         .DeserializeAsync<WeatherData>(responseStream);
                    WeatherData = data;

                    for (int i = 0; i < WeatherData.daily.time.Length; i++)
                    {
                        var daily2 = new Daily2
                        {
                            time = WeatherData.daily.time[i],
                            temperature_2m_max = WeatherData.daily.temperature_2m_max[i],                            
                            weather_code = WeatherData.daily.weather_code[i]
                        };
                        WeatherData.daily2.Add(daily2);
                    }
                    IsVisible = true;
                }
            }
            IsLoading = false;
        }

        private async Task<Location> GetCoordinatesAsync(string address)
        {
            try
            {
                IEnumerable<Location> locations = await Geocoding.Default.GetLocationsAsync(address);

                Location location = locations?.FirstOrDefault();

                if (location != null)
                {
                    Console.WriteLine($"Latitude: {location.Latitude}, Longitude: {location.Longitude}, Altitude: {location.Altitude}");
                    return location;
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Location Not Found", $"No coordinates were found for the address: {address}. Please check and try again.", "Ok");           
                    return null;
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"An error occurred while retrieving the coordinates: {ex.Message}", "Ok");
                return null;
            }
        }

    }
}
