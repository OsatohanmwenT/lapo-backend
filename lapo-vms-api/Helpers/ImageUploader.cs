using System;

namespace lapo_vms_api.Helpers;

public class ImageUploader
{
    public static async Task<string> UploadImage(IFormFile imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
            throw new ArgumentException("No file provided.");

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".avif", ".webp" };
        var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/avif", "image/gif", "image/webp" };

        var extension = Path.GetExtension(imageFile.FileName).ToLower();

        if (!allowedExtensions.Contains(extension))
            throw new Exception("Invalid file extension.");

        if (!allowedMimeTypes.Contains(imageFile.ContentType))
            throw new Exception("Invalid MIME type.");

        if (imageFile.Length > 5 * 1024 * 1024)
            throw new Exception("File too large.");

        var uploadsFolder = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "uploads",
            "visitors");

        Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await imageFile.CopyToAsync(stream);
        }
        ;

        return $"/uploads/visitors/{fileName}";
    }
}
