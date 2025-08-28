namespace WebApi;

public static class CsrfHeaderMiddlewareExtensions
{
    public static IApplicationBuilder CheckForCsrfHeader(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CsrfHeaderMiddleware>();
    }
}