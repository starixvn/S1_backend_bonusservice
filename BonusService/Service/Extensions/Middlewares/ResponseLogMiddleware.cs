using System.Diagnostics;
using System.Text;

namespace BonusService.Extensions.Middlewares
{
    public class ResponseLogMiddleware
    {
        private readonly ILogger _logger;
        private readonly RequestDelegate _next;

        public ResponseLogMiddleware(RequestDelegate next, ILoggerFactory factory)
        {
            _next = next;
            _logger = factory.CreateLogger("ResponseLog");
        }

        public async Task Invoke(HttpContext context)
        {
            var sw = Stopwatch.StartNew();
            var bodyStream = context.Response.Body;

            var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            await _next(context);

            responseBodyStream.Seek(0, SeekOrigin.Begin);
            var recordContent = await new StreamReader(responseBodyStream, Encoding.UTF8, true, 1024, true).ReadToEndAsync();
            using (_logger.BeginScope("ElapsedMilliseconds: {ElapsedMilliseconds}", sw.Elapsed.TotalMilliseconds))
            {
                _logger.LogInformation($"RESPONSE: {recordContent}");
            }

            responseBodyStream.Seek(0, SeekOrigin.Begin);
            await responseBodyStream.CopyToAsync(bodyStream);
        }
    }
}