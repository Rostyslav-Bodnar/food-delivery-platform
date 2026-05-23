using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DF.MenuService.Application.Services.Interfaces;
using DF.MenuService.Contracts.Models.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace DF.MenuService.Application.Services;

public class CloudinaryService : ICloudinaryService
{
    private const long MaxImageBytes = 5L * 1024 * 1024; // 5 MB

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/webp",
        "image/gif"
    };

    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IConfiguration configuration)
    {
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        _cloudinary = new Cloudinary(new Account(cloudName, apiKey, apiSecret));
    }

    public async Task<UploadImageResult> UploadAsync(IFormFile file, string? folder = null)
    {
        if (file is null || file.Length == 0)
            throw new ArgumentException("File is empty", nameof(file));

        if (file.Length > MaxImageBytes)
            throw new ArgumentException($"File exceeds the {MaxImageBytes / (1024 * 1024)} MB limit", nameof(file));

        if (string.IsNullOrEmpty(file.ContentType) || !AllowedContentTypes.Contains(file.ContentType))
            throw new ArgumentException($"Unsupported content type '{file.ContentType}'", nameof(file));

        // Never trust the client-supplied filename — generate a fresh one and keep the extension only.
        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(extension))
            extension = ".jpg";
        var safeName = $"{Guid.NewGuid():N}{extension}";

        await using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(safeName, stream),
            Folder = folder
        };

        var result = await _cloudinary.UploadAsync(uploadParams);

        if (result.StatusCode != System.Net.HttpStatusCode.OK)
            throw new Exception(result.Error?.Message ?? "Cloudinary upload failed");

        return new UploadImageResult(result.SecureUrl.ToString(), result.PublicId);
    }

    public async Task<bool> DeleteAsync(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            return false;

        var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId));
        return result.Result == "ok";
    }
}
