using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SwiftCare.Services
{
    public class FileUploadService
    {
        private readonly BlobContainerClient _containerClient;

        public FileUploadService(IConfiguration configuration)
        {
            var connectionString =
                configuration["AzureBlobStorage:ConnectionString"];

            var containerName =
                configuration["AzureBlobStorage:ContainerName"];

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "Azure Blob Storage connection string is missing."
                );
            }

            if (string.IsNullOrWhiteSpace(containerName))
            {
                throw new InvalidOperationException(
                    "Azure Blob Storage container name is missing."
                );
            }

            var blobServiceClient =
                new BlobServiceClient(connectionString);

            _containerClient =
                blobServiceClient.GetBlobContainerClient(containerName);
        }


        // ============================================================
        // UPLOAD IFORMFILE
        // ============================================================

        public async Task<string> UploadFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException(
                    "File is empty or null."
                );
            }

            await _containerClient.CreateIfNotExistsAsync(
                PublicAccessType.Blob
            );

            var originalFileName =
                Path.GetFileName(file.FileName);

            var uniqueFileName =
                $"{Guid.NewGuid()}_{originalFileName}";

            var blobClient =
                _containerClient.GetBlobClient(uniqueFileName);

            var blobHttpHeaders =
                new BlobHttpHeaders
                {
                    ContentType = file.ContentType
                };

            using var stream = file.OpenReadStream();

            await blobClient.UploadAsync(
                stream,
                new BlobUploadOptions
                {
                    HttpHeaders = blobHttpHeaders
                }
            );

            return blobClient.Uri.ToString();
        }


        // ============================================================
        // SAVE BYTE ARRAY
        // ============================================================

        public async Task<string> SaveFileAsync(
            byte[] fileBytes,
            string fileName)
        {
            if (fileBytes == null || fileBytes.Length == 0)
            {
                throw new ArgumentException(
                    "File bytes are empty or null."
                );
            }

            await _containerClient.CreateIfNotExistsAsync(
                PublicAccessType.Blob
            );

            var safeFileName =
                Path.GetFileName(fileName);

            var uniqueFileName =
                $"{Guid.NewGuid()}_{safeFileName}";

            var blobClient =
                _containerClient.GetBlobClient(uniqueFileName);

            using var stream =
                new MemoryStream(fileBytes);

            await blobClient.UploadAsync(
                stream,
                overwrite: true
            );

            return blobClient.Uri.ToString();
        }
    }
}