using System;
using Microsoft.AspNetCore.Http;

namespace CySim.Interfaces
{
    public interface IFileService
    {
        void WriteIFormFile(IFormFile file, String path);
        void MoveFile(String currPath, String newPath);
        void DeleteFile(String path);
    }
}

