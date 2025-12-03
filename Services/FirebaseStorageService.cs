using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;

namespace SkillShareBackend.Services;

public interface IFirebaseStorageService
{
    Task<string> UploadFileAsync(IFormFile file, string folder = "documents");
    Task<byte[]> DownloadFileAsync(string fileUrl);
    Task<bool> DeleteFileAsync(string fileUrl);
    string GetFileType(string fileName);
    string GetDownloadUrl(string filePath);
}

public class FirebaseStorageService : IFirebaseStorageService
{
    private readonly StorageClient _storageClient;
    private readonly ILogger<FirebaseStorageService> _logger;
    private readonly string _bucketName = "skillshare-flutter-da4ad.appspot.com";
    private readonly string _storageUrl = "https://firebasestorage.googleapis.com/v0/b/skillshare-flutter-da4ad.appspot.com/o/";
    private readonly HttpClient _httpClient;

    public FirebaseStorageService(IConfiguration configuration, ILogger<FirebaseStorageService> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(2)
        };

        try
        {
            _logger.LogInformation("🚀 Initializing Firebase Storage Service...");
            _logger.LogInformation($"📦 Bucket: {_bucketName}");

            // EL PROBLEMA: La configuración de Firebase puede no estar correcta
            var firebaseConfigJson = configuration["Firebase:Config"];
        
            if (!string.IsNullOrEmpty(firebaseConfigJson))
            {
                _logger.LogInformation("🔑 Using Firebase config from environment variables");
            
                try
                {
                    // Verificar que el JSON sea válido
                    var credential = GoogleCredential.FromJson(firebaseConfigJson);
                    _storageClient = StorageClient.Create(credential);
                    _logger.LogInformation("✅ Firebase credentials loaded successfully");
                }
                catch (Exception credentialEx)
                {
                    _logger.LogError(credentialEx, "❌ Error loading Firebase credentials from JSON");
                    throw;
                }
            }
            else
            {
                _logger.LogWarning("⚠️ No Firebase config found, using default credentials");
            
                // Para desarrollo local
                _storageClient = StorageClient.Create();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error initializing Firebase Storage Service");
            throw new Exception($"Failed to initialize Firebase Storage: {ex.Message}", ex);
        }
    }

    public async Task<string> UploadFileAsync(IFormFile file, string folder = "documents")
    {
        try
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty or null");

            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            var sanitizedFileName = Path.GetFileNameWithoutExtension(file.FileName)
                .Replace(" ", "_")
                .Replace("(", "")
                .Replace(")", "")
                .Replace("'", "")
                .Replace("\"", "");
            
            var fileName = $"{Guid.NewGuid()}_{sanitizedFileName}{fileExtension}";
            var objectName = $"{folder}/{fileName}";

            _logger.LogInformation($"📤 Uploading file: {fileName}");
            _logger.LogInformation($"📁 Destination: {objectName}");
            _logger.LogInformation($"📊 File size: {file.Length} bytes");

            using var stream = file.OpenReadStream();
            
            // Subir a Google Cloud Storage
            var uploadedObject = await _storageClient.UploadObjectAsync(
                bucket: _bucketName,
                objectName: objectName,
                contentType: GetContentType(fileExtension),
                source: stream,
                options: new UploadObjectOptions
                {
                    PredefinedAcl = PredefinedObjectAcl.PublicRead // Para acceso público
                }
            );

            // Construir URL de descarga pública
            var downloadUrl = $"{_storageUrl}{Uri.EscapeDataString(objectName)}?alt=media";
            
            _logger.LogInformation($"✅ File uploaded successfully!");
            _logger.LogInformation($"🔗 Download URL: {downloadUrl}");
            
            return downloadUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error uploading file to Firebase Storage");
            throw new Exception($"Error uploading file: {ex.Message}", ex);
        }
    }

    public async Task<byte[]> DownloadFileAsync(string fileUrl)
    {
        try
        {
            _logger.LogInformation($"📥 Downloading file from URL: {fileUrl}");

            // Extraer el nombre del objeto del URL
            var objectName = ExtractObjectNameFromUrl(fileUrl);
            
            if (string.IsNullOrEmpty(objectName))
                throw new ArgumentException("Invalid Firebase Storage URL");

            _logger.LogInformation($"📦 Extracted object name: {objectName}");

            // Descargar el objeto
            using var memoryStream = new MemoryStream();
            await _storageClient.DownloadObjectAsync(
                bucket: _bucketName,
                objectName: objectName,
                destination: memoryStream
            );

            var fileBytes = memoryStream.ToArray();
            
            _logger.LogInformation($"✅ File downloaded successfully! Size: {fileBytes.Length} bytes");
            return fileBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error downloading file from Firebase Storage");
            throw new Exception($"Error downloading file: {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteFileAsync(string fileUrl)
    {
        try
        {
            _logger.LogInformation($"🗑️ Deleting file: {fileUrl}");

            // Extraer el nombre del objeto del URL
            var objectName = ExtractObjectNameFromUrl(fileUrl);
            
            if (string.IsNullOrEmpty(objectName))
            {
                _logger.LogWarning("⚠️ Could not extract object name from URL");
                return false;
            }

            // Eliminar el objeto
            await _storageClient.DeleteObjectAsync(
                bucket: _bucketName,
                objectName: objectName
            );

            _logger.LogInformation($"✅ File deleted successfully: {objectName}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deleting file from Firebase Storage");
            return false;
        }
    }

    public string GetFileType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLower();
        return extension switch
        {
            ".pdf" => "pdf",
            ".doc" or ".docx" => "document",
            ".ppt" or ".pptx" => "presentation",
            ".xls" or ".xlsx" => "spreadsheet",
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".webp" => "image",
            ".txt" => "text",
            ".zip" or ".rar" => "archive",
            _ => "other"
        };
    }

    public string GetDownloadUrl(string filePath)
    {
        var encodedPath = Uri.EscapeDataString(filePath);
        return $"{_storageUrl}{encodedPath}?alt=media";
    }

    #region Helper Methods

    private string ExtractObjectNameFromUrl(string fileUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(fileUrl))
                return null;

            // Si ya es un nombre de objeto (no URL completa)
            if (!fileUrl.Contains("firebasestorage.googleapis.com"))
                return fileUrl;

            var uri = new Uri(fileUrl);
            var path = uri.AbsolutePath;
            
            // Extraer el nombre del objeto del path
            // Formato: /v0/b/{bucket}/o/{objectName}?alt=media
            var parts = path.Split('/');
            if (parts.Length >= 5)
            {
                var objectName = parts[4];
                return Uri.UnescapeDataString(objectName);
            }
            
            return null;
        }
        catch
        {
            return null;
        }
    }

    private string GetContentType(string fileExtension)
    {
        return fileExtension.ToLower() switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".txt" => "text/plain",
            ".zip" => "application/zip",
            ".rar" => "application/x-rar-compressed",
            _ => "application/octet-stream"
        };
    }

    #endregion
}