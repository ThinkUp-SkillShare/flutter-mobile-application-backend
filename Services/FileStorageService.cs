using System.Text;

namespace SkillShareBackend.Services;

/// <summary>
/// Servicio para manejo de archivos sin servicios externos de pago.
/// Almacena archivos pequeños como Base64 en DB y grandes en sistema de archivos local.
/// </summary>
public interface IFileStorageService
{
    Task<string> SaveFileAsync(string base64Data, string fileName, string fileType);
    Task<string> GetFileAsBase64Async(string filePath);
    Task<bool> DeleteFileAsync(string filePath);
    string GetFileUrl(string filePath);
    bool IsBase64String(string str);
}

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private const long MAX_BASE64_SIZE = 50 * 1024;
    private const long MAX_FILE_SIZE = 50 * 1024 * 1024;

    public FileStorageService(IWebHostEnvironment environment, IConfiguration configuration)
    {
        _environment = environment;
        _configuration = configuration;
    }

    /// <summary>
    /// Guarda un archivo desde Base64. 
    /// Si es menor a 50KB, retorna el Base64 para guardar en DB.
    /// Si es mayor, lo guarda en el sistema de archivos local.
    /// </summary>
    public async Task<string> SaveFileAsync(string base64Data, string fileName, string fileType)
    {
        try
        {
            Console.WriteLine($"💾 FileStorageService - Saving file: {fileName}, Type: {fileType}");

            // Limpiar el Base64 si viene con prefijo data:image/...
            var cleanBase64 = CleanBase64String(base64Data);
            
            // Convertir a bytes para verificar tamaño
            var fileBytes = Convert.FromBase64String(cleanBase64);
            var fileSize = fileBytes.Length;

            Console.WriteLine($"💾 FileStorageService - File size: {fileSize} bytes");

            if (fileSize > MAX_FILE_SIZE)
            {
                throw new Exception($"File size exceeds maximum allowed size of {MAX_FILE_SIZE / (1024 * 1024)}MB");
            }

            // SIEMPRE guardar en sistema de archivos
            var uploadsFolder = GetUploadsFolder(fileType);
            Console.WriteLine($"💾 FileStorageService - Uploads folder: {uploadsFolder}");
            
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}_{SanitizeFileName(fileName)}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            Console.WriteLine($"💾 FileStorageService - Saving to: {filePath}");

            await File.WriteAllBytesAsync(filePath, fileBytes);

            // Solo retornar el nombre del archivo (máximo 255 chars)
            Console.WriteLine($"💾 FileStorageService - File saved successfully: {uniqueFileName}");
            return uniqueFileName;
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"❌ FileStorageService - FormatException: {ex.Message}");
            throw new Exception("Invalid Base64 string format");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ FileStorageService - Error: {ex.Message}");
            Console.WriteLine($"❌ FileStorageService - StackTrace: {ex.StackTrace}");
            throw new Exception($"Error saving file: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene un archivo como Base64. Si está en DB, lo retorna directamente.
    /// Si está en el sistema de archivos, lo lee y convierte a Base64.
    /// </summary>
    public async Task<string> GetFileAsBase64Async(string filePath)
    {
        try
        {
            // Si ya es Base64, retornar directamente
            if (IsBase64String(filePath))
            {
                return filePath;
            }

            // Si es un nombre de archivo, determinar la carpeta y leer del sistema de archivos
            var uploadsFolder = GetUploadsFolderForExistingFile(filePath);
            var fullPath = Path.Combine(uploadsFolder, filePath);
            
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var fileBytes = await File.ReadAllBytesAsync(fullPath);
            var base64 = Convert.ToBase64String(fileBytes);
            var mimeType = GetMimeTypeFromPath(fullPath);

            return $"data:{mimeType};base64,{base64}";
        }
        catch (Exception ex)
        {
            throw new Exception($"Error reading file: {ex.Message}");
        }
    }

    /// <summary>
    /// Elimina un archivo del sistema de archivos (si no es Base64).
    /// </summary>
    public async Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            // Si es Base64, no hay nada que eliminar del sistema de archivos
            if (IsBase64String(filePath))
            {
                return true;
            }

            // Buscar el archivo en todas las carpetas de uploads
            var uploadsFolder = GetUploadsFolderForExistingFile(filePath);
            var fullPath = Path.Combine(uploadsFolder, filePath);
            
            if (File.Exists(fullPath))
            {
                await Task.Run(() => File.Delete(fullPath));
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting file: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Obtiene la URL para acceder al archivo.
    /// </summary>
    public string GetFileUrl(string filePath)
    {
        if (IsBase64String(filePath))
        {
            return filePath;
        }

        var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "http://localhost:5000";
        return $"{baseUrl}/uploads/{GetFileCategoryFromPath(filePath)}/{filePath}";
    }

    /// <summary>
    /// Verifica si un string es Base64.
    /// </summary>
    public bool IsBase64String(string str)
    {
        if (string.IsNullOrEmpty(str))
            return false;

        return str.StartsWith("data:") && str.Contains(";base64,");
    }

    #region Helper Methods

    private string CleanBase64String(string base64Data)
    {
        if (base64Data.Contains(";base64,"))
        {
            return base64Data.Split(";base64,")[1];
        }
        return base64Data;
    }

    private string GetUploadsFolder(string fileType)
    {
        var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads");
        
        return fileType.ToLower() switch
        {
            "image" => Path.Combine(uploadsRoot, "images"),
            "audio" => Path.Combine(uploadsRoot, "audio"),
            "file" => Path.Combine(uploadsRoot, "files"),
            _ => Path.Combine(uploadsRoot, "others")
        };
    }

    /// <summary>
    /// Busca en qué carpeta existe un archivo (para archivos existentes)
    /// </summary>
    private string GetUploadsFolderForExistingFile(string fileName)
    {
        var uploadsRoot = Path.Combine(_environment.WebRootPath, "uploads");
        var possibleFolders = new[] { "images", "audio", "files", "others" };

        foreach (var folder in possibleFolders)
        {
            var fullPath = Path.Combine(uploadsRoot, folder, fileName);
            if (File.Exists(fullPath))
            {
                return Path.Combine(uploadsRoot, folder);
            }
        }

        // Si no se encuentra, devolver la carpeta de imágenes por defecto
        return Path.Combine(uploadsRoot, "images");
    }

    /// <summary>
    /// Determina la categoría del archivo basado en su extensión
    /// </summary>
    private string GetFileCategoryFromPath(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        
        switch (extension)
        {
            case ".jpg":
            case ".jpeg":
            case ".png":
            case ".gif":
            case ".bmp":
            case ".webp":
                return "images";
            case ".mp3":
            case ".wav":
            case ".m4a":
            case ".aac":
            case ".ogg":
                return "audio";
            case ".pdf":
            case ".doc":
            case ".docx":
            case ".xls":
            case ".xlsx":
            case ".txt":
            case ".zip":
            case ".rar":
                return "files";
            default:
                return "others";
        }
    }

    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        return string.IsNullOrEmpty(sanitized) ? "file" : sanitized;
    }

    private string GetMimeType(string fileType)
    {
        return fileType.ToLower() switch
        {
            "image" => "image/jpeg",
            "audio" => "audio/mpeg",
            "file" => "application/octet-stream",
            _ => "application/octet-stream"
        };
    }

    private string GetMimeTypeFromPath(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLower();
        
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            ".mp3" => "audio/mpeg",
            ".m4a" => "audio/mp4",
            ".wav" => "audio/wav",
            ".aac" => "audio/aac",
            ".ogg" => "audio/ogg",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".txt" => "text/plain",
            ".zip" => "application/zip",
            ".rar" => "application/x-rar-compressed",
            _ => "application/octet-stream"
        };
    }

    #endregion
}