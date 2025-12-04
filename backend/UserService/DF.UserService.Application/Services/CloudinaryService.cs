using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DF.UserService.Application.Services.Interfaces;
using DF.UserService.Contracts.Models.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace DF.UserService.Application.Services;

public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary cloudinary;

    public CloudinaryService(IConfiguration configuration)
    {
        var cloudName = configuration["Cloudinary:CloudName"];
        var apiKey = configuration["Cloudinary:ApiKey"];
        var apiSecret = configuration["Cloudinary:ApiSecret"];

        var account = new CloudinaryDotNet.Account(cloudName, apiKey, apiSecret);
        cloudinary = new Cloudinary(account);
    }

    public async Task<UploadImageResult> UploadAsync(IFormFile file, string folder = null)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File is empty", nameof(file));

        await using var stream = file.OpenReadStream();

        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folder
        };

        var result = await cloudinary.UploadAsync(uploadParams);

        if (result.StatusCode != System.Net.HttpStatusCode.OK)
            throw new Exception(result.Error?.Message ?? "Cloudinary upload failed");

        return new UploadImageResult(result.SecureUrl.ToString(), result.PublicId);
    }

    public async Task<bool> DeleteAsync(string publicId)
    {
        var deletionParams = new DeletionParams(publicId);
        var result = await cloudinary.DestroyAsync(deletionParams);

        return result.Result == "ok";
    }
    
}