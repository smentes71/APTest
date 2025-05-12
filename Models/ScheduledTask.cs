using System.ComponentModel.DataAnnotations;

namespace RaspberryPiControl.Models
{
    public class ScheduledTask
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [Display(Name = "Task Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Device")]
        public string DeviceId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Command")]
        public CommandType Command { get; set; }

        [Required]
        [Display(Name = "Schedule Type")]
        public ScheduleType ScheduleType { get; set; }

        [Display(Name = "Execution Time")]
        public TimeSpan? ExecutionTime { get; set; }

        [Display(Name = "Execution Date")]
        public DateTime? ExecutionDate { get; set; }

        [Display(Name = "Repeat Days")]
        public List<DayOfWeek> RepeatDays { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ScheduledTaskStatus Status { get; set; } = ScheduledTaskStatus.Scheduled;

        public DateTime? LastExecuted { get; set; }

        public DateTime? NextExecution { get; set; }
    }

    public enum ScheduleType
    {
        OneTime,
        Daily,
        Weekly
    }

    public enum ScheduledTaskStatus
    {
        Scheduled,
        Completed,
        Failed,
        Cancelled
    }
}