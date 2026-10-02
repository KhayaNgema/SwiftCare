using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SwiftCare.Services
{
    public class FileUploadService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly ILogger<FileUploadService> _logger;

        // ============================================================
        // CONSTRUCTOR
        // ============================================================

        public FileUploadService(
            IConfiguration configuration,
            ILogger<FileUploadService> logger)
        {
            _logger = logger;

            var connectionString =
                configuration["AzureBlobStorage:ConnectionString"];

            var containerName =
                configuration["AzureBlobStorage:ContainerName"];

            _logger.LogInformation(
                "[AZURE BLOB] Initializing FileUploadService. " +
                "Container={Container}, ConnectionConfigured={ConnectionConfigured}",
                containerName,
                !string.IsNullOrWhiteSpace(connectionString));

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                _logger.LogError(
                    "[AZURE BLOB] Connection string is missing.");

                throw new InvalidOperationException(
                    "Azure Blob Storage connection string is missing.");
            }

            if (string.IsNullOrWhiteSpace(containerName))
            {
                _logger.LogError(
                    "[AZURE BLOB] Container name is missing.");

                throw new InvalidOperationException(
                    "Azure Blob Storage container name is missing.");
            }

            var blobServiceClient =
                new BlobServiceClient(
                    connectionString);

            _containerClient =
                blobServiceClient.GetBlobContainerClient(
                    containerName);

            _logger.LogInformation(
                "[AZURE BLOB] FileUploadService initialized successfully. " +
                "Container={Container}",
                _containerClient.Name);
        }

        // ============================================================
        // ENSURE CONTAINER EXISTS
        // ============================================================

        private async Task EnsureContainerExistsAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation(
                    "[AZURE BLOB] Checking container. " +
                    "Container={Container}",
                    _containerClient.Name);

                await _containerClient.CreateIfNotExistsAsync(
                    cancellationToken: cancellationToken);

                _logger.LogInformation(
                    "[AZURE BLOB] Container is available. " +
                    "Container={Container}",
                    _containerClient.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[AZURE BLOB] Failed to ensure container exists. " +
                    "Container={Container}",
                    _containerClient.Name);

                throw;
            }
        }

        // ============================================================
        // UPLOAD IFORMFILE
        // ============================================================

        public async Task<string> UploadFileAsync(
            IFormFile file,
            CancellationToken cancellationToken = default)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException(
                    "File is empty or null.",
                    nameof(file));
            }

            await EnsureContainerExistsAsync(
                cancellationToken);

            var originalFileName =
                Path.GetFileName(
                    file.FileName);

            var safeFileName =
                MakeSafeFileName(
                    originalFileName);

            var uniqueFileName =
                $"{Guid.NewGuid()}_{safeFileName}";

            var blobPath =
                uniqueFileName;

            var blobClient =
                _containerClient.GetBlobClient(
                    blobPath);

            var contentType =
                string.IsNullOrWhiteSpace(
                    file.ContentType)
                    ? GetContentType(
                        safeFileName)
                    : file.ContentType;

            var blobHttpHeaders =
                new BlobHttpHeaders
                {
                    ContentType =
                        contentType
                };

            _logger.LogInformation(
                "[AZURE BLOB] IFormFile upload starting. " +
                "Container={Container}, BlobPath={BlobPath}, " +
                "FileName={FileName}, FileSize={FileSize}, ContentType={ContentType}",
                _containerClient.Name,
                blobPath,
                safeFileName,
                file.Length,
                contentType);

            try
            {
                await using var stream =
                    file.OpenReadStream();

                await blobClient.UploadAsync(
                    stream,
                    new BlobUploadOptions
                    {
                        HttpHeaders =
                            blobHttpHeaders
                    },
                    cancellationToken);

                var exists =
                    await blobClient.ExistsAsync(
                        cancellationToken);

                _logger.LogInformation(
                    "[AZURE BLOB] IFormFile upload completed. " +
                    "Container={Container}, BlobPath={BlobPath}, " +
                    "ExistsAfterUpload={ExistsAfterUpload}",
                    _containerClient.Name,
                    blobPath,
                    exists.Value);

                return blobPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[AZURE BLOB] IFormFile upload FAILED. " +
                    "Container={Container}, BlobPath={BlobPath}",
                    _containerClient.Name,
                    blobPath);

                throw;
            }
        }

        // ============================================================
        // SAVE BYTE ARRAY
        // ============================================================

        public async Task<string> SaveFileAsync(
            byte[] fileBytes,
            string fileName,
            CancellationToken cancellationToken = default)
        {
            if (fileBytes == null ||
                fileBytes.Length == 0)
            {
                throw new ArgumentException(
                    "File bytes are empty or null.",
                    nameof(fileBytes));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException(
                    "File name is required.",
                    nameof(fileName));
            }

            await EnsureContainerExistsAsync(
                cancellationToken);

            var safeFileName =
                MakeSafeFileName(
                    Path.GetFileName(fileName));

            var uniqueFileName =
                $"{Guid.NewGuid()}_{safeFileName}";

            var blobPath =
                uniqueFileName;

            var blobClient =
                _containerClient.GetBlobClient(
                    blobPath);

            var contentType =
                GetContentType(
                    safeFileName);

            _logger.LogInformation(
                "[AZURE BLOB] Byte-array upload starting. " +
                "Container={Container}, BlobPath={BlobPath}, " +
                "FileSize={FileSize}, ContentType={ContentType}",
                _containerClient.Name,
                blobPath,
                fileBytes.Length,
                contentType);

            try
            {
                await using var stream =
                    new MemoryStream(fileBytes);

                await blobClient.UploadAsync(
                    stream,
                    new BlobUploadOptions
                    {
                        HttpHeaders =
                            new BlobHttpHeaders
                            {
                                ContentType =
                                    contentType
                            }
                    },
                    cancellationToken);

                var exists =
                    await blobClient.ExistsAsync(
                        cancellationToken);

                _logger.LogInformation(
                    "[AZURE BLOB] Byte-array upload completed. " +
                    "Container={Container}, BlobPath={BlobPath}, " +
                    "ExistsAfterUpload={ExistsAfterUpload}",
                    _containerClient.Name,
                    blobPath,
                    exists.Value);

                return blobPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[AZURE BLOB] Byte-array upload FAILED. " +
                    "Container={Container}, BlobPath={BlobPath}",
                    _containerClient.Name,
                    blobPath);

                throw;
            }
        }

        // ============================================================
        // SAVE BYTE ARRAY TO SPECIFIC FOLDER
        // ============================================================

        public async Task<string> SaveFileAsync(
            byte[] fileBytes,
            string fileName,
            string folderPath,
            CancellationToken cancellationToken = default)
        {
            if (fileBytes == null ||
                fileBytes.Length == 0)
            {
                throw new ArgumentException(
                    "File bytes are empty or null.",
                    nameof(fileBytes));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException(
                    "File name is required.",
                    nameof(fileName));
            }

            await EnsureContainerExistsAsync(
                cancellationToken);

            var safeFileName =
                MakeSafeFileName(
                    Path.GetFileName(fileName));

            var cleanFolderPath =
                CleanBlobPath(
                    folderPath);

            var blobPath =
                string.IsNullOrWhiteSpace(
                    cleanFolderPath)
                    ? safeFileName
                    : $"{cleanFolderPath}/{safeFileName}";

            var blobClient =
                _containerClient.GetBlobClient(
                    blobPath);

            var contentType =
                GetContentType(
                    safeFileName);

            // ========================================================
            // LOG EXACT UPLOAD TARGET
            // ========================================================

            _logger.LogInformation(
                "[AZURE BLOB] Folder upload starting. " +
                "Container={Container}, Folder={Folder}, " +
                "BlobPath={BlobPath}, FileName={FileName}, " +
                "FileSize={FileSize}, ContentType={ContentType}",
                _containerClient.Name,
                cleanFolderPath,
                blobPath,
                safeFileName,
                fileBytes.Length,
                contentType);

            try
            {
                await using var stream =
                    new MemoryStream(fileBytes);

                await blobClient.UploadAsync(
                    stream,
                    new BlobUploadOptions
                    {
                        HttpHeaders =
                            new BlobHttpHeaders
                            {
                                ContentType =
                                    contentType
                            }
                    },
                    cancellationToken);

                _logger.LogInformation(
                    "[AZURE BLOB] Upload request completed. " +
                    "Container={Container}, BlobPath={BlobPath}",
                    _containerClient.Name,
                    blobPath);

                // ====================================================
                // VERIFY BLOB REALLY EXISTS
                // ====================================================

                var exists =
                    await blobClient.ExistsAsync(
                        cancellationToken);

                _logger.LogInformation(
                    "[AZURE BLOB] Upload verification. " +
                    "Container={Container}, BlobPath={BlobPath}, " +
                    "ExistsAfterUpload={ExistsAfterUpload}",
                    _containerClient.Name,
                    blobPath,
                    exists.Value);

                if (!exists.Value)
                {
                    _logger.LogError(
                        "[AZURE BLOB] Upload returned successfully " +
                        "but blob could not be found afterwards. " +
                        "Container={Container}, BlobPath={BlobPath}",
                        _containerClient.Name,
                        blobPath);

                    throw new InvalidOperationException(
                        $"Azure Blob upload completed but the blob " +
                        $"could not be verified: {blobPath}");
                }

                _logger.LogInformation(
                    "[AZURE BLOB] Folder upload SUCCESSFUL. " +
                    "Container={Container}, BlobPath={BlobPath}",
                    _containerClient.Name,
                    blobPath);

                return blobPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[AZURE BLOB] Folder upload FAILED. " +
                    "Container={Container}, BlobPath={BlobPath}",
                    _containerClient.Name,
                    blobPath);

                throw;
            }
        }

        // ============================================================
        // DOWNLOAD FILE
        // ============================================================

        public async Task<FileDownloadResult?> DownloadFileAsync(
            string blobPath,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(blobPath))
            {
                return null;
            }

            var cleanBlobPath =
                CleanBlobPath(
                    blobPath);

            if (string.IsNullOrWhiteSpace(
                cleanBlobPath))
            {
                return null;
            }

            _logger.LogInformation(
                "[AZURE BLOB] Download requested. " +
                "Container={Container}, BlobPath={BlobPath}",
                _containerClient.Name,
                cleanBlobPath);

            var blobClient =
                _containerClient.GetBlobClient(
                    cleanBlobPath);

            try
            {
                var exists =
                    await blobClient.ExistsAsync(
                        cancellationToken);

                _logger.LogInformation(
                    "[AZURE BLOB] Download existence check. " +
                    "Container={Container}, BlobPath={BlobPath}, Exists={Exists}",
                    _containerClient.Name,
                    cleanBlobPath,
                    exists.Value);

                if (!exists.Value)
                {
                    return null;
                }

                var response =
                    await blobClient.DownloadContentAsync(
                        cancellationToken);

                var binaryData =
                    response.Value.Content;

                var properties =
                    await blobClient.GetPropertiesAsync(
                        cancellationToken: cancellationToken);

                var fileName =
                    Path.GetFileName(
                        cleanBlobPath);

                var contentType =
                    string.IsNullOrWhiteSpace(
                        properties.Value.ContentType)
                        ? GetContentType(fileName)
                        : properties.Value.ContentType;

                _logger.LogInformation(
                    "[AZURE BLOB] Download successful. " +
                    "Container={Container}, BlobPath={BlobPath}, " +
                    "FileSize={FileSize}, ContentType={ContentType}",
                    _containerClient.Name,
                    cleanBlobPath,
                    binaryData.Length,
                    contentType);

                return new FileDownloadResult
                {
                    Content =
                        binaryData.ToArray(),

                    FileName =
                        fileName,

                    ContentType =
                        contentType,

                    BlobPath =
                        cleanBlobPath
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[AZURE BLOB] Download FAILED. " +
                    "Container={Container}, BlobPath={BlobPath}",
                    _containerClient.Name,
                    cleanBlobPath);

                throw;
            }
        }

        // ============================================================
        // CHECK WHETHER FILE EXISTS
        // ============================================================

        public async Task<bool> FileExistsAsync(
            string blobPath,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(blobPath))
            {
                return false;
            }

            var cleanBlobPath =
                CleanBlobPath(
                    blobPath);

            if (string.IsNullOrWhiteSpace(
                cleanBlobPath))
            {
                return false;
            }

            var blobClient =
                _containerClient.GetBlobClient(
                    cleanBlobPath);

            try
            {
                var response =
                    await blobClient.ExistsAsync(
                        cancellationToken);

                _logger.LogInformation(
                    "[AZURE BLOB] Existence check. " +
                    "Container={Container}, BlobPath={BlobPath}, Exists={Exists}",
                    _containerClient.Name,
                    cleanBlobPath,
                    response.Value);

                return response.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[AZURE BLOB] Existence check FAILED. " +
                    "Container={Container}, BlobPath={BlobPath}",
                    _containerClient.Name,
                    cleanBlobPath);

                throw;
            }
        }

        // ============================================================
        // DELETE FILE
        // ============================================================

        public async Task<bool> DeleteFileAsync(
            string blobPath,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(blobPath))
            {
                return false;
            }

            var cleanBlobPath =
                CleanBlobPath(
                    blobPath);

            if (string.IsNullOrWhiteSpace(
                cleanBlobPath))
            {
                return false;
            }

            var blobClient =
                _containerClient.GetBlobClient(
                    cleanBlobPath);

            try
            {
                var response =
                    await blobClient.DeleteIfExistsAsync(
                        cancellationToken:
                            cancellationToken);

                _logger.LogInformation(
                    "[AZURE BLOB] Delete completed. " +
                    "Container={Container}, BlobPath={BlobPath}, Deleted={Deleted}",
                    _containerClient.Name,
                    cleanBlobPath,
                    response.Value);

                return response.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "[AZURE BLOB] Delete FAILED. " +
                    "Container={Container}, BlobPath={BlobPath}",
                    _containerClient.Name,
                    cleanBlobPath);

                throw;
            }
        }

        // ============================================================
        // GET BLOB URL
        // ============================================================

        public string GetBlobUrl(
            string blobPath)
        {
            if (string.IsNullOrWhiteSpace(blobPath))
            {
                throw new ArgumentException(
                    "Blob path is required.",
                    nameof(blobPath));
            }

            var cleanBlobPath =
                CleanBlobPath(
                    blobPath);

            var blobClient =
                _containerClient.GetBlobClient(
                    cleanBlobPath);

            _logger.LogInformation(
                "[AZURE BLOB] Blob URL requested. " +
                "Container={Container}, BlobPath={BlobPath}",
                _containerClient.Name,
                cleanBlobPath);

            return blobClient.Uri.ToString();
        }

        // ============================================================
        // SAFE FILE NAME
        // ============================================================

        private static string MakeSafeFileName(
            string fileName)
        {
            var safeFileName =
                Path.GetFileName(
                    fileName);

            foreach (
                var invalidCharacter
                in Path.GetInvalidFileNameChars())
            {
                safeFileName =
                    safeFileName.Replace(
                        invalidCharacter,
                        '_');
            }

            return string.IsNullOrWhiteSpace(
                safeFileName)
                ? $"file_{Guid.NewGuid():N}"
                : safeFileName;
        }

        // ============================================================
        // CLEAN BLOB PATH
        // ============================================================

        private static string CleanBlobPath(
            string? blobPath)
        {
            if (string.IsNullOrWhiteSpace(
                blobPath))
            {
                return string.Empty;
            }

            var path =
                blobPath
                    .Replace("\\", "/")
                    .Trim('/');

            while (path.Contains("//"))
            {
                path =
                    path.Replace(
                        "//",
                        "/");
            }

            return path;
        }

        // ============================================================
        // CONTENT TYPE
        // ============================================================

        private static string GetContentType(
            string fileName)
        {
            var extension =
                Path.GetExtension(
                    fileName)
                .ToLowerInvariant();

            return extension switch
            {
                ".pdf" =>
                    "application/pdf",

                ".jpg" =>
                    "image/jpeg",

                ".jpeg" =>
                    "image/jpeg",

                ".png" =>
                    "image/png",

                ".gif" =>
                    "image/gif",

                ".webp" =>
                    "image/webp",

                ".bmp" =>
                    "image/bmp",

                ".svg" =>
                    "image/svg+xml",

                ".doc" =>
                    "application/msword",

                ".docx" =>
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document",

                ".xls" =>
                    "application/vnd.ms-excel",

                ".xlsx" =>
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",

                ".csv" =>
                    "text/csv",

                ".txt" =>
                    "text/plain",

                ".zip" =>
                    "application/zip",

                ".mp4" =>
                    "video/mp4",

                ".mov" =>
                    "video/quicktime",

                ".avi" =>
                    "video/x-msvideo",

                ".webm" =>
                    "video/webm",

                _ =>
                    "application/octet-stream"
            };
        }
    }

    // ================================================================
    // FILE DOWNLOAD RESULT
    // ================================================================

    public class FileDownloadResult
    {
        public byte[] Content { get; set; } =
            Array.Empty<byte>();

        public string FileName { get; set; } =
            string.Empty;

        public string ContentType { get; set; } =
            "application/octet-stream";

        public string BlobPath { get; set; } =
            string.Empty;
    }
}