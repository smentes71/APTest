using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace RaspberryPiControl.Models
{
    public class RaspberryPi
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Device name is required")]
        [Display(Name = "Device Name")]
        public string Name { get; set; } = string.Empty;

        public PiStatus Status { get; set; }

        [Required(ErrorMessage = "IP Address is required")]
        [Display(Name = "IP Address")]
        [RegularExpression(@"^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$", ErrorMessage = "Please enter a valid IP address")]
        public string IpAddress { get; set; } = string.Empty;
        [JsonPropertyName("Group")]
        public string Group { get; set; } = string.Empty;

        [JsonPropertyName("Location")]
        public string Location { get; set; } = string.Empty;
        public AccessStatus AccessStatus { get; set; }
       

       

    }

    public enum PiStatus
    {
        Online,
        Offline
    }

    public enum AccessStatus
    {
        Open,
        Closed
    }
}