using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using siaw_DBContext.Models;
using siaw_DBContext.Models_Extra;
using System.Globalization;

namespace SIAW.Controllers.ventas
{
    [Route("api/venta/[controller]")]
    [ApiController]
    public class usoSIAWController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public usoSIAWController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }
        [HttpGet]
        [Route("ventasbyVendedorSIAW_SIA/{userConn}/{fecha}/{codalmacen}")]
        public async Task<ActionResult<IEnumerable<object>>> ventasbyVendedorSIAW_SIA(string userConn, DateTime fecha, int codalmacen)
        {
            try
            {
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                var usuarios = new List<string>();
                switch (codalmacen)
                {
                    case 311:
                        usuarios = new List<string> { "31emilio", "31isaias", "31nelson", "31percy", "31servclie", "31varancib", "31vflores", "operador3", "opergps", "pymes01", "pymes02", "pymes03" };
                        break;
                    case 411:
                        usuarios = new List<string> { "41marcoa", "41jconde", "41jquispe", "41mcopana", "41jhonnyp", "pymes42", "31servclie", "operador3", "31percy", "opergps", "41gchura" };
                        break;
                    case 811:
                        usuarios = new List<string> { "31percy", "31servclie", "81candia", "81dseptimo", "81fvelarde", "81gzurita", "81jlobo", "81lorena", "81vteran", "operador3", "opergps", "pymes81", "pymes82" };
                        break;
                    default:
                        usuarios = null;
                        break;
                }
                /*  
                var usuariosLP = new List<string> { "41marcoa", "41jconde", "41jquispe", "41mcopana", "41jhonnyp", "pymes42", "31servclie", "operador3", "31percy", "opergps", "41gchura" };
                var usuariosCB = new List<string> { "31emilio", "31isaias", "31nelson", "31percy", "31servclie", "31varancib", "31vflores", "operador3", "opergps", "pymes01", "pymes02", "pymes03" };
                var usuariosSC = new List<string> { "31percy", "31servclie", "81candia", "81dseptimo", "81fvelarde", "81gzurita", "81jlobo", "81lorena", "81vteran", "operador3", "opergps", "pymes81", "pymes82" };
                */
                if (usuarios == null)
                {
                    return BadRequest(new { resp = "No se encontró la lista de usuario de acuerdo al código de almacen, verifique esta situación." });
                }
                var resultados = await Detalle_Proformas_Grabadas_Use_SIAW(userConnectionString, fecha, usuarios);
                return Ok(new
                {
                    ventasSIAW = resultados.resultadoSIAW,
                    ventasSIA = resultados.resultadoSIA,
                });
            }
            catch (Exception ex)
            {
                return Problem($"Error en el servidor: {ex.Message}");
                throw;
            }
        }


        private async Task<(List<dataProf> resultadoSIAW, List<dataProf> resultadoSIA)> Detalle_Proformas_Grabadas_Use_SIAW(string userConnectionString, DateTime fecha, List<string> usuarios)
        {
            List<dataProf> resultadoSIAW = new List<dataProf>();
            List<dataProf> resultadoSIA = new List<dataProf>();
            try
            {
                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var usuarioPlaceholders = string.Join(",", usuarios.Select((u, i) => $"@usuario{i}"));
                    var fechaFormateada = fecha.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);

                    var sql = $@"
                        SELECT 
                            p4.nombre1 + ' ' + p4.apellido1 + ' ' + p4.apellido2 as persona,
                            p1.login AS usuario, 
                            ISNULL(p2.fecha, '{fechaFormateada}') AS fecha_grabacion,
                            COUNT(DISTINCT p3.codigo) AS total_PF_grabadas_SIAW
                        FROM 
                            adusuario p1
                        LEFT JOIN 
                             selog p2 ON  p1.login = p2.usuario
                            AND p2.entidad = 'SW_Proforma' 
                            AND p2.detalle LIKE 'Grabar%'  
                            AND p2.fecha = '{fechaFormateada}'
                        LEFT JOIN 
                            veproforma p3 ON p2.id_doc = p3.id 
                                          AND p2.numeroid_doc = p3.numeroid
                                          AND p3.aprobada = '1'
                                          AND p3.fechaaut = '{fechaFormateada}'
                        LEFT JOIN 
                            pepersona p4 ON p1.persona = p4.codigo
                        WHERE
                            p1.login IN ({usuarioPlaceholders})  -- Aquí se inserta la lista dinámica
                        GROUP BY 
                            p4.nombre1 + ' ' + p4.apellido1 + ' ' + p4.apellido2,
                            p1.login, 
                            p2.fecha
                        ORDER BY 
                            p1.login,
                            p2.fecha;";

                    // Ejecutar la consulta SQL
                    var connection = _context.Database.GetDbConnection();
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = sql;
                        // Agregar parámetros dinámicos para cada usuario
                        for (int i = 0; i < usuarios.Count; i++)
                        {
                            var userParam = command.CreateParameter();
                            userParam.ParameterName = $"usuario{i}";
                            userParam.Value = usuarios[i];
                            command.Parameters.Add(userParam);
                        }

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var resumen = new dataProf
                                {
                                    persona = reader.GetString(reader.GetOrdinal("persona")),
                                    usuario = reader.GetString(reader.GetOrdinal("usuario")),
                                    fecha_grabacion = (reader.GetDateTime(reader.GetOrdinal("fecha_grabacion"))).ToString("dd-MM-yyyy"),
                                    total_PF_grabadas = reader.GetInt32(reader.GetOrdinal("total_PF_grabadas_SIAW")),

                                };
                                resultadoSIAW.Add(resumen);
                            }
                        }
                    }




                    //////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // SIA ESCRITORIO
                    var sql2 = $@"
                        SELECT 
                            p4.nombre1 + ' ' + p4.apellido1 + ' ' + p4.apellido2 as persona,
                            p1.login AS usuario, 
                            ISNULL(p2.fecha, '{fechaFormateada}') AS fecha_grabacion,
                            COUNT(DISTINCT p3.codigo) AS total_PF_grabadas_SIA_Escritorio
                        FROM 
                            adusuario p1
                        LEFT JOIN 
                             selog p2 ON  p1.login = p2.usuario
                            AND p2.entidad = 'Proforma' 
                            AND p2.detalle LIKE 'Grabar%'  
                            AND p2.fecha = '{fechaFormateada}'
                        LEFT JOIN 
                            veproforma p3 ON p2.id_doc = p3.id 
                                          AND p2.numeroid_doc = p3.numeroid
                                          and p3.id not like 'WF%'
                                          AND p3.aprobada = '1'
                                          AND p3.fechaaut = '{fechaFormateada}'
                        LEFT JOIN 
                            pepersona p4 ON p1.persona = p4.codigo
                        WHERE
                            p1.login IN ({usuarioPlaceholders})  -- Aquí se inserta la lista dinámica
                        GROUP BY 
                            p4.nombre1 + ' ' + p4.apellido1 + ' ' + p4.apellido2,
                            p1.login, 
                            p2.fecha
                        ORDER BY 
                            p1.login,
                            p2.fecha;";

                    // Ejecutar la consulta SQL
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = sql2;
                        // Agregar parámetros dinámicos para cada usuario
                        for (int i = 0; i < usuarios.Count; i++)
                        {
                            var userParam = command.CreateParameter();
                            userParam.ParameterName = $"usuario{i}";
                            userParam.Value = usuarios[i];
                            command.Parameters.Add(userParam);
                        }

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var resumen = new dataProf
                                {
                                    persona = reader.GetString(reader.GetOrdinal("persona")),
                                    usuario = reader.GetString(reader.GetOrdinal("usuario")),
                                    fecha_grabacion = (reader.GetDateTime(reader.GetOrdinal("fecha_grabacion"))).ToString("dd-MM-yyyy"),
                                    total_PF_grabadas = reader.GetInt32(reader.GetOrdinal("total_PF_grabadas_SIA_Escritorio")),
                                };
                                resultadoSIA.Add(resumen);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return (resultadoSIAW,resultadoSIA);
        }
    }

    public class dataProf
    {
        public string persona { get; set; }
        public string usuario { get; set; }
        public string fecha_grabacion { get; set; }
        public int total_PF_grabadas { get; set; }
    }
}
