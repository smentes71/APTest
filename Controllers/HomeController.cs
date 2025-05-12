using Microsoft.AspNetCore.Mvc;
using RaspberryPiControl.Models;
using RaspberryPiControl.Services;

namespace RaspberryPiControl.Controllers
{
    public class HomeController : Controller
    {
        private readonly IRaspberryPiService _piService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IRaspberryPiService piService, ILogger<HomeController> logger)
        {
            _piService = piService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var devices = await _piService.GetAllDevicesAsync();
                if (!devices.Any())
                {
                    _logger.LogInformation("No devices found");
                }
                else
                {
                    _logger.LogInformation($"Retrieved {devices.Count()} devices");
                }
                return View(devices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving devices");
                return View(Enumerable.Empty<RaspberryPi>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendCommand(string IpAddress, CommandType Command)
        {
            Console.WriteLine(Request);
            if (string.IsNullOrEmpty(IpAddress))
            {
                return Json(new { success = false, message = "IP address is required" });
            }

            try
            {
                _logger.LogInformation($"Sending {Command} command to device at {IpAddress}");
                var result = await _piService.SendCommandAsync(IpAddress, Command);
                return Json(new { success = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending command to device {IpAddress}", IpAddress);
                return Json(new { success = false, message = "Failed to send command" });
            }
        }




       





        [HttpPost]
        public async Task<IActionResult> SendCommand3([FromBody] CommandRequest request)
        {
            if (string.IsNullOrEmpty(request.IpAddress) || string.IsNullOrEmpty(request.Command))
            {
                return Json(new { success = false, message = "IP address and command are required." });
            }

            try
            {
                var url = $"http://{request.IpAddress}/{request.Command}";
                using var httpClient = new HttpClient();

                var response = await httpClient.GetAsync(url);
                var responseBody = await response.Content.ReadAsStringAsync();

                bool isSuccess = response.IsSuccessStatusCode;

                return Json(new
                {
                    success = isSuccess,
                    message = isSuccess ? "Command sent successfully." : $"Error from device: {responseBody}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending command to {IpAddress}", request.IpAddress);
                return Json(new { success = false, message = "Failed to send command via HTTP." });
            }
        }
    }
   
}