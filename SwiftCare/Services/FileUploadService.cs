using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SwiftCare.Services
{
    public class FileUploadService
    {
        //private readonly string _uploadsDirectory = @"C:\inetpub\UploadedFiles";

        private readonly string _uploadsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "UploadedFiles");


        public FileUploadService()
        {
            Directory.CreateDirectory(_uploadsDirectory);
        }

        public async Task<string> UploadFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty or null.");
            }

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            var filePath = Path.Combine(_uploadsDirectory, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return Path.Combine("UploadedFiles", uniqueFileName);
        }


        public async Task<string> SaveFileAsync(byte[] fileBytes, string fileName)
        {
            var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
            var filePath = Path.Combine(_uploadsDirectory, uniqueFileName);

            await File.WriteAllBytesAsync(filePath, fileBytes);

            return Path.Combine("UploadedFiles", uniqueFileName);
        }
    }
}
