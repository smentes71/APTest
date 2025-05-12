using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RaspberryPiControl.Models;

namespace RaspberryPiControl.Services
{
    public class TaskSchedulerService : BackgroundService
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TaskSchedulerService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);
        private readonly JsonSerializerOptions _jsonOptions;

        public TaskSchedulerService(
            IWebHostEnvironment webHostEnvironment,
            IServiceScopeFactory scopeFactory,
            ILogger<TaskSchedulerService> logger)
        {
            _webHostEnvironment = webHostEnvironment;
            _scopeFactory = scopeFactory;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndExecuteTasks();
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking scheduled tasks");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
        }

        private async Task CheckAndExecuteTasks()
        {
            string tasksPath = Path.Combine(_webHostEnvironment.ContentRootPath, "taskdata.json");
            if (!File.Exists(tasksPath))
            {
                var initialData = new TaskData { Tasks = new List<ScheduledTask>() };
                await File.WriteAllTextAsync(tasksPath, JsonSerializer.Serialize(initialData, _jsonOptions));
                return;
            }

            string jsonContent = await File.ReadAllTextAsync(tasksPath);
            var taskData = JsonSerializer.Deserialize<TaskData>(jsonContent, _jsonOptions);
            bool hasChanges = false;

            if (taskData != null)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var piService = scope.ServiceProvider.GetRequiredService<IRaspberryPiService>();

                    foreach (var task in taskData.Tasks.Where(t => t.Status == ScheduledTaskStatus.Scheduled))
                    {
                        if (ShouldExecuteTask(task))
                        {
                            try
                            {
                                var device = await piService.GetDeviceByIdAsync(task.DeviceId);
                                if (device != null)
                                {
                                    await piService.SendCommandAsync(device.IpAddress, task.Command);
                                    task.LastExecuted = DateTime.UtcNow;
                                    task.NextExecution = CalculateNextExecution(task);

                                    if (task.ScheduleType == ScheduleType.OneTime)
                                    {
                                        task.Status = ScheduledTaskStatus.Completed;
                                    }

                                    hasChanges = true;
                                    _logger.LogInformation($"Successfully executed task {task.Name} for device {device.Name}");
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Failed to execute task {task.Name}");
                                if (task.ScheduleType == ScheduleType.OneTime)
                                {
                                    task.Status = ScheduledTaskStatus.Failed;
                                    hasChanges = true;
                                }
                            }
                        }
                    }
                }

                if (hasChanges)
                {
                    string updatedJson = JsonSerializer.Serialize(taskData, _jsonOptions);
                    await File.WriteAllTextAsync(tasksPath, updatedJson);
                }
            }
        }

        private bool ShouldExecuteTask(ScheduledTask task)
        {
            var now = DateTime.UtcNow;

            switch (task.ScheduleType)
            {
                case ScheduleType.OneTime:
                    return task.ExecutionDate.HasValue &&
                           task.ExecutionTime.HasValue &&
                           now.Date == task.ExecutionDate.Value.Date &&
                           now.TimeOfDay >= task.ExecutionTime.Value;

                case ScheduleType.Daily:
                    return task.ExecutionTime.HasValue &&
                           now.TimeOfDay >= task.ExecutionTime.Value &&
                           (!task.LastExecuted.HasValue || task.LastExecuted.Value.Date < now.Date);

                case ScheduleType.Weekly:
                    return task.ExecutionTime.HasValue &&
                           task.RepeatDays.Contains(now.DayOfWeek) &&
                           now.TimeOfDay >= task.ExecutionTime.Value &&
                           (!task.LastExecuted.HasValue || task.LastExecuted.Value.Date < now.Date);

                default:
                    return false;
            }
        }

        private DateTime? CalculateNextExecution(ScheduledTask task)
        {
            var now = DateTime.UtcNow;

            switch (task.ScheduleType)
            {
                case ScheduleType.OneTime:
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