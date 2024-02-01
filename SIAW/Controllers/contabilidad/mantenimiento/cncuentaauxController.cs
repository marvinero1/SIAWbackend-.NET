using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using siaw_DBContext.Models;

namespace SIAW.Controllers.contabilidad.mantenimiento
{
    [Route("api/contab/mant/[controller]")]
    [ApiController]
    public class cncuentaauxController : ControllerBase
    {
        private readonly UserConnectionManager _userConnectionManager;
        public cncuentaauxController(UserConnectionManager userConnectionManager)
        {
            _userConnectionManager = userConnectionManager;
        }


        // GET: api/catalogo
        [HttpGet]
        [Route("catalogo/{userConn}/{codcuenta}")]
        public async Task<ActionResult<IEnumerable<cncuentaaux>>> Getcncuentaaux_catalogo(string userConn, string codcuenta)
        {
            try
            {
                // Obtener el contexto de base de datos correspondiente al usuario
                string userConnectionString = _userConnectionManager.GetUserConnection(userConn);

                using (var _context = DbContextFactory.Create(userConnectionString))
                {
                    var query = _context.cncuentaaux
                        .Where(i => i.codcuenta == codcuenta)
                        .OrderBy(i => i.codigo)
                        .Select(i => new
                        {
                            i.codigo,
                            descripcion = ""
                        });

                    var result = query.ToList();

                    if (result.Count() == 0)
                    {
                        return BadRequest(new { resp = "No se encontraron registros con los datos proporcionados." });
                    }
                    return Ok(result);
                }

            }
            catch (Exception)
            {
                return Problem("Error en el servidor");
                throw;
            }
        }
    }
}
