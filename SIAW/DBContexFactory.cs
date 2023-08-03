using Microsoft.EntityFrameworkCore;
using SIAW.Data;

namespace SIAW
{
    public interface ICustomDbContextFactory
    {
        DBContext Create();
    }
    public class CustomDbContextFactory : ICustomDbContextFactory
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public CustomDbContextFactory(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        public DBContext Create()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var user = httpContext?.User;

            // Obtener la cadena de conexión almacenada en las claims del principal del usuario
            var connectionStringClaim = user?.FindFirst("ConnectionString");
            var connectionString = connectionStringClaim?.Value;

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new Exception("No se ha proporcionado una cadena de conexión válida.");
            }

            var optionsBuilder = new DbContextOptionsBuilder<DBContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new DBContext(optionsBuilder.Options);
        }

        
    }
}
