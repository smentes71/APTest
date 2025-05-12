using Microsoft.AspNetCore.Mvc;
using RaspberryPiControl.Models;
using RaspberryPiControl.Services;
using System.Text.Json;

namespace RaspberryPiControl.Controllers
{
    public class TasksController : Controller
    {
        private readonly IRaspberryPiService _piService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<TasksController> _logger;

        public TasksController(
            IRaspberryPiService piService,
            IWebHostEnvironment webHostEnvironment,
            ILogger<TasksController> logger)
        {
            _piService = piService;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var tasks = await GetAllTasksAsync();
            var devices = await _piService.GetAllDevicesAsync();
            ViewBag.Devices = devices;
            return View(tasks);
        }

        public async Task<IActionResult> Add()
        {
            var devices = await _piService.GetAllDevicesAsync();
            ViewBag.Devices = devices;
            var task = new ScheduledTask
            {
                ExecutionDate = DateTime.Today, // Set default execution date to today
                ExecutionTime = DateTime.Now.TimeOfDay // Set default execution time to current time
            };
            return View(task);
        }

        [HttpPost]
        public async Task<IActionResult> Add(ScheduledTask task)
        {
            if (!ModelState.IsValid)
            {
                var devices = await _piService.GetAllDevicesAsync();
                ViewBag.Devices = devices;
                return View(task);
            }

            try
            {
                string tasksPath = Path.Combine(_webHostEnvironment.ContentRootPath, "taskdata.json");
                var taskData = await LoadTaskDataAsync(tasksPath);

                task.Id = Guid.NewGuid().ToString();
                task.CreatedAt = DateTime.UtcNow;
                task.Status = ScheduledTaskStatus.Scheduled;

                // Ensure ExecutionDate is set based on ScheduleType
                switch (task.ScheduleType)
                {
                    case ScheduleType.OneTime:
                        if (!task.ExecutionDate.HasValue)
                        {
                            task.ExecutionDate = DateTime.Today;
                        }
                        break;
                    case ScheduleType.Daily:
                    case ScheduleType.Weekly:
                        task.ExecutionDate = DateTime.Today;
                        break;
                }

                // Ensure ExecutionTime is set
                if (!task.ExecutionTime.HasValue)
                {
                    task.ExecutionTime = DateTime.Now.TimeOfDay;
                }

                task.NextExecution = CalculateNextExecution(task);

                taskData.Tasks.Add(task);
                await SaveTaskDataAsync(tasksPath, taskData);

                TempData["SuccessMessage"] = "Task added successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding task");
                ModelState.AddModelError("", "An error occurred while adding the task.");
                var devices = await _piService.GetAllDevicesAsync();
                ViewBag.Devices = devices;
                return View(task);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(string id)
        {
            try
            {
                string tasksPath = Path.Combine(_webHostEnvironment.ContentRootPath, "taskdata.json");
                var taskData = await LoadTaskDataAsync(tasksPath);

                var task = taskData.Tasks.FirstOrDefault(t => t.Id == id);
                if (task == null)
                {
                    return NotFound();
                }

                task.Status = ScheduledTaskStatus.Cancelled;
                await SaveTaskDataAsync(tasksPath, taskData);

                return Json(new { success = true, message = "Task cancelled successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling task");
                return Json(new { success = false, message = "Failed to cancel task." });
            }
        }

        private async Task<List<ScheduledTask>> GetAllTasksAsync()
        {
            try
            {
                string tasksPath = Path.Combine(_webHostEnvironment.ContentRootPath, "taskdata.json");
                var taskData = await LoadTaskDataAsync(tasksPath);
                return taskData.Tasks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting tasks");
                return new List<ScheduledTask>();
            }
        }

        private async Task<TaskData> LoadTaskDataAsync(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                return new TaskData();
            }

            string jsonContent = await System.IO.File.ReadAllTextAsync(path);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            return JsonSerializer.Deserialize<TaskData>(jsonContent, options) ?? new TaskData();
        }

        private async Task SaveTaskDataAsync(string path, TaskData taskData)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            string jsonContent = JsonSerializer.Serialize(taskData, options);
            await System.IO.File.WriteAllTextAsync(path, jsonContent);
        }

        private DateTime? CalculateNextExecution(ScheduledTask task)
        {
            var now = DateTime.UtcNow;

            switch (task.ScheduleType)
            {
                case ScheduleType.OneTime:
                    if (task.ExecutionDate.HasValue && task.ExecutionTime.HasValue)
                    {
                        return task.ExecutionDate.Value.Date.Add(task.ExecutionTime.Value);
                    }
                    return null;

                case ScheduleType.Daily:
                    if (task.ExecutionTime.HasValue)
                    {
                        var next = now.Date.Add(task.ExecutionTime.Value);
                        if (next <= now)
                        {
                            next = next.AddDays(1);
                        }
                        return next;
                    }
                    return null;

                case ScheduleType.Weekly:
                    if (task.ExecutionTime.HasValue && task.RepeatDays.Any())
                    {
                        var next = now.Date.Add(task.ExecutionTime.Value);
                        while (!task.RepeatDays.Contains(next.DayOfWeek) || next <= now)
                        {
                            next = next.AddDays(1);
                        }
                        return next;
                    }
                    return null;

                default:
                    return null;
            }
        }
    }
}