using MongoDB.Driver;
using RaspberryPiControl.Models;

namespace RaspberryPiControl.Services
{
    public class MongoDbService
    {
        private readonly IMongoCollection<DeviceStatusHistory> _statusHistory;

        public MongoDbService(IConfiguration configuration)
        {
            var connectionString = configuration.GetValue<string>("MongoDB:ConnectionString");
            var databaseName = configuration.GetValue<string>("MongoDB:DatabaseName");
            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _statusHistory = database.GetCollection<DeviceStatusHistory>("DeviceStatusHistory");
        }

        public async Task AddStatusHistoryAsync(DeviceStatusHistory history)
        {
            await _statusHistory.InsertOneAsync(history);
        }

        public async Task<IEnumerable<DeviceStatusHistory>> GetDeviceHistoryAsync(string deviceId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var builder = Builders<DeviceStatusHistory>.Filter;
            var filter = builder.Eq(x => x.DeviceId, deviceId);

            if (startDate.HasValue)
                filter = filter & builder.Gte(x => x.Timestamp, startDate.Value);
            if (endDate.HasValue)
                filter = filter & builder.Lte(x => x.Timestamp, endDate.Value);

            return await _statusHistory.Find(filter)
                .Sort(Builders<DeviceStatusHistory>.Sort.Descending(x => x.Timestamp))
                .ToListAsync();
        }

        public async Task<IEnumerable<DeviceStatusHistory>> GetAllHistoryAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var builder = Builders<DeviceStatusHistory>.Filter;
            var filter = builder.Empty;

            if (startDate.HasValue)
                filter = filter & builder.Gte(x => x.Timestamp, startDate.Value);
            if (endDate.HasValue)
                filter = filter & builder.Lte(x => x.Timestamp, endDate.Value);

            return await _statusHistory.Find(filter)
                .Sort(Builders<DeviceStatusHistory>.Sort.Descending(x => x.Timestamp))
                .ToListAsync();
        }
    }
}