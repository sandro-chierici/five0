namespace ResourcesManager.Business.Application;

public enum ErrorCodes
{
    GenericError = 999, 
    ValuesNotFound = 100,
    QueryError = 1
}


public record Error(string? errorMessage, ErrorCodes? errorCode);
