using Microsoft.AspNetCore.Mvc;
using RaspberryPiControl.Models;
using RaspberryPiControl.Services;

namespace RaspberryPiControl.Controllers
{
    public class HistoryController : Controller
    {
        private readonly MongoDbService _mongoDbService;
        private readonly IRaspberryPiService _piService;
        private readonly ILogger<HistoryController> _logger;

        public HistoryController(
            MongoDbService mongoDbService, 
            IRaspberryPiService piService,
            ILogger<HistoryController> logger)
        {
            _mongoDbService = mongoDbService;
            _piService = piService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(
            string deviceName = null,
            string ipAddress = null,
            string status = null,
            string accessStatus = null,
            string location = null,
            string group = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                if (!await _mongoDbService.IsConnected())
                {
                    TempData["ErrorMessage"] = "Could not connect to database. Please ensure MongoDB is running.";
                    return View(Enumerable.Empty<DeviceStatusHistory>());
                }

                await _mongoDbService.EnsureCollectionExists();

                // Get all devices to populate location and group filters
                var devices = await _piService.GetAllDevicesAsync();
                var locations = devices.Select(d => d.Location).Where(l => !string.IsNullOrEmpty(l)).Distinct().OrderBy(l => l);
                var groups = devices.Select(d => d.Group).Where(g => !string.IsNullOrEmpty(g)).Distinct().OrderBy(g => g);

                ViewBag.Locations = locations;
                ViewBag.Groups = groups;

                var history = await _mongoDbService.GetAllHistoryAsync(startDate, endDate);

                // Apply filters
                if (!string.IsNullOrWhiteSpace(deviceName))
                {
                    history = history.Where(h => h.DeviceName.Contains(deviceName, StringComparison.OrdinalIgnoreCase));
                }
                if (!string.IsNullOrWhiteSpace(ipAddress))
                {
                    history = history.Where(h => h.IpAddress.Contains(ipAddress, StringComparison.OrdinalIgnoreCase));
                }
                if (!string.IsNullOrWhiteSpace(status))
                {
                    history = history.Where(h => h.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
                }
                if (!string.IsNullOrWhiteSpace(accessStatus))
                {
                    history = history.Where(h => h.AccessStatus.Equals(accessStatus, StringComparison.OrdinalIgnoreCase));
                }
                if (!string.IsNullOrWhiteSpace(location))
                {
                    history = history.Where(h => h.Location.Equals(location, StringComparison.OrdinalIgnoreCase));
                }
                if (!string.IsNullOrWhiteSpace(group))
                {
                    history = history.Where(h => h.Group.Equals(group, StringComparison.OrdinalIgnoreCase));
                }

                return View(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving device history");
                TempData["ErrorMessage"] = "Failed to retrieve device history. Please try again later.";
                return View(Enumerable.Empty<DeviceStatusHistory>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> ViewCollection()
        {
            try
            {
                if (!await _mongoDbService.IsConnected())
                {
                    return Json(new { 
                        success = false, 
                        error = "Could not connect to MongoDB. Please ensure the service is running."
                    });
                }

                await _mongoDbService.EnsureCollectionExists();
                var allHistory = await _mongoDbService.GetAllHistoryAsync();
                
                return Json(new { 
                    success = true, 
                    data = allHistory,
                    count = allHistory.Count()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving collection data");
                return Json(new { 
                    success = false, 
                    error = "Failed to retrieve collection data",
                    details = ex.Message
                });
            }
        }
    }
}