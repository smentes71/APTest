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

                history = history.OrderBy(h => h.Timestamp).ToList();

                var statusChanges = history.GroupBy(h => new { 
                    Date = h.Timestamp.Date,
                    Status = h.Status.ToLower()
                })
                .Select(g => new {
                    g.Key.Date,
                    g.Key.Status,
                    Count = g.Count()
                })
                .ToList();

                var accessChanges = history.GroupBy(h => new {
                    Date = h.Timestamp.Date,
                    AccessStatus = (h.AccessStatus ?? "").ToLower()
                })
                .Select(g => new {
                    g.Key.Date,
                    g.Key.AccessStatus,
                    Count = g.Count()
                })
                .ToList();

                var dateRange = Enumerable.Range(0, (endDate.Value - startDate.Value).Days + 1)
                    .Select(offset => startDate.Value.AddDays(offset).Date)
                    .ToList();

                var statusData = dateRange.Select(date => new {
                    Date = date.ToString("dd/MM/yyyy"),
                    Online = statusChanges.FirstOrDefault(s => s.Date == date && s.Status == "online")?.Count ?? 0,
                    Offline = statusChanges.FirstOrDefault(s => s.Date == date && s.Status == "offline")?.Count ?? 0
                }).ToList();

                var accessData = dateRange.Select(date => new {
                    Date = date.ToString("dd/MM/yyyy"),
                    Open = accessChanges.FirstOrDefault(a => a.Date == date && a.AccessStatus == "open")?.Count ?? 0,
                    Closed = accessChanges.FirstOrDefault(a => a.Date == date && a.AccessStatus == "closed")?.Count ?? 0
                }).ToList();

                _logger.LogInformation($"Retrieved chart data: Status changes: {statusChanges.Count}, Access changes: {accessChanges.Count}");

                return Json(new { statusData, accessData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chart data");
                return Json(new { error = "Grafik verisi alınırken hata oluştu" });
            }
        }
    }
}