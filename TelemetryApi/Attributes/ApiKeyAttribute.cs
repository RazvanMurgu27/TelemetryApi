using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TelemetryApi.Attributes;

[AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyAttribute: Attribute, IAsyncActionFilter
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if(!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            context.Result = new ContentResult() { StatusCode = 401, Content = "Lipseste cheia API (X-Api-Key)."};
            return;
        }

        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var validApiKey = configuration.GetValue<string>("HardwareApiKey");
        
        if (!validApiKey.Equals(extractedApiKey))
        {
            context.Result = new ContentResult() { StatusCode = 401, Content = "Cheie API invalida." };     
            return;
        }

        await next();
    }
}