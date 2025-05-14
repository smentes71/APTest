using Microsoft.AspNetCore.Mvc;
using RaspberryPiControl.Models;
using RaspberryPiControl.Services;

namespace RaspberryPiControl.Controllers
{
    public class HistoryController : Controller
    {
        private readonly MongoDbService _mongoDbService;
        private readonly ILogger<HistoryController> _logger;

        public HistoryController(MongoDbService mongoDbService, ILogger<HistoryController> logger)
        {
            _mongoDbService = mongoDbService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string deviceId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                if (!await _mongoDbService.IsConnected())
                {
                    TempData["ErrorMessage"] = "Veritabanına bağlanılamadı. Lütfen MongoDB'nin çalıştığından emin olun.";
                    return View(Enumerable.Empty<DeviceStatusHistory>());
                }

                await _mongoDbService.EnsureCollectionExists();

                var history = deviceId != null 
                    ? await _mongoDbService.GetDeviceHistoryAsync(deviceId, startDate, endDate)
                    : await _mongoDbService.GetAllHistoryAsync(startDate, endDate);

                return View(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cihaz geçmişi alınırken hata oluştu");
                TempData["ErrorMessage"] = "Cihaz geçmişi alınamadı. Lütfen daha sonra tekrar deneyin.";
                return View(Enumerable.Empty<DeviceStatusHistory>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> ViewCollection()
        {
            try
            {
                if (!await _mongoDbService.IsConnected())
                {
                    return Json(new { 
                        success = false, 
                        error = "MongoDB'ye bağlanılamadı. Lütfen servisin çalıştığından emin olun."
                    });
                }

                await _mongoDbService.EnsureCollectionExists();
                var allHistory = await _mongoDbService.GetAllHistoryAsync();
                
                return Json(new { 
                    success = true, 
                    data = allHistory,
                    count = allHistory.Count()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Koleksiyon verisi alınırken hata oluştu");
                return Json(new { 
                    success = false, 
                    error = "Koleksiyon verisi alınamadı",
                    details = ex.Message
                });
            }
        }
    }
}