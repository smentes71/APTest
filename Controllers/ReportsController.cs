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

                var statusData = history
                    .GroupBy(h => new { Date = h.Timestamp.Date, h.Status })
                    .Select(g => new
                    {
                        Date = g.Key.Date,
                        Status = g.Key.Status,
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Date)
                    .ToList();

                var accessData = history
                    .GroupBy(h => new { Date = h.Timestamp.Date, h.AccessStatus })
                    .Select(g => new
                    {
                        Date = g.Key.Date,
                        Status = g.Key.AccessStatus,
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Date)
                    .ToList();

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