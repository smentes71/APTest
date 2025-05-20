using Microsoft.AspNetCore.Mvc;
using RaspberryPiControl.Models;
using RaspberryPiControl.Services;
using System.Text.Json;

namespace RaspberryPiControl.Controllers
{
    public class DevicesController : Controller
    {
        private readonly IRaspberryPiService _piService;
        private readonly ILogger<DevicesController> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public DevicesController(
            IRaspberryPiService piService,
            ILogger<DevicesController> logger,
            IWebHostEnvironment webHostEnvironment)
        {
            _piService = piService;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<IActionResult> Index()
        {
            var devices = await _piService.GetAllDevicesAsync();
            return View(devices);
        }

        public IActionResult Add()
        {
            var device = new RaspberryPi
            {
                Id = Guid.NewGuid().ToString("N"),
                Status = PiStatus.Offline
            };
            return View(device);
        }

        [HttpPost]
        public async Task<IActionResult> Add([Bind("Id,Name,IpAddress,Status")] RaspberryPi device)
        {
            // Remove Id validation from ModelState since we'll set it ourselves
            ModelState.Remove("Id");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid: {Errors}",
                    string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)));
                return View(device);
            }

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

                // Ensure device has an Id
                if (string.IsNullOrEmpty(device.Id))
                {
                    device.Id = Guid.NewGuid().ToString("N");
                }

                var newDevice = new DeviceInfo
                {
                    Id = device.Id,
                    Name = device.Name,
                    IpAddress = device.IpAddress,
                    Status = device.Status.ToString()
                };

                deviceData.Devices.Add(newDevice);

                string updatedJson = JsonSerializer.Serialize(deviceData, options);
                await System.IO.File.WriteAllTextAsync(jsonPath, updatedJson);

                TempData["SuccessMessage"] = "Device added successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding device");
                ModelState.AddModelError("", "An error occurred while adding the device.");
                return View(device);
            }
        }

        public async Task<IActionResult> Edit(string id)
        {
            var device = await _piService.GetDeviceByIdAsync(id);
            if (device == null)
            {
                return NotFound();
            }
            return View(device);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(string id, [Bind("Id,Name,IpAddress,Status,Location,Group")] RaspberryPi device)
        {
            if (!ModelState.IsValid)
            {
                return View(device);
            }

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

                var existingDevice = deviceData.Devices.FirstOrDefault(d => d.Id == id);
                if (existingDevice == null)
                {
                    return NotFound();
                }

                existingDevice.Name = device.Name;
                existingDevice.IpAddress = device.IpAddress;
                //existingDevice.Status = device.Status.ToString();
                existingDevice.Group = device.Group;
                existingDevice.Location = device.Location;

                string updatedJson = JsonSerializer.Serialize(deviceData, options);
                await System.IO.File.WriteAllTextAsync(jsonPath, updatedJson);

                TempData["SuccessMessage"] = "Device updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating device");
                ModelState.AddModelError("", "An error occurred while updating the device.");
                return View(device);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
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

                var device = deviceData.Devices.FirstOrDefault(d => d.Id == id);
                if (device == null)
                {
                    return NotFound();
                }

                deviceData.Devices.Remove(device);

                string updatedJson = JsonSerializer.Serialize(deviceData, options);
                await System.IO.File.WriteAllTextAsync(jsonPath, updatedJson);

                return Json(new { success = true, message = "Device deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting device");
                return Json(new { success = false, message = "An error occurred while deleting the device." });
            }
        }
    }
}