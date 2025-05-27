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
                else
                    endDate = endDate.Value.Date.AddDays(1).AddTicks(-1); // günü 23:59:59.9999999 yapar

                var history = deviceId != null
                    ? await _mongoDbService.GetDeviceHistoryAsync(deviceId, startDate, endDate)
                    : await _mongoDbService.GetAllHistoryAsync(startDate, endDate);

                history = history.OrderBy(h => h.Timestamp).ToList();

                var dateRange = Enumerable.Range(0, (endDate.Value - startDate.Value).Days + 1)
                    .Select(offset => startDate.Value.AddDays(offset).Date)
                    .ToList();

                var result = dateRange.Select(date =>
                {
                    var dayHistory = history.Where(h => h.Timestamp.Date == date);
                    var onlineDevices = dayHistory.Where(h => h.Status.ToLower() == "online")
                        .Select(h => new { name = h.DeviceName, ipAddress = h.IpAddress, location = h.Location, group = h.Group })
                        
                        .ToList();
                    var offlineDevices = dayHistory.Where(h => h.Status.ToLower() == "offline")
                        .Select(h => new { name = h.DeviceName, ipAddress = h.IpAddress, location = h.Location, group = h.Group })
                        
                        .ToList();
                    var openDevices = dayHistory.Where(h => h.AccessStatus?.ToLower() == "open")
                        .Select(h => new { name = h.DeviceName, ipAddress = h.IpAddress, location = h.Location, group = h.Group })
                        
                        .ToList();
                    var closedDevices = dayHistory.Where(h => h.AccessStatus?.ToLower() == "closed")
                        .Select(h => new { name = h.DeviceName, ipAddress = h.IpAddress, location = h.Location, group = h.Group })
                        
                        .ToList();

                    return new
                    {
                        date = date.ToString("dd/MM/yyyy"),
                        online = onlineDevices.Count,
                        offline = offlineDevices.Count,
                        devices = new
                        {
                            online = onlineDevices,
                            offline = offlineDevices,
                            open = openDevices,
                            closed = closedDevices
                        }
                    };
                }).ToList();

                var accessData = dateRange.Select(date =>
                {
                    var dayHistory = history.Where(h => h.Timestamp.Date == date);
                    return new
                    {
                        date = date.ToString("dd/MM/yyyy"),
                        open = dayHistory.Count(h => h.AccessStatus?.ToLower() == "open"),
                        closed = dayHistory.Count(h => h.AccessStatus?.ToLower() == "closed")
                    };
                }).ToList();

                return Json(new { statusData = result, accessData });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chart data");
                return Json(new { error = "Grafik verisi alınırken hata oluştu" });
            }
        }
    }
}