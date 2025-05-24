using Microsoft.AspNetCore.Mvc;
using RaspberryPiControl.Models;
using RaspberryPiControl.Services;
using System.Text.Json;

namespace RaspberryPiControl.Controllers
{
    public class RulesController : Controller
    {
        private readonly IRaspberryPiService _piService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<RulesController> _logger;

        public RulesController(
            IRaspberryPiService piService,
            IWebHostEnvironment webHostEnvironment,
            ILogger<RulesController> logger)
        {
            _piService = piService;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var rules = await GetAllRulesAsync();
            var devices = await _piService.GetAllDevicesAsync();
            ViewBag.Devices = devices;
            return View(rules);
        }

        public async Task<IActionResult> Add()
        {
            var devices = await _piService.GetAllDevicesAsync();
            ViewBag.Devices = devices;
            return View(new Rule());
        }

        [HttpPost]
        public async Task<IActionResult> Add(Rule rule)
        {
            if (!ModelState.IsValid)
            {
                var devices = await _piService.GetAllDevicesAsync();
                ViewBag.Devices = devices;
                return View(rule);
            }

            try
            {
                string rulesPath = Path.Combine(_webHostEnvironment.ContentRootPath, "ruledata.json");
                var ruleData = await LoadRuleDataAsync(rulesPath);

                ruleData.Rules.Add(rule);
                await SaveRuleDataAsync(rulesPath, ruleData);

                TempData["SuccessMessage"] = "Rule added successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding rule");
                ModelState.AddModelError("", "An error occurred while adding the rule.");
                var devices = await _piService.GetAllDevicesAsync();
                ViewBag.Devices = devices;
                return View(rule);
            }
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            try
            {
                string rulesPath = Path.Combine(_webHostEnvironment.ContentRootPath, "ruledata.json");
                var ruleData = await LoadRuleDataAsync(rulesPath);

                var rule = ruleData.Rules.FirstOrDefault(r => r.Id == id);
                if (rule == null)
                {
                    return NotFound();
                }

                rule.Status = rule.Status == RuleStatus.Active ? RuleStatus.Inactive : RuleStatus.Active;
                await SaveRuleDataAsync(rulesPath, ruleData);

                return Json(new { success = true, message = "Rule status updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating rule status");
                return Json(new { success = false, message = "Failed to update rule status." });
            }
        }

        private async Task<List<Rule>> GetAllRulesAsync()
        {
            try
            {
                string rulesPath = Path.Combine(_webHostEnvironment.ContentRootPath, "ruledata.json");
                var ruleData = await LoadRuleDataAsync(rulesPath);
                return ruleData.Rules;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting rules");
                return new List<Rule>();
            }
        }

        private async Task<RuleData> LoadRuleDataAsync(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                return new RuleData();
            }

            string jsonContent = await System.IO.File.ReadAllTextAsync(path);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            return JsonSerializer.Deserialize<RuleData>(jsonContent, options) ?? new RuleData();
        }

        private async Task SaveRuleDataAsync(string path, RuleData ruleData)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            };

            string jsonContent = JsonSerializer.Serialize(ruleData, options);
            await System.IO.File.WriteAllTextAsync(path, jsonContent);
        }
    }

    public class RuleData
    {
        public List<Rule> Rules { get; set; } = new List<Rule>();
    }
}