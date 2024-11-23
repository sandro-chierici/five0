using JsonIngestion.DataModels;
using JsonIngestion.Implementation;
using JsonIngestion.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonIngestion.Api.V1;

/// <summary>
/// dataout
/// </summary>
/// <param name="message"></param>
/// <param name="data"></param>
public record ResponseJson(
    [property: JsonPropertyName("message")]
    string? Message = null,
    [property: JsonPropertyName("data")]
    object? Data = null
    );


/// <summary>
/// Ingestion json data files
/// 
/// Max 10MB could be safe to save
/// 
/// </summary>
/// <param name="logger"></param>
[ApiController]
[Route("api/v1/ingestion/json")]
public class JsonFileController(ILogger<JsonFileController> logger,
    ITokenPersistence tokenPersistence,
    DataProcessor processor,
    IConfiguration config) : ControllerBase
{
    /// <summary>
    /// Create token for upload
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    [HttpPost("preflight/{userId}")]
    public async ValueTask<IActionResult> CreateUploadToken(string userId)
    {
        if (userId.Trim().Length == 0)
            return BadRequest("userId is blank");

        var token = Guid.NewGuid().ToString();

        // persist 
        var (ok, message) = await tokenPersistence.PersistToken(token, userId);
        if (!ok)
            return BadRequest(message);

        return Ok(new ResponseJson(Data: new
        {
            token,
            userId
        }));
    }

    /// <summary>
    /// Upload a josn data file
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadJsonFile(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new ResponseJson(Message: "No file uploaded found. Expected a parameter with name 'file' containing file to upload"));

        if (file.ContentType != "application/json")
        {
            logger.LogWarning("Invalid file type, not recognized as a  JSON file.");
            return BadRequest(new ResponseJson(Message: "Invalid file type, not recognized as a JSON file."));
        }

        var maxBytes = config.GetValue<int?>("MaxBytesFilelUpload") ?? 10485760;

        if (file.Length > maxBytes)
        {
            logger.LogWarning($"File size exceeds the limit of {maxBytes} bytes");
            return BadRequest(new ResponseJson(Message: $"File size exceeds the limit of {maxBytes} bytes"));
        }

        // Secure Check, token is required from headers
        // for getting userId
        if (!Request.Headers.TryGetValue("five0-jsonupload", out var tokenValues))
        {
            logger.LogWarning("five0-jsonupload is required for this request.");
            return BadRequest(new ResponseJson(Message: "five0-jsonupload is required for this request."));
        }

        if (tokenValues.Count != 1)
        {
            logger.LogWarning("Invalid token, multiple tokens found.");
            return BadRequest(new ResponseJson(Message: "Invalid token, multiple tokens found."));
        }

        var (ok, userId) = await tokenPersistence.GetUserIdByToken($"{tokenValues[0]}");
        if (!ok)
        {
            logger.LogWarning("Invalid token, userId not found");
            return BadRequest(new ResponseJson(Message: "Invalid token, userId not found"));
        }

        // reading file
        try
        {
            using var memstream = new MemoryStream();
            await file.CopyToAsync(memstream);

            memstream.Position = 0;

            var jsonDocument = JsonDocument.Parse(memstream);

            var request = new RequestDetail(
                Request.Host.ToString(),
                Request.Headers.Origin.ToString(),
                file.Length,
                UserId: userId!,
                DateTimeOffset.UtcNow);

            logger.LogInformation($"JSON content is valid from {request.HostOrigin}, length {file.Length}");

            // enqueued for processing
            processor.EnqueueData(new InputDataDto(
                jsonDocument,
                request));
        }
        catch (Exception ex)
        {
            logger.LogError($"Error saving file: {ex.Message}");
            return StatusCode(500, new ResponseJson(Message: "Error saving file."));
        }

        logger.LogInformation("File uploaded successfully.");

        return Accepted(new ResponseJson(Message: "File uploaded and JSON content is valid and it will processed."));
    }
}
