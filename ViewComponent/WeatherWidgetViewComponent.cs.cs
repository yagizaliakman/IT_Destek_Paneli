using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace IT_Destek_Panel.ViewComponents
{
    public class WeatherWidgetViewComponent : ViewComponent
    {
        // Artık hem şehri hem de koordinatları alıyor (Varsayılan İzmir kalsın)
        public async Task<IViewComponentResult> InvokeAsync(string city = "İzmir", string lat = "38.4127", string lon = "27.1384")
        {
            string temp = "--";
            try
            {
                using (var client = new HttpClient())
                {
                    // LİNK ARTIK DİNAMİK: Gelen lat ve lon değerlerini kullanıyor!
                    string url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current_weather=true";

                    var response = await client.GetStringAsync(url);

                    using (JsonDocument doc = JsonDocument.Parse(response))
                    {
                        var current = doc.RootElement.GetProperty("current_weather");
                        temp = current.GetProperty("temperature").GetDouble().ToString();
                    }
                }
            }
            catch
            {
                temp = "Bağlantı Yok";
            }

            // Gelen şehir adını ekranda göstermek için ViewBag ile View'a paslıyoruz
            ViewBag.City = city;

            return View("Default", temp);
        }
    }
}