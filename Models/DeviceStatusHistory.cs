using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RaspberryPiControl.Models
{
    public class DeviceStatusHistory
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string IpAddress { get; set; }
        public string Status { get; set; }
        public string AccessStatus { get; set; }
        public DateTime Timestamp { get; set; }
        public string Location { get; set; }
        public string Group { get; set; }
    }
}