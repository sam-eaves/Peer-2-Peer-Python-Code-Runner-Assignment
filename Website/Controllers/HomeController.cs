using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using RestSharp;
using System.Diagnostics;
using Website.Models;

namespace Website.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> Dashboard()
        {
            var client = new RestClient("http://localhost:5000");
            var request = new RestRequest("/api/client/getAll", Method.Get);

            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                // Deserialize the JSON response to a list of Client objects using Newtonsoft.Json
                var clients = JsonConvert.DeserializeObject<List<Client>>(response.Content);
                return View(clients); // Pass the client data to the view
            }
            else
            {
                // Handle error if the API call fails
                return View(new List<Client>());
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
