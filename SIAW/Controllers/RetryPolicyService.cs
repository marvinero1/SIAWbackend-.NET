using Polly;
using Polly.Retry;
using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace SIAW.Controllers
{
    public class RetryPolicyService
    {
        // Exponer la política de reintento como una propiedad
        public static AsyncRetryPolicy RetryPolicy { get; } = Policy
            .Handle<DbException>() // Ajusta según los errores específicos que esperas
            .WaitAndRetryAsync(
                retryCount: 3,  // Número de intentos de reintento
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),  // Tiempo de espera exponencial (2, 4, 8 segundos)
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    Console.WriteLine($"Intento {retryCount} falló. Reintentando en {timeSpan.TotalSeconds} segundos. Error: {exception.Message}");
                });
    }
}
