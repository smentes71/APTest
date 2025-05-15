using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using RaspberryPiControl.Models;
using RaspberryPiControl.Services;

namespace RaspberryPiControl.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApiController : ControllerBase
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<ApiController> _logger;
        private readonly MongoDbService _mongoDbService;

        public ApiController(
            IWebHostEnvironment webHostEnvironment,
            ILogger<ApiController> logger,
            MongoDbService mongoDbService)
        {
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _mongoDbService = mongoDbService;
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus([FromBody] StatusUpdateRequest request)
        {
            try
            {
                string jsonPath = Path.Combine(_webHostEnvironment.ContentRootPath, "devicedata.json");
                string jsonContent = await System.IO.File.ReadAllTextAsync(jsonPath);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };

                var deviceData = JsonSerializer.Deserialize<DeviceData>(jsonContent, options) ?? new DeviceData();

                var device = deviceData.Devices.FirstOrDefault(d => d.IpAddress.Equals(request.IpAddress, StringComparison.OrdinalIgnoreCase));
                if (device != null)
                {
                    // Önceki durumları kaydet
                    var previousStatus = device.Status;
                    var previousAccessStatus = device.AccessStatus;

                    // Yeni durumları güncelle
                    device.Status = request.Status.ToLower() == "online" ? "Online" : "Offline";
                    device.AccessStatus = request.AccessStatus.ToLower() == "open" ? "Open" :
                        request.AccessStatus.ToLower() == "" ? "" :
                        "Closed";
                    device.LastUpdate = DateTime.UtcNow;

                    // JSON dosyasını güncelle
                    string updatedJson = JsonSerializer.Serialize(deviceData, options);
                    await System.IO.File.WriteAllTextAsync(jsonPath, updatedJson);

                    // Durum değişikliğini MongoDB'ye kaydet
                    if (device.Status != previousStatus || device.AccessStatus != previousAccessStatus)
                    {
                        var history = new DeviceStatusHistory
                        {
                            DeviceId = device.Id,
                            DeviceName = device.Name,
                            IpAddress = device.IpAddress,
                            Status = device.Status,
                            AccessStatus = device.AccessStatus,
                            Timestamp = DateTime.UtcNow,
                            Location = device.Location,
                            Group = device.Group
                        };

                        try
                        {
                            await _mongoDbService.AddStatusHistoryAsync(history);
                            _logger.LogInformation($"Durum değişikliği MongoDB'ye kaydedildi: {device.Name}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Durum değişikliği MongoDB'ye kaydedilemedi: {device.Name}");
                        }
                    }

                    _logger.LogInformation($"Cihaz {request.IpAddress} durumu güncellendi: {request.Status}");
                    return Ok(new { success = true });
                }

                return NotFound(new { success = false, message = "Cihaz bulunamadı" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cihaz durumu güncellenirken hata oluştu");
                return StatusCode(500, new { success = false, message = "Sunucu hatası" });
            }
        }
    }

    public class StatusUpdateRequest
    {
        public string IpAddress { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string AccessStatus { get; set; } = string.Empty;
        public DateTime? LastUpdate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
    }
}