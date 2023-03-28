using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using CySim.Interfaces;
namespace CySim.Services
{
    public class FileService : IFileService
    {
        private readonly ILogger<FileService> _logger;
            
        public FileService(ILogger<FileService> logger)
        {
            _logger = logger;
        }

        public void WriteIFormFile(IFormFile file, String path)
		{
            using (var stream = new FileStream(path, FileMode.Create))
            {
                file.CopyTo(stream);
                _logger.LogInformation("File was created or overwritten: " + path);
            }
		}
        
        public void MoveFile(String currPath, String newPath)
		{
            File.Move(currPath, newPath);
            _logger.LogInformation("File was moved: " + currPath + " --> " + newPath);
		}
        
        public void DeleteFile(String path)
		{
            File.Delete(path);
            _logger.LogInformation("File was deleted: " + path);
		}
    }
}

