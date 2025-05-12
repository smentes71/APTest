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

        public ApiController(
            IWebHostEnvironment webHostEnvironment,
            ILogger<ApiController> logger)
        {
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
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
                    device.Status = request.Status.ToLower() == "online" ? "Online" : "Offline";
                    device.AccessStatus = request.AccessStatus.ToLower() == "open" ? "Open" :
                      request.AccessStatus.ToLower() == "" ? "" :
                      "Closed";

                    
                    device.LastUpdate = DateTime.UtcNow;

                      string updatedJson = JsonSerializer.Serialize(deviceData, options);
                      await System.IO.File.WriteAllTextAsync(jsonPath, updatedJson);

                      _logger.LogInformation($"Updated status for device {request.IpAddress} to {request.Status}");
                      return Ok(new { success = true });
                  }

                  return NotFound(new { success = false, message = "Device not found" });
              }
              catch (Exception ex)
              {
                  _logger.LogError(ex, "Error updating device status");
                  return StatusCode(500, new { success = false, message = "Internal server error" });
              }
          }

       /* [HttpPost]

        public async Task<IActionResult> UpdateRelayStatus([FromBody] StatusUpdateRequest request)
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
                    device.AccessStatus = request.AccessStatus.ToLower() == "open" ? "Open" : "Closed";
                    device.LastUpdate = DateTime.UtcNow;

                    string updatedJson = JsonSerializer.Serialize(deviceData, options);
                    await System.IO.File.WriteAllTextAsync(jsonPath, updatedJson);

                    _logger.LogInformation($"Updated Relay status for device {request.IpAddress} to {request.Status}");
                    return Ok(new { success = true });
                }

                return NotFound(new { success = false, message = "Device not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating Relay  device status");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }*/
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
    
