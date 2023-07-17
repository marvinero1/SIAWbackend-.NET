using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace SIAW.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConnectionController : ControllerBase
    {
        public static string ConnectionString { get; private set; }

        private readonly IConfiguration _configuration;
        public ConnectionController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("{connectionName}")]
        public IActionResult GetConnectionString(string connectionName) {
            string connectionString = _configuration.GetConnectionString(connectionName);

            if (string.IsNullOrEmpty(connectionString))
            {
                return BadRequest("Nombre de conexion invalido");
            }
            if (!VerificarConexion(connectionString))
            {
                return BadRequest("No se puede establecer conexión con este Servidor");
            }
            ConnectionController.ConnectionString = connectionString;
            return Ok(true);
        }


        public static bool VerificarConexion(string cadenaConexion)
        {
            try
            {
                using (SqlConnection conexion = new SqlConnection(cadenaConexion))
                {
                    conexion.Open();
                    return true;
                }
            }
            catch (SqlException)
            {
                return false;
            }
        }

    }
}
