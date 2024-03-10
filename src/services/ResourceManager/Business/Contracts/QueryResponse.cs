﻿namespace ResourceManager.Business.Contracts;

public class QueryResponse<T>
{
    public T? Value { get; set; }
    public Error? Error { get; set; }
}
