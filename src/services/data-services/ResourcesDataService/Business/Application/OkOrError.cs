namespace ResourcesManager.Business.Application;
public class OkOrError<T>
{
    public OkOrError(T value) => Value = value;
    public OkOrError(string error, string? stackTrace = null)
    {
        Error = error;
        StackTrace = stackTrace;
    }
   
    public T? Value { get; set; }
    public string? Error { get; set; }
    public string? StackTrace { get; set; }
    public bool IsError => Error != null;
    public bool HasValue => Value != null;
    public bool IsGood => !IsError && HasValue;
}