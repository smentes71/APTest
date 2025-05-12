using System.Text.Json.Serialization;

namespace RaspberryPiControl.Services
{
    public class DeviceInfo2
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        [JsonPropertyName("ipAddress")]
        public string IpAddress { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        [JsonPropertyName("lastUpdate")]
        public DateTime? LastUpdate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;


    }
}