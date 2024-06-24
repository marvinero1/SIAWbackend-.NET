using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
//using SIAW.Data;
using siaw_DBContext.Data;
using siaw_funciones;

namespace SIAW.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConnectionController : ControllerBase
    {
        public static string ConnectionString { get; private set; }
        public static string FirstConnectionString { get; private set; }

        private readonly IConfiguration _configuration;
        public ConnectionController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /*
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
        */

        [HttpGet]
        [Route("connServers/{connectionName}")]
        public IActionResult GetConnectionDBString(string connectionName)
        {
            try
            {
                // OBTENER CLAVE ENCRIPTADA
                string encryptedConnectionString = _configuration.GetConnectionString(connectionName);
                FirstConnectionString = EncryptionHelper.DecryptString(encryptedConnectionString);
                //FirstConnectionString = _configuration.GetConnectionString(connectionName);

                if (string.IsNullOrEmpty(FirstConnectionString))
                {
                    return BadRequest(new { resp = "Nombre de conexion invalido" });
                }
                if (!VerificarConexion(FirstConnectionString))
                {
                    return BadRequest(new { resp = "No se puede establecer conexión con este Servidor" });
                }

                string query = "SELECT codigo, descripcion, orden FROM adempresa order by orden asc";
                using (SqlConnection con = new SqlConnection(FirstConnectionString))
                {
                    con.Open();
                    using (SqlCommand command = new SqlCommand(query, con))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            var data = new List<Dictionary<string, object>>();

                            while (reader.Read())
                            {
                                // Crear un diccionario para cada fila
                                var row = new Dictionary<string, object>();

                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    row[reader.GetName(i)] = reader[i];
                                }

                                data.Add(row);
                            }

                            // Serializar los datos a JSON
                            string jsonData = JsonConvert.SerializeObject(data);

                            con.Close();
                            // Retornar los datos en formato JSON
                            return Ok(jsonData);

                        }
                    }
                }
            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
            }
        }


        
        [HttpGet]
        [Route("connDBs/{codigo}")]
        public IActionResult EstableceConnectionString(string codigo)
        {
            try
            {
                var connectionString = "";
                if (string.IsNullOrEmpty(FirstConnectionString))
                {
                    return BadRequest(new { resp = "Nombre de conexion invalido" });
                }
                if (!VerificarConexion(FirstConnectionString))
                {
                    return BadRequest(new { resp = "No se puede establecer conexión con este Servidor" });
                }

                string query = "SELECT conexion FROM adempresa WHERE codigo='" + codigo + "'";
                using (SqlConnection con = new SqlConnection(FirstConnectionString))
                {
                    con.Open();
                    using (SqlCommand command = new SqlCommand(query, con))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            var data = new List<Dictionary<string, object>>();

                            if (reader.Read())
                            {
                                connectionString = reader["conexion"].ToString();
                            }
                        }
                    }
                    con.Close();
                }


                if (string.IsNullOrEmpty(connectionString))
                {
                    return BadRequest(new { resp = "Nombre de conexion invalido" });
                }
                connectionString = EncryptionHelper.DecryptString(connectionString);
                if (!VerificarConexion(connectionString))
                {
                    return BadRequest(new { resp = "No se puede establecer conexión con este Servidor" });
                }
                ConnectionController.ConnectionString = connectionString;
                return Ok(true);
            }
            catch (Exception)
            {
                return BadRequest("Ocurrio un Error en el servidor");
                throw;
            }
            
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
