using Microsoft.AspNetCore.Http;

namespace lapo_vms_api.Helpers;

public class ImageUploader
{
    private const long MaxFileSize = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".avif", ".webp"
    };

    private static readonly Dictionary<string, string> MimeTypeToExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/gif"] = ".gif",
        ["image/avif"] = ".avif",
        ["image/webp"] = ".webp"
    };

    public static async Task<string> UploadImage(IFormFile imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
            throw new ArgumentException("No file provided.");

        var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension))
            throw new Exception("Invalid file extension.");

        if (!MimeTypeToExtension.ContainsKey(imageFile.ContentType))
            throw new Exception("Invalid MIME type.");

        if (imageFile.Length > MaxFileSize)
            throw new Exception("File too large.");

        var (filePath, relativePath) = BuildOutputPaths(extension);

        await using var stream = new FileStream(filePath, FileMode.Create);
        await imageFile.CopyToAsync(stream);

        return relativePath;
    }

    public static async Task<string> UploadImage(string imageData)
    {
        if (string.IsNullOrWhiteSpace(imageData))
            throw new ArgumentException("No image data provided.");

        var (mimeType, base64Data) = ParseImageData(imageData);

        if (!MimeTypeToExtension.TryGetValue(mimeType, out var extension))
            throw new Exception("Invalid MIME type.");

        byte[] imageBytes;
        try
        {
            imageBytes = Convert.FromBase64String(base64Data);
        }
        catch (FormatException)
        {
            throw new Exception("Invalid base64 image data.");
        }

        if (imageBytes.Length == 0)
            throw new Exception("No file provided.");

        if (imageBytes.Length > MaxFileSize)
            throw new Exception("File too large.");

        var (filePath, relativePath) = BuildOutputPaths(extension);

        await File.WriteAllBytesAsync(filePath, imageBytes);

        return relativePath;
    }

    private static (string filePath, string relativePath) BuildOutputPaths(string extension)
    {
        var uploadsFolder = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "uploads",
            "visitors");

        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        return (filePath, $"/uploads/visitors/{fileName}");
    }

    private static (string mimeType, string base64Data) ParseImageData(string imageData)
    {
        var trimmed = imageData.Trim();

        if (!trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            // Treat bare base64 strings as PNG images.
            return ("image/png", trimmed);
        }

        var separatorIndex = trimmed.IndexOf(',');
        if (separatorIndex < 0)
            throw new Exception("Invalid image data URL format.");

        var metadata = trimmed[5..separatorIndex];
        var metadataParts = metadata.Split(';', StringSplitOptions.RemoveEmptyEntries);

        var mimeType = metadataParts.FirstOrDefault() ?? string.Empty;
        var hasBase64Marker = metadataParts.Any(part =>
            part.Equals("base64", StringComparison.OrdinalIgnoreCase));

        if (!hasBase64Marker)
            throw new Exception("Image data URL must be base64 encoded.");

        var base64Data = trimmed[(separatorIndex + 1)..];

        return (mimeType, base64Data);
    }
}
