using Microsoft.AspNetCore.Mvc;
using RaspberryPiControl.Models;
using RaspberryPiControl.Services;

namespace RaspberryPiControl.Controllers
{
    public class ReportsController : Controller
    {
        private readonly MongoDbService _mongoDbService;
        private readonly IRaspberryPiService _piService;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            MongoDbService mongoDbService,
            IRaspberryPiService piService,
            ILogger<ReportsController> logger)
        {
            _mongoDbService = mongoDbService;
            _piService = piService;
            _logger = logger;
        }

        public async Task<IActionResult> Graphics(string deviceId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var devices = await _piService.GetAllDevicesAsync();
                ViewBag.Devices = devices;

                if (!startDate.HasValue)
                    startDate = DateTime.UtcNow.AddDays(-7);
                if (!endDate.HasValue)
                    endDate = DateTime.UtcNow;

                var history = deviceId != null
                    ? await _mongoDbService.GetDeviceHistoryAsync(deviceId, startDate, endDate)
                    : await _mongoDbService.GetAllHistoryAsync(startDate, endDate);

                return View(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving report data");
                return View(Enumerable.Empty<DeviceStatusHistory>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetChartData(string deviceId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                if (!startDate.HasValue)
                    startDate = DateTime.UtcNow.AddDays(-7);
                if (!endDate.HasValue)
                    endDate = DateTime.UtcNow;

                var history = deviceId != null
                    ? await _mongoDbService.GetDeviceHistoryAsync(deviceId, startDate, endDate)
                    : await _mongoDbService.GetAllHistoryAsync(startDate, endDate);

                // Generate a complete date range
                var dateRange = Enumerable.Range(0, (endDate.Value - startDate.Value).Days + 1)
                    .Select(offset => startDate.Value.AddDays(offset).Date)
                    .ToList();

                var statusData = dateRange.Select(date => new
                {
                    Date = date.ToString("yyyy-MM-dd"),
                    Online = history.Count(h => h.Timestamp.Date == date && h.Status == "Online"),
                    Offline = history.Count(h => h.Timestamp.Date == date && h.Status == "Offline")
                }).ToList();

                var accessData = dateRange.Select(date => new
                {
                    Date = date.ToString("yyyy-MM-dd"),
                    Open = history.Count(h => h.Timestamp.Date == date && h.AccessStatus == "Open"),
                    Closed = history.Count(h => h.Timestamp.Date == date && h.AccessStatus == "Closed")
                }).ToList();

                return Json(new { statusData, accessData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chart data");
                return Json(new { error = "Failed to retrieve chart data" });
            }
        }
    }
}