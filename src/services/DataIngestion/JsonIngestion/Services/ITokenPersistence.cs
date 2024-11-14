using Microsoft.AspNetCore.Http.HttpResults;

namespace JsonIngestion.Services;

public interface ITokenPersistence
{
    ValueTask<(bool ok, string message)> PersistToken(string token, string userId);

    ValueTask<(bool ok, string? userId)> GetUserIdByToken(string token);
}
