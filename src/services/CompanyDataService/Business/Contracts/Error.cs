namespace CompanyDataService.Business.Contracts;

public enum ErrorCodes
{
    GenericError = 999, 
    ValuesNotFound = 100

}


public record Error(string? errorMessage, ErrorCodes? errorCode);
