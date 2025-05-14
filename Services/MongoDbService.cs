using MongoDB.Driver;
using RaspberryPiControl.Models;

namespace RaspberryPiControl.Services
{
    public class MongoDbService
    {
        private readonly IMongoCollection<DeviceStatusHistory> _statusHistory;
        private readonly ILogger<MongoDbService> _logger;

        public MongoDbService(IConfiguration configuration, ILogger<MongoDbService> logger)
        {
            _logger = logger;
            try
            {
                var connectionString = configuration.GetValue<string>("MongoDB:ConnectionString");
                var databaseName = configuration.GetValue<string>("MongoDB:DatabaseName");

                if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(databaseName))
                {
                    throw new ArgumentException("MongoDB connection string or database name is not configured");
                }

                var client = new MongoClient(connectionString);
                var database = client.GetDatabase(databaseName);
                _statusHistory = database.GetCollection<DeviceStatusHistory>("DeviceStatusHistory");
                
                // Create indexes
                var indexKeysDefinition = Builders<DeviceStatusHistory>.IndexKeys.Descending(x => x.Timestamp);
                var indexOptions = new CreateIndexOptions { Name = "TimestampIndex" };
                var indexModel = new CreateIndexModel<DeviceStatusHistory>(indexKeysDefinition, indexOptions);
                _statusHistory.Indexes.CreateOne(indexModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize MongoDB connection");
                throw;
            }
        }

        public async Task AddStatusHistoryAsync(DeviceStatusHistory history)
        {
            try
            {
                await _statusHistory.InsertOneAsync(history);
                _logger.LogInformation($"Added status history for device {history.DeviceId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to add status history for device {history.DeviceId}");
                throw;
            }
        }

        public async Task<IEnumerable<DeviceStatusHistory>> GetDeviceHistoryAsync(string deviceId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to get history for device {deviceId}");
                throw;
            }
        }

        public async Task<IEnumerable<DeviceStatusHistory>> GetAllHistoryAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all history");
                throw;
            }
        }
    }
}