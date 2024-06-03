namespace SIAW.Controllers
{
    public class RequestQueueMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestQueueMiddleware> _logger;
        private readonly SemaphoreSlim _semaphoreSlim;

        public RequestQueueMiddleware(RequestDelegate next, ILogger<RequestQueueMiddleware> logger, int maxConcurrentRequests)
        {
            _next = next;
            _logger = logger;
            _semaphoreSlim = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _semaphoreSlim.WaitAsync();

            try
            {
                await _next(context);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
    }
    public static class RequestQueueMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestQueue(this IApplicationBuilder builder, int maxConcurrentRequests)
        {
            return builder.UseMiddleware<RequestQueueMiddleware>(maxConcurrentRequests);
        }
    }
}
