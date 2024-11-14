using JsonIngestion.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace JsonIngestion.Api.V1;

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
    ITokenPersistence tokenPersistence) : ControllerBase
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

        return Ok(new 
        { 
            token,
            userId
        });
    }

    /// <summary>
    /// Upload a josn data file
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadJsonFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        // Check if the file is a JSON file
        if (file.ContentType != "application/json")
        {
            logger.LogWarning("Invalid file type, not recognized as a  JSON file.");
            return BadRequest("Invalid file type, not recognized as a  JSON file.");
        }

        if (file.Length > 10485760)
        {
            logger.LogWarning("File size exceeds the limit of 10MB.");
            return BadRequest("File size exceeds the limit of 10MB.");
        }

        // Secure Check, token is required from headers
        // for getting userId
        if (!Request.Headers.TryGetValue("five0-jsonupload", out var tokenValues))
        {
            logger.LogWarning("five0-uploadtoken is required for this request.");
            return BadRequest("five0-uploadtoken is required for this request.");
        }

        if (tokenValues.Count != 1)
        {
            logger.LogWarning("Invalid token, multiple tokens found.");
            return BadRequest("Invalid token, multiple tokens found.");
        }

        var (ok, userId) = await tokenPersistence.GetUserIdByToken($"{tokenValues[0]}");
        if (!ok)
        {
            logger.LogWarning("Invalid token, userId not found");
            return BadRequest("Invalid token, userId not found");
        }

        var filePath = Path.GetTempFileName();

        try
        {
            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);
        }
        catch (Exception ex)
        {
            logger.LogError($"Error saving file: {ex.Message}");
            return StatusCode(500, "Error saving file.");
        }

        logger.LogInformation("File uploaded successfully.");

        // Read the file content
        string fileContent;
        using (var reader = new StreamReader(filePath))
        {
            fileContent = await reader.ReadToEndAsync();
        }

        // Validate and extract data from JSON content
        try
        {
            var jsonDocument = JsonDocument.Parse(fileContent);
            logger.LogInformation("JSON content is valid.");

            // Extract specific data
            var rootElement = jsonDocument.RootElement;
            if (rootElement.TryGetProperty("specificProperty", out JsonElement specificProperty))
            {
                var extractedData = specificProperty.GetString();
                logger.LogInformation($"Extracted data: {extractedData}");
            }
            else
            {
                logger.LogWarning("Specific property not found in JSON.");
            }
        }
        catch (JsonException ex)
        {
            logger.LogError($"Invalid JSON content: {ex.Message}");
            return BadRequest("Invalid JSON content.");
        }

        // Further processing of the JSON content can be done here

        return Ok(new { message = "File uploaded and JSON content is valid." });
    }
}
