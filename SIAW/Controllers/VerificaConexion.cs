using Microsoft.AspNetCore.Mvc;

namespace SIAW.Controllers
{
    public class VerificaConexion
    {

        private readonly IConfiguration _configuration;
        public VerificaConexion(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        public bool VerConnection(string connectionName, string cadenaAntigua)
        {
            string connectionString = _configuration.GetConnectionString(connectionName);

            if (string.IsNullOrEmpty(connectionString))
            {
                return false;
            }
            if (connectionString != cadenaAntigua)
            {
                return false;
            }
            return true;
        }
    }
}
