using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TelemetryApi.Attributes;

/// <summary>
/// Restricționează accesul la acțiunile MVC prin verificarea unei chei API
/// transmise în antetul <c>X-Api-Key</c>.
/// </summary>
[AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyAttribute : Attribute, IAsyncActionFilter
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    /// <summary>
    /// Verifică cheia API înainte de executarea acțiunii și oprește cererea
    /// dacă antetul lipsește sau conține o valoare invalidă.
    /// </summary>
    /// <param name="context">Contextul acțiunii HTTP curente.</param>
    /// <param name="next">Delegatul pentru executarea următorului filtru sau a acțiunii.</param>
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if(!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
        {
            context.Result = new ContentResult() { StatusCode = 401, Content = "Lipseste cheia API (X-Api-Key)."};
            return;
        }

        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var validApiKey = configuration.GetValue<string>("HardwareApiKey");
        
        if (!string.Equals(validApiKey, extractedApiKey.ToString(), StringComparison.Ordinal))
        {
            context.Result = new ContentResult() { StatusCode = 401, Content = "Cheie API invalida." };     
            return;
        }

        await next();
    }
}