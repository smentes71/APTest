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
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving report data");
                return View();
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

                var dateRange = Enumerable.Range(0, (endDate.Value - startDate.Value).Days + 1)
                    .Select(offset => startDate.Value.AddDays(offset).Date)
                    .ToList();

                var statusData = dateRange.Select(date =>
                {
                    var dayHistory = history.Where(h => h.Timestamp.Date == date);
                    return new
                    {
                        Date = date.ToString("yyyy-MM-dd"),
                        Online = dayHistory.Count(h => h.Status.ToLower() == "online"),
                        Offline = dayHistory.Count(h => h.Status.ToLower() == "offline")
                    };
                }).ToList();

                var accessData = dateRange.Select(date =>
                {
                    var dayHistory = history.Where(h => h.Timestamp.Date == date);
                    return new
                    {
                        Date = date.ToString("yyyy-MM-dd"),
                        Open = dayHistory.Count(h => h.AccessStatus.ToLower() == "open"),
                        Closed = dayHistory.Count(h => h.AccessStatus.ToLower() == "closed")
                    };
                }).ToList();

                _logger.LogInformation($"Retrieved {statusData.Count} status records and {accessData.Count} access records");

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