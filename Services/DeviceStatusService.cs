using System.Text.Json;
using RaspberryPiControl.Models;

namespace RaspberryPiControl.Services
{
    public class DeviceStatusService : BackgroundService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<DeviceStatusService> _logger;
        private readonly MongoDbService _mongoDbService;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);
        private readonly TimeSpan _offlineThreshold = TimeSpan.FromMinutes(5);

        public DeviceStatusService(
            IWebHostEnvironment webHostEnvironment,
            ILogger<DeviceStatusService> logger,
            MongoDbService mongoDbService)
        {
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _mongoDbService = mongoDbService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckDeviceStatuses();
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Cihaz durumları kontrol edilirken hata oluştu");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
        }

        private async Task CheckDeviceStatuses()
        {
            string jsonPath = Path.Combine(_webHostEnvironment.ContentRootPath, "devicedata.json");
            if (!File.Exists(jsonPath))
            {
                return;
            }

            string jsonContent = await File.ReadAllTextAsync(jsonPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            var deviceData = JsonSerializer.Deserialize<DeviceData>(jsonContent, options);
            bool hasChanges = false;

            if (deviceData != null)
            {
                foreach (var device in deviceData.Devices)
                {
                    if (device.LastUpdate.HasValue &&
                        DateTime.UtcNow - device.LastUpdate.Value > _offlineThreshold &&
                        device.Status == "Online")
                    {
                        device.Status = "Offline";
                        hasChanges = true;
                        _logger.LogInformation($"Cihaz {device.IpAddress} aktivite olmadığı için çevrimdışı olarak işaretlendi");

                        // MongoDB'ye durum değişikliğini kaydet
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
                }

                if (hasChanges)
                {
                    string updatedJson = JsonSerializer.Serialize(deviceData, options);
                    await File.WriteAllTextAsync(jsonPath, updatedJson);
                }
            }
        }
    }
}