using Microsoft.AspNetCore.Builder;
using BonusService.Extensions.Middlewares;

namespace BonusService.Extensions
{
    public static class CustomMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogMiddleware(this IApplicationBuilder builder)
            => builder.UseMiddleware<RequestLogMiddleware>();

        public static IApplicationBuilder UseResponseLogMiddleware(this IApplicationBuilder builder)
            => builder.UseMiddleware<ResponseLogMiddleware>();
    }
}