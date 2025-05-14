using Microsoft.AspNetCore.Mvc;
using RaspberryPiControl.Models;
using RaspberryPiControl.Services;

namespace RaspberryPiControl.Controllers
{
    public class HistoryController : Controller
    {
        private readonly MongoDbService _mongoDbService;
        private readonly ILogger<HistoryController> _logger;

        public HistoryController(MongoDbService mongoDbService, ILogger<HistoryController> logger)
        {
            _mongoDbService = mongoDbService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string deviceId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                if (!await _mongoDbService.IsConnected())
                {
                    TempData["ErrorMessage"] = "Could not connect to the database. Please ensure MongoDB is running.";
                    return View(Enumerable.Empty<DeviceStatusHistory>());
                }

                var history = deviceId != null 
                    ? await _mongoDbService.GetDeviceHistoryAsync(deviceId, startDate, endDate)
                    : await _mongoDbService.GetAllHistoryAsync(startDate, endDate);

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