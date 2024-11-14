using JsonIngestion.Services;
using System.Collections.Concurrent;

namespace JsonIngestion.Implementation;

public class MemoryTokenPersistence(ILogger<MemoryTokenPersistence> logger) : ITokenPersistence
{
    private readonly ConcurrentDictionary<string, string> tokens = 
        new ConcurrentDictionary<string, string>();

    public ValueTask<(bool ok, string? userId)> GetUserIdByToken(string token)
    {
        var exists = tokens.TryGetValue(token, out var userId);

        return ValueTask.FromResult((exists, userId));
    }

    public ValueTask<(bool ok, string message)> PersistToken(string token, string userId)
    {
        if (token.Trim().Length == 0)
            return ValueTask.FromResult((false, "Token is blank"));

        // substitute the old user's token
        foreach (var item in tokens)
        {
            if (string.Equals(item.Value, userId, StringComparison.OrdinalIgnoreCase))
            {
                tokens.TryRemove(item.Key, out _);
                break;
            }                
        }

        tokens[token] = userId;

        logger.LogInformation("Token {token} is persisted for user {userId}", token, userId);

        return ValueTask.FromResult((true, ""));
    }
}
