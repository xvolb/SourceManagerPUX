using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SourceManagerPUX.Models;
using SourceManagerPUX.Services;

namespace SourceManagerPUX.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DirectoryAnalyzer _directoryAnalyzer;

        public HomeController(ILogger<HomeController> logger, DirectoryAnalyzer directoryAnalyzer)
        {
            _logger = logger;
            _directoryAnalyzer = directoryAnalyzer;
        }

        public IActionResult Index()
        {
            return View();
        }
        [HttpPost]
        public IActionResult AnalyzeDirectory(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                ViewBag.Error = "This directory does not exist on machine";
                return View("Index");
            }
            var result = _directoryAnalyzer.AnalyzeDirectory(directoryPath);
            ViewBag.Result = result;
            return View("Index");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
