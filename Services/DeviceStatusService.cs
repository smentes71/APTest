using System.Text.Json;
using RaspberryPiControl.Models;
using Supabase;

namespace RaspberryPiControl.Services
{
    public class DeviceStatusService : BackgroundService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<DeviceStatusService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);
        private readonly TimeSpan _offlineThreshold = TimeSpan.FromMinutes(5);
        private readonly Client _supabaseClient;

        public DeviceStatusService(
            IWebHostEnvironment webHostEnvironment,
            ILogger<DeviceStatusService> logger,
            Client supabaseClient)
        {
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
            _supabaseClient = supabaseClient;
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
                    _logger.LogError(ex, "Error checking device statuses");
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
                        _logger.LogInformation($"Device {device.IpAddress} marked as offline due to inactivity");

                        // Log status change to Supabase
                        await LogDeviceStatus(device);
                    }
                }

                if (hasChanges)
                {
                    string updatedJson = JsonSerializer.Serialize(deviceData, options);
                    await File.WriteAllTextAsync(jsonPath, updatedJson);
                }
            }
        }

        private async Task LogDeviceStatus(DeviceInfo device)
        {
            try
            {
                await _supabaseClient.From<DeviceStatusLog>()
                    .Insert(new DeviceStatusLog
                    {
                        DeviceId = device.Id,
                        Status = device.Status,
                        AccessStatus = device.AccessStatus,
                        IpAddress = device.IpAddress,
                        Location = device.Location,
                        Group = device.Group
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging device status to Supabase");
            }
        }
    }

    public class DeviceStatusLog
    {
        public string DeviceId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string AccessStatus { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? Group { get; set; }
    }
}