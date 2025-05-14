using MongoDB.Driver;
using RaspberryPiControl.Models;

namespace RaspberryPiControl.Services
{
    public class MongoDbService
    {
        private readonly IMongoCollection<DeviceStatusHistory> _statusHistory;
        private readonly ILogger<MongoDbService> _logger;
        private readonly IMongoClient _client;
        private readonly string _databaseName;

        public MongoDbService(IConfiguration configuration, ILogger<MongoDbService> logger)
        {
            _logger = logger;
            try
            {
                var connectionString = configuration.GetValue<string>("MongoDB:ConnectionString");
                _databaseName = configuration.GetValue<string>("MongoDB:DatabaseName");

                if (string.IsNullOrEmpty(connectionString) || string.IsNullOrEmpty(_databaseName))
                {
                    throw new ArgumentException("MongoDB bağlantı dizesi veya veritabanı adı yapılandırılmamış");
                }

                var settings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));
                settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
                
                _client = new MongoClient(settings);
                var database = _client.GetDatabase(_databaseName);
                _statusHistory = database.GetCollection<DeviceStatusHistory>("DeviceStatusHistory");

                // Bağlantıyı doğrula
                database.RunCommand<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("ping", 1));
                _logger.LogInformation("MongoDB'ye başarıyla bağlanıldı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MongoDB bağlantısı başlatılamadı");
                throw;
            }
        }

        public async Task AddStatusHistoryAsync(DeviceStatusHistory history)
        {
            try
            {
                await _statusHistory.InsertOneAsync(history);
                _logger.LogInformation($"{history.DeviceId} cihazı için durum geçmişi eklendi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{history.DeviceId} cihazı için durum geçmişi eklenemedi");
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
                _logger.LogError(ex, $"{deviceId} cihazı için geçmiş alınamadı");
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

                var result = await _statusHistory.Find(filter)
                    .Sort(Builders<DeviceStatusHistory>.Sort.Descending(x => x.Timestamp))
                    .ToListAsync();

                _logger.LogInformation($"{result.Count} adet geçmiş kaydı alındı");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tüm geçmiş alınamadı");
                throw;
            }
        }

        public async Task<bool> IsConnected()
        {
            try
            {
                await _client.GetDatabase(_databaseName).RunCommandAsync<MongoDB.Bson.BsonDocument>(
                    new MongoDB.Bson.BsonDocument("ping", 1));
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MongoDB bağlantı kontrolü başarısız");
                return false;
            }
        }

        public async Task<bool> EnsureCollectionExists()
        {
            try
            {
                var database = _client.GetDatabase(_databaseName);
                var collections = await (await database.ListCollectionNamesAsync()).ToListAsync();
                
                if (!collections.Contains("DeviceStatusHistory"))
                {
                    await database.CreateCollectionAsync("DeviceStatusHistory");
                    _logger.LogInformation("DeviceStatusHistory koleksiyonu oluşturuldu");
                }
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Koleksiyon kontrolü başarısız");
                return false;
            }
        }
    }
}