
public class OkOrError<T>()
{
    public T? Value { get; set; }
    public string? Error { get; set; }
    public string? StackTrace { get; set; }
    public bool IsError => Error != null;
    public bool HasValue => Value != null;
}