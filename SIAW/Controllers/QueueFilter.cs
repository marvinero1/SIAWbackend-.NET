using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SIAW.Controllers
{
    public class QueueFilter : IAsyncActionFilter
    {
        private readonly SemaphoreSlim _semaphoreSlim;

        public QueueFilter(int maxConcurrentRequests)
        {
            _semaphoreSlim = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            await _semaphoreSlim.WaitAsync();

            try
            {
                await next();
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }
    }
    public class QueueFilterAttribute : TypeFilterAttribute
    {
        public QueueFilterAttribute(int maxConcurrentRequests) : base(typeof(QueueFilter))
        {
            Arguments = new object[] { maxConcurrentRequests };
        }
    }
}
