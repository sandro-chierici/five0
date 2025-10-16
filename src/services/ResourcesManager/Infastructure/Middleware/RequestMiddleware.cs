using ResourcesManager.Business.Application;

namespace ResourcesManager.Infrastructure.Middleware;


/// <summary>
/// Add a pipline middleware to generate a unique ID for every request 
/// for traceability and logging
/// 
/// The unique id starting from request is then percolated throught all service pipeline to  the response
/// </summary>
/// <param name="Next"></param>
public class OriginTraceMiddleware(RequestDelegate Next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // service unique id starting request
        context.Request.Headers.TryGetValue(ResourceRules.TraceIdHeader, out var traceId);
        if (string.IsNullOrWhiteSpace(traceId))
        {
            // create it if does not exists, this is the real first origin request
            traceId = ResourceRules.GetNewTraceId();
        }

        // put it along request items
        context.Items[ResourceRules.TraceIdHeader] = traceId;   

        // Call the next delegate/middleware in the pipeline.
        await Next(context);
    }
}

public static class OriginTraceMiddlewareExtensions
{
    public static IApplicationBuilder UseOriginTraceMiddleware(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<OriginTraceMiddleware>();
    }
}