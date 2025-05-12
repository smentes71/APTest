using System.Text.Json;
using System.Text.Json.Serialization;
using RaspberryPiControl.Models;

namespace RaspberryPiControl.Services
{
    public class RaspberryPiService : IRaspberryPiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<RaspberryPiService> _logger;

        public RaspberryPiService(
            HttpClient httpClient,
            IConfiguration configuration,
            IWebHostEnvironment webHostEnvironment,
            ILogger<RaspberryPiService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        public async Task<IEnumerable<RaspberryPi>> GetAllDevicesAsync()
        {
            try
            {
                string jsonPath = Path.Combine(_webHostEnvironment.ContentRootPath, "devicedata.json");
                _logger.LogInformation($"Reading device data from: {jsonPath}");

                if (!File.Exists(jsonPath))
                {
                    _logger.LogWarning("devicedata.json file not found");
                    return Enumerable.Empty<RaspberryPi>();
                }

                string jsonContent = await File.ReadAllTextAsync(jsonPath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true
                };

                if (string.IsNullOrWhiteSpace(jsonContent))
                {
                    var initialData = new DeviceData { Devices = new List<DeviceInfo>() };
                    jsonContent = JsonSerializer.Serialize(initialData, options);
                    await File.WriteAllTextAsync(jsonPath, jsonContent);
                }

                var deviceData = JsonSerializer.Deserialize<DeviceData>(jsonContent, options);

                if (deviceData == null)
                {
                    _logger.LogError("Failed to deserialize device data");
                    return Enumerable.Empty<RaspberryPi>();
                }

                if (deviceData.Devices == null)
                {
                    deviceData.Devices = new List<DeviceInfo>();
                }

                var devices = deviceData.Devices.Select(d => new RaspberryPi
                {
                    Id = d.Id,
                    Name = d.Name,
                    Status = Enum.TryParse<PiStatus>(d.Status, true, out var status) ? status : PiStatus.Offline,
                    IpAddress = d.IpAddress,
                    AccessStatus = Enum.TryParse<AccessStatus>(d.AccessStatus, true, out var accessStatus) ? accessStatus : AccessStatus.Closed,
                    Location = d.Location, // Location alan? eklenmeli
                    Group = d.Group // Group alan? eklenmeli
                }).ToList();

                _logger.LogInformation($"Successfully mapped {devices.Count} devices");
                return devices;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading device data: {Message}", ex.Message);
                return Enumerable.Empty<RaspberryPi>();
            }
        }


        public async Task<bool> SendCommandAsync(string ipAddress, CommandType command)
        {
            try
            {
                string endpoint = command == CommandType.Shutdown ? "off" : "restart";
                var response = await _httpClient.PostAsync($"http://{ipAddress}/{endpoint}", null);

                _logger.LogInformation($"Sent {command} command to {ipAddress}, Status: {response.StatusCode}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending command to device {IpAddress}", ipAddress);
                return false;
            }
        }



        /*public async Task<bool> SendCommandAsync(string piId, CommandType command)
        {
            try
            {
                var apiEndpoint = _configuration["ApiSettings:BaseUrl"];
                var response = await _httpClient.PostAsJsonAsync($"{apiEndpoint}/command", new CommandModel
                {
                    Type = command,
                    Target = piId
                });

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending command  to device {PiId}", piId);
                return false;
            }
        }*/

        public async Task<RaspberryPi> GetDeviceByIdAsync(string id)
        {
            var devices = await GetAllDevicesAsync();
            return devices.FirstOrDefault(d => d.Id == id);
        }
    }

    public class DeviceData
    {
        public List<DeviceInfo> Devices { get; set; } = new List<DeviceInfo>();
    }

    public class DeviceInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string AccessStatus { get; set; } = "Closed";
        public DateTime? LastUpdate { get; set; }
       
        public string? Group { get; set; } 

        
        public string? Location { get; set; } 
    }
}