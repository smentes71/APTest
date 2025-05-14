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
                var allHistory = await _mongoDbService.GetAllHistoryAsync();
                return Json(new { success = true, data = allHistory });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving collection data");
                return Json(new { success = false, error = ex.Message });
            }
        }
    }
}