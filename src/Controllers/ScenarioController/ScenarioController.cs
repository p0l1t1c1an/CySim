using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CySim.Data;
using CySim.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using CySim.Models.Scenario;
using CySim.Interfaces;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CySim.Controllers
{
    [Authorize]
    public class ScenarioController : Controller
    {
        private readonly ILogger<ScenarioController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IFileService _fileService; 

        public ScenarioController(ILogger<ScenarioController> logger, ApplicationDbContext context, IFileService fileService)
        {
            _logger = logger;
            _context = context;
            _fileService = fileService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(_context.Scenarios.OrderBy(x => x.isRed).ToList());
        }

        [Authorize(Roles="Admin")]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [Authorize(Roles="Admin")]
        [HttpPost]
        public IActionResult Create(IFormFile file, String Description, bool isRed)
        {
            TempData["errors"] = "";

            if(file == null) 
            {
                _logger.LogError("Scenario Create: No file was uploaded");
                TempData["errors"] = "No file was uploaded"; 
                return View();
            }
            if(Description == null) 
            {
                _logger.LogError("Scenario Create: No Description was entered");
                TempData["errors"] = "No description was provided"; 
                return View();
            }
            
            var fileName = file.FileName; 
            
            if (_context.Scenarios.Any(x => x.FileName == fileName))
            {
                _logger.LogError("Scenario Create: FileName of uploaded file matched another scenario");
                TempData["errors"] = "Sorry this file name already exist";
                return View();
            }

            _fileService.WriteIFormFile(file, Path.Combine("wwwroot/Documents/Scenario", fileName));

            var scenario = new Scenario() 
            {
                FileName = fileName,
                FilePath = Path.Combine("Documents/Scenario", fileName),
                Description = Description,
                isRed = isRed,
            };

            if (ModelState.IsValid)
            {
                _context.Add(scenario);
                _context.SaveChanges();
                
                _logger.LogInformation("Scenario Create: New database entry created");
                
                return RedirectToAction(nameof(Index));
            }

            _logger.LogError("Scenario Create: Model state was invalid");
            return View();
        }


        [Authorize(Roles="Admin")]
        [HttpPost]
        public IActionResult Delete([FromRoute] int id)
        {
            var scenario = _context.Scenarios.Find(id);
            if(scenario == null) 
            { 
                _logger.LogError("Scenario Delete on id = " + id + ": No scenario has id = " + id);
                return RedirectToAction(nameof(Index));
            }
            
            var fileName = Path.Combine("wwwroot/", scenario.FilePath);
            
            if (ModelState.IsValid) // && System.IO.File.Exists(fileName))
            {
                _fileService.DeleteFile(fileName);
                _context.Scenarios.Remove(scenario);
                _context.SaveChanges();
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles="Admin")]
        [HttpGet]
        public IActionResult Edit([FromRoute] int id)
        {
            var scenario = _context.Scenarios.Find(id);
            if(scenario == null) 
            { 
                _logger.LogError("Scenario Edit on id = " + id + ": No scenario has id = " + id);
                return RedirectToAction(nameof(Index));
            }

            return View(scenario);
        }

        [Authorize(Roles="Admin")]
        [HttpPost]
        public IActionResult Edit([FromRoute]int id, IFormFile file, String FileName, String Description, bool isRed)
        {
            // HTML Form Error Handling 
            TempData["errors"] = "";

            var scenario = _context.Scenarios.Find(id);
            if(scenario == null) 
            { 
                _logger.LogError("Scenario Edit on id = " + id + ": No scenario has id = " + id);
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                _logger.LogError("Scenario Edit on id = " + id + ": Model state was invalid");
                
                var errorMessage = string.Join("; ", ModelState.Values
                                    .SelectMany(x => x.Errors)
                                    .Select(x => x.ErrorMessage));
                _logger.LogError("Model state errors messages: " + errorMessage);
 
                return RedirectToAction(nameof(Edit), new { id = id });
            }

            if(FileName == null) 
            {
                _logger.LogError("Scenario Edit on id = " + id + ": No FileName was entered");
                TempData["errors"] = "No file name was provided"; 
                return RedirectToAction(nameof(Edit), new { id = id });
            }
          
            if (_context.Scenarios.Any(x => x.Id != id && x.FileName == FileName))
            {
                _logger.LogError("Scenario Edit on id = " + id + ": FileName matched another scenario");
                TempData["errors"] = "This file name is already used by another scenario";
                return RedirectToAction(nameof(Edit), new { id = id });
            }

            if(Description == null) 
            {
                _logger.LogError("Scenario Edit on id = " + id + ": No Description was entered");
                TempData["errors"] = "No description was provided"; 
                return RedirectToAction(nameof(Edit), new { id = id });
            }
            

            var CurrFile = Path.Combine("wwwroot", scenario.FilePath);
            var NewFile = Path.Combine("wwwroot/Documents/Scenario", FileName);

            // Replace file contents
            if (file != null) 
            {
                _fileService.WriteIFormFile(file, CurrFile);
                _logger.LogInformation("Scenario Edit on id = " + id + ": File contents were replaced by uploaded file");
            }

            // Rename file
            if(CurrFile != NewFile) 
            {
                _fileService.MoveFile(CurrFile, NewFile);
                _logger.LogInformation("Scenario Edit on id = " + id + ": File was renamed");
            }
            
            // Update scenario variable
            scenario.FileName = FileName;
            scenario.FilePath = Path.Combine("Documents/Scenario", FileName); 
            scenario.Description = Description;
            scenario.isRed = isRed;
            
            _context.Scenarios.Update(scenario);
            _context.SaveChanges();
            
            _logger.LogInformation("Scenario Edit on id = " + id + ": Database entry was updated");
            
            return RedirectToAction(nameof(Index));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            _logger.LogError("User made an error at scenario controller");
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
