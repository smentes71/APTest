using System.ComponentModel.DataAnnotations;

namespace RaspberryPiControl.Models
{
    public class Rule
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [Display(Name = "Rule Name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Device")]
        public string DeviceId { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Command")]
        public CommandType Command { get; set; }

        [Required]
        [Display(Name = "Trigger Type")]
        public TriggerType TriggerType { get; set; }

        [Display(Name = "IP Address")]
        public string? IpAddress { get; set; }

        [Display(Name = "API URL")]
        public string? ApiUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public RuleStatus Status { get; set; } = RuleStatus.Active;
    }

    public enum TriggerType
    {
        IpAddress,
        Api
    }

    public enum RuleStatus
    {
        Active,
        Inactive
    }
}