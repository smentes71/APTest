using RaspberryPiControl.Models;
using RaspberryPiControl.Models;

namespace RaspberryPiControl.Services
{
    public class TaskData
    {
        public List<ScheduledTask> Tasks { get; set; } = new List<ScheduledTask>();
    }
}