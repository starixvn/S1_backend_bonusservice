using System.Text;

using Microsoft.AspNetCore.Http.Extensions;

using Newtonsoft.Json;

namespace BonusService.Extensions.Middlewares
{
    public class RequestLogMiddleware
    {
        private readonly ILogger _logger;
        private readonly RequestDelegate _next;

        public RequestLogMiddleware(RequestDelegate next, ILoggerFactory factory)
        {
            _next = next;
            _logger = factory.CreateLogger("RequestLog");
        }

        public async Task Invoke(HttpContext context)
        {
            context.Request.EnableBuffering();
            var url = context.Request.GetDisplayUrl();
            var requestBodyStream = new MemoryStream();
            var originalRequestBody = context.Request.Body;
            var content = string.Empty;

            if (context.Request.ContentType?.StartsWith("application/x-www-form-urlencoded") ?? false)
            {
                var formData = await context.Request.ReadFormAsync();
                content = JsonConvert.SerializeObject(formData);
            }

            await context.Request.Body.CopyToAsync(requestBodyStream);
            requestBodyStream.Seek(0, SeekOrigin.Begin);

            using (var requestBody = new StreamReader(requestBodyStream, Encoding.UTF8, true, 1024, true))
            {
                var codePage = requestBody.CurrentEncoding.CodePage;

                content = !context.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase)
                          && string.IsNullOrEmpty(content)
                    ? await requestBody.ReadToEndAsync()
                    : content;

                _logger.LogInformation(
                    $"REQUEST METHOD: {context.Request.Method}, CodePage: {codePage}, REQUEST BODY: {content}, REQUEST URL: {url}");
            }

            requestBodyStream.Seek(0, SeekOrigin.Begin);
            context.Request.Body = requestBodyStream;

            await _next(context);
            context.Request.Body = originalRequestBody;
        }
    }
}