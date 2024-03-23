namespace CompanyDataService.Business.Contracts;

public class QueryResponse<T>
{
    public T? Value { get; set; }
    public Error? Error { get; init; }
    public bool IsError { get => Error != null; }
}
